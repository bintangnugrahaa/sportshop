namespace SportShop.Application.Features.Addresses.UpdateAddress;

public sealed record UpdateAddressCommand(
    Guid UserId,
    Guid AddressId,
    string Label,
    string RecipientName,
    string Phone,
    string ProvinceCode,
    string City,
    string? District,
    string PostalCode,
    string Street,
    bool IsDefault);
