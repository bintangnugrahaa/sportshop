using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Common;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Catalog.DTOs;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class PublicProductsListTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PublicProductsListTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.49.{Random.Shared.Next(1, 10000)}");
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
    public async Task ListProducts_ShouldSupportFilteringSortingAndPagination()
    {
        var client = CreateTestClient();
        await AuthenticateAsAdminAsync(client);
        var suffix = Guid.NewGuid().ToString()[..8];

        // 1. Setup Categories: Parent (Sepatu) -> Child (Sepatu Lari), and Separate (Jersey)
        var catParentResp = await client.PostAsJsonAsync("/api/v1/admin/categories", new CreateCategoryRequest($"Sepatu-{suffix}", $"sepatu-{suffix}"));
        var catParent = await catParentResp.Content.ReadFromJsonAsync<CategoryDto>();

        var catChildResp = await client.PostAsJsonAsync("/api/v1/admin/categories", new CreateCategoryRequest($"Sepatu Lari-{suffix}", $"sepatu-lari-{suffix}", ParentId: catParent!.Id));
        var catChild = await catChildResp.Content.ReadFromJsonAsync<CategoryDto>();

        var catJerseyResp = await client.PostAsJsonAsync("/api/v1/admin/categories", new CreateCategoryRequest($"Jersey-{suffix}", $"jersey-{suffix}"));
        var catJersey = await catJerseyResp.Content.ReadFromJsonAsync<CategoryDto>();

        // 2. Setup Brands: Brand Nike & Brand Adidas
        var brandNikeResp = await client.PostAsJsonAsync("/api/v1/admin/brands", new CreateBrandRequest($"Nike-{suffix}", $"nike-{suffix}"));
        var brandNike = await brandNikeResp.Content.ReadFromJsonAsync<BrandDto>();

        var brandAdidasResp = await client.PostAsJsonAsync("/api/v1/admin/brands", new CreateBrandRequest($"Adidas-{suffix}", $"adidas-{suffix}"));
        var brandAdidas = await brandAdidasResp.Content.ReadFromJsonAsync<BrandDto>();

        // 3. Setup Products:
        // Product 1: In Child Category (Sepatu Lari), Nike, Price 750k, In Stock (stock 10), Name "Alpha Shoes"
        var p1Resp = await client.PostAsJsonAsync("/api/v1/admin/products", new CreateProductRequest(
            catChild!.Id, brandNike!.Id, $"Alpha Shoes {suffix}", $"alpha-shoes-{suffix}", "Sepatu Lari Cepat", 750_000m));
        var p1 = await p1Resp.Content.ReadFromJsonAsync<ProductDto>();
        await client.PostAsJsonAsync($"/api/v1/admin/products/{p1!.Id}/variants", new CreateVariantRequest($"SKU-P1-{suffix}", "42", "Hitam", 750_000m, 500, 10));

        // Product 2: In Jersey Category, Adidas, Price 300k, Out of Stock (stock 0), Name "Beta Jersey"
        var p2Resp = await client.PostAsJsonAsync("/api/v1/admin/products", new CreateProductRequest(
            catJersey!.Id, brandAdidas!.Id, $"Beta Jersey {suffix}", $"beta-jersey-{suffix}", "Jersey Keren", 300_000m));
        var p2 = await p2Resp.Content.ReadFromJsonAsync<ProductDto>();
        await client.PostAsJsonAsync($"/api/v1/admin/products/{p2!.Id}/variants", new CreateVariantRequest($"SKU-P2-{suffix}", "L", "Merah", 300_000m, 200, 0));

        // Product 3: In Parent Category (Sepatu), Nike, Price 1_500_000, In Stock (stock 5), Inactive
        var p3Resp = await client.PostAsJsonAsync("/api/v1/admin/products", new CreateProductRequest(
            catParent.Id, brandNike.Id, $"Gamma Shoes {suffix}", $"gamma-shoes-{suffix}", "Sepatu Mahal", 1_500_000m));
        var p3 = await p3Resp.Content.ReadFromJsonAsync<ProductDto>();
        await client.PostAsJsonAsync($"/api/v1/admin/products/{p3!.Id}/variants", new CreateVariantRequest($"SKU-P3-{suffix}", "44", "Putih", 1_500_000m, 600, 5));
        // Soft-delete P3 so it is inactive
        await client.DeleteAsync($"/api/v1/admin/products/{p3.Id}");

        // Now test as public client (no auth)
        var publicClient = CreateTestClient();

        // TEST 1: Inactive product (p3) is NEVER returned in public list
        var allResp = await publicClient.GetAsync($"/api/v1/products?q={suffix}");
        allResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var allResult = await allResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        allResult.Should().NotBeNull();
        allResult!.Items.Should().Contain(p => p.Id == p1.Id);
        allResult.Items.Should().Contain(p => p.Id == p2.Id);
        allResult.Items.Should().NotContain(p => p.Id == p3.Id);

        // TEST 2: Filter by category hierarchy (parent category slug should include child category products)
        var catParentFilterResp = await publicClient.GetAsync($"/api/v1/products?categorySlug=sepatu-{suffix}");
        var catParentResult = await catParentFilterResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        catParentResult!.Items.Should().Contain(p => p.Id == p1.Id); // p1 is under child category sepatu-lari
        catParentResult.Items.Should().NotContain(p => p.Id == p2.Id);

        // Filter by child category slug specifically
        var catChildFilterResp = await publicClient.GetAsync($"/api/v1/products?categorySlug=sepatu-lari-{suffix}");
        var catChildResult = await catChildFilterResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        catChildResult!.Items.Should().Contain(p => p.Id == p1.Id);
        catChildResult.Items.Should().NotContain(p => p.Id == p2.Id);

        // TEST 3: Filter by brand slug
        var brandFilterResp = await publicClient.GetAsync($"/api/v1/products?brandSlug=adidas-{suffix}");
        var brandResult = await brandFilterResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        brandResult!.Items.Should().Contain(p => p.Id == p2.Id);
        brandResult.Items.Should().NotContain(p => p.Id == p1.Id);

        // TEST 4: Filter by price range
        var priceFilterResp = await publicClient.GetAsync($"/api/v1/products?minPrice=500000&maxPrice=1000000&q={suffix}");
        var priceResult = await priceFilterResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        priceResult!.Items.Should().Contain(p => p.Id == p1.Id);
        priceResult.Items.Should().NotContain(p => p.Id == p2.Id);

        // TEST 5: Filter by inStock
        var inStockResp = await publicClient.GetAsync($"/api/v1/products?inStock=true&q={suffix}");
        var inStockResult = await inStockResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        inStockResult!.Items.Should().Contain(p => p.Id == p1.Id);
        inStockResult.Items.Should().NotContain(p => p.Id == p2.Id);

        var outOfStockResp = await publicClient.GetAsync($"/api/v1/products?inStock=false&q={suffix}");
        var outOfStockResult = await outOfStockResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        outOfStockResult!.Items.Should().Contain(p => p.Id == p2.Id);
        outOfStockResult.Items.Should().NotContain(p => p.Id == p1.Id);

        // TEST 6: Sort by price_asc and price_desc
        var sortAscResp = await publicClient.GetAsync($"/api/v1/products?sort=price_asc&q={suffix}");
        var sortAscResult = await sortAscResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        var matchingAsc = sortAscResult!.Items.Where(p => p.Id == p1.Id || p.Id == p2.Id).ToList();
        matchingAsc.Should().HaveCount(2);
        matchingAsc[0].Id.Should().Be(p2.Id); // 300k before 750k
        matchingAsc[1].Id.Should().Be(p1.Id);

        var sortDescResp = await publicClient.GetAsync($"/api/v1/products?sort=price_desc&q={suffix}");
        var sortDescResult = await sortDescResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        var matchingDesc = sortDescResult!.Items.Where(p => p.Id == p1.Id || p.Id == p2.Id).ToList();
        matchingDesc.Should().HaveCount(2);
        matchingDesc[0].Id.Should().Be(p1.Id); // 750k before 300k
        matchingDesc[1].Id.Should().Be(p2.Id);

        // TEST 7: Sort by name
        var sortNameResp = await publicClient.GetAsync($"/api/v1/products?sort=name&q={suffix}");
        var sortNameResult = await sortNameResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        var matchingName = sortNameResult!.Items.Where(p => p.Id == p1.Id || p.Id == p2.Id).ToList();
        matchingName.Should().HaveCount(2);
        matchingName[0].Name.Should().StartWith("Alpha");
        matchingName[1].Name.Should().StartWith("Beta");

        // TEST 8: Pagination
        var pagedResp = await publicClient.GetAsync($"/api/v1/products?page=1&pageSize=1&q={suffix}");
        var pagedResult = await pagedResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        pagedResult!.Page.Should().Be(1);
        pagedResult.PageSize.Should().Be(1);
        pagedResult.Items.Should().HaveCount(1);
        pagedResult.TotalItems.Should().Be(2);
        pagedResult.TotalPages.Should().Be(2);

        // TEST 9: Validation error when minPrice > maxPrice
        var invalidPriceResp = await publicClient.GetAsync("/api/v1/products?minPrice=500000&maxPrice=100000");
        invalidPriceResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // TEST 10: Validation error when sort is invalid
        var invalidSortResp = await publicClient.GetAsync("/api/v1/products?sort=random_sort");
        invalidSortResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
