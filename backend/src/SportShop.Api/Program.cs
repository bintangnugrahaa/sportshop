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

builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSportShopRequestLogging();

app.UseSwaggerDocumentation();

app.UseHttpsRedirection();
app.MapSportShopHealthChecks();
app.UseSportShopHangfireDashboard();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { Name = "SportShop API", Status = "Healthy" }));

app.Run();

public partial class Program;
