using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SportShop.Api.Models;
using SportShop.Application.Features.Auth.Register;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class RegisterTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RegisterTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_Returns201Created_WhenRequestIsValid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var uniqueEmail = $"user_{Guid.NewGuid():N}@sportshop.dev";
        var request = new RegisterCommand(uniqueEmail, "SecurePass123", "Customer One", "08123456789");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        body.Should().NotBeNull();
        body!.Email.Should().Be(uniqueEmail);
        body.FullName.Should().Be(request.FullName);
        body.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_Returns400BadRequest_WhenValidationFails()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterCommand("not-an-email", "weak", "", null);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(400);
        problem.Code.Should().Be("validation.failed");
        problem.Errors.Should().NotBeNull();
        problem.Errors.Should().ContainKey(nameof(RegisterCommand.Email));
        problem.Errors.Should().ContainKey(nameof(RegisterCommand.Password));
        problem.Errors.Should().ContainKey(nameof(RegisterCommand.FullName));
    }

    [Fact]
    public async Task Register_Returns409Conflict_WhenEmailIsAlreadyRegistered()
    {
        // Arrange
        var client = _factory.CreateClient();
        var duplicateEmail = $"duplicate_{Guid.NewGuid():N}@sportshop.dev";
        var request = new RegisterCommand(duplicateEmail, "SecurePass123", "Customer Duplicate", null);

        // First registration
        var firstResponse = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act: Second registration with same email
        var secondResponse = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await secondResponse.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
        problem.Code.Should().Be("auth.email_already_exists");
    }
}
