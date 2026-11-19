using SportShop.Domain.Common;

namespace SportShop.Domain.Catalog;

public class Category : IEntity
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();

    public Category()
    {
    }

    public Category(
        Guid id,
        string name,
        string slug,
        Guid? parentId = null,
        int sortOrder = 0,
        bool isActive = true,
        DateTime? createdAt = null)
    {
        Id = id;
        Name = name;
        Slug = slug;
        ParentId = parentId;
        SortOrder = sortOrder;
        IsActive = isActive;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public void SetParent(Category? parent)
    {
        if (parent is not null && parent.ParentId is not null)
        {
            throw new DomainException("category.max_depth", "Kategori maksimal 2 tingkat.");
        }

        ParentId = parent?.Id;
        Parent = parent;
    }

    public void Update(
        string name,
        string slug,
        Guid? parentId,
        int sortOrder,
        bool isActive,
        DateTime updatedAt)
    {
        Name = name;
        Slug = slug;
        ParentId = parentId;
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = updatedAt;
    }
}
