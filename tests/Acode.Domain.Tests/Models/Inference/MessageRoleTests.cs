namespace Acode.Domain.Tests.Models.Inference;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for MessageRole enum.
/// FR-004a-01 to FR-004a-10: MessageRole must have System, User, Assistant, Tool values
/// with proper serialization and parsing.
/// </summary>
public sealed class MessageRoleTests
{
    [Fact]
    public void MessageRole_HasSystemValue()
    {
        // FR-004a-02: MessageRole MUST have System value
        var role = MessageRole.System;
        role.Should().BeDefined();
    }

    [Fact]
    public void MessageRole_HasUserValue()
    {
        // FR-004a-03: MessageRole MUST have User value
        var role = MessageRole.User;
        role.Should().BeDefined();
    }

    [Fact]
    public void MessageRole_HasAssistantValue()
    {
        // FR-004a-04: MessageRole MUST have Assistant value
        var role = MessageRole.Assistant;
        role.Should().BeDefined();
    }

    [Fact]
    public void MessageRole_HasToolValue()
    {
        // FR-004a-05: MessageRole MUST have Tool value
        var role = MessageRole.Tool;
        role.Should().BeDefined();
    }

    [Theory]
    [InlineData(MessageRole.System, "system")]
    [InlineData(MessageRole.User, "user")]
    [InlineData(MessageRole.Assistant, "assistant")]
    [InlineData(MessageRole.Tool, "tool")]
    public void MessageRole_SerializesToLowercase(MessageRole role, string expected)
    {
        // FR-004a-06: MessageRole values MUST serialize to lowercase strings
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        };
        var json = JsonSerializer.Serialize(role, options);
        json.Should().Be($"\"{expected}\"");
    }

    [Theory]
    [InlineData("system", MessageRole.System)]
    [InlineData("user", MessageRole.User)]
    [InlineData("assistant", MessageRole.Assistant)]
    [InlineData("tool", MessageRole.Tool)]
    public void MessageRole_DeserializesFromLowercase(string json, MessageRole expected)
    {
        // FR-004a-07: MessageRole MUST support case-insensitive parsing (lowercase)
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        };
        var role = JsonSerializer.Deserialize<MessageRole>($"\"{json}\"", options);
        role.Should().Be(expected);
    }

    [Theory]
    [InlineData("SYSTEM", MessageRole.System)]
    [InlineData("USER", MessageRole.User)]
    [InlineData("ASSISTANT", MessageRole.Assistant)]
    [InlineData("TOOL", MessageRole.Tool)]
    public void MessageRole_DeserializesFromUppercase(string json, MessageRole expected)
    {
        // FR-004a-07: MessageRole MUST support case-insensitive parsing (uppercase)
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        };
        var role = JsonSerializer.Deserialize<MessageRole>($"\"{json}\"", options);
        role.Should().Be(expected);
    }

    [Theory]
    [InlineData("System", MessageRole.System)]
    [InlineData("User", MessageRole.User)]
    [InlineData("Assistant", MessageRole.Assistant)]
    [InlineData("Tool", MessageRole.Tool)]
    public void MessageRole_DeserializesFromMixedCase(string json, MessageRole expected)
    {
        // FR-004a-07: MessageRole MUST support case-insensitive parsing (mixed case)
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        };
        var role = JsonSerializer.Deserialize<MessageRole>($"\"{json}\"", options);
        role.Should().Be(expected);
    }

    [Fact]
    public void MessageRole_ThrowsOnInvalidString()
    {
        // FR-004a-08: Unknown role string MUST throw
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        };
        var act = () => JsonSerializer.Deserialize<MessageRole>("\"invalid\"", options);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void MessageRole_HasExplicitValues()
    {
        // FR-004a-09: MessageRole MUST have explicit integer values
        ((int)MessageRole.System).Should().Be(0);
        ((int)MessageRole.User).Should().Be(1);
        ((int)MessageRole.Assistant).Should().Be(2);
        ((int)MessageRole.Tool).Should().Be(3);
    }

    [Fact]
    public void MessageRole_HasNoUnusedValues()
    {
        // FR-004a-10: MessageRole MUST NOT have reserved/unused values
        var values = Enum.GetValues<MessageRole>();
        values.Should().HaveCount(4);
        values.Should().Contain(new[] { MessageRole.System, MessageRole.User, MessageRole.Assistant, MessageRole.Tool });
    }
}
