using FluentAssertions;
using Moq;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Auth.ConfirmEmail;
using SportShop.Application.Features.Auth.ForgotPassword;
using SportShop.Application.Features.Auth.Logout;
using SportShop.Application.Features.Auth.Refresh;
using SportShop.Application.Features.Auth.ResetPassword;
using Xunit;

namespace SportShop.Application.Tests;

public sealed class AuthAdditionalApplicationTests
{
    [Fact]
    public async Task RefreshToken_ShouldFail_WhenTokenIsEmpty()
    {
        var identityService = new Mock<IIdentityService>();
        var handler = new RefreshTokenCommandHandler(identityService.Object);

        var result = await handler.HandleAsync(new RefreshTokenCommand(""));
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.missing_refresh_token");
    }

    [Fact]
    public async Task RefreshToken_ShouldSucceed_WhenTokenValid()
    {
        var identityService = new Mock<IIdentityService>();
        var tokenResult = new AuthTokenResult("new-access", 900, "new-refresh", DateTime.UtcNow.AddDays(7));

        identityService.Setup(i => i.RefreshTokenAsync("valid-token", "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(tokenResult));

        var handler = new RefreshTokenCommandHandler(identityService.Object);
        var result = await handler.HandleAsync(new RefreshTokenCommand("valid-token"), "127.0.0.1");

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access");
    }

    [Fact]
    public async Task Logout_ShouldSucceed_WhenTokenEmptyOrValid()
    {
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(i => i.RevokeRefreshTokenAsync("token", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var handler = new LogoutCommandHandler(identityService.Object);

        var resEmpty = await handler.HandleAsync(new LogoutCommand(null));
        resEmpty.IsSuccess.Should().BeTrue();

        var resValid = await handler.HandleAsync(new LogoutCommand("token"));
        resValid.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPassword_ShouldSendEmail_WhenEmailValid()
    {
        var identityService = new Mock<IIdentityService>();
        var emailSender = new Mock<IEmailSender>();
        var validator = new ForgotPasswordCommandValidator();

        identityService.Setup(i => i.GeneratePasswordResetTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<string?>("reset-token-123"));

        var handler = new ForgotPasswordCommandHandler(identityService.Object, emailSender.Object, validator);
        var result = await handler.HandleAsync(new ForgotPasswordCommand("user@example.com"));

        result.IsSuccess.Should().BeTrue();
        emailSender.Verify(e => e.SendEmailAsync("user@example.com", It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_ShouldFail_WhenEmailInvalid()
    {
        var identityService = new Mock<IIdentityService>();
        var emailSender = new Mock<IEmailSender>();
        var validator = new ForgotPasswordCommandValidator();

        var handler = new ForgotPasswordCommandHandler(identityService.Object, emailSender.Object, validator);
        var result = await handler.HandleAsync(new ForgotPasswordCommand("invalid-email"));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task ResetPassword_ShouldSucceed_WhenInputsValid()
    {
        var identityService = new Mock<IIdentityService>();
        var validator = new ResetPasswordCommandValidator();

        identityService.Setup(i => i.ResetPasswordAsync("user@example.com", "token-123", "NewPassword123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var handler = new ResetPasswordCommandHandler(identityService.Object, validator);
        var result = await handler.HandleAsync(new ResetPasswordCommand("user@example.com", "token-123", "NewPassword123"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmail_ShouldSucceed_WhenInputsValid()
    {
        var identityService = new Mock<IIdentityService>();
        var validator = new ConfirmEmailCommandValidator();
        var userId = Guid.NewGuid();

        identityService.Setup(i => i.ConfirmEmailAsync(userId, "token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var handler = new ConfirmEmailCommandHandler(identityService.Object, validator);
        var result = await handler.HandleAsync(new ConfirmEmailCommand(userId, "token-123"));

        result.IsSuccess.Should().BeTrue();
    }
}
