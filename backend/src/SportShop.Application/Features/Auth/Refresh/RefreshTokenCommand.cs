namespace SportShop.Application.Features.Auth.Refresh;

public sealed record RefreshTokenCommand(string? RefreshToken);

public sealed record RefreshTokenResponse(string AccessToken, int ExpiresIn);
