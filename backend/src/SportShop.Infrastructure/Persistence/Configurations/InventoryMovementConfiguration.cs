using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportShop.Domain.Inventory;
using SportShop.Infrastructure.Identity;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements", t =>
        {
            t.HasCheckConstraint("ck_inventory_movements_reason", "reason IN ('Initial', 'Sale', 'Cancel', 'Expire', 'ManualAdjust')");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.VariantId)
            .IsRequired();

        builder.Property(m => m.ChangeQuantity)
            .IsRequired();

        builder.Property(m => m.Reason)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(m => m.OrderId);

        builder.Property(m => m.Note)
            .HasMaxLength(300);

        builder.Property(m => m.CreatedBy);

        builder.Property(m => m.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(m => new { m.VariantId, m.CreatedAt });
        builder.HasIndex(m => m.OrderId);

        builder.HasIndex(m => new { m.OrderId, m.VariantId, m.Reason })
            .IsUnique()
            .HasFilter("reason IN ('Cancel', 'Expire')")
            .HasDatabaseName("ix_inventory_movements_order_variant_cancel_expire");

        builder.HasOne(m => m.Variant)
            .WithMany(v => v.InventoryMovements)
            .HasForeignKey(m => m.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<SportShop.Domain.Orders.Order>()
            .WithMany()
            .HasForeignKey(m => m.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
