using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Cart.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Cart.RemoveItem;

public sealed class RemoveCartItemCommandHandler
{
    private readonly ICartRepository _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RemoveCartItemCommandHandler(
        ICartRepository cartRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CartDto>> HandleAsync(RemoveCartItemCommand command, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetByUserIdAsync(command.UserId, includeDetails: true, cancellationToken);
        if (cart is null)
        {
            return Result.Failure<CartDto>(Error.NotFound("cart.not_found", "Keranjang tidak ditemukan."));
        }

        var item = cart.Items.FirstOrDefault(i => i.VariantId == command.VariantId);
        if (item is not null)
        {
            cart.RemoveItem(command.VariantId, _clock.UtcNow);
            await _cartRepository.RemoveItemAsync(item, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var updatedCart = await _cartRepository.GetByUserIdAsync(command.UserId, includeDetails: true, cancellationToken);
        return Result.Success(updatedCart!.ToDto());
    }
}
