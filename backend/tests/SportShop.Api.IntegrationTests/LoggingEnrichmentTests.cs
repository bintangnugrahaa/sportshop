using System.Diagnostics;
using FluentAssertions;
using Moq;
using Serilog.Events;
using Serilog.Parsing;
using SportShop.Infrastructure.Observability;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public sealed class LoggingEnrichmentTests
{
    [Fact]
    public void ActivityEnricher_ShouldEnrichLogEvent_WithTraceIdAndSpanId_WhenActivityIsActive()
    {
        // Arrange
        var enricher = new ActivityEnricher();
        var activity = new Activity("TestActivity").Start();

        var propertyFactoryMock = new Mock<Serilog.Core.ILogEventPropertyFactory>();
        propertyFactoryMock
            .Setup(f => f.CreateProperty("TraceId", It.IsAny<object>(), false))
            .Returns((string name, object value, bool _) => new LogEventProperty(name, new ScalarValue(value)));
        propertyFactoryMock
            .Setup(f => f.CreateProperty("SpanId", It.IsAny<object>(), false))
            .Returns((string name, object value, bool _) => new LogEventProperty(name, new ScalarValue(value)));

        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test message", []),
            []);

        try
        {
            // Act
            enricher.Enrich(logEvent, propertyFactoryMock.Object);

            // Assert
            logEvent.Properties.Should().ContainKey("TraceId");
            logEvent.Properties["TraceId"].ToString().Should().Contain(activity.TraceId.ToString());
            logEvent.Properties.Should().ContainKey("SpanId");
            logEvent.Properties["SpanId"].ToString().Should().Contain(activity.SpanId.ToString());
        }
        finally
        {
            activity.Stop();
        }
    }
}
