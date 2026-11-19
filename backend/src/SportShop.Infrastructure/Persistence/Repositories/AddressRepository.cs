using Microsoft.EntityFrameworkCore;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Users;

namespace SportShop.Infrastructure.Persistence.Repositories;

public class AddressRepository : IAddressRepository
{
    private readonly AppDbContext _context;

    public AddressRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Address>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Addresses
            .Include(a => a.Province)
            .Where(a => a.UserId == userId && !a.IsDeleted)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Address?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Addresses
            .Include(a => a.Province)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Address?> GetUserDefaultAddressAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Addresses
            .Include(a => a.Province)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault && !a.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(Address address, CancellationToken cancellationToken = default)
    {
        await _context.Addresses.AddAsync(address, cancellationToken);
    }

    public Task UpdateAsync(Address address, CancellationToken cancellationToken = default)
    {
        _context.Addresses.Update(address);
        return Task.CompletedTask;
    }

    public async Task UnsetDefaultAddressesAsync(Guid userId, Guid? excludeAddressId = null, CancellationToken cancellationToken = default)
    {
        var defaults = await _context.Addresses
            .Where(a => a.UserId == userId && a.IsDefault && (excludeAddressId == null || a.Id != excludeAddressId.Value))
            .ToListAsync(cancellationToken);

        foreach (var addr in defaults)
        {
            addr.IsDefault = false;
        }

        if (defaults.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
