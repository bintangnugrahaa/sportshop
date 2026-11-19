namespace SportShop.Application.Features.Catalog.Categories.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentId = null,
    int SortOrder = 0,
    bool IsActive = true);
