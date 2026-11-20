using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportShop.Domain.Orders;
using SportShop.Domain.Payments;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", t =>
        {
            t.HasCheckConstraint("ck_payments_status", "status IN ('Pending', 'Settlement', 'Capture', 'Deny', 'Cancel', 'Expire', 'Failure', 'Refund')");
            t.HasCheckConstraint("ck_payments_gross_amount_positive", "gross_amount > 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrderId)
            .IsRequired();

        builder.Property(p => p.Provider)
            .HasMaxLength(30)
            .HasDefaultValue("Midtrans")
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.GrossAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.SnapToken)
            .HasMaxLength(200);

        builder.Property(p => p.SnapRedirectUrl)
            .HasMaxLength(500);

        builder.Property(p => p.PaymentType)
            .HasMaxLength(50);

        builder.Property(p => p.TransactionId)
            .HasMaxLength(100);

        builder.Property(p => p.FraudStatus)
            .HasMaxLength(20);

        builder.Property(p => p.TransactionTime)
            .HasColumnType("timestamptz");

        builder.Property(p => p.SettledAt)
            .HasColumnType("timestamptz");

        builder.Property(p => p.LastNotificationAt)
            .HasColumnType("timestamptz");

        builder.Property(p => p.RawLastPayload)
            .HasColumnType("jsonb");

        builder.Property(p => p.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasIndex(p => p.OrderId)
            .HasDatabaseName("ix_payments_order_id");

        builder.HasIndex(p => p.TransactionId)
            .HasDatabaseName("ix_payments_transaction_id");

        builder.HasIndex(p => new { p.Status, p.CreatedAt })
            .HasDatabaseName("ix_payments_status_created_at");

        builder.HasIndex(p => p.OrderId)
            .IsUnique()
            .HasFilter("status IN ('Pending', 'Settlement', 'Capture')")
            .HasDatabaseName("ix_payments_order_active");

        builder.HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
