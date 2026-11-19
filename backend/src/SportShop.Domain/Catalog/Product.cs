using SportShop.Domain.Common;

namespace SportShop.Domain.Catalog;

public class Product : IEntity
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid BrandId { get; set; }
    public Brand Brand { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public Product()
    {
    }

    public Product(
        Guid id,
        Guid categoryId,
        Guid brandId,
        string name,
        string slug,
        string description,
        decimal basePrice,
        bool isActive = true,
        DateTime? createdAt = null)
    {
        if (basePrice < 0)
        {
            throw new DomainException("product.invalid_price", "Harga dasar tidak boleh negatif.");
        }

        Id = id;
        CategoryId = categoryId;
        BrandId = brandId;
        Name = name;
        Slug = slug;
        Description = description;
        BasePrice = basePrice;
        IsActive = isActive;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public void UpdateDetails(
        string name,
        string slug,
        string description,
        Guid categoryId,
        Guid brandId,
        DateTime updatedAt)
    {
        Name = name;
        Slug = slug;
        Description = description;
        CategoryId = categoryId;
        BrandId = brandId;
        UpdatedAt = updatedAt;
    }

    public void UpdateBasePrice(decimal basePrice, DateTime updatedAt)
    {
        if (basePrice < 0)
        {
            throw new DomainException("product.invalid_price", "Harga dasar tidak boleh negatif.");
        }

        BasePrice = basePrice;
        UpdatedAt = updatedAt;
    }

    public void RecalculateBasePrice(DateTime updatedAt)
    {
        var activeVariants = Variants.Where(v => v.IsActive).ToList();
        if (activeVariants.Count > 0)
        {
            BasePrice = activeVariants.Min(v => v.Price);
        }
        else if (Variants.Count > 0)
        {
            BasePrice = Variants.Min(v => v.Price);
        }

        UpdatedAt = updatedAt;
    }

    public void SetActive(bool isActive, DateTime updatedAt)
    {
        IsActive = isActive;
        UpdatedAt = updatedAt;
    }
}
