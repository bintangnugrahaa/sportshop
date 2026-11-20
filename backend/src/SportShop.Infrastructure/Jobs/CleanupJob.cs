using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Persistence;

namespace SportShop.Infrastructure.Jobs;

[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public sealed partial class CleanupJob : ICleanupJob
{
    private readonly AppDbContext _context;
    private readonly IClock _clock;
    private readonly ILogger<CleanupJob> _logger;

    public CleanupJob(
        AppDbContext context,
        IClock clock,
        ILogger<CleanupJob> logger)
    {
        _context = context;
        _clock = clock;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var tokenExpiryThreshold = now.AddDays(-30);
        var notificationExpiryThreshold = now.AddDays(-180);

        // Delete expired or revoked refresh tokens older than 30 days
        var deletedTokensCount = await _context.RefreshTokens
            .Where(t => t.ExpiresAt < tokenExpiryThreshold || (t.RevokedAt != null && t.RevokedAt < tokenExpiryThreshold))
            .ExecuteDeleteAsync(cancellationToken);

        // Delete payment notifications older than 180 days
        var deletedNotificationsCount = await _context.PaymentNotifications
            .Where(n => n.ReceivedAt < notificationExpiryThreshold)
            .ExecuteDeleteAsync(cancellationToken);

        LogCleanupCompleted(_logger, deletedTokensCount, deletedNotificationsCount);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Cleanup job completed successfully. Deleted {DeletedTokensCount} expired refresh tokens and {DeletedNotificationsCount} old payment notifications.")]
    private static partial void LogCleanupCompleted(ILogger logger, int deletedTokensCount, int deletedNotificationsCount);
}
