using SportShop.Domain.Carts;

namespace SportShop.Application.Common.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(Guid userId, bool includeDetails = true, CancellationToken cancellationToken = default);
    Task<Cart> GetOrCreateByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Cart cart, CancellationToken cancellationToken = default);
    Task AddItemAsync(CartItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(Cart cart, CancellationToken cancellationToken = default);
    Task RemoveAsync(Cart cart, CancellationToken cancellationToken = default);
    Task RemoveItemAsync(CartItem item, CancellationToken cancellationToken = default);
}
