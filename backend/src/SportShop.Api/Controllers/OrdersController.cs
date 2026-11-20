using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Common;
using SportShop.Application.Features.Orders.CancelOrder;
using SportShop.Application.Features.Orders.Checkout;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Application.Features.Orders.GetCustomerOrder;
using SportShop.Application.Features.Orders.ListCustomerOrders;
using SportShop.Application.Features.Orders.PayOrder;
using SportShop.Domain.Orders;

namespace SportShop.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly CheckoutOrderCommandHandler _checkoutHandler;
    private readonly ListCustomerOrdersQueryHandler _listOrdersHandler;
    private readonly GetCustomerOrderByNumberQueryHandler _getOrderHandler;
    private readonly CancelCustomerOrderCommandHandler _cancelOrderHandler;
    private readonly PayOrderCommandHandler _payOrderHandler;

    public OrdersController(
        CheckoutOrderCommandHandler checkoutHandler,
        ListCustomerOrdersQueryHandler listOrdersHandler,
        GetCustomerOrderByNumberQueryHandler getOrderHandler,
        CancelCustomerOrderCommandHandler cancelOrderHandler,
        PayOrderCommandHandler payOrderHandler)
    {
        _checkoutHandler = checkoutHandler;
        _listOrdersHandler = listOrdersHandler;
        _getOrderHandler = getOrderHandler;
        _cancelOrderHandler = cancelOrderHandler;
        _payOrderHandler = payOrderHandler;
    }

    public sealed record CheckoutRequest(Guid AddressId);
    public sealed record CancelOrderRequest(string? Reason);

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

        return CreatedAtAction(nameof(GetOrderByNumber), new { orderNumber = result.Value.OrderNumber }, result.Value);
    }

    /// <summary>
    /// Mendapatkan daftar riwayat pesanan milik pelanggan.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListOrders(
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListCustomerOrdersQuery(status, page, pageSize);
        var result = await _listOrdersHandler.HandleAsync(query, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Mendapatkan detail pesanan milik pelanggan berdasarkan nomor pesanan.
    /// </summary>
    [HttpGet("{orderNumber}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderByNumber(
        [FromRoute] string orderNumber,
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerOrderByNumberQuery(orderNumber);
        var result = await _getOrderHandler.HandleAsync(query, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Membatalkan pesanan yang berstatus PendingPayment.
    /// </summary>
    [HttpPost("{orderNumber}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelOrder(
        [FromRoute] string orderNumber,
        [FromBody] CancelOrderRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new CancelCustomerOrderCommand(orderNumber, request?.Reason);
        var result = await _cancelOrderHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Melanjutkan pembayaran pesanan atau membuat ulang token Snap Midtrans.
    /// </summary>
    [HttpPost("{orderNumber}/pay")]
    [ProducesResponseType(typeof(PayOrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PayOrder(
        [FromRoute] string orderNumber,
        CancellationToken cancellationToken)
    {
        var command = new PayOrderCommand(orderNumber);
        var result = await _payOrderHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
