using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Features.Catalog.DTOs;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class ProductImageUploadTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductImageUploadTests(CustomWebApplicationFactory factory)
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

    private static async Task<ProductDto> CreateTestProductAsync(HttpClient client, string suffix)
    {
        var catResp = await client.PostAsJsonAsync("/api/v1/admin/categories", new CreateCategoryRequest($"CatImg-{suffix}", $"cat-img-{suffix}"));
        var cat = await catResp.Content.ReadFromJsonAsync<CategoryDto>();

        var brandResp = await client.PostAsJsonAsync("/api/v1/admin/brands", new CreateBrandRequest($"BrandImg-{suffix}", $"brand-img-{suffix}"));
        var brand = await brandResp.Content.ReadFromJsonAsync<BrandDto>();

        var prodResp = await client.PostAsJsonAsync("/api/v1/admin/products", new CreateProductRequest(
            cat!.Id, brand!.Id, $"Produk Img-{suffix}", $"produk-img-{suffix}", "Deskripsi"));
        return (await prodResp.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    [Fact]
    public async Task UploadImage_ShouldReject_WhenFileSizeExceeds2MB()
    {
        // Arrange
        var client = CreateTestClient();
        client.WithBearerToken(AuthTestHelper.GenerateAdminToken());
        var suffix = Guid.NewGuid().ToString()[..8];
        var product = await CreateTestProductAsync(client, suffix);

        // 2 MB + 100 KB
        var largeContent = new byte[(2 * 1024 * 1024) + (100 * 1024)];
        Random.Shared.NextBytes(largeContent);

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(largeContent);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(fileContent, "file", "large.jpg");

        // Act
        var response = await client.PostAsync($"/api/v1/admin/products/{product.Id}/images", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem!.Code.Should().Be("file.too_large");
    }

    [Fact]
    public async Task UploadImage_ShouldReject_WhenMimeTypeIsInvalid()
    {
        // Arrange
        var client = CreateTestClient();
        client.WithBearerToken(AuthTestHelper.GenerateAdminToken());
        var suffix = Guid.NewGuid().ToString()[..8];
        var product = await CreateTestProductAsync(client, suffix);

        var fileBytes = "fake pdf content"u8.ToArray();
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "document.pdf");

        // Act
        var response = await client.PostAsync($"/api/v1/admin/products/{product.Id}/images", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem!.Code.Should().Be("file.invalid_type");
    }

    [Fact]
    public async Task UploadImage_ShouldSucceed_AndAssignGuidNameAndManagePrimary()
    {
        // Arrange
        var client = CreateTestClient();
        client.WithBearerToken(AuthTestHelper.GenerateAdminToken());
        var suffix = Guid.NewGuid().ToString()[..8];
        var product = await CreateTestProductAsync(client, suffix);

        // 1. Upload first image (valid jpeg)
        var img1Bytes = new byte[1024];
        Random.Shared.NextBytes(img1Bytes);
        using var form1 = new MultipartFormDataContent();
        var fileContent1 = new ByteArrayContent(img1Bytes);
        fileContent1.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form1.Add(fileContent1, "file", "photo1.jpg");
        form1.Add(new StringContent("Foto 1"), "altText");

        var resp1 = await client.PostAsync($"/api/v1/admin/products/{product.Id}/images", form1);
        resp1.StatusCode.Should().Be(HttpStatusCode.Created);
        var img1 = await resp1.Content.ReadFromJsonAsync<ProductImageDto>();
        img1.Should().NotBeNull();
        img1!.Url.Should().StartWith("/uploads/products/");
        img1.Url.Should().EndWith(".jpg");
        img1.IsPrimary.Should().BeTrue(); // First image is automatically primary

        // 2. Upload second image with isPrimary = true (should become primary, demoting first image)
        var img2Bytes = new byte[1024];
        Random.Shared.NextBytes(img2Bytes);
        using var form2 = new MultipartFormDataContent();
        var fileContent2 = new ByteArrayContent(img2Bytes);
        fileContent2.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form2.Add(fileContent2, "file", "photo2.png");
        form2.Add(new StringContent("true"), "isPrimary");

        var resp2 = await client.PostAsync($"/api/v1/admin/products/{product.Id}/images", form2);
        resp2.StatusCode.Should().Be(HttpStatusCode.Created);
        var img2 = await resp2.Content.ReadFromJsonAsync<ProductImageDto>();
        img2!.IsPrimary.Should().BeTrue();

        // Verify product now has both images and img2 is primary
        var productDetail = await (await client.GetAsync($"/api/v1/admin/products/{product.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        productDetail!.Images.Should().HaveCount(2);
        productDetail.Images!.First(i => i.Id == img2.Id).IsPrimary.Should().BeTrue();
        productDetail.Images!.First(i => i.Id == img1.Id).IsPrimary.Should().BeFalse();

        // 3. Delete second image
        var deleteResp = await client.DeleteAsync($"/api/v1/admin/products/{product.Id}/images/{img2.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify remaining image is promoted to primary
        var productAfterDelete = await (await client.GetAsync($"/api/v1/admin/products/{product.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        productAfterDelete!.Images.Should().HaveCount(1);
        productAfterDelete.Images![0].Id.Should().Be(img1.Id);
        productAfterDelete.Images![0].IsPrimary.Should().BeTrue();
    }
}
