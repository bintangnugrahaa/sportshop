using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Cart.DTOs;
using SportShop.Domain.Catalog;
using SportShop.Infrastructure.Persistence;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class CartEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CartEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.47.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateTestClient();
        var email = $"cart_user_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Cart Tester", "08123456789"));
        regResp.EnsureSuccessStatusCode();

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        loginResp.EnsureSuccessStatusCode();
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        client.WithBearerToken(loginData!.AccessToken);
        return client;
    }

    private async Task<(Guid ProductId, Guid VariantId)> SeedProductAndVariantAsync(int stockQuantity = 10, decimal price = 150_000m)
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
            "Deskripsi produk test",
            price);
        db.Products.Add(product);

        var variant = new ProductVariant(
            Guid.NewGuid(),
            product.Id,
            $"SKU-{suffix}",
            price,
            weightGram: 500,
            stockQuantity: stockQuantity,
            optionSize: "42",
            optionColor: "Biru");
        db.ProductVariants.Add(variant);

        await db.SaveChangesAsync();
        return (product.Id, variant.Id);
    }

    [Fact]
    public async Task GetCart_ShouldReturnUnauthorized_WhenNotLoggedIn()
    {
        var client = CreateTestClient();

        var response = await client.GetAsync("/api/v1/cart");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cart_FullFlow_AddUpdateRemoveClear_ShouldSucceed()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var (_, variantId) = await SeedProductAndVariantAsync(stockQuantity: 10, price: 120_000m);

        // 1. Get empty cart
        var getCartResp = await client.GetAsync("/api/v1/cart");
        getCartResp.EnsureSuccessStatusCode();
        var emptyCart = await getCartResp.Content.ReadFromJsonAsync<CartDto>();
        emptyCart.Should().NotBeNull();
        emptyCart!.Items.Should().BeEmpty();
        emptyCart.TotalItems.Should().Be(0);
        emptyCart.Subtotal.Should().Be(0);

        // 2. Add item (qty = 2)
        var addResp = await client.PostAsJsonAsync("/api/v1/cart/items", new CartController.AddCartItemRequest(variantId, 2));
        addResp.EnsureSuccessStatusCode();
        var cartAfterAdd = await addResp.Content.ReadFromJsonAsync<CartDto>();
        cartAfterAdd.Should().NotBeNull();
        cartAfterAdd!.Items.Should().HaveCount(1);
        cartAfterAdd.TotalItems.Should().Be(2);
        cartAfterAdd.Subtotal.Should().Be(240_000m);
        cartAfterAdd.TotalWeightGram.Should().Be(1000);

        // 3. Add more of the same item (qty = 3) -> total 5
        var addMoreResp = await client.PostAsJsonAsync("/api/v1/cart/items", new CartController.AddCartItemRequest(variantId, 3));
        addMoreResp.EnsureSuccessStatusCode();
        var cartAfterAddMore = await addMoreResp.Content.ReadFromJsonAsync<CartDto>();
        cartAfterAddMore!.Items[0].Quantity.Should().Be(5);
        cartAfterAddMore.TotalItems.Should().Be(5);
        cartAfterAddMore.Subtotal.Should().Be(600_000m);

        // 4. Update quantity to 4
        var updateResp = await client.PutAsJsonAsync($"/api/v1/cart/items/{variantId}", new CartController.UpdateCartItemRequest(4));
        updateResp.EnsureSuccessStatusCode();
        var cartAfterUpdate = await updateResp.Content.ReadFromJsonAsync<CartDto>();
        cartAfterUpdate!.Items[0].Quantity.Should().Be(4);
        cartAfterUpdate.Subtotal.Should().Be(480_000m);

        // 5. Remove item
        var removeResp = await client.DeleteAsync($"/api/v1/cart/items/{variantId}");
        removeResp.EnsureSuccessStatusCode();
        var cartAfterRemove = await removeResp.Content.ReadFromJsonAsync<CartDto>();
        cartAfterRemove!.Items.Should().BeEmpty();

        // 6. Add again then Clear
        await client.PostAsJsonAsync("/api/v1/cart/items", new CartController.AddCartItemRequest(variantId, 1));
        var clearResp = await client.DeleteAsync("/api/v1/cart");
        clearResp.EnsureSuccessStatusCode();

        var finalGet = await client.GetAsync("/api/v1/cart");
        var finalCart = await finalGet.Content.ReadFromJsonAsync<CartDto>();
        finalCart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddItem_ShouldReturnConflict_WhenQuantityExceedsAvailableStock()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (_, variantId) = await SeedProductAndVariantAsync(stockQuantity: 3);

        // Requesting 4 when stock is 3
        var response = await client.PostAsJsonAsync("/api/v1/cart/items", new CartController.AddCartItemRequest(variantId, 4));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("stock.insufficient");
    }

    [Fact]
    public async Task Cart_ShouldBeIsolatedPerUser()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();

        var (_, variantId) = await SeedProductAndVariantAsync(stockQuantity: 10);

        // User A adds item
        var addResp = await clientA.PostAsJsonAsync("/api/v1/cart/items", new CartController.AddCartItemRequest(variantId, 2));
        addResp.EnsureSuccessStatusCode();

        // User B cart should still be empty
        var getBResp = await clientB.GetAsync("/api/v1/cart");
        getBResp.EnsureSuccessStatusCode();
        var cartB = await getBResp.Content.ReadFromJsonAsync<CartDto>();
        cartB!.Items.Should().BeEmpty();
    }
}
