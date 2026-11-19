using Microsoft.EntityFrameworkCore;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Carts;

namespace SportShop.Infrastructure.Persistence.Repositories;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _dbContext;

    public CartRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Cart?> GetByUserIdAsync(
        Guid userId,
        bool includeDetails = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Carts.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(c => c.Items.OrderBy(i => i.CreatedAt))
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images);
        }

        return await query.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<Cart> GetOrCreateByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cart = await GetByUserIdAsync(userId, includeDetails: true, cancellationToken);
        if (cart is not null)
        {
            return cart;
        }

        var newCart = new Cart(Guid.NewGuid(), userId, DateTime.UtcNow);
        try
        {
            await _dbContext.Carts.AddAsync(newCart, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return newCart;
        }
        catch (DbUpdateException)
        {
            // Another request might have created it concurrently
            _dbContext.Entry(newCart).State = EntityState.Detached;
            var existing = await GetByUserIdAsync(userId, includeDetails: true, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }

            throw;
        }
    }

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _dbContext.Carts.AddAsync(cart, cancellationToken);
    }

    public async Task AddItemAsync(CartItem item, CancellationToken cancellationToken = default)
    {
        await _dbContext.CartItems.AddAsync(item, cancellationToken);
    }

    public Task UpdateAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        _dbContext.Carts.Update(cart);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        _dbContext.Carts.Remove(cart);
        return Task.CompletedTask;
    }

    public Task RemoveItemAsync(CartItem item, CancellationToken cancellationToken = default)
    {
        _dbContext.CartItems.Remove(item);
        return Task.CompletedTask;
    }
}
