using FluentValidation;

namespace SportShop.Application.Features.Orders.Checkout;

public sealed class CheckoutOrderCommandValidator : AbstractValidator<CheckoutOrderCommand>
{
    public CheckoutOrderCommandValidator()
    {
        RuleFor(x => x.AddressId)
            .NotEmpty().WithMessage("Alamat pengiriman wajib dipilih.");

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(100).WithMessage("Idempotency key maksimal 100 karakter.");
    }
}
