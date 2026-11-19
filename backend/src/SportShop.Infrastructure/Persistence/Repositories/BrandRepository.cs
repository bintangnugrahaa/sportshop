using Microsoft.EntityFrameworkCore;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Catalog;

namespace SportShop.Infrastructure.Persistence.Repositories;

public sealed class BrandRepository : IBrandRepository
{
    private readonly AppDbContext _context;

    public BrandRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Brand?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Brands.FirstOrDefaultAsync(b => b.Slug == slug, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Brands.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(b => b.Id != excludeId.Value);
        }

        return await query.AnyAsync(b => b.Slug == slug, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Brands.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(b => b.Id != excludeId.Value);
        }

        return await query.AnyAsync(b => b.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Brand>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Brands.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(b => b.IsActive);
        }

        return await query
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Brand brand, CancellationToken cancellationToken = default)
    {
        await _context.Brands.AddAsync(brand, cancellationToken);
    }

    public Task UpdateAsync(Brand brand, CancellationToken cancellationToken = default)
    {
        _context.Brands.Update(brand);
        return Task.CompletedTask;
    }
}
