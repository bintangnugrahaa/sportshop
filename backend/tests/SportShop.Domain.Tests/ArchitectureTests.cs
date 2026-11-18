using FluentAssertions;
using SportShop.Domain.Common;
using Xunit;

namespace SportShop.Domain.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void DomainLayer_ShouldNotReferenceOtherApplicationLayers()
    {
        // Arrange
        var domainAssembly = typeof(IEntity).Assembly;
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        // Act & Assert: Domain must not reference Application, Infrastructure, or Api
        referencedAssemblies.Should().NotContain(name => name.StartsWith("SportShop.Application", StringComparison.OrdinalIgnoreCase));
        referencedAssemblies.Should().NotContain(name => name.StartsWith("SportShop.Infrastructure", StringComparison.OrdinalIgnoreCase));
        referencedAssemblies.Should().NotContain(name => name.StartsWith("SportShop.Api", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DomainLayer_ShouldNotReferenceExternalFrameworks()
    {
        // Arrange
        var domainAssembly = typeof(IEntity).Assembly;
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        // Act & Assert: Domain must not reference EF Core, ASP.NET, or database packages
        referencedAssemblies.Should().NotContain(name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
        referencedAssemblies.Should().NotContain(name => name.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase));
        referencedAssemblies.Should().NotContain(name => name.StartsWith("Npgsql", StringComparison.OrdinalIgnoreCase));
    }
}
