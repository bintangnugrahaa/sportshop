using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Cart.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Cart.UpdateItem;

public sealed class UpdateCartItemCommandHandler
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<UpdateCartItemCommand> _validator;

    public UpdateCartItemCommandHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<UpdateCartItemCommand> validator)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<CartDto>> HandleAsync(UpdateCartItemCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<CartDto>(Error.Validation("cart.validation_error", "Data perubahan jumlah keranjang tidak valid.", errors));
        }

        var cart = await _cartRepository.GetByUserIdAsync(command.UserId, includeDetails: true, cancellationToken);
        if (cart is null)
        {
            return Result.Failure<CartDto>(Error.NotFound("cart.not_found", "Keranjang tidak ditemukan."));
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.VariantId == command.VariantId);
        if (existingItem is null)
        {
            return Result.Failure<CartDto>(Error.NotFound("cart.item_not_found", "Item tidak ditemukan di keranjang."));
        }

        var variant = await _productRepository.GetVariantByIdAsync(command.VariantId, cancellationToken);
        if (variant is null)
        {
            return Result.Failure<CartDto>(Error.NotFound("variant.not_found", "Varian produk tidak ditemukan."));
        }

        if (command.Quantity > variant.StockQuantity)
        {
            return Result.Failure<CartDto>(Error.Conflict("stock.insufficient", $"Stok varian {variant.Sku} hanya tersisa {variant.StockQuantity}."));
        }

        cart.UpdateItemQuantity(command.VariantId, command.Quantity, variant.StockQuantity, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedCart = await _cartRepository.GetByUserIdAsync(command.UserId, includeDetails: true, cancellationToken);
        return Result.Success(updatedCart!.ToDto());
    }
}
