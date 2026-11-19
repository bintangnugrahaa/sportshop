using FluentValidation;

namespace SportShop.Application.Features.Catalog.Products.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID produk wajib diisi.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategori wajib dipilih.");

        RuleFor(x => x.BrandId)
            .NotEmpty().WithMessage("Brand wajib dipilih.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama produk wajib diisi.")
            .MaximumLength(200).WithMessage("Nama produk maksimal 200 karakter.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug produk wajib diisi.")
            .MaximumLength(220).WithMessage("Slug produk maksimal 220 karakter.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Format slug tidak valid (hanya huruf kecil, angka, dan tanda hubung).");
    }
}
