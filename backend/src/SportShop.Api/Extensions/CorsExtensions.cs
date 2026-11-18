using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Infrastructure.Options;

namespace SportShop.Api.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "SportShopCors";

    public static IServiceCollection AddSportShopCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()?.GetAllowedOrigins()
                      ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, builder =>
            {
                if (origins.Length > 0)
                {
                    builder.WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else
                {
                    builder.SetIsOriginAllowed(_ => false);
                }
            });
        });

        return services;
    }
}
