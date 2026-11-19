using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Addresses.DeleteAddress;

public sealed class DeleteAddressCommandHandler
{
    private readonly IAddressRepository _addressRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeleteAddressCommandHandler(
        IAddressRepository addressRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _addressRepository = addressRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(
        DeleteAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        var address = await _addressRepository.GetByIdAsync(command.AddressId, cancellationToken);
        if (address is null || address.UserId != command.UserId || address.IsDeleted)
        {
            return Result.Failure(Error.NotFound("address.not_found", "Alamat tidak ditemukan."));
        }

        address.MarkDeleted(_clock.UtcNow);

        await _addressRepository.UpdateAsync(address, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
