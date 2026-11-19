namespace SportShop.Application.Features.Addresses.CreateAddress;

public sealed record CreateAddressCommand(
    Guid UserId,
    string Label,
    string RecipientName,
    string Phone,
    string ProvinceCode,
    string City,
    string? District,
    string PostalCode,
    string Street,
    bool IsDefault);
