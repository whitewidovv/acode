namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput.Schema;

using System.Text.Json;
using Acode.Infrastructure.Vllm.StructuredOutput.Schema;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for SchemaValidator.
/// </summary>
public class SchemaValidatorTests
{
    [Fact]
    public void Validate_SimpleObjectSchema_ReturnsValid()
    {
        // Arrange
        var validator = new SchemaValidator(maxDepth: 10, maxSize: 65536);
        var schema = JsonDocument.Parse(@"{""type"":""object"",""properties"":{""name"":{""type"":""string""}}}").RootElement;

        // Act
        var result = validator.Validate(schema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_SchemaExceedsSize_ReturnsInvalid()
    {
        // Arrange
        var validator = new SchemaValidator(maxDepth: 10, maxSize: 100);
        var largeSchema = JsonDocument.Parse(@"{""type"":""object"",""description"":""This is a very long description that will exceed the size limit when combined with other properties""}").RootElement;

        // Act
        var result = validator.Validate(largeSchema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("size"));
    }

    [Fact]
    public void Validate_SchemaExceedsDepth_ReturnsInvalid()
    {
        // Arrange
        var validator = new SchemaValidator(maxDepth: 2, maxSize: 65536);
        var deepSchema = JsonDocument.Parse(@"{
            ""type"":""object"",
            ""properties"":{
                ""level1"":{
                    ""type"":""object"",
                    ""properties"":{
                        ""level2"":{
                            ""type"":""object"",
                            ""properties"":{
                                ""level3"":{""type"":""string""}
                            }
                        }
                    }
                }
            }
        }").RootElement;

        // Act
        var result = validator.Validate(deepSchema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("depth"));
    }

    [Fact]
    public void Validate_ExternalRef_ReturnsInvalid()
    {
        // Arrange
        var validator = new SchemaValidator(maxDepth: 10, maxSize: 65536);
        var schemaWithExternalRef = JsonDocument.Parse(@"{
            ""type"":""object"",
            ""$ref"":""http://example.com/schema.json#/definitions/Foo""
        }").RootElement;

        // Act
        var result = validator.Validate(schemaWithExternalRef);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("external"));
    }

    [Fact]
    public void Validate_LocalRef_ReturnsValid()
    {
        // Arrange
        var validator = new SchemaValidator(maxDepth: 10, maxSize: 65536);
        var schemaWithLocalRef = JsonDocument.Parse(@"{
            ""type"":""object"",
            ""properties"":{
                ""user"":{""$ref"":""#/definitions/User""}
            },
            ""definitions"":{
                ""User"":{""type"":""object"",""properties"":{""name"":{""type"":""string""}}}
            }
        }").RootElement;

        // Act
        var result = validator.Validate(schemaWithLocalRef);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_NestedObjectSchema_ReturnsValid()
    {
        // Arrange
        var validator = new SchemaValidator(maxDepth: 10, maxSize: 65536);
        var nestedSchema = JsonDocument.Parse(@"{
            ""type"":""object"",
            ""properties"":{
                ""person"":{
                    ""type"":""object"",
                    ""properties"":{
                        ""address"":{
                            ""type"":""object"",
                            ""properties"":{
                                ""street"":{""type"":""string""},
                                ""city"":{""type"":""string""}
                            }
                        }
                    }
                }
            }
        }").RootElement;

        // Act
        var result = validator.Validate(nestedSchema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ArraySchema_ReturnsValid()
    {
        // Arrange
        var validator = new SchemaValidator(maxDepth: 10, maxSize: 65536);
        var arraySchema = JsonDocument.Parse(@"{
            ""type"":""array"",
            ""items"":{""type"":""object"",""properties"":{""id"":{""type"":""number""}}}
        }").RootElement;

        // Act
        var result = validator.Validate(arraySchema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EnumSchema_ReturnsValid()
    {
        // Arrange
        var validator = new SchemaValidator(maxDepth: 10, maxSize: 65536);
        var enumSchema = JsonDocument.Parse(@"{
            ""type"":""string"",
            ""enum"":[""red"",""green"",""blue""]
        }").RootElement;

        // Act
        var result = validator.Validate(enumSchema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptySchema_ReturnsValid()
    {
        // Arrange
        var validator = new SchemaValidator(maxDepth: 10, maxSize: 65536);
        var emptySchema = JsonDocument.Parse(@"{}").RootElement;

        // Act
        var result = validator.Validate(emptySchema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
