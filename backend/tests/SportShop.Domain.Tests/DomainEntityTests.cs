using FluentAssertions;
using SportShop.Domain.Carts;
using SportShop.Domain.Catalog;
using SportShop.Domain.Common;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;
using SportShop.Domain.Payments;
using SportShop.Domain.Shipping;
using Xunit;

namespace SportShop.Domain.Tests;

public sealed class DomainEntityTests
{
    [Fact]
    public void Brand_ShouldSupportConstructorsAndPropertyUpdates()
    {
        var defaultBrand = new Brand();
        defaultBrand.Id = Guid.NewGuid();
        defaultBrand.Name = "Default";
        defaultBrand.Slug = "default";
        defaultBrand.LogoUrl = "https://logo.png";
        defaultBrand.IsActive = false;
        defaultBrand.CreatedAt = DateTime.UtcNow;
        defaultBrand.UpdatedAt = DateTime.UtcNow;
        defaultBrand.Products.Should().BeEmpty();

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var brand = new Brand(id, "Nike", "nike", "https://nike.com/logo.png", true, now);

        brand.Id.Should().Be(id);
        brand.Name.Should().Be("Nike");
        brand.Slug.Should().Be("nike");
        brand.LogoUrl.Should().Be("https://nike.com/logo.png");
        brand.IsActive.Should().BeTrue();
        brand.CreatedAt.Should().Be(now);

        var updateTime = now.AddHours(1);
        brand.Update("Nike Inc", "nike-inc", "https://nike.com/new-logo.png", false, updateTime);

        brand.Name.Should().Be("Nike Inc");
        brand.Slug.Should().Be("nike-inc");
        brand.LogoUrl.Should().Be("https://nike.com/new-logo.png");
        brand.IsActive.Should().BeFalse();
        brand.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void ProductImage_ShouldSupportConstructorsAndPropertyUpdates()
    {
        var defaultImg = new ProductImage();
        defaultImg.Id = Guid.NewGuid();
        defaultImg.ProductId = Guid.NewGuid();
        defaultImg.Url = "https://img.png";
        defaultImg.AltText = "Alt";
        defaultImg.SortOrder = 1;
        defaultImg.IsPrimary = true;
        defaultImg.Product = new Product();

        var id = Guid.NewGuid();
        var prodId = Guid.NewGuid();
        var img = new ProductImage(id, prodId, "https://example.com/shoe.png", "Sepatu", 0, true);

        img.Id.Should().Be(id);
        img.ProductId.Should().Be(prodId);
        img.Url.Should().Be("https://example.com/shoe.png");
        img.AltText.Should().Be("Sepatu");
        img.SortOrder.Should().Be(0);
        img.IsPrimary.Should().BeTrue();

        img.Update("https://example.com/shoe2.png", "Sepatu 2", 1, false);
        img.Url.Should().Be("https://example.com/shoe2.png");
        img.AltText.Should().Be("Sepatu 2");
        img.SortOrder.Should().Be(1);
        img.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void Category_ShouldSupportUpdateAndDefaultConstructor()
    {
        var defaultCat = new Category();
        defaultCat.Id = Guid.NewGuid();
        defaultCat.Name = "Default";
        defaultCat.Slug = "default";
        defaultCat.ParentId = null;
        defaultCat.SortOrder = 0;
        defaultCat.IsActive = true;
        defaultCat.CreatedAt = DateTime.UtcNow;
        defaultCat.UpdatedAt = null;
        defaultCat.SubCategories.Should().BeEmpty();
        defaultCat.Products.Should().BeEmpty();

        var cat = new Category(Guid.NewGuid(), "Pakaian", "pakaian", null, 1, true);
        var now = DateTime.UtcNow;
        var parentId = Guid.NewGuid();
        cat.Update("Pakaian Pria", "pakaian-pria", parentId, 2, false, now);

        cat.Name.Should().Be("Pakaian Pria");
        cat.Slug.Should().Be("pakaian-pria");
        cat.ParentId.Should().Be(parentId);
        cat.SortOrder.Should().Be(2);
        cat.IsActive.Should().BeFalse();
        cat.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Product_ShouldSupportMethodsAndValidation()
    {
        var defaultProd = new Product();
        defaultProd.Id = Guid.NewGuid();
        defaultProd.CategoryId = Guid.NewGuid();
        defaultProd.Category = new Category();
        defaultProd.BrandId = Guid.NewGuid();
        defaultProd.Brand = new Brand();
        defaultProd.Name = "Default";
        defaultProd.Slug = "default";
        defaultProd.Description = "Desc";
        defaultProd.BasePrice = 1000m;
        defaultProd.IsActive = true;
        defaultProd.CreatedAt = DateTime.UtcNow;
        defaultProd.UpdatedAt = null;
        defaultProd.Variants.Should().BeEmpty();
        defaultProd.Images.Should().BeEmpty();

        var prodId = Guid.NewGuid();
        var catId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var prod = new Product(prodId, catId, brandId, "Sepatu Running", "sepatu-running", "Deskripsi lari", 200_000m, true, now);

        var newCatId = Guid.NewGuid();
        var newBrandId = Guid.NewGuid();
        prod.UpdateDetails("Sepatu Trail", "sepatu-trail", "Deskripsi trail", newCatId, newBrandId, now);
        prod.Name.Should().Be("Sepatu Trail");
        prod.Slug.Should().Be("sepatu-trail");
        prod.Description.Should().Be("Deskripsi trail");
        prod.CategoryId.Should().Be(newCatId);
        prod.BrandId.Should().Be(newBrandId);

        prod.UpdateBasePrice(250_000m, now);
        prod.BasePrice.Should().Be(250_000m);

        var actNegative = () => prod.UpdateBasePrice(-10m, now);
        actNegative.Should().Throw<DomainException>().Where(e => e.Code == "product.invalid_price");

        prod.SetActive(false, now);
        prod.IsActive.Should().BeFalse();

        // RecalculateBasePrice with variants
        var v1 = new ProductVariant(Guid.NewGuid(), prod.Id, "SKU-1", 150_000m, 300, 5, isActive: false);
        var v2 = new ProductVariant(Guid.NewGuid(), prod.Id, "SKU-2", 180_000m, 300, 5, isActive: true);
        var v3 = new ProductVariant(Guid.NewGuid(), prod.Id, "SKU-3", 190_000m, 300, 5, isActive: true);
        prod.Variants.Add(v1);
        prod.Variants.Add(v2);
        prod.Variants.Add(v3);

        prod.RecalculateBasePrice(now);
        prod.BasePrice.Should().Be(180_000m); // Min of active variants

        v2.SetActive(false, now);
        v3.SetActive(false, now);
        prod.RecalculateBasePrice(now);
        prod.BasePrice.Should().Be(150_000m); // Min of all variants when none active
    }

    [Fact]
    public void ProductVariant_ShouldSupportUpdateAndValidations()
    {
        var defaultVariant = new ProductVariant();
        defaultVariant.Id = Guid.NewGuid();
        defaultVariant.ProductId = Guid.NewGuid();
        defaultVariant.Product = new Product();
        defaultVariant.Sku = "SKU-DEF";
        defaultVariant.Price = 1000m;
        defaultVariant.WeightGram = 100;
        defaultVariant.StockQuantity = 1;
        defaultVariant.IsActive = true;
        defaultVariant.InventoryMovements.Should().BeEmpty();

        var variant = new ProductVariant(Guid.NewGuid(), Guid.NewGuid(), "SKU-A", 100_000m, 500, 10, "M", "Merah", true);
        var now = DateTime.UtcNow;

        variant.Update("SKU-B", "L", "Biru", 120_000m, 600, false, now);
        variant.Sku.Should().Be("SKU-B");
        variant.OptionSize.Should().Be("L");
        variant.OptionColor.Should().Be("Biru");
        variant.Price.Should().Be(120_000m);
        variant.WeightGram.Should().Be(600);
        variant.IsActive.Should().BeFalse();

        var actPrice = () => variant.Update("SKU-C", "L", "Biru", -1m, 600, true, now);
        actPrice.Should().Throw<DomainException>().Where(e => e.Code == "variant.invalid_price");

        var actWeight = () => variant.Update("SKU-C", "L", "Biru", 100m, 0, true, now);
        actWeight.Should().Throw<DomainException>().Where(e => e.Code == "variant.invalid_weight");

        var actConstPrice = () => new ProductVariant(Guid.NewGuid(), Guid.NewGuid(), "SKU", -1m, 100, 10);
        actConstPrice.Should().Throw<DomainException>().Where(e => e.Code == "variant.invalid_price");

        variant.SetActive(true, now);
        variant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PaymentNotification_ShouldValidateAndBuildDedupKey()
    {
        var defaultNotif = new PaymentNotification();
        defaultNotif.Id = Guid.NewGuid();
        defaultNotif.PaymentId = null;
        defaultNotif.OrderNumber = "SPT-1";
        defaultNotif.TransactionId = "TX-1";
        defaultNotif.TransactionStatus = "settlement";
        defaultNotif.StatusCode = "200";
        defaultNotif.SignatureValid = true;
        defaultNotif.Payload = "{}";
        defaultNotif.ReceivedAt = DateTime.UtcNow;
        defaultNotif.DedupKey = "SPT-1|settlement|200|none";
        defaultNotif.Payment = null;

        var id = Guid.NewGuid();
        var payId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var notif = new PaymentNotification(id, payId, "SPT-20261120-0001", "TX-123", "settlement", "200", true, "{\"status\": 200}", now, "SPT-20261120-0001|settlement|200|2026-11-20");

        notif.OrderNumber.Should().Be("SPT-20261120-0001");
        notif.TransactionId.Should().Be("TX-123");
        notif.TransactionStatus.Should().Be("settlement");
        notif.StatusCode.Should().Be("200");
        notif.SignatureValid.Should().BeTrue();
        notif.Payload.Should().Be("{\"status\": 200}");
        notif.DedupKey.Should().Be("SPT-20261120-0001|settlement|200|2026-11-20");

        var actEmptyOrder = () => new PaymentNotification(id, payId, "", "TX-123", "settlement", "200", true, "{}", now, "key");
        actEmptyOrder.Should().Throw<DomainException>().Where(e => e.Code == "payment_notification.invalid_order_number");

        var actEmptyKey = () => new PaymentNotification(id, payId, "SPT-1", "TX-123", "settlement", "200", true, "{}", now, "");
        actEmptyKey.Should().Throw<DomainException>().Where(e => e.Code == "payment_notification.invalid_dedup_key");

        PaymentNotification.BuildDedupKey("SPT-1", "settlement", "200", null).Should().Be("SPT-1|settlement|200|none");
        PaymentNotification.BuildDedupKey("SPT-1", "settlement", "200", "").Should().Be("SPT-1|settlement|200|none");
        PaymentNotification.BuildDedupKey("SPT-1", "settlement", "200", "2026-11-20 12:00:00").Should().Be("SPT-1|settlement|200|2026-11-20 12:00:00");
    }

    [Fact]
    public void OrderShippingAddress_ShouldValidateFields()
    {
        var defaultAddr = new OrderShippingAddress();
        defaultAddr.RecipientName.Should().BeEmpty();

        var addr = new OrderShippingAddress("Budi", "08123456789", "JK", "DKI Jakarta", "Jakarta Selatan", "12345", "Jl. Melati 1");
        addr.RecipientName.Should().Be("Budi");
        addr.ProvinceCode.Should().Be("JK");

        var act1 = () => new OrderShippingAddress("", "0812", "JK", "DKI Jakarta", "Jakarta", "12345", "Jl");
        act1.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_recipient");

        var act2 = () => new OrderShippingAddress("Budi", "", "JK", "DKI Jakarta", "Jakarta", "12345", "Jl");
        act2.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_phone");

        var act3 = () => new OrderShippingAddress("Budi", "0812", "", "DKI Jakarta", "Jakarta", "12345", "Jl");
        act3.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_province");

        var act4 = () => new OrderShippingAddress("Budi", "0812", "JK", "", "Jakarta", "12345", "Jl");
        act4.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_province");

        var act5 = () => new OrderShippingAddress("Budi", "0812", "JK", "DKI Jakarta", "", "12345", "Jl");
        act5.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_city");

        var act6 = () => new OrderShippingAddress("Budi", "0812", "JK", "DKI Jakarta", "Jakarta", "", "Jl");
        act6.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_postal_code");

        var act7 = () => new OrderShippingAddress("Budi", "0812", "JK", "DKI Jakarta", "Jakarta", "12345", "");
        act7.Should().Throw<DomainException>().Where(e => e.Code == "order.invalid_street");
    }

    [Fact]
    public void OrderStatusHistory_ShouldSupportConstructors()
    {
        var defaultHist = new OrderStatusHistory();
        defaultHist.Id = Guid.NewGuid();
        defaultHist.OrderId = Guid.NewGuid();
        defaultHist.Order = new Order();
        defaultHist.FromStatus = OrderStatus.PendingPayment;
        defaultHist.ToStatus = OrderStatus.Paid;
        defaultHist.Note = "Note";
        defaultHist.ChangedBy = Guid.NewGuid();
        defaultHist.CreatedAt = DateTime.UtcNow;

        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var hist = new OrderStatusHistory(id, orderId, OrderStatus.PendingPayment, OrderStatus.Paid, "Paid note", changedBy, now);
        hist.Id.Should().Be(id);
        hist.OrderId.Should().Be(orderId);
        hist.FromStatus.Should().Be(OrderStatus.PendingPayment);
        hist.ToStatus.Should().Be(OrderStatus.Paid);
        hist.Note.Should().Be("Paid note");
        hist.ChangedBy.Should().Be(changedBy);
        hist.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void ShippingRate_ShouldValidateAndSupportUpdates()
    {
        var defaultRate = new ShippingRate();
        defaultRate.ProvinceCode = "JK";
        defaultRate.ProvinceName = "Jakarta";
        defaultRate.BaseFee = 10000m;
        defaultRate.PerKgFee = 5000m;
        defaultRate.EstimatedDaysMin = 1;
        defaultRate.EstimatedDaysMax = 3;
        defaultRate.IsActive = true;
        defaultRate.UpdatedAt = null;

        var rate = new ShippingRate("JB", "Jawa Barat", 12000m, 6000m, 2, 4, true);
        rate.ProvinceCode.Should().Be("JB");

        var now = DateTime.UtcNow;
        rate.UpdateRate(15000m, 7000m, 1, 3, true, now);
        rate.BaseFee.Should().Be(15000m);
        rate.PerKgFee.Should().Be(7000m);
        rate.EstimatedDaysMin.Should().Be(1);
        rate.EstimatedDaysMax.Should().Be(3);
        rate.UpdatedAt.Should().Be(now);

        var a1 = () => new ShippingRate("", "Prov", 10m, 10m, 1, 2);
        a1.Should().Throw<DomainException>().Where(e => e.Code == "shipping.invalid_province_code");

        var a2 = () => new ShippingRate("JK", "", 10m, 10m, 1, 2);
        a2.Should().Throw<DomainException>().Where(e => e.Code == "shipping.invalid_province_name");

        var a3 = () => new ShippingRate("JK", "Prov", -1m, 10m, 1, 2);
        a3.Should().Throw<DomainException>().Where(e => e.Code == "shipping.invalid_base_fee");

        var a4 = () => new ShippingRate("JK", "Prov", 10m, -1m, 1, 2);
        a4.Should().Throw<DomainException>().Where(e => e.Code == "shipping.invalid_per_kg_fee");

        var a5 = () => new ShippingRate("JK", "Prov", 10m, 10m, -1, 2);
        a5.Should().Throw<DomainException>().Where(e => e.Code == "shipping.invalid_estimated_days");

        var a6 = () => new ShippingRate("JK", "Prov", 10m, 10m, 5, 2);
        a6.Should().Throw<DomainException>().Where(e => e.Code == "shipping.invalid_estimated_days");

        // UpdateRate validation
        var u1 = () => rate.UpdateRate(-1m, 10m, 1, 2, true, now);
        u1.Should().Throw<DomainException>().Where(e => e.Code == "shipping.invalid_base_fee");

        var u2 = () => rate.UpdateRate(10m, -1m, 1, 2, true, now);
        u2.Should().Throw<DomainException>().Where(e => e.Code == "shipping.invalid_per_kg_fee");

        var u3 = () => rate.UpdateRate(10m, 10m, -1, 2, true, now);
        u3.Should().Throw<DomainException>().Where(e => e.Code == "shipping.invalid_estimated_days");

        var u4 = () => rate.UpdateRate(10m, 10m, 5, 2, true, now);
        u4.Should().Throw<DomainException>().Where(e => e.Code == "shipping.invalid_estimated_days");
    }

    [Fact]
    public void Cart_ShouldSupportClearAndRemoveItem()
    {
        var defaultCart = new Cart();
        defaultCart.Id = Guid.NewGuid();
        defaultCart.UserId = Guid.NewGuid();
        defaultCart.CreatedAt = DateTime.UtcNow;
        defaultCart.UpdatedAt = null;
        defaultCart.Items.Should().BeEmpty();

        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var variant = new ProductVariant(Guid.NewGuid(), Guid.NewGuid(), "SKU-TEST", 50000m, 200, 10, isActive: true);
        var now = DateTime.UtcNow;

        cart.AddItem(variant, 2, now);
        cart.Items.Should().HaveCount(1);

        var removed = cart.RemoveItem(variant.Id, now);
        removed.Should().BeTrue();
        cart.Items.Should().BeEmpty();

        var removedAgain = cart.RemoveItem(variant.Id, now);
        removedAgain.Should().BeFalse();

        cart.AddItem(variant, 1, now);
        cart.Clear(now);
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void CartItem_ShouldSupportConstructorsAndPropertyUpdates()
    {
        var defaultItem = new CartItem();
        defaultItem.Id = Guid.NewGuid();
        defaultItem.CartId = Guid.NewGuid();
        defaultItem.Cart = new Cart();
        defaultItem.VariantId = Guid.NewGuid();
        defaultItem.Variant = new ProductVariant();
        defaultItem.Quantity = 1;
        defaultItem.PriceAtAdd = 50000m;
        defaultItem.CreatedAt = DateTime.UtcNow;
        defaultItem.UpdatedAt = null;

        var id = Guid.NewGuid();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var item = new CartItem(id, cartId, variantId, 2, 75000m, now);
        item.Id.Should().Be(id);
        item.Quantity.Should().Be(2);
        item.PriceAtAdd.Should().Be(75000m);

        var actZero = () => new CartItem(id, cartId, variantId, 0, 75000m, now);
        actZero.Should().Throw<DomainException>().Where(e => e.Code == "cart.invalid_quantity");

        var actNegPrice = () => new CartItem(id, cartId, variantId, 1, -100m, now);
        actNegPrice.Should().Throw<DomainException>().Where(e => e.Code == "cart.invalid_price");
    }

    [Fact]
    public void DomainException_ShouldSupportAllConstructors()
    {
        var inner = new InvalidOperationException("Inner error");
        var ex1 = new DomainException("domain.test", "Test error");
        ex1.Code.Should().Be("domain.test");
        ex1.Message.Should().Be("Test error");

        var ex2 = new DomainException("domain.test2", "Test error with inner", inner);
        ex2.Code.Should().Be("domain.test2");
        ex2.Message.Should().Be("Test error with inner");
        ex2.InnerException.Should().Be(inner);
    }

    [Fact]
    public void InventoryMovement_ShouldSupportDefaultConstructorAndProperties()
    {
        var defaultMv = new InventoryMovement();
        defaultMv.Id = Guid.NewGuid();
        defaultMv.VariantId = Guid.NewGuid();
        defaultMv.Variant = new ProductVariant();
        defaultMv.ChangeQuantity = 5;
        defaultMv.Reason = StockReason.Initial;
        defaultMv.OrderId = null;
        defaultMv.Note = "Initial stock";
        defaultMv.CreatedBy = null;
        defaultMv.CreatedAt = DateTime.UtcNow;

        defaultMv.Reason.Should().Be(StockReason.Initial);
        defaultMv.ChangeQuantity.Should().Be(5);
    }
}
