using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Options;
using SportShop.Infrastructure.Services;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class MidtransClientTests
{
    private readonly MidtransOptions _options = new()
    {
        ServerKey = "SB-Mid-server-TESTKEY123",
        ClientKey = "SB-Mid-client-TESTKEY123",
        IsProduction = false
    };

    [Fact]
    public async Task CreateSnapTransactionAsync_WhenApiReturnsSuccess_ShouldReturnTokenAndRedirectUrl()
    {
        var responseJson = """
        {
            "token": "snap-token-xyz-123",
            "redirect_url": "https://app.sandbox.midtrans.com/snap/v2/vtweb/snap-token-xyz-123"
        }
        """;

        var fakeHandler = new FakeHttpMessageHandler((request, cancellationToken) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.ToString().Should().Be("https://app.sandbox.midtrans.com/snap/v1/transactions");
            request.Headers.Authorization.Should().NotBeNull();
            request.Headers.Authorization!.Scheme.Should().Be("Basic");

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            });
        });

        var httpClient = new HttpClient(fakeHandler);
        var client = new MidtransClient(httpClient, Options.Create(_options), NullLogger<MidtransClient>.Instance);

        var request = new CreateSnapTransactionRequest(
            OrderNumber: "SPT-20261120-000001",
            GrossAmount: 150000m,
            CustomerName: "Budi Santoso",
            CustomerEmail: "budi@example.com",
            CustomerPhone: "08123456789",
            Items: new List<SnapItemDto>
            {
                new("v1", "Sepatu Lari", 100000m, 1),
                new("shipping", "Ongkos Kirim", 50000m, 1)
            });

        var result = await client.CreateSnapTransactionAsync(request);

        result.Success.Should().BeTrue();
        result.Token.Should().Be("snap-token-xyz-123");
        result.RedirectUrl.Should().Be("https://app.sandbox.midtrans.com/snap/v2/vtweb/snap-token-xyz-123");
    }

    [Fact]
    public async Task CreateSnapTransactionAsync_WhenApiReturnsError_ShouldReturnFailureResult()
    {
        var errorJson = """
        {
            "error_messages": ["transaction_details.order_id already used"]
        }
        """;

        var fakeHandler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage((HttpStatusCode)406)
            {
                Content = new StringContent(errorJson, System.Text.Encoding.UTF8, "application/json")
            }));

        var httpClient = new HttpClient(fakeHandler);
        var client = new MidtransClient(httpClient, Options.Create(_options), NullLogger<MidtransClient>.Instance);

        var request = new CreateSnapTransactionRequest(
            OrderNumber: "SPT-20261120-000001",
            GrossAmount: 150000m,
            CustomerName: "Budi Santoso",
            CustomerEmail: "budi@example.com",
            CustomerPhone: "08123456789",
            Items: new List<SnapItemDto>());

        var result = await client.CreateSnapTransactionAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("midtrans.duplicate");
        result.ErrorMessage.Should().Contain("transaction_details.order_id already used");
    }

    [Fact]
    public async Task GetPaymentStatusAsync_WhenFound_ShouldParseAllFields()
    {
        var responseJson = """
        {
            "status_code": "200",
            "status_message": "Success, transaction found",
            "transaction_id": "trx-uuid-777",
            "order_id": "SPT-20261120-000001",
            "gross_amount": "150000.00",
            "payment_type": "bank_transfer",
            "transaction_time": "2026-11-20 14:00:00",
            "transaction_status": "settlement",
            "fraud_status": "accept",
            "settlement_time": "2026-11-20 14:05:00",
            "signature_key": "dummy-sig-key"
        }
        """;

        var fakeHandler = new FakeHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.ToString().Should().Be("https://api.sandbox.midtrans.com/v2/SPT-20261120-000001/status");

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            });
        });

        var httpClient = new HttpClient(fakeHandler);
        var client = new MidtransClient(httpClient, Options.Create(_options), NullLogger<MidtransClient>.Instance);

        var statusResult = await client.GetPaymentStatusAsync("SPT-20261120-000001");

        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be("200");
        statusResult.TransactionId.Should().Be("trx-uuid-777");
        statusResult.OrderNumber.Should().Be("SPT-20261120-000001");
        statusResult.GrossAmount.Should().Be(150000m);
        statusResult.PaymentType.Should().Be("bank_transfer");
        statusResult.TransactionStatus.Should().Be("settlement");
        statusResult.FraudStatus.Should().Be("accept");
        statusResult.SignatureKey.Should().Be("dummy-sig-key");
    }

    [Fact]
    public async Task GetPaymentStatusAsync_WhenNotFound_ShouldReturnNull()
    {
        var fakeHandler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var httpClient = new HttpClient(fakeHandler);
        var client = new MidtransClient(httpClient, Options.Create(_options), NullLogger<MidtransClient>.Instance);

        var statusResult = await client.GetPaymentStatusAsync("NON-EXISTENT");

        statusResult.Should().BeNull();
    }

    [Fact]
    public async Task CancelTransactionAsync_WhenApiReturnsSuccess_ShouldReturnTrue()
    {
        var fakeHandler = new FakeHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.ToString().Should().Be("https://api.sandbox.midtrans.com/v2/SPT-20261120-000001/cancel");

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status_code\":\"200\"}", System.Text.Encoding.UTF8, "application/json")
            });
        });

        var httpClient = new HttpClient(fakeHandler);
        var client = new MidtransClient(httpClient, Options.Create(_options), NullLogger<MidtransClient>.Instance);

        var cancelled = await client.CancelTransactionAsync("SPT-20261120-000001");

        cancelled.Should().BeTrue();
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
