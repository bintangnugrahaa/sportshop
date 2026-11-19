using SportShop.Domain.Orders;

namespace SportShop.Application.Features.Orders.ListCustomerOrders;

public sealed record ListCustomerOrdersQuery(
    OrderStatus? Status = null,
    int Page = 1,
    int PageSize = 20);
