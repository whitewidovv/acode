namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput.ResponseFormat;

using System.Text.Json;
using Acode.Infrastructure.Vllm.StructuredOutput.ResponseFormat;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ResponseFormatBuilder.
/// </summary>
public class ResponseFormatBuilderTests
{
    [Fact]
    public void Build_JsonObject_ReturnsCorrectFormat()
    {
        // Arrange
        var builder = new ResponseFormatBuilder();

        // Act
        var format = builder.Build(ResponseFormatType.JsonObject);

        // Assert
        format.Should().NotBeNull();
        format.Type.Should().Be("json_object");
        format.JsonSchema.Should().BeNull();
    }

    [Fact]
    public void Build_JsonSchema_WithSchema_ReturnsCorrectFormat()
    {
        // Arrange
        var builder = new ResponseFormatBuilder();
        var schema = JsonDocument.Parse(@"{""type"":""object"",""properties"":{""name"":{""type"":""string""}}}").RootElement;

        // Act
        var format = builder.Build(ResponseFormatType.JsonSchema, schema);

        // Assert
        format.Should().NotBeNull();
        format.Type.Should().Be("json_schema");
        format.JsonSchema.Should().NotBeNull();
        format.JsonSchema!.Value.TryGetProperty("type", out var typeProperty).Should().BeTrue();
        typeProperty.GetString().Should().Be("object");
    }

    [Fact]
    public void Build_JsonSchema_WithoutSchema_Throws()
    {
        // Arrange
        var builder = new ResponseFormatBuilder();

        // Act
        var action = () => builder.Build(ResponseFormatType.JsonSchema, null);

        // Assert
        action.Should().Throw<ArgumentException>().WithMessage("*Schema is required*");
    }

    [Fact]
    public void Build_InvalidType_Throws()
    {
        // Arrange
        var builder = new ResponseFormatBuilder();

        // Act
        var action = () => builder.Build((ResponseFormatType)999);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VllmResponseFormat_Properties_CanBeSet()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{""type"":""object""}").RootElement;
        var format = new VllmResponseFormat
        {
            Type = "json_schema",
            JsonSchema = schema,
        };

        // Act & Assert
        format.Type.Should().Be("json_schema");
        format.JsonSchema.Should().Be(schema);
    }
}
