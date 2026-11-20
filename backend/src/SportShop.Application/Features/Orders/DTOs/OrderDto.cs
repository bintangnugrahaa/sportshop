namespace SportShop.Application.Features.Orders.DTOs;

public sealed record OrderShippingAddressDto(
    string RecipientName,
    string Phone,
    string ProvinceCode,
    string ProvinceName,
    string City,
    string PostalCode,
    string Street);

public sealed record OrderItemDto(
    Guid Id,
    Guid VariantId,
    string ProductName,
    string VariantLabel,
    string Sku,
    string? ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record OrderStatusHistoryDto(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    string? Note,
    DateTime CreatedAt);

public sealed record PaymentDto(
    Guid Id,
    string Status,
    decimal GrossAmount,
    string? SnapToken,
    string? SnapRedirectUrl,
    string? PaymentType,
    DateTime? SettledAt,
    DateTime CreatedAt);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid UserId,
    string Status,
    decimal Subtotal,
    decimal ShippingFee,
    decimal Total,
    OrderShippingAddressDto ShippingAddress,
    int TotalWeightGram,
    string? Courier,
    string? TrackingNumber,
    DateTime ExpiresAt,
    DateTime? PaidAt,
    DateTime? ShippedAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    string? CancelReason,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderStatusHistoryDto> StatusHistories,
    PaymentDto? Payment = null,
    string? SnapToken = null,
    string? SnapRedirectUrl = null,
    string? PaymentStatus = null);
