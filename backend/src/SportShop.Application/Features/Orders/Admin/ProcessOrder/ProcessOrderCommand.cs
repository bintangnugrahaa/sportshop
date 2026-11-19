namespace SportShop.Application.Features.Orders.Admin.ProcessOrder;

public sealed record ProcessOrderCommand(string OrderNumber, string? Note = null);
