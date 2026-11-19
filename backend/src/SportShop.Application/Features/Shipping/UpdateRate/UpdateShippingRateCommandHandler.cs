using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Shipping.DTOs;
using SportShop.Application.Features.Shipping.GetActiveRates;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Shipping.UpdateRate;

public sealed class UpdateShippingRateCommandHandler
{
    private readonly IShippingRateRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<UpdateShippingRateCommand> _validator;

    public UpdateShippingRateCommandHandler(
        IShippingRateRepository repository,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<UpdateShippingRateCommand> validator)
    {
        _repository = repository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<ShippingRateDto>> HandleAsync(
        UpdateShippingRateCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<ShippingRateDto>(Error.Validation("shipping.validation_error", "Data tarif pengiriman tidak valid.", errors));
        }

        var rate = await _repository.GetByProvinceCodeAsync(command.ProvinceCode, cancellationToken);
        if (rate is null)
        {
            return Result.Failure<ShippingRateDto>(Error.NotFound("shipping.rate_not_found", $"Tarif untuk provinsi {command.ProvinceCode} tidak ditemukan."));
        }

        rate.UpdateRate(
            command.BaseFee,
            command.PerKgFee,
            command.EstimatedDaysMin,
            command.EstimatedDaysMax,
            command.IsActive,
            _clock.UtcNow);

        await _repository.UpdateAsync(rate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache immediately so changes are reflected at once
        await _cacheService.RemoveAsync(GetActiveShippingRatesQueryHandler.CacheKey, cancellationToken);

        return Result.Success(rate.ToDto());
    }
}
