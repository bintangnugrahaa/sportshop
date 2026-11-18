using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Infrastructure.Observability;

public static class OpenTelemetryExtensions
{
    public const string ServiceName = "SportShop.Api";

    public static IServiceCollection AddOpenTelemetryObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otelEndpoint = configuration["Otel:OtlpEndpoint"] ?? "http://localhost:5341/ingest/otlp";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: ServiceName, serviceVersion: "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ServiceName)
                    .AddSource("Npgsql")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    });

                if (!string.IsNullOrWhiteSpace(otelEndpoint) && Uri.TryCreate(otelEndpoint, UriKind.Absolute, out var endpointUri))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = endpointUri;
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(SportShopMetrics.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otelEndpoint) && Uri.TryCreate(otelEndpoint, UriKind.Absolute, out var endpointUri))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = endpointUri;
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
                }
            });

        services.AddSingleton<ISportShopMetrics, SportShopMetrics>();

        return services;
    }
}
