using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;
using SportShop.Domain.Payments;
using SportShop.Domain.Shipping;
using SportShop.Infrastructure.Persistence;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class ReconcilePaymentsJobTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReconcilePaymentsJobTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Guid> SeedVariantAsync(AppDbContext db)
    {
        var suffix = Guid.NewGuid().ToString()[..8];
        var category = new Category(Guid.NewGuid(), $"Cat-{suffix}", $"cat-{suffix}");
        var brand = new Brand(Guid.NewGuid(), $"Brand-{suffix}", $"brand-{suffix}");
        db.Categories.Add(category);
        db.Brands.Add(brand);

        var product = new Product(Guid.NewGuid(), category.Id, brand.Id, $"Prod-{suffix}", $"prod-{suffix}", "Desc", 100_000m);
        db.Products.Add(product);

        var variant = new ProductVariant(Guid.NewGuid(), product.Id, $"SKU-{suffix}", 100_000m, 500, 10, "M", "Hitam");
        db.ProductVariants.Add(variant);
        await db.SaveChangesAsync();

        return variant.Id;
    }

    [Fact]
    public async Task ExecuteAsync_WhenWebhookMissed_AndMidtransSettled_ShouldReconcileOrderToPaid()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var job = scope.ServiceProvider.GetRequiredService<IReconcilePaymentsJob>();

        var variantId = await SeedVariantAsync(db);
        var now = clock.UtcNow;
        var olderTime = now.AddMinutes(-25);

        var user = await db.Users.FirstAsync();
        var suffix = Guid.NewGuid().ToString()[..8];
        var orderNumber = $"SPT-RECON-{suffix}";

        var shippingAddress = new OrderShippingAddress("Joko", "0811111111", "31", "DKI Jakarta", "Jakarta", "10000", "Jl. Test");
        var order = new Order(
            Guid.NewGuid(),
            orderNumber,
            user.Id,
            shippingAddress,
            500,
            10_000m,
            now.AddHours(20),
            createdAt: olderTime);

        var item = new OrderItem(Guid.NewGuid(), order.Id, variantId, "Produk Uji", "M", "SKU-TEST-1", null, 100_000m, 1);
        order.AddItem(item);

        var payment = new Payment(
            Guid.NewGuid(),
            order.Id,
            order.Total,
            "snap-token-recon",
            "https://redirect.recon",
            "Midtrans",
            createdAt: olderTime);

        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        // Midtrans returns Settlement (webhook was completely missed)
        _factory.MockPaymentGateway
            .Setup(g => g.GetPaymentStatusAsync(orderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult("200", "Success", "trx-recon-1", orderNumber, order.Total, "qris", now, "settlement", "accept", now, "sig", "{\"status\":\"settlement\"}"));

        // Execute reconciliation job
        await job.ExecuteAsync();

        // Verify order and payment are reconciled to Paid / Settlement
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dbOrder = await verifyDb.Orders.FirstAsync(o => o.Id == order.Id);
        dbOrder.Status.Should().Be(OrderStatus.Paid);
        dbOrder.PaidAt.Should().NotBeNull();

        var dbPayment = await verifyDb.Payments.FirstAsync(p => p.Id == payment.Id);
        dbPayment.Status.Should().Be(PaymentStatus.Settlement);
        dbPayment.SettledAt.Should().NotBeNull();
        dbPayment.TransactionId.Should().Be("trx-recon-1");
    }

    [Fact]
    public async Task ExecuteAsync_WhenPaymentStillPendingInMidtrans_ShouldKeepPendingStatus()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var job = scope.ServiceProvider.GetRequiredService<IReconcilePaymentsJob>();

        var variantId = await SeedVariantAsync(db);
        var now = clock.UtcNow;
        var olderTime = now.AddMinutes(-20);

        var user = await db.Users.FirstAsync();
        var suffix = Guid.NewGuid().ToString()[..8];
        var orderNumber = $"SPT-RECON-PEND-{suffix}";

        var shippingAddress = new OrderShippingAddress("Joko", "0811111111", "31", "DKI Jakarta", "Jakarta", "10000", "Jl. Test");
        var order = new Order(
            Guid.NewGuid(),
            orderNumber,
            user.Id,
            shippingAddress,
            500,
            10_000m,
            now.AddHours(20),
            createdAt: olderTime);

        var item = new OrderItem(Guid.NewGuid(), order.Id, variantId, "Produk Uji Pend", "M", "SKU-PEND-1", null, 100_000m, 1);
        order.AddItem(item);

        var payment = new Payment(
            Guid.NewGuid(),
            order.Id,
            order.Total,
            "snap-token-recon-pend",
            "https://redirect.recon",
            "Midtrans",
            createdAt: olderTime);

        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        // Midtrans still reports pending
        _factory.MockPaymentGateway
            .Setup(g => g.GetPaymentStatusAsync(orderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult("201", "Pending", "trx-recon-pend", orderNumber, order.Total, "bank_transfer", now, "pending", null, null, "sig", "{}"));

        await job.ExecuteAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dbOrder = await verifyDb.Orders.FirstAsync(o => o.Id == order.Id);
        dbOrder.Status.Should().Be(OrderStatus.PendingPayment);

        var dbPayment = await verifyDb.Payments.FirstAsync(p => p.Id == payment.Id);
        dbPayment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMidtransReturnsCancel_ShouldCancelOrderAndRestoreStock()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var job = scope.ServiceProvider.GetRequiredService<IReconcilePaymentsJob>();

        var now = clock.UtcNow;
        var olderTime = now.AddMinutes(-20);

        var category = new Category(Guid.NewGuid(), "Cat-Recon", "cat-recon");
        var brand = new Brand(Guid.NewGuid(), "Brand-Recon", "brand-recon");
        db.Categories.Add(category);
        db.Brands.Add(brand);

        var product = new Product(Guid.NewGuid(), category.Id, brand.Id, "Product-Recon", "product-recon", "Desc", 100_000m);
        db.Products.Add(product);

        // Variant stock initially 3 (reserved 2 for this order, so current stock is 3)
        var variant = new ProductVariant(Guid.NewGuid(), product.Id, "SKU-RECON-1", 100_000m, 500, 3, "M", "Hitam");
        db.ProductVariants.Add(variant);

        var user = await db.Users.FirstAsync();
        var orderNumber = $"SPT-RECON-CNC-{Guid.NewGuid().ToString()[..8]}";
        var shippingAddress = new OrderShippingAddress("Joko", "0811111111", "31", "DKI Jakarta", "Jakarta", "10000", "Jl. Test");
        var order = new Order(
            Guid.NewGuid(),
            orderNumber,
            user.Id,
            shippingAddress,
            500,
            10_000m,
            now.AddHours(20),
            createdAt: olderTime);

        var item = new OrderItem(Guid.NewGuid(), order.Id, variant.Id, "Product-Recon", "M / Hitam", "SKU-RECON-1", null, 100_000m, 2);
        order.AddItem(item);

        var payment = new Payment(
            Guid.NewGuid(),
            order.Id,
            order.Total,
            "snap-token-recon-cnc",
            "https://redirect.recon",
            "Midtrans",
            createdAt: olderTime);

        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        // Midtrans reports Cancel
        _factory.MockPaymentGateway
            .Setup(g => g.GetPaymentStatusAsync(orderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult("200", "Cancel", "trx-recon-cnc", orderNumber, order.Total, "bank_transfer", now, "cancel", null, null, "sig", "{}"));

        await job.ExecuteAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dbOrder = await verifyDb.Orders.FirstAsync(o => o.Id == order.Id);
        dbOrder.Status.Should().Be(OrderStatus.Cancelled);

        var dbPayment = await verifyDb.Payments.FirstAsync(p => p.Id == payment.Id);
        dbPayment.Status.Should().Be(PaymentStatus.Cancel);

        // Stock restored from 3 back to 5
        var dbVariant = await verifyDb.ProductVariants.FindAsync(variant.Id);
        dbVariant!.StockQuantity.Should().Be(5);
    }
}
