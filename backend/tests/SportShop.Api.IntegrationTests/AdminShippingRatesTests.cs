using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Shipping.DTOs;
using SportShop.Application.Features.Shipping.GetActiveRates;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class AdminShippingRatesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminShippingRatesTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.52.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    [Fact]
    public async Task AdminShippingRates_ShouldRequireAdminRole()
    {
        var client = CreateTestClient();

        // 1. Unauthenticated -> 401
        var unauthResp = await client.GetAsync("/api/v1/admin/shipping-rates");
        unauthResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 2. Customer token -> 403
        client.WithBearerToken(AuthTestHelper.GenerateCustomerToken());
        var customerResp = await client.GetAsync("/api/v1/admin/shipping-rates");
        customerResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminShippingRates_CrudAndCacheInvalidation_ShouldSucceed()
    {
        var client = CreateTestClient();
        client.WithBearerToken(AuthTestHelper.GenerateAdminToken());
        var cacheService = _factory.Services.GetRequiredService<ICacheService>();
        var activeRatesHandler = _factory.Services.GetRequiredService<GetActiveShippingRatesQueryHandler>();

        // 1. List all rates (should contain seeded provinces)
        var listResp = await client.GetAsync("/api/v1/admin/shipping-rates");
        listResp.EnsureSuccessStatusCode();
        var allRates = await listResp.Content.ReadFromJsonAsync<List<ShippingRateDto>>();
        allRates.Should().NotBeNull();
        allRates!.Count.Should().BeGreaterThanOrEqualTo(30);

        // 2. Get JK rate
        var getJkResp = await client.GetAsync("/api/v1/admin/shipping-rates/JK");
        getJkResp.EnsureSuccessStatusCode();
        var jkRate = await getJkResp.Content.ReadFromJsonAsync<ShippingRateDto>();
        jkRate.Should().NotBeNull();
        jkRate!.ProvinceCode.Should().Be("JK");

        // 3. Populate ship:rates:v1 cache
        var activeRatesResult = await activeRatesHandler.HandleAsync(new GetActiveShippingRatesQuery());
        activeRatesResult.IsSuccess.Should().BeTrue();
        var cachedBefore = await cacheService.GetAsync<List<ShippingRateDto>>(GetActiveShippingRatesQueryHandler.CacheKey);
        cachedBefore.Should().NotBeNull();

        // 4. Admin updates JK rate: base_fee 13,000, per_kg_fee 6,500
        var updateRequest = new AdminShippingRatesController.UpdateShippingRateRequest(
            BaseFee: 13_000m,
            PerKgFee: 6_500m,
            EstimatedDaysMin: 1,
            EstimatedDaysMax: 2,
            IsActive: true);

        var updateResp = await client.PutAsJsonAsync("/api/v1/admin/shipping-rates/JK", updateRequest);
        updateResp.EnsureSuccessStatusCode();
        var updatedJk = await updateResp.Content.ReadFromJsonAsync<ShippingRateDto>();
        updatedJk.Should().NotBeNull();
        updatedJk!.BaseFee.Should().Be(13_000m);
        updatedJk.PerKgFee.Should().Be(6_500m);

        // 5. Verify ship:rates:v1 was invalidated in Redis (null immediately)
        var cachedAfter = await cacheService.GetAsync<List<ShippingRateDto>>(GetActiveShippingRatesQueryHandler.CacheKey);
        cachedAfter.Should().BeNull();

        // 6. Querying active rates again repopulates cache with updated values
        var repopulatedResult = await activeRatesHandler.HandleAsync(new GetActiveShippingRatesQuery());
        repopulatedResult.IsSuccess.Should().BeTrue();
        var repopulatedJk = repopulatedResult.Value.First(r => r.ProvinceCode == "JK");
        repopulatedJk.BaseFee.Should().Be(13_000m);
        repopulatedJk.PerKgFee.Should().Be(6_500m);
    }
}
