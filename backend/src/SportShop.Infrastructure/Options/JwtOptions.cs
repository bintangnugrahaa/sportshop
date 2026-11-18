using System.ComponentModel.DataAnnotations;

namespace SportShop.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "Jwt:Key is required.")]
    [MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters long.")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "Jwt:Issuer is required.")]
    public string Issuer { get; set; } = string.Empty;

    [Required(ErrorMessage = "Jwt:Audience is required.")]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Jwt:ExpiryMinutes must be between 1 and 1440 minutes.")]
    public int ExpiryMinutes { get; set; } = 15;

    [Range(1, 30, ErrorMessage = "Jwt:RefreshTokenExpiryDays must be between 1 and 30 days.")]
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
