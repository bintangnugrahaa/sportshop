using SportShop.Application.Common.Interfaces;

namespace SportShop.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    private readonly TimeProvider _timeProvider;

    public SystemClock(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;
}
