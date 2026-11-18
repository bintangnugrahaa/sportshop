using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class TestcontainersHealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TestcontainersHealthCheckTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthReady_WithTestcontainers_ShouldReturnHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonDocument>(_jsonOptions);
        content.Should().NotBeNull();

        var root = content!.RootElement;
        root.GetProperty("status").GetString().Should().Be("Healthy");

        var entries = root.GetProperty("entries");
        entries.GetProperty("postgres").GetProperty("status").GetString().Should().Be("Healthy");
        entries.GetProperty("redis").GetProperty("status").GetString().Should().Be("Healthy");
        entries.GetProperty("hangfire").GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public void AuthTestHelper_ShouldGenerateValidCustomerAndAdminTokens()
    {
        // Customer token
        var customerId = Guid.NewGuid();
        var customerToken = AuthTestHelper.GenerateCustomerToken(customerId, "customer@sportshop.dev");

        var handler = new JwtSecurityTokenHandler();
        var jwtCustomer = handler.ReadJwtToken(customerToken);

        jwtCustomer.Issuer.Should().Be(AuthTestHelper.DefaultIssuer);
        jwtCustomer.Audiences.Should().Contain(AuthTestHelper.DefaultAudience);
        jwtCustomer.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(customerId.ToString());
        jwtCustomer.Claims.First(c => c.Type == "role" || c.Type == ClaimTypes.Role).Value.Should().Be("Customer");

        // Admin token
        var adminId = Guid.NewGuid();
        var adminToken = AuthTestHelper.GenerateAdminToken(adminId, "admin@sportshop.dev");
        var jwtAdmin = handler.ReadJwtToken(adminToken);

        jwtAdmin.Claims.First(c => c.Type == "role" || c.Type == ClaimTypes.Role).Value.Should().Be("Admin");

        // Expired token
        var expiredToken = AuthTestHelper.GenerateExpiredToken();
        var jwtExpired = handler.ReadJwtToken(expiredToken);
        jwtExpired.ValidTo.Should().BeBefore(DateTime.UtcNow);
    }
}
