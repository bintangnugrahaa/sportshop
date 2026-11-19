using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;

namespace SportShop.Application.Features.Catalog.Products.CreateProduct;

public sealed class CreateProductCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<CreateProductCommand> _validator;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IBrandRepository brandRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<CreateProductCommand> validator)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<ProductDto>> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<ProductDto>(Error.Validation("product.validation_error", "Data produk tidak valid.", errors));
        }

        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound("category.not_found", "Kategori tidak ditemukan."));
        }

        var brand = await _brandRepository.GetByIdAsync(command.BrandId, cancellationToken);
        if (brand is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound("brand.not_found", "Brand tidak ditemukan."));
        }

        if (await _productRepository.SlugExistsAsync(command.Slug, null, cancellationToken))
        {
            return Result.Failure<ProductDto>(Error.Conflict("product.slug_duplicate", "Slug produk sudah digunakan."));
        }

        if (command.Variants is not null && command.Variants.Count > 0)
        {
            var duplicateSkuInRequest = command.Variants
                .GroupBy(v => v.Sku, StringComparer.OrdinalIgnoreCase)
                .Any(g => g.Count() > 1);

            if (duplicateSkuInRequest)
            {
                return Result.Failure<ProductDto>(Error.Validation("variant.sku_duplicate", "SKU dalam daftar varian tidak boleh duplikat."));
            }

            foreach (var variantItem in command.Variants)
            {
                if (await _productRepository.SkuExistsAsync(variantItem.Sku, null, cancellationToken))
                {
                    return Result.Failure<ProductDto>(Error.Conflict("variant.sku_duplicate", $"SKU '{variantItem.Sku}' sudah digunakan."));
                }
            }
        }

        var now = _clock.UtcNow;
        var product = new Product(
            Guid.NewGuid(),
            command.CategoryId,
            command.BrandId,
            command.Name,
            command.Slug,
            command.Description,
            command.BasePrice,
            command.IsActive,
            now);

        if (command.Variants is not null && command.Variants.Count > 0)
        {
            foreach (var item in command.Variants)
            {
                var variant = new ProductVariant(
                    Guid.NewGuid(),
                    product.Id,
                    item.Sku,
                    item.Price,
                    item.WeightGram,
                    item.StockQuantity,
                    item.OptionSize,
                    item.OptionColor,
                    item.IsActive,
                    now);

                product.Variants.Add(variant);

                if (item.StockQuantity > 0)
                {
                    var movement = new InventoryMovement(
                        Guid.NewGuid(),
                        variant.Id,
                        item.StockQuantity,
                        StockReason.Initial,
                        "Stok awal produk",
                        null,
                        null,
                        now);
                    await _productRepository.AddInventoryMovementAsync(movement, cancellationToken);
                }
            }

            product.RecalculateBasePrice(now);
        }

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        product.Category = category;
        product.Brand = brand;

        return Result.Success(CatalogMapper.ToDto(product));
    }
}
