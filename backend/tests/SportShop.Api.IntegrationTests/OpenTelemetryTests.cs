using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Observability;
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

    [Fact]
    public void CustomMetrics_EmitExpectedMeasurements()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var metrics = scope.ServiceProvider.GetRequiredService<ISportShopMetrics>();

        var recordedMeasurements = new List<(string InstrumentName, long Value)>();

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == SportShopMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            lock (recordedMeasurements)
            {
                recordedMeasurements.Add((instrument.Name, measurement));
            }
        });

        meterListener.Start();

        // Act
        metrics.OrderCreated();
        metrics.PaymentSettled();
        metrics.StockConflict();

        meterListener.RecordObservableInstruments();

        // Assert
        recordedMeasurements.Should().Contain(m => m.InstrumentName == "orders_created_total" && m.Value == 1);
        recordedMeasurements.Should().Contain(m => m.InstrumentName == "payments_settled_total" && m.Value == 1);
        recordedMeasurements.Should().Contain(m => m.InstrumentName == "stock_conflicts_total" && m.Value == 1);
    }
}
