using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Catalog.Brands.GetActiveBrands;

public sealed class GetActiveBrandsQueryHandler
{
    private readonly IBrandRepository _brandRepository;
    private readonly ICacheService _cacheService;

    public const string CacheKey = "brand:list:v1";
    public static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public GetActiveBrandsQueryHandler(
        IBrandRepository brandRepository,
        ICacheService cacheService)
    {
        _brandRepository = brandRepository;
        _cacheService = cacheService;
    }

    public async Task<Result<List<BrandDto>>> HandleAsync(
        GetActiveBrandsQuery query,
        CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<List<BrandDto>>(CacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var brands = await _brandRepository.GetAllAsync(includeInactive: false, cancellationToken);
        var dtos = CatalogMapper.ToBrandDtoList(brands);

        await _cacheService.SetAsync(CacheKey, dtos, CacheDuration, cancellationToken);

        return Result.Success(dtos);
    }
}
