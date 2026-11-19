using FluentValidation;

namespace SportShop.Application.Features.Orders.Admin.ProcessOrder;

public sealed class ProcessOrderCommandValidator : AbstractValidator<ProcessOrderCommand>
{
    public ProcessOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty()
            .WithMessage("Nomor pesanan wajib diisi.")
            .MaximumLength(30)
            .WithMessage("Nomor pesanan maksimal 30 karakter.");

        RuleFor(x => x.Note)
            .MaximumLength(300)
            .WithMessage("Catatan maksimal 300 karakter.");
    }
}
