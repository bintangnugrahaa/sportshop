namespace SportShop.Application.Features.Cart.RemoveItem;

public sealed record RemoveCartItemCommand(Guid UserId, Guid VariantId);
