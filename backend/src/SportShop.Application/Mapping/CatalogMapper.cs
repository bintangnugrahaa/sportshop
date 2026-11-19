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

    [MapProperty(nameof(Product.Category.Name), nameof(ProductDto.CategoryName))]
    [MapProperty(nameof(Product.Brand.Name), nameof(ProductDto.BrandName))]
    public static partial ProductDto ToDto(Product product);

    public static partial List<ProductDto> ToProductDtoList(IEnumerable<Product> products);

    [MapperIgnoreSource(nameof(ProductVariant.Product))]
    [MapperIgnoreSource(nameof(ProductVariant.InventoryMovements))]
    public static partial ProductVariantDto ToDto(ProductVariant variant);

    public static partial List<ProductVariantDto> ToVariantDtoList(IEnumerable<ProductVariant> variants);

    [MapperIgnoreSource(nameof(ProductImage.Product))]
    public static partial ProductImageDto ToDto(ProductImage image);

    public static partial List<ProductImageDto> ToImageDtoList(IEnumerable<ProductImage> images);
}
