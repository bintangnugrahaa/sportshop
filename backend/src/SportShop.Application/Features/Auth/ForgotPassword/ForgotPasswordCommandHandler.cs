using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email);

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .EmailAddress().WithMessage("Format email tidak valid.");
    }
}

public sealed class ForgotPasswordCommandHandler
{
    private readonly IIdentityService _identityService;
    private readonly IEmailSender _emailSender;
    private readonly IValidator<ForgotPasswordCommand> _validator;

    public ForgotPasswordCommandHandler(
        IIdentityService identityService,
        IEmailSender emailSender,
        IValidator<ForgotPasswordCommand> validator)
    {
        _identityService = identityService;
        _emailSender = emailSender;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken = default)
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

        var tokenResult = await _identityService.GeneratePasswordResetTokenAsync(command.Email, cancellationToken);
        if (tokenResult.IsSuccess && !string.IsNullOrEmpty(tokenResult.Value))
        {
            var body = $"Gunakan token berikut untuk mereset kata sandi akun SportShop Anda:\n\n{tokenResult.Value}\n\nToken ini berlaku selama 24 jam dan hanya dapat digunakan satu kali.";
            await _emailSender.SendEmailAsync(
                command.Email,
                "Reset Kata Sandi Akun SportShop",
                body,
                isHtml: false,
                cancellationToken);
        }

        return Result.Success();
    }
}
