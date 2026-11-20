using FluentAssertions;
using Moq;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Admin.DashboardSummary;
using SportShop.Application.Features.Admin.DashboardSummary.DTOs;
using SportShop.Application.Features.Catalog.DTOs;
using Xunit;

namespace SportShop.Application.Tests;

public class DashboardApplicationTests
{
    private readonly Mock<IDashboardRepository> _repoMock;
    private readonly Mock<IClock> _clockMock;
    private readonly GetDashboardSummaryQueryHandler _handler;

    public DashboardApplicationTests()
    {
        _repoMock = new Mock<IDashboardRepository>();
        _clockMock = new Mock<IClock>();
        _handler = new GetDashboardSummaryQueryHandler(_repoMock.Object, _clockMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSummaryFromRepository()
    {
        // Arrange
        var now = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        _clockMock.Setup(c => c.UtcNow).Returns(now);

        var expectedSummary = new DashboardSummaryDto(
            new DashboardKpiDto(5, 1_000_000m, 5_000_000m, 2, 3),
            new List<DailyRevenueDto>(),
            new List<TopSellingProductDto>(),
            new List<LowStockVariantDto>(),
            new List<RecentOrderDto>());

        _repoMock.Setup(r => r.GetSummaryAsync(now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _handler.HandleAsync(new GetDashboardSummaryQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedSummary);
        result.Value!.Kpi.OrdersToday.Should().Be(5);
        result.Value.Kpi.Revenue7Days.Should().Be(1_000_000m);
    }
}
