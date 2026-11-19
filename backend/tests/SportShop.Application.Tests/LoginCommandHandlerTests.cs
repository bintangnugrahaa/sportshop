using FluentAssertions;
using Moq;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Auth.Login;
using Xunit;

namespace SportShop.Application.Tests;

public class LoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly LoginCommandValidator _validator = new();

    private LoginCommandHandler CreateSut()
    {
        return new LoginCommandHandler(_identityServiceMock.Object, _validator);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("not-an-email", "password")]
    [InlineData("test@example.com", "")]
    public async Task HandleAsync_ReturnsValidationError_WhenInputsAreInvalid(string email, string password)
    {
        // Arrange
        var sut = CreateSut();
        var command = new LoginCommand(email, password);

        // Act
        var result = await sut.HandleAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuccess_WhenCredentialsAreValid()
    {
        // Arrange
        var sut = CreateSut();
        var command = new LoginCommand("john@example.com", "Password123");
        var expectedResult = new AuthTokenResult("access_token", 900, "refresh_token", DateTime.UtcNow.AddDays(7));

        _identityServiceMock
            .Setup(x => x.LoginAsync(command.Email, command.Password, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedResult));

        // Act
        var result = await sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access_token");
        result.Value.ExpiresIn.Should().Be(900);
    }
}
