using FluentValidation;

namespace SportShop.Application.Features.Catalog.Products.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
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

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Harga dasar tidak boleh negatif.");

        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(x => x.Sku)
                .NotEmpty().WithMessage("SKU varian wajib diisi.")
                .MaximumLength(64).WithMessage("SKU varian maksimal 64 karakter.");

            v.RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Harga varian tidak boleh negatif.");

            v.RuleFor(x => x.WeightGram)
                .GreaterThan(0).WithMessage("Berat varian harus lebih dari 0 gram.");

            v.RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stok varian tidak boleh negatif.");
        });
    }
}
