using System.ComponentModel.DataAnnotations;

namespace SportShop.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required(ErrorMessage = "ConnectionStrings:Default is required.")]
    public string Default { get; set; } = string.Empty;
}
