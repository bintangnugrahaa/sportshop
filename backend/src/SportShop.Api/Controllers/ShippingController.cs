using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Shipping.DTOs;
using SportShop.Application.Features.Shipping.GetActiveRates;
using SportShop.Application.Features.Shipping.GetQuote;

namespace SportShop.Api.Controllers;

[ApiController]
[Route("api/v1/shipping")]
public class ShippingController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly GetActiveShippingRatesQueryHandler _provincesHandler;
    private readonly GetShippingQuoteQueryHandler _quoteHandler;

    public ShippingController(
        ICurrentUser currentUser,
        GetActiveShippingRatesQueryHandler provincesHandler,
        GetShippingQuoteQueryHandler quoteHandler)
    {
        _currentUser = currentUser;
        _provincesHandler = provincesHandler;
        _quoteHandler = quoteHandler;
    }

    [HttpGet("provinces")]
    [ProducesResponseType(typeof(List<ShippingRateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProvinces(CancellationToken cancellationToken)
    {
        var result = await _provincesHandler.HandleAsync(new GetActiveShippingRatesQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    public sealed record ShippingQuoteRequest(Guid AddressId);

    [HttpPost("quote")]
    [Authorize]
    [ProducesResponseType(typeof(ShippingQuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetQuote(
        [FromBody] ShippingQuoteRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var query = new GetShippingQuoteQuery(_currentUser.UserId.Value, request.AddressId);
        var result = await _quoteHandler.HandleAsync(query, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
