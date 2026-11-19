using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;

namespace SportShop.Application.Features.Catalog.Variants.CreateVariant;

public sealed class CreateVariantCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<CreateVariantCommand> _validator;

    public CreateVariantCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<CreateVariantCommand> validator)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<ProductVariantDto>> HandleAsync(CreateVariantCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<ProductVariantDto>(Error.Validation("variant.validation_error", "Data varian tidak valid.", errors));
        }

        var product = await _productRepository.GetByIdAsync(command.ProductId, true, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductVariantDto>(Error.NotFound("product.not_found", "Produk tidak ditemukan."));
        }

        if (await _productRepository.SkuExistsAsync(command.Sku, null, cancellationToken))
        {
            return Result.Failure<ProductVariantDto>(Error.Conflict("variant.sku_duplicate", $"SKU '{command.Sku}' sudah digunakan."));
        }

        if (await _productRepository.VariantOptionsExistAsync(command.ProductId, command.OptionSize, command.OptionColor, null, cancellationToken))
        {
            return Result.Failure<ProductVariantDto>(Error.Conflict("variant.options_duplicate", "Kombinasi ukuran dan warna sudah ada untuk produk ini."));
        }

        var now = _clock.UtcNow;
        var variant = new ProductVariant(
            Guid.NewGuid(),
            command.ProductId,
            command.Sku,
            command.Price,
            command.WeightGram,
            command.StockQuantity,
            command.OptionSize,
            command.OptionColor,
            command.IsActive,
            now);

        await _productRepository.AddVariantAsync(variant, cancellationToken);

        if (command.StockQuantity > 0)
        {
            var movement = new InventoryMovement(
                Guid.NewGuid(),
                variant.Id,
                command.StockQuantity,
                StockReason.Initial,
                "Stok awal varian baru",
                null,
                null,
                now);
            await _productRepository.AddInventoryMovementAsync(movement, cancellationToken);
        }

        product.Variants.Add(variant);
        product.RecalculateBasePrice(now);
        await _productRepository.UpdateAsync(product, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CatalogMapper.ToDto(variant));
    }
}
