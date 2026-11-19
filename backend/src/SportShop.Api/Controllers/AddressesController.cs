using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Addresses.CreateAddress;
using SportShop.Application.Features.Addresses.DeleteAddress;
using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Application.Features.Addresses.GetAddress;
using SportShop.Application.Features.Addresses.ListAddresses;
using SportShop.Application.Features.Addresses.SetDefault;
using SportShop.Application.Features.Addresses.UpdateAddress;

namespace SportShop.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/addresses")]
public class AddressesController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly ListAddressesQueryHandler _listHandler;
    private readonly GetAddressByIdQueryHandler _getByIdHandler;
    private readonly CreateAddressCommandHandler _createHandler;
    private readonly UpdateAddressCommandHandler _updateHandler;
    private readonly DeleteAddressCommandHandler _deleteHandler;
    private readonly SetDefaultAddressCommandHandler _setDefaultHandler;

    public AddressesController(
        ICurrentUser currentUser,
        ListAddressesQueryHandler listHandler,
        GetAddressByIdQueryHandler getByIdHandler,
        CreateAddressCommandHandler createHandler,
        UpdateAddressCommandHandler updateHandler,
        DeleteAddressCommandHandler deleteHandler,
        SetDefaultAddressCommandHandler setDefaultHandler)
    {
        _currentUser = currentUser;
        _listHandler = listHandler;
        _getByIdHandler = getByIdHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _setDefaultHandler = setDefaultHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<AddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var result = await _listHandler.HandleAsync(new ListAddressesQuery(_currentUser.UserId.Value), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var result = await _getByIdHandler.HandleAsync(new GetAddressByIdQuery(_currentUser.UserId.Value, id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    public sealed record CreateAddressRequest(
        string Label,
        string RecipientName,
        string Phone,
        string ProvinceCode,
        string City,
        string? District,
        string PostalCode,
        string Street,
        bool IsDefault);

    [HttpPost]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateAddressRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var command = new CreateAddressCommand(
            _currentUser.UserId.Value,
            request.Label,
            request.RecipientName,
            request.Phone,
            request.ProvinceCode,
            request.City,
            request.District,
            request.PostalCode,
            request.Street,
            request.IsDefault);

        var result = await _createHandler.HandleAsync(command, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    public sealed record UpdateAddressRequest(
        string Label,
        string RecipientName,
        string Phone,
        string ProvinceCode,
        string City,
        string? District,
        string PostalCode,
        string Street,
        bool IsDefault);

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateAddressRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var command = new UpdateAddressCommand(
            _currentUser.UserId.Value,
            id,
            request.Label,
            request.RecipientName,
            request.Phone,
            request.ProvinceCode,
            request.City,
            request.District,
            request.PostalCode,
            request.Street,
            request.IsDefault);

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var command = new DeleteAddressCommand(_currentUser.UserId.Value, id);
        var result = await _deleteHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    [HttpPost("{id:guid}/set-default")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefault([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var command = new SetDefaultAddressCommand(_currentUser.UserId.Value, id);
        var result = await _setDefaultHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
