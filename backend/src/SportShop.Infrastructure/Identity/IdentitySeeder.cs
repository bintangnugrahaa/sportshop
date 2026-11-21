using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Infrastructure.Identity;

public sealed partial class IdentitySeeder : IIdentitySeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly IClock _clock;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        IClock clock,
        ILogger<IdentitySeeder> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _clock = clock;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
    }

    private async Task SeedRolesAsync()
    {
        string[] roles = [ApplicationRole.Admin, ApplicationRole.Customer];

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var roleResult = await _roleManager.CreateAsync(new ApplicationRole(role));
                if (roleResult.Succeeded)
                {
                    LogRoleCreated(_logger, role);
                }
                else
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    LogRoleCreationFailed(_logger, role, errors);
                }
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        var existingAdmins = await _userManager.GetUsersInRoleAsync(ApplicationRole.Admin);
        if (existingAdmins.Count > 0)
        {
            LogAdminAlreadyExists(_logger);
            return;
        }

        var adminEmail = _configuration["SEED_ADMIN_EMAIL"]
            ?? _configuration["SeedAdmin:Email"]
            ?? "admin@sportshop.com";

        var adminPassword = _configuration["SEED_ADMIN_PASSWORD"]
            ?? _configuration["SeedAdmin:Password"]
            ?? "J3&fP6z@";

        var adminFullName = _configuration["SEED_ADMIN_FULLNAME"]
            ?? _configuration["SeedAdmin:FullName"]
            ?? "Administrator";

        var existingUserByEmail = await _userManager.FindByEmailAsync(adminEmail);
        if (existingUserByEmail is not null)
        {
            if (!await _userManager.IsInRoleAsync(existingUserByEmail, ApplicationRole.Admin))
            {
                await _userManager.AddToRoleAsync(existingUserByEmail, ApplicationRole.Admin);
                LogExistingUserAssignedAdmin(_logger, adminEmail);
            }
            return;
        }

        var adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminEmail,
            Email = adminEmail,
            FullName = adminFullName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = _clock.UtcNow
        };

        var result = await _userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(adminUser, ApplicationRole.Admin);
            LogAdminSeeded(_logger, adminEmail);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            LogAdminSeedingFailed(_logger, errors);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Role {Role} successfully created.")]
    private static partial void LogRoleCreated(ILogger logger, string role);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to create role {Role}: {Errors}")]
    private static partial void LogRoleCreationFailed(ILogger logger, string role, string errors);

    [LoggerMessage(Level = LogLevel.Information, Message = "Admin user already exists. Skipping initial admin seeding.")]
    private static partial void LogAdminAlreadyExists(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Existing user {Email} granted Admin role.")]
    private static partial void LogExistingUserAssignedAdmin(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Initial admin user {Email} seeded successfully.")]
    private static partial void LogAdminSeeded(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to seed admin user: {Errors}")]
    private static partial void LogAdminSeedingFailed(ILogger logger, string errors);
}
