using System.Net;
using FluentAssertions;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class JwtBearerAuthenticationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public JwtBearerAuthenticationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedEndpoint_Returns200_WhenValidTokenProvided()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateCustomerToken();
        client.WithBearerToken(token);

        // Act
        var response = await client.GetAsync("/api/v1/auth/test-protected");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_Returns401_WhenNoTokenProvided()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/auth/test-protected");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_Returns401_WhenExpiredTokenProvided()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateExpiredToken();
        client.WithBearerToken(token);

        // Act
        var response = await client.GetAsync("/api/v1/auth/test-protected");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_Returns401_WhenCorruptedTokenProvided()
    {
        // Arrange
        var client = _factory.CreateClient();
        var corruptedToken = "not.a.valid.jwt.token";
        client.WithBearerToken(corruptedToken);

        // Act
        var response = await client.GetAsync("/api/v1/auth/test-protected");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_Returns200_WhenAdminTokenProvided()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateAdminToken();
        client.WithBearerToken(token);

        // Act
        var response = await client.GetAsync("/api/v1/admin/test-admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminEndpoint_Returns403_WhenCustomerTokenProvided()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateCustomerToken();
        client.WithBearerToken(token);

        // Act
        var response = await client.GetAsync("/api/v1/admin/test-admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
