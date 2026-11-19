namespace SportShop.Application.Features.Orders.CancelOrder;

public sealed record CancelCustomerOrderCommand(
    string OrderNumber,
    string? Reason = null);
