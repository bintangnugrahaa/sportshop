using System.Text.Json;
using FluentAssertions;
using Moq;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Payments.Webhook;
using SportShop.Domain.Orders;
using SportShop.Domain.Payments;
using Xunit;

namespace SportShop.Application.Tests;

public sealed class PaymentApplicationTests
{
    private const string _serverKey = "SB-Mid-server-TEST1234567890";
    private static readonly string[] _roles = ["Customer"];

    [Fact]
    public void MidtransSignatureVerifier_ShouldVerifyCorrectly()
    {
        var orderId = "SPT-20261120-0001";
        var statusCode = "200";
        var grossAmount = "150000.00";

        var signature = MidtransSignatureVerifier.CalculateSignature(orderId, statusCode, grossAmount, _serverKey);
        signature.Should().NotBeNullOrWhiteSpace();

        var isValid = MidtransSignatureVerifier.Verify(orderId, statusCode, grossAmount, _serverKey, signature);
        isValid.Should().BeTrue();

        var isInvalid = MidtransSignatureVerifier.Verify(orderId, statusCode, grossAmount, _serverKey, "wrong-signature");
        isInvalid.Should().BeFalse();

        var isEmpty = MidtransSignatureVerifier.Verify(orderId, statusCode, grossAmount, _serverKey, "");
        isEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessMidtransWebhook_EmptyPayload_ShouldReturnValidationError()
    {
        var handler = CreateHandler(out _, out _, out _, out _, out _, out _);
        var result = await handler.HandleAsync(new ProcessMidtransWebhookCommand("   "));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("webhook.payload_empty");
    }

    [Fact]
    public async Task ProcessMidtransWebhook_InvalidJson_ShouldReturnValidationError()
    {
        var handler = CreateHandler(out _, out _, out _, out _, out _, out _);
        var result = await handler.HandleAsync(new ProcessMidtransWebhookCommand("not json"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("webhook.invalid_json");
    }

    [Fact]
    public async Task ProcessMidtransWebhook_InvalidSignature_ShouldReturnUnauthorized()
    {
        var handler = CreateHandler(out _, out _, out var gateway, out _, out _, out _);

        var payload = JsonSerializer.Serialize(new
        {
            order_id = "SPT-1",
            status_code = "200",
            gross_amount = "100000.00",
            transaction_status = "settlement",
            signature_key = "invalid-signature"
        });

        gateway.Setup(g => g.VerifySignature("SPT-1", "200", "100000.00", "invalid-signature"))
            .Returns(false);

        var result = await handler.HandleAsync(new ProcessMidtransWebhookCommand(payload));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("midtrans.invalid_signature");
    }

    [Fact]
    public async Task ProcessMidtransWebhook_DuplicateNotification_ShouldReturnSuccessIdempotently()
    {
        var handler = CreateHandler(out _, out var paymentRepo, out var gateway, out _, out _, out _);

        var orderId = "SPT-20261120-0001";
        var statusCode = "200";
        var grossAmount = "100000.00";
        var signature = MidtransSignatureVerifier.CalculateSignature(orderId, statusCode, grossAmount, ServerKey);

        var payload = JsonSerializer.Serialize(new
        {
            order_id = orderId,
            status_code = statusCode,
            gross_amount = grossAmount,
            transaction_status = "settlement",
            signature_key = signature,
            transaction_time = "2026-11-20 10:00:00"
        });

        gateway.Setup(g => g.VerifySignature(orderId, statusCode, grossAmount, signature))
            .Returns(true);
        paymentRepo.Setup(p => p.HasNotificationBeenProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await handler.HandleAsync(new ProcessMidtransWebhookCommand(payload));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("duplicate_ignored");
    }

    [Fact]
    public async Task ProcessMidtransWebhook_Settlement_ShouldMarkOrderPaid()
    {
        var handler = CreateHandler(out var orderRepo, out var paymentRepo, out var gateway, out var notifService, out var identityService, out var unitOfWork);

        var orderNumber = "SPT-20261120-0001";
        var statusCode = "200";
        var grossAmount = "100000.00";
        var signature = MidtransSignatureVerifier.CalculateSignature(orderNumber, statusCode, grossAmount, ServerKey);

        var payload = JsonSerializer.Serialize(new
        {
            order_id = orderNumber,
            status_code = statusCode,
            gross_amount = grossAmount,
            transaction_status = "settlement",
            signature_key = signature,
            transaction_id = "tx-123",
            payment_type = "qris"
        });

        var order = new Order(
            Guid.NewGuid(),
            orderNumber,
            Guid.NewGuid(),
            new OrderShippingAddress("Budi", "0812345678", "JK", "Jakarta", "Jaksel", "12345", "Jl"),
            1000,
            0m,
            DateTime.UtcNow.AddDays(1));

        order.AddItem(new OrderItem(Guid.NewGuid(), order.Id, Guid.NewGuid(), "Sepatu", "42", "SKU-01", "https://img.png", 100000m, 1));

        var payment = new Payment(Guid.NewGuid(), order.Id, 100000m);

        identityService.Setup(i => i.GetUserProfileAsync(order.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserProfileResult(order.UserId, "budi@test.com", "Budi", "0812", Roles, DateTime.UtcNow)));

        gateway.Setup(g => g.VerifySignature(orderNumber, statusCode, grossAmount, signature))
            .Returns(true);
        paymentRepo.Setup(p => p.HasNotificationBeenProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        gateway.Setup(g => g.GetPaymentStatusAsync(orderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult("200", "Success", "tx-123", orderNumber, 100000m, "qris", DateTime.UtcNow, "settlement", "accept", DateTime.UtcNow, signature, payload));
        orderRepo.Setup(o => o.GetByOrderNumberAsync(orderNumber, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        paymentRepo.Setup(p => p.GetByOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var result = await handler.HandleAsync(new ProcessMidtransWebhookCommand(payload));

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        payment.Status.Should().Be(PaymentStatus.Settlement);
        unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ProcessMidtransWebhookCommandHandler CreateHandler(
        out Mock<IOrderRepository> orderRepo,
        out Mock<IPaymentRepository> paymentRepo,
        out Mock<IPaymentGateway> gateway,
        out Mock<IOrderNotificationService> notifService,
        out Mock<IIdentityService> identityService,
        out Mock<IUnitOfWork> unitOfWork)
    {
        orderRepo = new Mock<IOrderRepository>();
        paymentRepo = new Mock<IPaymentRepository>();
        var productRepo = new Mock<IProductRepository>();
        gateway = new Mock<IPaymentGateway>();
        notifService = new Mock<IOrderNotificationService>();
        identityService = new Mock<IIdentityService>();
        unitOfWork = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var metrics = new Mock<ISportShopMetrics>();

        unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        return new ProcessMidtransWebhookCommandHandler(
            orderRepo.Object,
            paymentRepo.Object,
            productRepo.Object,
            gateway.Object,
            notifService.Object,
            identityService.Object,
            unitOfWork.Object,
            clock.Object,
            metrics.Object);
    }
}
