using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Common;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Features.Catalog.Products.CreateProduct;
using SportShop.Application.Features.Catalog.Products.DeleteProduct;
using SportShop.Application.Features.Catalog.Products.GetProduct;
using SportShop.Application.Features.Catalog.Products.ListProducts;
using SportShop.Application.Features.Catalog.Products.UpdateProduct;
using SportShop.Application.Features.Catalog.Variants.CreateVariant;

namespace SportShop.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/products")]
public class AdminProductsController : ControllerBase
{
    private readonly CreateProductCommandHandler _createHandler;
    private readonly UpdateProductCommandHandler _updateHandler;
    private readonly GetProductByIdQueryHandler _getByIdHandler;
    private readonly ListAdminProductsQueryHandler _listHandler;
    private readonly DeleteProductCommandHandler _deleteHandler;
    private readonly CreateVariantCommandHandler _createVariantHandler;
    private readonly Application.Features.Catalog.Images.UploadImage.UploadProductImageCommandHandler _uploadImageHandler;
    private readonly Application.Features.Catalog.Images.DeleteImage.DeleteProductImageCommandHandler _deleteImageHandler;

    public AdminProductsController(
        CreateProductCommandHandler createHandler,
        UpdateProductCommandHandler updateHandler,
        GetProductByIdQueryHandler getByIdHandler,
        ListAdminProductsQueryHandler listHandler,
        DeleteProductCommandHandler deleteHandler,
        CreateVariantCommandHandler createVariantHandler,
        Application.Features.Catalog.Images.UploadImage.UploadProductImageCommandHandler uploadImageHandler,
        Application.Features.Catalog.Images.DeleteImage.DeleteProductImageCommandHandler deleteImageHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _getByIdHandler = getByIdHandler;
        _listHandler = listHandler;
        _deleteHandler = deleteHandler;
        _createVariantHandler = createVariantHandler;
        _uploadImageHandler = uploadImageHandler;
        _deleteImageHandler = deleteImageHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _listHandler.HandleAsync(
            new ListAdminProductsQuery(page, pageSize, categoryId, brandId, includeInactive),
            cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(new GetProductByIdQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.CategoryId,
            request.BrandId,
            request.Name,
            request.Slug,
            request.Description,
            request.BasePrice,
            request.IsActive,
            request.Variants?.Select(v => new CreateVariantItem(
                v.Sku,
                v.OptionSize,
                v.OptionColor,
                v.Price,
                v.WeightGram,
                v.StockQuantity,
                v.IsActive)).ToList());

        var result = await _createHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(HttpContext);
        }

        return CreatedAtAction(nameof(GetProductById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            id,
            request.CategoryId,
            request.BrandId,
            request.Name,
            request.Slug,
            request.Description,
            request.IsActive);

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deleteHandler.HandleAsync(new DeleteProductCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpPost("{id:guid}/variants")]
    [ProducesResponseType(typeof(ProductVariantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateVariant(Guid id, [FromBody] CreateVariantRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateVariantCommand(
            id,
            request.Sku,
            request.OptionSize,
            request.OptionColor,
            request.Price,
            request.WeightGram,
            request.StockQuantity,
            request.IsActive);

        var result = await _createVariantHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(HttpContext);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("{id:guid}/images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImage(
        Guid id,
        [FromForm] UploadProductImageFormRequest form,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
        {
            var problem = Error.Validation("file.empty", "File gambar tidak boleh kosong.").ToProblemDetails(HttpContext);
            return StatusCode(StatusCodes.Status400BadRequest, problem);
        }

        await using var stream = form.File.OpenReadStream();
        var command = new Application.Features.Catalog.Images.UploadImage.UploadProductImageCommand(
            id,
            stream,
            form.File.FileName,
            form.File.ContentType,
            form.File.Length,
            form.AltText,
            form.SortOrder,
            form.IsPrimary);

        var result = await _uploadImageHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(HttpContext);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var command = new Application.Features.Catalog.Images.DeleteImage.DeleteProductImageCommand(id, imageId);
        var result = await _deleteImageHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}

public sealed class UploadProductImageFormRequest
{
    public IFormFile File { get; set; } = null!;
    public string? AltText { get; set; }
    public int? SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed record CreateProductRequest(
    Guid CategoryId,
    Guid BrandId,
    string Name,
    string Slug,
    string Description,
    decimal BasePrice = 0,
    bool IsActive = true,
    List<CreateVariantRequest>? Variants = null);

public sealed record UpdateProductRequest(
    Guid CategoryId,
    Guid BrandId,
    string Name,
    string Slug,
    string Description,
    bool IsActive = true);

public sealed record CreateVariantRequest(
    string Sku,
    string? OptionSize,
    string? OptionColor,
    decimal Price,
    int WeightGram,
    int StockQuantity,
    bool IsActive = true);
