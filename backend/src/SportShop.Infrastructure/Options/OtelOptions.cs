using System.ComponentModel.DataAnnotations;

namespace SportShop.Infrastructure.Options;

public sealed class OtelOptions
{
    public const string SectionName = "Otel";

    [Required(ErrorMessage = "Otel:OtlpEndpoint is required.")]
    public string OtlpEndpoint { get; set; } = string.Empty;
}
