using System.ComponentModel.DataAnnotations;

namespace SportShop.Infrastructure.Options;

public sealed class MidtransOptions
{
    public const string SectionName = "Midtrans";

    [Required(ErrorMessage = "Midtrans:ServerKey is required.")]
    public string ServerKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Midtrans:ClientKey is required.")]
    public string ClientKey { get; set; } = string.Empty;

    public bool IsProduction { get; set; }

    public MidtransResilienceOptions Resilience { get; set; } = new();
}

public sealed class MidtransResilienceOptions
{
    public int MaxRetryAttempts { get; set; } = 3;
    public double RetryInitialDelayMs { get; set; } = 500;
    public int TimeoutSeconds { get; set; } = 10;
    public int TotalTimeoutSeconds { get; set; } = 30;
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;
    public int CircuitBreakerSamplingSeconds { get; set; } = 30;
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
}
