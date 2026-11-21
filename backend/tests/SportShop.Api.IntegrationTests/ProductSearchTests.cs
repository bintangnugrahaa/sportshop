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

public class ProductSearchTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductSearchTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.50.{Random.Shared.Next(1, 10000)}");
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
    public async Task Search_ShouldReturnRelevantResults_ForFullTextAndTrigramTypo()
    {
        // Arrange
        var adminClient = CreateTestClient();
        await AuthenticateAsAdminAsync(adminClient);
        var suffix = Guid.NewGuid().ToString()[..8];

        var catResp = await adminClient.PostAsJsonAsync("/api/v1/admin/categories", new CreateCategoryRequest($"Katalog Search-{suffix}", $"kat-search-{suffix}"));
        var cat = await catResp.Content.ReadFromJsonAsync<CategoryDto>();

        var brandResp = await adminClient.PostAsJsonAsync("/api/v1/admin/brands", new CreateBrandRequest($"Brand Search-{suffix}", $"brand-search-{suffix}"));
        var brand = await brandResp.Content.ReadFromJsonAsync<BrandDto>();

        // Product 1: "Sepatu Lari Super" with description "Sepatu lari marathon sangat nyaman"
        var p1Resp = await adminClient.PostAsJsonAsync("/api/v1/admin/products", new CreateProductRequest(
            cat!.Id, brand!.Id, $"Sepatu Lari Super {suffix}", $"sepatu-lari-super-{suffix}", "Sepatu lari marathon sangat nyaman", 850_000m));
        var p1 = await p1Resp.Content.ReadFromJsonAsync<ProductDto>();
        await adminClient.PostAsJsonAsync($"/api/v1/admin/products/{p1!.Id}/variants", new CreateVariantRequest($"SKU-RUN-{suffix}", "42", "Biru", 850_000m, 350, 10));

        // Product 2: "Raket Badminton Pro"
        var p2Resp = await adminClient.PostAsJsonAsync("/api/v1/admin/products", new CreateProductRequest(
            cat.Id, brand.Id, $"Raket Badminton Pro {suffix}", $"raket-badminton-{suffix}", "Raket bulutangkis karbon ringan", 1_200_000m));
        var p2 = await p2Resp.Content.ReadFromJsonAsync<ProductDto>();
        await adminClient.PostAsJsonAsync($"/api/v1/admin/products/{p2!.Id}/variants", new CreateVariantRequest($"SKU-RKT-{suffix}", "Standard", "Hitam", 1_200_000m, 150, 5));

        // Act & Assert with public client
        var publicClient = CreateTestClient();

        // 1. Full-text search for "sepatu lari"
        var ftsResp = await publicClient.GetAsync($"/api/v1/products?q=sepatu lari");
        ftsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var ftsResult = await ftsResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        ftsResult.Should().NotBeNull();
        ftsResult!.Items.Should().Contain(p => p.Id == p1.Id);
        ftsResult.Items.Should().NotContain(p => p.Id == p2.Id);

        // 2. Full-text search for word in description: "marathon"
        var descResp = await publicClient.GetAsync($"/api/v1/products?q=marathon");
        descResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var descResult = await descResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        descResult!.Items.Should().Contain(p => p.Id == p1.Id);
        descResult.Items.Should().NotContain(p => p.Id == p2.Id);

        // 3. Typo fuzzy search: "sepaatu" (trigram fallback should match "Sepatu Lari Super")
        var typoResp = await publicClient.GetAsync($"/api/v1/products?q=sepaatu");
        typoResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var typoResult = await typoResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        typoResult!.Items.Should().Contain(p => p.Id == p1.Id);
        typoResult.Items.Should().NotContain(p => p.Id == p2.Id);

        // 4. Typo fuzzy search: "badmintoon" (trigram fallback should match "Raket Badminton Pro")
        var typoResp2 = await publicClient.GetAsync($"/api/v1/products?q=badmintoon");
        typoResp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var typoResult2 = await typoResp2.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        typoResult2!.Items.Should().Contain(p => p.Id == p2.Id);
        typoResult2.Items.Should().NotContain(p => p.Id == p1.Id);

        // 5. Gibberish query
        var noneResp = await publicClient.GetAsync($"/api/v1/products?q=zzxxyyzz12345");
        noneResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var noneResult = await noneResp.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        noneResult!.Items.Should().BeEmpty();
    }
}
