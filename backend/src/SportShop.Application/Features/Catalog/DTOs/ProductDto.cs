namespace SportShop.Application.Features.Catalog.DTOs;

public sealed record ProductDto(
    Guid Id,
    Guid CategoryId,
    string? CategoryName,
    Guid BrandId,
    string? BrandName,
    string Name,
    string Slug,
    string Description,
    decimal BasePrice,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<ProductVariantDto>? Variants = null,
    List<ProductImageDto>? Images = null,
    string? CategorySlug = null,
    string? BrandSlug = null,
    string? PrimaryImageUrl = null,
    bool InStock = true,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? TotalStock = null);
