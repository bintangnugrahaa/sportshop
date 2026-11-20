using SportShop.Application.Features.Orders.DTOs;
using SportShop.Domain.Orders;
using SportShop.Domain.Payments;

namespace SportShop.Application.Mapping;

public static class OrderMappingExtensions
{
    public static OrderShippingAddressDto ToDto(this OrderShippingAddress address) =>
        new(
            address.RecipientName,
            address.Phone,
            address.ProvinceCode,
            address.ProvinceName,
            address.City,
            address.PostalCode,
            address.Street);

    public static OrderItemDto ToDto(this OrderItem item) =>
        new(
            item.Id,
            item.VariantId,
            item.ProductName,
            item.VariantLabel,
            item.Sku,
            item.ImageUrl,
            item.UnitPrice,
            item.Quantity,
            item.LineTotal);

    public static OrderStatusHistoryDto ToDto(this OrderStatusHistory history) =>
        new(
            history.Id,
            history.FromStatus?.ToString(),
            history.ToStatus.ToString(),
            history.Note,
            history.CreatedAt);

    public static PaymentDto ToDto(this Payment payment) =>
        new(
            payment.Id,
            payment.Status.ToString(),
            payment.GrossAmount,
            payment.SnapToken,
            payment.SnapRedirectUrl,
            payment.PaymentType,
            payment.SettledAt,
            payment.CreatedAt);

    public static OrderDto ToDto(
        this Order order,
        Payment? payment = null,
        string? snapToken = null,
        string? snapRedirectUrl = null,
        string? paymentStatus = null) =>
        new(
            order.Id,
            order.OrderNumber,
            order.UserId,
            order.Status.ToString(),
            order.Subtotal,
            order.ShippingFee,
            order.Total,
            order.ShippingAddress.ToDto(),
            order.TotalWeightGram,
            order.Courier,
            order.TrackingNumber,
            order.ExpiresAt,
            order.PaidAt,
            order.ShippedAt,
            order.CompletedAt,
            order.CancelledAt,
            order.CancelReason,
            order.CreatedAt,
            order.Items.Select(ToDto).ToList(),
            order.StatusHistories.Select(ToDto).ToList(),
            payment?.ToDto(),
            snapToken ?? payment?.SnapToken,
            snapRedirectUrl ?? payment?.SnapRedirectUrl,
            paymentStatus ?? (payment != null ? payment.Status.ToString().ToLowerInvariant() : null));

    public static List<OrderDto> ToDtoList(this IEnumerable<Order> orders) =>
        orders.Select(o => o.ToDto()).ToList();
}
