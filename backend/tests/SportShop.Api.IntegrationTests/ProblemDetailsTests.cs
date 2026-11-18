using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using SportShop.Api.Extensions;
using SportShop.Api.Middleware;
using SportShop.Api.Models;
using SportShop.Application.Common;
using SportShop.Domain.Common;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public sealed class ProblemDetailsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ProblemDetailsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CorrelationIdMiddleware_GeneratesCorrelationId_WhenHeaderIsNotProvided()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.Headers.Should().ContainKey("X-Correlation-Id");
        var correlationId = response.Headers.GetValues("X-Correlation-Id").FirstOrDefault();
        correlationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_PreservesCorrelationId_WhenHeaderIsProvided()
    {
        // Arrange
        var client = _factory.CreateClient();
        var customCorrelationId = Guid.NewGuid().ToString("D");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Correlation-Id", customCorrelationId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.Headers.Should().ContainKey("X-Correlation-Id");
        var correlationId = response.Headers.GetValues("X-Correlation-Id").FirstOrDefault();
        correlationId.Should().Be(customCorrelationId);
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_HandlesDomainException_ReturningRfc7807ProblemDetails()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new DomainException("stock.insufficient", "Stok varian SKU-123 hanya tersisa 2."),
            loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        context.Response.ContentType.Should().StartWith("application/problem+json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problemDetails = await JsonSerializer.DeserializeAsync<SportShopProblemDetails>(
            context.Response.Body,
            _jsonOptions);

        problemDetails.Should().NotBeNull();
        problemDetails!.Type.Should().Be("https://sportshop.dev/errors/stock.insufficient");
        problemDetails.Code.Should().Be("stock.insufficient");
        problemDetails.Status.Should().Be(409);
        problemDetails.Detail.Should().Be("Stok varian SKU-123 hanya tersisa 2.");
        problemDetails.TraceId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_HandlesUnhandledException_HidingDetails()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Internal database password leaked!"),
            loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().StartWith("application/problem+json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problemDetails = await JsonSerializer.DeserializeAsync<SportShopProblemDetails>(
            context.Response.Body,
            _jsonOptions);

        problemDetails.Should().NotBeNull();
        problemDetails!.Type.Should().Be("https://sportshop.dev/errors/server.internal_error");
        problemDetails.Code.Should().Be("server.internal_error");
        problemDetails.Status.Should().Be(500);
        problemDetails.Detail.Should().Be("Terjadi kesalahan internal yang tidak diharapkan.");
        problemDetails.Detail.Should().NotContain("password");
        problemDetails.TraceId.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(ErrorType.Validation, 400, "Validasi gagal")]
    [InlineData(ErrorType.Unauthorized, 401, "Autentikasi gagal atau tidak diizinkan")]
    [InlineData(ErrorType.Forbidden, 403, "Akses ditolak")]
    [InlineData(ErrorType.NotFound, 404, "Sumber daya tidak ditemukan")]
    [InlineData(ErrorType.Conflict, 409, "Terjadi konflik data")]
    [InlineData(ErrorType.BusinessRule, 422, "Aturan bisnis dilanggar")]
    [InlineData(ErrorType.RateLimit, 429, "Terlalu banyak permintaan")]
    [InlineData(ErrorType.Unexpected, 500, "Terjadi kesalahan pada server")]
    public void ResultExtensions_MapsAllErrorTypes_ToAppropriateStatusCode(ErrorType errorType, int expectedStatus, string expectedTitle)
    {
        // Arrange
        var error = new Error("test.error", "Test description", errorType);
        var httpContext = new DefaultHttpContext();

        // Act
        var problemDetails = error.ToProblemDetails(httpContext);

        // Assert
        problemDetails.Status.Should().Be(expectedStatus);
        problemDetails.Title.Should().Be(expectedTitle);
        problemDetails.Code.Should().Be("test.error");
        problemDetails.Type.Should().Be("https://sportshop.dev/errors/test.error");
        problemDetails.Detail.Should().Be("Test description");
    }

    [Fact]
    public void ResultExtensions_MapsValidationErrors_WhenPresent()
    {
        // Arrange
        var validationErrors = new Dictionary<string, string[]>
        {
            ["quantity"] = ["Jumlah melebihi stok"]
        };
        var error = Error.Validation("stock.insufficient", "Stok tidak mencukupi", validationErrors);
        var httpContext = new DefaultHttpContext();

        // Act
        var problemDetails = error.ToProblemDetails(httpContext);

        // Assert
        problemDetails.Status.Should().Be(400);
        problemDetails.Errors.Should().NotBeNull();
        problemDetails.Errors.Should().ContainKey("quantity");
        problemDetails.Errors!["quantity"].Should().Contain("Jumlah melebihi stok");
    }
}
