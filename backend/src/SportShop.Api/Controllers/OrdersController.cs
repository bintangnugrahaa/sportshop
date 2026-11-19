using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Features.Orders.Checkout;
using SportShop.Application.Features.Orders.DTOs;

namespace SportShop.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly CheckoutOrderCommandHandler _checkoutHandler;

    public OrdersController(CheckoutOrderCommandHandler checkoutHandler)
    {
        _checkoutHandler = checkoutHandler;
    }

    public sealed record CheckoutRequest(Guid AddressId);

    /// <summary>
    /// Checkout keranjang belanja menjadi pesanan baru.
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new CheckoutOrderCommand(request.AddressId, idempotencyKey);
        var result = await _checkoutHandler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToActionResult(HttpContext);
        }

        return Created($"/api/v1/orders/{result.Value.OrderNumber}", result.Value);
    }
}
