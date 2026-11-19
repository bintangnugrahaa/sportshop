namespace SportShop.Application.Features.Auth.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber);

public sealed record RegisterResponse(
    Guid Id,
    string Email,
    string FullName);
