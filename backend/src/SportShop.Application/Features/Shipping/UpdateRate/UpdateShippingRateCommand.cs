namespace SportShop.Application.Features.Shipping.UpdateRate;

public sealed record UpdateShippingRateCommand(
    string ProvinceCode,
    decimal BaseFee,
    decimal PerKgFee,
    short EstimatedDaysMin,
    short EstimatedDaysMax,
    bool IsActive);
