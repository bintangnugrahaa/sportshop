using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SportShop.Application.Common.Interfaces;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public sealed class OpenTelemetryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OpenTelemetryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void OpenTelemetry_TracerAndMeterProviders_AreRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var tracerProvider = scope.ServiceProvider.GetService<TracerProvider>();
        var meterProvider = scope.ServiceProvider.GetService<MeterProvider>();
        var metrics = scope.ServiceProvider.GetService<ISportShopMetrics>();

        // Assert
        tracerProvider.Should().NotBeNull();
        meterProvider.Should().NotBeNull();
        metrics.Should().NotBeNull();
    }

    [Fact]
    public void SportShopMetrics_RecordOperations_WithoutThrowing()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var metrics = scope.ServiceProvider.GetRequiredService<ISportShopMetrics>();

        // Act
        Action recordActions = () =>
        {
            metrics.OrderCreated();
            metrics.PaymentSettled();
            metrics.StockConflict();
        };

        // Assert
        recordActions.Should().NotThrow();
    }
}
