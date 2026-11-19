using FluentValidation;

namespace SportShop.Application.Features.Orders.Admin.ShipOrder;

public sealed class ShipOrderCommandValidator : AbstractValidator<ShipOrderCommand>
{
    public ShipOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty()
            .WithMessage("Nomor pesanan wajib diisi.")
            .MaximumLength(30)
            .WithMessage("Nomor pesanan maksimal 30 karakter.");

        RuleFor(x => x.Courier)
            .NotEmpty()
            .WithMessage("Nama kurir wajib diisi.")
            .MaximumLength(50)
            .WithMessage("Nama kurir maksimal 50 karakter.");

        RuleFor(x => x.TrackingNumber)
            .NotEmpty()
            .WithMessage("Nomor resi wajib diisi.")
            .MaximumLength(80)
            .WithMessage("Nomor resi maksimal 80 karakter.");

        RuleFor(x => x.Note)
            .MaximumLength(300)
            .WithMessage("Catatan maksimal 300 karakter.");
    }
}
