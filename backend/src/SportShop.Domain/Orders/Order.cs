using SportShop.Domain.Common;

namespace SportShop.Domain.Orders;

public class Order : IEntity
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public OrderStatus Status { get; set; }

    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total { get; set; }

    public OrderShippingAddress ShippingAddress { get; set; } = null!;
    public int TotalWeightGram { get; set; }

    public string? Courier { get; set; }
    public string? TrackingNumber { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public string? IdempotencyKey { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public uint Version { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();

    public Order()
    {
    }

    public Order(
        Guid id,
        string orderNumber,
        Guid userId,
        OrderShippingAddress shippingAddress,
        int totalWeightGram,
        decimal shippingFee,
        DateTime expiresAt,
        string? idempotencyKey = null,
        DateTime? createdAt = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("order.invalid_user", "User ID tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new DomainException("order.invalid_order_number", "Nomor pesanan tidak boleh kosong.");
        }

        if (shippingAddress is null)
        {
            throw new DomainException("order.invalid_shipping_address", "Alamat pengiriman wajib diisi.");
        }

        if (totalWeightGram <= 0)
        {
            throw new DomainException("order.invalid_weight", "Total berat pesanan harus lebih dari 0 gram.");
        }

        if (shippingFee < 0)
        {
            throw new DomainException("order.invalid_shipping_fee", "Biaya pengiriman tidak boleh negatif.");
        }

        Id = id;
        OrderNumber = orderNumber.Trim();
        UserId = userId;
        ShippingAddress = shippingAddress;
        TotalWeightGram = totalWeightGram;
        ShippingFee = shippingFee;
        ExpiresAt = expiresAt;
        IdempotencyKey = idempotencyKey?.Trim();
        Status = OrderStatus.PendingPayment;
        CreatedAt = createdAt ?? DateTime.UtcNow;

        StatusHistories.Add(new OrderStatusHistory(
            Guid.NewGuid(),
            Id,
            fromStatus: null,
            toStatus: OrderStatus.PendingPayment,
            note: "Pesanan dibuat",
            changedBy: userId,
            createdAt: CreatedAt));
    }

    public void AddItem(OrderItem item)
    {
        if (item is null)
        {
            throw new DomainException("order.invalid_item", "Item pesanan tidak boleh kosong.");
        }

        Items.Add(item);
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        Subtotal = Items.Sum(i => i.LineTotal);
        Total = Subtotal + ShippingFee;
    }

    public void MarkPaid(DateTime now, Guid? changedBy = null, string? note = null)
    {
        if (Status != OrderStatus.PendingPayment)
        {
            throw new DomainException("order.invalid_transition", $"Transisi status dari {Status} ke Paid tidak diizinkan.");
        }

        Status = OrderStatus.Paid;
        PaidAt = now;
        UpdatedAt = now;

        StatusHistories.Add(new OrderStatusHistory(
            Guid.NewGuid(),
            Id,
            fromStatus: OrderStatus.PendingPayment,
            toStatus: OrderStatus.Paid,
            note: note ?? "Pembayaran berhasil dikonfirmasi",
            changedBy: changedBy,
            createdAt: now));
    }

    public void Process(DateTime now, Guid? changedBy = null, string? note = null)
    {
        if (Status != OrderStatus.Paid)
        {
            throw new DomainException("order.invalid_transition", $"Transisi status dari {Status} ke Processing tidak diizinkan.");
        }

        Status = OrderStatus.Processing;
        UpdatedAt = now;

        StatusHistories.Add(new OrderStatusHistory(
            Guid.NewGuid(),
            Id,
            fromStatus: OrderStatus.Paid,
            toStatus: OrderStatus.Processing,
            note: note ?? "Pesanan sedang diproses",
            changedBy: changedBy,
            createdAt: now));
    }

    public void Ship(string courier, string trackingNumber, DateTime now, Guid? changedBy = null, string? note = null)
    {
        if (Status != OrderStatus.Processing)
        {
            throw new DomainException("order.invalid_transition", $"Transisi status dari {Status} ke Shipped tidak diizinkan.");
        }

        if (string.IsNullOrWhiteSpace(courier))
        {
            throw new DomainException("order.courier_required", "Nama kurir wajib diisi saat pesanan dikirim.");
        }

        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            throw new DomainException("order.tracking_number_required", "Nomor resi pengiriman wajib diisi saat pesanan dikirim.");
        }

        Courier = courier.Trim();
        TrackingNumber = trackingNumber.Trim();
        Status = OrderStatus.Shipped;
        ShippedAt = now;
        UpdatedAt = now;

        StatusHistories.Add(new OrderStatusHistory(
            Guid.NewGuid(),
            Id,
            fromStatus: OrderStatus.Processing,
            toStatus: OrderStatus.Shipped,
            note: note ?? $"Pesanan dikirim via {Courier} ({TrackingNumber})",
            changedBy: changedBy,
            createdAt: now));
    }

    public void Complete(DateTime now, Guid? changedBy = null, string? note = null)
    {
        if (Status != OrderStatus.Shipped)
        {
            throw new DomainException("order.invalid_transition", $"Transisi status dari {Status} ke Completed tidak diizinkan.");
        }

        Status = OrderStatus.Completed;
        CompletedAt = now;
        UpdatedAt = now;

        StatusHistories.Add(new OrderStatusHistory(
            Guid.NewGuid(),
            Id,
            fromStatus: OrderStatus.Shipped,
            toStatus: OrderStatus.Completed,
            note: note ?? "Pesanan selesai diterima pelanggan",
            changedBy: changedBy,
            createdAt: now));
    }

    public void Cancel(string? reason, DateTime now, Guid? changedBy = null)
    {
        if (Status != OrderStatus.PendingPayment && Status != OrderStatus.Paid && Status != OrderStatus.Processing)
        {
            throw new DomainException("order.invalid_transition", $"Transisi status dari {Status} ke Cancelled tidak diizinkan.");
        }

        var previousStatus = Status;
        Status = OrderStatus.Cancelled;
        CancelledAt = now;
        CancelReason = reason?.Trim();
        UpdatedAt = now;

        StatusHistories.Add(new OrderStatusHistory(
            Guid.NewGuid(),
            Id,
            fromStatus: previousStatus,
            toStatus: OrderStatus.Cancelled,
            note: reason ?? "Pesanan dibatalkan",
            changedBy: changedBy,
            createdAt: now));
    }

    public void Expire(DateTime now, Guid? changedBy = null, string? note = null)
    {
        if (Status != OrderStatus.PendingPayment)
        {
            throw new DomainException("order.invalid_transition", $"Transisi status dari {Status} ke Expired tidak diizinkan.");
        }

        Status = OrderStatus.Expired;
        UpdatedAt = now;

        StatusHistories.Add(new OrderStatusHistory(
            Guid.NewGuid(),
            Id,
            fromStatus: OrderStatus.PendingPayment,
            toStatus: OrderStatus.Expired,
            note: note ?? "Pesanan kedaluwarsa karena tidak dibayar",
            changedBy: changedBy,
            createdAt: now));
    }
}
