namespace SportShop.Application.Common.Interfaces;

public record SnapItemDto(string Id, string Name, decimal Price, int Quantity);

public record CreateSnapTransactionRequest(
    string OrderNumber,
    decimal GrossAmount,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    IReadOnlyList<SnapItemDto> Items);

public record CreateSnapTransactionResult(
    bool Success,
    string? Token,
    string? RedirectUrl,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public record PaymentStatusResult(
    string StatusCode,
    string StatusMessage,
    string TransactionId,
    string OrderNumber,
    decimal GrossAmount,
    string? PaymentType,
    DateTime? TransactionTime,
    string TransactionStatus,
    string? FraudStatus,
    DateTime? SettlementTime,
    string? SignatureKey,
    string? RawResponse);

public interface IPaymentGateway
{
    bool VerifySignature(
        string orderId,
        string statusCode,
        string grossAmount,
        string signatureKey);

    Task<CreateSnapTransactionResult> CreateSnapTransactionAsync(
        CreateSnapTransactionRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentStatusResult?> GetPaymentStatusAsync(
        string orderNumber,
        CancellationToken cancellationToken = default);

    Task<bool> CancelTransactionAsync(
        string orderNumber,
        CancellationToken cancellationToken = default);
}
