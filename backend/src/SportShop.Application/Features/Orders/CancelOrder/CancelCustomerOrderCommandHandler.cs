using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Application.Mapping;
using SportShop.Domain.Common;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;

namespace SportShop.Application.Features.Orders.CancelOrder;

public sealed class CancelCustomerOrderCommandHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<CancelCustomerOrderCommand> _validator;

    public CancelCustomerOrderCommandHandler(
        ICurrentUser currentUser,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<CancelCustomerOrderCommand> validator)
    {
        _currentUser = currentUser;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<OrderDto>> HandleAsync(
        CancelCustomerOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<OrderDto>(Error.Validation("order.validation_error", "Data pembatalan pesanan tidak valid.", errors));
        }

        var userId = _currentUser.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return Result.Failure<OrderDto>(Error.Unauthorized("auth.unauthorized", "Pengguna belum terautentikasi."));
        }

        var order = await _orderRepository.GetByOrderNumberAsync(command.OrderNumber.Trim(), includeDetails: true, cancellationToken);
        if (order is null || order.UserId != userId.Value)
        {
            return Result.Failure<OrderDto>(Error.NotFound("order.not_found", "Pesanan tidak ditemukan."));
        }

        // Customer can only cancel PendingPayment orders
        if (order.Status != OrderStatus.PendingPayment)
        {
            return Result.Failure<OrderDto>(Error.Conflict("order.invalid_transition", $"Transisi status dari {order.Status} ke Cancelled tidak diizinkan."));
        }

        var now = _clock.UtcNow;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                order.Cancel(command.Reason, now, changedBy: userId.Value);

                foreach (var item in order.Items)
                {
                    await _productRepository.RestoreStockAtomicAsync(item.VariantId, item.Quantity, now, ct);

                    var movement = new InventoryMovement(
                        Guid.NewGuid(),
                        item.VariantId,
                        item.Quantity,
                        StockReason.Cancel,
                        $"Pembatalan pesanan {order.OrderNumber}",
                        order.Id,
                        userId.Value,
                        now);

                    await _productRepository.AddInventoryMovementAsync(movement, ct);
                }

                await _orderRepository.UpdateAsync(order, ct);
            }, cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<OrderDto>(Error.Conflict(ex.Code, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<OrderDto>(Error.Unexpected("debug.error", ex.ToString()));
        }

        return Result.Success(order.ToDto());
    }
}
