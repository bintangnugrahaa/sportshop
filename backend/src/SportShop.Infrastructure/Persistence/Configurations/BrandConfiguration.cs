using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportShop.Domain.Catalog;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(b => b.Name)
            .IsUnique();

        builder.Property(b => b.Slug)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(b => b.Slug)
            .IsUnique();

        builder.Property(b => b.LogoUrl)
            .HasMaxLength(500);

        builder.Property(b => b.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .HasColumnType("timestamptz");
    }
}
