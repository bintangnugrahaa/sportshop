using FluentAssertions;
using Moq;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Admin.DashboardSummary;
using SportShop.Application.Features.Admin.DashboardSummary.DTOs;
using SportShop.Application.Features.Catalog.DTOs;
using Xunit;

namespace SportShop.Application.Tests;

public sealed class AdminDashboardTests
{
    [Fact]
    public async Task GetDashboardSummary_ShouldReturnSummary()
    {
        var repo = new Mock<IDashboardRepository>();
        var clock = new Mock<IClock>();
        var now = DateTime.UtcNow;
        clock.Setup(c => c.UtcNow).Returns(now);

        var kpi = new DashboardKpiDto(5, 500000m, 2000000m, 2, 1);
        var expected = new DashboardSummaryDto(
            kpi,
            new List<DailyRevenueDto>(),
            new List<TopSellingProductDto>(),
            new List<LowStockVariantDto>(),
            new List<RecentOrderDto>());

        repo.Setup(r => r.GetSummaryAsync(now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetDashboardSummaryQueryHandler(repo.Object, clock.Object);
        var result = await handler.HandleAsync(new GetDashboardSummaryQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected);
    }
}
