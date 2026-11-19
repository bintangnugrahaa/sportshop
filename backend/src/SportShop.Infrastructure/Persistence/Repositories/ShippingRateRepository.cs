using Microsoft.EntityFrameworkCore;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Shipping;

namespace SportShop.Infrastructure.Persistence.Repositories;

public class ShippingRateRepository : IShippingRateRepository
{
    private readonly AppDbContext _context;

    public ShippingRateRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ShippingRate>> GetAllAsync(bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        var query = _context.ShippingRates.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query.OrderBy(r => r.ProvinceName).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingRate>> GetActiveRatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ShippingRates
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.ProvinceName)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShippingRate?> GetByProvinceCodeAsync(string provinceCode, CancellationToken cancellationToken = default)
    {
        var code = provinceCode.Trim().ToUpperInvariant();
        return await _context.ShippingRates
            .FirstOrDefaultAsync(r => r.ProvinceCode == code, cancellationToken);
    }

    public Task UpdateAsync(ShippingRate rate, CancellationToken cancellationToken = default)
    {
        _context.ShippingRates.Update(rate);
        return Task.CompletedTask;
    }
}
