using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportShop.Application.Common;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Features.Catalog.Products.GetBySlug;
using SportShop.Application.Features.Catalog.Products.List;

namespace SportShop.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[AllowAnonymous]
public sealed class ProductsController : ControllerBase
{
    private readonly ListProductsQueryHandler _listProductsHandler;
    private readonly GetProductBySlugQueryHandler _getProductBySlugHandler;

    public ProductsController(
        ListProductsQueryHandler listProductsHandler,
        GetProductBySlugQueryHandler getProductBySlugHandler)
    {
        _listProductsHandler = listProductsHandler;
        _getProductBySlugHandler = getProductBySlugHandler;
    }

    /// <summary>
    /// Mengambil daftar produk publik dengan filter kategori, brand, rentang harga, stok, serta sortir dan paginasi.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListProducts(
        [FromQuery] string? q,
        [FromQuery] string? categorySlug,
        [FromQuery] string? brandSlug,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] bool? inStock,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListProductsQuery(q, categorySlug, brandSlug, minPrice, maxPrice, inStock, sort, page, pageSize);
        var result = await _listProductsHandler.HandleAsync(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.Type switch
            {
                ErrorType.Validation => ValidationProblem(new ValidationProblemDetails(result.Error.ValidationErrors ?? new Dictionary<string, string[]>())
                {
                    Title = result.Error.Description,
                    Detail = result.Error.Code,
                    Status = StatusCodes.Status400BadRequest
                }),
                _ => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: result.Error.Description,
                    detail: result.Error.Code)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Mengambil detail produk berdasarkan slug termasuk varian dan galeri gambar.
    /// </summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductBySlug(
        [FromRoute] string slug,
        CancellationToken cancellationToken = default)
    {
        var result = await _getProductBySlugHandler.HandleAsync(new GetProductBySlugQuery(slug), cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: result.Error.Description,
                detail: result.Error.Code);
        }

        return Ok(result.Value);
    }
}
