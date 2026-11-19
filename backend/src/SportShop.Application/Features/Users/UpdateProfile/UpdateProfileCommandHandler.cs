using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Users.GetProfile;

namespace SportShop.Application.Features.Users.UpdateProfile;

public sealed record UpdateProfileCommand(Guid UserId, string FullName, string? PhoneNumber);

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nama lengkap wajib diisi.")
            .MaximumLength(150).WithMessage("Nama lengkap maksimal 150 karakter.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Nomor telepon maksimal 20 karakter.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}

public sealed class UpdateProfileCommandHandler
{
    private readonly IIdentityService _identityService;
    private readonly IValidator<UpdateProfileCommand> _validator;

    public UpdateProfileCommandHandler(IIdentityService identityService, IValidator<UpdateProfileCommand> validator)
    {
        _identityService = identityService;
        _validator = validator;
    }

    public async Task<Result<UserProfileResponse>> HandleAsync(UpdateProfileCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Result.Failure<UserProfileResponse>(Error.Validation(
                "validation.failed",
                "Satu atau lebih validasi gagal.",
                errors));
        }

        var result = await _identityService.UpdateUserProfileAsync(command.UserId, command.FullName, command.PhoneNumber, cancellationToken);
        if (result.IsFailure)
        {
            return Result.Failure<UserProfileResponse>(result.Error);
        }

        var p = result.Value;
        return Result.Success(new UserProfileResponse(p.Id, p.Email, p.FullName, p.PhoneNumber, p.Roles, p.CreatedAt));
    }
}
