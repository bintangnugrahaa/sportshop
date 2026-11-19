namespace SportShop.Application.Common.Interfaces;

public sealed record AuthTokenResult(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public interface IIdentityService
{
    Task<Result<Guid>> RegisterUserAsync(
        string email,
        string password,
        string fullName,
        string? phoneNumber,
        CancellationToken cancellationToken = default);

    Task<Result<AuthTokenResult>> LoginAsync(
        string email,
        string password,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<Result<AuthTokenResult>> RefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
