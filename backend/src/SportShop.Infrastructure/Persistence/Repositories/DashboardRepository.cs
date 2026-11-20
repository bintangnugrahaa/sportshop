using Microsoft.EntityFrameworkCore;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Admin.DashboardSummary.DTOs;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Domain.Orders;

namespace SportShop.Infrastructure.Persistence.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _context;

    public DashboardRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var todayStartUtc = DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);
        var sevenDaysAgoUtc = utcNow.AddDays(-7);
        var thirtyDaysAgoUtc = utcNow.AddDays(-30);

        var paidStatuses = new[]
        {
            OrderStatus.Paid,
            OrderStatus.Processing,
            OrderStatus.Shipped,
            OrderStatus.Completed
        };

        var ordersToday = await _context.Orders.AsNoTracking()
            .CountAsync(o => o.CreatedAt >= todayStartUtc, cancellationToken);

        var revenue7Days = await _context.Orders.AsNoTracking()
            .Where(o => paidStatuses.Contains(o.Status) && o.CreatedAt >= sevenDaysAgoUtc)
            .SumAsync(o => (decimal?)o.Total, cancellationToken) ?? 0m;

        var revenue30Days = await _context.Orders.AsNoTracking()
            .Where(o => paidStatuses.Contains(o.Status) && o.CreatedAt >= thirtyDaysAgoUtc)
            .SumAsync(o => (decimal?)o.Total, cancellationToken) ?? 0m;

        var pendingProcessingOrders = await _context.Orders.AsNoTracking()
            .CountAsync(o => o.Status == OrderStatus.Paid, cancellationToken);

        var lowStockCount = await _context.ProductVariants.AsNoTracking()
            .CountAsync(v => v.IsActive && v.StockQuantity <= 5, cancellationToken);

        var kpi = new DashboardKpiDto(
            ordersToday,
            revenue7Days,
            revenue30Days,
            pendingProcessingOrders,
            lowStockCount);

        // 30 days revenue chart (from today - 29 days up to today)
        var startDate = DateOnly.FromDateTime(todayStartUtc).AddDays(-29);
        var startDateTimeUtc = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var recentPaidOrders = await _context.Orders.AsNoTracking()
            .Where(o => paidStatuses.Contains(o.Status) && o.CreatedAt >= startDateTimeUtc)
            .Select(o => new { o.CreatedAt, o.Total })
            .ToListAsync(cancellationToken);

        var ordersByDate = recentPaidOrders
            .GroupBy(o => DateOnly.FromDateTime(o.CreatedAt))
            .ToDictionary(g => g.Key, g => (Revenue: g.Sum(x => x.Total), Count: g.Count()));

        var revenueChart = new List<DailyRevenueDto>(30);
        for (var i = 0; i < 30; i++)
        {
            var date = startDate.AddDays(i);
            if (ordersByDate.TryGetValue(date, out var data))
            {
                revenueChart.Add(new DailyRevenueDto(date, data.Revenue, data.Count));
            }
            else
            {
                revenueChart.Add(new DailyRevenueDto(date, 0m, 0));
            }
        }

        // Top selling products (based on paid orders)
        var topSellingItems = await _context.OrderItems.AsNoTracking()
            .Where(i => paidStatuses.Contains(i.Order.Status))
            .GroupBy(i => new { i.Variant.ProductId, i.ProductName, i.Sku, i.ImageUrl })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.Sku,
                TotalQuantitySold = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.LineTotal),
                g.Key.ImageUrl
            })
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topSellingProducts = topSellingItems.Select(x => new TopSellingProductDto(
            x.ProductId,
            x.ProductName,
            x.Sku,
            x.TotalQuantitySold,
            x.TotalRevenue,
            x.ImageUrl)).ToList();

        // Low stock products
        var lowStockVariants = await _context.ProductVariants.AsNoTracking()
            .Include(v => v.Product)
            .Where(v => v.IsActive && v.StockQuantity <= 5)
            .OrderBy(v => v.StockQuantity)
            .Take(10)
            .ToListAsync(cancellationToken);

        var lowStockProducts = lowStockVariants.Select(v => new LowStockVariantDto(
            v.Id,
            v.ProductId,
            v.Product.Name,
            v.Sku,
            v.OptionSize,
            v.OptionColor,
            v.StockQuantity,
            v.Price)).ToList();

        // Recent orders
        var recentOrders = await _context.Orders.AsNoTracking()
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentOrderDtos = recentOrders.Select(o => new RecentOrderDto(
            o.Id,
            o.OrderNumber,
            o.ShippingAddress.RecipientName,
            o.Total,
            o.Status.ToString(),
            o.CreatedAt,
            o.Items.Sum(i => i.Quantity))).ToList();

        return new DashboardSummaryDto(
            kpi,
            revenueChart,
            topSellingProducts,
            lowStockProducts,
            recentOrderDtos);
    }
}
