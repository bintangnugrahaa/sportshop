using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SportShop.Infrastructure.Identity;
using SportShop.Infrastructure.Options;

namespace SportShop.Api.Extensions;

public static class AuthExtensions
{
    public const string AdminPolicy = "Admin";
    public const string CustomerPolicy = "Customer";

    public static IServiceCollection AddSportShopAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtKey = jwtSection[nameof(JwtOptions.Key)] ?? string.Empty;
        var jwtIssuer = jwtSection[nameof(JwtOptions.Issuer)] ?? string.Empty;
        var jwtAudience = jwtSection[nameof(JwtOptions.Audience)] ?? string.Empty;

        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ClockSkew = TimeSpan.FromSeconds(30),
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicy, policy => policy.RequireRole(ApplicationRole.Admin));
            options.AddPolicy(CustomerPolicy, policy => policy.RequireRole(ApplicationRole.Customer));
        });

        return services;
    }
}
