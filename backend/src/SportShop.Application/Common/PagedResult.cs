namespace SportShop.Application.Common;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public static class PagedResult
{
    public static PagedResult<T> Create<T>(IReadOnlyList<T> items, int page, int pageSize, int totalItems)
    {
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalItems / (double)pageSize) : 0;
        return new PagedResult<T>(items, page, pageSize, totalItems, totalPages);
    }
}
