using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public sealed class SwaggerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SwaggerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SwaggerEndpoint_ReturnsSuccessStatusCode_AndHtmlContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/index.html");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("swagger-ui");
    }

    [Fact]
    public async Task SwaggerJson_ReturnsValidOpenApiSpecification_WithBearerAuthScheme()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("openapi").GetString().Should().StartWith("3.");
        root.GetProperty("info").GetProperty("version").GetString().Should().Be("v1");
        root.GetProperty("info").GetProperty("title").GetString().Should().Be("SportShop API");

        // Verify Bearer security scheme is defined
        var securitySchemes = root.GetProperty("components").GetProperty("securitySchemes");
        securitySchemes.TryGetProperty("Bearer", out var bearerScheme).Should().BeTrue();
        bearerScheme.GetProperty("type").GetString().Should().Be("http");
        bearerScheme.GetProperty("scheme").GetString().Should().BeEquivalentTo("bearer");
        bearerScheme.GetProperty("bearerFormat").GetString().Should().Be("JWT");

        // Verify security requirement is present
        root.TryGetProperty("security", out var securityArray).Should().BeTrue();
        securityArray.GetArrayLength().Should().BeGreaterThan(0);
        securityArray[0].TryGetProperty("Bearer", out _).Should().BeTrue();
    }
}
