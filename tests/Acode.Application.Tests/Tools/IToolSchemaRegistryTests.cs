namespace Acode.Application.Tests.Tools;

using System.Text.Json;
using Acode.Application.Tools;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for IToolSchemaRegistry interface contract.
/// FR-007: Tool Schema Registry requirements.
/// </summary>
public sealed class IToolSchemaRegistryTests
{
    [Fact]
    public void IToolSchemaRegistry_ShouldDefineCountProperty()
    {
        // Assert - interface should define Count property
        typeof(IToolSchemaRegistry).GetProperty("Count").Should().NotBeNull();
        typeof(IToolSchemaRegistry).GetProperty("Count")!.PropertyType.Should().Be(typeof(int));
    }

    [Fact]
    public void IToolSchemaRegistry_ShouldDefineRegisterToolMethod()
    {
        // Assert
        var method = typeof(IToolSchemaRegistry).GetMethod("RegisterTool");
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(ToolDefinition));
    }

    [Fact]
    public void IToolSchemaRegistry_ShouldDefineGetToolDefinitionMethod()
    {
        // Assert
        var method = typeof(IToolSchemaRegistry).GetMethod("GetToolDefinition");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(ToolDefinition));
    }

    [Fact]
    public void IToolSchemaRegistry_ShouldDefineTryGetToolDefinitionMethod()
    {
        // Assert
        var method = typeof(IToolSchemaRegistry).GetMethod("TryGetToolDefinition");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(bool));
    }

    [Fact]
    public void IToolSchemaRegistry_ShouldDefineGetAllToolsMethod()
    {
        // Assert
        var method = typeof(IToolSchemaRegistry).GetMethod("GetAllTools");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(IReadOnlyCollection<ToolDefinition>));
    }

    [Fact]
    public void IToolSchemaRegistry_ShouldDefineValidateArgumentsMethod()
    {
        // Assert
        var method = typeof(IToolSchemaRegistry).GetMethod("ValidateArguments");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(JsonElement));
    }

    [Fact]
    public void IToolSchemaRegistry_ShouldDefineTryValidateArgumentsMethod()
    {
        // Assert
        var method = typeof(IToolSchemaRegistry).GetMethod("TryValidateArguments");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(bool));
    }

    [Fact]
    public void IToolSchemaRegistry_ShouldDefineIsRegisteredMethod()
    {
        // Assert
        var method = typeof(IToolSchemaRegistry).GetMethod("IsRegistered");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(bool));
    }
}
