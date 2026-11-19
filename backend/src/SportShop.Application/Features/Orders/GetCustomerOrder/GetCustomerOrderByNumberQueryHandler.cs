using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Orders.GetCustomerOrder;

public sealed class GetCustomerOrderByNumberQueryHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IOrderRepository _orderRepository;
    private readonly IValidator<GetCustomerOrderByNumberQuery> _validator;

    public GetCustomerOrderByNumberQueryHandler(
        ICurrentUser currentUser,
        IOrderRepository orderRepository,
        IValidator<GetCustomerOrderByNumberQuery> validator)
    {
        _currentUser = currentUser;
        _orderRepository = orderRepository;
        _validator = validator;
    }

    public async Task<Result<OrderDto>> HandleAsync(
        GetCustomerOrderByNumberQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<OrderDto>(Error.Validation("order.validation_error", "Parameter kueri tidak valid.", errors));
        }

        var userId = _currentUser.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return Result.Failure<OrderDto>(Error.Unauthorized("auth.unauthorized", "Pengguna belum terautentikasi."));
        }

        var order = await _orderRepository.GetByOrderNumberAsync(query.OrderNumber.Trim(), includeDetails: true, cancellationToken);
        if (order is null || order.UserId != userId.Value)
        {
            return Result.Failure<OrderDto>(Error.NotFound("order.not_found", "Pesanan tidak ditemukan."));
        }

        return Result.Success(order.ToDto());
    }
}
