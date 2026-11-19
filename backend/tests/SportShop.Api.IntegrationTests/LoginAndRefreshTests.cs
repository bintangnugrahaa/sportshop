using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Refresh;
using SportShop.Application.Features.Auth.Register;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class LoginAndRefreshTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LoginAndRefreshTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.10.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var match = Regex.Match(string.Join(";", cookies), $@"{cookieName}=([^;]+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        return string.Empty;
    }

    [Fact]
    public async Task Login_Returns200_WithAccessTokenAndSetsHttpOnlyCookie_WhenCredentialsAreValid()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"login_user_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        var registerRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Login Tester", null));
        registerRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.ExpiresIn.Should().Be(900); // 15 mins = 900 seconds

        var refreshTokenCookie = ExtractCookieValue(loginResponse, AuthController.RefreshTokenCookieName);
        refreshTokenCookie.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_Returns401_WhenPasswordIsIncorrect()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"wrong_pass_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Wrong Pass Tester", null));

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, "WrongPassword123!"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("auth.invalid_credentials");
    }

    [Fact]
    public async Task Login_LocksOutAccount_After5FailedAttempts()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"lockout_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Lockout Tester", null));

        // Act: 5 consecutive failed login attempts
        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, "WrongPassword!"));
        }

        // 6th attempt: should be locked out
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, "WrongPassword!"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("auth.locked_out");
    }

    [Fact]
    public async Task Refresh_ReturnsNewAccessToken_AndRotatesRefreshToken_WhenValidTokenInCookie()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"refresh_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Refresh Tester", null));
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        var initialRefreshToken = ExtractCookieValue(loginResponse, AuthController.RefreshTokenCookieName);

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"{AuthController.RefreshTokenCookieName}={initialRefreshToken}");

        // Act
        var refreshResponse = await client.SendAsync(refreshRequest);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();

        var newRefreshToken = ExtractCookieValue(refreshResponse, AuthController.RefreshTokenCookieName);
        newRefreshToken.Should().NotBeNullOrWhiteSpace();
        newRefreshToken.Should().NotBe(initialRefreshToken); // Rotated!
    }

    [Fact]
    public async Task Refresh_RevokesEntireFamily_WhenRevokedTokenIsReused_ReuseDetection()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"reuse_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Reuse Tester", null));

        // 1. Login to get token T1
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        var tokenT1 = ExtractCookieValue(loginResponse, AuthController.RefreshTokenCookieName);

        // 2. Refresh with T1 to get token T2 (T1 is now rotated)
        var refreshReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq1.Headers.Add("Cookie", $"{AuthController.RefreshTokenCookieName}={tokenT1}");
        var refreshRes1 = await client.SendAsync(refreshReq1);
        refreshRes1.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenT2 = ExtractCookieValue(refreshRes1, AuthController.RefreshTokenCookieName);

        // 3. Attempt to REUSE T1! (T1 was already rotated)
        var reuseReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        reuseReq.Headers.Add("Cookie", $"{AuthController.RefreshTokenCookieName}={tokenT1}");
        var reuseRes = await client.SendAsync(reuseReq);

        // Assert: Reuse detection triggered!
        reuseRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await reuseRes.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("auth.token_reuse_detected");

        // 4. Verify that token T2 (in the same family) is ALSO now revoked and fails refresh!
        var reqWithT2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        reqWithT2.Headers.Add("Cookie", $"{AuthController.RefreshTokenCookieName}={tokenT2}");
        var resWithT2 = await client.SendAsync(reqWithT2);

        resWithT2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
