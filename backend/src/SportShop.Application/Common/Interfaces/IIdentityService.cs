namespace SportShop.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<Guid>> RegisterUserAsync(
        string email,
        string password,
        string fullName,
        string? phoneNumber,
        CancellationToken cancellationToken = default);
}
