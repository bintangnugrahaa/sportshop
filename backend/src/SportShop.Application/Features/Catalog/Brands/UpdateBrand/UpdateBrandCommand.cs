namespace SportShop.Application.Features.Catalog.Brands.UpdateBrand;

public sealed record UpdateBrandCommand(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl = null,
    bool IsActive = true);
