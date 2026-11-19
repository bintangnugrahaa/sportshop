using SportShop.Domain.Common;

namespace SportShop.Domain.Orders;

public class OrderStatusHistory : IEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public Guid? ChangedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public OrderStatusHistory()
    {
    }

    public OrderStatusHistory(
        Guid id,
        Guid orderId,
        OrderStatus? fromStatus,
        OrderStatus toStatus,
        string? note = null,
        Guid? changedBy = null,
        DateTime? createdAt = null)
    {
        Id = id;
        OrderId = orderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Note = note;
        ChangedBy = changedBy;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}
