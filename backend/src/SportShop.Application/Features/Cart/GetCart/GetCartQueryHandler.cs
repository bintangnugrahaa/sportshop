using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Cart.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Cart.GetCart;

public sealed class GetCartQueryHandler
{
    private readonly ICartRepository _cartRepository;

    public GetCartQueryHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<Result<CartDto>> HandleAsync(GetCartQuery query, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetOrCreateByUserIdAsync(query.UserId, cancellationToken);
        return Result.Success(cart.ToDto());
    }
}
