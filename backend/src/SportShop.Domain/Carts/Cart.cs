using SportShop.Domain.Catalog;
using SportShop.Domain.Common;

namespace SportShop.Domain.Carts;

public class Cart : IEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    public Cart()
    {
    }

    public Cart(Guid id, Guid userId, DateTime? createdAt = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("cart.invalid_user", "User ID tidak boleh kosong.");
        }

        Id = id;
        UserId = userId;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public CartItem AddItem(
        ProductVariant variant,
        int quantity,
        DateTime now)
    {
        if (quantity <= 0)
        {
            throw new DomainException("cart.invalid_quantity", "Jumlah item harus lebih dari 0.");
        }

        if (!variant.IsActive)
        {
            throw new DomainException("cart.variant_inactive", "Varian produk tidak aktif.");
        }

        var existingItem = Items.FirstOrDefault(i => i.VariantId == variant.Id);
        if (existingItem is not null)
        {
            var targetQuantity = existingItem.Quantity + quantity;
            if (targetQuantity > variant.StockQuantity)
            {
                throw new DomainException("stock.insufficient", $"Stok varian {variant.Sku} hanya tersisa {variant.StockQuantity}.");
            }

            existingItem.Quantity = targetQuantity;
            existingItem.UpdatedAt = now;
            UpdatedAt = now;
            return existingItem;
        }

        if (quantity > variant.StockQuantity)
        {
            throw new DomainException("stock.insufficient", $"Stok varian {variant.Sku} hanya tersisa {variant.StockQuantity}.");
        }

        var newItem = new CartItem(
            Guid.NewGuid(),
            Id,
            variant.Id,
            quantity,
            variant.Price,
            now);

        newItem.Variant = variant;
        Items.Add(newItem);
        UpdatedAt = now;
        return newItem;
    }

    public void UpdateItemQuantity(
        Guid variantId,
        int quantity,
        int currentStockQuantity,
        DateTime now)
    {
        if (quantity <= 0)
        {
            throw new DomainException("cart.invalid_quantity", "Jumlah item harus lebih dari 0.");
        }

        var existingItem = Items.FirstOrDefault(i => i.VariantId == variantId);
        if (existingItem is null)
        {
            throw new DomainException("cart.item_not_found", "Item tidak ditemukan di keranjang.");
        }

        if (quantity > currentStockQuantity)
        {
            throw new DomainException("stock.insufficient", $"Jumlah melebihi stok yang tersedia ({currentStockQuantity}).");
        }

        existingItem.Quantity = quantity;
        existingItem.UpdatedAt = now;
        UpdatedAt = now;
    }

    public bool RemoveItem(Guid variantId, DateTime now)
    {
        var existingItem = Items.FirstOrDefault(i => i.VariantId == variantId);
        if (existingItem is null)
        {
            return false;
        }

        Items.Remove(existingItem);
        UpdatedAt = now;
        return true;
    }

    public void Clear(DateTime now)
    {
        Items.Clear();
        UpdatedAt = now;
    }
}
