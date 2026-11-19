namespace SportShop.Application.Features.Orders.Checkout;

public sealed record CheckoutOrderCommand(
    Guid AddressId,
    string? IdempotencyKey = null);
