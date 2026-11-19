using Microsoft.EntityFrameworkCore;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;

namespace SportShop.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.Images);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants.OrderBy(v => v.Price))
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task<bool> SkuExistsAsync(string sku, Guid? excludeVariantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ProductVariants.AsQueryable();
        if (excludeVariantId.HasValue)
        {
            query = query.Where(v => v.Id != excludeVariantId.Value);
        }

        return await query.AnyAsync(v => v.Sku == sku, cancellationToken);
    }

    public async Task<ProductVariant?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);
    }

    public async Task<bool> VariantOptionsExistAsync(
        Guid productId,
        string? optionSize,
        string? optionColor,
        Guid? excludeVariantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProductVariants
            .Where(v => v.ProductId == productId && v.OptionSize == optionSize && v.OptionColor == optionColor);

        if (excludeVariantId.HasValue)
        {
            query = query.Where(v => v.Id != excludeVariantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? categoryId = null,
        Guid? brandId = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (brandId.HasValue)
        {
            query = query.Where(p => p.BrandId == brandId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(product);
        return Task.CompletedTask;
    }

    public async Task AddVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        await _context.ProductVariants.AddAsync(variant, cancellationToken);
    }

    public Task UpdateVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        _context.ProductVariants.Update(variant);
        return Task.CompletedTask;
    }

    public async Task AddInventoryMovementAsync(InventoryMovement movement, CancellationToken cancellationToken = default)
    {
        await _context.InventoryMovements.AddAsync(movement, cancellationToken);
    }
}
