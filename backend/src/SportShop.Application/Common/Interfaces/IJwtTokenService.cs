using System.Security.Claims;

namespace SportShop.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string fullName, IEnumerable<string> roles);
    (string RawToken, string TokenHash) GenerateRefreshToken();
    string HashToken(string token);
    ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = false);
}
