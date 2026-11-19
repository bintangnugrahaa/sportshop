using FluentAssertions;
using Moq;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Auth.Register;
using Xunit;

namespace SportShop.Application.Tests;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly RegisterCommandValidator _validator = new();

    private RegisterCommandHandler CreateSut()
    {
        return new RegisterCommandHandler(_identityServiceMock.Object, _validator);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task HandleAsync_ReturnsValidationError_WhenEmailIsInvalid(string invalidEmail)
    {
        // Arrange
        var sut = CreateSut();
        var command = new RegisterCommand(invalidEmail, "Password123", "John Doe", "08123456789");

        // Act
        var result = await sut.HandleAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.ValidationErrors.Should().ContainKey(nameof(RegisterCommand.Email));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsInPassword")]
    public async Task HandleAsync_ReturnsValidationError_WhenPasswordDoesNotMeetRequirements(string invalidPassword)
    {
        // Arrange
        var sut = CreateSut();
        var command = new RegisterCommand("john@example.com", invalidPassword, "John Doe", "08123456789");

        // Act
        var result = await sut.HandleAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.ValidationErrors.Should().ContainKey(nameof(RegisterCommand.Password));
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenEmailAlreadyExists()
    {
        // Arrange
        var sut = CreateSut();
        var command = new RegisterCommand("existing@example.com", "Password123", "John Doe", null);

        _identityServiceMock
            .Setup(x => x.RegisterUserAsync(command.Email, command.Password, command.FullName, command.PhoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(Error.Conflict("auth.email_already_exists", "Email sudah terdaftar.")));

        // Act
        var result = await sut.HandleAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("auth.email_already_exists");
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuccess_WhenCommandIsValid()
    {
        // Arrange
        var sut = CreateSut();
        var command = new RegisterCommand("newuser@example.com", "Password123", "New User", "08123456789");
        var newUserId = Guid.NewGuid();

        _identityServiceMock
            .Setup(x => x.RegisterUserAsync(command.Email, command.Password, command.FullName, command.PhoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(newUserId));

        // Act
        var result = await sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(newUserId);
        result.Value.Email.Should().Be(command.Email);
        result.Value.FullName.Should().Be(command.FullName);
    }
}
