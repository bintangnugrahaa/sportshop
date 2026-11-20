using SportShop.Domain.Common;
using SportShop.Domain.Orders;

namespace SportShop.Domain.Payments;

public class Payment : IEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Provider { get; set; } = "Midtrans";
    public PaymentStatus Status { get; set; }
    public decimal GrossAmount { get; set; }
    public string? SnapToken { get; set; }
    public string? SnapRedirectUrl { get; set; }
    public string? PaymentType { get; set; }
    public string? TransactionId { get; set; }
    public string? FraudStatus { get; set; }
    public DateTime? TransactionTime { get; set; }
    public DateTime? SettledAt { get; set; }
    public DateTime? LastNotificationAt { get; set; }
    public string? RawLastPayload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Order? Order { get; set; }

    public bool IsFinalState => Status is PaymentStatus.Settlement or PaymentStatus.Cancel or PaymentStatus.Expire;

    public Payment()
    {
    }

    public Payment(
        Guid id,
        Guid orderId,
        decimal grossAmount,
        string? snapToken = null,
        string? snapRedirectUrl = null,
        string provider = "Midtrans",
        DateTime? createdAt = null)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("payment.invalid_order", "Order ID tidak boleh kosong.");
        }

        if (grossAmount <= 0)
        {
            throw new DomainException("payment.invalid_amount", "Jumlah pembayaran harus lebih dari 0.");
        }

        Id = id;
        OrderId = orderId;
        GrossAmount = grossAmount;
        SnapToken = snapToken?.Trim();
        SnapRedirectUrl = snapRedirectUrl?.Trim();
        Provider = string.IsNullOrWhiteSpace(provider) ? "Midtrans" : provider.Trim();
        Status = PaymentStatus.Pending;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public void SetSnapDetails(string token, string redirectUrl, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new DomainException("payment.invalid_snap_token", "Snap token tidak boleh kosong.");
        }

        SnapToken = token.Trim();
        SnapRedirectUrl = redirectUrl?.Trim();
        UpdatedAt = now;
    }

    public bool ApplyNotification(
        PaymentStatus newStatus,
        string? transactionId,
        string? paymentType,
        string? fraudStatus,
        DateTime? transactionTime,
        DateTime? settledAt,
        string? rawPayload,
        DateTime now)
    {
        LastNotificationAt = now;
        RawLastPayload = rawPayload;
        UpdatedAt = now;

        if (!string.IsNullOrWhiteSpace(transactionId))
        {
            TransactionId = transactionId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(paymentType))
        {
            PaymentType = paymentType.Trim();
        }

        if (!string.IsNullOrWhiteSpace(fraudStatus))
        {
            FraudStatus = fraudStatus.Trim();
        }

        if (transactionTime.HasValue)
        {
            TransactionTime = transactionTime.Value;
        }

        // Rule: Status pembayaran final (Settlement, Cancel, Expire) tidak dapat diubah mundur oleh notifikasi lama.
        if (IsFinalState && newStatus != Status)
        {
            return false;
        }

        Status = newStatus;

        if (newStatus is PaymentStatus.Settlement or PaymentStatus.Capture)
        {
            SettledAt = settledAt ?? now;
        }

        return true;
    }

    public void MarkCancelled(DateTime now)
    {
        if (Status == PaymentStatus.Settlement)
        {
            throw new DomainException("payment.invalid_transition", "Pembayaran yang sudah berhasil tidak dapat dibatalkan.");
        }

        Status = PaymentStatus.Cancel;
        UpdatedAt = now;
    }

    public void MarkExpired(DateTime now)
    {
        if (Status == PaymentStatus.Settlement)
        {
            throw new DomainException("payment.invalid_transition", "Pembayaran yang sudah berhasil tidak dapat kedaluwarsa.");
        }

        Status = PaymentStatus.Expire;
        UpdatedAt = now;
    }
}
