using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportShop.Domain.Carts;

namespace SportShop.Infrastructure.Persistence.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items", t =>
        {
            t.HasCheckConstraint("ck_cart_items_quantity", "quantity > 0");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.CartId)
            .IsRequired();

        builder.Property(i => i.VariantId)
            .IsRequired();

        builder.HasIndex(i => new { i.CartId, i.VariantId })
            .IsUnique();

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Property(i => i.PriceAtAdd)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasOne(i => i.Variant)
            .WithMany()
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
