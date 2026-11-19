using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportShop.Domain.Shipping;
using SportShop.Domain.Users;
using SportShop.Infrastructure.Identity;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.Label)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.RecipientName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.Phone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.ProvinceCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(a => a.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.District)
            .HasMaxLength(100);

        builder.Property(a => a.PostalCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(a => a.Street)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(a => a.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(a => a.UserId);

        builder.HasIndex(a => a.UserId)
            .IsUnique()
            .HasFilter("is_default")
            .HasDatabaseName("ix_addresses_user_id_is_default");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Province)
            .WithMany()
            .HasForeignKey(a => a.ProvinceCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
