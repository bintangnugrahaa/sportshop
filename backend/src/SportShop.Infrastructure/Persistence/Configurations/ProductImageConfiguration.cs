using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportShop.Domain.Catalog;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.Property(i => i.Url)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.AltText)
            .HasMaxLength(200);

        builder.Property(i => i.SortOrder)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(i => i.IsPrimary)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(i => i.ProductId)
            .IsUnique()
            .HasFilter("is_primary = true")
            .HasDatabaseName("ix_product_images_product_id_primary");

        builder.HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
