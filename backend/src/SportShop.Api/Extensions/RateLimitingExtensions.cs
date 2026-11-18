using System.Diagnostics;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Api.Models;

namespace SportShop.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string CheckoutPolicy = "checkout";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IServiceCollection AddSportShopRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json; charset=utf-8";

                var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

                var problemDetails = new SportShopProblemDetails
                {
                    Type = "https://sportshop.dev/errors/rate_limit.exceeded",
                    Title = "Terlalu banyak permintaan",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Batas percobaan terlampaui. Silakan coba lagi setelah 1 menit.",
                    Code = "rate_limit.exceeded",
                    TraceId = traceId
                };

                await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, _jsonOptions), token);
            };

            options.AddPolicy(AuthPolicy, httpContext =>
            {
                var clientIp = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                               ?? httpContext.Connection.RemoteIpAddress?.ToString()
                               ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(CheckoutPolicy, httpContext =>
            {
                var clientIp = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                               ?? httpContext.Connection.RemoteIpAddress?.ToString()
                               ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
