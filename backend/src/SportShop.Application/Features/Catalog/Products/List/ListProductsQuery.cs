namespace SportShop.Application.Features.Catalog.Products.List;

public sealed record ListProductsQuery(
    string? Q = null,
    string? CategorySlug = null,
    string? BrandSlug = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? InStock = null,
    string? Sort = "newest",
    int Page = 1,
    int PageSize = 20);
