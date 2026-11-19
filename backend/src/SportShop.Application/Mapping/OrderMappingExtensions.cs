using SportShop.Application.Features.Orders.DTOs;
using SportShop.Domain.Orders;

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

    public static OrderDto ToDto(this Order order) =>
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
            order.StatusHistories.Select(ToDto).ToList());

    public static List<OrderDto> ToDtoList(this IEnumerable<Order> orders) =>
        orders.Select(ToDto).ToList();
}
