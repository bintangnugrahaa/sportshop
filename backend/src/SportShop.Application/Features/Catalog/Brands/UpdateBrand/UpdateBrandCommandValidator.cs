using FluentValidation;

namespace SportShop.Application.Features.Catalog.Brands.UpdateBrand;

public sealed class UpdateBrandCommandValidator : AbstractValidator<UpdateBrandCommand>
{
    public UpdateBrandCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID brand wajib diisi.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama brand wajib diisi.")
            .MaximumLength(100).WithMessage("Nama brand maksimal 100 karakter.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug brand wajib diisi.")
            .MaximumLength(120).WithMessage("Slug brand maksimal 120 karakter.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Format slug tidak valid (hanya huruf kecil, angka, dan tanda hubung).");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("URL logo maksimal 500 karakter.")
            .Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Format URL logo tidak valid.");
    }
}
