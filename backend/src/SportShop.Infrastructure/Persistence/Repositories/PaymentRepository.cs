using Microsoft.EntityFrameworkCore;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Payments;

namespace SportShop.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _dbContext;

    public PaymentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Order)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
    }

    public async Task<Payment?> GetActivePaymentByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.OrderId == orderId &&
                (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Settlement || p.Status == PaymentStatus.Capture),
                cancellationToken);
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetPendingPaymentsOlderThanAsync(DateTime threshold, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Order)
                .ThenInclude(o => o!.Items)
            .Where(p => p.Status == PaymentStatus.Pending && p.CreatedAt <= threshold)
            .OrderBy(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Payments.AddAsync(payment, cancellationToken);
    }

    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _dbContext.Payments.Update(payment);
        return Task.CompletedTask;
    }

    public async Task<bool> HasNotificationBeenProcessedAsync(string dedupKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentNotifications
            .AnyAsync(n => n.DedupKey == dedupKey, cancellationToken);
    }

    public async Task AddNotificationAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentNotifications.AddAsync(notification, cancellationToken);
    }
}
