using FluentValidation;

namespace SportShop.Application.Features.Orders.CancelOrder;

public sealed class CancelCustomerOrderCommandValidator : AbstractValidator<CancelCustomerOrderCommand>
{
    public CancelCustomerOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("Nomor pesanan wajib diisi.")
            .MaximumLength(30).WithMessage("Nomor pesanan maksimal 30 karakter.");

        RuleFor(x => x.Reason)
            .MaximumLength(300).WithMessage("Alasan pembatalan maksimal 300 karakter.");
    }
}
