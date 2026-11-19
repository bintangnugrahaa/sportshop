using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Catalog.Products.ListProducts;

public sealed record ListAdminProductsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    bool IncludeInactive = true);

public sealed class ListAdminProductsQueryHandler
{
    private readonly IProductRepository _productRepository;

    public ListAdminProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<PagedResult<ProductDto>>> HandleAsync(ListAdminProductsQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var (items, totalCount) = await _productRepository.GetPagedAsync(
            page,
            pageSize,
            query.CategoryId,
            query.BrandId,
            query.IncludeInactive,
            cancellationToken);

        var dtos = CatalogMapper.ToProductDtoList(items);
        return Result.Success(PagedResult.Create(dtos, page, pageSize, totalCount));
    }
}
