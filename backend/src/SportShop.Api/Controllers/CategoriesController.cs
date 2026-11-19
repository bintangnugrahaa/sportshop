using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportShop.Application.Features.Catalog.Categories.GetCategoryTree;
using SportShop.Application.Features.Catalog.DTOs;

namespace SportShop.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
[AllowAnonymous]
public sealed class CategoriesController : ControllerBase
{
    private readonly GetCategoryTreeQueryHandler _getCategoryTreeHandler;

    public CategoriesController(GetCategoryTreeQueryHandler getCategoryTreeHandler)
    {
        _getCategoryTreeHandler = getCategoryTreeHandler;
    }

    /// <summary>
    /// Mengambil pohon kategori hierarkis untuk navigasi publik (di-cache di Redis).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryTree(CancellationToken cancellationToken = default)
    {
        var result = await _getCategoryTreeHandler.HandleAsync(new GetCategoryTreeQuery(), cancellationToken);
        return Ok(result.Value);
    }
}
