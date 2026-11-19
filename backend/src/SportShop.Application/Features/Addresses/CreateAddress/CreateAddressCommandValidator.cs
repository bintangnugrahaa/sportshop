using FluentValidation;

namespace SportShop.Application.Features.Addresses.CreateAddress;

public sealed class CreateAddressCommandValidator : AbstractValidator<CreateAddressCommand>
{
    public CreateAddressCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID wajib diisi.");

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Label alamat wajib diisi.")
            .MaximumLength(50).WithMessage("Label alamat maksimal 50 karakter.");

        RuleFor(x => x.RecipientName)
            .NotEmpty().WithMessage("Nama penerima wajib diisi.")
            .MaximumLength(150).WithMessage("Nama penerima maksimal 150 karakter.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Nomor telepon wajib diisi.")
            .MaximumLength(20).WithMessage("Nomor telepon maksimal 20 karakter.");

        RuleFor(x => x.ProvinceCode)
            .NotEmpty().WithMessage("Provinsi wajib dipilih.")
            .MaximumLength(10).WithMessage("Kode provinsi maksimal 10 karakter.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("Kota/kabupaten wajib diisi.")
            .MaximumLength(100).WithMessage("Kota/kabupaten maksimal 100 karakter.");

        RuleFor(x => x.District)
            .MaximumLength(100).WithMessage("Kecamatan maksimal 100 karakter.");

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Kode pos wajib diisi.")
            .MaximumLength(10).WithMessage("Kode pos maksimal 10 karakter.");

        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Alamat jalan wajib diisi.")
            .MaximumLength(300).WithMessage("Alamat jalan maksimal 300 karakter.");
    }
}
