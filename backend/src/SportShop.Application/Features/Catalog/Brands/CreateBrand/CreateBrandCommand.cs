namespace SportShop.Application.Features.Catalog.Brands.CreateBrand;

public sealed record CreateBrandCommand(
    string Name,
    string Slug,
    string? LogoUrl = null,
    bool IsActive = true);
