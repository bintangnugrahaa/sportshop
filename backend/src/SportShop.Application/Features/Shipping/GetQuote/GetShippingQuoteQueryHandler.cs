using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Shipping.DTOs;
using SportShop.Domain.Shipping;

namespace SportShop.Application.Features.Shipping.GetQuote;

public sealed class GetShippingQuoteQueryHandler
{
    private readonly IAddressRepository _addressRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IShippingRateRepository _shippingRateRepository;
    private readonly IValidator<GetShippingQuoteQuery> _validator;

    public GetShippingQuoteQueryHandler(
        IAddressRepository addressRepository,
        ICartRepository cartRepository,
        IShippingRateRepository shippingRateRepository,
        IValidator<GetShippingQuoteQuery> validator)
    {
        _addressRepository = addressRepository;
        _cartRepository = cartRepository;
        _shippingRateRepository = shippingRateRepository;
        _validator = validator;
    }

    public async Task<Result<ShippingQuoteDto>> HandleAsync(
        GetShippingQuoteQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<ShippingQuoteDto>(Error.Validation("shipping.validation_error", "Data permintaan ongkos kirim tidak valid.", errors));
        }

        var address = await _addressRepository.GetByIdAsync(query.AddressId, cancellationToken);
        if (address is null || address.UserId != query.UserId || address.IsDeleted)
        {
            return Result.Failure<ShippingQuoteDto>(Error.NotFound("address.not_found", "Alamat tidak ditemukan."));
        }

        var cart = await _cartRepository.GetByUserIdAsync(query.UserId, includeDetails: true, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            return Result.Failure<ShippingQuoteDto>(Error.BusinessRule("cart.empty", "Keranjang belanja kosong."));
        }

        var availableItems = cart.Items
            .Where(i => i.Variant != null && i.Variant.IsActive && (i.Variant.Product == null || i.Variant.Product.IsActive))
            .ToList();

        if (availableItems.Count == 0)
        {
            return Result.Failure<ShippingQuoteDto>(Error.BusinessRule("cart.no_available_items", "Tidak ada item yang dapat dikirim di keranjang."));
        }

        var totalWeightGram = availableItems.Sum(i => i.Quantity * i.Variant!.WeightGram);
        var subtotal = availableItems.Sum(i => i.Quantity * i.Variant!.Price);

        var rate = await _shippingRateRepository.GetByProvinceCodeAsync(address.ProvinceCode, cancellationToken);
        if (rate is null || !rate.IsActive)
        {
            return Result.Failure<ShippingQuoteDto>(Error.BusinessRule("shipping.rate_not_found", $"Tarif pengiriman untuk provinsi {address.ProvinceCode} tidak ditemukan atau sedang tidak aktif."));
        }

        var calculation = ShippingCalculator.Calculate(rate, totalWeightGram);

        return Result.Success(new ShippingQuoteDto(
            address.Id,
            address.RecipientName,
            rate.ProvinceCode,
            rate.ProvinceName,
            address.City,
            address.PostalCode,
            calculation.TotalWeightGram,
            calculation.BillableWeightKg,
            calculation.BaseFee,
            calculation.PerKgFee,
            calculation.TotalFee,
            calculation.EstimatedDaysMin,
            calculation.EstimatedDaysMax,
            subtotal,
            subtotal + calculation.TotalFee));
    }
}
