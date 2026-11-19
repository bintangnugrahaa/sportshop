using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Common;
using SportShop.Application.Features.Catalog.DTOs;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class AdminProductsAndVariantsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminProductsAndVariantsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.46.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private static async Task<(Guid CategoryId, Guid BrandId)> SetupCategoryAndBrandAsync(HttpClient client, string suffix)
    {
        var catResp = await client.PostAsJsonAsync("/api/v1/admin/categories", new CreateCategoryRequest($"Kategori-{suffix}", $"kat-{suffix}"));
        catResp.EnsureSuccessStatusCode();
        var cat = await catResp.Content.ReadFromJsonAsync<CategoryDto>();

        var brandResp = await client.PostAsJsonAsync("/api/v1/admin/brands", new CreateBrandRequest($"Brand-{suffix}", $"brand-{suffix}"));
        brandResp.EnsureSuccessStatusCode();
        var brand = await brandResp.Content.ReadFromJsonAsync<BrandDto>();

        return (cat!.Id, brand!.Id);
    }

    [Fact]
    public async Task Products_CrudAndValidation_ShouldSucceed()
    {
        // Arrange
        var client = CreateTestClient();
        client.WithBearerToken(AuthTestHelper.GenerateAdminToken());
        var suffix = Guid.NewGuid().ToString()[..8];
        var (categoryId, brandId) = await SetupCategoryAndBrandAsync(client, suffix);

        // 1. Invalid slug should return 400
        var invalidSlugResp = await client.PostAsJsonAsync("/api/v1/admin/products", new CreateProductRequest(
            categoryId, brandId, "Sepatu Running", "Sepatu Running INVALID SLUG", "Deskripsi"));
        invalidSlugResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 2. Non-existent category should return 404
        var nonExistentCatResp = await client.PostAsJsonAsync("/api/v1/admin/products", new CreateProductRequest(
            Guid.NewGuid(), brandId, "Sepatu Running", $"sepatu-{suffix}", "Deskripsi"));
        nonExistentCatResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 3. Create valid product
        var createRequest = new CreateProductRequest(
            categoryId,
            brandId,
            $"Sepatu Running Pro-{suffix}",
            $"sepatu-running-pro-{suffix}",
            "Sepatu lari kualitas premium",
            250_000m,
            true);

        var createResp = await client.PostAsJsonAsync("/api/v1/admin/products", createRequest);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createResp.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Slug.Should().Be($"sepatu-running-pro-{suffix}");
        product.BasePrice.Should().Be(250_000m);

        // 4. Duplicate slug should return 409
        var dupResp = await client.PostAsJsonAsync("/api/v1/admin/products", createRequest);
        dupResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 5. Update product
        var updateRequest = new UpdateProductRequest(
            categoryId,
            brandId,
            $"Sepatu Running Pro V2-{suffix}",
            $"sepatu-running-pro-v2-{suffix}",
            "Deskripsi baru",
            true);
        var updateResp = await client.PutAsJsonAsync($"/api/v1/admin/products/{product.Id}", updateRequest);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedProduct = await updateResp.Content.ReadFromJsonAsync<ProductDto>();
        updatedProduct!.Name.Should().Be($"Sepatu Running Pro V2-{suffix}");

        // 6. Get by id
        var getResp = await client.GetAsync($"/api/v1/admin/products/{product.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7. Soft delete product
        var deleteResp = await client.DeleteAsync($"/api/v1/admin/products/{product.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyResp = await client.GetAsync($"/api/v1/admin/products/{product.Id}");
        var verifyProduct = await verifyResp.Content.ReadFromJsonAsync<ProductDto>();
        verifyProduct!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Variants_CrudAndRules_ShouldEnforceSkuPriceWeightStockConstraints()
    {
        // Arrange
        var client = CreateTestClient();
        client.WithBearerToken(AuthTestHelper.GenerateAdminToken());
        var suffix = Guid.NewGuid().ToString()[..8];
        var (categoryId, brandId) = await SetupCategoryAndBrandAsync(client, suffix);

        var prodResp = await client.PostAsJsonAsync("/api/v1/admin/products", new CreateProductRequest(
            categoryId, brandId, $"Jersey Bola-{suffix}", $"jersey-bola-{suffix}", "Jersey"));
        prodResp.EnsureSuccessStatusCode();
        var product = await prodResp.Content.ReadFromJsonAsync<ProductDto>();

        // 1. Validation error: price < 0
        var negPriceResp = await client.PostAsJsonAsync($"/api/v1/admin/products/{product!.Id}/variants", new CreateVariantRequest(
            $"SKU-NEG-{suffix}", "M", "Merah", -50000m, 200, 10));
        negPriceResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 2. Validation error: weight <= 0
        var zeroWeightResp = await client.PostAsJsonAsync($"/api/v1/admin/products/{product.Id}/variants", new CreateVariantRequest(
            $"SKU-WT-{suffix}", "M", "Merah", 150000m, 0, 10));
        zeroWeightResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 3. Validation error: stock < 0
        var negStockResp = await client.PostAsJsonAsync($"/api/v1/admin/products/{product.Id}/variants", new CreateVariantRequest(
            $"SKU-STK-{suffix}", "M", "Merah", 150000m, 200, -5));
        negStockResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 4. Create first variant (price 150_000)
        var var1Req = new CreateVariantRequest($"SKU-1-{suffix}", "M", "Merah", 150000m, 200, 10);
        var var1Resp = await client.PostAsJsonAsync($"/api/v1/admin/products/{product.Id}/variants", var1Req);
        var1Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var variant1 = await var1Resp.Content.ReadFromJsonAsync<ProductVariantDto>();
        variant1.Should().NotBeNull();

        // Check product base price recalculated to 150_000
        var checkProd1 = await (await client.GetAsync($"/api/v1/admin/products/{product.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        checkProd1!.BasePrice.Should().Be(150000m);

        // 5. Create second variant with lower price (120_000)
        var var2Req = new CreateVariantRequest($"SKU-2-{suffix}", "L", "Merah", 120000m, 220, 5);
        var var2Resp = await client.PostAsJsonAsync($"/api/v1/admin/products/{product.Id}/variants", var2Req);
        var2Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var variant2 = await var2Resp.Content.ReadFromJsonAsync<ProductVariantDto>();

        // Check product base price recalculated to lowest variant: 120_000
        var checkProd2 = await (await client.GetAsync($"/api/v1/admin/products/{product.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        checkProd2!.BasePrice.Should().Be(120000m);

        // 6. Duplicate SKU should return 409 Conflict
        var dupSkuResp = await client.PostAsJsonAsync($"/api/v1/admin/products/{product.Id}/variants", new CreateVariantRequest(
            $"SKU-1-{suffix}", "XL", "Merah", 150000m, 250, 5));
        dupSkuResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 7. Duplicate Options (M, Merah) should return 409 Conflict
        var dupOptResp = await client.PostAsJsonAsync($"/api/v1/admin/products/{product.Id}/variants", new CreateVariantRequest(
            $"SKU-DIFF-{suffix}", "M", "Merah", 150000m, 200, 5));
        dupOptResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 8. Update variant 2 (change price to 180_000)
        var updateVar2Resp = await client.PutAsJsonAsync($"/api/v1/admin/variants/{variant2!.Id}", new UpdateVariantRequest(
            $"SKU-2-{suffix}", "L", "Merah", 180000m, 220, true));
        updateVar2Resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Product base price should now become 150_000 (variant 1 is now lowest)
        var checkProd3 = await (await client.GetAsync($"/api/v1/admin/products/{product.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        checkProd3!.BasePrice.Should().Be(150000m);

        // 9. Soft delete variant
        var deleteVarResp = await client.DeleteAsync($"/api/v1/admin/variants/{variant1!.Id}");
        deleteVarResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Product base price should now be 180_000 (only variant 2 is active)
        var checkProd4 = await (await client.GetAsync($"/api/v1/admin/products/{product.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        checkProd4!.BasePrice.Should().Be(180000m);
    }
}
