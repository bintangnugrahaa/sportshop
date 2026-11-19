using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Addresses.GetAddress;

public sealed class GetAddressByIdQueryHandler
{
    private readonly IAddressRepository _repository;

    public GetAddressByIdQueryHandler(IAddressRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<AddressDto>> HandleAsync(
        GetAddressByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var address = await _repository.GetByIdAsync(query.AddressId, cancellationToken);
        if (address is null || address.UserId != query.UserId || address.IsDeleted)
        {
            return Result.Failure<AddressDto>(Error.NotFound("address.not_found", "Alamat tidak ditemukan."));
        }

        return Result.Success(address.ToDto());
    }
}
