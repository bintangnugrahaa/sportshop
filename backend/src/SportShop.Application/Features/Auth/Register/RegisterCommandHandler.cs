using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Auth.Register;

public sealed class RegisterCommandHandler
{
    private readonly IIdentityService _identityService;
    private readonly IValidator<RegisterCommand> _validator;

    public RegisterCommandHandler(IIdentityService identityService, IValidator<RegisterCommand> validator)
    {
        _identityService = identityService;
        _validator = validator;
    }

    public async Task<Result<RegisterResponse>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Result.Failure<RegisterResponse>(Error.Validation(
                "validation.failed",
                "Satu atau lebih validasi gagal.",
                errors));
        }

        var result = await _identityService.RegisterUserAsync(
            command.Email,
            command.Password,
            command.FullName,
            command.PhoneNumber,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<RegisterResponse>(result.Error);
        }

        return Result.Success(new RegisterResponse(result.Value, command.Email, command.FullName));
    }
}
