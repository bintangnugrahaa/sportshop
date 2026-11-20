using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Options;

namespace SportShop.Infrastructure.Services;

public sealed partial class MidtransClient : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly MidtransOptions _options;
    private readonly ILogger<MidtransClient> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MidtransClient(
        HttpClient httpClient,
        IOptions<MidtransOptions> options,
        ILogger<MidtransClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    private string SnapUrl => _options.IsProduction
        ? "https://app.midtrans.com/snap/v1/transactions"
        : "https://app.sandbox.midtrans.com/snap/v1/transactions";

    private string ApiBaseUrl => _options.IsProduction
        ? "https://api.midtrans.com"
        : "https://api.sandbox.midtrans.com";

    private void SetAuthorizationHeader(HttpRequestMessage request)
    {
        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ServerKey}:"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public bool VerifySignature(string orderId, string statusCode, string grossAmount, string signatureKey)
    {
        return SportShop.Application.Features.Payments.Webhook.MidtransSignatureVerifier.Verify(
            orderId, statusCode, grossAmount, _options.ServerKey, signatureKey);
    }

    public async Task<CreateSnapTransactionResult> CreateSnapTransactionAsync(
        CreateSnapTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var grossAmountRounded = (long)Math.Round(request.GrossAmount, MidpointRounding.AwayFromZero);

            var items = request.Items.Select(i => new SnapItemRequest(
                Id: i.Id,
                Price: (long)Math.Round(i.Price, MidpointRounding.AwayFromZero),
                Quantity: i.Quantity,
                Name: i.Name.Length > 50 ? i.Name[..50] : i.Name
            )).ToList();

            var payload = new SnapCreateRequest(
                TransactionDetails: new SnapTransactionDetails(request.OrderNumber, grossAmountRounded),
                CustomerDetails: new SnapCustomerDetails(request.CustomerName, request.CustomerEmail, request.CustomerPhone),
                ItemDetails: items
            );

            var jsonContent = JsonSerializer.Serialize(payload, _jsonOptions);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, SnapUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            SetAuthorizationHeader(httpRequest);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var snapResponse = JsonSerializer.Deserialize<SnapCreateResponse>(responseContent, _jsonOptions);
                if (snapResponse is not null && !string.IsNullOrWhiteSpace(snapResponse.Token))
                {
                    return new CreateSnapTransactionResult(
                        Success: true,
                        Token: snapResponse.Token,
                        RedirectUrl: snapResponse.RedirectUrl);
                }
            }

            LogSnapCreationFailed(_logger, request.OrderNumber, (int)response.StatusCode, responseContent);

            var errorCode = response.StatusCode == HttpStatusCode.Conflict || response.StatusCode == (HttpStatusCode)406
                ? "midtrans.duplicate"
                : "midtrans.create_failed";

            return new CreateSnapTransactionResult(
                Success: false,
                Token: null,
                RedirectUrl: null,
                ErrorCode: errorCode,
                ErrorMessage: responseContent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogSnapCreationException(_logger, request.OrderNumber, ex.Message, ex);
            return new CreateSnapTransactionResult(
                Success: false,
                Token: null,
                RedirectUrl: null,
                ErrorCode: "midtrans.exception",
                ErrorMessage: ex.Message);
        }
    }

    public async Task<PaymentStatusResult?> GetPaymentStatusAsync(
        string orderNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{ApiBaseUrl}/v2/{orderNumber}/status";
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            SetAuthorizationHeader(httpRequest);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                LogStatusNotFound(_logger, orderNumber);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogStatusQueryFailed(_logger, orderNumber, (int)response.StatusCode, responseContent);
                return null;
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            var statusCode = root.TryGetProperty("status_code", out var sc) ? sc.GetString() ?? "" : "";
            var statusMessage = root.TryGetProperty("status_message", out var sm) ? sm.GetString() ?? "" : "";
            var transactionId = root.TryGetProperty("transaction_id", out var ti) ? ti.GetString() ?? "" : "";
            var orderId = root.TryGetProperty("order_id", out var oi) ? oi.GetString() ?? orderNumber : orderNumber;

            decimal grossAmount = 0;
            if (root.TryGetProperty("gross_amount", out var ga))
            {
                if (ga.ValueKind == JsonValueKind.String)
                {
                    decimal.TryParse(ga.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out grossAmount);
                }
                else if (ga.ValueKind == JsonValueKind.Number)
                {
                    grossAmount = ga.GetDecimal();
                }
            }

            var paymentType = root.TryGetProperty("payment_type", out var pt) ? pt.GetString() : null;
            var transactionStatus = root.TryGetProperty("transaction_status", out var ts) ? ts.GetString() ?? "" : "";
            var fraudStatus = root.TryGetProperty("fraud_status", out var fs) ? fs.GetString() : null;
            var signatureKey = root.TryGetProperty("signature_key", out var sk) ? sk.GetString() : null;

            DateTime? transactionTime = null;
            if (root.TryGetProperty("transaction_time", out var tt) && DateTime.TryParse(tt.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsedTt))
            {
                transactionTime = DateTime.SpecifyKind(parsedTt, DateTimeKind.Utc);
            }

            DateTime? settlementTime = null;
            if (root.TryGetProperty("settlement_time", out var st) && DateTime.TryParse(st.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsedSt))
            {
                settlementTime = DateTime.SpecifyKind(parsedSt, DateTimeKind.Utc);
            }

            return new PaymentStatusResult(
                StatusCode: statusCode,
                StatusMessage: statusMessage,
                TransactionId: transactionId,
                OrderNumber: orderId,
                GrossAmount: grossAmount,
                PaymentType: paymentType,
                TransactionTime: transactionTime,
                TransactionStatus: transactionStatus,
                FraudStatus: fraudStatus,
                SettlementTime: settlementTime,
                SignatureKey: signatureKey,
                RawResponse: responseContent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogStatusException(_logger, orderNumber, ex.Message, ex);
            return null;
        }
    }

    public async Task<bool> CancelTransactionAsync(
        string orderNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{ApiBaseUrl}/v2/{orderNumber}/cancel";
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            SetAuthorizationHeader(httpRequest);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                LogCancelSuccess(_logger, orderNumber);
                return true;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            LogCancelFailed(_logger, orderNumber, (int)response.StatusCode, content);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogCancelException(_logger, orderNumber, ex.Message, ex);
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Midtrans Snap transaction creation failed for order {OrderNumber} with status {StatusCode}: {Response}")]
    private static partial void LogSnapCreationFailed(ILogger logger, string orderNumber, int statusCode, string response);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected exception creating Midtrans Snap transaction for order {OrderNumber}: {Reason}")]
    private static partial void LogSnapCreationException(ILogger logger, string orderNumber, string reason, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderNumber} status not found in Midtrans")]
    private static partial void LogStatusNotFound(ILogger logger, string orderNumber);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to query Midtrans status for order {OrderNumber}: {StatusCode} - {Content}")]
    private static partial void LogStatusQueryFailed(ILogger logger, string orderNumber, int statusCode, string content);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error getting payment status from Midtrans for order {OrderNumber}: {Reason}")]
    private static partial void LogStatusException(ILogger logger, string orderNumber, string reason, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cancelled Midtrans transaction for order {OrderNumber}")]
    private static partial void LogCancelSuccess(ILogger logger, string orderNumber);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Midtrans cancel request for order {OrderNumber} returned {StatusCode}: {Content}")]
    private static partial void LogCancelFailed(ILogger logger, string orderNumber, int statusCode, string content);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to cancel Midtrans transaction for order {OrderNumber}: {Reason}")]
    private static partial void LogCancelException(ILogger logger, string orderNumber, string reason, Exception ex);

    private sealed record SnapTransactionDetails(string OrderId, long GrossAmount);
    private sealed record SnapCustomerDetails(string FirstName, string Email, string Phone);
    private sealed record SnapItemRequest(string Id, long Price, int Quantity, string Name);
    private sealed record SnapCreateRequest(
        SnapTransactionDetails TransactionDetails,
        SnapCustomerDetails CustomerDetails,
        List<SnapItemRequest> ItemDetails);
    private sealed record SnapCreateResponse(string? Token, [property: JsonPropertyName("redirect_url")] string? RedirectUrl);
}
