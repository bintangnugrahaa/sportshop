using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SportShop.Infrastructure.Options;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public sealed class OptionsValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OptionsValidationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Application_StartsSuccessfully_WhenConfigurationsAreValid()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("ConnectionStrings:Default", "")]
    [InlineData("Redis:ConnectionString", "")]
    [InlineData("Jwt:Key", "too-short")]
    [InlineData("Jwt:Issuer", "")]
    [InlineData("Midtrans:ServerKey", "")]
    [InlineData("Cors:Origins", "")]
    [InlineData("Seq:ServerUrl", "")]
    [InlineData("Otel:OtlpEndpoint", "")]
    [InlineData("Smtp:Host", "")]
    [InlineData("Smtp:From", "invalid-email")]
    public void Application_FailsToStart_WhenMandatoryConfigurationIsMissingOrInvalid(string configKey, string invalidValue)
    {
        // Arrange
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [configKey] = invalidValue
                });
            });
        });

        // Act
        Action act = () =>
        {
            using var client = customFactory.CreateClient();
        };

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }
}
