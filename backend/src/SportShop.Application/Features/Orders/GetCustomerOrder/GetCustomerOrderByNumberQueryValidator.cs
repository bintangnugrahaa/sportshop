using FluentValidation;

namespace SportShop.Application.Features.Orders.GetCustomerOrder;

public sealed class GetCustomerOrderByNumberQueryValidator : AbstractValidator<GetCustomerOrderByNumberQuery>
{
    public GetCustomerOrderByNumberQueryValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("Nomor pesanan wajib diisi.")
            .MaximumLength(30).WithMessage("Nomor pesanan maksimal 30 karakter.");
    }
}
