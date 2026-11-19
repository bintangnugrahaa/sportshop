namespace SportShop.Application.Features.Addresses.DeleteAddress;

public sealed record DeleteAddressCommand(Guid UserId, Guid AddressId);
