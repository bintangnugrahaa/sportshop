using SportShop.Domain.Catalog;
using SportShop.Domain.Common;

namespace SportShop.Domain.Orders;

public class OrderItem : IEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    public string ProductName { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }

    public OrderItem()
    {
    }

    public OrderItem(
        Guid id,
        Guid orderId,
        Guid variantId,
        string productName,
        string variantLabel,
        string sku,
        string? imageUrl,
        decimal unitPrice,
        int quantity)
    {
        if (variantId == Guid.Empty)
        {
            throw new DomainException("order_item.invalid_variant", "Variant ID tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new DomainException("order_item.invalid_name", "Nama produk tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("order_item.invalid_sku", "SKU tidak boleh kosong.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("order_item.invalid_price", "Harga satuan tidak boleh negatif.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("order_item.invalid_quantity", "Jumlah item harus lebih dari 0.");
        }

        Id = id;
        OrderId = orderId;
        VariantId = variantId;
        ProductName = productName.Trim();
        VariantLabel = variantLabel?.Trim() ?? string.Empty;
        Sku = sku.Trim();
        ImageUrl = imageUrl?.Trim();
        UnitPrice = unitPrice;
        Quantity = quantity;
        LineTotal = unitPrice * quantity;
    }
}
