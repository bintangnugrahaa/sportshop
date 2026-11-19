using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Application.Mapping;
using SportShop.Domain.Common;

namespace SportShop.Application.Features.Orders.Admin.CompleteOrder;

public sealed class CompleteOrderCommandHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<CompleteOrderCommand> _validator;

    public CompleteOrderCommandHandler(
        ICurrentUser currentUser,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<CompleteOrderCommand> validator)
    {
        _currentUser = currentUser;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<OrderDto>> HandleAsync(
        CompleteOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<OrderDto>(Error.Validation("order.validation_error", "Data penyelesaian pesanan tidak valid.", errors));
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
                order.Complete(now, changedBy: adminUserId, note: command.Note);
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
