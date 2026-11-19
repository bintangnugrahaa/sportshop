using SportShop.Domain.Orders;

namespace SportShop.Application.Features.Orders.Admin.ListAdminOrders;

public sealed record ListAdminOrdersQuery(
    OrderStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Q = null,
    int Page = 1,
    int PageSize = 20);
