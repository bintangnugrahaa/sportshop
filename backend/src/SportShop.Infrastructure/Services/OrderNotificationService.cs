using Hangfire;
using Microsoft.Extensions.Logging;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Infrastructure.Services;

public sealed partial class OrderNotificationService : IOrderNotificationService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<OrderNotificationService> _logger;

    public OrderNotificationService(
        IBackgroundJobClient backgroundJobClient,
        ILogger<OrderNotificationService> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public void EnqueueOrderCreatedEmail(string email, string recipientName, string orderNumber, decimal total, DateTime expiresAt)
    {
        try
        {
            _backgroundJobClient.Enqueue<ISendEmailJob>(job =>
                job.SendOrderCreatedEmailAsync(email, recipientName, orderNumber, total, expiresAt, CancellationToken.None));
            LogEnqueuedCreatedEmail(_logger, orderNumber, email);
        }
        catch (Exception ex)
        {
            LogEnqueueFailed(_logger, orderNumber, "OrderCreated", ex.Message, ex);
        }
    }

    public void EnqueueOrderPaidEmail(string email, string recipientName, string orderNumber, decimal total, DateTime paidAt)
    {
        try
        {
            _backgroundJobClient.Enqueue<ISendEmailJob>(job =>
                job.SendOrderPaidEmailAsync(email, recipientName, orderNumber, total, paidAt, CancellationToken.None));
            LogEnqueuedPaidEmail(_logger, orderNumber, email);
        }
        catch (Exception ex)
        {
            LogEnqueueFailed(_logger, orderNumber, "OrderPaid", ex.Message, ex);
        }
    }

    public void EnqueueOrderShippedEmail(string email, string recipientName, string orderNumber, string courier, string trackingNumber, DateTime shippedAt)
    {
        try
        {
            _backgroundJobClient.Enqueue<ISendEmailJob>(job =>
                job.SendOrderShippedEmailAsync(email, recipientName, orderNumber, courier, trackingNumber, shippedAt, CancellationToken.None));
            LogEnqueuedShippedEmail(_logger, orderNumber, email);
        }
        catch (Exception ex)
        {
            LogEnqueueFailed(_logger, orderNumber, "OrderShipped", ex.Message, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Enqueued OrderCreated email for order {OrderNumber} to {Recipient}")]
    private static partial void LogEnqueuedCreatedEmail(ILogger logger, string orderNumber, string recipient);

    [LoggerMessage(Level = LogLevel.Information, Message = "Enqueued OrderPaid email for order {OrderNumber} to {Recipient}")]
    private static partial void LogEnqueuedPaidEmail(ILogger logger, string orderNumber, string recipient);

    [LoggerMessage(Level = LogLevel.Information, Message = "Enqueued OrderShipped email for order {OrderNumber} to {Recipient}")]
    private static partial void LogEnqueuedShippedEmail(ILogger logger, string orderNumber, string recipient);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to enqueue {EventType} email for order {OrderNumber}: {ErrorMessage}")]
    private static partial void LogEnqueueFailed(ILogger logger, string orderNumber, string eventType, string errorMessage, Exception ex);
}
