using SportShop.Application.Features.Catalog.DTOs;

namespace SportShop.Application.Features.Catalog.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string Slug,
    Guid? ParentId = null,
    int SortOrder = 0,
    bool IsActive = true);
