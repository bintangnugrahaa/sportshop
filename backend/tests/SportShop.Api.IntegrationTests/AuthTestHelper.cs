using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SportShop.Api.IntegrationTests;

public static class AuthTestHelper
{
    public const string DefaultSigningKey = "super-secret-key-that-is-at-least-32-characters-long!";
    public const string DefaultIssuer = "SportShopApi";
    public const string DefaultAudience = "SportShopClient";

    public static string GenerateJwtToken(
        Guid? userId = null,
        string email = "user@sportshop.dev",
        string role = "Customer",
        TimeSpan? expiry = null,
        string? signingKey = null,
        string? issuer = null,
        string? audience = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? DefaultSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, (userId ?? Guid.NewGuid()).ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role)
        };

        var now = DateTime.UtcNow;
        var tokenExpiry = expiry ?? TimeSpan.FromMinutes(15);
        var notBefore = tokenExpiry < TimeSpan.Zero ? now.Add(tokenExpiry).AddMinutes(-5) : now;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            NotBefore = notBefore,
            Expires = now.Add(tokenExpiry),
            Issuer = issuer ?? DefaultIssuer,
            Audience = audience ?? DefaultAudience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static string GenerateCustomerToken(Guid? userId = null, string email = "customer@sportshop.dev") =>
        GenerateJwtToken(userId, email, "Customer");

    public static string GenerateAdminToken(Guid? userId = null, string email = "admin@sportshop.dev") =>
        GenerateJwtToken(userId, email, "Admin");

    public static string GenerateExpiredToken(string role = "Customer") =>
        GenerateJwtToken(role: role, expiry: TimeSpan.FromMinutes(-10));

    public static HttpClient WithBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
