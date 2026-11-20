using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Orders.DTOs;
using SportShop.Application.Mapping;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;
using SportShop.Domain.Payments;
using SportShop.Domain.Shipping;

namespace SportShop.Application.Features.Orders.Checkout;

public sealed class CheckoutOrderCommandHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly ICartRepository _cartRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IShippingRateRepository _shippingRateRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IOrderNotificationService _notificationService;
    private readonly IOrderNumberGenerator _orderNumberGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ISportShopMetrics _metrics;
    private readonly IValidator<CheckoutOrderCommand> _validator;

    public CheckoutOrderCommandHandler(
        ICurrentUser currentUser,
        ICartRepository cartRepository,
        IAddressRepository addressRepository,
        IShippingRateRepository shippingRateRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        IOrderNotificationService notificationService,
        IOrderNumberGenerator orderNumberGenerator,
        IUnitOfWork unitOfWork,
        IClock clock,
        ISportShopMetrics metrics,
        IValidator<CheckoutOrderCommand> validator)
    {
        _currentUser = currentUser;
        _cartRepository = cartRepository;
        _addressRepository = addressRepository;
        _shippingRateRepository = shippingRateRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
        _notificationService = notificationService;
        _orderNumberGenerator = orderNumberGenerator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _metrics = metrics;
        _validator = validator;
    }

    public async Task<Result<OrderDto>> HandleAsync(CheckoutOrderCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<OrderDto>(Error.Validation("order.validation_error", "Data checkout tidak valid.", errors));
        }

        var userIdNullable = _currentUser.UserId;
        if (!userIdNullable.HasValue || userIdNullable.Value == Guid.Empty)
        {
            return Result.Failure<OrderDto>(Error.Unauthorized("auth.unauthorized", "Pengguna belum terautentikasi."));
        }
        var userId = userIdNullable.Value;

        var idempotencyKey = string.IsNullOrWhiteSpace(command.IdempotencyKey) ? null : command.IdempotencyKey.Trim();

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(userId, idempotencyKey, cancellationToken);
            if (existingOrder is not null)
            {
                var existingPayment = await _paymentRepository.GetActivePaymentByOrderIdAsync(existingOrder.Id, cancellationToken);
                return Result.Success(existingOrder.ToDto(existingPayment));
            }
        }

        var address = await _addressRepository.GetByIdAsync(command.AddressId, cancellationToken);
        if (address is null || address.UserId != userId || address.IsDeleted)
        {
            return Result.Failure<OrderDto>(Error.NotFound("address.not_found", "Alamat pengiriman tidak ditemukan."));
        }

        var cart = await _cartRepository.GetByUserIdAsync(userId, includeDetails: true, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            return Result.Failure<OrderDto>(Error.Validation("cart.empty", "Keranjang belanja kosong."));
        }

        var rate = await _shippingRateRepository.GetByProvinceCodeAsync(address.ProvinceCode, cancellationToken);
        if (rate is null || !rate.IsActive)
        {
            return Result.Failure<OrderDto>(Error.BusinessRule("shipping.rate_not_found", "Tarif pengiriman untuk provinsi ini tidak tersedia."));
        }

        // Validate variants and products active status
        foreach (var item in cart.Items)
        {
            if (item.Variant is null || !item.Variant.IsActive || item.Variant.Product is null || !item.Variant.Product.IsActive)
            {
                var sku = item.Variant?.Sku ?? "item";
                return Result.Failure<OrderDto>(Error.BusinessRule("cart.variant_inactive", $"Varian {sku} tidak aktif atau tidak tersedia."));
            }
        }

        var totalWeightGram = cart.Items.Sum(i => i.Variant.WeightGram * i.Quantity);
        var shippingResult = ShippingCalculator.Calculate(rate, totalWeightGram);
        var shippingFee = shippingResult.TotalFee;
        var now = _clock.UtcNow;

        Order? createdOrder = null;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var orderNumber = _orderNumberGenerator.Generate(now);
                var expiresAt = now.AddHours(24);

                var addressSnapshot = new OrderShippingAddress(
                    address.RecipientName,
                    address.Phone,
                    address.ProvinceCode,
                    rate.ProvinceName,
                    address.City,
                    address.PostalCode,
                    address.Street);

                var order = new Order(
                    Guid.NewGuid(),
                    orderNumber,
                    userId,
                    addressSnapshot,
                    totalWeightGram,
                    shippingFee,
                    expiresAt,
                    idempotencyKey,
                    now);

                foreach (var item in cart.Items)
                {
                    var reserved = await _productRepository.ReserveStockAtomicAsync(item.VariantId, item.Quantity, now, ct);
                    if (!reserved)
                    {
                        throw new StockReservationException(item.Variant.Sku);
                    }

                    var variant = item.Variant;
                    var product = variant.Product;
                    var label = $"{variant.OptionSize} / {variant.OptionColor}".Trim(' ', '/');
                    if (string.IsNullOrEmpty(label))
                    {
                        label = variant.Sku;
                    }

                    var primaryImage = product.Images?.FirstOrDefault(img => img.IsPrimary)?.Url
                        ?? product.Images?.OrderBy(img => img.SortOrder).FirstOrDefault()?.Url;

                    var orderItem = new OrderItem(
                        Guid.NewGuid(),
                        order.Id,
                        item.VariantId,
                        product.Name,
                        label,
                        variant.Sku,
                        primaryImage,
                        variant.Price,
                        item.Quantity);

                    order.AddItem(orderItem);

                    var movement = new InventoryMovement(
                        Guid.NewGuid(),
                        item.VariantId,
                        -item.Quantity,
                        StockReason.Sale,
                        $"Penjualan pesanan {orderNumber}",
                        order.Id,
                        userId,
                        now);

                    await _productRepository.AddInventoryMovementAsync(movement, ct);
                }

                await _orderRepository.AddAsync(order, ct);

                cart.Clear(now);
                await _cartRepository.UpdateAsync(cart, ct);

                createdOrder = order;
            }, cancellationToken);
        }
        catch (StockReservationException ex)
        {
            _metrics.StockConflict();
            return Result.Failure<OrderDto>(Error.Conflict("stock.insufficient", $"Stok varian {ex.Sku} tidak mencukupi."));
        }
        catch (Exception) when (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(userId, idempotencyKey, cancellationToken);
            if (existingOrder is not null)
            {
                var existingPayment = await _paymentRepository.GetActivePaymentByOrderIdAsync(existingOrder.Id, cancellationToken);
                return Result.Success(existingOrder.ToDto(existingPayment));
            }
            throw;
        }

        CreateSnapTransactionResult? snapResult = null;
        Payment? payment = null;
        var customerEmail = _currentUser.Email ?? "customer@sportshop.dev";

        try
        {
            var snapItems = createdOrder!.Items.Select(i => new SnapItemDto(
                i.VariantId.ToString(),
                i.ProductName,
                i.UnitPrice,
                i.Quantity
            )).ToList();
            snapItems.Add(new SnapItemDto("shipping", "Biaya Pengiriman", createdOrder.ShippingFee, 1));

            var snapRequest = new CreateSnapTransactionRequest(
                OrderNumber: createdOrder.OrderNumber,
                GrossAmount: createdOrder.Total,
                CustomerName: createdOrder.ShippingAddress.RecipientName,
                CustomerEmail: customerEmail,
                CustomerPhone: createdOrder.ShippingAddress.Phone,
                Items: snapItems);

            snapResult = await _paymentGateway.CreateSnapTransactionAsync(snapRequest, cancellationToken);

            if (snapResult.Success && !string.IsNullOrWhiteSpace(snapResult.Token))
            {
                payment = new Payment(
                    Guid.NewGuid(),
                    createdOrder.Id,
                    createdOrder.Total,
                    snapResult.Token,
                    snapResult.RedirectUrl,
                    "Midtrans",
                    now);

                await _paymentRepository.AddAsync(payment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            // Token tak tersedia != pesanan hilang. Pesanan tetap PendingPayment.
        }

        var paymentStatus = snapResult is not null && snapResult.Success
            ? "pending"
            : "token_unavailable";

        _metrics.OrderCreated();

        _notificationService.EnqueueOrderCreatedEmail(
            customerEmail,
            createdOrder!.ShippingAddress.RecipientName,
            createdOrder.OrderNumber,
            createdOrder.Total,
            createdOrder.ExpiresAt);

        return Result.Success(createdOrder!.ToDto(
            payment: payment,
            snapToken: snapResult?.Token,
            snapRedirectUrl: snapResult?.RedirectUrl,
            paymentStatus: paymentStatus));
    }

    private sealed class StockReservationException : Exception
    {
        public string Sku { get; }

        public StockReservationException(string sku)
            : base($"Stok varian {sku} tidak mencukupi.")
        {
            Sku = sku;
        }
    }
}
