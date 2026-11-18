using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SportShop.Infrastructure.Caching;
using StackExchange.Redis;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public sealed class RedisCacheServiceTests
{
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock = new();
    private readonly Mock<IDatabase> _databaseMock = new();
    private readonly Mock<ILogger<RedisCacheService>> _loggerMock = new();

    public RedisCacheServiceTests()
    {
        _connectionMultiplexerMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenRedisThrowsException_ExecutesFactoryAndReturnsValueWithoutFailing()
    {
        // Arrange: simulate Redis down / connection failure
        _databaseMock.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis server is down"));
        _databaseMock.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis server is down"));

        var cacheService = new RedisCacheService(_connectionMultiplexerMock.Object, _loggerMock.Object);
        var expectedData = new TestItem("prod-1", "Sepatu Lari", 500000m);

        // Act
        var result = await cacheService.GetOrCreateAsync("prod:1", _ => Task.FromResult(expectedData));

        // Assert: request does not fail, factory returns data
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedData);
    }

    [Fact]
    public async Task GetAsync_WhenRedisThrowsException_ReturnsNullWithoutThrowing()
    {
        // Arrange
        _databaseMock.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisTimeoutException("Redis timeout", CommandStatus.WaitingToBeSent));

        var cacheService = new RedisCacheService(_connectionMultiplexerMock.Object, _loggerMock.Object);

        // Act
        var result = await cacheService.GetAsync<TestItem>("prod:1");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WhenRedisThrowsException_DoesNotThrow()
    {
        // Arrange
        _databaseMock.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisTimeoutException("Redis timeout", CommandStatus.WaitingToBeSent));

        var cacheService = new RedisCacheService(_connectionMultiplexerMock.Object, _loggerMock.Object);
        var item = new TestItem("prod-1", "Sepatu Lari", 500000m);

        // Act
        Func<Task> act = async () => await cacheService.SetAsync("prod:1", item);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveAsync_WhenRedisThrowsException_DoesNotThrow()
    {
        // Arrange
        _databaseMock.Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis down"));

        var cacheService = new RedisCacheService(_connectionMultiplexerMock.Object, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await cacheService.RemoveAsync("prod:1");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCached_ReturnsCachedValueWithoutCallingFactory()
    {
        // Arrange
        var cachedItem = new TestItem("prod-1", "Cached Sepatu Lari", 450000m);
        var json = System.Text.Json.JsonSerializer.Serialize(cachedItem, _jsonOptions);

        _databaseMock.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(json);

        var cacheService = new RedisCacheService(_connectionMultiplexerMock.Object, _loggerMock.Object);
        var factoryCalled = false;

        // Act
        var result = await cacheService.GetOrCreateAsync("prod:1", _ =>
        {
            factoryCalled = true;
            return Task.FromResult(new TestItem("prod-1", "Fresh Sepatu", 500000m));
        });

        // Assert
        factoryCalled.Should().BeFalse();
        result.Should().BeEquivalentTo(cachedItem);
    }

    private sealed record TestItem(string Id, string Name, decimal Price);
}
