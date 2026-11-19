using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportShop.Domain.Shipping;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class ShippingRateConfiguration : IEntityTypeConfiguration<ShippingRate>
{
    public void Configure(EntityTypeBuilder<ShippingRate> builder)
    {
        builder.ToTable("shipping_rates", t =>
        {
            t.HasCheckConstraint("ck_shipping_rates_base_fee", "base_fee >= 0");
            t.HasCheckConstraint("ck_shipping_rates_per_kg_fee", "per_kg_fee >= 0");
            t.HasCheckConstraint("ck_shipping_rates_estimated_days_min", "estimated_days_min >= 0");
            t.HasCheckConstraint("ck_shipping_rates_estimated_days_max", "estimated_days_max >= estimated_days_min");
        });

        builder.HasKey(r => r.ProvinceCode);

        builder.Property(r => r.ProvinceCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(r => r.ProvinceName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.BaseFee)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(r => r.PerKgFee)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(r => r.EstimatedDaysMin)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(r => r.EstimatedDaysMax)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasData(GetInitialProvinces());
    }

    public static ShippingRate[] GetInitialProvinces() =>
    [
        new("JK", "DKI Jakarta", 10_000m, 5_000m, 1, 2),
        new("JB", "Jawa Barat", 11_000m, 6_000m, 1, 3),
        new("BT", "Banten", 11_000m, 6_000m, 1, 3),
        new("JT", "Jawa Tengah", 15_000m, 8_000m, 2, 4),
        new("YO", "DI Yogyakarta", 15_000m, 8_000m, 2, 4),
        new("JI", "Jawa Timur", 16_000m, 9_000m, 2, 4),
        new("BA", "Bali", 20_000m, 12_000m, 3, 5),
        new("NB", "Nusa Tenggara Barat", 25_000m, 15_000m, 3, 6),
        new("NT", "Nusa Tenggara Timur", 30_000m, 18_000m, 4, 7),
        new("AC", "Aceh", 28_000m, 17_000m, 3, 6),
        new("SU", "Sumatera Utara", 25_000m, 15_000m, 3, 5),
        new("SB", "Sumatera Barat", 25_000m, 15_000m, 3, 5),
        new("RI", "Riau", 24_000m, 14_000m, 3, 5),
        new("KR", "Kepulauan Riau", 26_000m, 16_000m, 3, 6),
        new("JA", "Jambi", 23_000m, 14_000m, 3, 5),
        new("SS", "Sumatera Selatan", 22_000m, 13_000m, 2, 4),
        new("BB", "Kepulauan Bangka Belitung", 24_000m, 15_000m, 3, 5),
        new("BE", "Bengkulu", 24_000m, 14_000m, 3, 5),
        new("LA", "Lampung", 18_000m, 10_000m, 2, 4),
        new("KB", "Kalimantan Barat", 26_000m, 16_000m, 3, 6),
        new("KT", "Kalimantan Tengah", 28_000m, 17_000m, 3, 6),
        new("KS", "Kalimantan Selatan", 27_000m, 16_000m, 3, 6),
        new("KI", "Kalimantan Timur", 29_000m, 18_000m, 3, 6),
        new("KU", "Kalimantan Utara", 32_000m, 20_000m, 4, 7),
        new("SA", "Sulawesi Utara", 32_000m, 20_000m, 4, 7),
        new("ST", "Sulawesi Tengah", 33_000m, 21_000m, 4, 7),
        new("SN", "Sulawesi Selatan", 28_000m, 17_000m, 3, 6),
        new("SG", "Sulawesi Tenggara", 32_000m, 20_000m, 4, 7),
        new("GO", "Gorontalo", 34_000m, 22_000m, 4, 7),
        new("SR", "Sulawesi Barat", 33_000m, 21_000m, 4, 7),
        new("MA", "Maluku", 40_000m, 28_000m, 5, 8),
        new("MU", "Maluku Utara", 42_000m, 30_000m, 5, 8),
        new("PA", "Papua", 50_000m, 35_000m, 5, 9),
        new("PB", "Papua Barat", 50_000m, 35_000m, 5, 9),
        new("PS", "Papua Selatan", 55_000m, 38_000m, 6, 10),
        new("PT", "Papua Tengah", 55_000m, 38_000m, 6, 10),
        new("PE", "Papua Pegunungan", 60_000m, 42_000m, 7, 12),
        new("PD", "Papua Barat Daya", 52_000m, 36_000m, 5, 9)
    ];
}
