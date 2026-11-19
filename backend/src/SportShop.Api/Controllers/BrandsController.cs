using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportShop.Application.Features.Catalog.Brands.GetActiveBrands;
using SportShop.Application.Features.Catalog.DTOs;

namespace SportShop.Api.Controllers;

[ApiController]
[Route("api/v1/brands")]
[AllowAnonymous]
public sealed class BrandsController : ControllerBase
{
    private readonly GetActiveBrandsQueryHandler _getActiveBrandsHandler;

    public BrandsController(GetActiveBrandsQueryHandler getActiveBrandsHandler)
    {
        _getActiveBrandsHandler = getActiveBrandsHandler;
    }

    /// <summary>
    /// Mengambil daftar brand aktif untuk navigasi publik (di-cache di Redis).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<BrandDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveBrands(CancellationToken cancellationToken = default)
    {
        var result = await _getActiveBrandsHandler.HandleAsync(new GetActiveBrandsQuery(), cancellationToken);
        return Ok(result.Value);
    }
}
