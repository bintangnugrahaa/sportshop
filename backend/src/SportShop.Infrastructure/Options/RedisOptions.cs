using System.ComponentModel.DataAnnotations;

namespace SportShop.Infrastructure.Options;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    [Required(ErrorMessage = "Redis:ConnectionString is required.")]
    public string ConnectionString { get; set; } = string.Empty;
}
