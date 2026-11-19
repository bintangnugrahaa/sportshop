namespace SportShop.Application.Features.Catalog.Variants.CreateVariant;

public sealed record CreateVariantCommand(
    Guid ProductId,
    string Sku,
    string? OptionSize,
    string? OptionColor,
    decimal Price,
    int WeightGram,
    int StockQuantity,
    bool IsActive = true);
