using SportShop.Domain.Common;
using SportShop.Domain.Inventory;

namespace SportShop.Domain.Catalog;

public class ProductVariant : IEntity
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Sku { get; set; } = string.Empty;
    public string? OptionSize { get; set; }
    public string? OptionColor { get; set; }
    public decimal Price { get; set; }
    public int WeightGram { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();

    public ProductVariant()
    {
    }

    public ProductVariant(
        Guid id,
        Guid productId,
        string sku,
        decimal price,
        int weightGram,
        int stockQuantity,
        string? optionSize = null,
        string? optionColor = null,
        bool isActive = true,
        DateTime? createdAt = null)
    {
        if (price < 0)
        {
            throw new DomainException("variant.invalid_price", "Harga varian tidak boleh negatif.");
        }

        if (weightGram <= 0)
        {
            throw new DomainException("variant.invalid_weight", "Berat varian harus lebih dari 0 gram.");
        }

        if (stockQuantity < 0)
        {
            throw new DomainException("stock.negative", "Stok varian tidak boleh negatif.");
        }

        Id = id;
        ProductId = productId;
        Sku = sku;
        Price = price;
        WeightGram = weightGram;
        StockQuantity = stockQuantity;
        OptionSize = optionSize;
        OptionColor = optionColor;
        IsActive = isActive;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public void Update(
        string sku,
        string? optionSize,
        string? optionColor,
        decimal price,
        int weightGram,
        bool isActive,
        DateTime updatedAt)
    {
        if (price < 0)
        {
            throw new DomainException("variant.invalid_price", "Harga varian tidak boleh negatif.");
        }

        if (weightGram <= 0)
        {
            throw new DomainException("variant.invalid_weight", "Berat varian harus lebih dari 0 gram.");
        }

        Sku = sku;
        OptionSize = optionSize;
        OptionColor = optionColor;
        Price = price;
        WeightGram = weightGram;
        IsActive = isActive;
        UpdatedAt = updatedAt;
    }

    public void AdjustStock(int quantityDelta, DateTime updatedAt)
    {
        if (StockQuantity + quantityDelta < 0)
        {
            throw new DomainException("stock.insufficient", "Stok tidak mencukupi.");
        }

        StockQuantity += quantityDelta;
        UpdatedAt = updatedAt;
    }

    public void SetActive(bool isActive, DateTime updatedAt)
    {
        IsActive = isActive;
        UpdatedAt = updatedAt;
    }
}
