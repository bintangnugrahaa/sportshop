using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportShop.Infrastructure.Options;

namespace SportShop.Infrastructure.Jobs;

public static partial class HangfireExtensions
{
    public static IServiceCollection AddHangfireServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHangfire((sp, config) =>
        {
            var dbOptions = sp.GetService<IOptions<DatabaseOptions>>()?.Value;
            var connectionString = dbOptions?.Default;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            try
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
                    {
                        SchemaName = "hangfire",
                        PrepareSchemaIfNecessary = true,
                        QueuePollInterval = TimeSpan.FromSeconds(15)
                    });
            }
            catch (Exception ex)
            {
                var logger = sp.GetService<ILogger<HangfireAdminAuthorizationFilter>>();
                if (logger is not null)
                {
                    LogHangfireSchemaFallback(logger, ex.Message, ex);
                }

                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
                    {
                        SchemaName = "hangfire",
                        PrepareSchemaIfNecessary = false,
                        QueuePollInterval = TimeSpan.FromSeconds(15)
                    });
            }
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Math.Max(Environment.ProcessorCount * 2, 2);
        });

        services.AddTransient<ISampleJob, SampleJob>();
        services.AddScoped<Application.Common.Interfaces.IExpireUnpaidOrdersJob, ExpireUnpaidOrdersJob>();
        services.AddScoped<Application.Common.Interfaces.ISendEmailJob, SendEmailJob>();
        services.AddScoped<Application.Common.Interfaces.IReconcilePaymentsJob, ReconcilePaymentsJob>();
        services.AddScoped<Application.Common.Interfaces.ICleanupJob, CleanupJob>();

        return services;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Initial Hangfire schema preparation failed, falling back without auto-prepare: {ErrorMessage}")]
    private static partial void LogHangfireSchemaFallback(ILogger logger, string errorMessage, Exception ex);

    public static IApplicationBuilder UseSportShopHangfireDashboard(this IApplicationBuilder app)
    {
        var storage = app.ApplicationServices.GetService<JobStorage>();
        if (storage is null)
        {
            return app;
        }

        try
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new HangfireAdminAuthorizationFilter()],
                DashboardTitle = "SportShop - Hangfire Dashboard"
            }, storage);

            app.UseSportShopRecurringJobs();
        }
        catch (Exception)
        {
            // Do not crash host if storage is temporarily unreachable
        }

        return app;
    }

    public static IApplicationBuilder UseSportShopRecurringJobs(this IApplicationBuilder app)
    {
        var storage = app.ApplicationServices.GetService<JobStorage>();
        if (storage is null)
        {
            return app;
        }

        try
        {
            var recurringJobManager = app.ApplicationServices.GetService<IRecurringJobManager>();
            recurringJobManager?.AddOrUpdate<Application.Common.Interfaces.IExpireUnpaidOrdersJob>(
                "expire-unpaid-orders",
                job => job.ExecuteAsync(CancellationToken.None),
                "*/5 * * * *");

            recurringJobManager?.AddOrUpdate<Application.Common.Interfaces.IReconcilePaymentsJob>(
                "reconcile-payments",
                job => job.ExecuteAsync(CancellationToken.None),
                "*/10 * * * *");

            recurringJobManager?.AddOrUpdate<Application.Common.Interfaces.ICleanupJob>(
                "cleanup-job",
                job => job.ExecuteAsync(CancellationToken.None),
                "0 2 * * *");
        }
        catch (Exception)
        {
            // Do not crash host if storage is temporarily unreachable
        }

        return app;
    }
}
