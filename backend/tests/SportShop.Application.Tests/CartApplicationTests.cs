using FluentAssertions;
using Moq;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Cart.AddItem;
using SportShop.Application.Features.Cart.Clear;
using SportShop.Application.Features.Cart.RemoveItem;
using SportShop.Application.Features.Cart.UpdateItem;
using SportShop.Domain.Carts;
using SportShop.Domain.Catalog;
using Xunit;

namespace SportShop.Application.Tests;

public sealed class CartApplicationTests
{
    private readonly Mock<ICartRepository> _cartRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IClock> _clockMock = new();

    public CartApplicationTests()
    {
        _clockMock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc));
    }

    private static ProductVariant CreateTestVariant(int stock = 10, decimal price = 250_000m, bool isActive = true)
    {
        var product = new Product(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Sepatu Olahraga",
            "sepatu-olahraga",
            "Deskripsi",
            price,
            isActive: true);

        var variant = new ProductVariant(
            Guid.NewGuid(),
            product.Id,
            "SKU-SHOE-01",
            price,
            800,
            stock,
            "42",
            "Merah",
            isActive);

        variant.Product = product;
        return variant;
    }

    [Fact]
    public async Task AddItem_ShouldReturnValidationError_WhenQuantityIsZeroOrNegative()
    {
        var sut = new AddCartItemCommandHandler(
            _cartRepoMock.Object,
            _productRepoMock.Object,
            _uowMock.Object,
            _clockMock.Object,
            new AddCartItemCommandValidator());

        var result = await sut.HandleAsync(new AddCartItemCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task AddItem_ShouldReturnNotFound_WhenVariantDoesNotExist()
    {
        _productRepoMock.Setup(p => p.GetVariantByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductVariant?)null);

        var sut = new AddCartItemCommandHandler(
            _cartRepoMock.Object,
            _productRepoMock.Object,
            _uowMock.Object,
            _clockMock.Object,
            new AddCartItemCommandValidator());

        var result = await sut.HandleAsync(new AddCartItemCommand(Guid.NewGuid(), Guid.NewGuid(), 2));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task AddItem_ShouldReturnConflict_WhenQuantityExceedsStock()
    {
        var userId = Guid.NewGuid();
        var cart = new Cart(Guid.NewGuid(), userId);
        var variant = CreateTestVariant(stock: 3);

        _productRepoMock.Setup(p => p.GetVariantByIdAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);

        _cartRepoMock.Setup(c => c.GetOrCreateByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var sut = new AddCartItemCommandHandler(
            _cartRepoMock.Object,
            _productRepoMock.Object,
            _uowMock.Object,
            _clockMock.Object,
            new AddCartItemCommandValidator());

        var result = await sut.HandleAsync(new AddCartItemCommand(userId, variant.Id, 5));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("stock.insufficient");
    }

    [Fact]
    public async Task UpdateItem_ShouldReturnConflict_WhenNewQuantityExceedsStock()
    {
        var userId = Guid.NewGuid();
        var cart = new Cart(Guid.NewGuid(), userId);
        var variant = CreateTestVariant(stock: 4);
        cart.AddItem(variant, 2, DateTime.UtcNow);

        _cartRepoMock.Setup(c => c.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _productRepoMock.Setup(p => p.GetVariantByIdAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);

        var sut = new UpdateCartItemCommandHandler(
            _cartRepoMock.Object,
            _productRepoMock.Object,
            _uowMock.Object,
            _clockMock.Object,
            new UpdateCartItemCommandValidator());

        var result = await sut.HandleAsync(new UpdateCartItemCommand(userId, variant.Id, 10));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task ClearCart_ShouldInvokeRepositoryAndUnitOfWork()
    {
        var userId = Guid.NewGuid();
        var cart = new Cart(Guid.NewGuid(), userId);
        var variant = CreateTestVariant(stock: 4);
        cart.AddItem(variant, 1, DateTime.UtcNow);

        _cartRepoMock.Setup(c => c.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var sut = new ClearCartCommandHandler(
            _cartRepoMock.Object,
            _uowMock.Object,
            _clockMock.Object);

        var result = await sut.HandleAsync(new ClearCartCommand(userId));

        result.IsSuccess.Should().BeTrue();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
