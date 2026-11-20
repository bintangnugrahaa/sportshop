using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Orders.Checkout;
using SportShop.Domain.Catalog;
using SportShop.Domain.Orders;
using SportShop.Domain.Shipping;
using SportShop.Domain.Users;
using Xunit;

namespace SportShop.Application.Tests;

public class MetricsApplicationTests
{
    [Fact]
    public async Task Checkout_WhenSuccessful_RecordsOrderCreatedMetric()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.UserId).Returns(userId);
        currentUserMock.Setup(u => u.Email).Returns("user@sportshop.dev");

        var addressId = Guid.NewGuid();
        var address = new Address(
            addressId,
            userId,
            "Rumah",
            "Customer Test",
            "08123456789",
            "31",
            "Jakarta Selatan",
            "Kebayoran",
            "12345",
            "Jl. Senopati No. 1",
            isDefault: true);

        var addressRepoMock = new Mock<IAddressRepository>();
        addressRepoMock.Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        var category = new Category(Guid.NewGuid(), "Cat", "cat");
        var brand = new Brand(Guid.NewGuid(), "Brand", "brand");
        var product = new Product(Guid.NewGuid(), category.Id, brand.Id, "Product", "prod", "Desc", 100_000m);
        var variant = new ProductVariant(Guid.NewGuid(), product.Id, "SKU-001", 100_000m, 500, 10, "M", "Hitam");
        variant.Product = product;

        var now = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        var cart = new Domain.Carts.Cart(Guid.NewGuid(), userId);
        cart.AddItem(variant, 2, now);

        var cartRepoMock = new Mock<ICartRepository>();
        cartRepoMock.Setup(r => r.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var shippingRate = new ShippingRate("31", "DKI Jakarta", 10_000m, 5_000m, 1, 3);
        var shippingRepoMock = new Mock<IShippingRateRepository>();
        shippingRepoMock.Setup(r => r.GetByProvinceCodeAsync("31", It.IsAny<CancellationToken>()))
            .ReturnsAsync(shippingRate);

        var productRepoMock = new Mock<IProductRepository>();
        productRepoMock.Setup(r => r.ReserveStockAtomicAsync(variant.Id, 2, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var orderRepoMock = new Mock<IOrderRepository>();
        var paymentRepoMock = new Mock<IPaymentRepository>();
        var paymentGatewayMock = new Mock<IPaymentGateway>();
        paymentGatewayMock.Setup(p => p.CreateSnapTransactionAsync(It.IsAny<CreateSnapTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSnapTransactionResult(true, "snap-token", "https://redirect.url"));

        var notificationServiceMock = new Mock<IOrderNotificationService>();
        var orderNumberGeneratorMock = new Mock<IOrderNumberGenerator>();
        orderNumberGeneratorMock.Setup(g => g.Generate(It.IsAny<DateTime>())).Returns("SPT-20261120-123456");

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.UtcNow).Returns(now);

        var metricsMock = new Mock<ISportShopMetrics>();

        var validatorMock = new Mock<IValidator<CheckoutOrderCommand>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CheckoutOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var handler = new CheckoutOrderCommandHandler(
            currentUserMock.Object,
            cartRepoMock.Object,
            addressRepoMock.Object,
            shippingRepoMock.Object,
            productRepoMock.Object,
            orderRepoMock.Object,
            paymentRepoMock.Object,
            paymentGatewayMock.Object,
            notificationServiceMock.Object,
            orderNumberGeneratorMock.Object,
            unitOfWorkMock.Object,
            clockMock.Object,
            metricsMock.Object,
            validatorMock.Object);

        // Act
        var result = await handler.HandleAsync(new CheckoutOrderCommand(addressId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        metricsMock.Verify(m => m.OrderCreated(), Times.Once);
        metricsMock.Verify(m => m.StockConflict(), Times.Never);
    }

    [Fact]
    public async Task Checkout_WhenStockInsufficient_RecordsStockConflictMetric()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.UserId).Returns(userId);

        var addressId = Guid.NewGuid();
        var address = new Address(
            addressId,
            userId,
            "Rumah",
            "Customer Test",
            "08123456789",
            "31",
            "Jakarta Selatan",
            "Kebayoran",
            "12345",
            "Jl. Senopati No. 1",
            isDefault: true);

        var addressRepoMock = new Mock<IAddressRepository>();
        addressRepoMock.Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        var category = new Category(Guid.NewGuid(), "Cat", "cat");
        var brand = new Brand(Guid.NewGuid(), "Brand", "brand");
        var product = new Product(Guid.NewGuid(), category.Id, brand.Id, "Product", "prod", "Desc", 100_000m);
        var variant = new ProductVariant(Guid.NewGuid(), product.Id, "SKU-001", 100_000m, 500, 10, "M", "Hitam");
        variant.Product = product;

        var now = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        var cart = new Domain.Carts.Cart(Guid.NewGuid(), userId);
        cart.AddItem(variant, 2, now);

        var cartRepoMock = new Mock<ICartRepository>();
        cartRepoMock.Setup(r => r.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var shippingRate = new ShippingRate("31", "DKI Jakarta", 10_000m, 5_000m, 1, 3);
        var shippingRepoMock = new Mock<IShippingRateRepository>();
        shippingRepoMock.Setup(r => r.GetByProvinceCodeAsync("31", It.IsAny<CancellationToken>()))
            .ReturnsAsync(shippingRate);

        var productRepoMock = new Mock<IProductRepository>();
        // Atomic reserve fails due to stock conflict!
        productRepoMock.Setup(r => r.ReserveStockAtomicAsync(variant.Id, 2, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.UtcNow).Returns(now);

        var metricsMock = new Mock<ISportShopMetrics>();

        var validatorMock = new Mock<IValidator<CheckoutOrderCommand>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CheckoutOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var orderNumberGeneratorMock = new Mock<IOrderNumberGenerator>();
        orderNumberGeneratorMock.Setup(g => g.Generate(It.IsAny<DateTime>())).Returns("SPT-20261120-123456");

        var handler = new CheckoutOrderCommandHandler(
            currentUserMock.Object,
            cartRepoMock.Object,
            addressRepoMock.Object,
            shippingRepoMock.Object,
            productRepoMock.Object,
            new Mock<IOrderRepository>().Object,
            new Mock<IPaymentRepository>().Object,
            new Mock<IPaymentGateway>().Object,
            new Mock<IOrderNotificationService>().Object,
            orderNumberGeneratorMock.Object,
            unitOfWorkMock.Object,
            clockMock.Object,
            metricsMock.Object,
            validatorMock.Object);

        // Act
        var result = await handler.HandleAsync(new CheckoutOrderCommand(addressId));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("stock.insufficient");
        metricsMock.Verify(m => m.StockConflict(), Times.Once);
        metricsMock.Verify(m => m.OrderCreated(), Times.Never);
    }
}
