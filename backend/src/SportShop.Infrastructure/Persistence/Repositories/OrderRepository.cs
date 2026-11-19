using Microsoft.EntityFrameworkCore;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Orders;

namespace SportShop.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(o => o.Items)
                .Include(o => o.StatusHistories.OrderByDescending(h => h.CreatedAt));
        }

        return await query.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(o => o.Items)
                .Include(o => o.StatusHistories.OrderByDescending(h => h.CreatedAt));
        }

        return await query.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }

    public async Task<Order?> GetByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistories.OrderByDescending(h => h.CreatedAt))
            .FirstOrDefaultAsync(o => o.UserId == userId && o.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetCustomerOrdersPagedAsync(
        Guid userId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Where(o => o.UserId == userId)
            .AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Items)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetAdminOrdersPagedAsync(
        OrderStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        string? q,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(o =>
                o.OrderNumber.Contains(search) ||
                o.ShippingAddress.RecipientName.Contains(search) ||
                o.ShippingAddress.Phone.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Items)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Order>> GetExpiredPendingOrdersAsync(DateTime now, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Where(o => o.Status == OrderStatus.PendingPayment && o.ExpiresAt <= now)
            .OrderBy(o => o.ExpiresAt)
            .Take(limit)
            .Include(o => o.Items)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(order).State == EntityState.Detached)
        {
            _context.Orders.Update(order);
        }

        return Task.CompletedTask;
    }
}
