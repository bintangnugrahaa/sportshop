using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Shipping.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Shipping.ListRates;

public sealed class ListShippingRatesQueryHandler
{
    private readonly IShippingRateRepository _repository;

    public ListShippingRatesQueryHandler(IShippingRateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<ShippingRateDto>>> HandleAsync(
        ListShippingRatesQuery query,
        CancellationToken cancellationToken = default)
    {
        var rates = await _repository.GetAllAsync(query.IncludeInactive, cancellationToken);
        return Result.Success(rates.ToDtoList());
    }
}
