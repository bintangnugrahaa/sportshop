using FluentValidation;

namespace SportShop.Application.Features.Catalog.Brands.CreateBrand;

public sealed class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
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
