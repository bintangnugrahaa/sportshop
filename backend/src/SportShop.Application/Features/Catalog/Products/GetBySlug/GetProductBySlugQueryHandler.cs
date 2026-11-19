using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Catalog.Products.GetBySlug;

public sealed class GetProductBySlugQueryHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;

    public GetProductBySlugQueryHandler(
        IProductRepository productRepository,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
    }

    public async Task<Result<ProductDto>> HandleAsync(GetProductBySlugQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Slug))
        {
            return Result.Failure<ProductDto>(Error.NotFound("product.not_found", "Slug produk tidak valid."));
        }

        var normalizedSlug = query.Slug.Trim().ToLowerInvariant();
        var cacheKey = $"prod:detail:{normalizedSlug}:v1";

        var cached = await _cacheService.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var product = await _productRepository.GetBySlugAsync(normalizedSlug, cancellationToken);
        if (product is null || !product.IsActive)
        {
            return Result.Failure<ProductDto>(Error.NotFound("product.not_found", "Produk tidak ditemukan."));
        }

        var dto = product.ToDtoWithSummary();
        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), cancellationToken);

        return Result.Success(dto);
    }
}
