using SportShop.Domain.Common;

namespace SportShop.Domain.Shipping;

public class ShippingRate
{
    public string ProvinceCode { get; set; } = string.Empty;
    public string ProvinceName { get; set; } = string.Empty;
    public decimal BaseFee { get; set; }
    public decimal PerKgFee { get; set; }
    public short EstimatedDaysMin { get; set; }
    public short EstimatedDaysMax { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? UpdatedAt { get; set; }

    public ShippingRate()
    {
    }

    public ShippingRate(
        string provinceCode,
        string provinceName,
        decimal baseFee,
        decimal perKgFee,
        short estimatedDaysMin,
        short estimatedDaysMax,
        bool isActive = true,
        DateTime? updatedAt = null)
    {
        if (string.IsNullOrWhiteSpace(provinceCode))
        {
            throw new DomainException("shipping.invalid_province_code", "Kode provinsi tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(provinceName))
        {
            throw new DomainException("shipping.invalid_province_name", "Nama provinsi tidak boleh kosong.");
        }

        if (baseFee < 0)
        {
            throw new DomainException("shipping.invalid_base_fee", "Biaya dasar tidak boleh negatif.");
        }

        if (perKgFee < 0)
        {
            throw new DomainException("shipping.invalid_per_kg_fee", "Biaya per kg tidak boleh negatif.");
        }

        if (estimatedDaysMin < 0)
        {
            throw new DomainException("shipping.invalid_estimated_days", "Estimasi hari minimum tidak boleh negatif.");
        }

        if (estimatedDaysMax < estimatedDaysMin)
        {
            throw new DomainException("shipping.invalid_estimated_days", "Estimasi hari maksimum tidak boleh lebih kecil dari minimum.");
        }

        ProvinceCode = provinceCode.Trim().ToUpperInvariant();
        ProvinceName = provinceName.Trim();
        BaseFee = baseFee;
        PerKgFee = perKgFee;
        EstimatedDaysMin = estimatedDaysMin;
        EstimatedDaysMax = estimatedDaysMax;
        IsActive = isActive;
        UpdatedAt = updatedAt;
    }

    public void UpdateRate(
        decimal baseFee,
        decimal perKgFee,
        short estimatedDaysMin,
        short estimatedDaysMax,
        bool isActive,
        DateTime now)
    {
        if (baseFee < 0)
        {
            throw new DomainException("shipping.invalid_base_fee", "Biaya dasar tidak boleh negatif.");
        }

        if (perKgFee < 0)
        {
            throw new DomainException("shipping.invalid_per_kg_fee", "Biaya per kg tidak boleh negatif.");
        }

        if (estimatedDaysMin < 0)
        {
            throw new DomainException("shipping.invalid_estimated_days", "Estimasi hari minimum tidak boleh negatif.");
        }

        if (estimatedDaysMax < estimatedDaysMin)
        {
            throw new DomainException("shipping.invalid_estimated_days", "Estimasi hari maksimum tidak boleh lebih kecil dari minimum.");
        }

        BaseFee = baseFee;
        PerKgFee = perKgFee;
        EstimatedDaysMin = estimatedDaysMin;
        EstimatedDaysMax = estimatedDaysMax;
        IsActive = isActive;
        UpdatedAt = now;
    }
}
