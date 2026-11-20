using SportShop.Domain.Payments;

namespace SportShop.Application.Common.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Payment?> GetActivePaymentByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetPendingPaymentsOlderThanAsync(DateTime threshold, int limit = 100, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<bool> HasNotificationBeenProcessedAsync(string dedupKey, CancellationToken cancellationToken = default);
    Task AddNotificationAsync(PaymentNotification notification, CancellationToken cancellationToken = default);
}
