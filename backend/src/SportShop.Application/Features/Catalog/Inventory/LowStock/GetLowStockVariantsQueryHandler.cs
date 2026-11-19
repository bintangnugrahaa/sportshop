using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;

namespace SportShop.Application.Features.Catalog.Inventory.LowStock;

public sealed record GetLowStockVariantsQuery(int Threshold = 5);

public sealed class GetLowStockVariantsQueryHandler
{
    private readonly IProductRepository _productRepository;

    public GetLowStockVariantsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<List<LowStockVariantDto>>> HandleAsync(GetLowStockVariantsQuery query, CancellationToken cancellationToken = default)
    {
        var threshold = query.Threshold < 0 ? 5 : query.Threshold;
        var variants = await _productRepository.GetLowStockVariantsAsync(threshold, cancellationToken);

        var dtos = variants.Select(v => new LowStockVariantDto(
            v.Id,
            v.ProductId,
            v.Product.Name,
            v.Sku,
            v.OptionSize,
            v.OptionColor,
            v.StockQuantity,
            v.Price)).ToList();

        return Result.Success(dtos);
    }
}
