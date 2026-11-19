namespace SportShop.Application.Features.Catalog.Variants.UpdateVariant;

public sealed record UpdateVariantCommand(
    Guid Id,
    string Sku,
    string? OptionSize,
    string? OptionColor,
    decimal Price,
    int WeightGram,
    bool IsActive = true);
