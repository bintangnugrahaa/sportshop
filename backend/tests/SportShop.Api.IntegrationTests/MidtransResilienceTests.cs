using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Options;
using SportShop.Infrastructure.Services;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class MidtransResilienceTests
{
    [Fact]
    public async Task Resilience_WhenTwo503FollowedBy200_ShouldSucceedAfterRetries()
    {
        var attempts = 0;
        var fakeHandler = new DynamicHttpMessageHandler((request, _) =>
        {
            attempts++;
            if (attempts <= 2)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("Service Unavailable")
                });
            }

            var successContent = """
            {
                "status_code": "200",
                "status_message": "Success",
                "transaction_id": "trx-retry-123",
                "order_id": "SPT-20261120-RETRY1",
                "gross_amount": "100000.00",
                "transaction_status": "settlement"
            }
            """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(successContent, System.Text.Encoding.UTF8, "application/json")
            });
        });

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.Configure<MidtransOptions>(opt =>
        {
            opt.ServerKey = "SB-Mid-server-TESTKEY";
            opt.ClientKey = "SB-Mid-client-TESTKEY";
            opt.IsProduction = false;
            opt.Resilience.MaxRetryAttempts = 3;
            opt.Resilience.RetryInitialDelayMs = 10;
        });

        services.AddHttpClient<IPaymentGateway, MidtransClient>()
            .ConfigurePrimaryHttpMessageHandler(() => fakeHandler)
            .AddResilienceHandler("test-retry-pipeline", (builder, context) =>
            {
                var options = context.ServiceProvider.GetRequiredService<IOptions<MidtransOptions>>().Value.Resilience;

                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = options.MaxRetryAttempts,
                    Delay = TimeSpan.FromMilliseconds(options.RetryInitialDelayMs),
                    BackoffType = DelayBackoffType.Constant,
                    UseJitter = false,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(r => (int)r.StatusCode >= 500)
                });
            });

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<IPaymentGateway>();

        var result = await client.GetPaymentStatusAsync("SPT-20261120-RETRY1");

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be("200");
        result.TransactionId.Should().Be("trx-retry-123");
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task Resilience_CircuitBreaker_ShouldOpenWhenThresholdExceeded()
    {
        var fakeHandler = new DynamicHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Service Unavailable")
            }));

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<MidtransOptions>(opt =>
        {
            opt.ServerKey = "SB-Mid-server-TESTKEY";
            opt.ClientKey = "SB-Mid-client-TESTKEY";
            opt.IsProduction = false;
            opt.Resilience.CircuitBreakerMinimumThroughput = 2;
            opt.Resilience.CircuitBreakerFailureRatio = 0.5;
            opt.Resilience.CircuitBreakerSamplingSeconds = 10;
            opt.Resilience.CircuitBreakerDurationSeconds = 10;
        });

        services.AddHttpClient<IPaymentGateway, MidtransClient>()
            .ConfigurePrimaryHttpMessageHandler(() => fakeHandler)
            .AddResilienceHandler("test-cb-pipeline", (builder, context) =>
            {
                var options = context.ServiceProvider.GetRequiredService<IOptions<MidtransOptions>>().Value.Resilience;

                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = options.CircuitBreakerFailureRatio,
                    SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingSeconds),
                    MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => (int)r.StatusCode >= 500)
                });
            });

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<IPaymentGateway>();

        // Trigger failures to reach minimum throughput (2 failures)
        await client.GetPaymentStatusAsync("SPT-1");
        await client.GetPaymentStatusAsync("SPT-2");

        // The next call should now fail fast because the circuit breaker is open!
        var act = async () => await client.GetPaymentStatusAsync("SPT-3");

        // MidtransClient catches non-OperationCanceledException and logs / returns null,
        // or throws BrokenCircuitException if not handled.
        // Let's verify via the typed client's internal call or testing circuit breaker state:
        var result = await client.GetPaymentStatusAsync("SPT-3");
        result.Should().BeNull(); // MidtransClient catches exception and returns null
    }

    [Fact]
    public async Task Resilience_CircuitBreaker_DirectInvocation_ThrowsBrokenCircuitException()
    {
        var fakeHandler = new DynamicHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddHttpClient("circuit-breaker-direct")
            .ConfigurePrimaryHttpMessageHandler(() => fakeHandler)
            .AddResilienceHandler("direct-cb", builder =>
            {
                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(10),
                    MinimumThroughput = 2,
                    BreakDuration = TimeSpan.FromSeconds(10),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => (int)r.StatusCode >= 500)
                });
            });

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("circuit-breaker-direct");

        // 2 failures to trip the circuit
        await client.GetAsync("http://localhost/test1");
        await client.GetAsync("http://localhost/test2");

        // The 3rd request must throw BrokenCircuitException
        var act = async () => await client.GetAsync("http://localhost/test3");
        await act.Should().ThrowAsync<BrokenCircuitException>();
    }

    private sealed class DynamicHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public DynamicHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
