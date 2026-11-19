namespace SportShop.Application.Features.Cart.DTOs;

public sealed record CartDto(
    Guid Id,
    Guid UserId,
    IReadOnlyList<CartItemDto> Items,
    int TotalItems,
    int TotalWeightGram,
    decimal Subtotal,
    bool HasIssues);

public sealed record CartItemDto(
    Guid Id,
    Guid VariantId,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string Sku,
    string? OptionSize,
    string? OptionColor,
    string? PrimaryImageUrl,
    decimal UnitPrice,
    decimal PriceAtAdd,
    int Quantity,
    decimal LineTotal,
    int WeightGram,
    int TotalWeightGram,
    int StockQuantity,
    bool IsActive,
    bool IsPriceChanged,
    bool IsStockInsufficient,
    bool IsAvailable,
    string? StatusNote);
