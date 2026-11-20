using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Users.ChangePassword;
using SportShop.Application.Features.Users.GetProfile;
using SportShop.Application.Features.Users.UpdateProfile;

namespace SportShop.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me")]
public class MeController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly GetProfileQueryHandler _getProfileHandler;
    private readonly UpdateProfileCommandHandler _updateProfileHandler;
    private readonly ChangePasswordCommandHandler _changePasswordHandler;

    public MeController(
        ICurrentUser currentUser,
        GetProfileQueryHandler getProfileHandler,
        UpdateProfileCommandHandler updateProfileHandler,
        ChangePasswordCommandHandler changePasswordHandler)
    {
        _currentUser = currentUser;
        _getProfileHandler = getProfileHandler;
        _updateProfileHandler = updateProfileHandler;
        _changePasswordHandler = changePasswordHandler;
    }

    /// <summary>
    /// Mendapatkan data profil pengguna yang sedang login.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var result = await _getProfileHandler.HandleAsync(new GetProfileQuery(_currentUser.UserId.Value), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    public sealed record UpdateProfileRequest(string FullName, string? PhoneNumber);

    /// <summary>
    /// Memperbarui informasi profil pengguna yang sedang login.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var command = new UpdateProfileCommand(_currentUser.UserId.Value, request.FullName, request.PhoneNumber);
        var result = await _updateProfileHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    /// <summary>
    /// Mengubah kata sandi akun pengguna yang sedang login.
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var command = new ChangePasswordCommand(_currentUser.UserId.Value, request.CurrentPassword, request.NewPassword);
        var result = await _changePasswordHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(HttpContext);
        }

        return Ok(new { Message = "Kata sandi berhasil diubah." });
    }
}
