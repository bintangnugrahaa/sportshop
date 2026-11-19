using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Infrastructure.Identity;

public sealed partial class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClock _clock;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IClock clock,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
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

    [LoggerMessage(Level = LogLevel.Information, Message = "User {Email} registered successfully with ID {UserId}.")]
    private static partial void LogUserRegistered(ILogger logger, string email, Guid userId);
}
