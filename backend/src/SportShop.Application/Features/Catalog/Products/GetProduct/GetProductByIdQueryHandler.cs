using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Catalog.Products.GetProduct;

public sealed record GetProductByIdQuery(Guid Id);

public sealed class GetProductByIdQueryHandler
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductDto>> HandleAsync(GetProductByIdQuery query, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(query.Id, true, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound("product.not_found", "Produk tidak ditemukan."));
        }

        return Result.Success(CatalogMapper.ToDto(product));
    }
}
