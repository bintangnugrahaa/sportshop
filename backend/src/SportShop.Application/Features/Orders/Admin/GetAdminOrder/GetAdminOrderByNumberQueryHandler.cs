using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Orders.Admin.GetAdminOrder;

public sealed class GetAdminOrderByNumberQueryHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IValidator<GetAdminOrderByNumberQuery> _validator;

    public GetAdminOrderByNumberQueryHandler(
        IOrderRepository orderRepository,
        IValidator<GetAdminOrderByNumberQuery> validator)
    {
        _orderRepository = orderRepository;
        _validator = validator;
    }

    public async Task<Result<OrderDto>> HandleAsync(
        GetAdminOrderByNumberQuery query,
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

        var order = await _orderRepository.GetByOrderNumberAsync(query.OrderNumber.Trim(), includeDetails: true, cancellationToken);
        if (order is null)
        {
            return Result.Failure<OrderDto>(Error.NotFound("order.not_found", "Pesanan tidak ditemukan."));
        }

        return Result.Success(order.ToDto());
    }
}
