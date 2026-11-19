namespace SportShop.Application.Features.Catalog.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    Guid CategoryId,
    Guid BrandId,
    string Name,
    string Slug,
    string Description,
    bool IsActive = true);
