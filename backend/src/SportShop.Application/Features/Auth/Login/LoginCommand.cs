namespace SportShop.Application.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password);

public sealed record LoginResponse(string AccessToken, int ExpiresIn);
