using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Features.Admin.DashboardSummary;
using SportShop.Application.Features.Admin.DashboardSummary.DTOs;

namespace SportShop.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly GetDashboardSummaryQueryHandler _handler;

    public AdminDashboardController(GetDashboardSummaryQueryHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Mendapatkan ringkasan dashboard admin (KPI penjualan, grafik omzet 30 hari, produk terlaris, stok menipis, pesanan terbaru).
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var result = await _handler.HandleAsync(new GetDashboardSummaryQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
