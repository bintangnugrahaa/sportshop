using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Application.Features.Admin.DashboardSummary.DTOs;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Register;
using SportShop.Domain.Catalog;
using SportShop.Domain.Orders;
using SportShop.Infrastructure.Persistence;
using Xunit;

namespace SportShop.Api.IntegrationTests;

[Collection("IntegrationTests")]
public class AdminDashboardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminDashboardTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.80.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = CreateTestClient();
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand("admin@sportshop.com", "J3&fP6z@"));
        loginResp.EnsureSuccessStatusCode();
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        client.WithBearerToken(loginData!.AccessToken);
        return client;
    }

    private async Task<HttpClient> CreateCustomerClientAsync()
    {
        var client = CreateTestClient();
        var email = $"cust_dash_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Customer Dash", "08123456789"));
        regResp.EnsureSuccessStatusCode();

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        loginResp.EnsureSuccessStatusCode();
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        client.WithBearerToken(loginData!.AccessToken);
        return client;
    }

    [Fact]
    public async Task GetSummary_WithoutToken_ReturnsUnauthorized()
    {
        var client = CreateTestClient();
        var response = await client.GetAsync("/api/v1/admin/dashboard/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_WithCustomerToken_ReturnsForbidden()
    {
        var client = await CreateCustomerClientAsync();
        var response = await client.GetAsync("/api/v1/admin/dashboard/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSummary_WithAdminToken_ReturnsSummaryMatchingData()
    {
        var adminClient = await CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstAsync(u => u.Email == "admin@sportshop.com");

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var category = new Category(Guid.NewGuid(), $"CatDash-{suffix}", $"cat-dash-{suffix}");
        var brand = new Brand(Guid.NewGuid(), $"BrandDash-{suffix}", $"brand-dash-{suffix}");
        var product = new Product(Guid.NewGuid(), category.Id, brand.Id, $"ProductDash-{suffix}", $"prod-dash-{suffix}", "Desc", 100_000m);

        var variantLowStock = new ProductVariant(Guid.NewGuid(), product.Id, $"SKU-LOW-{suffix}", 100_000m, 500, 2, "M", "Hitam");
        var variantNormal = new ProductVariant(Guid.NewGuid(), product.Id, $"SKU-NORM-{suffix}", 150_000m, 500, 20, "L", "Putih");

        db.Categories.Add(category);
        db.Brands.Add(brand);
        db.Products.Add(product);
        db.ProductVariants.AddRange(variantLowStock, variantNormal);

        var nowUtc = DateTime.UtcNow;

        OrderShippingAddress CreateAddress() => new(
            "Recipient Dash",
            "08123456789",
            "DKI",
            "DKI Jakarta",
            "Jakarta Selatan",
            "12345",
            "Jl. Sudirman No 10");

        // Order 1: Paid today
        var orderPaid = new Order(
            Guid.NewGuid(),
            $"SPT-{nowUtc:yyyyMMdd}-P{suffix}",
            user.Id,
            CreateAddress(),
            500,
            10_000m,
            nowUtc.AddHours(24),
            null,
            nowUtc);
        orderPaid.AddItem(new OrderItem(Guid.NewGuid(), orderPaid.Id, variantLowStock.Id, product.Name, "M / Hitam", variantLowStock.Sku, null, 100_000m, 2));
        orderPaid.MarkPaid(nowUtc);

        // Order 2: Completed 3 days ago
        var orderCompleted = new Order(
            Guid.NewGuid(),
            $"SPT-{nowUtc:yyyyMMdd}-C{suffix}",
            user.Id,
            CreateAddress(),
            1000,
            20_000m,
            nowUtc.AddDays(-2),
            null,
            nowUtc.AddDays(-3));
        orderCompleted.AddItem(new OrderItem(Guid.NewGuid(), orderCompleted.Id, variantNormal.Id, product.Name, "L / Putih", variantNormal.Sku, null, 150_000m, 2));
        orderCompleted.MarkPaid(nowUtc.AddDays(-3));
        orderCompleted.Process(nowUtc.AddDays(-2));
        orderCompleted.Ship("JNE", "AWB123456", nowUtc.AddDays(-2));
        orderCompleted.Complete(nowUtc.AddDays(-1));

        // Order 3: PendingPayment today (not counted in revenue)
        var orderPending = new Order(
            Guid.NewGuid(),
            $"SPT-{nowUtc:yyyyMMdd}-X{suffix}",
            user.Id,
            CreateAddress(),
            500,
            10_000m,
            nowUtc.AddHours(24),
            null,
            nowUtc);
        orderPending.AddItem(new OrderItem(Guid.NewGuid(), orderPending.Id, variantNormal.Id, product.Name, "L / Putih", variantNormal.Sku, null, 150_000m, 1));

        db.Orders.AddRange(orderPaid, orderCompleted, orderPending);
        await db.SaveChangesAsync();

        // Call GET /api/v1/admin/dashboard/summary
        var response = await adminClient.GetAsync("/api/v1/admin/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        summary.Should().NotBeNull();

        // Check KPI numbers
        summary!.Kpi.OrdersToday.Should().BeGreaterThanOrEqualTo(2); // orderPaid and orderPending created today
        summary.Kpi.Revenue7Days.Should().BeGreaterThanOrEqualTo(orderPaid.Total + orderCompleted.Total);
        summary.Kpi.Revenue30Days.Should().BeGreaterThanOrEqualTo(orderPaid.Total + orderCompleted.Total);
        summary.Kpi.PendingProcessingOrders.Should().BeGreaterThanOrEqualTo(1); // orderPaid is in status Paid (waiting for admin to process)
        summary.Kpi.LowStockCount.Should().BeGreaterThanOrEqualTo(1);

        // Check revenue chart
        summary.RevenueChart.Should().HaveCount(30);

        // Check top selling products
        summary.TopSellingProducts.Should().NotBeEmpty();

        // Check low stock products
        summary.LowStockProducts.Should().Contain(p => p.VariantId == variantLowStock.Id);

        // Check recent orders
        summary.RecentOrders.Should().NotBeEmpty();
        summary.RecentOrders.Should().Contain(o => o.OrderNumber == orderPaid.OrderNumber);
    }
}
