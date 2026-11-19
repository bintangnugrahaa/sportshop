namespace SportShop.Application.Common.Interfaces;

public interface ICatalogDemoSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
