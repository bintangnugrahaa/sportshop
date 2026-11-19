using FluentAssertions;
using Moq;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Shipping.DTOs;
using SportShop.Application.Features.Shipping.GetActiveRates;
using SportShop.Application.Features.Shipping.UpdateRate;
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
}
