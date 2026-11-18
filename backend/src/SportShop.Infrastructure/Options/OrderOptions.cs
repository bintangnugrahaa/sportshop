using System.ComponentModel.DataAnnotations;

namespace SportShop.Infrastructure.Options;

public sealed class OrderOptions
{
    public const string SectionName = "Order";

    [Range(1, 168, ErrorMessage = "Order:ExpiryHours must be between 1 and 168 hours.")]
    public int ExpiryHours { get; set; } = 24;
}
