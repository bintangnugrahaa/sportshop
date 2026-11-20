using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SportShop.Application.Tests;

public sealed class TestServiceCollection : IServiceCollection
{
    private readonly List<ServiceDescriptor> _descriptors = [];

    public int Count => _descriptors.Count;
    public bool IsReadOnly => false;

    public ServiceDescriptor this[int index]
    {
        get => _descriptors[index];
        set => _descriptors[index] = value;
    }

    public void Add(ServiceDescriptor item) => _descriptors.Add(item);
    public void Clear() => _descriptors.Clear();
    public bool Contains(ServiceDescriptor item) => _descriptors.Contains(item);
    public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => _descriptors.CopyTo(array, arrayIndex);
    public IEnumerator<ServiceDescriptor> GetEnumerator() => _descriptors.GetEnumerator();
    public int IndexOf(ServiceDescriptor item) => _descriptors.IndexOf(item);
    public void Insert(int index, ServiceDescriptor item) => _descriptors.Insert(index, item);
    public bool Remove(ServiceDescriptor item) => _descriptors.Remove(item);
    public void RemoveAt(int index) => _descriptors.RemoveAt(index);
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _descriptors.GetEnumerator();
}

public sealed class AppDITests
{
    [Fact]
    public void AddApplication_ShouldRegisterApplicationServices()
    {
        var services = new TestServiceCollection();
        services.AddApplication();

        services.Should().NotBeEmpty();
    }
}
