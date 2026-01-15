namespace Acode.Application.Tests.Inference;

using System.Text.Json;
using Acode.Application.Inference;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ResponseFormat and JsonSchemaFormat classes.
/// </summary>
public class ResponseFormatTests
{
    [Fact]
    public void Should_Create_JsonObject_ResponseFormat()
    {
        // Arrange & Act
        var format = new ResponseFormat { Type = "json_object" };

        // Assert
        format.Type.Should().Be("json_object");
        format.JsonSchema.Should().BeNull();
    }

    [Fact]
    public void Should_Create_JsonSchema_ResponseFormat()
    {
        // Arrange
        var schemaJson = JsonDocument.Parse("""{"type":"object","properties":{"name":{"type":"string"}}}""").RootElement;
        var jsonSchema = new JsonSchemaFormat
        {
            Name = "user_info",
            Schema = schemaJson
        };

        // Act
        var format = new ResponseFormat
        {
            Type = "json_schema",
            JsonSchema = jsonSchema
        };

        // Assert
        format.Type.Should().Be("json_schema");
        format.JsonSchema.Should().NotBeNull();
        format.JsonSchema.Name.Should().Be("user_info");
    }

    [Fact]
    public void Should_Create_JsonSchemaFormat_With_Name()
    {
        // Arrange
        var schemaJson = JsonDocument.Parse("""{"type":"object"}""").RootElement;

        // Act
        var schema = new JsonSchemaFormat
        {
            Name = "test_schema",
            Schema = schemaJson
        };

        // Assert
        schema.Name.Should().Be("test_schema");
        schema.Schema.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void Should_Support_Null_JsonSchema()
    {
        // Arrange & Act
        var format = new ResponseFormat { Type = "json_object" };

        // Assert
        format.JsonSchema.Should().BeNull("json_object mode doesn't require a schema");
    }

    [Fact]
    public void Should_Allow_Multiple_ResponseFormats()
    {
        // Arrange
        var schema1 = new JsonSchemaFormat { Name = "schema1" };
        var schema2 = new JsonSchemaFormat { Name = "schema2" };

        // Act
        var format1 = new ResponseFormat { Type = "json_schema", JsonSchema = schema1 };
        var format2 = new ResponseFormat { Type = "json_schema", JsonSchema = schema2 };

        // Assert
        format1.JsonSchema!.Name.Should().Be("schema1");
        format2.JsonSchema!.Name.Should().Be("schema2");
    }
}
