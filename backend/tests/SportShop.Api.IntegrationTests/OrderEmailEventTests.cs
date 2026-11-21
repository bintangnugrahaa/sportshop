using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
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
using SportShop.Domain.Shipping;
using SportShop.Infrastructure.Options;
using SportShop.Infrastructure.Persistence;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class OrderEmailEventTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrderEmailEventTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private (HttpClient Client, Mock<IOrderNotificationService> MockNotification) CreateTestClientWithMockNotification()
    {
        var mockNotification = new Mock<IOrderNotificationService>();
        var customizedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IOrderNotificationService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton(mockNotification.Object);
            });
        });

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.99.{Random.Shared.Next(1, 10000)}");

        return (client, mockNotification);
    }

    private async Task<(HttpClient Client, Guid UserId, string Email)> CreateAuthenticatedClientAsync(
        HttpClient client,
        string? emailOverride = null)
    {
        var email = emailOverride ?? $"email_event_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Email Event Tester", "08123456789"));
        regResp.EnsureSuccessStatusCode();

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        loginResp.EnsureSuccessStatusCode();
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        client.WithBearerToken(loginData!.AccessToken);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);

        return (client, user.Id, email);
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

        var variant = new ProductVariant(Guid.NewGuid(), product.Id, $"SKU-{suffix}", price, 500, stock, "42", "Hitam");
        db.ProductVariants.Add(variant);

        await db.SaveChangesAsync();
        return variant.Id;
    }

    private async Task EnsureShippingRateAsync(string provinceCode = "JK")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var exists = await db.ShippingRates.AnyAsync(r => r.ProvinceCode == provinceCode);
        if (!exists)
        {
            db.ShippingRates.Add(new ShippingRate(provinceCode, "DKI Jakarta", 15_000m, 5_000m, 1, 3, true));
            await db.SaveChangesAsync();
        }
    }

    private async Task<OrderDto> CreateTestOrderViaCheckoutAsync(
        HttpClient client,
        Guid variantId,
        string provinceCode = "JK")
    {
        await EnsureShippingRateAsync(provinceCode);

        // Add Address
        var addrResp = await client.PostAsJsonAsync("/api/v1/addresses", new
        {
            label = "Rumah",
            recipientName = "Budi Notif",
            phone = "08123456789",
            provinceCode = provinceCode,
            city = "Jakarta Selatan",
            district = "Kebayoran",
            postalCode = "12345",
            street = "Jl. Notif No. 12",
            isDefault = true
        });
        addrResp.EnsureSuccessStatusCode();
        var address = await addrResp.Content.ReadFromJsonAsync<AddressDto>();

        // Add to Cart
        var cartResp = await client.PostAsJsonAsync("/api/v1/cart/items", new
        {
            variantId = variantId,
            quantity = 1
        });
        cartResp.EnsureSuccessStatusCode();

        // Checkout
        var checkoutResp = await client.PostAsJsonAsync("/api/v1/orders/checkout", new
        {
            addressId = address!.Id,
            idempotencyKey = Guid.NewGuid().ToString()
        });
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
    public async Task CheckoutOrder_ShouldTrigger_OrderCreatedEmail()
    {
        var (client, mockNotification) = CreateTestClientWithMockNotification();
        var (authedClient, _, email) = await CreateAuthenticatedClientAsync(client);
        var variantId = await SeedProductAndVariantAsync(stock: 10, price: 100_000m);

        var order = await CreateTestOrderViaCheckoutAsync(authedClient, variantId);

        order.Should().NotBeNull();
        mockNotification.Verify(
            s => s.EnqueueOrderCreatedEmail(
                email,
                "Budi Notif",
                order.OrderNumber,
                order.Total,
                It.IsAny<DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task WebhookSettlement_ShouldTrigger_OrderPaidEmail_ExactlyOnce_AndIdempotentOnDuplicate()
    {
        var (client, mockNotification) = CreateTestClientWithMockNotification();
        var (authedClient, _, email) = await CreateAuthenticatedClientAsync(client);
        var variantId = await SeedProductAndVariantAsync(stock: 10, price: 200_000m);

        var order = await CreateTestOrderViaCheckoutAsync(authedClient, variantId);

        var grossAmount = order.Total.ToString("0.00", CultureInfo.InvariantCulture);
        var statusCode = "200";
        var transactionStatus = "settlement";
        var serverKey = GetServerKey();
        var signature = MidtransSignatureVerifier.CalculateSignature(order.OrderNumber, statusCode, grossAmount, serverKey);

        _factory.MockPaymentGateway
            .Setup(g => g.GetPaymentStatusAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult(statusCode, "Success", "trx-wh-123", order.OrderNumber, order.Total, "qris", DateTime.UtcNow, transactionStatus, "accept", DateTime.UtcNow, signature, "{}"));

        var payload = new
        {
            order_id = order.OrderNumber,
            status_code = statusCode,
            gross_amount = grossAmount,
            signature_key = signature,
            transaction_status = transactionStatus,
            fraud_status = "accept",
            transaction_id = $"midtrans-trx-{Guid.NewGuid():N}",
            payment_type = "qris",
            transaction_time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
        };

        // First webhook delivery
        var webhookResp1 = await client.PostAsJsonAsync("/api/v1/payments/midtrans/notification", payload);
        webhookResp1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verification: EnqueueOrderPaidEmail called exactly once
        mockNotification.Verify(
            s => s.EnqueueOrderPaidEmail(
                email,
                "Budi Notif",
                order.OrderNumber,
                order.Total,
                It.IsAny<DateTime>()),
            Times.Once);

        // Second duplicate webhook delivery
        var webhookResp2 = await client.PostAsJsonAsync("/api/v1/payments/midtrans/notification", payload);
        webhookResp2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verification: Still called exactly once, NOT duplicated
        mockNotification.Verify(
            s => s.EnqueueOrderPaidEmail(
                email,
                "Budi Notif",
                order.OrderNumber,
                order.Total,
                It.IsAny<DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task AdminShipOrder_ShouldTrigger_OrderShippedEmail_ExactlyOnce()
    {
        var (client, mockNotification) = CreateTestClientWithMockNotification();
        var (authedClient, _, email) = await CreateAuthenticatedClientAsync(client);
        var variantId = await SeedProductAndVariantAsync(stock: 10, price: 150_000m);

        var order = await CreateTestOrderViaCheckoutAsync(authedClient, variantId);

        // Transition order: Mark Paid in DB, then process
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var orderEntity = await db.Orders.FirstAsync(o => o.Id == order.Id);
            orderEntity.MarkPaid(DateTime.UtcNow, changedBy: null, note: "Paid in test");
            await db.SaveChangesAsync();
        }

        // Login as admin
        var adminLoginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand("admin@sportshop.com", "J3&fP6z@"));
        adminLoginResp.EnsureSuccessStatusCode();
        var adminLoginData = await adminLoginResp.Content.ReadFromJsonAsync<LoginResponse>();
        client.WithBearerToken(adminLoginData!.AccessToken);

        // Admin Process
        var processResp = await client.PostAsJsonAsync($"/api/v1/admin/orders/{order.OrderNumber}/process", new { note = "Memproses pesanan" });
        processResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Admin Ship
        var courier = "JNE Express";
        var trackingNumber = "JNE123456789";
        var shipResp = await client.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/ship",
            new AdminOrdersController.AdminShipOrderRequest(courier, trackingNumber, "Paket diserahkan ke kurir"));
        shipResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verification: EnqueueOrderShippedEmail called exactly once with correct parameters
        mockNotification.Verify(
            s => s.EnqueueOrderShippedEmail(
                email,
                "Budi Notif",
                order.OrderNumber,
                courier,
                trackingNumber,
                It.IsAny<DateTime>()),
            Times.Once);
    }
}
