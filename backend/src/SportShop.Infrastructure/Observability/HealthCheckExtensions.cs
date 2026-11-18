using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SportShop.Infrastructure.Observability;

public static class HealthCheckExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static IServiceCollection AddSportShopHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddNpgSql(
                sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Options.DatabaseOptions>>().Value.Default,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"])
            .AddRedis(
                sp => sp.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>(),
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: ["ready"]);

        return services;
    }

    public static IEndpointRouteBuilder MapSportShopHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteLiveResponse
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = WriteReadyResponse
        });

        return endpoints;
    }

    private static Task WriteLiveResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var response = new
        {
            status = report.Status.ToString()
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }

    private static Task WriteReadyResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            entries = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    status = kvp.Value.Status.ToString(),
                    duration = kvp.Value.Duration.ToString(),
                    description = kvp.Value.Description,
                    error = kvp.Value.Exception?.Message
                })
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }
}
