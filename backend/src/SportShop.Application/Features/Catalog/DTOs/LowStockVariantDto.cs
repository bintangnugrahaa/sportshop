namespace SportShop.Application.Features.Catalog.DTOs;

public sealed record LowStockVariantDto(
    Guid VariantId,
    Guid ProductId,
    string ProductName,
    string Sku,
    string? OptionSize,
    string? OptionColor,
    int StockQuantity,
    decimal Price);
