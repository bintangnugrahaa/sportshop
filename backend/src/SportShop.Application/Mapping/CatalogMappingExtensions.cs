using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Domain.Catalog;

namespace SportShop.Application.Mapping;

public static class CatalogMappingExtensions
{
    public static ProductDto ToDtoWithSummary(this Product product)
    {
        var dto = CatalogMapper.ToDto(product);
        var activeVariants = product.Variants?.Where(v => v.IsActive).ToList();
        var primaryImage = product.Images?.FirstOrDefault(i => i.IsPrimary)?.Url
            ?? product.Images?.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url;

        var inStock = activeVariants?.Any(v => v.StockQuantity > 0) ?? false;
        var totalStock = activeVariants?.Sum(v => v.StockQuantity) ?? 0;
        var minPrice = activeVariants != null && activeVariants.Count > 0
            ? activeVariants.Min(v => v.Price)
            : product.BasePrice;
        var maxPrice = activeVariants != null && activeVariants.Count > 0
            ? activeVariants.Max(v => v.Price)
            : product.BasePrice;

        return dto with
        {
            PrimaryImageUrl = primaryImage,
            InStock = inStock,
            TotalStock = totalStock,
            MinPrice = minPrice,
            MaxPrice = maxPrice
        };
    }

    public static List<ProductDto> ToProductDtoListWithSummary(this IEnumerable<Product> products) =>
        products.Select(ToDtoWithSummary).ToList();
}
