using System.ComponentModel.DataAnnotations;

namespace SportShop.Infrastructure.Options;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required(ErrorMessage = "Smtp:Host is required.")]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Smtp:Port must be between 1 and 65535.")]
    public int Port { get; set; } = 1025;

    public string? User { get; set; }

    public string? Password { get; set; }

    [Required(ErrorMessage = "Smtp:From is required.")]
    [EmailAddress(ErrorMessage = "Smtp:From must be a valid email address.")]
    public string From { get; set; } = string.Empty;
}
