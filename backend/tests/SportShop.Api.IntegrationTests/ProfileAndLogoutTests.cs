using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SportShop.Api.Controllers;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Users.GetProfile;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class ProfileAndLogoutTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProfileAndLogoutTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.20.{Random.Shared.Next(1, 10000)}");
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
    public async Task GetProfile_Returns200_WithUserProfile_WhenAuthenticated()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"profile_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Profile Tester", "081299998888"));
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        client.WithBearerToken(loginData!.AccessToken);

        // Act
        var response = await client.GetAsync("/api/v1/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be(email);
        profile.FullName.Should().Be("Profile Tester");
        profile.PhoneNumber.Should().Be("081299998888");
        profile.Roles.Should().Contain("Customer");
    }

    [Fact]
    public async Task GetProfile_Returns401_WhenNotAuthenticated()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var response = await client.GetAsync("/api/v1/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_Returns200_WithUpdatedData()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"update_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Original Name", null));
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        client.WithBearerToken(loginData!.AccessToken);

        var updateRequest = new MeController.UpdateProfileRequest("Updated Name", "081122334455");

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedProfile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        updatedProfile.Should().NotBeNull();
        updatedProfile!.FullName.Should().Be("Updated Name");
        updatedProfile.PhoneNumber.Should().Be("081122334455");
    }

    [Fact]
    public async Task ChangePassword_Succeeds_AndAllowsLoginWithNewPassword()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"change_pass_{Guid.NewGuid():N}@sportshop.dev";
        var oldPassword = "OldPassword123!";
        var newPassword = "NewPassword123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, oldPassword, "Pass Tester", null));
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, oldPassword));
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        client.WithBearerToken(loginData!.AccessToken);

        var changePassRequest = new MeController.ChangePasswordRequest(oldPassword, newPassword);

        // Act: change password
        var changeResponse = await client.PostAsJsonAsync("/api/v1/me/change-password", changePassRequest);
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: login with old password fails
        var failLoginClient = CreateTestClient();
        var oldLoginRes = await failLoginClient.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, oldPassword));
        oldLoginRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Assert: login with new password succeeds
        var newLoginRes = await failLoginClient.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, newPassword));
        newLoginRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken_AndClearsCookie()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"logout_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Logout Tester", null));
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        var refreshToken = ExtractCookieValue(loginRes, AuthController.RefreshTokenCookieName);

        client.WithBearerToken(loginData!.AccessToken);
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutRequest.Headers.Add("Cookie", $"{AuthController.RefreshTokenCookieName}={refreshToken}");

        // Act: logout
        var logoutResponse = await client.SendAsync(logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: refresh token is revoked; refresh request now fails with 401
        var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Headers.Add("Cookie", $"{AuthController.RefreshTokenCookieName}={refreshToken}");
        var refreshRes = await client.SendAsync(refreshReq);

        refreshRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
