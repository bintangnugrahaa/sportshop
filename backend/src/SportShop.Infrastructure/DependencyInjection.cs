using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Caching;
using SportShop.Infrastructure.Email;
using SportShop.Infrastructure.Identity;
using SportShop.Infrastructure.Jobs;
using SportShop.Infrastructure.Observability;
using SportShop.Infrastructure.Options;
using SportShop.Infrastructure.Persistence;
using SportShop.Infrastructure.Services;
using StackExchange.Redis;

namespace SportShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RedisOptions>()
            .BindConfiguration(RedisOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<MidtransOptions>()
            .BindConfiguration(MidtransOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CorsOptions>()
            .BindConfiguration(CorsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SeqOptions>()
            .BindConfiguration(SeqOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OtelOptions>()
            .BindConfiguration(OtelOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SmtpOptions>()
            .BindConfiguration(SmtpOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OrderOptions>()
            .BindConfiguration(OrderOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOpenTelemetryObservability(configuration);

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(dbOptions.Default, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
            options.UseSnakeCaseNamingConvention();
        });

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IIdentitySeeder, IdentitySeeder>();
        services.AddScoped<ICatalogDemoSeeder, Seeders.CatalogDemoSeeder>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddTransient<IEmailSender, SmtpEmailSender>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICategoryRepository, Persistence.Repositories.CategoryRepository>();
        services.AddScoped<IBrandRepository, Persistence.Repositories.BrandRepository>();
        services.AddScoped<IProductRepository, Persistence.Repositories.ProductRepository>();
        services.AddScoped<ICartRepository, Persistence.Repositories.CartRepository>();
        services.AddScoped<IShippingRateRepository, Persistence.Repositories.ShippingRateRepository>();
        services.AddScoped<IAddressRepository, Persistence.Repositories.AddressRepository>();
        services.AddScoped<IOrderRepository, Persistence.Repositories.OrderRepository>();
        services.AddScoped<IPaymentRepository, Persistence.Repositories.PaymentRepository>();
        services.AddHttpClient<IPaymentGateway, Services.MidtransClient>()
            .AddResilienceHandler("midtrans-pipeline", (builder, context) =>
            {
                var midtransOptions = context.ServiceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<MidtransOptions>>().Value;
                var resilience = midtransOptions.Resilience;

                builder.AddTimeout(TimeSpan.FromSeconds(resilience.TotalTimeoutSeconds));

                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = resilience.MaxRetryAttempts,
                    Delay = TimeSpan.FromMilliseconds(resilience.RetryInitialDelayMs),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutRejectedException>()
                        .HandleResult(r => (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                });

                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = resilience.CircuitBreakerFailureRatio,
                    SamplingDuration = TimeSpan.FromSeconds(resilience.CircuitBreakerSamplingSeconds),
                    MinimumThroughput = resilience.CircuitBreakerMinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(resilience.CircuitBreakerDurationSeconds),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutRejectedException>()
                        .HandleResult(r => (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                });

                builder.AddTimeout(TimeSpan.FromSeconds(resilience.TimeoutSeconds));
            });
        services.AddSingleton<IOrderNumberGenerator, Services.OrderNumberGenerator>();
        services.AddScoped<IFileStorage, Storage.LocalFileStorage>();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisOptions>>().Value;
            var config = ConfigurationOptions.Parse(redisOptions.ConnectionString);
            config.AbortOnConnectFail = false;
            config.ConnectTimeout = 2000;
            return ConnectionMultiplexer.Connect(config);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddSportShopHealthChecks(configuration);
        services.AddHangfireServices(configuration);

        return services;
    }
}
