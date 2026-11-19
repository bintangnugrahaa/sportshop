using SportShop.Domain.Common;

namespace SportShop.Domain.Shipping;

public static class ShippingCalculator
{
    public const int MinimumWeightGram = 1000;

    public static ShippingCalculationResult Calculate(
        ShippingRate rate,
        int totalWeightGram)
    {
        ArgumentNullException.ThrowIfNull(rate);

        if (!rate.IsActive)
        {
            throw new DomainException("shipping.rate_inactive", $"Tarif pengiriman untuk provinsi {rate.ProvinceName} sedang tidak aktif.");
        }

        if (totalWeightGram < 0)
        {
            throw new DomainException("shipping.invalid_weight", "Berat total pengiriman tidak boleh negatif.");
        }

        // Business rule: Min weight 1 kg, ceil(berat_total_gram / 1000)
        var effectiveGram = Math.Max(MinimumWeightGram, totalWeightGram);
        var billableWeightKg = (int)Math.Ceiling(effectiveGram / 1000.0);

        var totalFee = rate.BaseFee + (billableWeightKg * rate.PerKgFee);

        return new ShippingCalculationResult(
            rate.ProvinceCode,
            rate.ProvinceName,
            rate.BaseFee,
            rate.PerKgFee,
            totalWeightGram,
            billableWeightKg,
            totalFee,
            rate.EstimatedDaysMin,
            rate.EstimatedDaysMax);
    }
}

public sealed record ShippingCalculationResult(
    string ProvinceCode,
    string ProvinceName,
    decimal BaseFee,
    decimal PerKgFee,
    int TotalWeightGram,
    int BillableWeightKg,
    decimal TotalFee,
    short EstimatedDaysMin,
    short EstimatedDaysMax);
