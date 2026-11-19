using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Users.GetProfile;

public sealed record GetProfileQuery(Guid UserId);

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    IReadOnlyCollection<string> Roles,
    DateTime CreatedAt);

public sealed class GetProfileQueryHandler
{
    private readonly IIdentityService _identityService;

    public GetProfileQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<UserProfileResponse>> HandleAsync(GetProfileQuery query, CancellationToken cancellationToken = default)
    {
        var result = await _identityService.GetUserProfileAsync(query.UserId, cancellationToken);
        if (result.IsFailure)
        {
            return Result.Failure<UserProfileResponse>(result.Error);
        }

        var p = result.Value;
        return Result.Success(new UserProfileResponse(p.Id, p.Email, p.FullName, p.PhoneNumber, p.Roles, p.CreatedAt));
    }
}
