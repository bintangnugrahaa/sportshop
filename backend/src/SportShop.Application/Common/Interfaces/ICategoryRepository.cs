using SportShop.Domain.Catalog;

namespace SportShop.Application.Common.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetRootCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);
}
