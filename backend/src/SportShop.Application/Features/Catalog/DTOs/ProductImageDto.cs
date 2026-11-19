namespace SportShop.Application.Features.Catalog.DTOs;

public sealed record ProductImageDto(
    Guid Id,
    Guid ProductId,
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary);
