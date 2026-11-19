using Microsoft.EntityFrameworkCore;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Catalog;

namespace SportShop.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.Parent)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.Parent)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(c => c.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories
            .Include(c => c.SubCategories)
            .AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetRootCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories
            .Include(c => c.SubCategories.OrderBy(s => s.SortOrder).ThenBy(s => s.Name))
            .Where(c => c.ParentId == null)
            .AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(category, cancellationToken);
    }

    public Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Update(category);
        return Task.CompletedTask;
    }
}
