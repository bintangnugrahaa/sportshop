namespace SportShop.Application.Features.Orders.Admin.CompleteOrder;

public sealed record CompleteOrderCommand(string OrderNumber, string? Note = null);
