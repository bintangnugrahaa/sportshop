namespace SportShop.Application.Common.Interfaces;

public sealed record AuthTokenResult(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public sealed record UserProfileResult(
    Guid Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    IReadOnlyCollection<string> Roles,
    DateTime CreatedAt);

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

    Task<Result> RevokeRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<Result<UserProfileResult>> GetUserProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<UserProfileResult>> UpdateUserProfileAsync(
        Guid userId,
        string fullName,
        string? phoneNumber,
        CancellationToken cancellationToken = default);

    Task<Result> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}
