using Hangfire;
using Microsoft.Extensions.Logging;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;
using SportShop.Domain.Payments;

namespace SportShop.Infrastructure.Jobs;

[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public sealed partial class ReconcilePaymentsJob : IReconcilePaymentsJob
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<ReconcilePaymentsJob> _logger;

    public ReconcilePaymentsJob(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<ReconcilePaymentsJob> logger)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var threshold = now.AddMinutes(-15);

        var pendingPayments = await _paymentRepository.GetPendingPaymentsOlderThanAsync(threshold, limit: 100, cancellationToken);
        if (pendingPayments.Count == 0)
        {
            return;
        }

        LogProcessingReconciliations(_logger, pendingPayments.Count);

        foreach (var payment in pendingPayments)
        {
            if (payment.Order is null)
            {
                continue;
            }

            try
            {
                var order = payment.Order;
                var statusResult = await _paymentGateway.GetPaymentStatusAsync(order.OrderNumber, cancellationToken);
                if (statusResult is null)
                {
                    continue;
                }

                var mappedStatus = MapPaymentStatus(statusResult.TransactionStatus, statusResult.FraudStatus);
                if (mappedStatus == PaymentStatus.Pending)
                {
                    // Still pending in Midtrans
                    continue;
                }

                await _unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    payment.ApplyNotification(
                        mappedStatus,
                        statusResult.TransactionId,
                        statusResult.PaymentType,
                        statusResult.FraudStatus,
                        statusResult.TransactionTime,
                        statusResult.SettlementTime,
                        statusResult.RawResponse,
                        now);

                    if (mappedStatus is PaymentStatus.Settlement or PaymentStatus.Capture)
                    {
                        if (order.Status == OrderStatus.PendingPayment)
                        {
                            order.MarkPaid(now, note: "Pembayaran disinkronkan oleh ReconcilePaymentsJob");
                            await _orderRepository.UpdateAsync(order, ct);
                        }
                    }
                    else if (mappedStatus == PaymentStatus.Cancel)
                    {
                        if (order.Status == OrderStatus.PendingPayment)
                        {
                            order.Cancel("Dibatalkan menurut Midtrans (rekonsiliasi)", now);
                            foreach (var item in order.Items)
                            {
                                await _productRepository.RestoreStockAtomicAsync(item.VariantId, item.Quantity, now, ct);
                                var movement = new InventoryMovement(
                                    Guid.NewGuid(),
                                    item.VariantId,
                                    item.Quantity,
                                    StockReason.Cancel,
                                    $"Pembatalan Midtrans rekonsiliasi pesanan {order.OrderNumber}",
                                    order.Id,
                                    null,
                                    now);
                                await _productRepository.AddInventoryMovementAsync(movement, ct);
                            }
                            await _orderRepository.UpdateAsync(order, ct);
                        }
                    }
                    else if (mappedStatus == PaymentStatus.Expire)
                    {
                        if (order.Status == OrderStatus.PendingPayment)
                        {
                            order.Expire(now, note: "Kedaluwarsa menurut Midtrans (rekonsiliasi)");
                            foreach (var item in order.Items)
                            {
                                await _productRepository.RestoreStockAtomicAsync(item.VariantId, item.Quantity, now, ct);
                                var movement = new InventoryMovement(
                                    Guid.NewGuid(),
                                    item.VariantId,
                                    item.Quantity,
                                    StockReason.Expire,
                                    $"Kedaluwarsa Midtrans rekonsiliasi pesanan {order.OrderNumber}",
                                    order.Id,
                                    null,
                                    now);
                                await _productRepository.AddInventoryMovementAsync(movement, ct);
                            }
                            await _orderRepository.UpdateAsync(order, ct);
                        }
                    }

                    await _paymentRepository.UpdateAsync(payment, ct);

                    var dedupKey = PaymentNotification.BuildDedupKey(
                        order.OrderNumber,
                        statusResult.TransactionStatus,
                        statusResult.StatusCode,
                        statusResult.TransactionTime?.ToString("o"));

                    if (!await _paymentRepository.HasNotificationBeenProcessedAsync(dedupKey, ct))
                    {
                        var notification = new PaymentNotification(
                            Guid.NewGuid(),
                            payment.Id,
                            order.OrderNumber,
                            statusResult.TransactionId,
                            statusResult.TransactionStatus,
                            statusResult.StatusCode,
                            signatureValid: true,
                            payload: statusResult.RawResponse ?? "{}",
                            receivedAt: now,
                            dedupKey: dedupKey);

                        await _paymentRepository.AddNotificationAsync(notification, ct);
                    }
                }, cancellationToken);

                LogReconciledSuccess(_logger, order.OrderNumber, mappedStatus.ToString(), order.Status.ToString());
            }
            catch (Exception ex)
            {
                LogReconciliationFailed(_logger, payment.Order.OrderNumber, ex.Message, ex);
            }
        }
    }

    private static PaymentStatus MapPaymentStatus(string transactionStatus, string? fraudStatus)
    {
        return transactionStatus.ToLowerInvariant() switch
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Checking {Count} pending payments older than 15 minutes for reconciliation")]
    private static partial void LogProcessingReconciliations(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reconciled order {OrderNumber}: payment -> {PaymentStatus}, order -> {OrderStatus}")]
    private static partial void LogReconciledSuccess(ILogger logger, string orderNumber, string paymentStatus, string orderStatus);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to reconcile payment for order {OrderNumber}: {ErrorMessage}")]
    private static partial void LogReconciliationFailed(ILogger logger, string orderNumber, string errorMessage, Exception ex);
}
