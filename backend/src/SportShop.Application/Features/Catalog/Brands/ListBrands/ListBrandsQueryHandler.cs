using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Catalog.Brands.ListBrands;

public sealed record ListBrandsQuery(bool IncludeInactive = false);

public sealed class ListBrandsQueryHandler
{
    private readonly IBrandRepository _brandRepository;

    public ListBrandsQueryHandler(IBrandRepository brandRepository)
    {
        _brandRepository = brandRepository;
    }

    public async Task<Result<List<BrandDto>>> HandleAsync(ListBrandsQuery query, CancellationToken cancellationToken = default)
    {
        var brands = await _brandRepository.GetAllAsync(query.IncludeInactive, cancellationToken);
        return Result.Success(CatalogMapper.ToBrandDtoList(brands));
    }
}
