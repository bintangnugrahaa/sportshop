using FluentAssertions;
using Moq;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Services;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public sealed class OrderNumberGeneratorTests
{
    [Fact]
    public void Generate_ShouldFollowExpectedFormat()
    {
        var fixedDate = new DateTime(2026, 11, 19, 10, 30, 0, DateTimeKind.Utc);
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.UtcNow).Returns(fixedDate);

        var generator = new OrderNumberGenerator(clockMock.Object);

        var orderNumber = generator.Generate();

        orderNumber.Should().StartWith("SPT-20261119-");
        orderNumber.Length.Should().Be(19);
        orderNumber.Should().MatchRegex(@"^SPT-20261119-[A-Z0-9]{6}$");
    }

    [Fact]
    public void Generate_MultipleCalls_ShouldBeUnique()
    {
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        var generator = new OrderNumberGenerator(clockMock.Object);

        var numbers = Enumerable.Range(0, 100)
            .Select(_ => generator.Generate())
            .ToList();

        numbers.Distinct().Count().Should().Be(100);
    }
}
