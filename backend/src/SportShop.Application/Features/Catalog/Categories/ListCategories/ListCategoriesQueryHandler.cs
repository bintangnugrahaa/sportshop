using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Catalog.Categories.ListCategories;

public sealed record ListCategoriesQuery(bool IncludeInactive = false, bool TreeOnly = false);

public sealed class ListCategoriesQueryHandler
{
    private readonly ICategoryRepository _categoryRepository;

    public ListCategoriesQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<List<CategoryDto>>> HandleAsync(ListCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        var categories = query.TreeOnly
            ? await _categoryRepository.GetRootCategoriesAsync(query.IncludeInactive, cancellationToken)
            : await _categoryRepository.GetAllAsync(query.IncludeInactive, cancellationToken);

        return Result.Success(CatalogMapper.ToCategoryDtoList(categories));
    }
}
