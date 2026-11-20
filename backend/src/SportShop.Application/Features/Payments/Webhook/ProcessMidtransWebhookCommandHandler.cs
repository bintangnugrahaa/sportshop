using System.Globalization;
using System.Text.Json;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;
using SportShop.Domain.Payments;

namespace SportShop.Application.Features.Payments.Webhook;

public sealed class ProcessMidtransWebhookCommandHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IOrderNotificationService _notificationService;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ProcessMidtransWebhookCommandHandler(
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IProductRepository productRepository,
        IPaymentGateway paymentGateway,
        IOrderNotificationService notificationService,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _productRepository = productRepository;
        _paymentGateway = paymentGateway;
        _notificationService = notificationService;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<WebhookProcessResult>> HandleAsync(
        ProcessMidtransWebhookCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RawPayload))
        {
            return Result.Failure<WebhookProcessResult>(Error.Validation("webhook.payload_empty", "Payload webhook kosong."));
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(command.RawPayload);
        }
        catch (JsonException ex)
        {
            return Result.Failure<WebhookProcessResult>(Error.Validation("webhook.invalid_json", $"Format JSON payload tidak valid: {ex.Message}"));
        }

        using (doc)
        {
            var root = doc.RootElement;
            var orderId = root.TryGetProperty("order_id", out var oi) ? oi.GetString()?.Trim() ?? string.Empty : string.Empty;
            var statusCode = root.TryGetProperty("status_code", out var sc) ? sc.GetString()?.Trim() ?? string.Empty : string.Empty;
            var signatureKey = root.TryGetProperty("signature_key", out var sk) ? sk.GetString()?.Trim() ?? string.Empty : string.Empty;
            var transactionStatus = root.TryGetProperty("transaction_status", out var ts) ? ts.GetString()?.Trim().ToLowerInvariant() ?? string.Empty : string.Empty;
            var fraudStatus = root.TryGetProperty("fraud_status", out var fs) ? fs.GetString()?.Trim().ToLowerInvariant() : null;
            var transactionId = root.TryGetProperty("transaction_id", out var ti) ? ti.GetString()?.Trim() : null;
            var paymentType = root.TryGetProperty("payment_type", out var pt) ? pt.GetString()?.Trim() : null;
            var transactionTimeStr = root.TryGetProperty("transaction_time", out var tt) ? tt.GetString()?.Trim() : null;
            var settlementTimeStr = root.TryGetProperty("settlement_time", out var st) ? st.GetString()?.Trim() : null;

            string grossAmountStr = string.Empty;
            decimal grossAmount = 0;
            if (root.TryGetProperty("gross_amount", out var ga))
            {
                if (ga.ValueKind == JsonValueKind.String)
                {
                    grossAmountStr = ga.GetString()?.Trim() ?? string.Empty;
                    decimal.TryParse(grossAmountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out grossAmount);
                }
                else if (ga.ValueKind == JsonValueKind.Number)
                {
                    grossAmount = ga.GetDecimal();
                    grossAmountStr = grossAmount.ToString("0.00", CultureInfo.InvariantCulture);
                }
            }

            // 1. Verify signature using constant-time comparison via gateway
            var isSignatureValid = _paymentGateway.VerifySignature(orderId, statusCode, grossAmountStr, signatureKey);
            if (!isSignatureValid)
            {
                return Result.Failure<WebhookProcessResult>(Error.Unauthorized("midtrans.invalid_signature", "Signature Midtrans tidak valid."));
            }

            // 2. Check deduplication key
            var dedupKey = PaymentNotification.BuildDedupKey(orderId, transactionStatus, statusCode, transactionTimeStr);
            var isDuplicate = await _paymentRepository.HasNotificationBeenProcessedAsync(dedupKey, cancellationToken);
            if (isDuplicate)
            {
                return Result.Success(new WebhookProcessResult("duplicate_ignored", "Notifikasi duplikat berhasil diabaikan."));
            }

            // 3. Confirm with Midtrans Status API
            var statusApiResult = await _paymentGateway.GetPaymentStatusAsync(orderId, cancellationToken);
            if (statusApiResult is not null)
            {
                transactionStatus = statusApiResult.TransactionStatus.ToLowerInvariant();
                fraudStatus = statusApiResult.FraudStatus?.ToLowerInvariant() ?? fraudStatus;
                transactionId = statusApiResult.TransactionId ?? transactionId;
                paymentType = statusApiResult.PaymentType ?? paymentType;
            }

            // 4. Find order
            var order = await _orderRepository.GetByOrderNumberAsync(orderId, includeDetails: true, cancellationToken);
            if (order is null)
            {
                return Result.Failure<WebhookProcessResult>(Error.NotFound("order.not_found", $"Pesanan {orderId} tidak ditemukan."));
            }

            // 5. Verify gross amount
            var now = _clock.UtcNow;
            if (Math.Abs(order.Total - grossAmount) > 0.01m)
            {
                var mismatchNotification = new PaymentNotification(
                    Guid.NewGuid(),
                    null,
                    orderId,
                    transactionId,
                    transactionStatus,
                    statusCode,
                    signatureValid: true,
                    payload: command.RawPayload,
                    receivedAt: now,
                    dedupKey: dedupKey);

                await _paymentRepository.AddNotificationAsync(mismatchNotification, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Success(new WebhookProcessResult("amount_mismatch", "Jumlah pembayaran tidak sesuai dengan total pesanan."));
            }

            // 6. Find or create payment entity
            var payment = await _paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            if (payment is null)
            {
                payment = new Payment(
                    Guid.NewGuid(),
                    order.Id,
                    grossAmount,
                    null,
                    null,
                    "Midtrans",
                    now);
                await _paymentRepository.AddAsync(payment, cancellationToken);
            }

            // 7. Map status and apply transitions
            var mappedPaymentStatus = MapPaymentStatus(transactionStatus, fraudStatus);

            DateTime? parsedTransactionTime = null;
            if (DateTime.TryParse(transactionTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var ptt))
            {
                parsedTransactionTime = DateTime.SpecifyKind(ptt, DateTimeKind.Utc);
            }

            DateTime? parsedSettlementTime = null;
            if (DateTime.TryParse(settlementTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var pst))
            {
                parsedSettlementTime = DateTime.SpecifyKind(pst, DateTimeKind.Utc);
            }

            var transitionedToPaid = false;

            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                payment.ApplyNotification(
                    mappedPaymentStatus,
                    transactionId,
                    paymentType,
                    fraudStatus,
                    parsedTransactionTime,
                    parsedSettlementTime,
                    command.RawPayload,
                    now);

                if (mappedPaymentStatus is PaymentStatus.Settlement or PaymentStatus.Capture)
                {
                    if (order.Status == OrderStatus.PendingPayment)
                    {
                        order.MarkPaid(now, note: $"Pembayaran berhasil diverifikasi ({transactionStatus})");
                        await _orderRepository.UpdateAsync(order, ct);
                        transitionedToPaid = true;
                    }
                }
                else if (mappedPaymentStatus == PaymentStatus.Cancel)
                {
                    if (order.Status == OrderStatus.PendingPayment)
                    {
                        order.Cancel("Dibatalkan oleh sistem pembayaran Midtrans", now);
                        foreach (var item in order.Items)
                        {
                            await _productRepository.RestoreStockAtomicAsync(item.VariantId, item.Quantity, now, ct);
                            var movement = new InventoryMovement(
                                Guid.NewGuid(),
                                item.VariantId,
                                item.Quantity,
                                StockReason.Cancel,
                                $"Pembatalan Midtrans pesanan {order.OrderNumber}",
                                order.Id,
                                null,
                                now);
                            await _productRepository.AddInventoryMovementAsync(movement, ct);
                        }
                        await _orderRepository.UpdateAsync(order, ct);
                    }
                }
                else if (mappedPaymentStatus == PaymentStatus.Expire)
                {
                    if (order.Status == OrderStatus.PendingPayment)
                    {
                        order.Expire(now, note: "Kedaluwarsa oleh sistem pembayaran Midtrans");
                        foreach (var item in order.Items)
                        {
                            await _productRepository.RestoreStockAtomicAsync(item.VariantId, item.Quantity, now, ct);
                            var movement = new InventoryMovement(
                                Guid.NewGuid(),
                                item.VariantId,
                                item.Quantity,
                                StockReason.Expire,
                                $"Kedaluwarsa Midtrans pesanan {order.OrderNumber}",
                                order.Id,
                                null,
                                now);
                            await _productRepository.AddInventoryMovementAsync(movement, ct);
                        }
                        await _orderRepository.UpdateAsync(order, ct);
                    }
                }

                await _paymentRepository.UpdateAsync(payment, ct);

                var notification = new PaymentNotification(
                    Guid.NewGuid(),
                    payment.Id,
                    order.OrderNumber,
                    transactionId,
                    transactionStatus,
                    statusCode,
                    signatureValid: true,
                    payload: command.RawPayload,
                    receivedAt: now,
                    dedupKey: dedupKey);

                await _paymentRepository.AddNotificationAsync(notification, ct);
            }, cancellationToken);

            if (transitionedToPaid)
            {
                var profileResult = await _identityService.GetUserProfileAsync(order.UserId, cancellationToken);
                var email = profileResult.IsSuccess ? profileResult.Value.Email : "customer@sportshop.dev";
                _notificationService.EnqueueOrderPaidEmail(email, order.ShippingAddress.RecipientName, order.OrderNumber, order.Total, now);
            }

            return Result.Success(new WebhookProcessResult("processed", $"Notifikasi berhasil diproses: {mappedPaymentStatus}"));
        }
    }

    private static PaymentStatus MapPaymentStatus(string transactionStatus, string? fraudStatus)
    {
        return transactionStatus switch
        {
            "capture" => fraudStatus == "accept" ? PaymentStatus.Capture : PaymentStatus.Pending,
            "settlement" => PaymentStatus.Settlement,
            "pending" => PaymentStatus.Pending,
            "deny" => PaymentStatus.Deny,
            "failure" => PaymentStatus.Failure,
            "cancel" => PaymentStatus.Cancel,
            "expire" => PaymentStatus.Expire,
            "refund" or "partial_refund" => PaymentStatus.Refund,
            _ => PaymentStatus.Pending
        };
    }
}
