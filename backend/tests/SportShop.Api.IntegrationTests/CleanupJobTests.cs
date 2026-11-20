using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Payments;
using SportShop.Infrastructure.Identity;
using SportShop.Infrastructure.Persistence;
using Xunit;

namespace SportShop.Api.IntegrationTests;

[Collection("IntegrationTests")]
public class CleanupJobTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CleanupJobTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CleanupJob_DeletesExpiredTokensAndOldNotifications_RetainingActiveData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cleanupJob = scope.ServiceProvider.GetRequiredService<ICleanupJob>();

        var user = await db.Users.FirstAsync(u => u.Email == "admin@sportshop.dev");

        var now = DateTime.UtcNow;

        // 1. RefreshToken expired 40 days ago -> should be deleted
        var tokenExpiredOld = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = $"hash_exp_old_{Guid.NewGuid():N}",
            FamilyId = Guid.NewGuid(),
            ExpiresAt = now.AddDays(-40),
            CreatedAt = now.AddDays(-47),
            CreatedByIp = "127.0.0.1"
        };

        // 2. RefreshToken revoked 35 days ago -> should be deleted
        var tokenRevokedOld = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = $"hash_rev_old_{Guid.NewGuid():N}",
            FamilyId = Guid.NewGuid(),
            ExpiresAt = now.AddDays(-10),
            CreatedAt = now.AddDays(-36),
            RevokedAt = now.AddDays(-35),
            RevokeReason = "Logout",
            CreatedByIp = "127.0.0.1"
        };

        // 3. RefreshToken valid -> should NOT be deleted
        var tokenValid = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = $"hash_valid_{Guid.NewGuid():N}",
            FamilyId = Guid.NewGuid(),
            ExpiresAt = now.AddDays(5),
            CreatedAt = now.AddDays(-2),
            CreatedByIp = "127.0.0.1"
        };

        // 4. RefreshToken recently expired (5 days ago, less than 30 days) -> should NOT be deleted
        var tokenRecentlyExpired = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = $"hash_recent_exp_{Guid.NewGuid():N}",
            FamilyId = Guid.NewGuid(),
            ExpiresAt = now.AddDays(-5),
            CreatedAt = now.AddDays(-12),
            CreatedByIp = "127.0.0.1"
        };

        db.RefreshTokens.AddRange(tokenExpiredOld, tokenRevokedOld, tokenValid, tokenRecentlyExpired);

        // 5. PaymentNotification received 200 days ago -> should be deleted
        var notifOld = new PaymentNotification(
            Guid.NewGuid(),
            null,
            $"SPT-OLD-{Guid.NewGuid():N}"[..25],
            Guid.NewGuid().ToString(),
            "settlement",
            "200",
            true,
            "{}",
            now.AddDays(-200),
            $"dedup-old-{Guid.NewGuid():N}");

        // 6. PaymentNotification received 10 days ago -> should NOT be deleted
        var notifRecent = new PaymentNotification(
            Guid.NewGuid(),
            null,
            $"SPT-REC-{Guid.NewGuid():N}"[..25],
            Guid.NewGuid().ToString(),
            "settlement",
            "200",
            true,
            "{}",
            now.AddDays(-10),
            $"dedup-rec-{Guid.NewGuid():N}");

        db.PaymentNotifications.AddRange(notifOld, notifRecent);
        await db.SaveChangesAsync();

        // Act
        await cleanupJob.ExecuteAsync(CancellationToken.None);

        // Assert
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var remainingTokens = await verifyDb.RefreshTokens
            .Where(t => t.Id == tokenExpiredOld.Id || t.Id == tokenRevokedOld.Id || t.Id == tokenValid.Id || t.Id == tokenRecentlyExpired.Id)
            .ToListAsync();

        remainingTokens.Should().NotContain(t => t.Id == tokenExpiredOld.Id);
        remainingTokens.Should().NotContain(t => t.Id == tokenRevokedOld.Id);
        remainingTokens.Should().Contain(t => t.Id == tokenValid.Id);
        remainingTokens.Should().Contain(t => t.Id == tokenRecentlyExpired.Id);

        var remainingNotifs = await verifyDb.PaymentNotifications
            .Where(n => n.Id == notifOld.Id || n.Id == notifRecent.Id)
            .ToListAsync();

        remainingNotifs.Should().NotContain(n => n.Id == notifOld.Id);
        remainingNotifs.Should().Contain(n => n.Id == notifRecent.Id);
    }
}
