using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportShop.Infrastructure.Identity;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(r => r.TokenHash)
            .IsUnique();

        builder.Property(r => r.FamilyId)
            .IsRequired();

        builder.Property(r => r.ExpiresAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(r => r.CreatedByIp)
            .HasMaxLength(45);

        builder.Property(r => r.RevokedAt)
            .HasColumnType("timestamptz");

        builder.Property(r => r.RevokeReason)
            .HasMaxLength(100);

        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.FamilyId);
        builder.HasIndex(r => r.ExpiresAt);
    }
}
