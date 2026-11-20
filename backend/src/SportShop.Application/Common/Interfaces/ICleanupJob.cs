namespace SportShop.Application.Common.Interfaces;

public interface ICleanupJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
