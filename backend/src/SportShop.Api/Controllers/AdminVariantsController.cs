using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Features.Catalog.Inventory.AdjustStock;
using SportShop.Application.Features.Catalog.Variants.DeleteVariant;
using SportShop.Application.Features.Catalog.Variants.UpdateVariant;

namespace SportShop.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/variants")]
public class AdminVariantsController : ControllerBase
{
    private readonly UpdateVariantCommandHandler _updateHandler;
    private readonly DeleteVariantCommandHandler _deleteHandler;
    private readonly AdjustStockCommandHandler _adjustStockHandler;

    public AdminVariantsController(
        UpdateVariantCommandHandler updateHandler,
        DeleteVariantCommandHandler deleteHandler,
        AdjustStockCommandHandler adjustStockHandler)
    {
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _adjustStockHandler = adjustStockHandler;
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductVariantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateVariant(Guid id, [FromBody] UpdateVariantRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateVariantCommand(
            id,
            request.Sku,
            request.OptionSize,
            request.OptionColor,
            request.Price,
            request.WeightGram,
            request.IsActive);

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVariant(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deleteHandler.HandleAsync(new DeleteVariantCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpPost("{id:guid}/stock-adjustments")]
    [ProducesResponseType(typeof(InventoryMovementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AdjustStock(Guid id, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var command = new AdjustStockCommand(id, request.ChangeQuantity, request.Note);
        var result = await _adjustStockHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}

public sealed record UpdateVariantRequest(
    string Sku,
    string? OptionSize,
    string? OptionColor,
    decimal Price,
    int WeightGram,
    bool IsActive = true);

public sealed record AdjustStockRequest(
    int ChangeQuantity,
    string Note);
