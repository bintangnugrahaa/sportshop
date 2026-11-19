using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Catalog.Categories.GetCategoryTree;

public sealed class GetCategoryTreeQueryHandler
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;

    public const string CacheKey = "cat:tree:v1";
    public static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public GetCategoryTreeQueryHandler(
        ICategoryRepository categoryRepository,
        ICacheService cacheService)
    {
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
    }

    public async Task<Result<List<CategoryDto>>> HandleAsync(
        GetCategoryTreeQuery query,
        CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<List<CategoryDto>>(CacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var roots = await _categoryRepository.GetRootCategoriesAsync(includeInactive: false, cancellationToken);
        var dtos = roots.Select(root =>
        {
            var rootDto = CatalogMapper.ToDto(root);
            var activeChildren = root.SubCategories?
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(CatalogMapper.ToDto)
                .ToList();

            return rootDto with { SubCategories = activeChildren };
        }).ToList();

        await _cacheService.SetAsync(CacheKey, dtos, CacheDuration, cancellationToken);

        return Result.Success(dtos);
    }
}
