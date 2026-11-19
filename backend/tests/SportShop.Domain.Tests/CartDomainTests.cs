using FluentAssertions;
using SportShop.Domain.Carts;
using SportShop.Domain.Catalog;
using SportShop.Domain.Common;
using Xunit;

namespace SportShop.Domain.Tests;

public sealed class CartDomainTests
{
    private static ProductVariant CreateVariant(int stock = 10, decimal price = 100_000m, bool isActive = true)
    {
        return new ProductVariant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SKU-TEST-001",
            price,
            500,
            stock,
            "L",
            "Hitam",
            isActive);
    }

    [Fact]
    public void Cart_AddItem_ShouldThrow_WhenQuantityIsZeroOrNegative()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var variant = CreateVariant(stock: 5);

        var actZero = () => cart.AddItem(variant, 0, DateTime.UtcNow);
        var actNegative = () => cart.AddItem(variant, -1, DateTime.UtcNow);

        actZero.Should().Throw<DomainException>().Where(e => e.Code == "cart.invalid_quantity");
        actNegative.Should().Throw<DomainException>().Where(e => e.Code == "cart.invalid_quantity");
    }

    [Fact]
    public void Cart_AddItem_ShouldThrow_WhenVariantIsInactive()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var variant = CreateVariant(stock: 10, isActive: false);

        var act = () => cart.AddItem(variant, 1, DateTime.UtcNow);

        act.Should().Throw<DomainException>().Where(e => e.Code == "cart.variant_inactive");
    }

    [Fact]
    public void Cart_AddItem_ShouldThrow_WhenQuantityExceedsStock()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var variant = CreateVariant(stock: 3);

        var act = () => cart.AddItem(variant, 4, DateTime.UtcNow);

        act.Should().Throw<DomainException>().Where(e => e.Code == "stock.insufficient");
    }

    [Fact]
    public void Cart_AddItem_ShouldSucceed_AndStorePriceAtAdd()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var variant = CreateVariant(stock: 5, price: 150_000m);
        var now = DateTime.UtcNow;

        var item = cart.AddItem(variant, 2, now);

        cart.Items.Should().HaveCount(1);
        item.Quantity.Should().Be(2);
        item.PriceAtAdd.Should().Be(150_000m);
        item.VariantId.Should().Be(variant.Id);
    }

    [Fact]
    public void Cart_AddItem_WhenAlreadyInCart_ShouldIncreaseQuantity_OrThrowIfExceedsStock()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var variant = CreateVariant(stock: 5);
        var now = DateTime.UtcNow;

        cart.AddItem(variant, 3, now);

        var actExceed = () => cart.AddItem(variant, 3, now);
        actExceed.Should().Throw<DomainException>().Where(e => e.Code == "stock.insufficient");

        var updatedItem = cart.AddItem(variant, 2, now);
        updatedItem.Quantity.Should().Be(5);
        cart.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Cart_UpdateItemQuantity_ShouldUpdate_OrThrowIfExceedsStock()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var variant = CreateVariant(stock: 5);
        var now = DateTime.UtcNow;

        cart.AddItem(variant, 2, now);

        var actExceed = () => cart.UpdateItemQuantity(variant.Id, 6, currentStockQuantity: 5, now);
        actExceed.Should().Throw<DomainException>().Where(e => e.Code == "stock.insufficient");

        cart.UpdateItemQuantity(variant.Id, 4, currentStockQuantity: 5, now);
        cart.Items.First().Quantity.Should().Be(4);
    }

    [Fact]
    public void Cart_RemoveItem_And_Clear_ShouldWorkCorrectly()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var variant1 = CreateVariant(stock: 5);
        var variant2 = CreateVariant(stock: 5);
        var now = DateTime.UtcNow;

        cart.AddItem(variant1, 1, now);
        cart.AddItem(variant2, 2, now);
        cart.Items.Should().HaveCount(2);

        var removed = cart.RemoveItem(variant1.Id, now);
        removed.Should().BeTrue();
        cart.Items.Should().HaveCount(1);
        cart.Items.First().VariantId.Should().Be(variant2.Id);

        cart.Clear(now);
        cart.Items.Should().BeEmpty();
    }
}
