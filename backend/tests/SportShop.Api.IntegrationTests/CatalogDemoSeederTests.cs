using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Persistence;
using SportShop.Infrastructure.Seeders;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class CatalogDemoSeederTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CatalogDemoSeederTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DemoSeeder_ShouldSeedAtLeast30Products_WhenFlagIsEnabled_AndBeIdempotent()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var loggerMock = new Mock<ILogger<CatalogDemoSeeder>>();

        // 1. When SEED_DEMO_DATA is false, nothing is seeded
        var configDisabled = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SEED_DEMO_DATA"] = "false"
            })
            .Build();

        var seederDisabled = new CatalogDemoSeeder(dbContext, configDisabled, clock, loggerMock.Object);
        var initialCount = await dbContext.Products.CountAsync();
        await seederDisabled.SeedAsync();
        var afterDisabledCount = await dbContext.Products.CountAsync();
        afterDisabledCount.Should().Be(initialCount);

        // 2. When SEED_DEMO_DATA is true, seeds 30 products
        var configEnabled = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SEED_DEMO_DATA"] = "true"
            })
            .Build();

        var seederEnabled = new CatalogDemoSeeder(dbContext, configEnabled, clock, loggerMock.Object);
        await seederEnabled.SeedAsync();

        var finalProductsCount = await dbContext.Products.CountAsync();
        finalProductsCount.Should().BeGreaterThanOrEqualTo(30);

        // Verify categories (at least 3 root categories)
        var rootCats = await dbContext.Categories.Where(c => c.ParentId == null).ToListAsync();
        rootCats.Should().HaveCountGreaterThanOrEqualTo(3);

        // Verify 8 brands
        var brands = await dbContext.Brands.ToListAsync();
        brands.Should().HaveCountGreaterThanOrEqualTo(8);

        // Verify variants and initial inventory movements
        var allVariants = await dbContext.ProductVariants.ToListAsync();
        allVariants.Should().HaveCountGreaterThanOrEqualTo(60); // 30 products * 2-5 variants >= 60

        var allMovements = await dbContext.InventoryMovements.ToListAsync();
        allMovements.Should().HaveCountGreaterThanOrEqualTo(60);

        // 3. Test Idempotency: Running seeder again should NOT duplicate products
        await seederEnabled.SeedAsync();
        var countAfterSecondRun = await dbContext.Products.CountAsync();
        countAfterSecondRun.Should().Be(finalProductsCount);
    }
}
