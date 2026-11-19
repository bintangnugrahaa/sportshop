using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Common;
using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;
using SportShop.Domain.Shipping;
using SportShop.Infrastructure.Persistence;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class CustomerOrderEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerOrderEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.72.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedClientAsync()
    {
        var client = CreateTestClient();
        var email = $"order_user_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Order Tester", "08123456789"));
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

    private async Task<Guid> SeedProductAndVariantAsync(int stockQuantity = 10, decimal price = 200_000m, int weightGram = 500)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var suffix = Guid.NewGuid().ToString()[..8];
        var category = new Category(Guid.NewGuid(), $"Cat-{suffix}", $"cat-{suffix}");
        var brand = new Brand(Guid.NewGuid(), $"Brand-{suffix}", $"brand-{suffix}");
        db.Categories.Add(category);
        db.Brands.Add(brand);

        var product = new Product(
            Guid.NewGuid(),
            category.Id,
            brand.Id,
            $"Produk-{suffix}",
            $"produk-{suffix}",
            "Deskripsi produk uji",
            price);
        db.Products.Add(product);

        var variant = new ProductVariant(
            Guid.NewGuid(),
            product.Id,
            $"SKU-{suffix}",
            price,
            weightGram,
            stockQuantity,
            "42",
            "Hitam");
        db.ProductVariants.Add(variant);

        await db.SaveChangesAsync();
        return variant.Id;
    }

    private async Task<Guid> CreateAddressAsync(HttpClient client, string provinceCode = "31")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var exists = await db.ShippingRates.AnyAsync(r => r.ProvinceCode == provinceCode);
        if (!exists)
        {
            db.ShippingRates.Add(new ShippingRate(provinceCode, "DKI Jakarta", 10_000m, 5_000m, 1, 3));
            await db.SaveChangesAsync();
        }

        var req = new AddressesController.CreateAddressRequest(
            "Rumah",
            "Budi Santoso",
            "08123456789",
            provinceCode,
            "Jakarta Selatan",
            "Kebayoran Baru",
            "12130",
            "Jl. Senopati No. 45",
            IsDefault: true);

        var resp = await client.PostAsJsonAsync("/api/v1/addresses", req);
        resp.EnsureSuccessStatusCode();

        var created = await resp.Content.ReadFromJsonAsync<AddressDto>();
        return created!.Id;
    }

    private async Task<(OrderDto Order, Guid VariantId)> CreateOrderAsync(HttpClient client, int stock = 10, int qty = 2)
    {
        var variantId = await SeedProductAndVariantAsync(stockQuantity: stock);
        var addressId = await CreateAddressAsync(client);

        var addResp = await client.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = qty });
        addResp.EnsureSuccessStatusCode();

        var checkoutReq = new OrdersController.CheckoutRequest(addressId);
        var response = await client.PostAsJsonAsync("/api/v1/orders/checkout", checkoutReq);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        return (order!, variantId);
    }

    [Fact]
    public async Task ListOrders_ShouldReturnPagedOrders_ForAuthenticatedCustomer()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var (order, _) = await CreateOrderAsync(client);

        var response = await client.GetAsync("/api/v1/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().Contain(o => o.OrderNumber == order.OrderNumber);
        paged.TotalItems.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetOrderByNumber_ShouldReturnOrderDetail_WhenBelongsToCustomer()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var (order, _) = await CreateOrderAsync(client);

        var response = await client.GetAsync($"/api/v1/orders/{order.OrderNumber}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<OrderDto>();
        detail.Should().NotBeNull();
        detail!.OrderNumber.Should().Be(order.OrderNumber);
        detail.Status.Should().Be("PendingPayment");
        detail.Items.Should().NotBeEmpty();
        detail.StatusHistories.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetOrderByNumber_ShouldReturn404_WhenBelongsToAnotherCustomer()
    {
        var (clientA, _) = await CreateAuthenticatedClientAsync();
        var (clientB, _) = await CreateAuthenticatedClientAsync();
        var (orderA, _) = await CreateOrderAsync(clientA);

        var response = await clientB.GetAsync($"/api/v1/orders/{orderA.OrderNumber}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem!.Code.Should().Be("order.not_found");
    }

    [Fact]
    public async Task CancelOrder_PendingPayment_ShouldSucceed_AndRestoreStock()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var (order, variantId) = await CreateOrderAsync(client, stock: 10, qty: 2);

        // Verify stock is 8 after checkout
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var variant = await db.ProductVariants.FindAsync(variantId);
            variant!.StockQuantity.Should().Be(8);
        }

        // Cancel order
        var cancelReq = new OrdersController.CancelOrderRequest("Berubah pikiran");
        var cancelResp = await client.PostAsJsonAsync($"/api/v1/orders/{order.OrderNumber}/cancel", cancelReq);
        if (cancelResp.StatusCode != HttpStatusCode.OK)
        {
            var str = await cancelResp.Content.ReadAsStringAsync();
            Assert.Fail("CANCEL ERROR: " + str);
        }

        var cancelledOrder = await cancelResp.Content.ReadFromJsonAsync<OrderDto>();
        cancelledOrder!.Status.Should().Be("Cancelled");
        cancelledOrder.CancelReason.Should().Be("Berubah pikiran");
        cancelledOrder.CancelledAt.Should().NotBeNull();

        // Verify DB: stock restored back to 10
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var variant = await db.ProductVariants.FindAsync(variantId);
            variant!.StockQuantity.Should().Be(10);

            var movement = await db.InventoryMovements
                .FirstOrDefaultAsync(m => m.OrderId == order.Id && m.Reason == StockReason.Cancel);
            movement.Should().NotBeNull();
            movement!.ChangeQuantity.Should().Be(2);
        }
    }

    [Fact]
    public async Task CancelOrder_SecondTime_ShouldReturn409Conflict_AndNotDuplicateStock()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var (order, variantId) = await CreateOrderAsync(client, stock: 10, qty: 2);

        // First cancel
        var cancelReq = new OrdersController.CancelOrderRequest("Pembatalan pertama");
        var firstResp = await client.PostAsJsonAsync($"/api/v1/orders/{order.OrderNumber}/cancel", cancelReq);
        firstResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify stock is 10
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var variant = await db.ProductVariants.FindAsync(variantId);
            variant!.StockQuantity.Should().Be(10);
        }

        // Second cancel attempt on already Cancelled order
        var secondResp = await client.PostAsJsonAsync($"/api/v1/orders/{order.OrderNumber}/cancel", cancelReq);
        secondResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await secondResp.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem!.Code.Should().Be("order.invalid_transition");

        // Verify stock is STILL 10, not doubled to 12!
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var variant = await db.ProductVariants.FindAsync(variantId);
            variant!.StockQuantity.Should().Be(10);

            var cancelMovements = await db.InventoryMovements
                .Where(m => m.OrderId == order.Id && m.Reason == StockReason.Cancel)
                .ToListAsync();
            cancelMovements.Should().HaveCount(1);
        }
    }
}
