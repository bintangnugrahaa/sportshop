namespace SportShop.Application.Features.Shipping.DTOs;

public sealed record ShippingQuoteDto(
    Guid AddressId,
    string RecipientName,
    string ProvinceCode,
    string ProvinceName,
    string City,
    string PostalCode,
    int TotalWeightGram,
    int BillableWeightKg,
    decimal BaseFee,
    decimal PerKgFee,
    decimal ShippingFee,
    short EstimatedDaysMin,
    short EstimatedDaysMax,
    decimal CartSubtotal,
    decimal EstimatedTotal);
