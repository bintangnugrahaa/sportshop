using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Api.Models;
using SportShop.Application.Features.Auth.ConfirmEmail;
using SportShop.Application.Features.Auth.ForgotPassword;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Auth.ResetPassword;
using SportShop.Infrastructure.Identity;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class PasswordResetAndEmailConfirmationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PasswordResetAndEmailConfirmationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.30.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    [Fact]
    public async Task ForgotPassword_AlwaysReturns200_EvenForNonExistentEmail()
    {
        // Arrange
        var client = CreateTestClient();
        var command = new ForgotPasswordCommand("does-not-exist@sportshop.dev");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_SucceedsWithValidToken_AndAllowsLoginWithNewPassword()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"pwd_reset_{Guid.NewGuid():N}@sportshop.dev";
        var oldPassword = "OldPassword123!";
        var newPassword = "BrandNewPassword123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, oldPassword, "Reset Tester", null));

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            user.Should().NotBeNull();
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user!);

            // Act: reset password with valid token
            var resetCommand = new ResetPasswordCommand(email, resetToken, newPassword);
            var resetResponse = await client.PostAsJsonAsync("/api/v1/auth/reset-password", resetCommand);

            // Assert
            resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Verify login with new password succeeds
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, newPassword));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_Fails_WhenTokenIsReused_OneTimeUse()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"reuse_reset_{Guid.NewGuid():N}@sportshop.dev";
        var oldPassword = "OldPassword123!";
        var newPassword = "BrandNewPassword123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, oldPassword, "Reuse Reset Tester", null));

        string resetToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            resetToken = await userManager.GeneratePasswordResetTokenAsync(user!);
        }

        var resetCommand = new ResetPasswordCommand(email, resetToken, newPassword);

        // 1st reset: succeeds
        var firstResponse = await client.PostAsJsonAsync("/api/v1/auth/reset-password", resetCommand);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: 2nd reset with same token (TOKEN REUSE)
        var secondResetCommand = new ResetPasswordCommand(email, resetToken, "AnotherPassword123!");
        var secondResponse = await client.PostAsJsonAsync("/api/v1/auth/reset-password", secondResetCommand);

        // Assert: One-time token used fails!
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await secondResponse.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("auth.invalid_reset_token");
    }

    [Fact]
    public async Task ConfirmEmail_Succeeds_AndIsOneTimeUse()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"confirm_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        var regRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Confirm Tester", null));
        var regData = await regRes.Content.ReadFromJsonAsync<RegisterResponse>();

        string confirmToken;
        Guid userId = regData!.Id;

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId.ToString());
            user.Should().NotBeNull();
            user!.EmailConfirmed.Should().BeFalse();

            confirmToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        }

        // Act: Confirm email
        var confirmCommand = new ConfirmEmailCommand(userId, confirmToken);
        var confirmResponse = await client.PostAsJsonAsync("/api/v1/auth/confirm-email", confirmCommand);

        // Assert
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var updatedUser = await userManager.FindByIdAsync(userId.ToString());
            updatedUser!.EmailConfirmed.Should().BeTrue();
        }

        // 2nd confirmation attempt with the same token fails
        var secondConfirmResponse = await client.PostAsJsonAsync("/api/v1/auth/confirm-email", confirmCommand);
        secondConfirmResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
