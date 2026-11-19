using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Cart.Clear;

public sealed class ClearCartCommandHandler
{
    private readonly ICartRepository _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ClearCartCommandHandler(
        ICartRepository cartRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(ClearCartCommand command, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetByUserIdAsync(command.UserId, includeDetails: true, cancellationToken);
        if (cart is not null)
        {
            foreach (var item in cart.Items.ToList())
            {
                await _cartRepository.RemoveItemAsync(item, cancellationToken);
            }

            cart.Clear(_clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
