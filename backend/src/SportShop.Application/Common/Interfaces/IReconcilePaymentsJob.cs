namespace SportShop.Application.Common.Interfaces;

public interface IReconcilePaymentsJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
