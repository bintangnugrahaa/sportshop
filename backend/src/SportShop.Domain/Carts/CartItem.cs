using SportShop.Domain.Catalog;
using SportShop.Domain.Common;

namespace SportShop.Domain.Carts;

public class CartItem : IEntity
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal PriceAtAdd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public CartItem()
    {
    }

    public CartItem(
        Guid id,
        Guid cartId,
        Guid variantId,
        int quantity,
        decimal priceAtAdd,
        DateTime? createdAt = null)
    {
        if (quantity <= 0)
        {
            throw new DomainException("cart.invalid_quantity", "Jumlah item harus lebih dari 0.");
        }

        if (priceAtAdd < 0)
        {
            throw new DomainException("cart.invalid_price", "Harga tidak boleh negatif.");
        }

        Id = id;
        CartId = cartId;
        VariantId = variantId;
        Quantity = quantity;
        PriceAtAdd = priceAtAdd;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}
