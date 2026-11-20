using FluentAssertions;
using Moq;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Users.ChangePassword;
using SportShop.Application.Features.Users.GetProfile;
using SportShop.Application.Features.Users.UpdateProfile;
using Xunit;

namespace SportShop.Application.Tests;

public sealed class UserApplicationTests
{
    private static readonly string[] _roles = ["Customer"];

    [Fact]
    public async Task GetProfile_ShouldReturnProfile_WhenUserExists()
    {
        var identityService = new Mock<IIdentityService>();
        var userId = Guid.NewGuid();
        var profile = new UserProfileResult(userId, "user@test.com", "Test User", "0812345678", _roles, DateTime.UtcNow);

        identityService.Setup(i => i.GetUserProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(profile));

        var handler = new GetProfileQueryHandler(identityService.Object);
        var result = await handler.HandleAsync(new GetProfileQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(userId);
        result.Value.Email.Should().Be("user@test.com");
        result.Value.FullName.Should().Be("Test User");
    }

    [Fact]
    public async Task GetProfile_ShouldReturnFailure_WhenUserNotFound()
    {
        var identityService = new Mock<IIdentityService>();
        var userId = Guid.NewGuid();

        identityService.Setup(i => i.GetUserProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserProfileResult>(Error.NotFound("user.not_found", "Pengguna tidak ditemukan.")));

        var handler = new GetProfileQueryHandler(identityService.Object);
        var result = await handler.HandleAsync(new GetProfileQuery(userId));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.not_found");
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturnValidationError_WhenInputInvalid()
    {
        var identityService = new Mock<IIdentityService>();
        var validator = new UpdateProfileCommandValidator();
        var handler = new UpdateProfileCommandHandler(identityService.Object, validator);

        var result = await handler.HandleAsync(new UpdateProfileCommand(Guid.NewGuid(), "", ""));
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturnSuccess_WhenInputValid()
    {
        var identityService = new Mock<IIdentityService>();
        var validator = new UpdateProfileCommandValidator();
        var userId = Guid.NewGuid();
        var updatedProfile = new UserProfileResult(userId, "user@test.com", "New Name", "0812345678", _roles, DateTime.UtcNow);

        identityService.Setup(i => i.UpdateUserProfileAsync(userId, "New Name", "0812345678", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(updatedProfile));

        var handler = new UpdateProfileCommandHandler(identityService.Object, validator);
        var result = await handler.HandleAsync(new UpdateProfileCommand(userId, "New Name", "0812345678"));

        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("New Name");
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnValidationError_WhenPasswordWeak()
    {
        var identityService = new Mock<IIdentityService>();
        var validator = new ChangePasswordCommandValidator();
        var handler = new ChangePasswordCommandHandler(identityService.Object, validator);

        var result = await handler.HandleAsync(new ChangePasswordCommand(Guid.NewGuid(), "OldPass123", "weak"));
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnSuccess_WhenPasswordValid()
    {
        var identityService = new Mock<IIdentityService>();
        var validator = new ChangePasswordCommandValidator();
        var userId = Guid.NewGuid();

        identityService.Setup(i => i.ChangePasswordAsync(userId, "OldPass123", "NewPass123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var handler = new ChangePasswordCommandHandler(identityService.Object, validator);
        var result = await handler.HandleAsync(new ChangePasswordCommand(userId, "OldPass123", "NewPass123"));

        result.IsSuccess.Should().BeTrue();
    }
}
