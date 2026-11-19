using FluentValidation;

namespace SportShop.Application.Features.Catalog.Categories.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID kategori wajib diisi.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama kategori wajib diisi.")
            .MaximumLength(100).WithMessage("Nama kategori maksimal 100 karakter.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug kategori wajib diisi.")
            .MaximumLength(120).WithMessage("Slug kategori maksimal 120 karakter.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Format slug tidak valid (hanya huruf kecil, angka, dan tanda hubung).");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Urutan tampil harus lebih besar atau sama dengan 0.");
    }
}
