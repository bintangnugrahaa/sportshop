using System.Globalization;
using System.Security.Cryptography;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Infrastructure.Services;

public sealed class OrderNumberGenerator : IOrderNumberGenerator
{
    private static readonly char[] _characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private readonly IClock _clock;

    public OrderNumberGenerator(IClock clock)
    {
        _clock = clock;
    }

    public string Generate(DateTime? timestamp = null)
    {
        var date = (timestamp ?? _clock.UtcNow).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var randomPart = RandomNumberGenerator.GetString(_characters, 6);
        return $"SPT-{date}-{randomPart}";
    }
}
