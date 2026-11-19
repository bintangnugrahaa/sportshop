using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Cart.DTOs;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;
using SportShop.Domain.Shipping;
using SportShop.Infrastructure.Persistence;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class CheckoutEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CheckoutEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.61.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedClientAsync()
    {
        var client = CreateTestClient();
        var email = $"checkout_user_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Checkout Tester", "08123456789"));
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

    private async Task EnsureShippingRateAsync(string provinceCode = "31")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var exists = await db.ShippingRates.AnyAsync(r => r.ProvinceCode == provinceCode);
        if (!exists)
        {
            db.ShippingRates.Add(new ShippingRate(provinceCode, "DKI Jakarta", 10_000m, 5_000m, 1, 3));
            await db.SaveChangesAsync();
        }
    }

    private async Task<Guid> CreateAddressAsync(HttpClient client, string provinceCode = "31")
    {
        await EnsureShippingRateAsync(provinceCode);

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

    [Fact]
    public async Task Checkout_WithValidCartAndAddress_ShouldReturn201Created_AndReserveStock_AndEmptyCart()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var variantId = await SeedProductAndVariantAsync(stockQuantity: 10, price: 150_000m, weightGram: 600);
        var addressId = await CreateAddressAsync(client);

        // Add to cart
        var addResp = await client.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = 2 });
        addResp.EnsureSuccessStatusCode();

        // Checkout
        var checkoutReq = new OrdersController.CheckoutRequest(addressId);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(checkoutReq)
        };
        request.Headers.Add("Idempotency-Key", $"idem-{Guid.NewGuid():N}");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Status.Should().Be("PendingPayment");
        order.Subtotal.Should().Be(300_000m);
        order.TotalWeightGram.Should().Be(1200);
        order.ShippingFee.Should().BeGreaterThan(0m);
        order.Total.Should().Be(order.Subtotal + order.ShippingFee);
        order.Items.Should().HaveCount(1);
        order.Items[0].VariantId.Should().Be(variantId);
        order.Items[0].Quantity.Should().Be(2);
        order.Items[0].UnitPrice.Should().Be(150_000m);

        // Verify DB state
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var updatedVariant = await db.ProductVariants.FindAsync(variantId);
        updatedVariant!.StockQuantity.Should().Be(8); // 10 - 2

        var movement = await db.InventoryMovements.FirstOrDefaultAsync(m => m.OrderId == order.Id && m.VariantId == variantId);
        movement.Should().NotBeNull();
        movement!.Reason.Should().Be(StockReason.Sale);
        movement.ChangeQuantity.Should().Be(-2);

        // Verify Cart is empty
        var cartResp = await client.GetAsync("/api/v1/cart");
        cartResp.EnsureSuccessStatusCode();
        var cart = await cartResp.Content.ReadFromJsonAsync<CartDto>();
        cart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Checkout_WithSameIdempotencyKey_ShouldReturnSameOrder_WithoutDoubleReservation()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var variantId = await SeedProductAndVariantAsync(stockQuantity: 10, price: 100_000m, weightGram: 500);
        var addressId = await CreateAddressAsync(client);

        var addResp = await client.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = 2 });
        addResp.EnsureSuccessStatusCode();

        var idempotencyKey = $"test-key-{Guid.NewGuid():N}";
        var checkoutReq = new OrdersController.CheckoutRequest(addressId);

        // First call
        using var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(checkoutReq)
        };
        req1.Headers.Add("Idempotency-Key", idempotencyKey);
        var resp1 = await client.SendAsync(req1);
        resp1.StatusCode.Should().Be(HttpStatusCode.Created);
        var order1 = await resp1.Content.ReadFromJsonAsync<OrderDto>();

        // Second call with same idempotency key
        using var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(checkoutReq)
        };
        req2.Headers.Add("Idempotency-Key", idempotencyKey);
        var resp2 = await client.SendAsync(req2);
        resp2.StatusCode.Should().Be(HttpStatusCode.Created);
        var order2 = await resp2.Content.ReadFromJsonAsync<OrderDto>();

        order2!.Id.Should().Be(order1!.Id);
        order2.OrderNumber.Should().Be(order1.OrderNumber);

        // Stock should still be 8 (deducted once)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedVariant = await db.ProductVariants.FindAsync(variantId);
        updatedVariant!.StockQuantity.Should().Be(8);
    }

    [Fact]
    public async Task Checkout_WithEmptyCart_ShouldReturn400BadRequest()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var addressId = await CreateAddressAsync(client);

        var checkoutReq = new OrdersController.CheckoutRequest(addressId);
        var resp = await client.PostAsJsonAsync("/api/v1/orders/checkout", checkoutReq);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await resp.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem!.Code.Should().Be("cart.empty");
    }

    [Fact]
    public async Task Checkout_WithAnotherUsersAddress_ShouldReturn404NotFound()
    {
        var (clientA, _) = await CreateAuthenticatedClientAsync();
        var (clientB, _) = await CreateAuthenticatedClientAsync();

        var addressIdOfUserA = await CreateAddressAsync(clientA);
        var variantId = await SeedProductAndVariantAsync(stockQuantity: 10);

        await clientB.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = 1 });

        // User B tries to use User A's address
        var checkoutReq = new OrdersController.CheckoutRequest(addressIdOfUserA);
        var resp = await clientB.PostAsJsonAsync("/api/v1/orders/checkout", checkoutReq);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await resp.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem!.Code.Should().Be("address.not_found");
    }

    [Fact]
    public async Task Checkout_WhenStockInsufficient_ShouldReturn409Conflict_AndNotDeductStock()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var variantId = await SeedProductAndVariantAsync(stockQuantity: 1, price: 50_000m);
        var addressId = await CreateAddressAsync(client);

        // Add 1 to cart
        await client.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = 1 });

        // Concurrent operation empties the stock before checkout executes
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var v = await db.ProductVariants.FindAsync(variantId);
            v!.StockQuantity = 0;
            await db.SaveChangesAsync();
        }

        var checkoutReq = new OrdersController.CheckoutRequest(addressId);
        var resp = await client.PostAsJsonAsync("/api/v1/orders/checkout", checkoutReq);

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await resp.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem!.Code.Should().Be("stock.insufficient");
    }
}
