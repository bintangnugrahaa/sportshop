using System.Net;
using FluentAssertions;
using Hangfire;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Infrastructure.Jobs;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public sealed class HangfireTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HangfireTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void HangfireServices_AreRegisteredInDependencyInjection()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var jobStorage = scope.ServiceProvider.GetService<JobStorage>();
        var backgroundJobClient = scope.ServiceProvider.GetService<IBackgroundJobClient>();
        var recurringJobManager = scope.ServiceProvider.GetService<IRecurringJobManager>();
        var sampleJob = scope.ServiceProvider.GetService<ISampleJob>();

        // Assert
        jobStorage.Should().NotBeNull();
        backgroundJobClient.Should().NotBeNull();
        recurringJobManager.Should().NotBeNull();
        sampleJob.Should().NotBeNull();
    }

    [Fact]
    public void SampleJob_ExecutesSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var sampleJob = scope.ServiceProvider.GetRequiredService<ISampleJob>();

        // Act
        Action act = () => sampleJob.Execute();

        // Assert: criteria: "Job contoh berjalan"
        act.Should().NotThrow();
    }

    [Fact]
    public void BackgroundJobClient_EnqueuesSampleJob_Successfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

        // Act: Enqueue sample job
        var jobId = client.Enqueue<ISampleJob>(job => job.Execute());

        // Assert: jobId is generated indicating successful enqueue to PostgreSQL storage
        jobId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task HangfireDashboard_ReturnsSuccessStatusCode_InDevelopmentEnvironment()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/hangfire");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.MovedPermanently, HttpStatusCode.Redirect);
    }
}
