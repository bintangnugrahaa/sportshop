using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Addresses.ListAddresses;

public sealed class ListAddressesQueryHandler
{
    private readonly IAddressRepository _repository;

    public ListAddressesQueryHandler(IAddressRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<AddressDto>>> HandleAsync(
        ListAddressesQuery query,
        CancellationToken cancellationToken = default)
    {
        var addresses = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        return Result.Success(addresses.ToDtoList());
    }
}
