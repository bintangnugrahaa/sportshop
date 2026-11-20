using FluentAssertions;
using Moq;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Shipping.DTOs;
using SportShop.Application.Features.Shipping.GetActiveRates;
using SportShop.Application.Features.Shipping.UpdateRate;
using SportShop.Domain.Catalog;
using SportShop.Domain.Shipping;
using Xunit;

namespace SportShop.Application.Tests;

public sealed class ShippingRatesApplicationTests
{
    private readonly Mock<IShippingRateRepository> _repoMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IClock> _clockMock = new();

    public ShippingRatesApplicationTests()
    {
        _clockMock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task UpdateRate_ShouldReturnValidationError_WhenBaseFeeIsNegative()
    {
        var sut = new UpdateShippingRateCommandHandler(
            _repoMock.Object,
            _cacheMock.Object,
            _uowMock.Object,
            _clockMock.Object,
            new UpdateShippingRateCommandValidator());

        var command = new UpdateShippingRateCommand("JK", -10_000m, 5_000m, 1, 2, true);
        var result = await sut.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task UpdateRate_ShouldReturnNotFound_WhenProvinceDoesNotExist()
    {
        _repoMock.Setup(r => r.GetByProvinceCodeAsync("XX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingRate?)null);

        var sut = new UpdateShippingRateCommandHandler(
            _repoMock.Object,
            _cacheMock.Object,
            _uowMock.Object,
            _clockMock.Object,
            new UpdateShippingRateCommandValidator());

        var command = new UpdateShippingRateCommand("XX", 10_000m, 5_000m, 1, 2, true);
        var result = await sut.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateRate_ShouldUpdateRate_AndInvalidateShipRatesCache()
    {
        var existingRate = new ShippingRate("JK", "DKI Jakarta", 10_000m, 5_000m, 1, 2, true);

        _repoMock.Setup(r => r.GetByProvinceCodeAsync("JK", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRate);

        var sut = new UpdateShippingRateCommandHandler(
            _repoMock.Object,
            _cacheMock.Object,
            _uowMock.Object,
            _clockMock.Object,
            new UpdateShippingRateCommandValidator());

        var command = new UpdateShippingRateCommand("JK", 12_000m, 6_000m, 1, 3, true);
        var result = await sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.BaseFee.Should().Be(12_000m);
        result.Value.PerKgFee.Should().Be(6_000m);
        result.Value.EstimatedDaysMax.Should().Be((short)3);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("ship:rates:v1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActiveRates_ShouldReturnFromCache_WhenCacheHit()
    {
        var cached = new List<ShippingRateDto>
        {
            new("JK", "DKI Jakarta", 10_000m, 5_000m, 1, 2, true, null)
        };

        _cacheMock.Setup(c => c.GetAsync<List<ShippingRateDto>>("ship:rates:v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var sut = new GetActiveShippingRatesQueryHandler(_repoMock.Object, _cacheMock.Object);

        var result = await sut.HandleAsync(new GetActiveShippingRatesQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cached);
        _repoMock.Verify(r => r.GetActiveRatesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetActiveRates_ShouldFetchAndPopulateCache_WhenCacheMiss()
    {
        _cacheMock.Setup(c => c.GetAsync<List<ShippingRateDto>>("ship:rates:v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<ShippingRateDto>?)null);

        var ratesFromDb = new List<ShippingRate>
        {
            new("JK", "DKI Jakarta", 10_000m, 5_000m, 1, 2, true)
        };

        _repoMock.Setup(r => r.GetActiveRatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ratesFromDb);

        var sut = new GetActiveShippingRatesQueryHandler(_repoMock.Object, _cacheMock.Object);

        var result = await sut.HandleAsync(new GetActiveShippingRatesQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        _cacheMock.Verify(c => c.SetAsync("ship:rates:v1", It.IsAny<List<ShippingRateDto>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetShippingRate_ShouldReturnRate_WhenFound()
    {
        var rate = new ShippingRate("JK", "DKI Jakarta", 10000m, 5000m, 1, 2, true);
        _repoMock.Setup(r => r.GetByProvinceCodeAsync("JK", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rate);

        var sut = new SportShop.Application.Features.Shipping.GetRate.GetShippingRateQueryHandler(_repoMock.Object);
        var result = await sut.HandleAsync(new SportShop.Application.Features.Shipping.GetRate.GetShippingRateQuery("JK"));

        result.IsSuccess.Should().BeTrue();
        result.Value.ProvinceName.Should().Be("DKI Jakarta");
    }

    [Fact]
    public async Task GetShippingRate_ShouldReturnNotFound_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByProvinceCodeAsync("XX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingRate?)null);

        var sut = new SportShop.Application.Features.Shipping.GetRate.GetShippingRateQueryHandler(_repoMock.Object);
        var result = await sut.HandleAsync(new SportShop.Application.Features.Shipping.GetRate.GetShippingRateQuery("XX"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("shipping.rate_not_found");
    }

    [Fact]
    public async Task ListShippingRates_ShouldReturnAllRates()
    {
        var rates = new List<ShippingRate>
        {
            new("JK", "DKI Jakarta", 10000m, 5000m, 1, 2, true),
            new("JB", "Jawa Barat", 12000m, 6000m, 1, 3, true)
        };
        _repoMock.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);

        var sut = new SportShop.Application.Features.Shipping.ListRates.ListShippingRatesQueryHandler(_repoMock.Object);
        var result = await sut.HandleAsync(new SportShop.Application.Features.Shipping.ListRates.ListShippingRatesQuery(true));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetShippingQuote_ShouldCalculateQuote_WhenValid()
    {
        var userId = Guid.NewGuid();
        var addressRepo = new Mock<IAddressRepository>();
        var cartRepo = new Mock<ICartRepository>();
        var validator = new SportShop.Application.Features.Shipping.GetQuote.GetShippingQuoteQueryValidator();

        var address = new SportShop.Domain.Users.Address(
            Guid.NewGuid(), userId, "Rumah", "Budi", "0812345678", "JK", "Jakarta", "Jaksel", "12345", "Jl. Melati");

        var cart = new SportShop.Domain.Carts.Cart(Guid.NewGuid(), userId);
        var variant = new ProductVariant(Guid.NewGuid(), Guid.NewGuid(), "SKU-01", 100000m, 500, 10);
        cart.AddItem(variant, 2, DateTime.UtcNow);

        var rate = new ShippingRate("JK", "DKI Jakarta", 10000m, 5000m, 1, 2, true);

        addressRepo.Setup(a => a.GetByIdAsync(address.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);
        cartRepo.Setup(c => c.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        _repoMock.Setup(r => r.GetByProvinceCodeAsync("JK", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rate);

        var sut = new SportShop.Application.Features.Shipping.GetQuote.GetShippingQuoteQueryHandler(
            addressRepo.Object, cartRepo.Object, _repoMock.Object, validator);

        var result = await sut.HandleAsync(new SportShop.Application.Features.Shipping.GetQuote.GetShippingQuoteQuery(userId, address.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.ShippingFee.Should().Be(15000m); // 10000 + ceil(1000g/1000)*5000
    }
}
