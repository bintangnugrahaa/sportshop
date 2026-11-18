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
}
