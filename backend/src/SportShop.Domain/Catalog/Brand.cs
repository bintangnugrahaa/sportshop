using SportShop.Domain.Common;

namespace SportShop.Domain.Catalog;

public class Brand : IEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();

    public Brand()
    {
    }

    public Brand(
        Guid id,
        string name,
        string slug,
        string? logoUrl = null,
        bool isActive = true,
        DateTime? createdAt = null)
    {
        Id = id;
        Name = name;
        Slug = slug;
        LogoUrl = logoUrl;
        IsActive = isActive;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public void Update(
        string name,
        string slug,
        string? logoUrl,
        bool isActive,
        DateTime updatedAt)
    {
        Name = name;
        Slug = slug;
        LogoUrl = logoUrl;
        IsActive = isActive;
        UpdatedAt = updatedAt;
    }
}
