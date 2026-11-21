using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Catalog.DTOs;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class PublicCatalogAndCachingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PublicCatalogAndCachingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.51.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private static async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand("admin@sportshop.com", "J3&fP6z@"));
        loginResp.EnsureSuccessStatusCode();
        var loginResult = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        client.WithBearerToken(loginResult!.AccessToken);
    }

    [Fact]
    public async Task CategoriesTree_ShouldBeCached_AndInvalidatedOnAdminChange()
    {
        var adminClient = CreateTestClient();
        await AuthenticateAsAdminAsync(adminClient);
        var publicClient = CreateTestClient();
        var suffix = Guid.NewGuid().ToString()[..8];

        var cacheService = _factory.Services.GetRequiredService<ICacheService>();

        // 1. Initial GET /categories
        var firstResp = await publicClient.GetAsync("/api/v1/categories");
        firstResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify cat:tree:v1 is cached in Redis
        var cachedTree = await cacheService.GetAsync<List<CategoryDto>>("cat:tree:v1");
        cachedTree.Should().NotBeNull();

        // 2. Admin creates a new category
        var createCatResp = await adminClient.PostAsJsonAsync("/api/v1/admin/categories",
            new CreateCategoryRequest($"TreeCat-{suffix}", $"tree-cat-{suffix}"));
        createCatResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify cat:tree:v1 was invalidated (null in Redis)
        var invalidatedTree = await cacheService.GetAsync<List<CategoryDto>>("cat:tree:v1");
        invalidatedTree.Should().BeNull();

        // 3. Second GET /categories repopulates cache and contains new category
        var secondResp = await publicClient.GetAsync("/api/v1/categories");
        secondResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondCategories = await secondResp.Content.ReadFromJsonAsync<List<CategoryDto>>();
        secondCategories.Should().Contain(c => c.Slug == $"tree-cat-{suffix}");

        var repopulatedTree = await cacheService.GetAsync<List<CategoryDto>>("cat:tree:v1");
        repopulatedTree.Should().NotBeNull();
    }

    [Fact]
    public async Task BrandsList_ShouldBeCached_AndInvalidatedOnAdminChange()
    {
        var adminClient = CreateTestClient();
        await AuthenticateAsAdminAsync(adminClient);
        var publicClient = CreateTestClient();
        var suffix = Guid.NewGuid().ToString()[..8];

        var cacheService = _factory.Services.GetRequiredService<ICacheService>();

        // 1. Initial GET /brands
        var firstResp = await publicClient.GetAsync("/api/v1/brands");
        firstResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var cachedBrands = await cacheService.GetAsync<List<BrandDto>>("brand:list:v1");
        cachedBrands.Should().NotBeNull();

        // 2. Admin creates a new brand
        var createBrandResp = await adminClient.PostAsJsonAsync("/api/v1/admin/brands",
            new CreateBrandRequest($"BrandCache-{suffix}", $"brand-cache-{suffix}"));
        createBrandResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify brand:list:v1 was invalidated
        var invalidatedBrands = await cacheService.GetAsync<List<BrandDto>>("brand:list:v1");
        invalidatedBrands.Should().BeNull();

        // 3. Second GET /brands repopulates cache
        var secondResp = await publicClient.GetAsync("/api/v1/brands");
        secondResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondList = await secondResp.Content.ReadFromJsonAsync<List<BrandDto>>();
        secondList.Should().Contain(b => b.Slug == $"brand-cache-{suffix}");

        var repopulatedBrands = await cacheService.GetAsync<List<BrandDto>>("brand:list:v1");
        repopulatedBrands.Should().NotBeNull();
    }

    [Fact]
    public async Task ProductDetail_ShouldBeCached_AndInvalidatedOnAdminUpdate()
    {
        var adminClient = CreateTestClient();
        await AuthenticateAsAdminAsync(adminClient);
        var publicClient = CreateTestClient();
        var suffix = Guid.NewGuid().ToString()[..8];

        var cacheService = _factory.Services.GetRequiredService<ICacheService>();

        // Setup category & brand
        var catResp = await adminClient.PostAsJsonAsync("/api/v1/admin/categories", new CreateCategoryRequest($"CatProd-{suffix}", $"cat-prod-{suffix}"));
        var cat = await catResp.Content.ReadFromJsonAsync<CategoryDto>();

        var brandResp = await adminClient.PostAsJsonAsync("/api/v1/admin/brands", new CreateBrandRequest($"BrandProd-{suffix}", $"brand-prod-{suffix}"));
        var brand = await brandResp.Content.ReadFromJsonAsync<BrandDto>();

        var prodSlug = $"cached-product-{suffix}";
        var prodResp = await adminClient.PostAsJsonAsync("/api/v1/admin/products", new CreateProductRequest(
            cat!.Id, brand!.Id, $"Cached Product {suffix}", prodSlug, "Deskripsi awal", 500_000m));
        var product = await prodResp.Content.ReadFromJsonAsync<ProductDto>();

        await adminClient.PostAsJsonAsync($"/api/v1/admin/products/{product!.Id}/variants",
            new CreateVariantRequest($"SKU-CP-{suffix}", "42", "Merah", 500_000m, 400, 15));

        // 1. Initial GET /products/{slug}
        var firstResp = await publicClient.GetAsync($"/api/v1/products/{prodSlug}");
        firstResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialDetail = await firstResp.Content.ReadFromJsonAsync<ProductDto>();
        initialDetail!.Description.Should().Be("Deskripsi awal");

        // Verify prod:detail:{slug}:v1 is in Redis
        var cacheKey = $"prod:detail:{prodSlug}:v1";
        var cachedProd = await cacheService.GetAsync<ProductDto>(cacheKey);
        cachedProd.Should().NotBeNull();
        cachedProd!.Name.Should().Be($"Cached Product {suffix}");

        // 2. Admin updates product details
        var updateResp = await adminClient.PutAsJsonAsync($"/api/v1/admin/products/{product.Id}", new UpdateProductRequest(
            cat.Id, brand.Id, $"Cached Product {suffix}", prodSlug, "Deskripsi baru yang diperbarui", true));
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify cache was invalidated
        var invalidatedProd = await cacheService.GetAsync<ProductDto>(cacheKey);
        invalidatedProd.Should().BeNull();

        // 3. Second GET /products/{slug} retrieves updated data
        var secondResp = await publicClient.GetAsync($"/api/v1/products/{prodSlug}");
        secondResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedDetail = await secondResp.Content.ReadFromJsonAsync<ProductDto>();
        updatedDetail!.Description.Should().Be("Deskripsi baru yang diperbarui");

        // 4. Non-existent slug returns 404
        var notFoundResp = await publicClient.GetAsync("/api/v1/products/slug-yang-pasti-tidak-ada-12345678");
        notFoundResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
