using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Catalog.DTOs;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class InventoryAndStockAdjustmentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public InventoryAndStockAdjustmentTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.48.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private static async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand("admin@sportshop.dev", "Admin123!"));
        loginResp.EnsureSuccessStatusCode();
        var loginResult = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        client.WithBearerToken(loginResult!.AccessToken);
    }

    [Fact]
    public async Task StockAdjustments_ShouldEnforceNonNegativeStock_AndRecordMovements()
    {
        // Arrange
        var client = CreateTestClient();
        await AuthenticateAsAdminAsync(client);
        var suffix = Guid.NewGuid().ToString()[..8];

        var catResp = await client.PostAsJsonAsync("/api/v1/admin/categories", new CreateCategoryRequest($"CatStock-{suffix}", $"cat-stock-{suffix}"));
        var cat = await catResp.Content.ReadFromJsonAsync<CategoryDto>();

        var brandResp = await client.PostAsJsonAsync("/api/v1/admin/brands", new CreateBrandRequest($"BrandStock-{suffix}", $"brand-stock-{suffix}"));
        var brand = await brandResp.Content.ReadFromJsonAsync<BrandDto>();

        var prodResp = await client.PostAsJsonAsync("/api/v1/admin/products", new CreateProductRequest(
            cat!.Id, brand!.Id, $"Bola Basket-{suffix}", $"bola-basket-{suffix}", "Bola Basket", 200_000m));
        var product = await prodResp.Content.ReadFromJsonAsync<ProductDto>();

        // Create variant with stock 10
        var varReq = new CreateVariantRequest($"SKU-BALL-{suffix}", "Standard", "Oranye", 200000m, 600, 10);
        var varResp = await client.PostAsJsonAsync($"/api/v1/admin/products/{product!.Id}/variants", varReq);
        var variant = await varResp.Content.ReadFromJsonAsync<ProductVariantDto>();

        // 1. Validation error: Note is required for manual adjust
        var noNoteResp = await client.PostAsJsonAsync($"/api/v1/admin/variants/{variant!.Id}/stock-adjustments", new AdjustStockRequest(5, ""));
        noNoteResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 2. Add stock (+5)
        var addStockResp = await client.PostAsJsonAsync($"/api/v1/admin/variants/{variant.Id}/stock-adjustments", new AdjustStockRequest(5, "Penerimaan barang dari supplier"));
        addStockResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var addMovement = await addStockResp.Content.ReadFromJsonAsync<InventoryMovementDto>();
        addMovement.Should().NotBeNull();
        addMovement!.ChangeQuantity.Should().Be(5);
        addMovement.Reason.Should().Be("ManualAdjust");
        addMovement.Note.Should().Be("Penerimaan barang dari supplier");

        // Verify stock is now 15
        var checkProd1 = await (await client.GetAsync($"/api/v1/admin/products/{product.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        checkProd1!.Variants!.First(v => v.Id == variant.Id).StockQuantity.Should().Be(15);

        // 3. Deduct stock (-10)
        var deductResp = await client.PostAsJsonAsync($"/api/v1/admin/variants/{variant.Id}/stock-adjustments", new AdjustStockRequest(-10, "Koreksi barang rusak"));
        deductResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify stock is now 5
        var checkProd2 = await (await client.GetAsync($"/api/v1/admin/products/{product.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        checkProd2!.Variants!.First(v => v.Id == variant.Id).StockQuantity.Should().Be(5);

        // 4. Over-deduct (-6 when stock is 5) -> should return 409 Conflict (stock cannot be negative)
        var overDeductResp = await client.PostAsJsonAsync($"/api/v1/admin/variants/{variant.Id}/stock-adjustments", new AdjustStockRequest(-6, "Percobaan over-deduct"));
        overDeductResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var conflictProblem = await overDeductResp.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        conflictProblem!.Code.Should().Be("stock.insufficient");

        // Stock should still be 5
        var checkProd3 = await (await client.GetAsync($"/api/v1/admin/products/{product.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        checkProd3!.Variants!.First(v => v.Id == variant.Id).StockQuantity.Should().Be(5);

        // 5. Test Low Stock endpoint (threshold 5)
        var lowStockResp = await client.GetAsync("/api/v1/admin/inventory/low-stock?threshold=5");
        lowStockResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var lowStockItems = await lowStockResp.Content.ReadFromJsonAsync<List<LowStockVariantDto>>();
        lowStockItems.Should().NotBeNull();
        lowStockItems.Should().Contain(v => v.VariantId == variant.Id && v.StockQuantity == 5);
    }
}
