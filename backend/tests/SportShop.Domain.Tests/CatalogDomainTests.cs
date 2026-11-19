using FluentAssertions;
using SportShop.Domain.Catalog;
using SportShop.Domain.Common;
using SportShop.Domain.Inventory;
using Xunit;

namespace SportShop.Domain.Tests;

public sealed class CatalogDomainTests
{
    [Fact]
    public void Category_SetParent_ShouldThrow_WhenParentAlreadyHasParent()
    {
        // Arrange
        var grandParent = new Category(Guid.NewGuid(), "Olahraga", "olahraga");
        var parent = new Category(Guid.NewGuid(), "Sepatu", "sepatu", grandParent.Id);
        parent.Parent = grandParent;

        var child = new Category(Guid.NewGuid(), "Sepatu Lari", "sepatu-lari");

        // Act
        var act = () => child.SetParent(parent);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*2 tingkat*")
            .Where(e => e.Code == "category.max_depth");
    }

    [Fact]
    public void Category_SetParent_ShouldSucceed_WhenParentIsRoot()
    {
        // Arrange
        var root = new Category(Guid.NewGuid(), "Sepatu", "sepatu");
        var child = new Category(Guid.NewGuid(), "Sepatu Lari", "sepatu-lari");

        // Act
        child.SetParent(root);

        // Assert
        child.ParentId.Should().Be(root.Id);
        child.Parent.Should().Be(root);
    }

    [Fact]
    public void Product_Creation_ShouldThrow_WhenBasePriceIsNegative()
    {
        // Act
        var act = () => new Product(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Sepatu Nike",
            "sepatu-nike",
            "Deskripsi",
            -1000m);

        // Assert
        act.Should().Throw<DomainException>()
            .Where(e => e.Code == "product.invalid_price");
    }

    [Fact]
    public void ProductVariant_Creation_ShouldThrow_WhenStockIsNegative()
    {
        // Act
        var act = () => new ProductVariant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SKU-001",
            100_000m,
            500,
            -1);

        // Assert
        act.Should().Throw<DomainException>()
            .Where(e => e.Code == "stock.negative");
    }

    [Fact]
    public void ProductVariant_Creation_ShouldThrow_WhenWeightIsZeroOrNegative()
    {
        // Act
        var act = () => new ProductVariant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SKU-001",
            100_000m,
            0,
            10);

        // Assert
        act.Should().Throw<DomainException>()
            .Where(e => e.Code == "variant.invalid_weight");
    }

    [Fact]
    public void ProductVariant_AdjustStock_ShouldThrow_WhenStockBecomesNegative()
    {
        // Arrange
        var variant = new ProductVariant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SKU-001",
            100_000m,
            500,
            5);

        // Act
        var act = () => variant.AdjustStock(-6, DateTime.UtcNow);

        // Assert
        act.Should().Throw<DomainException>()
            .Where(e => e.Code == "stock.insufficient");
    }

    [Fact]
    public void InventoryMovement_ShouldThrow_WhenManualAdjustHasNoNote()
    {
        // Act
        var act = () => new InventoryMovement(
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            StockReason.ManualAdjust,
            note: "   ");

        // Assert
        act.Should().Throw<DomainException>()
            .Where(e => e.Code == "inventory.note_required");
    }

    [Fact]
    public void InventoryMovement_ShouldSucceed_WhenManualAdjustHasValidNote()
    {
        // Act
        var movement = new InventoryMovement(
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            StockReason.ManualAdjust,
            note: "Penambahan stok gudang");

        // Assert
        movement.Reason.Should().Be(StockReason.ManualAdjust);
        movement.Note.Should().Be("Penambahan stok gudang");
    }
}
