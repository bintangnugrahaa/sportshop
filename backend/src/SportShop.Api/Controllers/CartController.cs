using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Cart.AddItem;
using SportShop.Application.Features.Cart.Clear;
using SportShop.Application.Features.Cart.DTOs;
using SportShop.Application.Features.Cart.GetCart;
using SportShop.Application.Features.Cart.RemoveItem;
using SportShop.Application.Features.Cart.UpdateItem;

namespace SportShop.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/cart")]
public class CartController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly GetCartQueryHandler _getCartHandler;
    private readonly AddCartItemCommandHandler _addItemHandler;
    private readonly UpdateCartItemCommandHandler _updateItemHandler;
    private readonly RemoveCartItemCommandHandler _removeItemHandler;
    private readonly ClearCartCommandHandler _clearCartHandler;

    public CartController(
        ICurrentUser currentUser,
        GetCartQueryHandler getCartHandler,
        AddCartItemCommandHandler addItemHandler,
        UpdateCartItemCommandHandler updateItemHandler,
        RemoveCartItemCommandHandler removeItemHandler,
        ClearCartCommandHandler clearCartHandler)
    {
        _currentUser = currentUser;
        _getCartHandler = getCartHandler;
        _addItemHandler = addItemHandler;
        _updateItemHandler = updateItemHandler;
        _removeItemHandler = removeItemHandler;
        _clearCartHandler = clearCartHandler;
    }

    /// <summary>
    /// Mendapatkan data keranjang belanja aktif milik pengguna saat ini.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var result = await _getCartHandler.HandleAsync(new GetCartQuery(_currentUser.UserId.Value), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    public sealed record AddCartItemRequest(Guid VariantId, int Quantity);

    /// <summary>
    /// Menambahkan item varian produk ke dalam keranjang belanja.
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var command = new AddCartItemCommand(_currentUser.UserId.Value, request.VariantId, request.Quantity);
        var result = await _addItemHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    public sealed record UpdateCartItemRequest(int Quantity);

    /// <summary>
    /// Memperbarui jumlah item varian produk dalam keranjang belanja.
    /// </summary>
    [HttpPut("items/{variantId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateItem(
        [FromRoute] Guid variantId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var command = new UpdateCartItemCommand(_currentUser.UserId.Value, variantId, request.Quantity);
        var result = await _updateItemHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Menghapus item varian produk dari keranjang belanja.
    /// </summary>
    [HttpDelete("items/{variantId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(
        [FromRoute] Guid variantId,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var command = new RemoveCartItemCommand(_currentUser.UserId.Value, variantId);
        var result = await _removeItemHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Mengosongkan seluruh item dalam keranjang belanja.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var command = new ClearCartCommand(_currentUser.UserId.Value);
        var result = await _clearCartHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
