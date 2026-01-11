namespace Acode.Infrastructure.Tests.Tools;

using System.Text.Json;
using Acode.Application.Tools;
using Acode.Domain.Models.Inference;
using Acode.Domain.Tools;
using Acode.Infrastructure.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Tests for ToolSchemaRegistry implementation.
/// FR-007: Tool Schema Registry requirements.
/// </summary>
public sealed class ToolSchemaRegistryTests
{
    private static readonly JsonElement ValidObjectSchema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "path": { "type": "string" },
                "encoding": { "type": "string" }
            },
            "required": ["path"]
        }
        """).RootElement;

    private readonly ToolSchemaRegistry sut;

    public ToolSchemaRegistryTests()
    {
        this.sut = new ToolSchemaRegistry(NullLogger<ToolSchemaRegistry>.Instance);
    }

    [Fact]
    public void Constructor_ShouldCreateEmptyRegistry()
    {
        // Assert
        this.sut.Count.Should().Be(0);
        this.sut.GetAllTools().Should().BeEmpty();
    }

    [Fact]
    public void RegisterTool_WithValidTool_ShouldRegister()
    {
        // Arrange
        var tool = new ToolDefinition("read_file", "Reads a file", ValidObjectSchema);

        // Act
        this.sut.RegisterTool(tool);

        // Assert
        this.sut.Count.Should().Be(1);
        this.sut.IsRegistered("read_file").Should().BeTrue();
    }

    [Fact]
    public void RegisterTool_WithNullTool_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => this.sut.RegisterTool(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterTool_WithSameToolTwice_ShouldBeIdempotent()
    {
        // Arrange
        var tool = new ToolDefinition("read_file", "Reads a file", ValidObjectSchema);

        // Act
        this.sut.RegisterTool(tool);
        this.sut.RegisterTool(tool);

        // Assert - Idempotent, no exception
        this.sut.Count.Should().Be(1);
    }

    [Fact]
    public void RegisterTool_WithConflictingDefinition_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tool1 = new ToolDefinition("read_file", "Reads a file", ValidObjectSchema);
        var tool2 = new ToolDefinition("read_file", "Different description", ValidObjectSchema);

        // Act
        this.sut.RegisterTool(tool1);
        var act = () => this.sut.RegisterTool(tool2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*read_file*already registered*different*");
    }

    [Fact]
    public void GetToolDefinition_WithRegisteredTool_ShouldReturnTool()
    {
        // Arrange
        var tool = new ToolDefinition("read_file", "Reads a file", ValidObjectSchema);
        this.sut.RegisterTool(tool);

        // Act
        var result = this.sut.GetToolDefinition("read_file");

        // Assert
        result.Should().Be(tool);
    }

    [Fact]
    public void GetToolDefinition_WithUnregisteredTool_ShouldThrowKeyNotFoundException()
    {
        // Act
        var act = () => this.sut.GetToolDefinition("unknown_tool");

        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*unknown_tool*not registered*");
    }

    [Fact]
    public void TryGetToolDefinition_WithRegisteredTool_ShouldReturnTrue()
    {
        // Arrange
        var tool = new ToolDefinition("read_file", "Reads a file", ValidObjectSchema);
        this.sut.RegisterTool(tool);

        // Act
        var result = this.sut.TryGetToolDefinition("read_file", out var foundTool);

        // Assert
        result.Should().BeTrue();
        foundTool.Should().Be(tool);
    }

    [Fact]
    public void TryGetToolDefinition_WithUnregisteredTool_ShouldReturnFalse()
    {
        // Act
        var result = this.sut.TryGetToolDefinition("unknown_tool", out var foundTool);

        // Assert
        result.Should().BeFalse();
        foundTool.Should().BeNull();
    }

    [Fact]
    public void GetAllTools_ShouldReturnAllRegisteredTools()
    {
        // Arrange
        var tool1 = new ToolDefinition("read_file", "Reads a file", ValidObjectSchema);
        var tool2 = new ToolDefinition("write_file", "Writes a file", ValidObjectSchema);
        this.sut.RegisterTool(tool1);
        this.sut.RegisterTool(tool2);

        // Act
        var result = this.sut.GetAllTools();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(tool1);
        result.Should().Contain(tool2);
    }

    [Fact]
    public void IsRegistered_WithRegisteredTool_ShouldReturnTrue()
    {
        // Arrange
        var tool = new ToolDefinition("read_file", "Reads a file", ValidObjectSchema);
        this.sut.RegisterTool(tool);

        // Act & Assert
        this.sut.IsRegistered("read_file").Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_WithUnregisteredTool_ShouldReturnFalse()
    {
        // Act & Assert
        this.sut.IsRegistered("unknown_tool").Should().BeFalse();
    }

    [Fact]
    public void ValidateArguments_WithValidArguments_ShouldReturnArguments()
    {
        // Arrange
        var tool = new ToolDefinition("read_file", "Reads a file", ValidObjectSchema);
        this.sut.RegisterTool(tool);
        var args = JsonDocument.Parse("""{"path": "test.txt"}""").RootElement;

        // Act
        var result = this.sut.ValidateArguments("read_file", args);

        // Assert
        result.GetProperty("path").GetString().Should().Be("test.txt");
    }

    [Fact]
    public void ValidateArguments_WithMissingRequiredProperty_ShouldThrowSchemaValidationException()
    {
        // Arrange
        var tool = new ToolDefinition("read_file", "Reads a file", ValidObjectSchema);
        this.sut.RegisterTool(tool);
        var args = JsonDocument.Parse("""{"encoding": "utf-8"}""").RootElement;

        // Act
        var act = () => this.sut.ValidateArguments("read_file", args);

        // Assert
        act.Should().Throw<SchemaValidationException>()
            .Where(e => e.ToolName == "read_file")
            .Where(e => e.Errors.Any(err => err.Path.Contains("path", StringComparison.Ordinal)));
    }

    [Fact]
    public void ValidateArguments_WithUnknownTool_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var args = JsonDocument.Parse("""{}""").RootElement;

        // Act
        var act = () => this.sut.ValidateArguments("unknown_tool", args);

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void TryValidateArguments_WithValidArguments_ShouldReturnTrueAndNoErrors()
    {
        // Arrange
        var tool = new ToolDefinition("read_file", "Reads a file", ValidObjectSchema);
        this.sut.RegisterTool(tool);
        var args = JsonDocument.Parse("""{"path": "test.txt"}""").RootElement;

        // Act
        var result = this.sut.TryValidateArguments("read_file", args, out var errors, out var validated);

        // Assert
        result.Should().BeTrue();
        errors.Should().BeEmpty();
        validated.GetProperty("path").GetString().Should().Be("test.txt");
    }

    [Fact]
    public void TryValidateArguments_WithInvalidArguments_ShouldReturnFalseAndErrors()
    {
        // Arrange
        var tool = new ToolDefinition("read_file", "Reads a file", ValidObjectSchema);
        this.sut.RegisterTool(tool);
        var args = JsonDocument.Parse("""{}""").RootElement;

        // Act
        var result = this.sut.TryValidateArguments("read_file", args, out var errors, out var validated);

        // Assert
        result.Should().BeFalse();
        errors.Should().NotBeEmpty();
        validated.ValueKind.Should().Be(JsonValueKind.Undefined);
    }

    [Fact]
    public void ValidateArguments_WithTypeMismatch_ShouldThrowSchemaValidationException()
    {
        // Arrange
        var schemaJson = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "count": { "type": "integer" }
                },
                "required": ["count"]
            }
            """).RootElement;
        var tool = new ToolDefinition("count_lines", "Counts lines", schemaJson);
        this.sut.RegisterTool(tool);
        var args = JsonDocument.Parse("""{"count": "not a number"}""").RootElement;

        // Act
        var act = () => this.sut.ValidateArguments("count_lines", args);

        // Assert
        act.Should().Throw<SchemaValidationException>()
            .Where(e => e.Errors.Any());
    }

    [Fact]
    public void Registry_ShouldImplementIToolSchemaRegistry()
    {
        // Assert
        this.sut.Should().BeAssignableTo<IToolSchemaRegistry>();
    }
}
