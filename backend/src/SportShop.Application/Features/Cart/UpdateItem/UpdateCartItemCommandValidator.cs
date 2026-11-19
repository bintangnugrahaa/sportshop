using FluentValidation;

namespace SportShop.Application.Features.Cart.UpdateItem;

public sealed class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID wajib diisi.");

        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("Variant ID wajib diisi.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Jumlah item harus lebih dari 0.");
    }
}
