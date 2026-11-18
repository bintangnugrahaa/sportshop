using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace SportShop.Infrastructure.Observability;

public static class LoggingExtensions
{
    public static LoggerConfiguration ConfigureSportShopLogging(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration)
    {
        var seqUrl = configuration["Seq:ServerUrl"];

        loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("Application", "SportShop.Api")
            .Enrich.With<ActivityEnricher>()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{TraceId}] {Message:lj}{NewLine}{Exception}",
                formatProvider: System.Globalization.CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(seqUrl) && Uri.TryCreate(seqUrl, UriKind.Absolute, out _))
        {
            loggerConfiguration.WriteTo.Seq(seqUrl);
        }

        return loggerConfiguration;
    }

    public static IApplicationBuilder UseSportShopRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = EnrichDiagnosticContext;
        });
    }

    private static void EnrichDiagnosticContext(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        var activity = Activity.Current;
        if (activity is not null)
        {
            var traceId = activity.TraceId.ToString();
            var spanId = activity.SpanId.ToString();

            if (!string.IsNullOrEmpty(traceId) && traceId != "00000000000000000000000000000000")
            {
                diagnosticContext.Set("TraceId", traceId);
            }

            if (!string.IsNullOrEmpty(spanId) && spanId != "0000000000000000")
            {
                diagnosticContext.Set("SpanId", spanId);
            }
        }

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            diagnosticContext.Set("UserId", userId);
        }

        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(clientIp))
        {
            diagnosticContext.Set("ClientIp", clientIp);
        }

        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId);
        }
    }
}
