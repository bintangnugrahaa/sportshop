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
}
