using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SportShop.Api.Models;
using SportShop.Domain.Common;

namespace SportShop.Api.Middleware;

public sealed partial class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException dex)
        {
            LogDomainException(_logger, dex.Code, dex.Message, dex);
            await HandleDomainExceptionAsync(context, dex);
        }
        catch (Exception ex)
        {
            LogUnhandledException(_logger, ex.Message, ex);
            await HandleUnhandledExceptionAsync(context);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Domain exception occurred: {Code} - {Message}")]
    private static partial void LogDomainException(ILogger logger, string code, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "An unhandled exception occurred: {Message}")]
    private static partial void LogUnhandledException(ILogger logger, string message, Exception ex);

    private static async Task HandleDomainExceptionAsync(HttpContext context, DomainException dex)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var problemDetails = new SportShopProblemDetails
        {
            Type = $"https://sportshop.dev/errors/{dex.Code}",
            Title = "Pelanggaran aturan domain",
            Status = StatusCodes.Status409Conflict,
            Detail = dex.Message,
            Code = dex.Code,
            TraceId = traceId
        };

        context.Response.ContentType = "application/problem+json; charset=utf-8";
        context.Response.StatusCode = StatusCodes.Status409Conflict;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, _jsonOptions));
    }

    private static async Task HandleUnhandledExceptionAsync(HttpContext context)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var problemDetails = new SportShopProblemDetails
        {
            Type = "https://sportshop.dev/errors/server.internal_error",
            Title = "Terjadi kesalahan pada server",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "Terjadi kesalahan internal yang tidak diharapkan.",
            Code = "server.internal_error",
            TraceId = traceId
        };

        context.Response.ContentType = "application/problem+json; charset=utf-8";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, _jsonOptions));
    }
}
