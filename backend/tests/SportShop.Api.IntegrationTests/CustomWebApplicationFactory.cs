using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("sportshop_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCommand("-c", "max_connections=300")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public Mock<IPaymentGateway> MockPaymentGateway { get; } = new();

    public string PostgreSqlConnectionString => _postgreSqlContainer.GetConnectionString();
    public string RedisConnectionString => _redisContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        await _redisContainer.StartAsync();

        // Run migrations and identity seeder on the Testcontainers database instance
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<SportShop.Infrastructure.Identity.IIdentitySeeder>();
        await seeder.SeedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgreSqlContainer.StopAsync();
        await _redisContainer.StopAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgreSqlContainer.GetConnectionString() + ";Maximum Pool Size=10;Timeout=60;Command Timeout=60;",
                ["Redis:ConnectionString"] = _redisContainer.GetConnectionString()
            };

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPaymentGateway));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            MockPaymentGateway.Setup(g => g.VerifySignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string orderId, string statusCode, string grossAmount, string sig) =>
                    SportShop.Application.Features.Payments.Webhook.MidtransSignatureVerifier.Verify(orderId, statusCode, grossAmount, "SB-Mid-server-placeholder", sig));

            MockPaymentGateway.Setup(g => g.CreateSnapTransactionAsync(It.IsAny<CreateSnapTransactionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CreateSnapTransactionRequest req, CancellationToken _) =>
                    new CreateSnapTransactionResult(true, $"snap-token-{req.OrderNumber}", $"https://app.sandbox.midtrans.com/snap/v2/vtweb/snap-token-{req.OrderNumber}"));

            MockPaymentGateway.Setup(g => g.GetPaymentStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string orderNum, CancellationToken _) =>
                    new PaymentStatusResult("200", "Success", $"trx-{orderNum}", orderNum, 100000m, "qris", DateTime.UtcNow, "settlement", "accept", DateTime.UtcNow, "dummy-sig", "{}"));

            MockPaymentGateway.Setup(g => g.CancelTransactionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            services.AddSingleton(MockPaymentGateway);
            services.AddSingleton(MockPaymentGateway.Object);
        });
    }
}
