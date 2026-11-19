using SportShop.Domain.Catalog;

namespace SportShop.Application.Common.Interfaces;

public interface IBrandRepository
{
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Brand?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Brand>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task AddAsync(Brand brand, CancellationToken cancellationToken = default);
    Task UpdateAsync(Brand brand, CancellationToken cancellationToken = default);
}
