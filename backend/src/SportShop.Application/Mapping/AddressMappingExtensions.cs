using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Domain.Users;

namespace SportShop.Application.Mapping;

public static class AddressMappingExtensions
{
    public static AddressDto ToDto(this Address address) =>
        new(
            address.Id,
            address.UserId,
            address.Label,
            address.RecipientName,
            address.Phone,
            address.ProvinceCode,
            address.Province?.ProvinceName ?? address.ProvinceCode,
            address.City,
            address.District,
            address.PostalCode,
            address.Street,
            address.IsDefault,
            address.CreatedAt,
            address.UpdatedAt);

    public static List<AddressDto> ToDtoList(this IEnumerable<Address> addresses) =>
        addresses.Select(ToDto).ToList();
}
