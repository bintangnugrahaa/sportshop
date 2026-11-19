using FluentValidation;

namespace SportShop.Application.Features.Shipping.GetQuote;

public sealed class GetShippingQuoteQueryValidator : AbstractValidator<GetShippingQuoteQuery>
{
    public GetShippingQuoteQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID wajib diisi.");

        RuleFor(x => x.AddressId)
            .NotEmpty().WithMessage("Address ID wajib diisi.");
    }
}
