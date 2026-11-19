using SportShop.Domain.Shipping;

namespace SportShop.Application.Common.Interfaces;

public interface IShippingRateRepository
{
    Task<IReadOnlyList<ShippingRate>> GetAllAsync(bool includeInactive = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingRate>> GetActiveRatesAsync(CancellationToken cancellationToken = default);
    Task<ShippingRate?> GetByProvinceCodeAsync(string provinceCode, CancellationToken cancellationToken = default);
    Task UpdateAsync(ShippingRate rate, CancellationToken cancellationToken = default);
}
