using System.Diagnostics.Metrics;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Infrastructure.Observability;

public sealed class SportShopMetrics : ISportShopMetrics, IDisposable
{
    public const string MeterName = "SportShop.Api";
    private readonly Meter _meter;
    private readonly Counter<long> _ordersCreatedCounter;
    private readonly Counter<long> _paymentsSettledCounter;
    private readonly Counter<long> _stockConflictsCounter;

    public SportShopMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");
        _ordersCreatedCounter = _meter.CreateCounter<long>(
            "orders_created_total",
            description: "Total number of orders created");
        _paymentsSettledCounter = _meter.CreateCounter<long>(
            "payments_settled_total",
            description: "Total number of payments successfully settled");
        _stockConflictsCounter = _meter.CreateCounter<long>(
            "stock_conflicts_total",
            description: "Total number of stock conflict / insufficient stock events");
    }

    public void OrderCreated() => _ordersCreatedCounter.Add(1);

    public void PaymentSettled() => _paymentsSettledCounter.Add(1);

    public void StockConflict() => _stockConflictsCounter.Add(1);

    public void Dispose()
    {
        _meter.Dispose();
    }
}
