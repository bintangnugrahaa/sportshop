namespace SportShop.Application.Features.Orders.PayOrder;

public sealed record PayOrderResponseDto(
    string OrderNumber,
    string? SnapToken,
    string? SnapRedirectUrl,
    string PaymentStatus);
