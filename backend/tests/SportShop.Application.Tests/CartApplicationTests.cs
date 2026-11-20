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

    [Fact]
    public void ToDto_ShouldMarkPriceChanged_WhenVariantPriceChanges()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var variant = CreateTestVariant(stock: 10, price: 100_000m);
        var item = cart.AddItem(variant, 2, DateTime.UtcNow);

        // Price changed from 100,000 to 125,000
        variant.Price = 125_000m;

        var dto = SportShop.Application.Mapping.CartMappingExtensions.ToDto(cart);

        dto.HasIssues.Should().BeTrue();
        var itemDto = dto.Items[0];
        itemDto.IsPriceChanged.Should().BeTrue();
        itemDto.PriceAtAdd.Should().Be(100_000m);
        itemDto.UnitPrice.Should().Be(125_000m);
        itemDto.StatusNote.Should().Contain("Harga berubah");
        itemDto.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void ToDto_ShouldMarkStockInsufficient_WhenVariantStockDecreasesBelowCartQuantity()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var variant = CreateTestVariant(stock: 10, price: 100_000m);
        cart.AddItem(variant, 5, DateTime.UtcNow);

        // Stock decreased to 2 (e.g. bought by other users)
        variant.StockQuantity = 2;

        var dto = SportShop.Application.Mapping.CartMappingExtensions.ToDto(cart);

        dto.HasIssues.Should().BeTrue();
        var itemDto = dto.Items[0];
        itemDto.IsStockInsufficient.Should().BeTrue();
        itemDto.IsAvailable.Should().BeFalse();
        itemDto.StatusNote.Should().Contain("Stok tidak mencukupi");
        dto.Subtotal.Should().Be(0); // Excluded from subtotal
    }

    [Fact]
    public void ToDto_ShouldMarkInactive_WhenVariantOrProductDeactivated()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var variant = CreateTestVariant(stock: 10, price: 100_000m);
        cart.AddItem(variant, 2, DateTime.UtcNow);

        variant.IsActive = false;

        var dto = SportShop.Application.Mapping.CartMappingExtensions.ToDto(cart);

        dto.HasIssues.Should().BeTrue();
        var itemDto = dto.Items[0];
        itemDto.IsActive.Should().BeFalse();
        itemDto.IsAvailable.Should().BeFalse();
        itemDto.StatusNote.Should().Contain("tidak aktif");
        dto.Subtotal.Should().Be(0);
    }

    [Fact]
    public async Task GetCart_ShouldReturnCartDto_WhenRequested()
    {
        var userId = Guid.NewGuid();
        var cart = new Cart(Guid.NewGuid(), userId);
        _cartRepoMock.Setup(c => c.GetOrCreateByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var handler = new SportShop.Application.Features.Cart.GetCart.GetCartQueryHandler(_cartRepoMock.Object);
        var result = await handler.HandleAsync(new SportShop.Application.Features.Cart.GetCart.GetCartQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task RemoveCartItem_ShouldReturnNotFound_WhenCartNull()
    {
        var userId = Guid.NewGuid();
        _cartRepoMock.Setup(c => c.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        var handler = new RemoveCartItemCommandHandler(_cartRepoMock.Object, _uowMock.Object, _clockMock.Object);
        var result = await handler.HandleAsync(new RemoveCartItemCommand(userId, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task RemoveCartItem_ShouldRemoveItemAndSave_WhenItemExists()
    {
        var userId = Guid.NewGuid();
        var cart = new Cart(Guid.NewGuid(), userId);
        var variant = CreateTestVariant();
        cart.AddItem(variant, 2, DateTime.UtcNow);

        _cartRepoMock.Setup(c => c.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var handler = new RemoveCartItemCommandHandler(_cartRepoMock.Object, _uowMock.Object, _clockMock.Object);
        var result = await handler.HandleAsync(new RemoveCartItemCommand(userId, variant.Id));

        result.IsSuccess.Should().BeTrue();
        _cartRepoMock.Verify(c => c.RemoveItemAsync(It.IsAny<CartItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateItem_ShouldReturnNotFound_WhenCartOrItemOrVariantMissing()
    {
        var userId = Guid.NewGuid();
        var validator = new UpdateCartItemCommandValidator();
        var handler = new UpdateCartItemCommandHandler(
            _cartRepoMock.Object, _productRepoMock.Object, _uowMock.Object, _clockMock.Object, validator);

        // Validation error
        var valResult = await handler.HandleAsync(new UpdateCartItemCommand(userId, Guid.NewGuid(), -1));
        valResult.IsFailure.Should().BeTrue();
        valResult.Error.Type.Should().Be(ErrorType.Validation);

        // Cart null
        _cartRepoMock.Setup(c => c.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);
        var cartNullResult = await handler.HandleAsync(new UpdateCartItemCommand(userId, Guid.NewGuid(), 2));
        cartNullResult.IsFailure.Should().BeTrue();

        // Item missing in cart
        var cart = new Cart(Guid.NewGuid(), userId);
        _cartRepoMock.Setup(c => c.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        var itemMissingResult = await handler.HandleAsync(new UpdateCartItemCommand(userId, Guid.NewGuid(), 2));
        itemMissingResult.IsFailure.Should().BeTrue();

        // Variant not found
        var variant = CreateTestVariant();
        cart.AddItem(variant, 1, DateTime.UtcNow);
        _productRepoMock.Setup(p => p.GetVariantByIdAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductVariant?)null);
        var varNullResult = await handler.HandleAsync(new UpdateCartItemCommand(userId, variant.Id, 2));
        varNullResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateItem_ShouldSucceed_WhenValid()
    {
        var userId = Guid.NewGuid();
        var cart = new Cart(Guid.NewGuid(), userId);
        var variant = CreateTestVariant(stock: 10);
        cart.AddItem(variant, 1, DateTime.UtcNow);

        _cartRepoMock.Setup(c => c.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        _productRepoMock.Setup(p => p.GetVariantByIdAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);

        var validator = new UpdateCartItemCommandValidator();
        var handler = new UpdateCartItemCommandHandler(
            _cartRepoMock.Object, _productRepoMock.Object, _uowMock.Object, _clockMock.Object, validator);

        var result = await handler.HandleAsync(new UpdateCartItemCommand(userId, variant.Id, 3));
        result.IsSuccess.Should().BeTrue();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
