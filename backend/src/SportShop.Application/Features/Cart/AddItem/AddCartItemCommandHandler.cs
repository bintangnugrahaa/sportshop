using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Cart.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Cart.AddItem;

public sealed class AddCartItemCommandHandler
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<AddCartItemCommand> _validator;

    public AddCartItemCommandHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<AddCartItemCommand> validator)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<CartDto>> HandleAsync(AddCartItemCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<CartDto>(Error.Validation("cart.validation_error", "Data item keranjang tidak valid.", errors));
        }

        var variant = await _productRepository.GetVariantByIdAsync(command.VariantId, cancellationToken);
        if (variant is null)
        {
            return Result.Failure<CartDto>(Error.NotFound("variant.not_found", "Varian produk tidak ditemukan."));
        }

        if (!variant.IsActive || (variant.Product is not null && !variant.Product.IsActive))
        {
            return Result.Failure<CartDto>(Error.BusinessRule("variant.inactive", "Varian produk sedang tidak aktif."));
        }

        var cart = await _cartRepository.GetOrCreateByUserIdAsync(command.UserId, cancellationToken);

        var existingItem = cart.Items.FirstOrDefault(i => i.VariantId == command.VariantId);
        var targetQuantity = (existingItem?.Quantity ?? 0) + command.Quantity;

        if (targetQuantity > variant.StockQuantity)
        {
            return Result.Failure<CartDto>(Error.Conflict("stock.insufficient", $"Stok varian {variant.Sku} hanya tersisa {variant.StockQuantity}."));
        }

        var isNewItem = existingItem is null;
        var item = cart.AddItem(variant, command.Quantity, _clock.UtcNow);
        if (isNewItem)
        {
            await _cartRepository.AddItemAsync(item, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload fresh state with product details
        var updatedCart = await _cartRepository.GetByUserIdAsync(command.UserId, includeDetails: true, cancellationToken);
        return Result.Success(updatedCart!.ToDto());
    }
}
