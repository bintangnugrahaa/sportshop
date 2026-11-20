using FluentValidation;

namespace SportShop.Application.Features.Orders.PayOrder;

public sealed class PayOrderCommandValidator : AbstractValidator<PayOrderCommand>
{
    public PayOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("Nomor pesanan wajib diisi.")
            .MaximumLength(30).WithMessage("Nomor pesanan maksimal 30 karakter.");
    }
}
