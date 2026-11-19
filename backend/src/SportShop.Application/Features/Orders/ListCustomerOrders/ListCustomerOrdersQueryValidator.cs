using FluentValidation;

namespace SportShop.Application.Features.Orders.ListCustomerOrders;

public sealed class ListCustomerOrdersQueryValidator : AbstractValidator<ListCustomerOrdersQuery>
{
    public ListCustomerOrdersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page harus minimal 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize harus antara 1 dan 100.");
    }
}
