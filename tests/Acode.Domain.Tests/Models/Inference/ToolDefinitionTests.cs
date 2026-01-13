namespace Acode.Domain.Tests.Models.Inference;

using System;
using System.Text.Json;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for ToolDefinition record.
/// FR-004a-71 to FR-004a-89: ToolDefinition must be immutable record with Name, Description, Parameters
/// with validation and factory methods.
/// </summary>
public sealed class ToolDefinitionTests
{
    [Fact]
    public void ToolDefinition_IsRecord()
    {
        // FR-004a-71: ToolDefinition MUST be a record type
        var params1 = JsonDocument.Parse("{\"type\":\"object\",\"properties\":{}}").RootElement;
        var tool1 = new ToolDefinition("my_tool", "Description", params1);
        var tool2 = new ToolDefinition("my_tool", "Description", params1);
        tool1.Should().Be(tool2); // Records have value equality
    }

    [Fact]
    public void ToolDefinition_HasNameProperty()
    {
        // FR-004a-73: ToolDefinition MUST have Name property
        var parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var tool = new ToolDefinition("get_weather", "Gets weather data", parameters);
        tool.Name.Should().Be("get_weather");
    }

    [Fact]
    public void ToolDefinition_HasDescriptionProperty()
    {
        // FR-004a-75: ToolDefinition MUST have Description property
        var parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var tool = new ToolDefinition("my_tool", "A helpful tool", parameters);
        tool.Description.Should().Be("A helpful tool");
    }

    [Fact]
    public void ToolDefinition_HasParametersProperty()
    {
        // FR-004a-78: ToolDefinition MUST have Parameters property
        var parameters = JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"city\":{\"type\":\"string\"}}}").RootElement;
        var tool = new ToolDefinition("my_tool", "Description", parameters);
        tool.Parameters.GetProperty("type").GetString().Should().Be("object");
    }

    [Fact]
    public void ToolDefinition_HasStrictProperty()
    {
        // FR-004a-82: ToolDefinition MAY have Strict property
        var parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var tool = new ToolDefinition("my_tool", "Description", parameters, true);
        tool.Strict.Should().BeTrue();
    }

    [Fact]
    public void ToolDefinition_StrictDefaultsToTrue()
    {
        // FR-004a-83: Strict MUST default to true
        var parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var tool = new ToolDefinition("my_tool", "Description", parameters);
        tool.Strict.Should().BeTrue();
    }

    [Fact]
    public void ToolDefinition_ThrowsOnEmptyName()
    {
        // FR-004a-73, FR-004a-87: Name MUST be non-empty
        var parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;

        var act1 = () => new ToolDefinition(string.Empty, "Description", parameters);
        act1.Should().Throw<ArgumentException>().WithParameterName("Name");

        var act2 = () => new ToolDefinition(null!, "Description", parameters);
        act2.Should().Throw<ArgumentException>().WithParameterName("Name");
    }

    [Fact]
    public void ToolDefinition_ThrowsOnInvalidName()
    {
        // FR-004a-74: Name MUST follow same rules as ToolCall.Name
        var parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;

        var act = () => new ToolDefinition("invalid-name", "Description", parameters);
        act.Should().Throw<ArgumentException>().WithParameterName("Name");
    }

    [Fact]
    public void ToolDefinition_ThrowsOnNameTooLong()
    {
        // FR-004a-74: Name MUST be <= 64 chars (same as ToolCall)
        var parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var longName = new string('a', 65);

        var act = () => new ToolDefinition(longName, "Description", parameters);
        act.Should().Throw<ArgumentException>().WithParameterName("Name");
    }

    [Fact]
    public void ToolDefinition_ThrowsOnEmptyDescription()
    {
        // FR-004a-76: Description MUST be non-empty
        var parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;

        var act1 = () => new ToolDefinition("my_tool", string.Empty, parameters);
        act1.Should().Throw<ArgumentException>().WithParameterName("Description");

        var act2 = () => new ToolDefinition("my_tool", null!, parameters);
        act2.Should().Throw<ArgumentException>().WithParameterName("Description");
    }

    [Fact]
    public void ToolDefinition_AllowsDescriptionUpTo1024Chars()
    {
        // FR-004a-77: Description SHOULD be max 1024 characters
        var parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var desc1024 = new string('x', 1024);

        var tool = new ToolDefinition("my_tool", desc1024, parameters);
        tool.Description.Should().HaveLength(1024);
    }

    [Fact]
    public void ToolDefinition_ThrowsOnDescriptionTooLong()
    {
        // FR-004a-77: Description max 1024 characters
        var parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var desc1025 = new string('x', 1025);

        var act = () => new ToolDefinition("my_tool", desc1025, parameters);
        act.Should().Throw<ArgumentException>().WithParameterName("Description");
    }

    [Fact]
    public void ToolDefinition_ThrowsOnParametersWithoutType()
    {
        // FR-004a-81: Parameters MUST have type: "object"
        var invalidParams = JsonDocument.Parse("{\"properties\":{}}").RootElement;

        var act = () => new ToolDefinition("my_tool", "Description", invalidParams);
        act.Should().Throw<ArgumentException>().WithParameterName("Parameters");
    }

    [Fact]
    public void ToolDefinition_ThrowsOnParametersWithWrongType()
    {
        // FR-004a-81: Parameters type MUST be "object"
        var invalidParams = JsonDocument.Parse("{\"type\":\"array\"}").RootElement;

        var act = () => new ToolDefinition("my_tool", "Description", invalidParams);
        act.Should().Throw<ArgumentException>().WithParameterName("Parameters");
    }

    [Fact]
    public void ToolDefinition_AcceptsValidJsonSchema()
    {
        // FR-004a-80: Parameters MUST be valid JSON Schema object
        var schema = JsonDocument.Parse(@"{
            ""type"": ""object"",
            ""properties"": {
                ""city"": { ""type"": ""string"" },
                ""units"": { ""type"": ""string"", ""enum"": [""metric"", ""imperial""] }
            },
            ""required"": [""city""]
        }").RootElement;

        var tool = new ToolDefinition("get_weather", "Gets weather for a city", schema);
        tool.Parameters.GetProperty("required")[0].GetString().Should().Be("city");
    }

    [Fact]
    public void ToolDefinition_SerializesToJson()
    {
        // FR-004a-85: ToolDefinition MUST serialize to JSON
        var parameters = JsonDocument.Parse("{\"type\":\"object\",\"properties\":{}}").RootElement;
        var tool = new ToolDefinition("my_tool", "A description", parameters);

        var json = JsonSerializer.Serialize(tool);
        json.Should().Contain("\"name\":\"my_tool\"");
        json.Should().Contain("\"description\":\"A description\"");
    }

    [Fact]
    public void ToolDefinition_DeserializesFromJson()
    {
        // FR-004a-85: ToolDefinition MUST deserialize from JSON
        var json = @"{
            ""name"": ""my_tool"",
            ""description"": ""Test tool"",
            ""parameters"": {""type"": ""object""},
            ""strict"": false
        }";

        var tool = JsonSerializer.Deserialize<ToolDefinition>(json);
        tool.Should().NotBeNull();
        tool!.Name.Should().Be("my_tool");
        tool.Description.Should().Be("Test tool");
        tool.Strict.Should().BeFalse();
    }

    [Fact]
    public void ToolDefinition_IsImmutable()
    {
        // FR-004a-72: ToolDefinition MUST be immutable
        var parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var tool = new ToolDefinition("my_tool", "Description", parameters);

        // Verify immutability via constructor
        var tool2 = new ToolDefinition("other_tool", "Other", parameters, false);
        tool2.Name.Should().Be("other_tool");
        tool2.Strict.Should().BeFalse();
    }

    [Fact]
    public void ToolDefinition_HasValueEquality()
    {
        // FR-004a-88: ToolDefinition MUST implement value equality
        var params1 = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var params2 = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;

        var tool1 = new ToolDefinition("my_tool", "Description", params1);
        var tool2 = new ToolDefinition("my_tool", "Description", params2);
        var tool3 = new ToolDefinition("other_tool", "Description", params1);

        tool1.Should().Be(tool2);
        tool1.Should().NotBe(tool3);
    }

    [Fact]
    public void ToolDefinition_SupportsUnicodeInDescription()
    {
        // FR-004a-75, FR-004a-76: Description must support unicode
        var parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var unicodeDesc = "获取天气信息 ☀️ 🌧️";

        var tool = new ToolDefinition("get_weather", unicodeDesc, parameters);
        tool.Description.Should().Be(unicodeDesc);
    }

    [Theory]
    [InlineData("my_tool")]
    [InlineData("get_weather_forecast")]
    [InlineData("search_123")]
    [InlineData("_private")]
    [InlineData("UPPERCASE_TOOL")]
    public void ToolDefinition_AllowsValidNames(string name)
    {
        // FR-004a-74: Name follows ToolCall naming rules
        var parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var tool = new ToolDefinition(name, "Description", parameters);
        tool.Name.Should().Be(name);
    }

    [Fact]
    public void CreateFromType_Should_Generate_Schema_For_Simple_Type()
    {
        // Arrange - simple parameter type
        var tool = ToolDefinition.CreateFromType<SimpleParams>("my_tool", "Test tool");

        // Assert
        tool.Name.Should().Be("my_tool");
        tool.Description.Should().Be("Test tool");
        tool.Parameters.GetProperty("type").GetString().Should().Be("object");
        tool.Parameters.TryGetProperty("properties", out _).Should().BeTrue();
    }

    [Fact]
    public void CreateFromType_Should_Include_Required_Properties()
    {
        // Arrange & Act
        var tool = ToolDefinition.CreateFromType<RequiredParams>("test", "Description");

        // Assert
        tool.Parameters.TryGetProperty("required", out var required).Should().BeTrue();
        var requiredArray = required.EnumerateArray().Select(e => e.GetString()).ToArray();
        requiredArray.Should().Contain("requiredProp");
    }

    [Fact]
    public void CreateFromType_Should_Handle_Optional_Properties()
    {
        // Arrange & Act
        var tool = ToolDefinition.CreateFromType<OptionalParams>("test", "Description");

        // Assert
        tool.Parameters.TryGetProperty("properties", out var props).Should().BeTrue();
        props.TryGetProperty("optionalProp", out _).Should().BeTrue();
    }

    // Test parameter types
    private record SimpleParams(string Name, int Age);

    private record RequiredParams
    {
        public required string RequiredProp { get; init; }

        public string? OptionalProp { get; init; }
    }

    private record OptionalParams
    {
        public string? OptionalProp { get; init; }
    }
}
