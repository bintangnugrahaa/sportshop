namespace SportShop.Application.Features.Cart.UpdateItem;

public sealed record UpdateCartItemCommand(Guid UserId, Guid VariantId, int Quantity);
