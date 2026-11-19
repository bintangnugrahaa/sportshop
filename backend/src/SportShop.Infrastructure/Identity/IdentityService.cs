using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Options;
using SportShop.Infrastructure.Persistence;

namespace SportShop.Infrastructure.Identity;

public sealed partial class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly IClock _clock;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions,
        IClock clock,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<Guid>> RegisterUserAsync(
        string email,
        string password,
        string fullName,
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return Result.Failure<Guid>(Error.Conflict("auth.email_already_exists", "Email sudah terdaftar."));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            EmailConfirmed = false,
            IsActive = true,
            CreatedAt = _clock.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errorDescription = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return Result.Failure<Guid>(Error.Validation("auth.registration_failed", errorDescription));
        }

        await _userManager.AddToRoleAsync(user, ApplicationRole.Customer);
        LogUserRegistered(_logger, email, user.Id);

        return Result.Success(user.Id);
    }

    public async Task<Result<AuthTokenResult>> LoginAsync(
        string email,
        string password,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Result.Failure<AuthTokenResult>(Error.Unauthorized(
                "auth.invalid_credentials",
                "Email atau kata sandi tidak valid."));
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Result.Failure<AuthTokenResult>(Error.Validation(
                "auth.locked_out",
                "Akun Anda terkunci sementara karena 5 kali percobaan gagal. Silakan coba lagi dalam 15 menit."));
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthTokenResult>(Error.Forbidden(
                "auth.account_inactive",
                "Akun Anda tidak aktif."));
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            if (await _userManager.IsLockedOutAsync(user))
            {
                LogAccountLockedOut(_logger, email);
                return Result.Failure<AuthTokenResult>(Error.Validation(
                    "auth.locked_out",
                    "Akun Anda terkunci sementara karena 5 kali percobaan gagal. Silakan coba lagi dalam 15 menit."));
            }

            return Result.Failure<AuthTokenResult>(Error.Unauthorized(
                "auth.invalid_credentials",
                "Email atau kata sandi tidak valid."));
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, user.FullName, roles);
        var (rawRefreshToken, tokenHash) = _jwtTokenService.GenerateRefreshToken();

        var familyId = Guid.NewGuid();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            FamilyId = familyId,
            CreatedAt = _clock.UtcNow,
            ExpiresAt = _clock.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays),
            CreatedByIp = ipAddress
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var expiresIn = _jwtOptions.ExpiryMinutes * 60;
        LogUserLoggedIn(_logger, email, user.Id);

        return Result.Success(new AuthTokenResult(accessToken, expiresIn, rawRefreshToken, refreshToken.ExpiresAt));
    }

    public async Task<Result<AuthTokenResult>> RefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _jwtTokenService.HashToken(rawRefreshToken);

        var token = await _dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (token is null)
        {
            return Result.Failure<AuthTokenResult>(Error.Unauthorized(
                "auth.invalid_refresh_token",
                "Refresh token tidak valid."));
        }

        if (token.RevokedAt is not null)
        {
            var familyTokens = await _dbContext.RefreshTokens
                .Where(r => r.UserId == token.UserId && r.FamilyId == token.FamilyId && r.RevokedAt == null)
                .ToListAsync(cancellationToken);

            var now = _clock.UtcNow;
            foreach (var ft in familyTokens)
            {
                ft.RevokedAt = now;
                ft.RevokeReason = "ReuseDetected";
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            LogTokenReuseDetected(_logger, token.UserId, token.FamilyId);

            return Result.Failure<AuthTokenResult>(Error.Unauthorized(
                "auth.token_reuse_detected",
                "Indikasi penyalahgunaan token terdeteksi. Seluruh sesi telah dicabut."));
        }

        if (token.ExpiresAt < _clock.UtcNow)
        {
            return Result.Failure<AuthTokenResult>(Error.Unauthorized(
                "auth.expired_refresh_token",
                "Refresh token sudah kedaluwarsa."));
        }

        if (token.User is null || !token.User.IsActive)
        {
            return Result.Failure<AuthTokenResult>(Error.Forbidden(
                "auth.account_inactive",
                "Akun pengguna tidak aktif."));
        }

        if (await _userManager.IsLockedOutAsync(token.User))
        {
            return Result.Failure<AuthTokenResult>(Error.Validation(
                "auth.locked_out",
                "Akun pengguna sedang terkunci."));
        }

        var (newRawToken, newTokenHash) = _jwtTokenService.GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = token.UserId,
            TokenHash = newTokenHash,
            FamilyId = token.FamilyId,
            CreatedAt = _clock.UtcNow,
            ExpiresAt = _clock.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays),
            CreatedByIp = ipAddress
        };

        token.RevokedAt = _clock.UtcNow;
        token.RevokeReason = "Rotated";
        token.ReplacedByTokenId = newRefreshToken.Id;

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = await _userManager.GetRolesAsync(token.User);
        var accessToken = _jwtTokenService.GenerateAccessToken(token.User.Id, token.User.Email!, token.User.FullName, roles);
        var expiresIn = _jwtOptions.ExpiryMinutes * 60;

        LogTokenRotated(_logger, token.UserId, token.FamilyId);

        return Result.Success(new AuthTokenResult(accessToken, expiresIn, newRawToken, newRefreshToken.ExpiresAt));
    }

    public async Task<Result> RevokeRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _jwtTokenService.HashToken(rawRefreshToken);
        var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);
        if (token is null)
        {
            return Result.Success();
        }

        if (token.RevokedAt is null)
        {
            token.RevokedAt = _clock.UtcNow;
            token.RevokeReason = "Logout";
            await _dbContext.SaveChangesAsync(cancellationToken);
            LogTokenRevoked(_logger, token.UserId, "Logout");
        }

        return Result.Success();
    }

    public async Task<Result<UserProfileResult>> GetUserProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result.Failure<UserProfileResult>(Error.NotFound("user.not_found", "Pengguna tidak ditemukan."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Success(new UserProfileResult(
            user.Id,
            user.Email!,
            user.FullName,
            user.PhoneNumber,
            roles.ToList().AsReadOnly(),
            user.CreatedAt));
    }

    public async Task<Result<UserProfileResult>> UpdateUserProfileAsync(
        Guid userId,
        string fullName,
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result.Failure<UserProfileResult>(Error.NotFound("user.not_found", "Pengguna tidak ditemukan."));
        }

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        user.UpdatedAt = _clock.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errorDescription = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            return Result.Failure<UserProfileResult>(Error.Validation("user.update_failed", errorDescription));
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Success(new UserProfileResult(
            user.Id,
            user.Email!,
            user.FullName,
            user.PhoneNumber,
            roles.ToList().AsReadOnly(),
            user.CreatedAt));
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result.Failure(Error.NotFound("user.not_found", "Pengguna tidak ditemukan."));
        }

        var changeResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!changeResult.Succeeded)
        {
            var errorDescription = string.Join("; ", changeResult.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("auth.invalid_password", errorDescription));
        }

        user.UpdatedAt = _clock.UtcNow;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<Result<string?>> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            return Result.Success<string?>(null);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        return Result.Success<string?>(token);
    }

    public async Task<Result> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            return Result.Failure(Error.Validation("auth.invalid_reset_token", "Token reset tidak valid atau sudah kedaluwarsa."));
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var errorDescription = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("auth.invalid_reset_token", errorDescription));
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        user.UpdatedAt = _clock.UtcNow;
        await _userManager.UpdateAsync(user);

        LogPasswordReset(_logger, email);
        return Result.Success();
    }

    public async Task<Result<string?>> GenerateEmailConfirmationTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result.Failure<string?>(Error.NotFound("user.not_found", "Pengguna tidak ditemukan."));
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        return Result.Success<string?>(token);
    }

    public async Task<Result> ConfirmEmailAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result.Failure(Error.Validation("auth.invalid_confirm_token", "Pengguna tidak ditemukan atau tidak aktif."));
        }

        if (user.EmailConfirmed)
        {
            return Result.Failure(Error.Validation("auth.already_confirmed", "Email sudah dikonfirmasi sebelumnya."));
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            var errorDescription = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("auth.invalid_confirm_token", errorDescription));
        }

        user.UpdatedAt = _clock.UtcNow;
        await _userManager.UpdateAsync(user);

        LogEmailConfirmed(_logger, user.Email!);
        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User {Email} registered successfully with ID {UserId}.")]
    private static partial void LogUserRegistered(ILogger logger, string email, Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "User {Email} logged in successfully with ID {UserId}.")]
    private static partial void LogUserLoggedIn(ILogger logger, string email, Guid userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Account {Email} locked out due to multiple failed access attempts.")]
    private static partial void LogAccountLockedOut(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refresh token reuse detected for user {UserId} and family {FamilyId}. Entire token family revoked.")]
    private static partial void LogTokenReuseDetected(ILogger logger, Guid userId, Guid familyId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Refresh token rotated successfully for user {UserId} and family {FamilyId}.")]
    private static partial void LogTokenRotated(ILogger logger, Guid userId, Guid familyId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Refresh token revoked for user {UserId}. Reason: {Reason}")]
    private static partial void LogTokenRevoked(ILogger logger, Guid userId, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Password reset successfully for user {Email}.")]
    private static partial void LogPasswordReset(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Email confirmed successfully for user {Email}.")]
    private static partial void LogEmailConfirmed(ILogger logger, string email);
}
