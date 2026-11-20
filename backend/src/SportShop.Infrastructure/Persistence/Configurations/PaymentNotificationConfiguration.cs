using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportShop.Domain.Payments;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class PaymentNotificationConfiguration : IEntityTypeConfiguration<PaymentNotification>
{
    public void Configure(EntityTypeBuilder<PaymentNotification> builder)
    {
        builder.ToTable("payment_notifications");

        builder.HasKey(pn => pn.Id);

        builder.Property(pn => pn.PaymentId);

        builder.Property(pn => pn.OrderNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(pn => pn.TransactionId)
            .HasMaxLength(100);

        builder.Property(pn => pn.TransactionStatus)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(pn => pn.StatusCode)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(pn => pn.SignatureValid)
            .IsRequired();

        builder.Property(pn => pn.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(pn => pn.ReceivedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(pn => pn.DedupKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(pn => pn.DedupKey)
            .IsUnique()
            .HasDatabaseName("ix_payment_notifications_dedup_key");

        builder.HasIndex(pn => pn.OrderNumber)
            .HasDatabaseName("ix_payment_notifications_order_number");

        builder.HasOne(pn => pn.Payment)
            .WithMany()
            .HasForeignKey(pn => pn.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
