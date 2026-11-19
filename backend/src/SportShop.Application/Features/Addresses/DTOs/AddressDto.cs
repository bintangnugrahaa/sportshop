namespace SportShop.Application.Features.Addresses.DTOs;

public sealed record AddressDto(
    Guid Id,
    Guid UserId,
    string Label,
    string RecipientName,
    string Phone,
    string ProvinceCode,
    string ProvinceName,
    string City,
    string? District,
    string PostalCode,
    string Street,
    bool IsDefault,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
