namespace Acode.Application.Tests.Tools;

using Acode.Application.Tools;
using Acode.Domain.Models.Inference;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for IToolSchemaProvider interface contract.
/// FR-007: Tool Schema Registry requirements.
/// </summary>
public sealed class IToolSchemaProviderTests
{
    [Fact]
    public void IToolSchemaProvider_ShouldDefineNameProperty()
    {
        // Assert
        typeof(IToolSchemaProvider).GetProperty("Name").Should().NotBeNull();
        typeof(IToolSchemaProvider).GetProperty("Name")!.PropertyType.Should().Be(typeof(string));
    }

    [Fact]
    public void IToolSchemaProvider_ShouldDefineVersionProperty()
    {
        // Assert
        typeof(IToolSchemaProvider).GetProperty("Version").Should().NotBeNull();
        typeof(IToolSchemaProvider).GetProperty("Version")!.PropertyType.Should().Be(typeof(string));
    }

    [Fact]
    public void IToolSchemaProvider_ShouldDefineOrderProperty()
    {
        // Assert
        typeof(IToolSchemaProvider).GetProperty("Order").Should().NotBeNull();
        typeof(IToolSchemaProvider).GetProperty("Order")!.PropertyType.Should().Be(typeof(int));
    }

    [Fact]
    public void IToolSchemaProvider_ShouldDefineGetToolDefinitionsMethod()
    {
        // Assert
        var method = typeof(IToolSchemaProvider).GetMethod("GetToolDefinitions");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(IEnumerable<ToolDefinition>));
    }
}
