namespace SportShop.Application.Common.Interfaces;

public interface ISportShopMetrics
{
    void OrderCreated();
    void PaymentSettled();
    void StockConflict();
}
