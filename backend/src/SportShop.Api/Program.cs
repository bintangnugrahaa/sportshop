using Serilog;
using SportShop.Api.Extensions;
using SportShop.Api.Middleware;
using SportShop.Application;
using SportShop.Infrastructure;
using SportShop.Infrastructure.Jobs;
using SportShop.Infrastructure.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
{
    configuration.ConfigureSportShopLogging(context.Configuration);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSportShopCors(builder.Configuration);
builder.Services.AddSportShopRateLimiting();

builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSportShopRequestLogging();

app.UseCors(CorsExtensions.PolicyName);
app.UseRateLimiter();

app.UseSwaggerDocumentation();

app.UseHttpsRedirection();
app.MapSportShopHealthChecks();
app.UseSportShopHangfireDashboard();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { Name = "SportShop API", Status = "Healthy" }));

// Rate-limited endpoint mappings for verification and auth/checkout policies
var authGroup = app.MapGroup("/api/v1/auth").RequireRateLimiting(RateLimitingExtensions.AuthPolicy);
authGroup.MapPost("/test-login", () => Results.Ok(new { Message = "Login ok" }));

var checkoutGroup = app.MapGroup("/api/v1/checkout").RequireRateLimiting(RateLimitingExtensions.CheckoutPolicy);
checkoutGroup.MapPost("/test-checkout", () => Results.Ok(new { Message = "Checkout ok" }));

app.Run();

public partial class Program;
