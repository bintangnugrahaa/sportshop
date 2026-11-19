using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using SportShop.Domain.Catalog;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", t =>
        {
            t.HasCheckConstraint("ck_products_base_price", "base_price >= 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.CategoryId)
            .IsRequired();

        builder.Property(p => p.BrandId)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasMaxLength(220)
            .IsRequired();

        builder.HasIndex(p => p.Slug)
            .IsUnique();

        builder.Property(p => p.Description)
            .HasColumnType("text")
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(p => p.BasePrice)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasColumnName("search_vector")
            .HasComputedColumnSql("to_tsvector('simple', coalesce(name, '') || ' ' || coalesce(description, ''))", stored: true);

        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.BrandId);
        builder.HasIndex(p => new { p.IsActive, p.BasePrice });
        builder.HasIndex("SearchVector").HasMethod("GIN");
        builder.HasIndex(p => p.Name).HasMethod("GIN").HasOperators("gin_trgm_ops");

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
