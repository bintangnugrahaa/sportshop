using System.ComponentModel.DataAnnotations;

namespace SportShop.Infrastructure.Options;

public sealed class SeqOptions
{
    public const string SectionName = "Seq";

    [Required(ErrorMessage = "Seq:ServerUrl is required.")]
    public string ServerUrl { get; set; } = string.Empty;
}
