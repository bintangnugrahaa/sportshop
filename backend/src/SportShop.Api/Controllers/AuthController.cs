using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Features.Auth.ConfirmEmail;
using SportShop.Application.Features.Auth.ForgotPassword;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Logout;
using SportShop.Application.Features.Auth.Refresh;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Auth.ResetPassword;

namespace SportShop.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
public class AuthController : ControllerBase
{
    public const string RefreshTokenCookieName = "refreshToken";

    private readonly RegisterCommandHandler _registerHandler;
    private readonly LoginCommandHandler _loginHandler;
    private readonly RefreshTokenCommandHandler _refreshHandler;
    private readonly LogoutCommandHandler _logoutHandler;
    private readonly ForgotPasswordCommandHandler _forgotPasswordHandler;
    private readonly ResetPasswordCommandHandler _resetPasswordHandler;
    private readonly ConfirmEmailCommandHandler _confirmEmailHandler;

    public AuthController(
        RegisterCommandHandler registerHandler,
        LoginCommandHandler loginHandler,
        RefreshTokenCommandHandler refreshHandler,
        LogoutCommandHandler logoutHandler,
        ForgotPasswordCommandHandler forgotPasswordHandler,
        ResetPasswordCommandHandler resetPasswordHandler,
        ConfirmEmailCommandHandler confirmEmailHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _refreshHandler = refreshHandler;
        _logoutHandler = logoutHandler;
        _forgotPasswordHandler = forgotPasswordHandler;
        _resetPasswordHandler = resetPasswordHandler;
        _confirmEmailHandler = confirmEmailHandler;
    }

    /// <summary>
    /// Registrasi akun pelanggan baru.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await _registerHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(HttpContext);
        }

        return CreatedAtAction(nameof(Register), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Masuk (login) dengan email dan password untuk mendapatkan access token JWT dan refresh token cookie.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _loginHandler.HandleAsync(command, GetIpAddress(), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(HttpContext);
        }

        SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiresAt);

        return Ok(new LoginResponse(result.Value.AccessToken, result.Value.ExpiresIn));
    }

    /// <summary>
    /// Memperbarui access token menggunakan refresh token (rotasi refresh token).
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand? command, CancellationToken cancellationToken)
    {
        var rawToken = Request.Cookies[RefreshTokenCookieName] ?? command?.RefreshToken;
        var refreshCommand = new RefreshTokenCommand(rawToken);

        var result = await _refreshHandler.HandleAsync(refreshCommand, GetIpAddress(), cancellationToken);
        if (result.IsFailure)
        {
            Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
            {
                Path = "/api/v1/auth"
            });
            return result.ToActionResult(HttpContext);
        }

        SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiresAt);

        return Ok(new RefreshTokenResponse(result.Value.AccessToken, result.Value.ExpiresIn));
    }

    /// <summary>
    /// Keluar (logout) dan mencabut refresh token aktif.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var rawToken = Request.Cookies[RefreshTokenCookieName];
        await _logoutHandler.HandleAsync(new LogoutCommand(rawToken), GetIpAddress(), cancellationToken);

        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            Path = "/api/v1/auth"
        });

        return Ok(new { Message = "Logout berhasil." });
    }

    /// <summary>
    /// Mengirim tautan/token reset kata sandi ke email pengguna.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _forgotPasswordHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(HttpContext);
        }

        return Ok(new { Message = "Jika email terdaftar, instruksi reset kata sandi telah dikirim ke alamat email Anda." });
    }

    /// <summary>
    /// Mengatur ulang kata sandi akun menggunakan token reset.
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _resetPasswordHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(HttpContext);
        }

        return Ok(new { Message = "Kata sandi berhasil direset. Silakan masuk dengan kata sandi baru Anda." });
    }

    /// <summary>
    /// Mengonfirmasi alamat email pendaftaran akun menggunakan token konfirmasi.
    /// </summary>
    [HttpPost("confirm-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        var result = await _confirmEmailHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(HttpContext);
        }

        return Ok(new { Message = "Email berhasil dikonfirmasi." });
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            Expires = expiresAt
        };

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    private string? GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}
