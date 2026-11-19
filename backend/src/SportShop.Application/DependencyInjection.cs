using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Refresh;
using SportShop.Application.Features.Auth.Register;

namespace SportShop.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<RegisterCommandHandler>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();

        return services;
    }
}
