using FluentAssertions;
using SportShop.Domain.Common;
using SportShop.Domain.Shipping;
using Xunit;

namespace SportShop.Domain.Tests;

public sealed class ShippingCalculatorTests
{
    private static ShippingRate CreateRate(
        decimal baseFee = 10_000m,
        decimal perKgFee = 5_000m,
        bool isActive = true)
    {
        return new ShippingRate(
            "JK",
            "DKI Jakarta",
            baseFee,
            perKgFee,
            1,
            2,
            isActive);
    }

    [Theory]
    [InlineData(0, 1)]      // 0 gram -> minimum 1 kg
    [InlineData(1, 1)]      // 1 gram -> minimum 1 kg
    [InlineData(500, 1)]    // 500 gram -> minimum 1 kg
    [InlineData(999, 1)]    // 999 gram -> minimum 1 kg
    [InlineData(1000, 1)]   // 1000 gram (exact 1 kg) -> 1 kg
    [InlineData(1001, 2)]   // 1001 gram (boundary over 1 kg) -> ceil gives 2 kg
    [InlineData(1500, 2)]   // 1500 gram -> ceil gives 2 kg
    [InlineData(2000, 2)]   // 2000 gram (exact 2 kg) -> 2 kg
    [InlineData(2001, 3)]   // 2001 gram (boundary over 2 kg) -> ceil gives 3 kg
    [InlineData(3200, 4)]   // 3200 gram -> ceil gives 4 kg
    [InlineData(5000, 5)]   // 5000 gram (exact 5 kg) -> 5 kg
    [InlineData(5001, 6)]   // 5001 gram (boundary over 5 kg) -> 6 kg
    public void Calculate_ShouldDetermineCorrectBillableWeightKg(int totalWeightGram, int expectedBillableKg)
    {
        var rate = CreateRate(baseFee: 10_000m, perKgFee: 5_000m);

        var result = ShippingCalculator.Calculate(rate, totalWeightGram);

        result.BillableWeightKg.Should().Be(expectedBillableKg);
        result.TotalWeightGram.Should().Be(totalWeightGram);
        result.TotalFee.Should().Be(10_000m + (expectedBillableKg * 5_000m));
        result.ProvinceCode.Should().Be("JK");
        result.ProvinceName.Should().Be("DKI Jakarta");
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenTotalWeightIsNegative()
    {
        var rate = CreateRate();

        var act = () => ShippingCalculator.Calculate(rate, -100);

        act.Should().Throw<DomainException>()
            .Where(e => e.Code == "shipping.invalid_weight");
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenRateIsInactive()
    {
        var rate = CreateRate(isActive: false);

        var act = () => ShippingCalculator.Calculate(rate, 1500);

        act.Should().Throw<DomainException>()
            .Where(e => e.Code == "shipping.rate_inactive");
    }

    [Fact]
    public void ShippingRate_Creation_ShouldValidateInputBounds()
    {
        var actNegativeBase = () => new ShippingRate("JK", "DKI Jakarta", -1m, 5000m, 1, 2);
        actNegativeBase.Should().Throw<DomainException>().Where(e => e.Code == "shipping.invalid_base_fee");

        var actNegativePerKg = () => new ShippingRate("JK", "DKI Jakarta", 10000m, -1m, 1, 2);
        actNegativePerKg.Should().Throw<DomainException>().Where(e => e.Code == "shipping.invalid_per_kg_fee");

        var actInvalidDays = () => new ShippingRate("JK", "DKI Jakarta", 10000m, 5000m, 3, 1);
        actInvalidDays.Should().Throw<DomainException>().Where(e => e.Code == "shipping.invalid_estimated_days");
    }
}
