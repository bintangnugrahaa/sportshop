using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportShop.Api.Models;
using SportShop.Application.Common;

namespace SportShop.Api.Extensions;

public static class ResultExtensions
{
    public static SportShopProblemDetails ToProblemDetails(this Error error, HttpContext context)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
            ErrorType.RateLimit => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status500InternalServerError
        };

        var title = error.Type switch
        {
            ErrorType.Validation => "Validasi gagal",
            ErrorType.Unauthorized => "Autentikasi gagal atau tidak diizinkan",
            ErrorType.Forbidden => "Akses ditolak",
            ErrorType.NotFound => "Sumber daya tidak ditemukan",
            ErrorType.Conflict => "Terjadi konflik data",
            ErrorType.BusinessRule => "Aturan bisnis dilanggar",
            ErrorType.RateLimit => "Terlalu banyak permintaan",
            _ => "Terjadi kesalahan pada server"
        };

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        return new SportShopProblemDetails
        {
            Type = $"https://sportshop.dev/errors/{error.Code}",
            Title = title,
            Status = statusCode,
            Detail = error.Description,
            Code = error.Code,
            TraceId = traceId,
            Errors = error.ValidationErrors
        };
    }

    public static IActionResult ToActionResult(this Result result, HttpContext context)
    {
        if (result.IsSuccess)
        {
            return new OkResult();
        }

        var problemDetails = result.Error.ToProblemDetails(context);
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }

    public static IActionResult ToActionResult<T>(this Result<T> result, HttpContext context)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        var problemDetails = result.Error.ToProblemDetails(context);
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }
}
