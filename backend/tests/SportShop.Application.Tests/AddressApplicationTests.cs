using FluentAssertions;
using Moq;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Addresses.CreateAddress;
using SportShop.Application.Features.Addresses.DeleteAddress;
using SportShop.Application.Features.Addresses.GetAddress;
using SportShop.Application.Features.Addresses.SetDefault;
using SportShop.Application.Features.Addresses.UpdateAddress;
using SportShop.Application.Features.Shipping.GetQuote;
using SportShop.Domain.Carts;
using SportShop.Domain.Catalog;
using SportShop.Domain.Shipping;
using SportShop.Domain.Users;
using Xunit;

namespace SportShop.Application.Tests;

public sealed class AddressApplicationTests
{
    private readonly Mock<IAddressRepository> _addressRepoMock = new();
    private readonly Mock<IShippingRateRepository> _shippingRateRepoMock = new();
    private readonly Mock<ICartRepository> _cartRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IClock> _clockMock = new();

    public AddressApplicationTests()
    {
        _clockMock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc));
    }

    private static Address CreateTestAddress(Guid? userId = null, Guid? addressId = null, bool isDefault = false, bool isDeleted = false)
    {
        var addr = new Address(
            addressId ?? Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            "Rumah",
            "Penerima",
            "08123456789",
            "JK",
            "Jakarta",
            "Tebet",
            "12810",
            "Jl. Tebet Raya No. 10",
            isDefault);

        if (isDeleted)
        {
            addr.MarkDeleted(DateTime.UtcNow);
        }

        return addr;
    }

    [Fact]
    public async Task CreateAddress_ShouldSetDefault_WhenUserHasNoAddressesYet()
    {
        var userId = Guid.NewGuid();
        var province = new ShippingRate("JK", "DKI Jakarta", 10_000m, 5_000m, 1, 2);

        _shippingRateRepoMock.Setup(r => r.GetByProvinceCodeAsync("JK", It.IsAny<CancellationToken>()))
            .ReturnsAsync(province);

        _addressRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Address>()); // No existing addresses

        var sut = new CreateAddressCommandHandler(
            _addressRepoMock.Object,
            _shippingRateRepoMock.Object,
            _uowMock.Object,
            _clockMock.Object,
            new CreateAddressCommandValidator());

        var command = new CreateAddressCommand(
            userId, "Rumah", "Penerima", "08123456789", "JK", "Jakarta", "Tebet", "12810", "Jl. Tebet", false);

        var result = await sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsDefault.Should().BeTrue(); // Automatically default!
        _addressRepoMock.Verify(r => r.AddAsync(It.Is<Address>(a => a.IsDefault), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAddressById_ShouldReturnNotFound_WhenAddressBelongsToAnotherUser()
    {
        var ownerId = Guid.NewGuid();
        var intruderId = Guid.NewGuid();
        var address = CreateTestAddress(userId: ownerId);

        _addressRepoMock.Setup(r => r.GetByIdAsync(address.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        var sut = new GetAddressByIdQueryHandler(_addressRepoMock.Object);

        var result = await sut.HandleAsync(new GetAddressByIdQuery(intruderId, address.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("address.not_found");
    }

    [Fact]
    public async Task UpdateAddress_ShouldReturnNotFound_WhenAddressBelongsToAnotherUser()
    {
        var ownerId = Guid.NewGuid();
        var intruderId = Guid.NewGuid();
        var address = CreateTestAddress(userId: ownerId);

        _addressRepoMock.Setup(r => r.GetByIdAsync(address.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        var sut = new UpdateAddressCommandHandler(
            _addressRepoMock.Object,
            _shippingRateRepoMock.Object,
            _uowMock.Object,
            _clockMock.Object,
            new UpdateAddressCommandValidator());

        var command = new UpdateAddressCommand(
            intruderId, address.Id, "Kantor", "Budi", "0812", "JK", "Jakarta", null, "12810", "Jl. Tebet", false);

        var result = await sut.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteAddress_ShouldReturnNotFound_WhenAddressBelongsToAnotherUser()
    {
        var ownerId = Guid.NewGuid();
        var intruderId = Guid.NewGuid();
        var address = CreateTestAddress(userId: ownerId);

        _addressRepoMock.Setup(r => r.GetByIdAsync(address.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        var sut = new DeleteAddressCommandHandler(
            _addressRepoMock.Object,
            _uowMock.Object,
            _clockMock.Object);

        var result = await sut.HandleAsync(new DeleteAddressCommand(intruderId, address.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task SetDefault_ShouldUnsetOtherDefaults_AndSetThisAddress()
    {
        var userId = Guid.NewGuid();
        var address = CreateTestAddress(userId: userId, isDefault: false);

        _addressRepoMock.Setup(r => r.GetByIdAsync(address.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        var sut = new SetDefaultAddressCommandHandler(
            _addressRepoMock.Object,
            _uowMock.Object,
            _clockMock.Object);

        var result = await sut.HandleAsync(new SetDefaultAddressCommand(userId, address.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsDefault.Should().BeTrue();
        _addressRepoMock.Verify(r => r.UnsetDefaultAddressesAsync(userId, address.Id, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetShippingQuote_ShouldCalculateCorrectFee_ForCartItems()
    {
        var userId = Guid.NewGuid();
        var address = CreateTestAddress(userId: userId);

        var product = new Product(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Raket Tenis", "raket-tenis", "Deskripsi", 500_000m);
        var variant = new ProductVariant(Guid.NewGuid(), product.Id, "SKU-RKT-01", 500_000m, weightGram: 750, stockQuantity: 10);
        variant.Product = product;

        var cart = new Cart(Guid.NewGuid(), userId);
        cart.AddItem(variant, 2, DateTime.UtcNow); // 2 * 750g = 1500g -> ceil(1.5kg) = 2kg billable

        var shippingRate = new ShippingRate("JK", "DKI Jakarta", baseFee: 10_000m, perKgFee: 5_000m, 1, 2);

        _addressRepoMock.Setup(r => r.GetByIdAsync(address.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        _cartRepoMock.Setup(c => c.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _shippingRateRepoMock.Setup(r => r.GetByProvinceCodeAsync("JK", It.IsAny<CancellationToken>()))
            .ReturnsAsync(shippingRate);

        var sut = new GetShippingQuoteQueryHandler(
            _addressRepoMock.Object,
            _cartRepoMock.Object,
            _shippingRateRepoMock.Object,
            new GetShippingQuoteQueryValidator());

        var result = await sut.HandleAsync(new GetShippingQuoteQuery(userId, address.Id));

        result.IsSuccess.Should().BeTrue();
        var quote = result.Value;
        quote.TotalWeightGram.Should().Be(1500);
        quote.BillableWeightKg.Should().Be(2);
        quote.BaseFee.Should().Be(10_000m);
        quote.PerKgFee.Should().Be(5_000m);
        quote.ShippingFee.Should().Be(20_000m); // 10,000 + (2 * 5,000)
        quote.CartSubtotal.Should().Be(1_000_000m); // 2 * 500,000
        quote.EstimatedTotal.Should().Be(1_020_000m);
    }

    [Fact]
    public async Task ListAddresses_ShouldReturnUserAddresses()
    {
        var userId = Guid.NewGuid();
        var addresses = new List<Address> { CreateTestAddress(userId) };

        _addressRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);

        var sut = new SportShop.Application.Features.Addresses.ListAddresses.ListAddressesQueryHandler(_addressRepoMock.Object);
        var result = await sut.HandleAsync(new SportShop.Application.Features.Addresses.ListAddresses.ListAddressesQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateAddress_ShouldSucceed_WhenValid()
    {
        var userId = Guid.NewGuid();
        var address = CreateTestAddress(userId);
        var province = new ShippingRate("JB", "Jawa Barat", 12000m, 6000m, 1, 3);

        _addressRepoMock.Setup(r => r.GetByIdAsync(address.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);
        _shippingRateRepoMock.Setup(r => r.GetByProvinceCodeAsync("JB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(province);

        var sut = new UpdateAddressCommandHandler(
            _addressRepoMock.Object,
            _shippingRateRepoMock.Object,
            _uowMock.Object,
            _clockMock.Object,
            new UpdateAddressCommandValidator());

        var command = new UpdateAddressCommand(
            userId, address.Id, "Kantor", "Budi", "0812345678", "JB", "Bandung", "Coblong", "40132", "Jl. Dago", false);

        var result = await sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        address.Label.Should().Be("Kantor");
    }
}
