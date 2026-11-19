namespace SportShop.Application.Features.Catalog.DTOs;

public sealed record CategoryDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<CategoryDto>? SubCategories = null);
