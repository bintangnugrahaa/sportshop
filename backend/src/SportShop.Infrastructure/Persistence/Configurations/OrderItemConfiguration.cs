using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportShop.Domain.Orders;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", t =>
        {
            t.HasCheckConstraint("ck_order_items_quantity_positive", "quantity > 0");
            t.HasCheckConstraint("ck_order_items_unit_price_non_negative", "unit_price >= 0");
            t.HasCheckConstraint("ck_order_items_line_total_non_negative", "line_total >= 0");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.OrderId)
            .IsRequired();

        builder.Property(i => i.VariantId)
            .IsRequired();

        builder.Property(i => i.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.VariantLabel)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.Sku)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(i => i.ImageUrl)
            .HasMaxLength(500);

        builder.Property(i => i.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Property(i => i.LineTotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(i => i.OrderId);
        builder.HasIndex(i => i.VariantId);

        builder.HasOne(i => i.Variant)
            .WithMany()
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
