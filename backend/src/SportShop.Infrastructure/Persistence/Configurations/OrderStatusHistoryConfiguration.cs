using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportShop.Domain.Orders;
using SportShop.Infrastructure.Identity;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_histories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.OrderId)
            .IsRequired();

        builder.Property(h => h.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(h => h.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(h => h.Note)
            .HasMaxLength(300);

        builder.Property(h => h.ChangedBy);

        builder.Property(h => h.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(h => new { h.OrderId, h.CreatedAt });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(h => h.ChangedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
