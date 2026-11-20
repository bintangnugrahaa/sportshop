using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Orders;
using SportShop.Domain.Payments;

namespace SportShop.Application.Features.Orders.PayOrder;

public sealed class PayOrderCommandHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<PayOrderCommand> _validator;

    public PayOrderCommandHandler(
        ICurrentUser currentUser,
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<PayOrderCommand> validator)
    {
        _currentUser = currentUser;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<PayOrderResponseDto>> HandleAsync(
        PayOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<PayOrderResponseDto>(Error.Validation("order.validation_error", "Data permintaan bayar tidak valid.", errors));
        }

        var userId = _currentUser.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return Result.Failure<PayOrderResponseDto>(Error.Unauthorized("auth.unauthorized", "Pengguna belum terautentikasi."));
        }

        var order = await _orderRepository.GetByOrderNumberAsync(command.OrderNumber.Trim(), includeDetails: true, cancellationToken);
        if (order is null || order.UserId != userId.Value)
        {
            return Result.Failure<PayOrderResponseDto>(Error.NotFound("order.not_found", "Pesanan tidak ditemukan."));
        }

        var now = _clock.UtcNow;

        if (order.Status != OrderStatus.PendingPayment)
        {
            return Result.Failure<PayOrderResponseDto>(Error.Conflict("order.not_payable", $"Pesanan dengan status {order.Status} tidak dapat dibayar."));
        }

        if (order.ExpiresAt <= now)
        {
            return Result.Failure<PayOrderResponseDto>(Error.Conflict("order.expired", "Pesanan sudah kedaluwarsa."));
        }

        var existingPayment = await _paymentRepository.GetActivePaymentByOrderIdAsync(order.Id, cancellationToken);
        if (existingPayment is not null && !string.IsNullOrWhiteSpace(existingPayment.SnapToken))
        {
            return Result.Success(new PayOrderResponseDto(
                order.OrderNumber,
                existingPayment.SnapToken,
                existingPayment.SnapRedirectUrl,
                "pending"));
        }

        var snapItems = order.Items.Select(i => new SnapItemDto(
            i.VariantId.ToString(),
            i.ProductName,
            i.UnitPrice,
            i.Quantity
        )).ToList();
        snapItems.Add(new SnapItemDto("shipping", "Biaya Pengiriman", order.ShippingFee, 1));

        var customerEmail = _currentUser.Email ?? "customer@sportshop.dev";
        var snapRequest = new CreateSnapTransactionRequest(
            OrderNumber: order.OrderNumber,
            GrossAmount: order.Total,
            CustomerName: order.ShippingAddress.RecipientName,
            CustomerEmail: customerEmail,
            CustomerPhone: order.ShippingAddress.Phone,
            Items: snapItems);

        var snapResult = await _paymentGateway.CreateSnapTransactionAsync(snapRequest, cancellationToken);

        if (snapResult.Success && !string.IsNullOrWhiteSpace(snapResult.Token))
        {
            if (existingPayment is null)
            {
                existingPayment = new Payment(
                    Guid.NewGuid(),
                    order.Id,
                    order.Total,
                    snapResult.Token,
                    snapResult.RedirectUrl,
                    "Midtrans",
                    now);

                await _paymentRepository.AddAsync(existingPayment, cancellationToken);
            }
            else
            {
                existingPayment.SetSnapDetails(snapResult.Token, snapResult.RedirectUrl ?? string.Empty, now);
                await _paymentRepository.UpdateAsync(existingPayment, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new PayOrderResponseDto(
                order.OrderNumber,
                snapResult.Token,
                snapResult.RedirectUrl,
                "pending"));
        }

        return Result.Success(new PayOrderResponseDto(
            order.OrderNumber,
            null,
            null,
            "token_unavailable"));
    }
}
