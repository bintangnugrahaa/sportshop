namespace SportShop.Application.Features.Orders.Admin.CancelAdminOrder;

public sealed record CancelAdminOrderCommand(string OrderNumber, string? Reason = null);
