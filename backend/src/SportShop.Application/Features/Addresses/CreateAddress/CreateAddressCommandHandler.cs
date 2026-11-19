using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Application.Mapping;
using SportShop.Domain.Users;

namespace SportShop.Application.Features.Addresses.CreateAddress;

public sealed class CreateAddressCommandHandler
{
    private readonly IAddressRepository _addressRepository;
    private readonly IShippingRateRepository _shippingRateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<CreateAddressCommand> _validator;

    public CreateAddressCommandHandler(
        IAddressRepository addressRepository,
        IShippingRateRepository shippingRateRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<CreateAddressCommand> validator)
    {
        _addressRepository = addressRepository;
        _shippingRateRepository = shippingRateRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<AddressDto>> HandleAsync(
        CreateAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<AddressDto>(Error.Validation("address.validation_error", "Data alamat tidak valid.", errors));
        }

        var province = await _shippingRateRepository.GetByProvinceCodeAsync(command.ProvinceCode, cancellationToken);
        if (province is null)
        {
            return Result.Failure<AddressDto>(Error.NotFound("shipping.rate_not_found", "Provinsi tidak valid atau tarif tidak ditemukan."));
        }

        var existingAddresses = await _addressRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var shouldBeDefault = command.IsDefault || existingAddresses.Count == 0;

        if (shouldBeDefault && existingAddresses.Count > 0)
        {
            await _addressRepository.UnsetDefaultAddressesAsync(command.UserId, null, cancellationToken);
        }

        var address = new Address(
            Guid.NewGuid(),
            command.UserId,
            command.Label,
            command.RecipientName,
            command.Phone,
            province.ProvinceCode,
            command.City,
            command.District,
            command.PostalCode,
            command.Street,
            shouldBeDefault,
            _clock.UtcNow);

        address.Province = province;

        await _addressRepository.AddAsync(address, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(address.ToDto());
    }
}
