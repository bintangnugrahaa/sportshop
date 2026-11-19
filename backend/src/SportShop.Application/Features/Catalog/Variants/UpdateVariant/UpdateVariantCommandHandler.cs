using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Catalog.Variants.UpdateVariant;

public sealed class UpdateVariantCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<UpdateVariantCommand> _validator;

    public UpdateVariantCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<UpdateVariantCommand> validator)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<ProductVariantDto>> HandleAsync(UpdateVariantCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<ProductVariantDto>(Error.Validation("variant.validation_error", "Data varian tidak valid.", errors));
        }

        var variant = await _productRepository.GetVariantByIdAsync(command.Id, cancellationToken);
        if (variant is null)
        {
            return Result.Failure<ProductVariantDto>(Error.NotFound("variant.not_found", "Varian tidak ditemukan."));
        }

        if (await _productRepository.SkuExistsAsync(command.Sku, command.Id, cancellationToken))
        {
            return Result.Failure<ProductVariantDto>(Error.Conflict("variant.sku_duplicate", $"SKU '{command.Sku}' sudah digunakan."));
        }

        if (await _productRepository.VariantOptionsExistAsync(variant.ProductId, command.OptionSize, command.OptionColor, command.Id, cancellationToken))
        {
            return Result.Failure<ProductVariantDto>(Error.Conflict("variant.options_duplicate", "Kombinasi ukuran dan warna sudah ada untuk produk ini."));
        }

        var now = _clock.UtcNow;
        variant.Update(
            command.Sku,
            command.OptionSize,
            command.OptionColor,
            command.Price,
            command.WeightGram,
            command.IsActive,
            now);

        await _productRepository.UpdateVariantAsync(variant, cancellationToken);

        var product = await _productRepository.GetByIdAsync(variant.ProductId, true, cancellationToken);
        if (product is not null)
        {
            product.RecalculateBasePrice(now);
            await _productRepository.UpdateAsync(product, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CatalogMapper.ToDto(variant));
    }
}
