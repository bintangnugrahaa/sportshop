namespace SportShop.Application.Features.Shipping.GetQuote;

public sealed record GetShippingQuoteQuery(Guid UserId, Guid AddressId);
