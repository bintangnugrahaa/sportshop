using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Orders.ListCustomerOrders;

public sealed class ListCustomerOrdersQueryHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IOrderRepository _orderRepository;
    private readonly IValidator<ListCustomerOrdersQuery> _validator;

    public ListCustomerOrdersQueryHandler(
        ICurrentUser currentUser,
        IOrderRepository orderRepository,
        IValidator<ListCustomerOrdersQuery> validator)
    {
        _currentUser = currentUser;
        _orderRepository = orderRepository;
        _validator = validator;
    }

    public async Task<Result<PagedResult<OrderDto>>> HandleAsync(
        ListCustomerOrdersQuery query,
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

        var userId = _currentUser.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return Result.Failure<PagedResult<OrderDto>>(Error.Unauthorized("auth.unauthorized", "Pengguna belum terautentikasi."));
        }

        var (items, totalCount) = await _orderRepository.GetCustomerOrdersPagedAsync(
            userId.Value,
            query.Status,
            query.Page,
            query.PageSize,
            cancellationToken);

        var dtos = items.Select(o => o.ToDto()).ToList();
        return Result.Success(PagedResult.Create(dtos, query.Page, query.PageSize, totalCount));
    }
}
