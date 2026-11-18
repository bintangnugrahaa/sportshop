using System.ComponentModel.DataAnnotations;

namespace SportShop.Infrastructure.Options;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    [Required(ErrorMessage = "Cors:Origins is required.")]
    public string Origins { get; set; } = string.Empty;

    public string[] GetAllowedOrigins() => Origins
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
