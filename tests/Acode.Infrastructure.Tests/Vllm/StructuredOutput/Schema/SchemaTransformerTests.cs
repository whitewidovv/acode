namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput.Schema;

using System.Text.Json;
using Acode.Infrastructure.Vllm.StructuredOutput.Exceptions;
using Acode.Infrastructure.Vllm.StructuredOutput.Schema;
using FluentAssertions;

/// <summary>
/// Tests for SchemaTransformer functionality.
/// </summary>
public sealed class SchemaTransformerTests
{
    private readonly SchemaTransformer transformer = new();

    [Fact]
    public void Transform_SimpleSchema_ReturnsUnchanged()
    {
        // Arrange
        var schemaJson = """
        {
            "type": "object",
            "properties": {
                "path": { "type": "string" }
            },
            "required": ["path"]
        }
        """;
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var result = this.transformer.Transform(schema);

        // Assert
        result.TryGetProperty("type", out var typeEl).Should().BeTrue();
        typeEl.GetString().Should().Be("object");
        result.TryGetProperty("properties", out var propsEl).Should().BeTrue();
        propsEl.TryGetProperty("path", out _).Should().BeTrue();
    }

    [Fact]
    public void Transform_SchemaWithLocalRef_ResolvesRef()
    {
        // Arrange
        var schemaJson = """
        {
            "type": "object",
            "properties": {
                "user": { "$ref": "#/$defs/User" }
            },
            "$defs": {
                "User": {
                    "type": "object",
                    "properties": {
                        "name": { "type": "string" }
                    }
                }
            }
        }
        """;
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var result = this.transformer.Transform(schema);

        // Assert - $defs should be removed, user should have resolved schema
        result.TryGetProperty("$defs", out _).Should().BeFalse();
        result.TryGetProperty("properties", out var propsEl).Should().BeTrue();
        propsEl.TryGetProperty("user", out var userEl).Should().BeTrue();
        userEl.TryGetProperty("type", out var typeEl).Should().BeTrue();
        typeEl.GetString().Should().Be("object");
    }

    [Fact]
    public void Transform_SchemaExceedsSizeLimit_ThrowsSchemaTooComplexException()
    {
        // Arrange - Create transformer with very low size limit
        var smallTransformer = new SchemaTransformer(maxSize: 50);
        var schemaJson = """
        {
            "type": "object",
            "properties": {
                "path": { "type": "string" }
            }
        }
        """;
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var act = () => smallTransformer.Transform(schema);

        // Assert
        var ex = act.Should().Throw<SchemaTooComplexException>().Which;
        ex.ErrorCode.Should().Be("ACODE-VLM-SO-001");
        ex.ActualSize.Should().BeGreaterThan(50);
        ex.MaxSize.Should().Be(50);
    }

    [Fact]
    public void Transform_SchemaExceedsDepthLimit_ThrowsSchemaTooComplexException()
    {
        // Arrange - Create transformer with low depth limit
        var shallowTransformer = new SchemaTransformer(maxDepth: 2);
        var schemaJson = """
        {
            "type": "object",
            "properties": {
                "level1": {
                    "type": "object",
                    "properties": {
                        "level2": {
                            "type": "object",
                            "properties": {
                                "level3": { "type": "string" }
                            }
                        }
                    }
                }
            }
        }
        """;
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var act = () => shallowTransformer.Transform(schema);

        // Assert
        var ex = act.Should().Throw<SchemaTooComplexException>().Which;
        ex.ErrorCode.Should().Be("ACODE-VLM-SO-001");
        ex.ActualDepth.Should().BeGreaterThan(2);
        ex.MaxDepth.Should().Be(2);
        ex.DeepestPath.Should().Contain("level3");
    }

    [Fact]
    public void Transform_CircularRef_ThrowsSchemaTooComplexException()
    {
        // Arrange - Schema with circular reference
        var schemaJson = """
        {
            "type": "object",
            "properties": {
                "node": { "$ref": "#/$defs/Node" }
            },
            "$defs": {
                "Node": {
                    "type": "object",
                    "properties": {
                        "child": { "$ref": "#/$defs/Node" }
                    }
                }
            }
        }
        """;
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var act = () => this.transformer.Transform(schema);

        // Assert
        var ex = act.Should().Throw<SchemaTooComplexException>().Which;
        ex.ErrorCode.Should().Be("ACODE-VLM-SO-002");
        ex.Message.Should().Contain("Circular");
    }

    [Fact]
    public void Transform_ExternalRef_ThrowsSchemaTooComplexException()
    {
        // Arrange - Schema with external reference (not supported)
        var schemaJson = """
        {
            "type": "object",
            "properties": {
                "user": { "$ref": "https://example.com/schema.json" }
            }
        }
        """;
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var act = () => this.transformer.Transform(schema);

        // Assert
        var ex = act.Should().Throw<SchemaTooComplexException>().Which;
        ex.ErrorCode.Should().Be("ACODE-VLM-SO-002");
        ex.Message.Should().Contain("Only local $ref");
    }

    [Fact]
    public void Transform_UnresolvableRef_ThrowsSchemaTooComplexException()
    {
        // Arrange - Schema with reference to non-existent definition
        var schemaJson = """
        {
            "type": "object",
            "properties": {
                "user": { "$ref": "#/$defs/NonExistent" }
            },
            "$defs": {}
        }
        """;
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var act = () => this.transformer.Transform(schema);

        // Assert
        var ex = act.Should().Throw<SchemaTooComplexException>().Which;
        ex.ErrorCode.Should().Be("ACODE-VLM-SO-002");
        ex.Message.Should().Contain("Cannot resolve");
    }

    [Fact]
    public void Transform_ArraySchema_PreservesItems()
    {
        // Arrange
        var schemaJson = """
        {
            "type": "object",
            "properties": {
                "tags": {
                    "type": "array",
                    "items": { "type": "string" }
                }
            }
        }
        """;
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var result = this.transformer.Transform(schema);

        // Assert
        result.TryGetProperty("properties", out var propsEl).Should().BeTrue();
        propsEl.TryGetProperty("tags", out var tagsEl).Should().BeTrue();
        tagsEl.TryGetProperty("items", out var itemsEl).Should().BeTrue();
        itemsEl.TryGetProperty("type", out var typeEl).Should().BeTrue();
        typeEl.GetString().Should().Be("string");
    }

    [Fact]
    public void Validate_ValidSchema_ReturnsIsValidTrue()
    {
        // Arrange
        var schemaJson = """
        {
            "type": "object",
            "properties": {
                "path": { "type": "string" }
            }
        }
        """;
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var result = this.transformer.Validate(schema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Depth.Should().Be(1);
        result.SizeBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Validate_SchemaWithRef_AddsWarning()
    {
        // Arrange
        var schemaJson = """
        {
            "type": "object",
            "properties": {
                "user": { "$ref": "#/$defs/User" }
            },
            "$defs": {
                "User": { "type": "object" }
            }
        }
        """;
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var result = this.transformer.Validate(schema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("$ref"));
    }

    [Fact]
    public void Validate_TooDeep_ReturnsError()
    {
        // Arrange
        var shallowTransformer = new SchemaTransformer(maxDepth: 1);
        var schemaJson = """
        {
            "type": "object",
            "properties": {
                "nested": {
                    "type": "object",
                    "properties": {
                        "deep": { "type": "string" }
                    }
                }
            }
        }
        """;
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var result = shallowTransformer.Validate(schema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("depth limit"));
    }

    [Fact]
    public void Validate_TooLarge_ReturnsError()
    {
        // Arrange
        var smallTransformer = new SchemaTransformer(maxSize: 20);
        var schemaJson = """
        {
            "type": "object",
            "properties": {
                "path": { "type": "string" }
            }
        }
        """;
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var result = smallTransformer.Validate(schema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("size limit"));
    }

    [Fact]
    public void Transform_NestedRefs_ResolvesAll()
    {
        // Arrange - Schema with nested refs (non-circular)
        var schemaJson = """
        {
            "type": "object",
            "properties": {
                "address": { "$ref": "#/$defs/Address" }
            },
            "$defs": {
                "Address": {
                    "type": "object",
                    "properties": {
                        "street": { "$ref": "#/$defs/Street" }
                    }
                },
                "Street": {
                    "type": "string"
                }
            }
        }
        """;
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var result = this.transformer.Transform(schema);

        // Assert - All refs should be resolved
        result.TryGetProperty("$defs", out _).Should().BeFalse();
        result.TryGetProperty("properties", out var propsEl).Should().BeTrue();
        propsEl.TryGetProperty("address", out var addressEl).Should().BeTrue();
        addressEl.TryGetProperty("properties", out var addrPropsEl).Should().BeTrue();
        addrPropsEl.TryGetProperty("street", out var streetEl).Should().BeTrue();
        streetEl.TryGetProperty("type", out var typeEl).Should().BeTrue();
        typeEl.GetString().Should().Be("string");
    }
}
