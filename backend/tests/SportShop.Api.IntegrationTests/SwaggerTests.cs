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

    [Fact]
    public async Task SwaggerJson_AllEndpointsHaveSummariesAndDocumentedResponses()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        // Assert
        foreach (var pathProperty in paths.EnumerateObject())
        {
            var pathItem = pathProperty.Value;
            foreach (var operationProperty in pathItem.EnumerateObject())
            {
                var method = operationProperty.Name;
                if (method is not ("get" or "post" or "put" or "delete" or "patch"))
                {
                    continue;
                }

                var operation = operationProperty.Value;
                operation.TryGetProperty("summary", out var summary).Should().BeTrue($"Operation {method.ToUpperInvariant()} {pathProperty.Name} should have a summary");
                summary.GetString().Should().NotBeNullOrWhiteSpace($"Operation {method.ToUpperInvariant()} {pathProperty.Name} summary should not be empty");

                operation.TryGetProperty("responses", out var responses).Should().BeTrue($"Operation {method.ToUpperInvariant()} {pathProperty.Name} should define responses");
                responses.EnumerateObject().Should().NotBeEmpty($"Operation {method.ToUpperInvariant()} {pathProperty.Name} should have at least one documented response");
            }
        }
    }

    [Fact]
    public async Task SwaggerJson_CheckoutAndWebhookEndpoints_ContainExamples()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        // Checkout example verification
        paths.TryGetProperty("/api/v1/orders/checkout", out var checkoutPath).Should().BeTrue();
        var checkoutPost = checkoutPath.GetProperty("post");
        checkoutPost.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").TryGetProperty("example", out var checkoutReqExample).Should().BeTrue();
        checkoutReqExample.TryGetProperty("addressId", out _).Should().BeTrue();

        checkoutPost.GetProperty("responses").GetProperty("201").GetProperty("content").GetProperty("application/json").TryGetProperty("example", out var checkoutRespExample).Should().BeTrue();
        checkoutRespExample.TryGetProperty("orderNumber", out _).Should().BeTrue();
        checkoutRespExample.TryGetProperty("snapToken", out _).Should().BeTrue();

        // Webhook example verification
        paths.TryGetProperty("/api/v1/payments/midtrans/notification", out var webhookPath).Should().BeTrue();
        var webhookPost = webhookPath.GetProperty("post");
        webhookPost.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").TryGetProperty("example", out var webhookReqExample).Should().BeTrue();
        webhookReqExample.TryGetProperty("order_id", out _).Should().BeTrue();
        webhookReqExample.TryGetProperty("signature_key", out _).Should().BeTrue();

        webhookPost.GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").TryGetProperty("example", out var webhookRespExample).Should().BeTrue();
        webhookRespExample.TryGetProperty("status", out _).Should().BeTrue();
    }
}
