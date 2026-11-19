using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportShop.Domain.Orders;
using SportShop.Infrastructure.Identity;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", t =>
        {
            t.HasCheckConstraint("ck_orders_status", "status IN ('PendingPayment', 'Paid', 'Processing', 'Shipped', 'Completed', 'Cancelled', 'Expired')");
            t.HasCheckConstraint("ck_orders_total_sum", "total = subtotal + shipping_fee");
            t.HasCheckConstraint("ck_orders_subtotal_non_negative", "subtotal >= 0");
            t.HasCheckConstraint("ck_orders_shipping_fee_non_negative", "shipping_fee >= 0");
            t.HasCheckConstraint("ck_orders_weight_positive", "total_weight_gram > 0");
        });

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.UserId)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.ShippingFee)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.Total)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.OwnsOne(o => o.ShippingAddress, sa =>
        {
            sa.Property(a => a.RecipientName)
                .HasColumnName("ship_recipient_name")
                .HasMaxLength(150)
                .IsRequired();

            sa.Property(a => a.Phone)
                .HasColumnName("ship_phone")
                .HasMaxLength(20)
                .IsRequired();

            sa.Property(a => a.ProvinceCode)
                .HasColumnName("ship_province_code")
                .HasMaxLength(10)
                .IsRequired();

            sa.Property(a => a.ProvinceName)
                .HasColumnName("ship_province_name")
                .HasMaxLength(100)
                .IsRequired();

            sa.Property(a => a.City)
                .HasColumnName("ship_city")
                .HasMaxLength(100)
                .IsRequired();

            sa.Property(a => a.PostalCode)
                .HasColumnName("ship_postal_code")
                .HasMaxLength(10)
                .IsRequired();

            sa.Property(a => a.Street)
                .HasColumnName("ship_street")
                .HasMaxLength(300)
                .IsRequired();
        });

        builder.Navigation(o => o.ShippingAddress).IsRequired();

        builder.Property(o => o.TotalWeightGram)
            .IsRequired();

        builder.Property(o => o.Courier)
            .HasMaxLength(50);

        builder.Property(o => o.TrackingNumber)
            .HasMaxLength(80);

        builder.Property(o => o.ExpiresAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(o => o.PaidAt)
            .HasColumnType("timestamptz");

        builder.Property(o => o.ShippedAt)
            .HasColumnType("timestamptz");

        builder.Property(o => o.CompletedAt)
            .HasColumnType("timestamptz");

        builder.Property(o => o.CancelledAt)
            .HasColumnType("timestamptz");

        builder.Property(o => o.CancelReason)
            .HasMaxLength(300);

        builder.Property(o => o.IdempotencyKey)
            .HasMaxLength(100);

        builder.Property(o => o.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.Property(o => o.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.HasIndex(o => new { o.UserId, o.CreatedAt });
        builder.HasIndex(o => new { o.Status, o.CreatedAt });

        builder.HasIndex(o => o.ExpiresAt)
            .HasFilter("status = 'PendingPayment'")
            .HasDatabaseName("ix_orders_expires_at_pending");

        builder.HasIndex(o => new { o.UserId, o.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL")
            .HasDatabaseName("ix_orders_user_idempotency_key");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.StatusHistories)
            .WithOne(h => h.Order)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
