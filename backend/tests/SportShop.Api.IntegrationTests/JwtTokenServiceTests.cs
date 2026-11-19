using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Identity;
using SportShop.Infrastructure.Options;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class JwtTokenServiceTests
{
    private static readonly string[] CustomerRoles = ["Customer"];
    private static readonly string[] MultiRoles = ["Customer", "Admin"];

    private readonly JwtOptions _options = new()
    {
        Key = "super-secret-key-that-is-at-least-32-characters-long!",
        Issuer = "SportShopApi",
        Audience = "SportShopClient",
        ExpiryMinutes = 15,
        RefreshTokenExpiryDays = 7
    };

    private readonly Mock<IClock> _clockMock = new();

    private JwtTokenService CreateSut()
    {
        _clockMock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc));
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(_options);
        return new JwtTokenService(optionsWrapper, _clockMock.Object);
    }

    [Fact]
    public void GenerateAccessToken_ProducesValidJwt_WithExpectedClaimsAndExpiry()
    {
        // Arrange
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var email = "john.doe@example.com";
        var fullName = "John Doe";

        // Act
        var tokenString = sut.GenerateAccessToken(userId, email, fullName, MultiRoles);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        jwt.Issuer.Should().Be(_options.Issuer);
        jwt.Audiences.Should().Contain(_options.Audience);
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(userId.ToString());
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be(email);
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value.Should().Be(fullName);

        var roleClaims = jwt.Claims.Where(c => c.Type == "role" || c.Type == ClaimTypes.Role).Select(c => c.Value);
        roleClaims.Should().Contain("Customer");
        roleClaims.Should().Contain("Admin");

        jwt.ValidTo.Should().Be(new DateTime(2026, 11, 19, 12, 15, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GenerateRefreshToken_ProducesRandomToken_AndMatchingSha256Hash()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var (rawToken, tokenHash) = sut.GenerateRefreshToken();

        // Assert
        rawToken.Should().NotBeNullOrWhiteSpace();
        tokenHash.Should().NotBeNullOrWhiteSpace();
        tokenHash.Length.Should().Be(64);

        var computedHash = sut.HashToken(rawToken);
        computedHash.Should().Be(tokenHash);
    }

    [Fact]
    public void GetPrincipalFromToken_ReturnsPrincipal_ForValidToken()
    {
        // Arrange
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var tokenString = sut.GenerateAccessToken(userId, "test@example.com", "Test User", CustomerRoles);

        // Act
        var principal = sut.GetPrincipalFromToken(tokenString, validateLifetime: false);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(JwtRegisteredClaimNames.Sub)?.Value.Should().Be(userId.ToString());
    }

    [Fact]
    public void GetPrincipalFromToken_ReturnsNull_ForTamperedToken()
    {
        // Arrange
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var tokenString = sut.GenerateAccessToken(userId, "test@example.com", "Test User", CustomerRoles);
        var tamperedToken = tokenString[..^5] + "abcde";

        // Act
        var principal = sut.GetPrincipalFromToken(tamperedToken, validateLifetime: false);

        // Assert
        principal.Should().BeNull();
    }
}
