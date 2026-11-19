using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Shipping.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Shipping.GetRate;

public sealed class GetShippingRateQueryHandler
{
    private readonly IShippingRateRepository _repository;

    public GetShippingRateQueryHandler(IShippingRateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ShippingRateDto>> HandleAsync(
        GetShippingRateQuery query,
        CancellationToken cancellationToken = default)
    {
        var rate = await _repository.GetByProvinceCodeAsync(query.ProvinceCode, cancellationToken);
        if (rate is null)
        {
            return Result.Failure<ShippingRateDto>(Error.NotFound("shipping.rate_not_found", $"Tarif untuk provinsi {query.ProvinceCode} tidak ditemukan."));
        }

        return Result.Success(rate.ToDto());
    }
}
