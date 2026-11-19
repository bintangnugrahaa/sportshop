using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Common.Interfaces;
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

public class ExpireUnpaidOrdersJobTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ExpireUnpaidOrdersJobTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.73.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedClientAsync()
    {
        var client = CreateTestClient();
        var email = $"expire_user_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Expire Tester", "08123456789"));
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

    private async Task<Guid> SeedProductAndVariantAsync(int stockQuantity = 10, decimal price = 150_000m, int weightGram = 400)
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
    public async Task ExecuteAsync_ShouldExpireOverdueOrders_AndRestoreStock()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var (order, variantId) = await CreateOrderAsync(client, stock: 10, qty: 2);

        // Verify stock is 8 after checkout
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var variant = await db.ProductVariants.FindAsync(variantId);
            variant!.StockQuantity.Should().Be(8);

            // Make order overdue
            var orderEntity = await db.Orders.FindAsync(order.Id);
            orderEntity!.ExpiresAt = DateTime.UtcNow.AddMinutes(-10);
            await db.SaveChangesAsync();
        }

        // Run the background job
        using (var scope = _factory.Services.CreateScope())
        {
            var job = scope.ServiceProvider.GetRequiredService<IExpireUnpaidOrdersJob>();
            await job.ExecuteAsync();
        }

        // Verify in DB: order expired, stock restored, inventory movement recorded
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var orderEntity = await db.Orders
                .Include(o => o.StatusHistories)
                .FirstAsync(o => o.Id == order.Id);

            orderEntity.Status.Should().Be(OrderStatus.Expired);
            orderEntity.StatusHistories.Should().Contain(h => h.ToStatus == OrderStatus.Expired);

            var variant = await db.ProductVariants.FindAsync(variantId);
            variant!.StockQuantity.Should().Be(10);

            var movement = await db.InventoryMovements
                .FirstOrDefaultAsync(m => m.OrderId == order.Id && m.Reason == StockReason.Expire);
            movement.Should().NotBeNull();
            movement!.ChangeQuantity.Should().Be(2);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBeIdempotent_WhenExecutedMultipleTimes()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var (order, variantId) = await CreateOrderAsync(client, stock: 10, qty: 3);

        // Make order overdue
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var orderEntity = await db.Orders.FindAsync(order.Id);
            orderEntity!.ExpiresAt = DateTime.UtcNow.AddMinutes(-30);
            await db.SaveChangesAsync();
        }

        // Run the job first time
        using (var scope = _factory.Services.CreateScope())
        {
            var job = scope.ServiceProvider.GetRequiredService<IExpireUnpaidOrdersJob>();
            await job.ExecuteAsync();
        }

        // Run the job second time
        using (var scope = _factory.Services.CreateScope())
        {
            var job = scope.ServiceProvider.GetRequiredService<IExpireUnpaidOrdersJob>();
            await job.ExecuteAsync();
        }

        // Verify: stock is exactly 10, exactly 1 Expire movement
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var variant = await db.ProductVariants.FindAsync(variantId);
            variant!.StockQuantity.Should().Be(10);

            var movements = await db.InventoryMovements
                .Where(m => m.OrderId == order.Id && m.Reason == StockReason.Expire)
                .ToListAsync();
            movements.Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotExpire_OrdersNotYetOverdue()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var (order, variantId) = await CreateOrderAsync(client, stock: 10, qty: 2);

        // Order ExpiresAt is 24 hours in future (default)
        // Run job
        using (var scope = _factory.Services.CreateScope())
        {
            var job = scope.ServiceProvider.GetRequiredService<IExpireUnpaidOrdersJob>();
            await job.ExecuteAsync();
        }

        // Verify in DB: order remains PendingPayment, stock remains 8
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var orderEntity = await db.Orders.FindAsync(order.Id);
            orderEntity!.Status.Should().Be(OrderStatus.PendingPayment);

            var variant = await db.ProductVariants.FindAsync(variantId);
            variant!.StockQuantity.Should().Be(8);

            var movement = await db.InventoryMovements
                .FirstOrDefaultAsync(m => m.OrderId == order.Id && m.Reason == StockReason.Expire);
            movement.Should().BeNull();
        }
    }
}
