using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Catalog.Categories.GetCategory;

public sealed record GetCategoryByIdQuery(Guid Id);

public sealed class GetCategoryByIdQueryHandler
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<CategoryDto>> HandleAsync(GetCategoryByIdQuery query, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(query.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure<CategoryDto>(Error.NotFound("category.not_found", "Kategori tidak ditemukan."));
        }

        return Result.Success(CatalogMapper.ToDto(category));
    }
}
