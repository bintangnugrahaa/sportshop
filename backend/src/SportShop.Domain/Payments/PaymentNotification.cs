using SportShop.Domain.Common;

namespace SportShop.Domain.Payments;

public class PaymentNotification : IEntity
{
    public Guid Id { get; set; }
    public Guid? PaymentId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string TransactionStatus { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public bool SignatureValid { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string DedupKey { get; set; } = string.Empty;

    public Payment? Payment { get; set; }

    public PaymentNotification()
    {
    }

    public PaymentNotification(
        Guid id,
        Guid? paymentId,
        string orderNumber,
        string? transactionId,
        string transactionStatus,
        string statusCode,
        bool signatureValid,
        string payload,
        DateTime receivedAt,
        string dedupKey)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new DomainException("payment_notification.invalid_order_number", "Nomor pesanan tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(dedupKey))
        {
            throw new DomainException("payment_notification.invalid_dedup_key", "Dedup key tidak boleh kosong.");
        }

        Id = id;
        PaymentId = paymentId;
        OrderNumber = orderNumber.Trim();
        TransactionId = transactionId?.Trim();
        TransactionStatus = transactionStatus?.Trim() ?? string.Empty;
        StatusCode = statusCode?.Trim() ?? string.Empty;
        SignatureValid = signatureValid;
        Payload = payload ?? string.Empty;
        ReceivedAt = receivedAt;
        DedupKey = dedupKey.Trim();
    }

    public static string BuildDedupKey(string orderNumber, string transactionStatus, string statusCode, string? transactionTime)
    {
        var time = string.IsNullOrWhiteSpace(transactionTime) ? "none" : transactionTime.Trim();
        return $"{orderNumber.Trim()}|{transactionStatus.Trim()}|{statusCode.Trim()}|{time}";
    }
}
