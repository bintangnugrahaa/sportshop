using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Shipping.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Shipping.GetActiveRates;

public sealed class GetActiveShippingRatesQueryHandler
{
    public const string CacheKey = "ship:rates:v1";
    public static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly IShippingRateRepository _repository;
    private readonly ICacheService _cacheService;

    public GetActiveShippingRatesQueryHandler(
        IShippingRateRepository repository,
        ICacheService cacheService)
    {
        _repository = repository;
        _cacheService = cacheService;
    }

    public async Task<Result<List<ShippingRateDto>>> HandleAsync(
        GetActiveShippingRatesQuery query,
        CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<List<ShippingRateDto>>(CacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var rates = await _repository.GetActiveRatesAsync(cancellationToken);
        var dtos = rates.ToDtoList();

        await _cacheService.SetAsync(CacheKey, dtos, CacheDuration, cancellationToken);
        return Result.Success(dtos);
    }
}
