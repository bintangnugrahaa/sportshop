namespace SportShop.Application.Common.Interfaces;

public interface IExpireUnpaidOrdersJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
