using Serilog;
using SportShop.Api.Middleware;
using SportShop.Application;
using SportShop.Infrastructure;
using SportShop.Infrastructure.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
{
    configuration.ConfigureSportShopLogging(context.Configuration);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSportShopRequestLogging();

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { Name = "SportShop API", Status = "Healthy" }));

app.Run();

public partial class Program;
