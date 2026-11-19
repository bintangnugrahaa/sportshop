namespace SportShop.Application.Features.Shipping.DTOs;

public sealed record ShippingRateDto(
    string ProvinceCode,
    string ProvinceName,
    decimal BaseFee,
    decimal PerKgFee,
    short EstimatedDaysMin,
    short EstimatedDaysMax,
    bool IsActive,
    DateTime? UpdatedAt);
