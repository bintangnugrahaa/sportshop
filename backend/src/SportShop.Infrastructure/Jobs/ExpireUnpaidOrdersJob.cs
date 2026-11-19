using Microsoft.Extensions.Logging;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Common;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;

namespace SportShop.Infrastructure.Jobs;

public sealed partial class ExpireUnpaidOrdersJob : IExpireUnpaidOrdersJob
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<ExpireUnpaidOrdersJob> _logger;

    public ExpireUnpaidOrdersJob(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<ExpireUnpaidOrdersJob> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var expiredOrders = await _orderRepository.GetExpiredPendingOrdersAsync(now, limit: 100, cancellationToken);
        if (expiredOrders.Count == 0)
        {
            return;
        }

        LogProcessingExpiredOrders(_logger, expiredOrders.Count);

        foreach (var order in expiredOrders)
        {
            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    order.Expire(now, changedBy: null, note: "Pesanan kedaluwarsa otomatis karena melewati batas waktu pembayaran");

                    foreach (var item in order.Items)
                    {
                        await _productRepository.RestoreStockAtomicAsync(item.VariantId, item.Quantity, now, ct);

                        var movement = new InventoryMovement(
                            Guid.NewGuid(),
                            item.VariantId,
                            item.Quantity,
                            StockReason.Expire,
                            $"Kedaluwarsa pesanan {order.OrderNumber}",
                            order.Id,
                            createdBy: null,
                            createdAt: now);

                        await _productRepository.AddInventoryMovementAsync(movement, ct);
                    }

                    await _orderRepository.UpdateAsync(order, ct);
                }, cancellationToken);

                LogOrderExpiredSuccess(_logger, order.OrderNumber);
            }
            catch (DomainException ex)
            {
                LogOrderExpiredSkipped(_logger, order.OrderNumber, ex.Message);
            }
            catch (Exception ex)
            {
                LogOrderExpiredError(_logger, order.OrderNumber, ex.Message);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing {Count} expired pending orders")]
    private static partial void LogProcessingExpiredOrders(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderNumber} expired successfully and stock restored")]
    private static partial void LogOrderExpiredSuccess(ILogger logger, string orderNumber);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping order {OrderNumber} expiration: {Reason}")]
    private static partial void LogOrderExpiredSkipped(ILogger logger, string orderNumber, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to expire order {OrderNumber}: {Error}")]
    private static partial void LogOrderExpiredError(ILogger logger, string orderNumber, string error);
}
