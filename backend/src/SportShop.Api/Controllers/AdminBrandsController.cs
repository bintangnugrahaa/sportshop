using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Features.Catalog.Brands.CreateBrand;
using SportShop.Application.Features.Catalog.Brands.DeleteBrand;
using SportShop.Application.Features.Catalog.Brands.GetBrand;
using SportShop.Application.Features.Catalog.Brands.ListBrands;
using SportShop.Application.Features.Catalog.Brands.UpdateBrand;
using SportShop.Application.Features.Catalog.DTOs;

namespace SportShop.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/brands")]
public class AdminBrandsController : ControllerBase
{
    private readonly CreateBrandCommandHandler _createHandler;
    private readonly UpdateBrandCommandHandler _updateHandler;
    private readonly GetBrandByIdQueryHandler _getByIdHandler;
    private readonly ListBrandsQueryHandler _listHandler;
    private readonly DeleteBrandCommandHandler _deleteHandler;

    public AdminBrandsController(
        CreateBrandCommandHandler createHandler,
        UpdateBrandCommandHandler updateHandler,
        GetBrandByIdQueryHandler getByIdHandler,
        ListBrandsQueryHandler listHandler,
        DeleteBrandCommandHandler deleteHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _getByIdHandler = getByIdHandler;
        _listHandler = listHandler;
        _deleteHandler = deleteHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListBrands(
        [FromQuery] bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _listHandler.HandleAsync(new ListBrandsQuery(includeInactive), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBrandById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(new GetBrandByIdQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateBrandCommand(
            request.Name,
            request.Slug,
            request.LogoUrl,
            request.IsActive);

        var result = await _createHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(HttpContext);
        }

        return CreatedAtAction(nameof(GetBrandById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateBrand(Guid id, [FromBody] UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateBrandCommand(
            id,
            request.Name,
            request.Slug,
            request.LogoUrl,
            request.IsActive);

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBrand(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deleteHandler.HandleAsync(new DeleteBrandCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}

public sealed record CreateBrandRequest(
    string Name,
    string Slug,
    string? LogoUrl = null,
    bool IsActive = true);

public sealed record UpdateBrandRequest(
    string Name,
    string Slug,
    string? LogoUrl = null,
    bool IsActive = true);
