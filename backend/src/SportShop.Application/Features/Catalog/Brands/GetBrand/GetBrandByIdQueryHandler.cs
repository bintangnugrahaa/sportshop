using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Catalog.Brands.GetBrand;

public sealed record GetBrandByIdQuery(Guid Id);

public sealed class GetBrandByIdQueryHandler
{
    private readonly IBrandRepository _brandRepository;

    public GetBrandByIdQueryHandler(IBrandRepository brandRepository)
    {
        _brandRepository = brandRepository;
    }

    public async Task<Result<BrandDto>> HandleAsync(GetBrandByIdQuery query, CancellationToken cancellationToken = default)
    {
        var brand = await _brandRepository.GetByIdAsync(query.Id, cancellationToken);
        if (brand is null)
        {
            return Result.Failure<BrandDto>(Error.NotFound("brand.not_found", "Brand tidak ditemukan."));
        }

        return Result.Success(CatalogMapper.ToDto(brand));
    }
}
