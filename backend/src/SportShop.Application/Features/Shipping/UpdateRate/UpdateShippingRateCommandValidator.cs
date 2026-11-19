using FluentValidation;

namespace SportShop.Application.Features.Shipping.UpdateRate;

public sealed class UpdateShippingRateCommandValidator : AbstractValidator<UpdateShippingRateCommand>
{
    public UpdateShippingRateCommandValidator()
    {
        RuleFor(x => x.ProvinceCode)
            .NotEmpty().WithMessage("Kode provinsi wajib diisi.")
            .MaximumLength(10).WithMessage("Kode provinsi maksimal 10 karakter.");

        RuleFor(x => x.BaseFee)
            .GreaterThanOrEqualTo(0).WithMessage("Biaya dasar tidak boleh negatif.");

        RuleFor(x => x.PerKgFee)
            .GreaterThanOrEqualTo(0).WithMessage("Biaya per kg tidak boleh negatif.");

        RuleFor(x => x.EstimatedDaysMin)
            .GreaterThanOrEqualTo((short)0).WithMessage("Estimasi hari minimum tidak boleh negatif.");

        RuleFor(x => x.EstimatedDaysMax)
            .GreaterThanOrEqualTo(x => x.EstimatedDaysMin).WithMessage("Estimasi hari maksimum harus lebih besar atau sama dengan minimum.");
    }
}
