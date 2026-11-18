using FluentAssertions;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Services;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public sealed class SystemClockTests
{
    [Fact]
    public void UtcNow_ShouldReturnCurrentUtcTime()
    {
        // Arrange
        IClock clock = new SystemClock();

        // Act
        var now = clock.UtcNow;

        // Assert
        now.Kind.Should().Be(DateTimeKind.Utc);
        now.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
