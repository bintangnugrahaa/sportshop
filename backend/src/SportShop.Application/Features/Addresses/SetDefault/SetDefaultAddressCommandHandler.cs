using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Addresses.SetDefault;

public sealed class SetDefaultAddressCommandHandler
{
    private readonly IAddressRepository _addressRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SetDefaultAddressCommandHandler(
        IAddressRepository addressRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _addressRepository = addressRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<AddressDto>> HandleAsync(
        SetDefaultAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        var address = await _addressRepository.GetByIdAsync(command.AddressId, cancellationToken);
        if (address is null || address.UserId != command.UserId || address.IsDeleted)
        {
            return Result.Failure<AddressDto>(Error.NotFound("address.not_found", "Alamat tidak ditemukan."));
        }

        if (!address.IsDefault)
        {
            await _addressRepository.UnsetDefaultAddressesAsync(command.UserId, address.Id, cancellationToken);
            address.SetDefault(true, _clock.UtcNow);
            await _addressRepository.UpdateAsync(address, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(address.ToDto());
    }
}
