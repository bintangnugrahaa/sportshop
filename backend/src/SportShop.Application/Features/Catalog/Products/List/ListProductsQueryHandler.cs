using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Catalog.Products.List;

public sealed class ListProductsQueryHandler
{
    private readonly IProductRepository _productRepository;
    private readonly IValidator<ListProductsQuery> _validator;

    public ListProductsQueryHandler(
        IProductRepository productRepository,
        IValidator<ListProductsQuery> validator)
    {
        _productRepository = productRepository;
        _validator = validator;
    }

    public async Task<Result<PagedResult<ProductDto>>> HandleAsync(
        ListProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<PagedResult<ProductDto>>(
                Error.Validation("products.validation_error", "Parameter pencarian tidak valid.", errors));
        }

        var (items, totalCount) = await _productRepository.GetPublicProductsAsync(
            query.Q,
            query.CategorySlug,
            query.BrandSlug,
            query.MinPrice,
            query.MaxPrice,
            query.InStock,
            query.Sort,
            query.Page,
            query.PageSize,
            cancellationToken);

        var dtos = items.ToProductDtoListWithSummary();
        return Result.Success(PagedResult.Create(dtos, query.Page, query.PageSize, totalCount));
    }
}
