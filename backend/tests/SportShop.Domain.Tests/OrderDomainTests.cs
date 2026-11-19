using FluentAssertions;
using SportShop.Domain.Common;
using SportShop.Domain.Orders;
using Xunit;

namespace SportShop.Domain.Tests;

public sealed class OrderDomainTests
{
    private static OrderShippingAddress CreateAddress()
    {
        return new OrderShippingAddress(
            recipientName: "Budi Santoso",
            phone: "08123456789",
            provinceCode: "31",
            provinceName: "DKI Jakarta",
            city: "Jakarta Selatan",
            postalCode: "12340",
            street: "Jl. Sudirman No. 10");
    }

    private static Order CreateOrder(
        OrderStatus initialStatus = OrderStatus.PendingPayment,
        decimal shippingFee = 20_000m)
    {
        var order = new Order(
            Guid.NewGuid(),
            "SPT-20261119-000001",
            Guid.NewGuid(),
            CreateAddress(),
            totalWeightGram: 1000,
            shippingFee: shippingFee,
            expiresAt: DateTime.UtcNow.AddHours(24),
            idempotencyKey: "test-idem-key");

        order.Status = initialStatus;
        return order;
    }

    private static OrderItem CreateOrderItem(Guid orderId, decimal unitPrice = 100_000m, int quantity = 2)
    {
        return new OrderItem(
            Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            "Sepatu Lari RoadRunner",
            "Ukuran 42 / Hitam",
            "SKU-RUN-42-BLK",
            "https://example.com/img.jpg",
            unitPrice,
            quantity);
    }

    [Fact]
    public void Order_Creation_ShouldInitializeCorrectly()
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var address = CreateAddress();

        var order = new Order(
            Guid.NewGuid(),
            "SPT-20261119-000001",
            userId,
            address,
            totalWeightGram: 1200,
            shippingFee: 25_000m,
            expiresAt: now.AddHours(24),
            idempotencyKey: "idem-123",
            createdAt: now);

        order.Status.Should().Be(OrderStatus.PendingPayment);
        order.TotalWeightGram.Should().Be(1200);
        order.ShippingFee.Should().Be(25_000m);
        order.Subtotal.Should().Be(0m);
        order.Total.Should().Be(0m);
        order.StatusHistories.Should().HaveCount(1);
        order.StatusHistories.First().ToStatus.Should().Be(OrderStatus.PendingPayment);
        order.StatusHistories.First().FromStatus.Should().BeNull();
    }

    [Fact]
    public void Order_AddItem_ShouldRecalculateSubtotalAndTotal()
    {
        var order = CreateOrder(shippingFee: 15_000m);
        var item1 = CreateOrderItem(order.Id, unitPrice: 100_000m, quantity: 2);
        var item2 = CreateOrderItem(order.Id, unitPrice: 50_000m, quantity: 1);

        order.AddItem(item1);
        order.AddItem(item2);

        order.Items.Should().HaveCount(2);
        order.Subtotal.Should().Be(250_000m);
        order.Total.Should().Be(265_000m);
    }

    [Fact]
    public void Order_Creation_ShouldThrow_WhenArgumentsAreInvalid()
    {
        var validAddress = CreateAddress();
        var now = DateTime.UtcNow;

        var actEmptyUser = () => new Order(Guid.NewGuid(), "SPT-001", Guid.Empty, validAddress, 1000, 10_000m, now);
        actEmptyUser.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_user");

        var actEmptyNumber = () => new Order(Guid.NewGuid(), "", Guid.NewGuid(), validAddress, 1000, 10_000m, now);
        actEmptyNumber.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_order_number");

        var actNullAddress = () => new Order(Guid.NewGuid(), "SPT-001", Guid.NewGuid(), null!, 1000, 10_000m, now);
        actNullAddress.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_shipping_address");

        var actInvalidWeight = () => new Order(Guid.NewGuid(), "SPT-001", Guid.NewGuid(), validAddress, 0, 10_000m, now);
        actInvalidWeight.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_weight");

        var actNegativeShipping = () => new Order(Guid.NewGuid(), "SPT-001", Guid.NewGuid(), validAddress, 1000, -1m, now);
        actNegativeShipping.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_shipping_fee");
    }

    [Fact]
    public void OrderItem_Creation_ShouldThrow_WhenArgumentsAreInvalid()
    {
        var actEmptyVariant = () => new OrderItem(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "Sepatu", "42", "SKU-1", null, 100_000m, 1);
        actEmptyVariant.Should().Throw<DomainException>().Where(e => e.Code == "order_item.invalid_variant");

        var actEmptyName = () => new OrderItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "", "42", "SKU-1", null, 100_000m, 1);
        actEmptyName.Should().Throw<DomainException>().Where(e => e.Code == "order_item.invalid_name");

        var actEmptySku = () => new OrderItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sepatu", "42", "", null, 100_000m, 1);
        actEmptySku.Should().Throw<DomainException>().Where(e => e.Code == "order_item.invalid_sku");

        var actNegativePrice = () => new OrderItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sepatu", "42", "SKU-1", null, -1m, 1);
        actNegativePrice.Should().Throw<DomainException>().Where(e => e.Code == "order_item.invalid_price");

        var actZeroQty = () => new OrderItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sepatu", "42", "SKU-1", null, 100_000m, 0);
        actZeroQty.Should().Throw<DomainException>().Where(e => e.Code == "order_item.invalid_quantity");
    }

    // ==========================================
    // VALID STATE TRANSITIONS (PRD 6.1)
    // ==========================================

    [Fact]
    public void PendingPayment_To_Paid_ShouldSucceed()
    {
        var order = CreateOrder(OrderStatus.PendingPayment);
        var now = DateTime.UtcNow;

        order.MarkPaid(now, changedBy: null, note: "Settlement Midtrans");

        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAt.Should().Be(now);
        order.UpdatedAt.Should().Be(now);
        order.StatusHistories.Should().ContainSingle(h => h.FromStatus == OrderStatus.PendingPayment && h.ToStatus == OrderStatus.Paid);
    }

    [Fact]
    public void PendingPayment_To_Cancelled_ShouldSucceed()
    {
        var order = CreateOrder(OrderStatus.PendingPayment);
        var now = DateTime.UtcNow;

        order.Cancel("Ingin ganti alamat", now, changedBy: order.UserId);

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().Be(now);
        order.CancelReason.Should().Be("Ingin ganti alamat");
        order.StatusHistories.Should().ContainSingle(h => h.FromStatus == OrderStatus.PendingPayment && h.ToStatus == OrderStatus.Cancelled);
    }

    [Fact]
    public void PendingPayment_To_Expired_ShouldSucceed()
    {
        var order = CreateOrder(OrderStatus.PendingPayment);
        var now = DateTime.UtcNow;

        order.Expire(now, changedBy: null, note: "Batas 24 jam terlewati");

        order.Status.Should().Be(OrderStatus.Expired);
        order.UpdatedAt.Should().Be(now);
        order.StatusHistories.Should().ContainSingle(h => h.FromStatus == OrderStatus.PendingPayment && h.ToStatus == OrderStatus.Expired);
    }

    [Fact]
    public void Paid_To_Processing_ShouldSucceed()
    {
        var order = CreateOrder(OrderStatus.Paid);
        var now = DateTime.UtcNow;

        order.Process(now, changedBy: Guid.NewGuid(), note: "Admin memproses");

        order.Status.Should().Be(OrderStatus.Processing);
        order.UpdatedAt.Should().Be(now);
        order.StatusHistories.Should().ContainSingle(h => h.FromStatus == OrderStatus.Paid && h.ToStatus == OrderStatus.Processing);
    }

    [Fact]
    public void Paid_To_Cancelled_ShouldSucceed()
    {
        var order = CreateOrder(OrderStatus.Paid);
        var now = DateTime.UtcNow;

        order.Cancel("Permintaan pembatalan admin", now, changedBy: Guid.NewGuid());

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().Be(now);
        order.CancelReason.Should().Be("Permintaan pembatalan admin");
        order.StatusHistories.Should().ContainSingle(h => h.FromStatus == OrderStatus.Paid && h.ToStatus == OrderStatus.Cancelled);
    }

    [Fact]
    public void Processing_To_Shipped_ShouldSucceed_WhenCourierAndTrackingProvided()
    {
        var order = CreateOrder(OrderStatus.Processing);
        var now = DateTime.UtcNow;

        order.Ship("JNE", "JNE1234567890", now, changedBy: Guid.NewGuid());

        order.Status.Should().Be(OrderStatus.Shipped);
        order.Courier.Should().Be("JNE");
        order.TrackingNumber.Should().Be("JNE1234567890");
        order.ShippedAt.Should().Be(now);
        order.StatusHistories.Should().ContainSingle(h => h.FromStatus == OrderStatus.Processing && h.ToStatus == OrderStatus.Shipped);
    }

    [Theory]
    [InlineData("", "123456")]
    [InlineData("   ", "123456")]
    [InlineData(null, "123456")]
    public void Processing_To_Shipped_ShouldThrow_WhenCourierMissing(string? courier, string trackingNumber)
    {
        var order = CreateOrder(OrderStatus.Processing);
        var now = DateTime.UtcNow;

        var act = () => order.Ship(courier!, trackingNumber, now);

        act.Should().Throw<DomainException>().Where(e => e.Code == "order.courier_required");
    }

    [Theory]
    [InlineData("JNE", "")]
    [InlineData("JNE", "   ")]
    [InlineData("JNE", null)]
    public void Processing_To_Shipped_ShouldThrow_WhenTrackingNumberMissing(string courier, string? trackingNumber)
    {
        var order = CreateOrder(OrderStatus.Processing);
        var now = DateTime.UtcNow;

        var act = () => order.Ship(courier, trackingNumber!, now);

        act.Should().Throw<DomainException>().Where(e => e.Code == "order.tracking_number_required");
    }

    [Fact]
    public void Processing_To_Cancelled_ShouldSucceed()
    {
        var order = CreateOrder(OrderStatus.Processing);
        var now = DateTime.UtcNow;

        order.Cancel("Stok gudang rusak", now, changedBy: Guid.NewGuid());

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().Be(now);
        order.CancelReason.Should().Be("Stok gudang rusak");
        order.StatusHistories.Should().ContainSingle(h => h.FromStatus == OrderStatus.Processing && h.ToStatus == OrderStatus.Cancelled);
    }

    [Fact]
    public void Shipped_To_Completed_ShouldSucceed()
    {
        var order = CreateOrder(OrderStatus.Shipped);
        var now = DateTime.UtcNow;

        order.Complete(now, changedBy: Guid.NewGuid());

        order.Status.Should().Be(OrderStatus.Completed);
        order.CompletedAt.Should().Be(now);
        order.StatusHistories.Should().ContainSingle(h => h.FromStatus == OrderStatus.Shipped && h.ToStatus == OrderStatus.Completed);
    }

    // ==========================================
    // ILLEGAL TRANSITIONS MATRIX (PRD 6.1)
    // All other transitions must throw DomainException("order.invalid_transition")
    // ==========================================

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Expired)]
    public void MarkPaid_ShouldThrow_WhenStatusIsNotPendingPayment(OrderStatus fromStatus)
    {
        var order = CreateOrder(fromStatus);
        var act = () => order.MarkPaid(DateTime.UtcNow);

        act.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_transition");
    }

    [Theory]
    [InlineData(OrderStatus.PendingPayment)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Expired)]
    public void Process_ShouldThrow_WhenStatusIsNotPaid(OrderStatus fromStatus)
    {
        var order = CreateOrder(fromStatus);
        var act = () => order.Process(DateTime.UtcNow);

        act.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_transition");
    }

    [Theory]
    [InlineData(OrderStatus.PendingPayment)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Expired)]
    public void Ship_ShouldThrow_WhenStatusIsNotProcessing(OrderStatus fromStatus)
    {
        var order = CreateOrder(fromStatus);
        var act = () => order.Ship("JNE", "RESI-123", DateTime.UtcNow);

        act.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_transition");
    }

    [Theory]
    [InlineData(OrderStatus.PendingPayment)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Expired)]
    public void Complete_ShouldThrow_WhenStatusIsNotShipped(OrderStatus fromStatus)
    {
        var order = CreateOrder(fromStatus);
        var act = () => order.Complete(DateTime.UtcNow);

        act.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_transition");
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Expired)]
    public void Cancel_ShouldThrow_WhenStatusIsNotCancellable(OrderStatus fromStatus)
    {
        var order = CreateOrder(fromStatus);
        var act = () => order.Cancel("Alasan pembatalan", DateTime.UtcNow);

        act.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_transition");
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Expired)]
    public void Expire_ShouldThrow_WhenStatusIsNotPendingPayment(OrderStatus fromStatus)
    {
        var order = CreateOrder(fromStatus);
        var act = () => order.Expire(DateTime.UtcNow);

        act.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_transition");
    }
}
