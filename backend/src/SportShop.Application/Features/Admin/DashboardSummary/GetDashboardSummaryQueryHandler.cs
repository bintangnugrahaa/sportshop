using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Admin.DashboardSummary.DTOs;

namespace SportShop.Application.Features.Admin.DashboardSummary;

public sealed class GetDashboardSummaryQueryHandler
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IClock _clock;

    public GetDashboardSummaryQueryHandler(IDashboardRepository dashboardRepository, IClock clock)
    {
        _dashboardRepository = dashboardRepository;
        _clock = clock;
    }

    public async Task<Result<DashboardSummaryDto>> HandleAsync(GetDashboardSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var summary = await _dashboardRepository.GetSummaryAsync(_clock.UtcNow, cancellationToken);
        return Result.Success(summary);
    }
}
