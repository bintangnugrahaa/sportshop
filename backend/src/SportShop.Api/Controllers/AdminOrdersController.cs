using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Common;
using SportShop.Application.Features.Orders.Admin.CancelAdminOrder;
using SportShop.Application.Features.Orders.Admin.CompleteOrder;
using SportShop.Application.Features.Orders.Admin.GetAdminOrder;
using SportShop.Application.Features.Orders.Admin.ListAdminOrders;
using SportShop.Application.Features.Orders.Admin.ProcessOrder;
using SportShop.Application.Features.Orders.Admin.ShipOrder;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Domain.Orders;

namespace SportShop.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/orders")]
public class AdminOrdersController : ControllerBase
{
    private readonly ListAdminOrdersQueryHandler _listHandler;
    private readonly GetAdminOrderByNumberQueryHandler _getHandler;
    private readonly ProcessOrderCommandHandler _processHandler;
    private readonly ShipOrderCommandHandler _shipHandler;
    private readonly CompleteOrderCommandHandler _completeHandler;
    private readonly CancelAdminOrderCommandHandler _cancelHandler;

    public AdminOrdersController(
        ListAdminOrdersQueryHandler listHandler,
        GetAdminOrderByNumberQueryHandler getHandler,
        ProcessOrderCommandHandler processHandler,
        ShipOrderCommandHandler shipHandler,
        CompleteOrderCommandHandler completeHandler,
        CancelAdminOrderCommandHandler cancelHandler)
    {
        _listHandler = listHandler;
        _getHandler = getHandler;
        _processHandler = processHandler;
        _shipHandler = shipHandler;
        _completeHandler = completeHandler;
        _cancelHandler = cancelHandler;
    }

    public sealed record AdminProcessOrderRequest(string? Note = null);
    public sealed record AdminShipOrderRequest(string Courier, string TrackingNumber, string? Note = null);
    public sealed record AdminCompleteOrderRequest(string? Note = null);
    public sealed record AdminCancelOrderRequest(string? Reason = null);

    /// <summary>
    /// Daftar semua pesanan dengan filter status, tanggal, dan pencarian (Admin).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListOrders(
        [FromQuery] OrderStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListAdminOrdersQuery(status, from, to, q, page, pageSize);
        var result = await _listHandler.HandleAsync(query, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Detail pesanan berdasarkan nomor pesanan (Admin).
    /// </summary>
    [HttpGet("{orderNumber}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderByNumber(
        [FromRoute] string orderNumber,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAdminOrderByNumberQuery(orderNumber);
        var result = await _getHandler.HandleAsync(query, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Memproses pesanan dari status Paid menjadi Processing (Admin).
    /// </summary>
    [HttpPost("{orderNumber}/process")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ProcessOrder(
        [FromRoute] string orderNumber,
        [FromBody] AdminProcessOrderRequest? request,
        CancellationToken cancellationToken = default)
    {
        var command = new ProcessOrderCommand(orderNumber, request?.Note);
        var result = await _processHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Mengirim pesanan dari status Processing menjadi Shipped (Admin). Kurir dan nomor resi wajib diisi.
    /// </summary>
    [HttpPost("{orderNumber}/ship")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ShipOrder(
        [FromRoute] string orderNumber,
        [FromBody] AdminShipOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ShipOrderCommand(orderNumber, request.Courier, request.TrackingNumber, request.Note);
        var result = await _shipHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Menyelesaikan pesanan dari status Shipped menjadi Completed (Admin).
    /// </summary>
    [HttpPost("{orderNumber}/complete")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteOrder(
        [FromRoute] string orderNumber,
        [FromBody] AdminCompleteOrderRequest? request,
        CancellationToken cancellationToken = default)
    {
        var command = new CompleteOrderCommand(orderNumber, request?.Note);
        var result = await _completeHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Membatalkan pesanan (Admin) sebelum dikirim (PendingPayment, Paid, Processing). Mengembalikan stok.
    /// </summary>
    [HttpPost("{orderNumber}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelOrder(
        [FromRoute] string orderNumber,
        [FromBody] AdminCancelOrderRequest? request,
        CancellationToken cancellationToken = default)
    {
        var command = new CancelAdminOrderCommand(orderNumber, request?.Reason);
        var result = await _cancelHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
