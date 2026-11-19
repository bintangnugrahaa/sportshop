using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Auth.ConfirmEmail;

public sealed record ConfirmEmailCommand(Guid UserId, string Token);

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId wajib diisi.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token konfirmasi email wajib diisi.");
    }
}

public sealed class ConfirmEmailCommandHandler
{
    private readonly IIdentityService _identityService;
    private readonly IValidator<ConfirmEmailCommand> _validator;

    public ConfirmEmailCommandHandler(
        IIdentityService identityService,
        IValidator<ConfirmEmailCommand> validator)
    {
        _identityService = identityService;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(ConfirmEmailCommand command, CancellationToken cancellationToken = default)
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

        return await _identityService.ConfirmEmailAsync(command.UserId, command.Token, cancellationToken);
    }
}
