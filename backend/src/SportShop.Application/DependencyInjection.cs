using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Logout;
using SportShop.Application.Features.Auth.Refresh;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Users.ChangePassword;
using SportShop.Application.Features.Users.GetProfile;
using SportShop.Application.Features.Users.UpdateProfile;

namespace SportShop.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<RegisterCommandHandler>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<GetProfileQueryHandler>();
        services.AddScoped<UpdateProfileCommandHandler>();
        services.AddScoped<ChangePasswordCommandHandler>();

        return services;
    }
}
