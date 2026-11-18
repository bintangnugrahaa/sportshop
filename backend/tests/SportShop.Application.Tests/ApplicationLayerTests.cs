using FluentAssertions;
using SportShop.Application.Common;
using Xunit;

namespace SportShop.Application.Tests;

public sealed class ApplicationLayerTests
{
    [Fact]
    public void ApplicationLayer_ShouldReferenceDomainLayer()
    {
        // Arrange
        var appAssembly = typeof(IApplicationMarker).Assembly;
        var referencedAssemblies = appAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        // Act & Assert
        referencedAssemblies.Should().Contain(name => name.StartsWith("SportShop.Domain", StringComparison.OrdinalIgnoreCase));
    }
}
