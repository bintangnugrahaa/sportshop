using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Features.Catalog.DTOs;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class AdminCategoriesAndBrandsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminCategoriesAndBrandsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.45.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    [Fact]
    public async Task AdminEndpoints_ShouldReturn401Unauthorized_WhenAnonymous()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var catResponse = await client.GetAsync("/api/v1/admin/categories");
        var brandResponse = await client.GetAsync("/api/v1/admin/brands");

        // Assert
        catResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        brandResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoints_ShouldReturn403Forbidden_WhenCustomerRole()
    {
        // Arrange
        var client = CreateTestClient();
        client.WithBearerToken(AuthTestHelper.GenerateCustomerToken());

        // Act
        var catResponse = await client.GetAsync("/api/v1/admin/categories");
        var brandResponse = await client.GetAsync("/api/v1/admin/brands");

        // Assert
        catResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        brandResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Categories_FullCrud_ShouldSucceed_AndEnforceMaxDepthAndUniqueSlug()
    {
        // Arrange
        var client = CreateTestClient();
        client.WithBearerToken(AuthTestHelper.GenerateAdminToken());
        var uniqueSuffix = Guid.NewGuid().ToString()[..8];

        // 1. Create root category
        var rootRequest = new CreateCategoryRequest($"Sepatu-{uniqueSuffix}", $"sepatu-{uniqueSuffix}", null, 1);
        var createRootResp = await client.PostAsJsonAsync("/api/v1/admin/categories", rootRequest);
        createRootResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var rootCategory = await createRootResp.Content.ReadFromJsonAsync<CategoryDto>();
        rootCategory.Should().NotBeNull();
        rootCategory!.Slug.Should().Be($"sepatu-{uniqueSuffix}");

        // 2. Duplicate slug should return 409 Conflict
        var dupSlugResp = await client.PostAsJsonAsync("/api/v1/admin/categories", rootRequest);
        dupSlugResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var conflictDetails = await dupSlugResp.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        conflictDetails!.Code.Should().Be("category.slug_duplicate");

        // 3. Create level 2 child category
        var childRequest = new CreateCategoryRequest($"Sepatu Lari-{uniqueSuffix}", $"sepatu-lari-{uniqueSuffix}", rootCategory.Id, 2);
        var createChildResp = await client.PostAsJsonAsync("/api/v1/admin/categories", childRequest);
        createChildResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var childCategory = await createChildResp.Content.ReadFromJsonAsync<CategoryDto>();
        childCategory.Should().NotBeNull();
        childCategory!.ParentId.Should().Be(rootCategory.Id);

        // 4. Create level 3 grandchild category (should fail max 2 levels)
        var grandChildRequest = new CreateCategoryRequest($"Sepatu Trail-{uniqueSuffix}", $"sepatu-trail-{uniqueSuffix}", childCategory.Id, 3);
        var createGrandChildResp = await client.PostAsJsonAsync("/api/v1/admin/categories", grandChildRequest);
        createGrandChildResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var depthProblem = await createGrandChildResp.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        depthProblem!.Code.Should().Be("category.max_depth");

        // 5. Update root category
        var updateRequest = new UpdateCategoryRequest($"Sepatu Olahraga-{uniqueSuffix}", $"sepatu-olahraga-{uniqueSuffix}", null, 5, true);
        var updateResp = await client.PutAsJsonAsync($"/api/v1/admin/categories/{rootCategory.Id}", updateRequest);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedCategory = await updateResp.Content.ReadFromJsonAsync<CategoryDto>();
        updatedCategory!.Name.Should().Be($"Sepatu Olahraga-{uniqueSuffix}");

        // 6. Get by id
        var getResp = await client.GetAsync($"/api/v1/admin/categories/{rootCategory.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7. Soft delete
        var deleteResp = await client.DeleteAsync($"/api/v1/admin/categories/{rootCategory.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyDeletedResp = await client.GetAsync($"/api/v1/admin/categories/{rootCategory.Id}");
        var verifyCategory = await verifyDeletedResp.Content.ReadFromJsonAsync<CategoryDto>();
        verifyCategory!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Brands_FullCrud_ShouldSucceed_AndEnforceUniqueConstraints()
    {
        // Arrange
        var client = CreateTestClient();
        client.WithBearerToken(AuthTestHelper.GenerateAdminToken());
        var uniqueSuffix = Guid.NewGuid().ToString()[..8];

        // 1. Create brand
        var brandRequest = new CreateBrandRequest($"Nike-{uniqueSuffix}", $"nike-{uniqueSuffix}", "https://example.com/logo.png");
        var createResp = await client.PostAsJsonAsync("/api/v1/admin/brands", brandRequest);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var brand = await createResp.Content.ReadFromJsonAsync<BrandDto>();
        brand.Should().NotBeNull();
        brand!.Name.Should().Be($"Nike-{uniqueSuffix}");

        // 2. Duplicate name should return 409
        var dupNameRequest = new CreateBrandRequest($"Nike-{uniqueSuffix}", $"other-slug-{uniqueSuffix}");
        var dupNameResp = await client.PostAsJsonAsync("/api/v1/admin/brands", dupNameRequest);
        dupNameResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var dupNameDetails = await dupNameResp.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        dupNameDetails!.Code.Should().Be("brand.name_duplicate");

        // 3. Duplicate slug should return 409
        var dupSlugRequest = new CreateBrandRequest($"Other Name-{uniqueSuffix}", $"nike-{uniqueSuffix}");
        var dupSlugResp = await client.PostAsJsonAsync("/api/v1/admin/brands", dupSlugRequest);
        dupSlugResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var dupSlugDetails = await dupSlugResp.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        dupSlugDetails!.Code.Should().Be("brand.slug_duplicate");

        // 4. Update brand
        var updateRequest = new UpdateBrandRequest($"Nike Global-{uniqueSuffix}", $"nike-global-{uniqueSuffix}", "https://example.com/new-logo.png", true);
        var updateResp = await client.PutAsJsonAsync($"/api/v1/admin/brands/{brand.Id}", updateRequest);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedBrand = await updateResp.Content.ReadFromJsonAsync<BrandDto>();
        updatedBrand!.Name.Should().Be($"Nike Global-{uniqueSuffix}");

        // 5. Get by ID
        var getResp = await client.GetAsync($"/api/v1/admin/brands/{brand.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Delete (soft delete)
        var deleteResp = await client.DeleteAsync($"/api/v1/admin/brands/{brand.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyDeletedResp = await client.GetAsync($"/api/v1/admin/brands/{brand.Id}");
        var verifyBrand = await verifyDeletedResp.Content.ReadFromJsonAsync<BrandDto>();
        verifyBrand!.IsActive.Should().BeFalse();
    }
}
