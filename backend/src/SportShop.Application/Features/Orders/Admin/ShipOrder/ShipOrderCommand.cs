namespace SportShop.Application.Features.Orders.Admin.ShipOrder;

public sealed record ShipOrderCommand(
    string OrderNumber,
    string Courier,
    string TrackingNumber,
    string? Note = null);
