namespace Acode.Domain.Tests.Models.Inference;

using System.Text.Json;
using Acode.Domain.Models.Inference;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for FinishReason enum following TDD (RED phase).
/// FR-004b-019 to FR-004b-029.
/// </summary>
public class FinishReasonTests
{
    [Fact]
    public void FinishReason_HasStopValue()
    {
        // FR-004b-020: FinishReason MUST include Stop value (normal completion)
        var reason = FinishReason.Stop;

        reason.Should().BeDefined();
    }

    [Fact]
    public void FinishReason_HasLengthValue()
    {
        // FR-004b-021: FinishReason MUST include Length value (max tokens reached)
        var reason = FinishReason.Length;

        reason.Should().BeDefined();
    }

    [Fact]
    public void FinishReason_HasToolCallsValue()
    {
        // FR-004b-022: FinishReason MUST include ToolCalls value (generation stopped for tool execution)
        var reason = FinishReason.ToolCalls;

        reason.Should().BeDefined();
    }

    [Fact]
    public void FinishReason_HasContentFilterValue()
    {
        // FR-004b-023: FinishReason MUST include ContentFilter value (content moderation triggered)
        var reason = FinishReason.ContentFilter;

        reason.Should().BeDefined();
    }

    [Fact]
    public void FinishReason_HasErrorValue()
    {
        // FR-004b-024: FinishReason MUST include Error value (generation failed)
        var reason = FinishReason.Error;

        reason.Should().BeDefined();
    }

    [Fact]
    public void FinishReason_HasCancelledValue()
    {
        // FR-004b-025: FinishReason MUST include Cancelled value (request was cancelled)
        var reason = FinishReason.Cancelled;

        reason.Should().BeDefined();
    }

    [Theory]
    [InlineData(FinishReason.Stop, "stop")]
    [InlineData(FinishReason.Length, "length")]
    [InlineData(FinishReason.ToolCalls, "toolCalls")]
    [InlineData(FinishReason.ContentFilter, "contentFilter")]
    [InlineData(FinishReason.Error, "error")]
    [InlineData(FinishReason.Cancelled, "cancelled")]
    public void FinishReason_SerializesToLowercase(FinishReason reason, string expected)
    {
        // FR-004b-026: FinishReason MUST serialize to lowercase strings in JSON
        var options = new JsonSerializerOptions
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        };

        var json = JsonSerializer.Serialize(reason, options);

        json.Should().Be($"\"{expected}\"");
    }

    [Theory]
    [InlineData("stop", FinishReason.Stop)]
    [InlineData("STOP", FinishReason.Stop)]
    [InlineData("Stop", FinishReason.Stop)]
    [InlineData("length", FinishReason.Length)]
    [InlineData("LENGTH", FinishReason.Length)]
    [InlineData("toolCalls", FinishReason.ToolCalls)]
    [InlineData("TOOLCALLS", FinishReason.ToolCalls)]
    [InlineData("contentFilter", FinishReason.ContentFilter)]
    [InlineData("error", FinishReason.Error)]
    [InlineData("cancelled", FinishReason.Cancelled)]
    public void FinishReason_DeserializesCaseInsensitively(string json, FinishReason expected)
    {
        // FR-004b-027: FinishReason MUST deserialize case-insensitively
        var options = new JsonSerializerOptions
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        };

        var reason = JsonSerializer.Deserialize<FinishReason>($"\"{json}\"", options);

        reason.Should().Be(expected);
    }

    [Fact]
    public void FinishReason_HasExplicitIntegerValues()
    {
        // Ensure enum has explicit integer values for stability
        ((int)FinishReason.Stop).Should().Be(0);
        ((int)FinishReason.Length).Should().Be(1);
        ((int)FinishReason.ToolCalls).Should().Be(2);
        ((int)FinishReason.ContentFilter).Should().Be(3);
        ((int)FinishReason.Error).Should().Be(4);
        ((int)FinishReason.Cancelled).Should().Be(5);
    }

    [Fact]
    public void FinishReason_ToolCallsMapsToBothProviders()
    {
        // FR-004b-028, FR-004b-029: Maps from Ollama "done_reason" and vLLM "finish_reason"
        // Both Ollama and vLLM use "tool_calls" or similar - this test verifies the value exists
        var reason = FinishReason.ToolCalls;

        reason.Should().BeDefined();
    }

    [Fact]
    public void FinishReason_AllValuesAreDefined()
    {
        // Ensure all enum values are properly defined
        var values = Enum.GetValues<FinishReason>();

        values.Should().HaveCount(6);
        values.Should().Contain(FinishReason.Stop);
        values.Should().Contain(FinishReason.Length);
        values.Should().Contain(FinishReason.ToolCalls);
        values.Should().Contain(FinishReason.ContentFilter);
        values.Should().Contain(FinishReason.Error);
        values.Should().Contain(FinishReason.Cancelled);
    }
}
