using FluentAssertions;
using Moq;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.Brands.CreateBrand;
using SportShop.Application.Features.Catalog.Brands.GetActiveBrands;
using SportShop.Application.Features.Catalog.Brands.GetBrand;
using SportShop.Application.Features.Catalog.Brands.ListBrands;
using SportShop.Application.Features.Catalog.Brands.UpdateBrand;
using SportShop.Application.Features.Catalog.Categories.CreateCategory;
using SportShop.Application.Features.Catalog.Categories.GetCategoryTree;
using SportShop.Application.Features.Catalog.Categories.ListCategories;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Features.Catalog.Images.DeleteImage;
using SportShop.Application.Features.Catalog.Images.UploadImage;
using SportShop.Application.Features.Catalog.Inventory.AdjustStock;
using SportShop.Application.Features.Catalog.Products.CreateProduct;
using SportShop.Application.Features.Catalog.Products.DeleteProduct;
using SportShop.Application.Features.Catalog.Products.GetBySlug;
using SportShop.Application.Features.Catalog.Products.GetProduct;
using SportShop.Application.Features.Catalog.Products.List;
using SportShop.Application.Features.Catalog.Products.ListProducts;
using SportShop.Application.Features.Catalog.Products.UpdateProduct;
using SportShop.Application.Features.Catalog.Variants.CreateVariant;
using SportShop.Application.Features.Catalog.Variants.DeleteVariant;
using SportShop.Application.Features.Catalog.Variants.UpdateVariant;
using SportShop.Domain.Catalog;
using Xunit;

namespace SportShop.Application.Tests;

public sealed class CatalogAdditionalTests
{
    [Fact]
    public async Task CreateVariant_ShouldSucceed_WhenValid()
    {
        var prodRepo = new Mock<IProductRepository>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new CreateVariantCommandValidator();

        var cat = new Category(Guid.NewGuid(), "Sepatu", "sepatu");
        var brand = new Brand(Guid.NewGuid(), "Nike", "nike");
        var prod = new Product(Guid.NewGuid(), cat.Id, brand.Id, "Sepatu", "sepatu", "Desc", 100000m);

        prodRepo.Setup(p => p.GetByIdAsync(prod.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prod);
        prodRepo.Setup(p => p.SkuExistsAsync("SKU-VAR-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        prodRepo.Setup(p => p.VariantOptionsExistAsync(prod.Id, "42", "Hitam", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateVariantCommandHandler(prodRepo.Object, cache.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new CreateVariantCommand(prod.Id, "SKU-VAR-1", "42", "Hitam", 150000m, 500, 10, true));

        result.IsSuccess.Should().BeTrue();
        result.Value.Sku.Should().Be("SKU-VAR-1");
    }

    [Fact]
    public async Task UpdateVariant_ShouldSucceed_WhenValid()
    {
        var prodRepo = new Mock<IProductRepository>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new UpdateVariantCommandValidator();

        var variant = new ProductVariant(Guid.NewGuid(), Guid.NewGuid(), "SKU-OLD", 100000m, 500, 10);
        var prod = new Product(variant.ProductId, Guid.NewGuid(), Guid.NewGuid(), "Sepatu", "sepatu", "Desc", 100000m);

        prodRepo.Setup(p => p.GetVariantByIdAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);
        prodRepo.Setup(p => p.SkuExistsAsync("SKU-NEW", variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        prodRepo.Setup(p => p.VariantOptionsExistAsync(variant.ProductId, "43", "Biru", variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        prodRepo.Setup(p => p.GetByIdAsync(variant.ProductId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prod);

        var handler = new UpdateVariantCommandHandler(prodRepo.Object, cache.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new UpdateVariantCommand(variant.Id, "SKU-NEW", "43", "Biru", 120000m, 600, true));

        result.IsSuccess.Should().BeTrue();
        variant.Sku.Should().Be("SKU-NEW");
    }

    [Fact]
    public async Task DeleteVariant_ShouldSucceed_WhenFound()
    {
        var prodRepo = new Mock<IProductRepository>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        var variant = new ProductVariant(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", 100000m, 500, 10);
        var prod = new Product(variant.ProductId, Guid.NewGuid(), Guid.NewGuid(), "Sepatu", "sepatu", "Desc", 100000m);

        prodRepo.Setup(p => p.GetVariantByIdAsync(variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);
        prodRepo.Setup(p => p.GetByIdAsync(variant.ProductId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prod);

        var handler = new DeleteVariantCommandHandler(prodRepo.Object, cache.Object, uow.Object, clock.Object);
        var result = await handler.HandleAsync(new DeleteVariantCommand(variant.Id));

        result.IsSuccess.Should().BeTrue();
        variant.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProduct_ShouldSucceed_WhenValid()
    {
        var prodRepo = new Mock<IProductRepository>();
        var catRepo = new Mock<ICategoryRepository>();
        var brandRepo = new Mock<IBrandRepository>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        var validator = new UpdateProductCommandValidator();

        var cat = new Category(Guid.NewGuid(), "Sepatu", "sepatu");
        var brand = new Brand(Guid.NewGuid(), "Nike", "nike");
        var prod = new Product(Guid.NewGuid(), cat.Id, brand.Id, "Sepatu", "sepatu", "Desc", 100000m);
        prod.Category = cat;
        prod.Brand = brand;

        prodRepo.Setup(p => p.GetByIdAsync(prod.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prod);
        prodRepo.Setup(p => p.SlugExistsAsync("sepatu-baru", prod.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        catRepo.Setup(c => c.GetByIdAsync(cat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cat);
        brandRepo.Setup(b => b.GetByIdAsync(brand.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(brand);

        var handler = new UpdateProductCommandHandler(prodRepo.Object, catRepo.Object, brandRepo.Object, cache.Object, uow.Object, clock.Object, validator);
        var result = await handler.HandleAsync(new UpdateProductCommand(prod.Id, cat.Id, brand.Id, "Sepatu Baru", "sepatu-baru", "Desc Baru", true));

        result.IsSuccess.Should().BeTrue();
        prod.Name.Should().Be("Sepatu Baru");
    }

    [Fact]
    public async Task DeleteProduct_ShouldSucceed_WhenFound()
    {
        var prodRepo = new Mock<IProductRepository>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        var prod = new Product(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sepatu", "sepatu", "Desc", 100000m);
        prodRepo.Setup(p => p.GetByIdAsync(prod.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prod);

        var handler = new DeleteProductCommandHandler(prodRepo.Object, cache.Object, uow.Object, clock.Object);
        var result = await handler.HandleAsync(new DeleteProductCommand(prod.Id));

        result.IsSuccess.Should().BeTrue();
        prod.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetProductById_ShouldSucceed_WhenFound()
    {
        var prodRepo = new Mock<IProductRepository>();
        var cat = new Category(Guid.NewGuid(), "Sepatu", "sepatu");
        var brand = new Brand(Guid.NewGuid(), "Nike", "nike");
        var prod = new Product(Guid.NewGuid(), cat.Id, brand.Id, "Sepatu", "sepatu", "Desc", 100000m);
        prod.Category = cat;
        prod.Brand = brand;

        prodRepo.Setup(p => p.GetByIdAsync(prod.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prod);

        var handler = new GetProductByIdQueryHandler(prodRepo.Object);
        var result = await handler.HandleAsync(new GetProductByIdQuery(prod.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Sepatu");
    }

    [Fact]
    public async Task UploadProductImage_ShouldSucceed_WhenValid()
    {
        var prodRepo = new Mock<IProductRepository>();
        var fileStorage = new Mock<IFileStorage>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();

        var prod = new Product(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sepatu", "sepatu", "Desc", 100000m);
        prodRepo.Setup(p => p.GetByIdAsync(prod.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prod);
        prodRepo.Setup(p => p.GetProductImagesAsync(prod.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductImage>());
        fileStorage.Setup(f => f.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://cdn.example.com/shoe.png");

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var handler = new UploadProductImageCommandHandler(prodRepo.Object, fileStorage.Object, cache.Object, uow.Object);
        var result = await handler.HandleAsync(new UploadProductImageCommand(prod.Id, stream, "shoe.png", "image/png", 3, "Sepatu", 0, true));

        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be("https://cdn.example.com/shoe.png");
    }

    [Fact]
    public async Task DeleteProductImage_ShouldSucceed_WhenFound()
    {
        var prodRepo = new Mock<IProductRepository>();
        var fileStorage = new Mock<IFileStorage>();
        var cache = new Mock<ICacheService>();
        var uow = new Mock<IUnitOfWork>();

        var prodId = Guid.NewGuid();
        var imgId = Guid.NewGuid();
        var image = new ProductImage(imgId, prodId, "https://example.com/img.png", "Alt", 0, true);
        var prod = new Product(prodId, Guid.NewGuid(), Guid.NewGuid(), "Sepatu", "sepatu", "Desc", 100000m);

        prodRepo.Setup(p => p.GetImageByIdAsync(imgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        prodRepo.Setup(p => p.GetProductImagesAsync(prodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductImage> { image });
        prodRepo.Setup(p => p.GetByIdAsync(prodId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prod);

        var handler = new DeleteProductImageCommandHandler(prodRepo.Object, fileStorage.Object, cache.Object, uow.Object);
        var result = await handler.HandleAsync(new DeleteProductImageCommand(prodId, imgId));

        result.IsSuccess.Should().BeTrue();
        fileStorage.Verify(f => f.DeleteFileAsync(image.Url, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Categories_TreeAndList_ShouldReturnData()
    {
        var catRepo = new Mock<ICategoryRepository>();
        var cache = new Mock<ICacheService>();

        var categories = new List<Category> { new(Guid.NewGuid(), "Sepatu", "sepatu") };
        catRepo.Setup(c => c.GetRootCategoriesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);
        catRepo.Setup(c => c.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var treeHandler = new GetCategoryTreeQueryHandler(catRepo.Object, cache.Object);
        var treeResult = await treeHandler.HandleAsync(new GetCategoryTreeQuery());
        treeResult.IsSuccess.Should().BeTrue();

        var listHandler = new ListCategoriesQueryHandler(catRepo.Object);
        var listResult = await listHandler.HandleAsync(new ListCategoriesQuery(false));
        listResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Brands_Queries_ShouldReturnData()
    {
        var brandRepo = new Mock<IBrandRepository>();
        var cache = new Mock<ICacheService>();

        var brand = new Brand(Guid.NewGuid(), "Nike", "nike");
        var brands = new List<Brand> { brand };

        brandRepo.Setup(b => b.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(brands);
        brandRepo.Setup(b => b.GetByIdAsync(brand.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(brand);

        var listHandler = new ListBrandsQueryHandler(brandRepo.Object);
        var listResult = await listHandler.HandleAsync(new ListBrandsQuery(false));
        listResult.IsSuccess.Should().BeTrue();

        var activeHandler = new GetActiveBrandsQueryHandler(brandRepo.Object, cache.Object);
        var activeResult = await activeHandler.HandleAsync(new GetActiveBrandsQuery());
        activeResult.IsSuccess.Should().BeTrue();

        var getHandler = new GetBrandByIdQueryHandler(brandRepo.Object);
        var getResult = await getHandler.HandleAsync(new GetBrandByIdQuery(brand.Id));
        getResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetProductBySlug_ShouldReturnFromCache_WhenCacheHit()
    {
        var prodRepo = new Mock<IProductRepository>();
        var cache = new Mock<ICacheService>();

        var cachedDto = new ProductDto(
            Guid.NewGuid(), Guid.NewGuid(), "Sepatu", Guid.NewGuid(), "Nike",
            "Sepatu Lari", "sepatu-lari", "Desc", 150000m, true, DateTime.UtcNow, null);

        cache.Setup(c => c.GetAsync<ProductDto>("prod:detail:sepatu-lari:v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDto);

        var handler = new GetProductBySlugQueryHandler(prodRepo.Object, cache.Object);
        var result = await handler.HandleAsync(new GetProductBySlugQuery("sepatu-lari"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(cachedDto);
        prodRepo.Verify(r => r.GetBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetProductBySlug_ShouldFetchAndCache_WhenCacheMiss()
    {
        var prodRepo = new Mock<IProductRepository>();
        var cache = new Mock<ICacheService>();

        var cat = new Category(Guid.NewGuid(), "Sepatu", "sepatu");
        var brand = new Brand(Guid.NewGuid(), "Nike", "nike");
        var prod = new Product(Guid.NewGuid(), cat.Id, brand.Id, "Sepatu Lari", "sepatu-lari", "Desc", 150000m);
        prod.Category = cat;
        prod.Brand = brand;

        cache.Setup(c => c.GetAsync<ProductDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);
        prodRepo.Setup(r => r.GetBySlugAsync("sepatu-lari", It.IsAny<CancellationToken>()))
            .ReturnsAsync(prod);

        var handler = new GetProductBySlugQueryHandler(prodRepo.Object, cache.Object);
        var result = await handler.HandleAsync(new GetProductBySlugQuery("sepatu-lari"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Sepatu Lari");
        cache.Verify(c => c.SetAsync(
            "prod:detail:sepatu-lari:v1",
            It.IsAny<ProductDto>(),
            It.Is<TimeSpan>(t => t == TimeSpan.FromMinutes(5)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProductBySlug_ShouldReturnNotFound_WhenProductMissingOrInactive()
    {
        var prodRepo = new Mock<IProductRepository>();
        var cache = new Mock<ICacheService>();

        var handler = new GetProductBySlugQueryHandler(prodRepo.Object, cache.Object);

        // Empty slug
        var emptyResult = await handler.HandleAsync(new GetProductBySlugQuery("   "));
        emptyResult.IsFailure.Should().BeTrue();

        // Not found
        prodRepo.Setup(r => r.GetBySlugAsync("not-found", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var notFoundResult = await handler.HandleAsync(new GetProductBySlugQuery("not-found"));
        notFoundResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ListProducts_ShouldReturnPaged_WhenValid()
    {
        var prodRepo = new Mock<IProductRepository>();
        var validator = new ListProductsQueryValidator();

        var cat = new Category(Guid.NewGuid(), "Sepatu", "sepatu");
        var brand = new Brand(Guid.NewGuid(), "Nike", "nike");
        var prod = new Product(Guid.NewGuid(), cat.Id, brand.Id, "Sepatu Lari", "sepatu-lari", "Desc", 150000m);
        prod.Category = cat;
        prod.Brand = brand;

        prodRepo.Setup(r => r.GetPublicProductsAsync(
            null, null, null, null, null, null, "newest", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Product> { prod }, 1));

        var handler = new ListProductsQueryHandler(prodRepo.Object, validator);
        var result = await handler.HandleAsync(new ListProductsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task ListProducts_ShouldReturnValidationError_WhenQueryInvalid()
    {
        var prodRepo = new Mock<IProductRepository>();
        var validator = new ListProductsQueryValidator();

        var handler = new ListProductsQueryHandler(prodRepo.Object, validator);
        var result = await handler.HandleAsync(new ListProductsQuery(MinPrice: 200000m, MaxPrice: 100000m));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task ListAdminProducts_ShouldReturnPaged()
    {
        var prodRepo = new Mock<IProductRepository>();

        var cat = new Category(Guid.NewGuid(), "Sepatu", "sepatu");
        var brand = new Brand(Guid.NewGuid(), "Nike", "nike");
        var prod = new Product(Guid.NewGuid(), cat.Id, brand.Id, "Sepatu Lari", "sepatu-lari", "Desc", 150000m);
        prod.Category = cat;
        prod.Brand = brand;

        prodRepo.Setup(r => r.GetPagedAsync(1, 20, null, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Product> { prod }, 1));

        var handler = new ListAdminProductsQueryHandler(prodRepo.Object);
        var result = await handler.HandleAsync(new ListAdminProductsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task CatalogCommands_ValidationFailures_ShouldReturnValidationResult()
    {
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IClock>();
        var cache = new Mock<ICacheService>();
        var catRepo = new Mock<ICategoryRepository>();
        var brandRepo = new Mock<IBrandRepository>();
        var prodRepo = new Mock<IProductRepository>();
        var user = new Mock<ICurrentUser>();

        // CreateCategory validation failure (empty name)
        var createCatHandler = new CreateCategoryCommandHandler(catRepo.Object, cache.Object, uow.Object, clock.Object, new CreateCategoryCommandValidator());
        var catResult = await createCatHandler.HandleAsync(new CreateCategoryCommand("", ""));
        catResult.IsFailure.Should().BeTrue();
        catResult.Error.Type.Should().Be(ErrorType.Validation);

        // CreateBrand validation failure (empty name)
        var createBrandHandler = new CreateBrandCommandHandler(brandRepo.Object, cache.Object, uow.Object, clock.Object, new CreateBrandCommandValidator());
        var brandResult = await createBrandHandler.HandleAsync(new CreateBrandCommand("", ""));
        brandResult.IsFailure.Should().BeTrue();
        brandResult.Error.Type.Should().Be(ErrorType.Validation);

        // UpdateBrand validation failure (empty name)
        var updateBrandHandler = new UpdateBrandCommandHandler(brandRepo.Object, cache.Object, uow.Object, clock.Object, new UpdateBrandCommandValidator());
        var updBrandResult = await updateBrandHandler.HandleAsync(new UpdateBrandCommand(Guid.NewGuid(), "", ""));
        updBrandResult.IsFailure.Should().BeTrue();
        updBrandResult.Error.Type.Should().Be(ErrorType.Validation);

        // CreateProduct validation failure (empty name)
        var createProdHandler = new CreateProductCommandHandler(prodRepo.Object, catRepo.Object, brandRepo.Object, uow.Object, clock.Object, new CreateProductCommandValidator());
        var prodResult = await createProdHandler.HandleAsync(new CreateProductCommand(Guid.NewGuid(), Guid.NewGuid(), "", "", "Desc", -1m));
        prodResult.IsFailure.Should().BeTrue();
        prodResult.Error.Type.Should().Be(ErrorType.Validation);

        // UpdateProduct validation failure (empty name)
        var updateProdHandler = new UpdateProductCommandHandler(prodRepo.Object, catRepo.Object, brandRepo.Object, cache.Object, uow.Object, clock.Object, new UpdateProductCommandValidator());
        var updProdResult = await updateProdHandler.HandleAsync(new UpdateProductCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "", "slug", "Desc", true));
        updProdResult.IsFailure.Should().BeTrue();
        updProdResult.Error.Type.Should().Be(ErrorType.Validation);

        // CreateVariant validation failure (empty SKU)
        var createVarHandler = new CreateVariantCommandHandler(prodRepo.Object, cache.Object, uow.Object, clock.Object, new CreateVariantCommandValidator());
        var varResult = await createVarHandler.HandleAsync(new CreateVariantCommand(Guid.NewGuid(), "", null, null, -10m, -1, -1));
        varResult.IsFailure.Should().BeTrue();
        varResult.Error.Type.Should().Be(ErrorType.Validation);

        // UpdateVariant validation failure (empty SKU)
        var updateVarHandler = new UpdateVariantCommandHandler(prodRepo.Object, cache.Object, uow.Object, clock.Object, new UpdateVariantCommandValidator());
        var updVarResult = await updateVarHandler.HandleAsync(new UpdateVariantCommand(Guid.NewGuid(), "", null, null, -10m, -1));
        updVarResult.IsFailure.Should().BeTrue();
        updVarResult.Error.Type.Should().Be(ErrorType.Validation);

        // AdjustStock validation failure (zero quantity)
        var adjustHandler = new AdjustStockCommandHandler(prodRepo.Object, cache.Object, user.Object, uow.Object, clock.Object, new AdjustStockCommandValidator());
        var adjustResult = await adjustHandler.HandleAsync(new AdjustStockCommand(Guid.NewGuid(), 0, ""));
        adjustResult.IsFailure.Should().BeTrue();
        adjustResult.Error.Type.Should().Be(ErrorType.Validation);
    }
}
