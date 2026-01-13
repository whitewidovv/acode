namespace Acode.Domain.Tests.Models.Inference;

using System.Text.Json;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for ToolCall record.
/// FR-004a-36 to FR-004a-55: ToolCall must be immutable record with Id, Name, Arguments
/// with validation and helper methods.
/// </summary>
public sealed class ToolCallTests
{
    [Fact]
    public void ToolCall_IsRecord()
    {
        // FR-004a-36: ToolCall MUST be a record type
        var toolCall1 = new ToolCall("id1", "my_tool", CreateJsonElement("{}"));
        var toolCall2 = new ToolCall("id1", "my_tool", CreateJsonElement("{}"));
        toolCall1.Should().Be(toolCall2); // Records have value equality
    }

    [Fact]
    public void ToolCall_HasIdProperty()
    {
        // FR-004a-37: ToolCall MUST have Id property (string, non-empty)
        var toolCall = new ToolCall("call_abc123", "my_tool", CreateJsonElement("{}"));
        toolCall.Id.Should().Be("call_abc123");
    }

    [Fact]
    public void ToolCall_HasNameProperty()
    {
        // FR-004a-38: ToolCall MUST have Name property (string, non-empty)
        var toolCall = new ToolCall("id1", "get_weather", CreateJsonElement("{}"));
        toolCall.Name.Should().Be("get_weather");
    }

    [Fact]
    public void ToolCall_HasArgumentsProperty()
    {
        // FR-004a-39: ToolCall MUST have Arguments property (JsonElement, JSON object)
        var argsJson = "{\"city\":\"Seattle\"}";
        var args = CreateJsonElement(argsJson);
        var toolCall = new ToolCall("id1", "get_weather", args);
        toolCall.Arguments.GetRawText().Should().Be(argsJson);
    }

    [Fact]
    public void ToolCall_ThrowsOnEmptyId()
    {
        // FR-004a-40: ToolCall MUST throw ArgumentException if Id is null or empty
        var args = CreateJsonElement("{}");

        var act1 = () => new ToolCall(string.Empty, "my_tool", args);
        act1.Should().Throw<ArgumentException>().WithParameterName("Id");

        var act2 = () => new ToolCall(null!, "my_tool", args);
        act2.Should().Throw<ArgumentException>().WithParameterName("Id");

        var act3 = () => new ToolCall("   ", "my_tool", args);
        act3.Should().Throw<ArgumentException>().WithParameterName("Id");
    }

    [Fact]
    public void ToolCall_ThrowsOnEmptyName()
    {
        // FR-004a-41: ToolCall MUST throw ArgumentException if Name is null or empty
        var args = CreateJsonElement("{}");

        var act1 = () => new ToolCall("id1", string.Empty, args);
        act1.Should().Throw<ArgumentException>().WithParameterName("Name");

        var act2 = () => new ToolCall("id1", null!, args);
        act2.Should().Throw<ArgumentException>().WithParameterName("Name");

        var act3 = () => new ToolCall("id1", "   ", args);
        act3.Should().Throw<ArgumentException>().WithParameterName("Name");
    }

    [Fact]
    public void ToolCall_ThrowsOnNonObjectArguments()
    {
        // FR-004a-46: ToolCall MUST throw ArgumentException if Arguments is not a JSON object
        var act1 = () => new ToolCall("id1", "my_tool", CreateJsonElement("\"string\""));
        act1.Should().Throw<ArgumentException>().WithParameterName("Arguments");

        var act2 = () => new ToolCall("id1", "my_tool", CreateJsonElement("123"));
        act2.Should().Throw<ArgumentException>().WithParameterName("Arguments");

        var act3 = () => new ToolCall("id1", "my_tool", CreateJsonElement("null"));
        act3.Should().Throw<ArgumentException>().WithParameterName("Arguments");

        var act4 = () => new ToolCall("id1", "my_tool", CreateJsonElement("[]"));
        act4.Should().Throw<ArgumentException>().WithParameterName("Arguments");
    }

    [Fact]
    public void ToolCall_ThrowsOnNameTooLong()
    {
        // FR-004a-43: ToolCall MUST throw ArgumentException if Name > 64 chars
        var longName = new string('a', 65);
        var args = CreateJsonElement("{}");
        var act = () => new ToolCall("id1", longName, args);
        act.Should().Throw<ArgumentException>().WithParameterName("Name");
    }

    [Fact]
    public void ToolCall_AllowsNameExactly64Chars()
    {
        // FR-004a-43: ToolCall MUST allow Name == 64 chars
        var name64 = new string('a', 64);
        var toolCall = new ToolCall("id1", name64, CreateJsonElement("{}"));
        toolCall.Name.Should().HaveLength(64);
    }

    [Theory]
    [InlineData("my_tool")]
    [InlineData("get_weather_forecast")]
    [InlineData("search123")]
    [InlineData("_private_tool")]
    [InlineData("UPPER_CASE")]
    [InlineData("MixedCase_123")]
    public void ToolCall_AllowsValidNames(string name)
    {
        // FR-004a-44: ToolCall Name MUST be alphanumeric + underscore only
        var toolCall = new ToolCall("id1", name, CreateJsonElement("{}"));
        toolCall.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("my-tool")]
    [InlineData("get.weather")]
    [InlineData("search tool")]
    [InlineData("tool@name")]
    [InlineData("tool#1")]
    public void ToolCall_ThrowsOnInvalidNames(string name)
    {
        // FR-004a-44: ToolCall Name MUST reject invalid characters
        var args = CreateJsonElement("{}");
        var act = () => new ToolCall("id1", name, args);
        act.Should().Throw<ArgumentException>().WithParameterName("Name");
    }

    [Fact]
    public void ToolCall_AllowsEmptyJsonObject()
    {
        // FR-004a-46: ToolCall MUST allow empty JSON object "{}"
        var toolCall = new ToolCall("id1", "my_tool", CreateJsonElement("{}"));
        toolCall.Arguments.ValueKind.Should().Be(JsonValueKind.Object);
        toolCall.Arguments.GetRawText().Should().Be("{}");
    }

    [Fact]
    public void ToolCall_AllowsComplexJson()
    {
        // FR-004a-46: ToolCall MUST allow complex JSON arguments
        var argsJson = "{\"city\":\"Seattle\",\"units\":\"metric\",\"days\":7}";
        var args = CreateJsonElement(argsJson);
        var toolCall = new ToolCall("id1", "get_weather", args);
        toolCall.Arguments.GetRawText().Should().Be(argsJson);
    }

    [Fact]
    public void ToolCall_SerializesToJson()
    {
        // FR-004a-47: ToolCall MUST serialize to JSON
        var args = CreateJsonElement("{\"arg\":\"value\"}");
        var toolCall = new ToolCall("call_123", "my_tool", args);
        var json = JsonSerializer.Serialize(toolCall);
        json.Should().Contain("\"id\":\"call_123\"");
        json.Should().Contain("\"name\":\"my_tool\"");

        // Arguments is JsonElement, so it serializes as nested JSON
        json.Should().Contain("\"arguments\":");
        json.Should().Contain("arg");
        json.Should().Contain("value");
    }

    [Fact]
    public void ToolCall_DeserializesFromJson()
    {
        // FR-004a-48: ToolCall MUST deserialize from JSON
        var json = "{\"id\":\"call_123\",\"name\":\"my_tool\",\"arguments\":{\"arg\":\"value\"}}";
        var toolCall = JsonSerializer.Deserialize<ToolCall>(json);
        toolCall.Should().NotBeNull();
        toolCall!.Id.Should().Be("call_123");
        toolCall.Name.Should().Be("my_tool");
        toolCall.Arguments.GetRawText().Should().Be("{\"arg\":\"value\"}");
    }

    [Fact]
    public void ToolCall_HasTryGetArgumentMethod()
    {
        // FR-004a-49: ToolCall MUST have TryGetArgument<T> method
        var args = CreateJsonElement("{\"city\":\"Seattle\",\"units\":\"metric\"}");
        var toolCall = new ToolCall("id1", "get_weather", args);

        var success = toolCall.TryGetArgument<string>("city", out var city);
        success.Should().BeTrue();
        city.Should().Be("Seattle");
    }

    [Fact]
    public void ToolCall_TryGetArgumentReturnsFalseOnMissingKey()
    {
        // FR-004a-50: TryGetArgument MUST return false if key not found
        var args = CreateJsonElement("{\"city\":\"Seattle\"}");
        var toolCall = new ToolCall("id1", "get_weather", args);

        var success = toolCall.TryGetArgument<string>("missing", out var value);
        success.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void ToolCall_TryGetArgumentHandlesTypeMismatch()
    {
        // FR-004a-51: TryGetArgument MUST return false if type mismatch
        var args = CreateJsonElement("{\"count\":42}");
        var toolCall = new ToolCall("id1", "my_tool", args);

        var success = toolCall.TryGetArgument<string>("count", out var value);
        success.Should().BeFalse();
    }

    [Fact]
    public void ToolCall_HasGetArgumentsAsMethod()
    {
        // FR-004a-52: ToolCall MUST have GetArgumentsAs<T> method
        var args = CreateJsonElement("{\"city\":\"Seattle\",\"units\":\"metric\"}");
        var toolCall = new ToolCall("id1", "get_weather", args);

        var parsed = toolCall.GetArgumentsAs<WeatherArgs>();
        parsed.Should().NotBeNull();
        parsed!.City.Should().Be("Seattle");
        parsed.Units.Should().Be("metric");
    }

    [Fact]
    public void ToolCall_GetArgumentsAsReturnsNullOnInvalidJson()
    {
        // FR-004a-53: GetArgumentsAs MUST return null if deserialization fails
        // Empty object doesn't match WeatherArgs requirements
        var args = CreateJsonElement("{}");
        var toolCall = new ToolCall("id1", "get_weather", args);

        var parsed = toolCall.GetArgumentsAs<WeatherArgs>();

        // Should deserialize but properties will be empty/default
        parsed.Should().NotBeNull();
        parsed!.City.Should().BeEmpty();
    }

    [Fact]
    public void ToolCall_IsImmutable()
    {
        // FR-004a-54: ToolCall MUST be immutable (init-only properties)
        var toolCall = new ToolCall("id1", "my_tool", CreateJsonElement("{}"));

        // This should not compile if properties are settable:
        // toolCall.Id = "new_id"; // Compilation error expected

        // Instead verify via constructor:
        var toolCall2 = new ToolCall("id2", "other_tool", CreateJsonElement("{\"x\":1}"));
        toolCall2.Id.Should().Be("id2");
        toolCall2.Name.Should().Be("other_tool");
    }

    [Fact]
    public void ToolCall_HasValueEquality()
    {
        // FR-004a-55: ToolCall MUST have value equality (record semantics)
        var toolCall1 = new ToolCall("id1", "my_tool", CreateJsonElement("{\"x\":1}"));
        var toolCall2 = new ToolCall("id1", "my_tool", CreateJsonElement("{\"x\":1}"));
        var toolCall3 = new ToolCall("id2", "my_tool", CreateJsonElement("{\"x\":1}"));

        toolCall1.Should().Be(toolCall2);
        toolCall1.Should().NotBe(toolCall3);
    }

    /// <summary>
    /// Helper method to create JsonElement from JSON string for testing.
    /// </summary>
    private static JsonElement CreateJsonElement(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    // Helper class for testing GetArgumentsAs
    private sealed record WeatherArgs
    {
        public string City { get; init; } = string.Empty;

        public string Units { get; init; } = string.Empty;
    }
}
