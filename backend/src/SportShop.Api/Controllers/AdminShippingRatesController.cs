using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Features.Shipping.DTOs;
using SportShop.Application.Features.Shipping.GetRate;
using SportShop.Application.Features.Shipping.ListRates;
using SportShop.Application.Features.Shipping.UpdateRate;

namespace SportShop.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/shipping-rates")]
public class AdminShippingRatesController : ControllerBase
{
    private readonly ListShippingRatesQueryHandler _listHandler;
    private readonly GetShippingRateQueryHandler _getHandler;
    private readonly UpdateShippingRateCommandHandler _updateHandler;

    public AdminShippingRatesController(
        ListShippingRatesQueryHandler listHandler,
        GetShippingRateQueryHandler getHandler,
        UpdateShippingRateCommandHandler updateHandler)
    {
        _listHandler = listHandler;
        _getHandler = getHandler;
        _updateHandler = updateHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ShippingRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListAll(CancellationToken cancellationToken)
    {
        var result = await _listHandler.HandleAsync(new ListShippingRatesQuery(IncludeInactive: true), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpGet("{provinceCode}")]
    [ProducesResponseType(typeof(ShippingRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProvinceCode(
        [FromRoute] string provinceCode,
        CancellationToken cancellationToken)
    {
        var result = await _getHandler.HandleAsync(new GetShippingRateQuery(provinceCode), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    public sealed record UpdateShippingRateRequest(
        decimal BaseFee,
        decimal PerKgFee,
        short EstimatedDaysMin,
        short EstimatedDaysMax,
        bool IsActive);

    [HttpPut("{provinceCode}")]
    [ProducesResponseType(typeof(ShippingRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRate(
        [FromRoute] string provinceCode,
        [FromBody] UpdateShippingRateRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateShippingRateCommand(
            provinceCode,
            request.BaseFee,
            request.PerKgFee,
            request.EstimatedDaysMin,
            request.EstimatedDaysMax,
            request.IsActive);

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
