using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportShop.Domain.Catalog;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants", t =>
        {
            t.HasCheckConstraint("ck_product_variants_stock_quantity", "stock_quantity >= 0");
            t.HasCheckConstraint("ck_product_variants_price", "price >= 0");
            t.HasCheckConstraint("ck_product_variants_weight_gram", "weight_gram > 0");
        });

        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasKey(v => v.Id);

        builder.Property(v => v.ProductId)
            .IsRequired();

        builder.Property(v => v.Sku)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(v => v.Sku)
            .IsUnique();

        builder.Property(v => v.OptionSize)
            .HasMaxLength(30);

        builder.Property(v => v.OptionColor)
            .HasMaxLength(30);

        builder.Property(v => v.Price)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(v => v.WeightGram)
            .IsRequired();

        builder.Property(v => v.StockQuantity)
            .IsRequired();

        builder.Property(v => v.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(v => v.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(v => new { v.ProductId, v.OptionSize, v.OptionColor })
            .IsUnique()
            .AreNullsDistinct(false);

        builder.HasIndex(v => v.ProductId);

        builder.HasIndex(v => v.StockQuantity)
            .HasFilter("stock_quantity <= 5")
            .HasDatabaseName("ix_product_variants_low_stock");

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.InventoryMovements)
            .WithOne(m => m.Variant)
            .HasForeignKey(m => m.VariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
