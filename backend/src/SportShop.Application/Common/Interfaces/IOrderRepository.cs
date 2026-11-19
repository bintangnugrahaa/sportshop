using SportShop.Domain.Orders;

namespace SportShop.Application.Common.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, bool includeDetails = true, CancellationToken cancellationToken = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, bool includeDetails = true, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetCustomerOrdersPagedAsync(Guid userId, OrderStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetAdminOrdersPagedAsync(OrderStatus? status, DateTime? fromDate, DateTime? toDate, string? q, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetExpiredPendingOrdersAsync(DateTime now, int limit = 100, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}
