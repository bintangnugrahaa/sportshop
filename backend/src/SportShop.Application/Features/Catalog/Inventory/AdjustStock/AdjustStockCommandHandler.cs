using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;
using SportShop.Domain.Inventory;

namespace SportShop.Application.Features.Catalog.Inventory.AdjustStock;

public sealed class AdjustStockCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<AdjustStockCommand> _validator;

    public AdjustStockCommandHandler(
        IProductRepository productRepository,
        ICacheService cacheService,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<AdjustStockCommand> validator)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<InventoryMovementDto>> HandleAsync(AdjustStockCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<InventoryMovementDto>(Error.Validation("inventory.validation_error", "Data penyesuaian stok tidak valid.", errors));
        }

        var variant = await _productRepository.GetVariantByIdAsync(command.VariantId, cancellationToken);
        if (variant is null)
        {
            return Result.Failure<InventoryMovementDto>(Error.NotFound("variant.not_found", "Varian tidak ditemukan."));
        }

        if (variant.StockQuantity + command.ChangeQuantity < 0)
        {
            return Result.Failure<InventoryMovementDto>(Error.Conflict("stock.insufficient", $"Stok tidak mencukupi untuk pengurangan sebesar {Math.Abs(command.ChangeQuantity)}. Stok saat ini: {variant.StockQuantity}."));
        }

        var now = _clock.UtcNow;
        variant.AdjustStock(command.ChangeQuantity, now);

        var movement = new InventoryMovement(
            Guid.NewGuid(),
            variant.Id,
            command.ChangeQuantity,
            StockReason.ManualAdjust,
            command.Note,
            null,
            _currentUser.UserId,
            now);

        await _productRepository.AddInventoryMovementAsync(movement, cancellationToken);
        await _productRepository.UpdateVariantAsync(variant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var product = await _productRepository.GetByIdAsync(variant.ProductId, false, cancellationToken);
        if (product is not null)
        {
            await _cacheService.RemoveAsync($"prod:detail:{product.Slug.ToLowerInvariant()}:v1", cancellationToken);
        }

        return Result.Success(CatalogMapper.ToDto(movement));
    }
}
