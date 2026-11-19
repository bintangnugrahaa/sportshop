namespace SportShop.Application.Features.Catalog.DTOs;

public sealed record ProductVariantDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string? OptionSize,
    string? OptionColor,
    decimal Price,
    int WeightGram,
    int StockQuantity,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
