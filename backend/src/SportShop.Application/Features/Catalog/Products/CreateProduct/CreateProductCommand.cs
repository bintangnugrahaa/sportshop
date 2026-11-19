namespace SportShop.Application.Features.Catalog.Products.CreateProduct;

public sealed record CreateVariantItem(
    string Sku,
    string? OptionSize,
    string? OptionColor,
    decimal Price,
    int WeightGram,
    int StockQuantity,
    bool IsActive = true);

public sealed record CreateProductCommand(
    Guid CategoryId,
    Guid BrandId,
    string Name,
    string Slug,
    string Description,
    decimal BasePrice = 0,
    bool IsActive = true,
    List<CreateVariantItem>? Variants = null);
