using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SportShop.Api.Controllers;
using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Application.Features.Orders.PayOrder;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Catalog;
using SportShop.Domain.Shipping;
using SportShop.Infrastructure.Persistence;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class OrderPaymentEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrderPaymentEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.77.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedClientAsync()
    {
        var client = CreateTestClient();
        var email = $"pay_tester_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Pay Tester", "08123456789"));
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

    private async Task<Guid> SeedProductAndVariantAsync(int stock = 10, decimal price = 250_000m)
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

        var variant = new ProductVariant(Guid.NewGuid(), product.Id, $"SKU-{suffix}", price, 500, stock, "L", "Hitam");
        db.ProductVariants.Add(variant);

        await db.SaveChangesAsync();
        return variant.Id;
    }

    private async Task EnsureShippingRateAsync(string provinceCode = "31")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var exists = await db.ShippingRates.AnyAsync(r => r.ProvinceCode == provinceCode);
        if (!exists)
        {
            db.ShippingRates.Add(new ShippingRate(provinceCode, "DKI Jakarta", 10_000m, 5_000m, 1, 3, true));
            await db.SaveChangesAsync();
        }
    }

    private async Task<Guid> CreateAddressAsync(HttpClient client)
    {
        await EnsureShippingRateAsync("31");
        var createDto = new
        {
            label = "Rumah",
            recipientName = "Budi Santoso",
            phone = "08123456789",
            provinceCode = "31",
            city = "Jakarta Selatan",
            district = "Kebayoran Baru",
            postalCode = "12110",
            street = "Jl. Senopati No. 1",
            isDefault = true
        };

        var resp = await client.PostAsJsonAsync("/api/v1/addresses", createDto);
        resp.EnsureSuccessStatusCode();

        var created = await resp.Content.ReadFromJsonAsync<AddressDto>();
        return created!.Id;
    }

    [Fact]
    public async Task Checkout_ShouldCreatePaymentRecord_AndReturnSnapToken()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var variantId = await SeedProductAndVariantAsync(stock: 5, price: 100_000m);
        var addressId = await CreateAddressAsync(client);

        await client.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = 1 });

        var checkoutReq = new OrdersController.CheckoutRequest(addressId);
        var checkoutResp = await client.PostAsJsonAsync("/api/v1/orders/checkout", checkoutReq);

        checkoutResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();

        order.Should().NotBeNull();
        order!.SnapToken.Should().NotBeNullOrWhiteSpace();
        order.SnapToken.Should().Contain("snap-token-");
        order.PaymentStatus.Should().Be("pending");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
        payment.Should().NotBeNull();
        payment!.SnapToken.Should().Be(order.SnapToken);
    }

    [Fact]
    public async Task Checkout_WhenSnapGatewayFails_ShouldKeepOrderIntact_WithTokenUnavailable()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var variantId = await SeedProductAndVariantAsync(stock: 5, price: 100_000m);
        var addressId = await CreateAddressAsync(client);

        await client.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = 1 });

        // Temporarily configure gateway to fail
        _factory.MockPaymentGateway
            .Setup(g => g.CreateSnapTransactionAsync(It.IsAny<CreateSnapTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSnapTransactionResult(false, null, null, "midtrans.down", "Midtrans service unavailable"));

        try
        {
            var checkoutReq = new OrdersController.CheckoutRequest(addressId);
            var checkoutResp = await client.PostAsJsonAsync("/api/v1/orders/checkout", checkoutReq);

            checkoutResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();

            order.Should().NotBeNull();
            order!.Status.Should().Be("PendingPayment");
            order.PaymentStatus.Should().Be("token_unavailable");
            order.SnapToken.Should().BeNull();

            // Verify order still exists in DB and stock was reduced
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbOrder = await db.Orders.FirstOrDefaultAsync(o => o.Id == order.Id);
            dbOrder.Should().NotBeNull();
            dbOrder!.Status.Should().Be(Domain.Orders.OrderStatus.PendingPayment);

            var variant = await db.ProductVariants.FindAsync(variantId);
            variant!.StockQuantity.Should().Be(4);
        }
        finally
        {
            // Reset mock
            _factory.MockPaymentGateway
                .Setup(g => g.CreateSnapTransactionAsync(It.IsAny<CreateSnapTransactionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CreateSnapTransactionRequest req, CancellationToken _) =>
                    new CreateSnapTransactionResult(true, $"snap-token-{req.OrderNumber}", $"https://app.sandbox.midtrans.com/snap/v2/vtweb/snap-token-{req.OrderNumber}"));
        }
    }

    [Fact]
    public async Task PayOrder_WhenOrderPendingPayment_ShouldReturnSnapToken()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var variantId = await SeedProductAndVariantAsync(stock: 5, price: 100_000m);
        var addressId = await CreateAddressAsync(client);

        await client.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = 1 });

        var checkoutReq = new OrdersController.CheckoutRequest(addressId);
        var checkoutResp = await client.PostAsJsonAsync("/api/v1/orders/checkout", checkoutReq);
        var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();

        // Call POST /orders/{n}/pay
        var payResp = await client.PostAsync($"/api/v1/orders/{order!.OrderNumber}/pay", null);
        payResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var payResult = await payResp.Content.ReadFromJsonAsync<PayOrderResponseDto>();
        payResult.Should().NotBeNull();
        payResult!.OrderNumber.Should().Be(order.OrderNumber);
        payResult.SnapToken.Should().NotBeNullOrWhiteSpace();
        payResult.PaymentStatus.Should().Be("pending");
    }

    [Fact]
    public async Task PayOrder_WhenOrderBelongsToAnotherUser_ShouldReturn404()
    {
        var (client1, _) = await CreateAuthenticatedClientAsync();
        var (client2, _) = await CreateAuthenticatedClientAsync();

        var variantId = await SeedProductAndVariantAsync(stock: 5, price: 100_000m);
        var addressId = await CreateAddressAsync(client1);

        await client1.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = 1 });
        var checkoutResp = await client1.PostAsJsonAsync("/api/v1/orders/checkout", new OrdersController.CheckoutRequest(addressId));
        var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();

        // client2 tries to pay client1's order
        var payResp = await client2.PostAsync($"/api/v1/orders/{order!.OrderNumber}/pay", null);
        payResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
