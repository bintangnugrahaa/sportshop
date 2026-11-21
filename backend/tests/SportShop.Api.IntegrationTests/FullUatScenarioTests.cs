using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Application.Features.Payments.Webhook;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;
using SportShop.Domain.Payments;
using SportShop.Domain.Shipping;
using SportShop.Infrastructure.Options;
using SportShop.Infrastructure.Persistence;
using Xunit;

namespace SportShop.Api.IntegrationTests;

/// <summary>
/// T11-04: UAT Skenario Penuh (Sandbox Midtrans & Lifecycle):
/// 1. Beli -> Bayar -> Proses -> Kirim -> Selesai
/// 2. Kedaluwarsa (Expire) -> Stok kembali atomik
/// 3. Batal (Cancel) -> Stok kembali atomik
/// 4. Webhook Duplikat (Idempotensi)
/// </summary>
public class FullUatScenarioTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FullUatScenarioTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.99.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private async Task<(HttpClient Client, Guid UserId, string Email)> CreateAuthenticatedCustomerClientAsync()
    {
        var client = CreateTestClient();
        var email = $"uat_customer_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "UAT Customer", "08123456789"));
        regResp.EnsureSuccessStatusCode();

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        loginResp.EnsureSuccessStatusCode();
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        client.WithBearerToken(loginData!.AccessToken);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);

        return (client, user.Id, email);
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

    private async Task<(Guid ProductId, Guid VariantId)> SeedProductAndVariantAsync(int initialStock = 20, decimal price = 250_000m)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var suffix = Guid.NewGuid().ToString()[..8];
        var category = new Category(Guid.NewGuid(), $"UAT-Kategori-{suffix}", $"uat-cat-{suffix}");
        var brand = new Brand(Guid.NewGuid(), $"UAT-Brand-{suffix}", $"uat-brand-{suffix}");
        db.Categories.Add(category);
        db.Brands.Add(brand);

        var product = new Product(
            Guid.NewGuid(),
            category.Id,
            brand.Id,
            $"UAT Sepatu Lari {suffix}",
            $"uat-sepatu-lari-{suffix}",
            "Sepatu lari performa tinggi untuk pengujian sistem end-to-end.",
            price);
        db.Products.Add(product);

        var variant = new ProductVariant(
            Guid.NewGuid(),
            product.Id,
            $"SKU-UAT-{suffix}",
            price,
            weightGram: 600,
            stockQuantity: initialStock,
            optionSize: "42",
            optionColor: "Hitam/Oranye");
        db.ProductVariants.Add(variant);

        if (!await db.ShippingRates.AnyAsync(r => r.ProvinceCode == "32"))
        {
            db.ShippingRates.Add(new ShippingRate("32", "Jawa Barat", 12_000m, 6_000m, 1, 3, true));
        }

        await db.SaveChangesAsync();
        return (product.Id, variant.Id);
    }

    private static async Task<Guid> CreateCustomerAddressAsync(HttpClient client)
    {
        var createDto = new
        {
            label = "Kantor UAT",
            recipientName = "UAT Tester",
            phone = "081298765432",
            provinceCode = "32",
            city = "Bandung",
            district = "Coblong",
            postalCode = "40132",
            street = "Jl. Dipati Ukur No. 45",
            isDefault = true
        };

        var resp = await client.PostAsJsonAsync("/api/v1/addresses", createDto);
        resp.EnsureSuccessStatusCode();

        var created = await resp.Content.ReadFromJsonAsync<AddressDto>();
        return created!.Id;
    }

    private string GetMidtransServerKey()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<MidtransOptions>>().Value;
        return options.ServerKey;
    }

    [Fact]
    public async Task UAT_Scenario1_FullPurchaseLifecycle_Buy_Pay_Process_Ship_Complete()
    {
        // -------------------------------------------------------------
        // Step 1: Customer Beli (Katalog -> Keranjang -> Checkout)
        // -------------------------------------------------------------
        var (customerClient, _, _) = await CreateAuthenticatedCustomerClientAsync();
        var (_, variantId) = await SeedProductAndVariantAsync(initialStock: 10, price: 300_000m);
        var addressId = await CreateCustomerAddressAsync(customerClient);

        // Tambah ke keranjang
        var addCartResp = await customerClient.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = 2 });
        addCartResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Checkout
        var idempotencyKey = Guid.NewGuid().ToString("N");
        customerClient.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);
        var checkoutResp = await customerClient.PostAsJsonAsync("/api/v1/orders/checkout", new OrdersController.CheckoutRequest(addressId));
        checkoutResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Status.Should().Be("PendingPayment");
        order.SnapToken.Should().NotBeNullOrEmpty();
        order.Subtotal.Should().Be(600_000m);

        // Verifikasi reservasi stok (10 - 2 = 8)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var variant = await db.ProductVariants.FirstAsync(v => v.Id == variantId);
            variant.StockQuantity.Should().Be(8);
        }

        // -------------------------------------------------------------
        // Step 2: Bayar (Simulasi Webhook Midtrans Settlement dengan Signature Asli)
        // -------------------------------------------------------------
        var serverKey = GetMidtransServerKey();
        var grossAmountStr = order.Total.ToString("0.00", CultureInfo.InvariantCulture);
        var statusCode = "200";
        var transactionStatus = "settlement";
        var signatureKey = MidtransSignatureVerifier.CalculateSignature(order.OrderNumber, statusCode, grossAmountStr, serverKey);

        _factory.MockPaymentGateway
            .Setup(g => g.GetPaymentStatusAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult(statusCode, "Success", "trx-uat-101", order.OrderNumber, order.Total, "qris", DateTime.UtcNow, transactionStatus, "accept", DateTime.UtcNow, signatureKey, "{}"));

        var webhookClient = CreateTestClient();
        var webhookPayload = new
        {
            order_id = order.OrderNumber,
            status_code = statusCode,
            gross_amount = grossAmountStr,
            signature_key = signatureKey,
            transaction_status = transactionStatus,
            transaction_id = "trx-uat-101",
            payment_type = "qris",
            transaction_time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            settlement_time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
        };

        var webhookResp = await webhookClient.PostAsJsonAsync("/api/v1/payments/midtrans/notification", webhookPayload);
        webhookResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verifikasi pesanan berubah jadi Paid
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var paidOrder = await db.Orders.Include(o => o.StatusHistories).FirstAsync(o => o.Id == order.Id);
            paidOrder.Status.Should().Be(OrderStatus.Paid);
            paidOrder.PaidAt.Should().NotBeNull();
            paidOrder.StatusHistories.Should().Contain(h => h.ToStatus == OrderStatus.Paid);
        }

        // -------------------------------------------------------------
        // Step 3: Admin Proses (PendingPayment/Paid -> Processing)
        // -------------------------------------------------------------
        var adminClient = await CreateAdminClientAsync();
        var processResp = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/process",
            new AdminOrdersController.AdminProcessOrderRequest("Pesanan sedang dipacking oleh staf gudang"));
        processResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var processedOrder = await processResp.Content.ReadFromJsonAsync<OrderDto>();
        processedOrder!.Status.Should().Be("Processing");

        // -------------------------------------------------------------
        // Step 4: Admin Kirim (Processing -> Shipped + Nomor Resi)
        // -------------------------------------------------------------
        var shipResp = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/ship",
            new AdminOrdersController.AdminShipOrderRequest("JNE Express", "JNE-UAT-99887766", "Paket telah diserahkan ke kurir"));
        shipResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var shippedOrder = await shipResp.Content.ReadFromJsonAsync<OrderDto>();
        shippedOrder!.Status.Should().Be("Shipped");
        shippedOrder.Courier.Should().Be("JNE Express");
        shippedOrder.TrackingNumber.Should().Be("JNE-UAT-99887766");
        shippedOrder.ShippedAt.Should().NotBeNull();

        // -------------------------------------------------------------
        // Step 5: Admin / Pelanggan Selesai (Shipped -> Completed)
        // -------------------------------------------------------------
        var completeResp = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/orders/{order.OrderNumber}/complete",
            new AdminOrdersController.AdminCompleteOrderRequest("Pesanan telah diterima oleh pembeli dengan baik"));
        completeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var completedOrder = await completeResp.Content.ReadFromJsonAsync<OrderDto>();
        completedOrder!.Status.Should().Be("Completed");
        completedOrder.CompletedAt.Should().NotBeNull();

        // Verifikasi Riwayat Transisi Status Lengkap
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var finalOrder = await db.Orders.Include(o => o.StatusHistories).FirstAsync(o => o.Id == order.Id);
            finalOrder.Status.Should().Be(OrderStatus.Completed);

            var statuses = finalOrder.StatusHistories.OrderBy(h => h.CreatedAt).Select(h => h.ToStatus).ToList();
            statuses.Should().ContainInOrder(
                OrderStatus.PendingPayment,
                OrderStatus.Paid,
                OrderStatus.Processing,
                OrderStatus.Shipped,
                OrderStatus.Completed);
        }
    }

    [Fact]
    public async Task UAT_Scenario2_PaymentExpiration_RestoresStockAtomically()
    {
        var (customerClient, _, _) = await CreateAuthenticatedCustomerClientAsync();
        var (_, variantId) = await SeedProductAndVariantAsync(initialStock: 15, price: 200_000m);
        var addressId = await CreateCustomerAddressAsync(customerClient);

        // Tambah ke keranjang & checkout 3 unit
        await customerClient.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = 3 });
        var checkoutResp = await customerClient.PostAsJsonAsync("/api/v1/orders/checkout", new OrdersController.CheckoutRequest(addressId));
        checkoutResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();

        // Verifikasi stok berkurang: 15 - 3 = 12
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var variant = await db.ProductVariants.FirstAsync(v => v.Id == variantId);
            variant.StockQuantity.Should().Be(12);
        }

        // Simulasi notifikasi Expire dari Midtrans
        var serverKey = GetMidtransServerKey();
        var grossAmountStr = order!.Total.ToString("0.00", CultureInfo.InvariantCulture);
        var statusCode = "202";
        var transactionStatus = "expire";
        var signatureKey = MidtransSignatureVerifier.CalculateSignature(order.OrderNumber, statusCode, grossAmountStr, serverKey);

        _factory.MockPaymentGateway
            .Setup(g => g.GetPaymentStatusAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult(statusCode, "Expire", "trx-uat-exp", order.OrderNumber, order.Total, "bank_transfer", DateTime.UtcNow, transactionStatus, "accept", null, signatureKey, "{}"));

        var webhookClient = CreateTestClient();
        var webhookPayload = new
        {
            order_id = order.OrderNumber,
            status_code = statusCode,
            gross_amount = grossAmountStr,
            signature_key = signatureKey,
            transaction_status = transactionStatus,
            transaction_id = "trx-uat-exp",
            payment_type = "bank_transfer",
            transaction_time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
        };

        var webhookResp = await webhookClient.PostAsJsonAsync("/api/v1/payments/midtrans/notification", webhookPayload);
        webhookResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verifikasi pesanan Expired & stok varian kembali utuh menjadi 15
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var expiredOrder = await db.Orders.FirstAsync(o => o.Id == order.Id);
            expiredOrder.Status.Should().Be(OrderStatus.Expired);

            var variant = await db.ProductVariants.FirstAsync(v => v.Id == variantId);
            variant.StockQuantity.Should().Be(15); // Stok kembali atomik tepat sekali!

            var movement = await db.InventoryMovements
                .FirstOrDefaultAsync(m => m.VariantId == variantId && m.Reason == StockReason.Expire);
            movement.Should().NotBeNull();
            movement!.ChangeQuantity.Should().Be(3);
        }
    }

    [Fact]
    public async Task UAT_Scenario3_CustomerCancellation_RestoresStockAtomically()
    {
        var (customerClient, _, _) = await CreateAuthenticatedCustomerClientAsync();
        var (_, variantId) = await SeedProductAndVariantAsync(initialStock: 25, price: 150_000m);
        var addressId = await CreateCustomerAddressAsync(customerClient);

        // Tambah ke keranjang & checkout 5 unit
        await customerClient.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = 5 });
        var checkoutResp = await customerClient.PostAsJsonAsync("/api/v1/orders/checkout", new OrdersController.CheckoutRequest(addressId));
        checkoutResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();

        // Verifikasi stok berkurang: 25 - 5 = 20
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var variant = await db.ProductVariants.FirstAsync(v => v.Id == variantId);
            variant.StockQuantity.Should().Be(20);
        }

        // Customer membatalkan pesanan
        var cancelResp = await customerClient.PostAsJsonAsync(
            $"/api/v1/orders/{order!.OrderNumber}/cancel",
            new OrdersController.CancelOrderRequest("Berubah pikiran, ingin mengganti varian warna lain"));
        cancelResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelledOrder = await cancelResp.Content.ReadFromJsonAsync<OrderDto>();
        cancelledOrder!.Status.Should().Be("Cancelled");

        // Verifikasi stok varian kembali utuh menjadi 25
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbOrder = await db.Orders.FirstAsync(o => o.Id == order.Id);
            dbOrder.Status.Should().Be(OrderStatus.Cancelled);

            var variant = await db.ProductVariants.FirstAsync(v => v.Id == variantId);
            variant.StockQuantity.Should().Be(25); // Stok kembali atomik

            var movement = await db.InventoryMovements
                .FirstOrDefaultAsync(m => m.VariantId == variantId && m.Reason == StockReason.Cancel);
            movement.Should().NotBeNull();
            movement!.ChangeQuantity.Should().Be(5);
        }
    }

    [Fact]
    public async Task UAT_Scenario4_DuplicateAndIdempotentWebhooks_SafeExecution()
    {
        var (customerClient, _, _) = await CreateAuthenticatedCustomerClientAsync();
        var (_, variantId) = await SeedProductAndVariantAsync(initialStock: 10, price: 100_000m);
        var addressId = await CreateCustomerAddressAsync(customerClient);

        await customerClient.PostAsJsonAsync("/api/v1/cart/items", new { variantId, quantity = 1 });
        var checkoutResp = await customerClient.PostAsJsonAsync("/api/v1/orders/checkout", new OrdersController.CheckoutRequest(addressId));
        var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();

        var serverKey = GetMidtransServerKey();
        var grossAmountStr = order!.Total.ToString("0.00", CultureInfo.InvariantCulture);
        var statusCode = "200";
        var transactionStatus = "settlement";
        var signatureKey = MidtransSignatureVerifier.CalculateSignature(order.OrderNumber, statusCode, grossAmountStr, serverKey);

        _factory.MockPaymentGateway
            .Setup(g => g.GetPaymentStatusAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult(statusCode, "Success", "trx-uat-dup", order.OrderNumber, order.Total, "qris", DateTime.UtcNow, transactionStatus, "accept", DateTime.UtcNow, signatureKey, "{}"));

        var webhookClient = CreateTestClient();
        var webhookPayload = new
        {
            order_id = order.OrderNumber,
            status_code = statusCode,
            gross_amount = grossAmountStr,
            signature_key = signatureKey,
            transaction_status = transactionStatus,
            transaction_id = "trx-uat-dup",
            payment_type = "qris",
            transaction_time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
        };

        // 1. Panggilan Webhook Pertama -> 200 OK
        var firstResp = await webhookClient.PostAsJsonAsync("/api/v1/payments/midtrans/notification", webhookPayload);
        firstResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Panggilan Webhook Kedua (Duplikat identik) -> Harus tetap 200 OK tanpa error atau state ganda
        var secondResp = await webhookClient.PostAsJsonAsync("/api/v1/payments/midtrans/notification", webhookPayload);
        secondResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Panggilan Webhook Ketiga (Simulasi notifikasi out-of-order 'pending' setelah sudah 'settlement')
        var pendingSignature = MidtransSignatureVerifier.CalculateSignature(order.OrderNumber, "201", grossAmountStr, serverKey);
        var outOfOrderPayload = new
        {
            order_id = order.OrderNumber,
            status_code = "201",
            gross_amount = grossAmountStr,
            signature_key = pendingSignature,
            transaction_status = "pending",
            transaction_id = "trx-uat-dup",
            payment_type = "qris"
        };
        var thirdResp = await webhookClient.PostAsJsonAsync("/api/v1/payments/midtrans/notification", outOfOrderPayload);
        thirdResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verifikasi di database: Status tetap Paid, status histories tidak terduplikasi secara berantakan
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbOrder = await db.Orders.Include(o => o.StatusHistories).FirstAsync(o => o.Id == order.Id);
            dbOrder.Status.Should().Be(OrderStatus.Paid);

            // Paid history harus tepat 1 kali
            dbOrder.StatusHistories.Count(h => h.ToStatus == OrderStatus.Paid).Should().Be(1);

            // Stok tetap berkurang 1 (tidak terpotong lagi)
            var variant = await db.ProductVariants.FirstAsync(v => v.Id == variantId);
            variant.StockQuantity.Should().Be(9);
        }
    }
}
