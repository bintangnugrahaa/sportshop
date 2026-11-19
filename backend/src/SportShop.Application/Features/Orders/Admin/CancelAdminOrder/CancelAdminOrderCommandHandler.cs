using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Application.Mapping;
using SportShop.Domain.Common;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;

namespace SportShop.Application.Features.Orders.Admin.CancelAdminOrder;

public sealed class CancelAdminOrderCommandHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<CancelAdminOrderCommand> _validator;

    public CancelAdminOrderCommandHandler(
        ICurrentUser currentUser,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<CancelAdminOrderCommand> validator)
    {
        _currentUser = currentUser;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<OrderDto>> HandleAsync(
        CancelAdminOrderCommand command,
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

        var order = await _orderRepository.GetByOrderNumberAsync(command.OrderNumber.Trim(), includeDetails: true, cancellationToken);
        if (order is null)
        {
            return Result.Failure<OrderDto>(Error.NotFound("order.not_found", "Pesanan tidak ditemukan."));
        }

        var now = _clock.UtcNow;
        var adminUserId = _currentUser.UserId;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                order.Cancel(command.Reason, now, changedBy: adminUserId);

                foreach (var item in order.Items)
                {
                    await _productRepository.RestoreStockAtomicAsync(item.VariantId, item.Quantity, now, ct);

                    var movement = new InventoryMovement(
                        Guid.NewGuid(),
                        item.VariantId,
                        item.Quantity,
                        StockReason.Cancel,
                        $"Pembatalan pesanan {order.OrderNumber} oleh Admin",
                        order.Id,
                        adminUserId,
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

        return Result.Success(order.ToDto());
    }
}
