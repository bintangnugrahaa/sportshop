using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Auth.Login;

public sealed class LoginCommandHandler
{
    private readonly IIdentityService _identityService;
    private readonly IValidator<LoginCommand> _validator;

    public LoginCommandHandler(IIdentityService identityService, IValidator<LoginCommand> validator)
    {
        _identityService = identityService;
        _validator = validator;
    }

    public async Task<Result<AuthTokenResult>> HandleAsync(
        LoginCommand command,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Result.Failure<AuthTokenResult>(Error.Validation(
                "validation.failed",
                "Satu atau lebih validasi gagal.",
                errors));
        }

        return await _identityService.LoginAsync(command.Email, command.Password, ipAddress, cancellationToken);
    }
}
