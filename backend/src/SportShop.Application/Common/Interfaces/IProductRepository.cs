using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;

namespace SportShop.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, bool includeDetails = true, CancellationToken cancellationToken = default);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, Guid? excludeVariantId = null, CancellationToken cancellationToken = default);
    Task<ProductVariant?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default);
    Task<bool> VariantOptionsExistAsync(Guid productId, string? optionSize, string? optionColor, Guid? excludeVariantId = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? categoryId = null,
        Guid? brandId = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task AddVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default);
    Task UpdateVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default);
    Task AddInventoryMovementAsync(InventoryMovement movement, CancellationToken cancellationToken = default);
    Task<ProductImage?> GetImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductImage>> GetProductImagesAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddImageAsync(ProductImage image, CancellationToken cancellationToken = default);
    Task RemoveImageAsync(ProductImage image, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductVariant>> GetLowStockVariantsAsync(int threshold = 5, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryMovement>> GetVariantMovementsAsync(Guid variantId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPublicProductsAsync(
        string? q = null,
        string? categorySlug = null,
        string? brandSlug = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? inStock = null,
        string? sort = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
