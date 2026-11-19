using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Orders.Admin.ListAdminOrders;

public sealed class ListAdminOrdersQueryHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IValidator<ListAdminOrdersQuery> _validator;

    public ListAdminOrdersQueryHandler(
        IOrderRepository orderRepository,
        IValidator<ListAdminOrdersQuery> validator)
    {
        _orderRepository = orderRepository;
        _validator = validator;
    }

    public async Task<Result<PagedResult<OrderDto>>> HandleAsync(
        ListAdminOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<PagedResult<OrderDto>>(Error.Validation("order.validation_error", "Parameter kueri tidak valid.", errors));
        }

        var (items, totalCount) = await _orderRepository.GetAdminOrdersPagedAsync(
            query.Status,
            query.FromDate,
            query.ToDate,
            query.Q,
            query.Page,
            query.PageSize,
            cancellationToken);

        var dtos = items.Select(o => o.ToDto()).ToList();
        return Result.Success(PagedResult.Create(dtos, query.Page, query.PageSize, totalCount));
    }
}
