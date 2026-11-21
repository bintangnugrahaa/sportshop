using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Infrastructure.Identity;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class IdentitySeederTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public IdentitySeederTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Roles_AreSeededSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Act & Assert
        var adminRoleExists = await roleManager.RoleExistsAsync(ApplicationRole.Admin);
        var customerRoleExists = await roleManager.RoleExistsAsync(ApplicationRole.Customer);

        adminRoleExists.Should().BeTrue();
        customerRoleExists.Should().BeTrue();
    }

    [Fact]
    public async Task AdminUser_IsSeededSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Act
        var adminUsers = await userManager.GetUsersInRoleAsync(ApplicationRole.Admin);

        // Assert
        adminUsers.Should().NotBeEmpty();
        var admin = adminUsers.First();
        admin.Email.Should().Be("admin@sportshop.com");
        admin.FullName.Should().Be("Administrator");
        admin.EmailConfirmed.Should().BeTrue();
        admin.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task IdentitySeeder_IsIdempotent_WhenExecutedAgain()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IIdentitySeeder>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Act
        await seeder.SeedAsync();

        // Assert
        var adminUsers = await userManager.GetUsersInRoleAsync(ApplicationRole.Admin);
        adminUsers.Should().HaveCount(1);
    }
}
