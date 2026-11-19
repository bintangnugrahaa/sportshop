using FluentValidation;

namespace SportShop.Application.Features.Orders.Admin.ListAdminOrders;

public sealed class ListAdminOrdersQueryValidator : AbstractValidator<ListAdminOrdersQuery>
{
    public ListAdminOrdersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Halaman harus lebih besar atau sama dengan 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Ukuran halaman harus antara 1 dan 100.");

        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate.Value <= x.ToDate.Value)
            .WithMessage("Tanggal awal tidak boleh melebihi tanggal akhir.");
    }
}
