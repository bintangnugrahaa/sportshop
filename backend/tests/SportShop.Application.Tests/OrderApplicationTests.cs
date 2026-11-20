using FluentAssertions;
using Moq;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Orders.Admin.CancelAdminOrder;
using SportShop.Application.Features.Orders.Admin.CompleteOrder;
using SportShop.Application.Features.Orders.Admin.GetAdminOrder;
using SportShop.Application.Features.Orders.Admin.ListAdminOrders;
using SportShop.Application.Features.Orders.Admin.ProcessOrder;
using SportShop.Application.Features.Orders.Admin.ShipOrder;
using SportShop.Application.Features.Orders.CancelOrder;
using SportShop.Application.Features.Orders.Checkout;
using SportShop.Application.Features.Orders.GetCustomerOrder;
using SportShop.Application.Features.Orders.ListCustomerOrders;
using SportShop.Application.Features.Orders.PayOrder;
using SportShop.Domain.Orders;
using SportShop.Domain.Payments;
using SportShop.Domain.Users;
using Xunit;

namespace SportShop.Application.Tests;

public sealed class OrderApplicationTests
{
    private static readonly string[] _roles = ["Customer"];

    private static Order CreateTestOrder(Guid userId, string orderNumber = "SPT-20261120-0001", OrderStatus status = OrderStatus.PendingPayment)
    {
        var address = new OrderShippingAddress("Budi", "0812345678", "JK", "Jakarta", "Jaksel", "12345", "Jl. Melati");
        var order = new Order(Guid.NewGuid(), orderNumber, userId, address, 500, 10000m, DateTime.UtcNow.AddDays(1));

        var variantId = Guid.NewGuid();
        order.AddItem(new OrderItem(Guid.NewGuid(), order.Id, variantId, "Sepatu Lari", "42 / Hitam", "SKU-01", "https://img.png", 100000m, 1));

        if (status == OrderStatus.Paid)
        {
            order.MarkPaid(DateTime.UtcNow);
        }
        else if (status == OrderStatus.Processing)
        {
            order.MarkPaid(DateTime.UtcNow);
            order.Process(DateTime.UtcNow);
        }
        else if (status == OrderStatus.Shipped)
        {
            order.MarkPaid(DateTime.UtcNow);
            order.Process(DateTime.UtcNow);
            order.Ship("JNE", "JNE123456", DateTime.UtcNow);
        }
        else if (status == OrderStatus.Completed)
        {
            order.MarkPaid(DateTime.UtcNow);
            order.Process(DateTime.UtcNow);
            order.Ship("JNE", "JNE123456", DateTime.UtcNow);
            order.Complete(DateTime.UtcNow);
        }

        return order;
    }

    [Fact]
    public async Task Checkout_ShouldFail_WhenAddressNotFound()
    {
        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var cartRepo = new Mock<ICartRepository>();
        var addressRepo = new Mock<IAddressRepository>();
        addressRepo.Setup(a => a.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address?)null);

        var shippingRateRepo = new Mock<IShippingRateRepository>();
        var productRepo = new Mock<IProductRepository>();
        var orderRepo = new Mock<IOrderRepository>();
        var paymentRepo = new Mock<IPaymentRepository>();
        var gateway = new Mock<IPaymentGateway>();
        var notifService = new Mock<IOrderNotificationService>();
        var numGen = new Mock<IOrderNumberGenerator>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        var metrics = new Mock<ISportShopMetrics>();
        var validator = new CheckoutOrderCommandValidator();

        var handler = new CheckoutOrderCommandHandler(
            currentUser.Object, cartRepo.Object, addressRepo.Object, shippingRateRepo.Object,
            productRepo.Object, orderRepo.Object, paymentRepo.Object, gateway.Object,
            notifService.Object, numGen.Object, uow.Object, clock.Object, metrics.Object, validator);

        var result = await handler.HandleAsync(new CheckoutOrderCommand(Guid.NewGuid()));
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("address.not_found");
    }

    [Fact]
    public async Task Checkout_ShouldSucceed_WhenCartAndAddressValid()
    {
        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(c => c.UserId).Returns(userId);
        currentUser.Setup(c => c.Email).Returns("budi@example.com");

        var cartRepo = new Mock<ICartRepository>();
        var addressRepo = new Mock<IAddressRepository>();
        var shippingRateRepo = new Mock<IShippingRateRepository>();
        var productRepo = new Mock<IProductRepository>();
        var orderRepo = new Mock<IOrderRepository>();
        var paymentRepo = new Mock<IPaymentRepository>();
        var gateway = new Mock<IPaymentGateway>();
        var notifService = new Mock<IOrderNotificationService>();
        var numGen = new Mock<IOrderNumberGenerator>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        var now = DateTime.UtcNow;
        clock.Setup(c => c.UtcNow).Returns(now);
        var metrics = new Mock<ISportShopMetrics>();
        var validator = new CheckoutOrderCommandValidator();

        var address = new Address(
            Guid.NewGuid(), userId, "Rumah", "Budi", "0812345678", "JK", "Jakarta", "Jaksel", "12345", "Jl. Melati");
        addressRepo.Setup(a => a.GetByIdAsync(address.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        var cart = new SportShop.Domain.Carts.Cart(Guid.NewGuid(), userId);
        var prod = new SportShop.Domain.Catalog.Product(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sepatu", "sepatu", "Desc", 100000m);
        var variant = new SportShop.Domain.Catalog.ProductVariant(Guid.NewGuid(), prod.Id, "SKU-01", 100000m, 500, 10);
        variant.Product = prod;
        cart.AddItem(variant, 1, now);

        cartRepo.Setup(c => c.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var rate = new SportShop.Domain.Shipping.ShippingRate("JK", "DKI Jakarta", 10000m, 5000m, 1, 2, true);
        shippingRateRepo.Setup(s => s.GetByProvinceCodeAsync("JK", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rate);

        numGen.Setup(n => n.Generate(now)).Returns("SPT-20261120-9999");
        productRepo.Setup(p => p.ReserveStockAtomicAsync(variant.Id, 1, now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        gateway.Setup(g => g.CreateSnapTransactionAsync(It.IsAny<CreateSnapTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSnapTransactionResult(true, "snap-token-xyz", "https://redirect.url"));

        var handler = new CheckoutOrderCommandHandler(
            currentUser.Object, cartRepo.Object, addressRepo.Object, shippingRateRepo.Object,
            productRepo.Object, orderRepo.Object, paymentRepo.Object, gateway.Object,
            notifService.Object, numGen.Object, uow.Object, clock.Object, metrics.Object, validator);

        var result = await handler.HandleAsync(new CheckoutOrderCommand(address.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderNumber.Should().Be("SPT-20261120-9999");
        result.Value.Payment.Should().NotBeNull();
        result.Value.Payment!.SnapToken.Should().Be("snap-token-xyz");
    }

    [Fact]
    public async Task GetCustomerOrder_ShouldReturnNotFound_WhenWrongUser()
    {
        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var orderRepo = new Mock<IOrderRepository>();
        var paymentRepo = new Mock<IPaymentRepository>();
        var validator = new GetCustomerOrderByNumberQueryValidator();

        var otherUserOrder = CreateTestOrder(Guid.NewGuid());
        orderRepo.Setup(o => o.GetByOrderNumberAsync("SPT-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherUserOrder);

        var handler = new GetCustomerOrderByNumberQueryHandler(currentUser.Object, orderRepo.Object, paymentRepo.Object, validator);
        var result = await handler.HandleAsync(new GetCustomerOrderByNumberQuery("SPT-1"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("order.not_found");
    }

    [Fact]
    public async Task GetCustomerOrder_ShouldReturnOrder_WhenMatchesUser()
    {
        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var orderRepo = new Mock<IOrderRepository>();
        var paymentRepo = new Mock<IPaymentRepository>();
        var validator = new GetCustomerOrderByNumberQueryValidator();

        var order = CreateTestOrder(userId);
        orderRepo.Setup(o => o.GetByOrderNumberAsync("SPT-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new GetCustomerOrderByNumberQueryHandler(currentUser.Object, orderRepo.Object, paymentRepo.Object, validator);
        var result = await handler.HandleAsync(new GetCustomerOrderByNumberQuery("SPT-1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderNumber.Should().Be(order.OrderNumber);
    }

    [Fact]
    public async Task ListCustomerOrders_ShouldReturnPagedOrders()
    {
        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var orderRepo = new Mock<IOrderRepository>();
        var validator = new ListCustomerOrdersQueryValidator();

        var orders = new List<Order> { CreateTestOrder(userId) };
        orderRepo.Setup(o => o.GetCustomerOrdersPagedAsync(userId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((orders, 1));

        var handler = new ListCustomerOrdersQueryHandler(currentUser.Object, orderRepo.Object, validator);
        var result = await handler.HandleAsync(new ListCustomerOrdersQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task CancelCustomerOrder_ShouldSucceed_WhenOrderPending()
    {
        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var orderRepo = new Mock<IOrderRepository>();
        var productRepo = new Mock<IProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new CancelCustomerOrderCommandValidator();

        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        var order = CreateTestOrder(userId);
        orderRepo.Setup(o => o.GetByOrderNumberAsync("SPT-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        productRepo.Setup(p => p.RestoreStockAtomicAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CancelCustomerOrderCommandHandler(currentUser.Object, orderRepo.Object, productRepo.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new CancelCustomerOrderCommand("SPT-1", "Berubah pikiran"));

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task PayOrder_ShouldReturnSnapToken_WhenOrderPending()
    {
        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var orderRepo = new Mock<IOrderRepository>();
        var paymentRepo = new Mock<IPaymentRepository>();
        var gateway = new Mock<IPaymentGateway>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new PayOrderCommandValidator();

        var order = CreateTestOrder(userId);
        var payment = new Payment(Guid.NewGuid(), order.Id, order.Total, "existing-token", "https://redirect.url");

        orderRepo.Setup(o => o.GetByOrderNumberAsync("SPT-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        paymentRepo.Setup(p => p.GetActivePaymentByOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var handler = new PayOrderCommandHandler(currentUser.Object, orderRepo.Object, paymentRepo.Object, gateway.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new PayOrderCommand("SPT-1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.SnapToken.Should().Be("existing-token");
    }

    [Fact]
    public async Task ProcessOrder_ShouldChangeStatusToProcessing_WhenOrderPaid()
    {
        var currentUser = new Mock<ICurrentUser>();
        var orderRepo = new Mock<IOrderRepository>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new ProcessOrderCommandValidator();

        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        var order = CreateTestOrder(Guid.NewGuid(), status: OrderStatus.Paid);
        orderRepo.Setup(o => o.GetByOrderNumberAsync("SPT-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new ProcessOrderCommandHandler(currentUser.Object, orderRepo.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new ProcessOrderCommand("SPT-1"));

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public async Task ShipOrder_ShouldChangeStatusToShipped_WhenOrderProcessing()
    {
        var currentUser = new Mock<ICurrentUser>();
        var orderRepo = new Mock<IOrderRepository>();
        var notifService = new Mock<IOrderNotificationService>();
        var identityService = new Mock<IIdentityService>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new ShipOrderCommandValidator();

        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        var order = CreateTestOrder(Guid.NewGuid(), status: OrderStatus.Processing);
        orderRepo.Setup(o => o.GetByOrderNumberAsync("SPT-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        identityService.Setup(i => i.GetUserProfileAsync(order.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserProfileResult(order.UserId, "customer@test.com", "Budi", "0812", _roles, DateTime.UtcNow)));

        var handler = new ShipOrderCommandHandler(currentUser.Object, orderRepo.Object, notifService.Object, identityService.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new ShipOrderCommand("SPT-1", "JNE", "JNE123456789"));

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);
        order.Courier.Should().Be("JNE");
        order.TrackingNumber.Should().Be("JNE123456789");
    }

    [Fact]
    public async Task CompleteOrder_ShouldChangeStatusToCompleted_WhenOrderShipped()
    {
        var currentUser = new Mock<ICurrentUser>();
        var orderRepo = new Mock<IOrderRepository>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new CompleteOrderCommandValidator();

        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        var order = CreateTestOrder(Guid.NewGuid(), status: OrderStatus.Shipped);
        orderRepo.Setup(o => o.GetByOrderNumberAsync("SPT-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new CompleteOrderCommandHandler(currentUser.Object, orderRepo.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new CompleteOrderCommand("SPT-1"));

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public async Task CancelAdminOrder_ShouldCancelAndRestoreStock()
    {
        var currentUser = new Mock<ICurrentUser>();
        var orderRepo = new Mock<IOrderRepository>();
        var productRepo = new Mock<IProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new CancelAdminOrderCommandValidator();

        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        var order = CreateTestOrder(Guid.NewGuid(), status: OrderStatus.Paid);
        orderRepo.Setup(o => o.GetByOrderNumberAsync("SPT-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        productRepo.Setup(p => p.RestoreStockAtomicAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CancelAdminOrderCommandHandler(currentUser.Object, orderRepo.Object, productRepo.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new CancelAdminOrderCommand("SPT-1", "Stok rusak"));

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task GetAdminOrder_ShouldReturnOrder_WhenExists()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var validator = new GetAdminOrderByNumberQueryValidator();

        var order = CreateTestOrder(Guid.NewGuid());
        orderRepo.Setup(o => o.GetByOrderNumberAsync("SPT-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new GetAdminOrderByNumberQueryHandler(orderRepo.Object, validator);
        var result = await handler.HandleAsync(new GetAdminOrderByNumberQuery("SPT-1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderNumber.Should().Be(order.OrderNumber);
    }

    [Fact]
    public async Task ListAdminOrders_ShouldReturnPagedOrders()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var validator = new ListAdminOrdersQueryValidator();

        var orders = new List<Order> { CreateTestOrder(Guid.NewGuid()) };
        orderRepo.Setup(o => o.GetAdminOrdersPagedAsync(null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((orders, 1));

        var handler = new ListAdminOrdersQueryHandler(orderRepo.Object, validator);
        var result = await handler.HandleAsync(new ListAdminOrdersQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }
}
