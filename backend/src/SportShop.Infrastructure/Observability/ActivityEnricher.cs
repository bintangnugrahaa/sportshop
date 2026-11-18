using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace SportShop.Infrastructure.Observability;

public sealed class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        var traceId = activity.TraceId.ToString();
        var spanId = activity.SpanId.ToString();

        if (!string.IsNullOrEmpty(traceId) && traceId != "00000000000000000000000000000000")
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", traceId));
        }

        if (!string.IsNullOrEmpty(spanId) && spanId != "0000000000000000")
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", spanId));
        }

        if (activity.ParentSpanId != default)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ParentSpanId", activity.ParentSpanId.ToString()));
        }
    }
}
