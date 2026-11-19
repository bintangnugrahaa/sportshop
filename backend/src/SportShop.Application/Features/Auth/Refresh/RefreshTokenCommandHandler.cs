using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Auth.Refresh;

public sealed class RefreshTokenCommandHandler
{
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<AuthTokenResult>> HandleAsync(
        RefreshTokenCommand command,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Result.Failure<AuthTokenResult>(Error.Unauthorized(
                "auth.missing_refresh_token",
                "Refresh token tidak ditemukan."));
        }

        return await _identityService.RefreshTokenAsync(command.RefreshToken, ipAddress, cancellationToken);
    }
}
