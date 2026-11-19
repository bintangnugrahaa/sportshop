using Riok.Mapperly.Abstractions;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Domain.Catalog;

namespace SportShop.Application.Mapping;

[Mapper]
public static partial class CatalogMapper
{
    [MapperIgnoreSource(nameof(Category.Parent))]
    [MapperIgnoreSource(nameof(Category.Products))]
    public static partial CategoryDto ToDto(Category category);

    public static partial List<CategoryDto> ToCategoryDtoList(IEnumerable<Category> categories);

    [MapperIgnoreSource(nameof(Brand.Products))]
    public static partial BrandDto ToDto(Brand brand);

    public static partial List<BrandDto> ToBrandDtoList(IEnumerable<Brand> brands);
}
