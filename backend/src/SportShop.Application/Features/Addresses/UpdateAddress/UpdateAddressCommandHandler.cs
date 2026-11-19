using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Addresses.UpdateAddress;

public sealed class UpdateAddressCommandHandler
{
    private readonly IAddressRepository _addressRepository;
    private readonly IShippingRateRepository _shippingRateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<UpdateAddressCommand> _validator;

    public UpdateAddressCommandHandler(
        IAddressRepository addressRepository,
        IShippingRateRepository shippingRateRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<UpdateAddressCommand> validator)
    {
        _addressRepository = addressRepository;
        _shippingRateRepository = shippingRateRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<AddressDto>> HandleAsync(
        UpdateAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<AddressDto>(Error.Validation("address.validation_error", "Data perubahan alamat tidak valid.", errors));
        }

        var address = await _addressRepository.GetByIdAsync(command.AddressId, cancellationToken);
        if (address is null || address.UserId != command.UserId || address.IsDeleted)
        {
            return Result.Failure<AddressDto>(Error.NotFound("address.not_found", "Alamat tidak ditemukan."));
        }

        var province = await _shippingRateRepository.GetByProvinceCodeAsync(command.ProvinceCode, cancellationToken);
        if (province is null)
        {
            return Result.Failure<AddressDto>(Error.NotFound("shipping.rate_not_found", "Provinsi tidak valid atau tarif tidak ditemukan."));
        }

        if (command.IsDefault && !address.IsDefault)
        {
            await _addressRepository.UnsetDefaultAddressesAsync(command.UserId, address.Id, cancellationToken);
        }

        address.UpdateDetails(
            command.Label,
            command.RecipientName,
            command.Phone,
            province.ProvinceCode,
            command.City,
            command.District,
            command.PostalCode,
            command.Street,
            command.IsDefault,
            _clock.UtcNow);

        address.Province = province;

        await _addressRepository.UpdateAsync(address, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(address.ToDto());
    }
}
