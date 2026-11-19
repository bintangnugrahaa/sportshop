using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Features.Catalog.Inventory.LowStock;

namespace SportShop.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/inventory")]
public class AdminInventoryController : ControllerBase
{
    private readonly GetLowStockVariantsQueryHandler _lowStockHandler;

    public AdminInventoryController(GetLowStockVariantsQueryHandler lowStockHandler)
    {
        _lowStockHandler = lowStockHandler;
    }

    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(List<LowStockVariantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLowStock([FromQuery] int threshold = 5, CancellationToken cancellationToken = default)
    {
        var result = await _lowStockHandler.HandleAsync(new GetLowStockVariantsQuery(threshold), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
