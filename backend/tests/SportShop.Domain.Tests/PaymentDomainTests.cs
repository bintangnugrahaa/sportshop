using FluentAssertions;
using SportShop.Domain.Common;
using SportShop.Domain.Payments;
using Xunit;

namespace SportShop.Domain.Tests;

public class PaymentDomainTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldInitializeCorrectly()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var now = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

        var payment = new Payment(id, orderId, 150000m, "snap-token-123", "https://app.sandbox.midtrans.com/snap/v2/vtweb/snap-token-123", "Midtrans", now);

        payment.Id.Should().Be(id);
        payment.OrderId.Should().Be(orderId);
        payment.GrossAmount.Should().Be(150000m);
        payment.SnapToken.Should().Be("snap-token-123");
        payment.SnapRedirectUrl.Should().Be("https://app.sandbox.midtrans.com/snap/v2/vtweb/snap-token-123");
        payment.Provider.Should().Be("Midtrans");
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void Constructor_WithEmptyOrderId_ShouldThrowDomainException()
    {
        var act = () => new Payment(Guid.NewGuid(), Guid.Empty, 150000m);

        act.Should().Throw<DomainException>()
            .WithMessage("*Order ID tidak boleh kosong*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Constructor_WithZeroOrNegativeAmount_ShouldThrowDomainException(decimal amount)
    {
        var act = () => new Payment(Guid.NewGuid(), Guid.NewGuid(), amount);

        act.Should().Throw<DomainException>()
            .WithMessage("*Jumlah pembayaran harus lebih dari 0*");
    }

    [Fact]
    public void SetSnapDetails_WithValidToken_ShouldUpdateProperties()
    {
        var payment = new Payment(Guid.NewGuid(), Guid.NewGuid(), 200000m);
        var now = DateTime.UtcNow;

        payment.SetSnapDetails("new-token-abc", "https://redirect.url", now);

        payment.SnapToken.Should().Be("new-token-abc");
        payment.SnapRedirectUrl.Should().Be("https://redirect.url");
        payment.UpdatedAt.Should().Be(now);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetSnapDetails_WithInvalidToken_ShouldThrowDomainException(string? token)
    {
        var payment = new Payment(Guid.NewGuid(), Guid.NewGuid(), 200000m);

        var act = () => payment.SetSnapDetails(token!, "https://redirect.url", DateTime.UtcNow);

        act.Should().Throw<DomainException>()
            .WithMessage("*Snap token tidak boleh kosong*");
    }

    [Fact]
    public void ApplyNotification_WhenStatusPending_ShouldTransitionToSettlement()
    {
        var payment = new Payment(Guid.NewGuid(), Guid.NewGuid(), 100000m);
        var now = DateTime.UtcNow;

        var result = payment.ApplyNotification(
            newStatus: PaymentStatus.Settlement,
            transactionId: "trx-123",
            paymentType: "qris",
            fraudStatus: "accept",
            transactionTime: now.AddMinutes(-5),
            settledAt: now,
            rawPayload: "{\"status\":\"settlement\"}",
            now: now);

        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Settlement);
        payment.TransactionId.Should().Be("trx-123");
        payment.PaymentType.Should().Be("qris");
        payment.FraudStatus.Should().Be("accept");
        payment.SettledAt.Should().Be(now);
        payment.LastNotificationAt.Should().Be(now);
        payment.RawLastPayload.Should().Be("{\"status\":\"settlement\"}");
    }

    [Fact]
    public void ApplyNotification_WhenAlreadySettled_ShouldNotRevertToPending()
    {
        var payment = new Payment(Guid.NewGuid(), Guid.NewGuid(), 100000m);
        var now = DateTime.UtcNow;

        payment.ApplyNotification(
            PaymentStatus.Settlement,
            "trx-123",
            "bank_transfer",
            "accept",
            now,
            now,
            "{}",
            now);

        payment.IsFinalState.Should().BeTrue();

        // Late or out-of-order notification with 'Pending'
        var result = payment.ApplyNotification(
            PaymentStatus.Pending,
            "trx-123",
            "bank_transfer",
            "accept",
            now.AddMinutes(-1),
            null,
            "{}",
            now.AddMinutes(1));

        result.Should().BeFalse();
        payment.Status.Should().Be(PaymentStatus.Settlement);
    }

    [Fact]
    public void MarkCancelled_WhenPending_ShouldTransitionToCancel()
    {
        var payment = new Payment(Guid.NewGuid(), Guid.NewGuid(), 100000m);
        var now = DateTime.UtcNow;

        payment.MarkCancelled(now);

        payment.Status.Should().Be(PaymentStatus.Cancel);
        payment.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void MarkCancelled_WhenAlreadySettled_ShouldThrowDomainException()
    {
        var payment = new Payment(Guid.NewGuid(), Guid.NewGuid(), 100000m);
        var now = DateTime.UtcNow;
        payment.ApplyNotification(PaymentStatus.Settlement, "trx", "gopay", null, now, now, null, now);

        var act = () => payment.MarkCancelled(now);

        act.Should().Throw<DomainException>()
            .WithMessage("*sudah berhasil tidak dapat dibatalkan*");
    }

    [Fact]
    public void MarkExpired_WhenPending_ShouldTransitionToExpire()
    {
        var payment = new Payment(Guid.NewGuid(), Guid.NewGuid(), 100000m);
        var now = DateTime.UtcNow;

        payment.MarkExpired(now);

        payment.Status.Should().Be(PaymentStatus.Expire);
        payment.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void PaymentNotification_Constructor_ShouldValidateAndSetProperties()
    {
        var id = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dedupKey = PaymentNotification.BuildDedupKey("SPT-20261120-000001", "settlement", "200", "2026-11-20 10:00:00");

        var notification = new PaymentNotification(
            id,
            paymentId,
            "SPT-20261120-000001",
            "trx-999",
            "settlement",
            "200",
            true,
            "{\"status_code\":\"200\"}",
            now,
            dedupKey);

        notification.Id.Should().Be(id);
        notification.PaymentId.Should().Be(paymentId);
        notification.OrderNumber.Should().Be("SPT-20261120-000001");
        notification.TransactionId.Should().Be("trx-999");
        notification.TransactionStatus.Should().Be("settlement");
        notification.StatusCode.Should().Be("200");
        notification.SignatureValid.Should().BeTrue();
        notification.ReceivedAt.Should().Be(now);
        notification.DedupKey.Should().Be(dedupKey);
    }
}
