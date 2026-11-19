namespace SportShop.Application.Features.Catalog.DTOs;

public sealed record BrandDto(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
