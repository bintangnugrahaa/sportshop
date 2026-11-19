using FluentValidation;

namespace SportShop.Application.Features.Catalog.Variants.CreateVariant;

public sealed class CreateVariantCommandValidator : AbstractValidator<CreateVariantCommand>
{
    public CreateVariantCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ID produk wajib diisi.");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU varian wajib diisi.")
            .MaximumLength(64).WithMessage("SKU varian maksimal 64 karakter.");

        RuleFor(x => x.OptionSize)
            .MaximumLength(30).WithMessage("Ukuran varian maksimal 30 karakter.");

        RuleFor(x => x.OptionColor)
            .MaximumLength(30).WithMessage("Warna varian maksimal 30 karakter.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Harga varian tidak boleh negatif.");

        RuleFor(x => x.WeightGram)
            .GreaterThan(0).WithMessage("Berat varian harus lebih dari 0 gram.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stok varian tidak boleh negatif.");
    }
}
