using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Users.ChangePassword;

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword);

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Kata sandi saat ini wajib diisi.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Kata sandi baru wajib diisi.")
            .MinimumLength(8).WithMessage("Kata sandi baru minimal 8 karakter.")
            .Matches(@"[A-Z]").WithMessage("Kata sandi baru harus mengandung huruf besar.")
            .Matches(@"[a-z]").WithMessage("Kata sandi baru harus mengandung huruf kecil.")
            .Matches(@"[0-9]").WithMessage("Kata sandi baru harus mengandung angka.");
    }
}

public sealed class ChangePasswordCommandHandler
{
    private readonly IIdentityService _identityService;
    private readonly IValidator<ChangePasswordCommand> _validator;

    public ChangePasswordCommandHandler(IIdentityService identityService, IValidator<ChangePasswordCommand> validator)
    {
        _identityService = identityService;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
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

        return await _identityService.ChangePasswordAsync(command.UserId, command.CurrentPassword, command.NewPassword, cancellationToken);
    }
}
