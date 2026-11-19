using SportShop.Application.Features.Shipping.DTOs;
using SportShop.Domain.Shipping;

namespace SportShop.Application.Mapping;

public static class ShippingMappingExtensions
{
    public static ShippingRateDto ToDto(this ShippingRate rate) =>
        new(
            rate.ProvinceCode,
            rate.ProvinceName,
            rate.BaseFee,
            rate.PerKgFee,
            rate.EstimatedDaysMin,
            rate.EstimatedDaysMax,
            rate.IsActive,
            rate.UpdatedAt);

    public static List<ShippingRateDto> ToDtoList(this IEnumerable<ShippingRate> rates) =>
        rates.Select(ToDto).ToList();
}
