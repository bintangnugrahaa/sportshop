using Serilog;
using SportShop.Api.Extensions;
using SportShop.Api.Middleware;
using SportShop.Application;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure;
using SportShop.Infrastructure.Identity;
using SportShop.Infrastructure.Jobs;
using SportShop.Infrastructure.Observability;
using SportShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
{
    configuration.ConfigureSportShopLogging(context.Configuration);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSportShopCors(builder.Configuration);
builder.Services.AddSportShopRateLimiting();
builder.Services.AddSportShopAuthentication(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSportShopRequestLogging();

app.UseCors(CorsExtensions.PolicyName);
app.UseStaticFiles();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwaggerDocumentation();

app.UseHttpsRedirection();
app.MapSportShopHealthChecks();
app.UseSportShopHangfireDashboard();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { Name = "SportShop API", Status = "Healthy" }));

// Rate-limited endpoint mappings for verification and auth/checkout policies
var authGroup = app.MapGroup("/api/v1/auth").RequireRateLimiting(RateLimitingExtensions.AuthPolicy);
authGroup.MapPost("/test-login", () => Results.Ok(new { Message = "Login ok" }));
authGroup.MapGet("/test-protected", () => Results.Ok(new { Message = "Authenticated ok" })).RequireAuthorization();

var adminGroup = app.MapGroup("/api/v1/admin").RequireAuthorization(AuthExtensions.AdminPolicy);
adminGroup.MapGet("/test-admin", () => Results.Ok(new { Message = "Admin ok" }));

var checkoutGroup = app.MapGroup("/api/v1/checkout").RequireRateLimiting(RateLimitingExtensions.CheckoutPolicy);
checkoutGroup.MapPost("/test-checkout", () => Results.Ok(new { Message = "Checkout ok" }));

if (builder.Configuration.GetValue<bool>("AutoMigrate", false))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
        var seeder = scope.ServiceProvider.GetRequiredService<IIdentitySeeder>();
        await seeder.SeedAsync();

        var demoSeeder = scope.ServiceProvider.GetRequiredService<ICatalogDemoSeeder>();
        await demoSeeder.SeedAsync();
    }
}

app.Run();

public partial class Program;
