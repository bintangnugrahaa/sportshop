using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using SportShop.Api.Controllers;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Application.Features.Payments.Webhook;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;
using SportShop.Domain.Payments;
using SportShop.Domain.Shipping;
using SportShop.Infrastructure.Options;
using SportShop.Infrastructure.Persistence;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class MidtransWebhookTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MidtransWebhookTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.88.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedClientAsync()
    {
        var client = CreateTestClient();
        var email = $"webhook_user_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Webhook Tester", "08123456789"));
        regResp.EnsureSuccessStatusCode();

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        loginResp.EnsureSuccessStatusCode();
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        client.WithBearerToken(loginData!.AccessToken);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);

        return (client, user.Id);
    }

    private async Task<Guid> SeedProductAndVariantAsync(int stock = 10, decimal price = 150_000m)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var suffix = Guid.NewGuid().ToString()[..8];
        var category = new Category(Guid.NewGuid(), $"Cat-{suffix}", $"cat-{suffix}");
        var brand = new Brand(Guid.NewGuid(), $"Brand-{suffix}", $"brand-{suffix}");
        db.Categories.Add(category);
        db.Brands.Add(brand);

        var product = new Product(Guid.NewGuid(), category.Id, brand.Id, $"Produk-{suffix}", $"produk-{suffix}", "Deskripsi", price);
        db.Products.Add(product);

        var variant = new ProductVariant(Guid.NewGuid(), product.Id, $"SKU-{suffix}", price, 500, stock, "M", "Biru");
        db.ProductVariants.Add(variant);

        await db.SaveChangesAsync();
        return variant.Id;
    }

    private async Task<Guid> CreateAddressAsync(HttpClient client)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!await db.ShippingRates.AnyAsync(r => r.ProvinceCode == "31"))
        {
            db.ShippingRates.Add(new ShippingRate("31", "DKI Jakarta", 10_000m, 5_000m, 1, 3, true));
            await db.SaveChangesAsync();
        }

        var createDto = new
        {
            label = "Rumah",
            recipientName = "Agus Kurniawan",
            phone = "08123456789",
            provinceCode = "31",
            city = "Jakarta Pusat",
            district = "Menteng",
            postalCode = "10310",
            street = "Jl. Teuku Umar No. 1",
            isDefault = true
        };

        var resp = await client.PostAsJsonAsync("/api/v1/addresses", createDto);
        resp.EnsureSuccessStatusCode();

        var created = await resp.Content.ReadFromJsonAsync<AddressDto>();
        return created!.Id;
    }

    private async Task<OrderDto> CreateTestOrderAsync(HttpClient client, Guid variantId)
    {
        var addressId = await CreateAddressAsync(client);
        await client.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = 2 });

        var checkoutResp = await client.PostAsJsonAsync("/api/v1/orders/checkout", new OrdersController.CheckoutRequest(addressId));
        checkoutResp.EnsureSuccessStatusCode();
        var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();
        return order!;
    }

    private string GetServerKey()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<MidtransOptions>>().Value;
        return options.ServerKey;
    }

    [Fact]
    public async Task Webhook_WithInvalidSignature_ShouldReturn401Unauthorized()
    {
        var client = CreateTestClient();
        var payload = new
        {
            order_id = "SPT-20261120-INVALID",
            status_code = "200",
            gross_amount = "150000.00",
            signature_key = "invalid-signature-hex",
            transaction_status = "settlement"
        };

        var response = await client.PostAsJsonAsync("/api/v1/payments/midtrans/notification", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhook_WithValidSettlement_ShouldMarkOrderPaid_AndPaymentSettlement()
    {
        var (authClient, _) = await CreateAuthenticatedClientAsync();
        var variantId = await SeedProductAndVariantAsync();
        var order = await CreateTestOrderAsync(authClient, variantId);

        var serverKey = GetServerKey();
        var grossAmountStr = order.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        var statusCode = "200";
        var transactionStatus = "settlement";
        var signatureKey = MidtransSignatureVerifier.CalculateSignature(order.OrderNumber, statusCode, grossAmountStr, serverKey);

        _factory.MockPaymentGateway
            .Setup(g => g.GetPaymentStatusAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult(statusCode, "Success", "trx-wh-123", order.OrderNumber, order.Total, "qris", DateTime.UtcNow, transactionStatus, "accept", DateTime.UtcNow, signatureKey, "{}"));

        var payload = new
        {
            order_id = order.OrderNumber,
            status_code = statusCode,
            gross_amount = grossAmountStr,
            signature_key = signatureKey,
            transaction_status = transactionStatus,
            transaction_id = "trx-wh-123",
            payment_type = "qris",
            transaction_time = "2026-11-20 15:00:00",
            settlement_time = "2026-11-20 15:05:00"
        };

        var client = CreateTestClient();
        var response = await client.PostAsJsonAsync("/api/v1/payments/midtrans/notification", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify order in database is Paid
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedOrder = await db.Orders.Include(o => o.StatusHistories).FirstAsync(o => o.Id == order.Id);
        updatedOrder.Status.Should().Be(OrderStatus.Paid);
        updatedOrder.PaidAt.Should().NotBeNull();
        updatedOrder.StatusHistories.Should().Contain(h => h.ToStatus == OrderStatus.Paid);

        var updatedPayment = await db.Payments.FirstAsync(p => p.OrderId == order.Id);
        updatedPayment.Status.Should().Be(PaymentStatus.Settlement);
        updatedPayment.SettledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Webhook_WhenDuplicateNotification_ShouldSafelyIgnoreAndReturn200()
    {
        var (authClient, _) = await CreateAuthenticatedClientAsync();
        var variantId = await SeedProductAndVariantAsync();
        var order = await CreateTestOrderAsync(authClient, variantId);

        var serverKey = GetServerKey();
        var grossAmountStr = order.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        var statusCode = "200";
        var transactionStatus = "settlement";
        var signatureKey = MidtransSignatureVerifier.CalculateSignature(order.OrderNumber, statusCode, grossAmountStr, serverKey);

        _factory.MockPaymentGateway
            .Setup(g => g.GetPaymentStatusAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult(statusCode, "Success", "trx-wh-dup", order.OrderNumber, order.Total, "qris", DateTime.UtcNow, transactionStatus, "accept", DateTime.UtcNow, signatureKey, "{}"));

        var payload = new
        {
            order_id = order.OrderNumber,
            status_code = statusCode,
            gross_amount = grossAmountStr,
            signature_key = signatureKey,
            transaction_status = transactionStatus,
            transaction_id = "trx-wh-dup",
            payment_type = "qris",
            transaction_time = "2026-11-20 15:00:00"
        };

        var client = CreateTestClient();
        // First delivery
        var resp1 = await client.PostAsJsonAsync("/api/v1/payments/midtrans/notification", payload);
        resp1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second delivery (duplicate)
        var resp2 = await client.PostAsJsonAsync("/api/v1/payments/midtrans/notification", payload);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await resp2.Content.ReadFromJsonAsync<WebhookProcessResult>();
        content!.Status.Should().Be("duplicate_ignored");

        // Verify only 1 payment notification in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifCount = await db.PaymentNotifications.CountAsync(n => n.OrderNumber == order.OrderNumber);
        notifCount.Should().Be(1);
    }

    [Fact]
    public async Task Webhook_WhenGrossAmountMismatched_ShouldMarkMismatchAndNotSetOrderPaid()
    {
        var (authClient, _) = await CreateAuthenticatedClientAsync();
        var variantId = await SeedProductAndVariantAsync();
        var order = await CreateTestOrderAsync(authClient, variantId);

        var serverKey = GetServerKey();
        // Tampered amount (e.g. 50,000 instead of order total)
        var fakeAmountStr = "50000.00";
        var statusCode = "200";
        var signatureKey = MidtransSignatureVerifier.CalculateSignature(order.OrderNumber, statusCode, fakeAmountStr, serverKey);

        _factory.MockPaymentGateway
            .Setup(g => g.GetPaymentStatusAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult(statusCode, "Success", "trx-mismatch", order.OrderNumber, 50000m, "qris", DateTime.UtcNow, "settlement", "accept", DateTime.UtcNow, signatureKey, "{}"));

        var payload = new
        {
            order_id = order.OrderNumber,
            status_code = statusCode,
            gross_amount = fakeAmountStr,
            signature_key = signatureKey,
            transaction_status = "settlement",
            transaction_id = "trx-mismatch",
            payment_type = "qris",
            transaction_time = "2026-11-20 15:00:00"
        };

        var client = CreateTestClient();
        var response = await client.PostAsJsonAsync("/api/v1/payments/midtrans/notification", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WebhookProcessResult>();
        result!.Status.Should().Be("amount_mismatch");

        // Verify order is NOT marked as Paid
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbOrder = await db.Orders.FirstAsync(o => o.Id == order.Id);
        dbOrder.Status.Should().Be(OrderStatus.PendingPayment);
    }

    [Fact]
    public async Task Webhook_WhenReversedOrder_PendingNotificationAfterSettlement_ShouldKeepPaidStatus()
    {
        var (authClient, _) = await CreateAuthenticatedClientAsync();
        var variantId = await SeedProductAndVariantAsync();
        var order = await CreateTestOrderAsync(authClient, variantId);

        var serverKey = GetServerKey();
        var grossAmountStr = order.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        // First receive settlement
        var sigSettlement = MidtransSignatureVerifier.CalculateSignature(order.OrderNumber, "200", grossAmountStr, serverKey);
        _factory.MockPaymentGateway
            .Setup(g => g.GetPaymentStatusAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult("200", "Success", "trx-rev", order.OrderNumber, order.Total, "bank_transfer", DateTime.UtcNow, "settlement", "accept", DateTime.UtcNow, sigSettlement, "{}"));

        var client = CreateTestClient();
        await client.PostAsJsonAsync("/api/v1/payments/midtrans/notification", new
        {
            order_id = order.OrderNumber,
            status_code = "200",
            gross_amount = grossAmountStr,
            signature_key = sigSettlement,
            transaction_status = "settlement",
            transaction_id = "trx-rev",
            transaction_time = "2026-11-20 15:05:00"
        });

        // Now an out-of-order 'pending' notification arrives
        var sigPending = MidtransSignatureVerifier.CalculateSignature(order.OrderNumber, "201", grossAmountStr, serverKey);
        _factory.MockPaymentGateway
            .Setup(g => g.GetPaymentStatusAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult("201", "Pending", "trx-rev", order.OrderNumber, order.Total, "bank_transfer", DateTime.UtcNow, "pending", null, null, sigPending, "{}"));

        var pendingResp = await client.PostAsJsonAsync("/api/v1/payments/midtrans/notification", new
        {
            order_id = order.OrderNumber,
            status_code = "201",
            gross_amount = grossAmountStr,
            signature_key = sigPending,
            transaction_status = "pending",
            transaction_id = "trx-rev",
            transaction_time = "2026-11-20 15:00:00"
        });
        pendingResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify order and payment DID NOT revert to Pending
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbOrder = await db.Orders.FirstAsync(o => o.Id == order.Id);
        dbOrder.Status.Should().Be(OrderStatus.Paid);

        var dbPayment = await db.Payments.FirstAsync(p => p.OrderId == order.Id);
        dbPayment.Status.Should().Be(PaymentStatus.Settlement);
    }

    [Fact]
    public async Task Webhook_WhenExpiredNotification_ShouldExpireOrderAndRestoreStock()
    {
        var (authClient, _) = await CreateAuthenticatedClientAsync();
        var variantId = await SeedProductAndVariantAsync(stock: 10);
        var order = await CreateTestOrderAsync(authClient, variantId);

        // Stock after checkout should be 8 (2 items bought)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var v = await db.ProductVariants.FindAsync(variantId);
            v!.StockQuantity.Should().Be(8);
        }

        var serverKey = GetServerKey();
        var grossAmountStr = order.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        var sigExpire = MidtransSignatureVerifier.CalculateSignature(order.OrderNumber, "202", grossAmountStr, serverKey);

        _factory.MockPaymentGateway
            .Setup(g => g.GetPaymentStatusAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult("202", "Expire", "trx-exp", order.OrderNumber, order.Total, "bank_transfer", DateTime.UtcNow, "expire", null, null, sigExpire, "{}"));

        var client = CreateTestClient();
        var resp = await client.PostAsJsonAsync("/api/v1/payments/midtrans/notification", new
        {
            order_id = order.OrderNumber,
            status_code = "202",
            gross_amount = grossAmountStr,
            signature_key = sigExpire,
            transaction_status = "expire",
            transaction_id = "trx-exp",
            transaction_time = "2026-11-20 15:30:00"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify order is Expired and stock restored back to 10
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbOrder = await db.Orders.FirstAsync(o => o.Id == order.Id);
            dbOrder.Status.Should().Be(OrderStatus.Expired);

            var v = await db.ProductVariants.FindAsync(variantId);
            v!.StockQuantity.Should().Be(10);

            var movement = await db.InventoryMovements.FirstOrDefaultAsync(m => m.OrderId == order.Id && m.Reason == StockReason.Expire);
            movement.Should().NotBeNull();
            movement!.ChangeQuantity.Should().Be(2);
        }
    }
}
