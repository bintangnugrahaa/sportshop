using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SportShop.Api.Models;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Users.GetProfile;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class SecurityMatrixTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SecurityMatrixTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.40.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    [Theory]
    [InlineData("/api/v1/me", "GET")]
    [InlineData("/api/v1/me", "PUT")]
    [InlineData("/api/v1/me/change-password", "POST")]
    [InlineData("/api/v1/auth/logout", "POST")]
    [InlineData("/api/v1/admin/test-admin", "GET")]
    public async Task ProtectedEndpoints_Return401Unauthorized_WhenNoTokenIsProvided(string endpoint, string method)
    {
        // Arrange
        var client = CreateTestClient();
        var request = new HttpRequestMessage(new HttpMethod(method), endpoint);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/v1/me", "GET")]
    [InlineData("/api/v1/admin/test-admin", "GET")]
    public async Task ProtectedEndpoints_Return401Unauthorized_WhenTokenIsExpired(string endpoint, string method)
    {
        // Arrange
        var client = CreateTestClient();
        var expiredToken = AuthTestHelper.GenerateExpiredToken();
        client.WithBearerToken(expiredToken);
        var request = new HttpRequestMessage(new HttpMethod(method), endpoint);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/v1/me", "GET")]
    [InlineData("/api/v1/admin/test-admin", "GET")]
    public async Task ProtectedEndpoints_Return401Unauthorized_WhenTokenIsTampered(string endpoint, string method)
    {
        // Arrange
        var client = CreateTestClient();
        var validToken = AuthTestHelper.GenerateCustomerToken();
        var tamperedToken = validToken[..^6] + "xxxxxx";
        client.WithBearerToken(tamperedToken);
        var request = new HttpRequestMessage(new HttpMethod(method), endpoint);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoints_Return403Forbidden_WhenCustomerTokenIsProvided()
    {
        // Arrange
        var client = CreateTestClient();
        var customerToken = AuthTestHelper.GenerateCustomerToken();
        client.WithBearerToken(customerToken);

        // Act
        var response = await client.GetAsync("/api/v1/admin/test-admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoints_Return200OK_WhenAdminTokenIsProvided()
    {
        // Arrange
        var client = CreateTestClient();
        var adminToken = AuthTestHelper.GenerateAdminToken();
        client.WithBearerToken(adminToken);

        // Act
        var response = await client.GetAsync("/api/v1/admin/test-admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProfileEndpoint_EnforcesOwnership_UserOnlyAccessesOwnData()
    {
        // Arrange: User 1
        var client1 = CreateTestClient();
        var email1 = $"user1_{Guid.NewGuid():N}@sportshop.dev";
        await client1.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email1, "Password123!", "User One", null));
        var login1 = await client1.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email1, "Password123!"));
        var data1 = await login1.Content.ReadFromJsonAsync<LoginResponse>();
        client1.WithBearerToken(data1!.AccessToken);

        // Arrange: User 2
        var client2 = CreateTestClient();
        var email2 = $"user2_{Guid.NewGuid():N}@sportshop.dev";
        await client2.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email2, "Password123!", "User Two", null));
        var login2 = await client2.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email2, "Password123!"));
        var data2 = await login2.Content.ReadFromJsonAsync<LoginResponse>();
        client2.WithBearerToken(data2!.AccessToken);

        // Act: User 1 fetches /me
        var profile1 = await client1.GetFromJsonAsync<UserProfileResponse>("/api/v1/me");
        // Act: User 2 fetches /me
        var profile2 = await client2.GetFromJsonAsync<UserProfileResponse>("/api/v1/me");

        // Assert: Complete isolation of identity & resource ownership
        profile1!.Email.Should().Be(email1);
        profile1.FullName.Should().Be("User One");

        profile2!.Email.Should().Be(email2);
        profile2.FullName.Should().Be("User Two");

        profile1.Id.Should().NotBe(profile2.Id);
    }

    [Fact]
    public async Task ProfileEndpoint_Returns404NotFound_WhenUserDoesNotExistInDatabase()
    {
        // Arrange: Generate valid JWT token for a random Non-Existent User ID
        var client = CreateTestClient();
        var nonExistentUserId = Guid.NewGuid();
        var token = AuthTestHelper.GenerateCustomerToken(userId: nonExistentUserId, email: "ghost@sportshop.dev");
        client.WithBearerToken(token);

        // Act
        var response = await client.GetAsync("/api/v1/me");

        // Assert: 404 Not Found as per AGENTS.md rule
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(404);
        problem.Code.Should().Be("user.not_found");
    }
}
