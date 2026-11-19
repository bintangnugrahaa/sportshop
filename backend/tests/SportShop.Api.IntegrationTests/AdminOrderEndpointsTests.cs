using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Common;
using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;
using SportShop.Domain.Shipping;
using SportShop.Infrastructure.Persistence;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class AdminOrderEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminOrderEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.74.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = CreateTestClient();
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand("admin@sportshop.dev", "Admin123!"));
        loginResp.EnsureSuccessStatusCode();
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        client.WithBearerToken(loginData!.AccessToken);
        return client;
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedCustomerClientAsync()
    {
        var client = CreateTestClient();
        var email = $"customer_admin_test_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Customer Tester", "08123456789"));
        regResp.EnsureSuccessStatusCode();

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        loginResp.EnsureSuccessStatusCode();
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        client.WithBearerToken(loginData!.AccessToken);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);

        return (client, user.Id);
    }

    private async Task<Guid> SeedProductAndVariantAsync(int stockQuantity = 10, decimal price = 250_000m, int weightGram = 500)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var suffix = Guid.NewGuid().ToString()[..8];
        var category = new Category(Guid.NewGuid(), $"Cat-{suffix}", $"cat-{suffix}");
        var brand = new Brand(Guid.NewGuid(), $"Brand-{suffix}", $"brand-{suffix}");
        db.Categories.Add(category);
        db.Brands.Add(brand);

        var product = new Product(
            Guid.NewGuid(),
            category.Id,
            brand.Id,
            $"Produk-{suffix}",
            $"produk-{suffix}",
            "Deskripsi produk uji",
            price);
        db.Products.Add(product);

        var variant = new ProductVariant(
            Guid.NewGuid(),
            product.Id,
            $"SKU-{suffix}",
            price,
            weightGram,
            stockQuantity,
            "42",
            "Hitam");
        db.ProductVariants.Add(variant);

        await db.SaveChangesAsync();
        return variant.Id;
    }

    private async Task<Guid> CreateAddressAsync(HttpClient client, string provinceCode = "31")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var exists = await db.ShippingRates.AnyAsync(r => r.ProvinceCode == provinceCode);
        if (!exists)
        {
            db.ShippingRates.Add(new ShippingRate(provinceCode, "DKI Jakarta", 10_000m, 5_000m, 1, 3));
            await db.SaveChangesAsync();
        }

        var req = new AddressesController.CreateAddressRequest(
            "Rumah",
            "Budi Santoso",
            "08123456789",
            provinceCode,
            "Jakarta Selatan",
            "Kebayoran Baru",
            "12130",
            "Jl. Senopati No. 45",
            IsDefault: true);

        var resp = await client.PostAsJsonAsync("/api/v1/addresses", req);
        resp.EnsureSuccessStatusCode();

        var created = await resp.Content.ReadFromJsonAsync<AddressDto>();
        return created!.Id;
    }

    private async Task<(OrderDto Order, Guid VariantId)> CreateOrderAsync(HttpClient client, int stock = 10, int qty = 2)
    {
        var variantId = await SeedProductAndVariantAsync(stockQuantity: stock);
        var addressId = await CreateAddressAsync(client);

        var addResp = await client.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = qty });
        addResp.EnsureSuccessStatusCode();

        var checkoutReq = new OrdersController.CheckoutRequest(addressId);
        var response = await client.PostAsJsonAsync("/api/v1/orders/checkout", checkoutReq);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        return (order!, variantId);
    }

    [Fact]
    public async Task NonAdmin_ShouldReceive403Forbidden_WhenAccessingAdminEndpoints()
    {
        var (customerClient, _) = await CreateAuthenticatedCustomerClientAsync();

        var listResp = await customerClient.GetAsync("/api/v1/admin/orders");
        listResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var processResp = await customerClient.PostAsJsonAsync("/api/v1/admin/orders/SPT-ANY/process", new AdminOrdersController.AdminProcessOrderRequest());
        processResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListOrders_ShouldReturnPagedOrders_ForAdmin()
    {
        var (customerClient, _) = await CreateAuthenticatedCustomerClientAsync();
        var (order, _) = await CreateOrderAsync(customerClient);

        var adminClient = await CreateAdminClientAsync();
        var resp = await adminClient.GetAsync("/api/v1/admin/orders?page=1&pageSize=10");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = await resp.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().Contain(o => o.OrderNumber == order.OrderNumber);
        paged.TotalItems.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetOrderByNumber_ShouldReturnFullDetail_ForAdmin()
    {
        var (customerClient, _) = await CreateAuthenticatedCustomerClientAsync();
        var (order, _) = await CreateOrderAsync(customerClient);

        var adminClient = await CreateAdminClientAsync();
        var resp = await adminClient.GetAsync($"/api/v1/admin/orders/{order.OrderNumber}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await resp.Content.ReadFromJsonAsync<OrderDto>();
        detail.Should().NotBeNull();
        detail!.OrderNumber.Should().Be(order.OrderNumber);
        detail.Items.Should().NotBeEmpty();
        detail.ShippingAddress.Should().NotBeNull();
        detail.StatusHistories.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OrderLifecycle_FullTransition_Paid_Process_Ship_Complete()
    {
        var (customerClient, _) = await CreateAuthenticatedCustomerClientAsync();
        var (order, _) = await CreateOrderAsync(customerClient);

        // Transition order to Paid in DB (simulating payment confirmation)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var orderEntity = await db.Orders
                .Include(o => o.StatusHistories)
                .FirstAsync(o => o.Id == order.Id);

            orderEntity.MarkPaid(DateTime.UtcNow, changedBy: null, note: "Pembayaran terverifikasi");
            await db.SaveChangesAsync();
        }

        var adminClient = await CreateAdminClientAsync();

        // 1. Process
        var processResp = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/process",
            new AdminOrdersController.AdminProcessOrderRequest("Sedang dipacking oleh gudang"));
        processResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var processedOrder = await processResp.Content.ReadFromJsonAsync<OrderDto>();
        processedOrder!.Status.Should().Be("Processing");

        // 2. Ship
        var shipResp = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/ship",
            new AdminOrdersController.AdminShipOrderRequest("JNE", "JNE8877665544", "Paket diserahkan ke kurir"));
        shipResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var shippedOrder = await shipResp.Content.ReadFromJsonAsync<OrderDto>();
        shippedOrder!.Status.Should().Be("Shipped");
        shippedOrder.Courier.Should().Be("JNE");
        shippedOrder.TrackingNumber.Should().Be("JNE8877665544");
        shippedOrder.ShippedAt.Should().NotBeNull();

        // 3. Complete
        var completeResp = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/complete",
            new AdminOrdersController.AdminCompleteOrderRequest("Diterima oleh pelanggan"));
        completeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var completedOrder = await completeResp.Content.ReadFromJsonAsync<OrderDto>();
        completedOrder!.Status.Should().Be("Completed");
        completedOrder.CompletedAt.Should().NotBeNull();

        // Verify status histories in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var orderEntity = await db.Orders
                .Include(o => o.StatusHistories)
                .FirstAsync(o => o.Id == order.Id);

            orderEntity.Status.Should().Be(OrderStatus.Completed);
            orderEntity.StatusHistories.Should().HaveCount(5); // PendingPayment, Paid, Processing, Shipped, Completed
        }
    }

    [Fact]
    public async Task ShipOrder_ShouldFail_WhenCourierOrTrackingNumberMissing()
    {
        var (customerClient, _) = await CreateAuthenticatedCustomerClientAsync();
        var (order, _) = await CreateOrderAsync(customerClient);

        // Set to Paid -> Processing
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var orderEntity = await db.Orders.Include(o => o.StatusHistories).FirstAsync(o => o.Id == order.Id);
            orderEntity.MarkPaid(DateTime.UtcNow);
            orderEntity.Process(DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        var adminClient = await CreateAdminClientAsync();

        // Missing courier
        var badResp1 = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/ship",
            new AdminOrdersController.AdminShipOrderRequest("", "TRK12345"));
        badResp1.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Missing tracking number
        var badResp2 = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/ship",
            new AdminOrdersController.AdminShipOrderRequest("SiCepat", ""));
        badResp2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AdminCancelOrder_WhenProcessing_ShouldSucceed_AndRestoreStock()
    {
        var (customerClient, _) = await CreateAuthenticatedCustomerClientAsync();
        var (order, variantId) = await CreateOrderAsync(customerClient, stock: 10, qty: 2);

        // Set to Paid -> Processing
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var orderEntity = await db.Orders.Include(o => o.StatusHistories).FirstAsync(o => o.Id == order.Id);
            orderEntity.MarkPaid(DateTime.UtcNow);
            orderEntity.Process(DateTime.UtcNow);
            await db.SaveChangesAsync();

            var variant = await db.ProductVariants.FindAsync(variantId);
            variant!.StockQuantity.Should().Be(8);
        }

        var adminClient = await CreateAdminClientAsync();

        // Admin cancels order
        var cancelResp = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/cancel",
            new AdminOrdersController.AdminCancelOrderRequest("Barang cacat saat quality control"));
        cancelResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelledOrder = await cancelResp.Content.ReadFromJsonAsync<OrderDto>();
        cancelledOrder!.Status.Should().Be("Cancelled");
        cancelledOrder.CancelReason.Should().Be("Barang cacat saat quality control");

        // Verify DB: stock restored back to 10
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var variant = await db.ProductVariants.FindAsync(variantId);
            variant!.StockQuantity.Should().Be(10);

            var movement = await db.InventoryMovements
                .FirstOrDefaultAsync(m => m.OrderId == order.Id && m.Reason == StockReason.Cancel);
            movement.Should().NotBeNull();
            movement!.ChangeQuantity.Should().Be(2);
        }
    }

    [Fact]
    public async Task AdminCancelOrder_WhenShipped_ShouldReturn409Conflict()
    {
        var (customerClient, _) = await CreateAuthenticatedCustomerClientAsync();
        var (order, _) = await CreateOrderAsync(customerClient);

        // Set to Shipped
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var orderEntity = await db.Orders.Include(o => o.StatusHistories).FirstAsync(o => o.Id == order.Id);
            orderEntity.MarkPaid(DateTime.UtcNow);
            orderEntity.Process(DateTime.UtcNow);
            orderEntity.Ship("JNE", "JNE123", DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        var adminClient = await CreateAdminClientAsync();

        // Attempt cancel
        var cancelResp = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/cancel",
            new AdminOrdersController.AdminCancelOrderRequest("Mencoba batal"));
        cancelResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await cancelResp.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem!.Code.Should().Be("order.invalid_transition");
    }

    [Fact]
    public async Task IllegalTransitions_ShouldReturn409Conflict()
    {
        var (customerClient, _) = await CreateAuthenticatedCustomerClientAsync();
        var (order, _) = await CreateOrderAsync(customerClient);

        var adminClient = await CreateAdminClientAsync();

        // Order is PendingPayment -> cannot Process directly (must be Paid)
        var processResp = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/process",
            new AdminOrdersController.AdminProcessOrderRequest());
        processResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Cannot Ship directly
        var shipResp = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/ship",
            new AdminOrdersController.AdminShipOrderRequest("JNE", "JNE123"));
        shipResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Cannot Complete directly
        var completeResp = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/complete",
            new AdminOrdersController.AdminCompleteOrderRequest());
        completeResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
