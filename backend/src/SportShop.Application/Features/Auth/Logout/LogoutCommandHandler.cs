using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Auth.Logout;

public sealed record LogoutCommand(string? RefreshToken);

public sealed class LogoutCommandHandler
{
    private readonly IIdentityService _identityService;

    public LogoutCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> HandleAsync(
        LogoutCommand command,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Result.Success(); // Idempotent logout
        }

        return await _identityService.RevokeRefreshTokenAsync(command.RefreshToken, ipAddress, cancellationToken);
    }
}
