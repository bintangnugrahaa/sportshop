using System.Text.Json;
using Microsoft.Extensions.Logging;
using SportShop.Application.Common.Interfaces;
using StackExchange.Redis;

namespace SportShop.Infrastructure.Caching;

public sealed partial class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            var value = await db.StringGetAsync(key).ConfigureAwait(false);
            if (!value.HasValue)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions);
        }
        catch (Exception ex)
        {
            LogCacheGetFailed(_logger, key, ex.Message, ex);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            await db.StringSetAsync(key, json, expiration).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogCacheSetFailed(_logger, key, ex.Message, ex);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(key).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogCacheRemoveFailed(_logger, key, ex.Message, ex);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var fresh = await factory(cancellationToken).ConfigureAwait(false);
        await SetAsync(key, fresh, expiration, cancellationToken).ConfigureAwait(false);
        return fresh;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Redis cache GET failed for key '{Key}'. Reason: {Reason}")]
    private static partial void LogCacheGetFailed(ILogger logger, string key, string reason, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Redis cache SET failed for key '{Key}'. Reason: {Reason}")]
    private static partial void LogCacheSetFailed(ILogger logger, string key, string reason, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Redis cache REMOVE failed for key '{Key}'. Reason: {Reason}")]
    private static partial void LogCacheRemoveFailed(ILogger logger, string key, string reason, Exception ex);
}
