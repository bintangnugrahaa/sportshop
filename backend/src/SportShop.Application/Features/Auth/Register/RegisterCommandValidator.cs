using FluentValidation;

namespace SportShop.Application.Features.Auth.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .EmailAddress().WithMessage("Format email tidak valid.")
            .MaximumLength(256).WithMessage("Email maksimal 256 karakter.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Kata sandi wajib diisi.")
            .MinimumLength(8).WithMessage("Kata sandi minimal 8 karakter.")
            .Matches(@"[A-Z]").WithMessage("Kata sandi harus mengandung huruf besar.")
            .Matches(@"[a-z]").WithMessage("Kata sandi harus mengandung huruf kecil.")
            .Matches(@"[0-9]").WithMessage("Kata sandi harus mengandung angka.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nama lengkap wajib diisi.")
            .MaximumLength(150).WithMessage("Nama lengkap maksimal 150 karakter.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Nomor telepon maksimal 20 karakter.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
