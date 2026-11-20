namespace SportShop.Application.Common.Interfaces;

public interface ISendEmailJob
{
    Task SendEmailAsync(
        string recipient,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);

    Task SendOrderCreatedEmailAsync(
        string recipientEmail,
        string recipientName,
        string orderNumber,
        decimal total,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    Task SendOrderPaidEmailAsync(
        string recipientEmail,
        string recipientName,
        string orderNumber,
        decimal total,
        DateTime paidAt,
        CancellationToken cancellationToken = default);

    Task SendOrderShippedEmailAsync(
        string recipientEmail,
        string recipientName,
        string orderNumber,
        string courier,
        string trackingNumber,
        DateTime shippedAt,
        CancellationToken cancellationToken = default);
}
