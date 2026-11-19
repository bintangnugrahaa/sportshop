using FluentValidation;

namespace SportShop.Application.Features.Orders.Admin.CancelAdminOrder;

public sealed class CancelAdminOrderCommandValidator : AbstractValidator<CancelAdminOrderCommand>
{
    public CancelAdminOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty()
            .WithMessage("Nomor pesanan wajib diisi.")
            .MaximumLength(30)
            .WithMessage("Nomor pesanan maksimal 30 karakter.");

        RuleFor(x => x.Reason)
            .MaximumLength(300)
            .WithMessage("Alasan pembatalan maksimal 300 karakter.");
    }
}
