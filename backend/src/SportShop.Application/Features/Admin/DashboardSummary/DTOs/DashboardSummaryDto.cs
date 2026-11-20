using SportShop.Application.Features.Catalog.DTOs;

namespace SportShop.Application.Features.Admin.DashboardSummary.DTOs;

public sealed record DashboardKpiDto(
    int OrdersToday,
    decimal Revenue7Days,
    decimal Revenue30Days,
    int PendingProcessingOrders,
    int LowStockCount);

public sealed record DailyRevenueDto(
    DateOnly Date,
    decimal Revenue,
    int OrdersCount);

public sealed record TopSellingProductDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    int TotalQuantitySold,
    decimal TotalRevenue,
    string? ImageUrl);

public sealed record RecentOrderDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    decimal Total,
    string Status,
    DateTime CreatedAt,
    int ItemsCount);

public sealed record DashboardSummaryDto(
    DashboardKpiDto Kpi,
    IReadOnlyList<DailyRevenueDto> RevenueChart,
    IReadOnlyList<TopSellingProductDto> TopSellingProducts,
    IReadOnlyList<LowStockVariantDto> LowStockProducts,
    IReadOnlyList<RecentOrderDto> RecentOrders);
