using FluentValidation;

namespace SportShop.Application.Features.Orders.Admin.GetAdminOrder;

public sealed class GetAdminOrderByNumberQueryValidator : AbstractValidator<GetAdminOrderByNumberQuery>
{
    public GetAdminOrderByNumberQueryValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty()
            .WithMessage("Nomor pesanan wajib diisi.")
            .MaximumLength(30)
            .WithMessage("Nomor pesanan maksimal 30 karakter.");
    }
}
