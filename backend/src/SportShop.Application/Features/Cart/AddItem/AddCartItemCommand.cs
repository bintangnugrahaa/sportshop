namespace SportShop.Application.Features.Cart.AddItem;

public sealed record AddCartItemCommand(Guid UserId, Guid VariantId, int Quantity);
