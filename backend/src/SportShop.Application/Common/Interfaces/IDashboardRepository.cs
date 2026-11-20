using SportShop.Application.Features.Admin.DashboardSummary.DTOs;

namespace SportShop.Application.Common.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
