using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Api.Controllers;
using SportShop.Domain.Carts;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;
using SportShop.Domain.Shipping;
using SportShop.Domain.Users;
using SportShop.Infrastructure.Identity;
using SportShop.Infrastructure.Persistence;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class OrderConcurrencyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrderConcurrencyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConcurrentCheckout_50ParallelRequests_WithStock10_Exactly10Succeed_StockBecomes0()
    {
        const int initialStock = 10;
        const int concurrentUsersCount = 50;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Ensure shipping rate exists
        var rate = await db.ShippingRates.FirstOrDefaultAsync(r => r.ProvinceCode == "31");
        if (rate is null)
        {
            rate = new ShippingRate("31", "DKI Jakarta", 10_000m, 5_000m, 1, 3);
            db.ShippingRates.Add(rate);
        }

        // 2. Seed product and variant with stock = 10
        var suffix = Guid.NewGuid().ToString()[..8];
        var category = new Category(Guid.NewGuid(), $"Cat-{suffix}", $"cat-{suffix}");
        var brand = new Brand(Guid.NewGuid(), $"Brand-{suffix}", $"brand-{suffix}");
        db.Categories.Add(category);
        db.Brands.Add(brand);

        var product = new Product(
            Guid.NewGuid(),
            category.Id,
            brand.Id,
            $"Produk-Konkurensi-{suffix}",
            $"produk-konkurensi-{suffix}",
            "Produk untuk uji konkurensi",
            100_000m);
        db.Products.Add(product);

        var variant = new ProductVariant(
            Guid.NewGuid(),
            product.Id,
            $"SKU-CONC-{suffix}",
            100_000m,
            weightGram: 500,
            stockQuantity: initialStock,
            "42",
            "Merah");
        db.ProductVariants.Add(variant);

        // 3. Prepare 50 users, addresses, and carts
        var userSetups = new List<(Guid UserId, Guid AddressId, HttpClient Client)>();

        for (var i = 1; i <= concurrentUsersCount; i++)
        {
            var userId = Guid.NewGuid();
            var email = $"conc_user_{suffix}_{i:D3}@sportshop.dev";

            var user = new ApplicationUser
            {
                Id = userId,
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                FullName = $"Concurrency User {i}",
                PhoneNumber = $"0812{i:D7}",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            db.Users.Add(user);

            var address = new Address(
                Guid.NewGuid(),
                userId,
                "Rumah",
                $"Penerima {i}",
                $"0812{i:D7}",
                "31",
                "Jakarta Selatan",
                "Cilandak",
                "12430",
                $"Jl. Uji Konkurensi No. {i}",
                isDefault: true);
            db.Addresses.Add(address);

            var cart = new Cart(Guid.NewGuid(), userId);
            var cartItem = new CartItem(
                Guid.NewGuid(),
                cart.Id,
                variant.Id,
                quantity: 1,
                priceAtAdd: variant.Price);
            cart.Items.Add(cartItem);
            db.Carts.Add(cart);

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });
            client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.0.0.{i}");
            var token = AuthTestHelper.GenerateCustomerToken(userId, email);
            client.WithBearerToken(token);

            userSetups.Add((userId, address.Id, client));
        }

        await db.SaveChangesAsync();

        // 4. Fire all 50 checkout requests concurrently
        var checkoutTasks = userSetups.Select(u =>
        {
            var request = new OrdersController.CheckoutRequest(u.AddressId);
            return u.Client.PostAsJsonAsync("/api/v1/orders/checkout", request);
        }).ToList();

        var responses = await Task.WhenAll(checkoutTasks);

        // 5. Assertions
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        var otherStatusCodes = responses.Where(r => r.StatusCode != HttpStatusCode.Created && r.StatusCode != HttpStatusCode.Conflict).ToList();

        otherStatusCodes.Should().BeEmpty();
        successCount.Should().Be(initialStock, "tepat 10 pesanan yang berhasil sesuai stok awal");
        conflictCount.Should().Be(concurrentUsersCount - initialStock, "40 permintaan lainnya harus mendapat 409 Conflict karena stok habis");

        // 6. Verify database consistency
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var updatedVariant = await verifyDb.ProductVariants.AsNoTracking().FirstAsync(v => v.Id == variant.Id);
        updatedVariant.StockQuantity.Should().Be(0, "stok akhir varian harus tepat 0 tanpa overselling");

        var ordersCount = await verifyDb.Orders.CountAsync(o => o.Items.Any(i => i.VariantId == variant.Id));
        ordersCount.Should().Be(initialStock, "harus ada tepat 10 pesanan tercatat di database");

        var movementsCount = await verifyDb.InventoryMovements
            .CountAsync(m => m.VariantId == variant.Id && m.Reason == StockReason.Sale);
        movementsCount.Should().Be(initialStock, "harus ada tepat 10 pergerakan stok penjualan");
    }
}
