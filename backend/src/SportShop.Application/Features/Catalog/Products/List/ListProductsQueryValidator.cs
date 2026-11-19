using FluentValidation;

namespace SportShop.Application.Features.Catalog.Products.List;

public sealed class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
{
    private static readonly string[] _validSortOptions = ["newest", "price_asc", "price_desc", "name"];

    public ListProductsQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Halaman harus lebih besar dari atau sama dengan 1.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Ukuran halaman harus antara 1 dan 100.");

        RuleFor(q => q.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(q => q.MinPrice.HasValue)
            .WithMessage("Harga minimum tidak boleh negatif.");

        RuleFor(q => q.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(q => q.MaxPrice.HasValue)
            .WithMessage("Harga maksimum tidak boleh negatif.");

        RuleFor(q => q)
            .Must(q => !q.MinPrice.HasValue || !q.MaxPrice.HasValue || q.MaxPrice.Value >= q.MinPrice.Value)
            .WithMessage("Harga maksimum harus lebih besar dari atau sama dengan harga minimum.");

        RuleFor(q => q.Sort)
            .Must(s => string.IsNullOrEmpty(s) || _validSortOptions.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage("Opsi sortir tidak valid. Pilihan yang tersedia: newest, price_asc, price_desc, name.");
    }
}
