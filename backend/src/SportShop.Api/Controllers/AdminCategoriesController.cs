using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Features.Catalog.Categories.CreateCategory;
using SportShop.Application.Features.Catalog.Categories.DeleteCategory;
using SportShop.Application.Features.Catalog.Categories.GetCategory;
using SportShop.Application.Features.Catalog.Categories.ListCategories;
using SportShop.Application.Features.Catalog.Categories.UpdateCategory;
using SportShop.Application.Features.Catalog.DTOs;

namespace SportShop.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/categories")]
public class AdminCategoriesController : ControllerBase
{
    private readonly CreateCategoryCommandHandler _createHandler;
    private readonly UpdateCategoryCommandHandler _updateHandler;
    private readonly GetCategoryByIdQueryHandler _getByIdHandler;
    private readonly ListCategoriesQueryHandler _listHandler;
    private readonly DeleteCategoryCommandHandler _deleteHandler;

    public AdminCategoriesController(
        CreateCategoryCommandHandler createHandler,
        UpdateCategoryCommandHandler updateHandler,
        GetCategoryByIdQueryHandler getByIdHandler,
        ListCategoriesQueryHandler listHandler,
        DeleteCategoryCommandHandler deleteHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _getByIdHandler = getByIdHandler;
        _listHandler = listHandler;
        _deleteHandler = deleteHandler;
    }

    /// <summary>
    /// Mengambil daftar semua kategori produk (Admin).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListCategories(
        [FromQuery] bool includeInactive = true,
        [FromQuery] bool treeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _listHandler.HandleAsync(new ListCategoriesQuery(includeInactive, treeOnly), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Mengambil detail kategori berdasarkan ID (Admin).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(new GetCategoryByIdQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Membuat kategori produk baru (maksimal 2 tingkat kedalaman) (Admin).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand(
            request.Name,
            request.Slug,
            request.ParentId,
            request.SortOrder,
            request.IsActive);

        var result = await _createHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(HttpContext);
        }

        return CreatedAtAction(nameof(GetCategoryById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Memperbarui kategori produk berdasarkan ID (Admin).
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(
            id,
            request.Name,
            request.Slug,
            request.ParentId,
            request.SortOrder,
            request.IsActive);

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Menghapus kategori produk berdasarkan ID (soft delete) (Admin).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deleteHandler.HandleAsync(new DeleteCategoryCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}

public sealed record CreateCategoryRequest(
    string Name,
    string Slug,
    Guid? ParentId = null,
    int SortOrder = 0,
    bool IsActive = true);

public sealed record UpdateCategoryRequest(
    string Name,
    string Slug,
    Guid? ParentId = null,
    int SortOrder = 0,
    bool IsActive = true);
