using FluentValidation;

namespace SportShop.Application.Features.Catalog.Inventory.AdjustStock;

public sealed class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("ID varian wajib diisi.");

        RuleFor(x => x.ChangeQuantity)
            .NotEqual(0).WithMessage("Jumlah perubahan stok tidak boleh 0.");

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Catatan wajib diisi untuk penyesuaian manual.")
            .MaximumLength(300).WithMessage("Catatan maksimal 300 karakter.");
    }
}
