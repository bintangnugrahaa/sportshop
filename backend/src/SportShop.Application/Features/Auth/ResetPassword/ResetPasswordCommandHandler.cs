using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Auth.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword);

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .EmailAddress().WithMessage("Format email tidak valid.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token reset wajib diisi.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Kata sandi baru wajib diisi.")
            .MinimumLength(8).WithMessage("Kata sandi baru minimal 8 karakter.")
            .Matches(@"[A-Z]").WithMessage("Kata sandi baru harus mengandung huruf besar.")
            .Matches(@"[a-z]").WithMessage("Kata sandi baru harus mengandung huruf kecil.")
            .Matches(@"[0-9]").WithMessage("Kata sandi baru harus mengandung angka.");
    }
}

public sealed class ResetPasswordCommandHandler
{
    private readonly IIdentityService _identityService;
    private readonly IValidator<ResetPasswordCommand> _validator;

    public ResetPasswordCommandHandler(
        IIdentityService identityService,
        IValidator<ResetPasswordCommand> validator)
    {
        _identityService = identityService;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Result.Failure(Error.Validation(
                "validation.failed",
                "Satu atau lebih validasi gagal.",
                errors));
        }

        return await _identityService.ResetPasswordAsync(command.Email, command.Token, command.NewPassword, cancellationToken);
    }
}
