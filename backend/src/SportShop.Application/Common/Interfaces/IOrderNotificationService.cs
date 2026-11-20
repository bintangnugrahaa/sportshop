namespace SportShop.Application.Common.Interfaces;

public interface IOrderNotificationService
{
    void EnqueueOrderCreatedEmail(string email, string recipientName, string orderNumber, decimal total, DateTime expiresAt);
    void EnqueueOrderPaidEmail(string email, string recipientName, string orderNumber, decimal total, DateTime paidAt);
    void EnqueueOrderShippedEmail(string email, string recipientName, string orderNumber, string courier, string trackingNumber, DateTime shippedAt);
}
