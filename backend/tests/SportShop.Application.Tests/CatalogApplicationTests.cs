using FluentAssertions;
using Moq;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.Brands.CreateBrand;
using SportShop.Application.Features.Catalog.Brands.DeleteBrand;
using SportShop.Application.Features.Catalog.Brands.GetActiveBrands;
using SportShop.Application.Features.Catalog.Brands.GetBrand;
using SportShop.Application.Features.Catalog.Brands.ListBrands;
using SportShop.Application.Features.Catalog.Brands.UpdateBrand;
using SportShop.Application.Features.Catalog.Categories.CreateCategory;
using SportShop.Application.Features.Catalog.Categories.DeleteCategory;
using SportShop.Application.Features.Catalog.Categories.GetCategory;
using SportShop.Application.Features.Catalog.Categories.GetCategoryTree;
using SportShop.Application.Features.Catalog.Categories.ListCategories;
using SportShop.Application.Features.Catalog.Categories.UpdateCategory;
using SportShop.Application.Features.Catalog.Images.DeleteImage;
using SportShop.Application.Features.Catalog.Images.UploadImage;
using SportShop.Application.Features.Catalog.Inventory.AdjustStock;
using SportShop.Application.Features.Catalog.Inventory.LowStock;
using SportShop.Application.Features.Catalog.Products.CreateProduct;
using SportShop.Application.Features.Catalog.Products.DeleteProduct;
using SportShop.Application.Features.Catalog.Products.GetBySlug;
using SportShop.Application.Features.Catalog.Products.GetProduct;
using SportShop.Application.Features.Catalog.Products.List;
using SportShop.Application.Features.Catalog.Products.UpdateProduct;
using SportShop.Application.Features.Catalog.Variants.CreateVariant;
using SportShop.Application.Features.Catalog.Variants.DeleteVariant;
using SportShop.Application.Features.Catalog.Variants.UpdateVariant;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;
using Xunit;

namespace SportShop.Application.Tests;

public sealed class CatalogApplicationTests
{
    [Fact]
    public async Task CreateCategory_ShouldSucceed_WhenValid()
    {
        var repo = new Mock<ICategoryRepository>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new CreateCategoryCommandValidator();

        repo.Setup(r => r.SlugExistsAsync("sepatu", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateCategoryCommandHandler(repo.Object, cache.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new CreateCategoryCommand("Sepatu", "sepatu", null, 0, true));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Sepatu");
        repo.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCategory_ShouldFail_WhenSlugDuplicate()
    {
        var repo = new Mock<ICategoryRepository>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        var validator = new CreateCategoryCommandValidator();

        repo.Setup(r => r.SlugExistsAsync("sepatu", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateCategoryCommandHandler(repo.Object, cache.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new CreateCategoryCommand("Sepatu", "sepatu", null, 0, true));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("category.slug_duplicate");
    }

    [Fact]
    public async Task UpdateCategory_ShouldSucceed_WhenValid()
    {
        var repo = new Mock<ICategoryRepository>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new UpdateCategoryCommandValidator();

        var category = new Category(Guid.NewGuid(), "Sepatu", "sepatu");
        repo.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        repo.Setup(r => r.SlugExistsAsync("sepatu-baru", category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new UpdateCategoryCommandHandler(repo.Object, cache.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new UpdateCategoryCommand(category.Id, "Sepatu Baru", "sepatu-baru", null, 1, true));

        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("Sepatu Baru");
    }

    [Fact]
    public async Task DeleteCategory_ShouldSucceed_WhenFound()
    {
        var repo = new Mock<ICategoryRepository>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        var category = new Category(Guid.NewGuid(), "Sepatu", "sepatu");
        repo.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var handler = new DeleteCategoryCommandHandler(repo.Object, cache.Object, uow.Object, clock.Object);
        var result = await handler.HandleAsync(new DeleteCategoryCommand(category.Id));

        result.IsSuccess.Should().BeTrue();
        category.IsActive.Should().BeFalse();
        repo.Verify(r => r.UpdateAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCategoryById_ShouldReturnCategory_WhenFound()
    {
        var repo = new Mock<ICategoryRepository>();
        var category = new Category(Guid.NewGuid(), "Sepatu", "sepatu");
        repo.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var handler = new GetCategoryByIdQueryHandler(repo.Object);
        var result = await handler.HandleAsync(new GetCategoryByIdQuery(category.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Sepatu");
    }

    [Fact]
    public async Task CreateBrand_ShouldSucceed_WhenValid()
    {
        var repo = new Mock<IBrandRepository>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new CreateBrandCommandValidator();

        repo.Setup(r => r.SlugExistsAsync("nike", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateBrandCommandHandler(repo.Object, cache.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new CreateBrandCommand("Nike", "nike", "https://logo.png", true));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Nike");
    }

    [Fact]
    public async Task UpdateBrand_ShouldSucceed_WhenValid()
    {
        var repo = new Mock<IBrandRepository>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new UpdateBrandCommandValidator();

        var brand = new Brand(Guid.NewGuid(), "Nike", "nike");
        repo.Setup(r => r.GetByIdAsync(brand.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(brand);

        var handler = new UpdateBrandCommandHandler(repo.Object, cache.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new UpdateBrandCommand(brand.Id, "Nike Inc", "nike-inc", null, true));

        result.IsSuccess.Should().BeTrue();
        brand.Name.Should().Be("Nike Inc");
    }

    [Fact]
    public async Task DeleteBrand_ShouldSucceed_WhenFound()
    {
        var repo = new Mock<IBrandRepository>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        var brand = new Brand(Guid.NewGuid(), "Nike", "nike");
        repo.Setup(r => r.GetByIdAsync(brand.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(brand);

        var handler = new DeleteBrandCommandHandler(repo.Object, cache.Object, uow.Object, clock.Object);
        var result = await handler.HandleAsync(new DeleteBrandCommand(brand.Id));

        result.IsSuccess.Should().BeTrue();
        brand.IsActive.Should().BeFalse();
        repo.Verify(r => r.UpdateAsync(brand, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProduct_ShouldSucceed_WhenValid()
    {
        var prodRepo = new Mock<IProductRepository>();
        var catRepo = new Mock<ICategoryRepository>();
        var brandRepo = new Mock<IBrandRepository>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new CreateProductCommandValidator();

        var cat = new Category(Guid.NewGuid(), "Sepatu", "sepatu");
        var brand = new Brand(Guid.NewGuid(), "Nike", "nike");

        catRepo.Setup(c => c.GetByIdAsync(cat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cat);
        brandRepo.Setup(b => b.GetByIdAsync(brand.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(brand);
        prodRepo.Setup(p => p.SlugExistsAsync("sepatu-nike-air", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        prodRepo.Setup(p => p.SkuExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateProductCommandHandler(prodRepo.Object, catRepo.Object, brandRepo.Object, uow.Object, clock.Object, validator);
        var command = new CreateProductCommand(
            cat.Id, brand.Id, "Sepatu Nike Air", "sepatu-nike-air", "Deskripsi", 200000m, true,
            new List<CreateVariantItem>
            {
                new("SKU-001", "42", "Hitam", 200000m, 500, 10, true)
            });

        var result = await handler.HandleAsync(command);
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Sepatu Nike Air");
    }

    [Fact]
    public async Task GetProductBySlug_ShouldReturnProduct_WhenFound()
    {
        var prodRepo = new Mock<IProductRepository>();
        var cache = new Mock<ICacheService>();

        var cat = new Category(Guid.NewGuid(), "Sepatu", "sepatu");
        var brand = new Brand(Guid.NewGuid(), "Nike", "nike");
        var product = new Product(Guid.NewGuid(), cat.Id, brand.Id, "Sepatu Lari", "sepatu-lari", "Desc", 100000m);
        product.Category = cat;
        product.Brand = brand;

        prodRepo.Setup(p => p.GetBySlugAsync("sepatu-lari", It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var handler = new GetProductBySlugQueryHandler(prodRepo.Object, cache.Object);
        var result = await handler.HandleAsync(new GetProductBySlugQuery("sepatu-lari"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Sepatu Lari");
    }

    [Fact]
    public async Task AdjustStock_ShouldSucceed_WhenValid()
    {
        var prodRepo = new Mock<IProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var cache = new Mock<ICacheService>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new AdjustStockCommandValidator();

        var variant = new ProductVariant(Guid.NewGuid(), Guid.NewGuid(), "SKU-001", 100000m, 500, 10);
        prodRepo.Setup(p => p.GetVariantByIdAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);

        var handler = new AdjustStockCommandHandler(prodRepo.Object, cache.Object, currentUser.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new AdjustStockCommand(variant.Id, 5, "Restock"));

        result.IsSuccess.Should().BeTrue();
        variant.StockQuantity.Should().Be(15);
    }

    [Fact]
    public async Task GetLowStockVariants_ShouldReturnLowStockItems()
    {
        var prodRepo = new Mock<IProductRepository>();
        var cat = new Category(Guid.NewGuid(), "Sepatu", "sepatu");
        var brand = new Brand(Guid.NewGuid(), "Nike", "nike");
        var prod = new Product(Guid.NewGuid(), cat.Id, brand.Id, "Sepatu", "sepatu", "Desc", 100000m);
        var variant = new ProductVariant(Guid.NewGuid(), prod.Id, "SKU-LOW", 100000m, 500, 2);
        variant.Product = prod;

        prodRepo.Setup(p => p.GetLowStockVariantsAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductVariant> { variant });

        var handler = new GetLowStockVariantsQueryHandler(prodRepo.Object);
        var result = await handler.HandleAsync(new GetLowStockVariantsQuery(5));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Sku.Should().Be("SKU-LOW");
    }
}
