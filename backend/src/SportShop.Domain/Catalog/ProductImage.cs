using SportShop.Domain.Common;

namespace SportShop.Domain.Catalog;

public class ProductImage : IEntity
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }

    public ProductImage()
    {
    }

    public ProductImage(
        Guid id,
        Guid productId,
        string url,
        string? altText = null,
        int sortOrder = 0,
        bool isPrimary = false)
    {
        Id = id;
        ProductId = productId;
        Url = url;
        AltText = altText;
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
    }

    public void Update(
        string url,
        string? altText,
        int sortOrder,
        bool isPrimary)
    {
        Url = url;
        AltText = altText;
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
    }
}
