using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SportShop.Api.Models;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class SecurityAndRateLimitingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SecurityAndRateLimitingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Response_ShouldContainSecurityHeaders()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");

        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");

        response.Headers.Should().ContainKey("X-XSS-Protection");
        response.Headers.GetValues("X-XSS-Protection").First().Should().Be("1; mode=block");

        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.GetValues("Referrer-Policy").First().Should().Be("strict-origin-when-cross-origin");

        response.Headers.Should().ContainKey("X-Permitted-Cross-Domain-Policies");
        response.Headers.GetValues("X-Permitted-Cross-Domain-Policies").First().Should().Be("none");

        response.Headers.Should().ContainKey("Permissions-Policy");
    }

    [Fact]
    public async Task Cors_WithAllowedOrigin_ShouldReturnAllowOriginHeader()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/test-login");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.GetValues("Access-Control-Allow-Origin").First().Should().Be("http://localhost:5173");
        response.Headers.Should().ContainKey("Access-Control-Allow-Credentials");
        response.Headers.GetValues("Access-Control-Allow-Credentials").First().Should().Be("true");
    }

    [Fact]
    public async Task Cors_WithDisallowedOrigin_ShouldNotReturnAllowOriginHeader()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/test-login");
        request.Headers.Add("Origin", "http://untrusted-site.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.Headers.Should().NotContainKey("Access-Control-Allow-Origin");
    }

    [Fact]
    public async Task RateLimiting_AuthEndpoint_MoreThan10RequestsPerMinute_ShouldReturn429ProblemDetails()
    {
        // Arrange
        var client = _factory.CreateClient();
        var testIp = $"192.168.10.{Random.Shared.Next(1, 250)}";

        // Act: send 10 allowed requests with unique IP partition
        for (var i = 0; i < 10; i++)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/test-login");
            req.Headers.Add("X-Forwarded-For", testIp);
            var res = await client.SendAsync(req);
            res.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // 11th request from the same IP should be blocked
        var blockedReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/test-login");
        blockedReq.Headers.Add("X-Forwarded-For", testIp);
        var blockedRes = await client.SendAsync(blockedReq);

        // Assert
        blockedRes.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var problemDetails = await blockedRes.Content.ReadFromJsonAsync<SportShopProblemDetails>(_jsonOptions);

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(429);
        problemDetails.Code.Should().Be("rate_limit.exceeded");
        problemDetails.Detail.Should().Contain("Batas percobaan terlampaui");
        problemDetails.TraceId.Should().NotBeNullOrWhiteSpace();
    }
}
