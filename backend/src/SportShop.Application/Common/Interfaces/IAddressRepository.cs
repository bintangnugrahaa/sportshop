using SportShop.Domain.Users;

namespace SportShop.Application.Common.Interfaces;

public interface IAddressRepository
{
    Task<IReadOnlyList<Address>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Address?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Address?> GetUserDefaultAddressAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Address address, CancellationToken cancellationToken = default);
    Task UpdateAsync(Address address, CancellationToken cancellationToken = default);
    Task UnsetDefaultAddressesAsync(Guid userId, Guid? excludeAddressId = null, CancellationToken cancellationToken = default);
}
