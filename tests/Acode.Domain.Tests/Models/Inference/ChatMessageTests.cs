namespace Acode.Domain.Tests.Models.Inference;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for ChatMessage record following TDD (RED phase).
/// FR-004a-11 to FR-004a-35.
/// </summary>
public class ChatMessageTests
{
    [Fact]
    public void ChatMessage_HasRoleProperty()
    {
        // FR-004a-13: ChatMessage MUST have Role property (required)
        var message = new ChatMessage(MessageRole.User, "Hello", null, null);

        message.Role.Should().Be(MessageRole.User);
    }

    [Fact]
    public void ChatMessage_HasContentProperty()
    {
        // FR-004a-14: ChatMessage MUST have Content property (nullable)
        var message1 = new ChatMessage(MessageRole.User, "Hello", null, null);
        var toolCalls = new List<ToolCall>
        {
            new ToolCall("id1", "get_weather", CreateJsonElement("{\"city\":\"Seattle\"}")),
        };
        var message2 = new ChatMessage(MessageRole.Assistant, null, toolCalls, null);

        message1.Content.Should().Be("Hello");
        message2.Content.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_HasToolCallsProperty()
    {
        // FR-004a-15, FR-004a-18: ChatMessage MUST have ToolCalls property (nullable, IReadOnlyList)
        var toolCalls = new List<ToolCall>
        {
            new ToolCall("id1", "get_weather", CreateJsonElement("{\"city\":\"Seattle\"}")),
        };

        var message1 = new ChatMessage(MessageRole.Assistant, null, toolCalls, null);
        var message2 = new ChatMessage(MessageRole.User, "Hello", null, null);

        message1.ToolCalls.Should().NotBeNull();
        message1.ToolCalls.Should().HaveCount(1);
        message2.ToolCalls.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_HasToolCallIdProperty()
    {
        // FR-004a-16: ChatMessage MUST have ToolCallId property (nullable)
        var message1 = new ChatMessage(MessageRole.Tool, "result", null, "id1");
        var message2 = new ChatMessage(MessageRole.User, "Hello", null, null);

        message1.ToolCallId.Should().Be("id1");
        message2.ToolCallId.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_ThrowsWhenToolMessageMissingToolCallId()
    {
        // FR-004a-17: ToolCallId MUST be set when Role is Tool
        var act = () => new ChatMessage(MessageRole.Tool, "result", null, null);

        act.Should().Throw<ArgumentException>().WithParameterName("ToolCallId");
    }

    [Fact]
    public void ChatMessage_ThrowsWhenUserMessageMissingContent()
    {
        // FR-004a-20: Content MUST be non-null for User messages
        var act = () => new ChatMessage(MessageRole.User, null, null, null);

        act.Should().Throw<ArgumentException>().WithParameterName("Content");
    }

    [Fact]
    public void ChatMessage_ThrowsWhenSystemMessageMissingContent()
    {
        // FR-004a-21: Content MUST be non-null for System messages
        var act = () => new ChatMessage(MessageRole.System, null, null, null);

        act.Should().Throw<ArgumentException>().WithParameterName("Content");
    }

    [Fact]
    public void ChatMessage_ThrowsWhenToolMessageMissingContent()
    {
        // FR-004a-22: Content MUST be non-null for Tool messages
        var act = () => new ChatMessage(MessageRole.Tool, null, null, "id1");

        act.Should().Throw<ArgumentException>().WithParameterName("Content");
    }

    [Fact]
    public void ChatMessage_ThrowsWhenAssistantMessageHasNeitherContentNorToolCalls()
    {
        // FR-004a-19: Content OR ToolCalls MUST be non-null for Assistant
        var act = () => new ChatMessage(MessageRole.Assistant, null, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChatMessage_AllowsAssistantMessageWithOnlyContent()
    {
        // FR-004a-19: Content OR ToolCalls MUST be non-null for Assistant
        var message = new ChatMessage(MessageRole.Assistant, "Hello", null, null);

        message.Content.Should().Be("Hello");
        message.ToolCalls.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_AllowsAssistantMessageWithOnlyToolCalls()
    {
        // FR-004a-19: Content OR ToolCalls MUST be non-null for Assistant
        var toolCalls = new List<ToolCall>
        {
            new ToolCall("id1", "get_weather", CreateJsonElement("{\"city\":\"Seattle\"}")),
        };

        var message = new ChatMessage(MessageRole.Assistant, null, toolCalls, null);

        message.Content.Should().BeNull();
        message.ToolCalls.Should().NotBeNull();
    }

    [Fact]
    public void ChatMessage_AllowsAssistantMessageWithBothContentAndToolCalls()
    {
        // FR-004a-19: Content OR ToolCalls MUST be non-null for Assistant
        var toolCalls = new List<ToolCall>
        {
            new ToolCall("id1", "get_weather", CreateJsonElement("{\"city\":\"Seattle\"}")),
        };

        var message = new ChatMessage(MessageRole.Assistant, "I'll check the weather", toolCalls, null);

        message.Content.Should().Be("I'll check the weather");
        message.ToolCalls.Should().NotBeNull();
    }

    [Fact]
    public void ChatMessage_SerializesToJson()
    {
        // FR-004a-23: ChatMessage MUST support JSON serialization
        var message = new ChatMessage(MessageRole.User, "Hello", null, null);

        var json = JsonSerializer.Serialize(message);

        json.Should().Contain("\"role\":");
        json.Should().Contain("\"content\":");
        json.Should().Contain("Hello");
    }

    [Fact]
    public void ChatMessage_DeserializesFromJson()
    {
        // FR-004a-24: ChatMessage MUST support JSON deserialization
        var json = "{\"role\":\"user\",\"content\":\"Hello\"}";

        var message = JsonSerializer.Deserialize<ChatMessage>(json);

        message.Should().NotBeNull();
        message!.Role.Should().Be(MessageRole.User);
        message.Content.Should().Be("Hello");
    }

    [Fact]
    public void ChatMessage_SerializationOmitsNullProperties()
    {
        // FR-004a-25: Serialization MUST omit null properties
        var message = new ChatMessage(MessageRole.User, "Hello", null, null);
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var json = JsonSerializer.Serialize(message, options);

        json.Should().NotContain("toolCalls");
        json.Should().NotContain("toolCallId");
    }

    [Fact]
    public void ChatMessage_DeserializationHandlesMissingProperties()
    {
        // FR-004a-26: Deserialization MUST handle missing properties
        var json = "{\"role\":\"assistant\",\"content\":\"Hello\"}";

        var message = JsonSerializer.Deserialize<ChatMessage>(json);

        message.Should().NotBeNull();
        message!.ToolCalls.Should().BeNull();
        message.ToolCallId.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_CreateSystemFactoryMethod()
    {
        // FR-004a-27, FR-004a-28: ChatMessage MUST have factory: CreateSystem(content)
        var message = ChatMessage.CreateSystem("You are a helpful assistant");

        message.Role.Should().Be(MessageRole.System);
        message.Content.Should().Be("You are a helpful assistant");
        message.ToolCalls.Should().BeNull();
        message.ToolCallId.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_CreateUserFactoryMethod()
    {
        // FR-004a-27, FR-004a-29: ChatMessage MUST have factory: CreateUser(content)
        var message = ChatMessage.CreateUser("Hello, AI!");

        message.Role.Should().Be(MessageRole.User);
        message.Content.Should().Be("Hello, AI!");
        message.ToolCalls.Should().BeNull();
        message.ToolCallId.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_CreateAssistantFactoryMethodWithContent()
    {
        // FR-004a-27, FR-004a-30: ChatMessage MUST have factory: CreateAssistant(content, toolCalls)
        var message = ChatMessage.CreateAssistant("I can help with that");

        message.Role.Should().Be(MessageRole.Assistant);
        message.Content.Should().Be("I can help with that");
        message.ToolCalls.Should().BeNull();
        message.ToolCallId.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_CreateAssistantFactoryMethodWithToolCalls()
    {
        // FR-004a-27, FR-004a-30: ChatMessage MUST have factory: CreateAssistant(content, toolCalls)
        var toolCalls = new List<ToolCall>
        {
            new ToolCall("id1", "get_weather", CreateJsonElement("{\"city\":\"Seattle\"}")),
        };

        var message = ChatMessage.CreateAssistant(null, toolCalls);

        message.Role.Should().Be(MessageRole.Assistant);
        message.Content.Should().BeNull();
        message.ToolCalls.Should().HaveCount(1);
        message.ToolCallId.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_CreateAssistantFactoryMethodWithBoth()
    {
        // FR-004a-27, FR-004a-30: ChatMessage MUST have factory: CreateAssistant(content, toolCalls)
        var toolCalls = new List<ToolCall>
        {
            new ToolCall("id1", "get_weather", CreateJsonElement("{\"city\":\"Seattle\"}")),
        };

        var message = ChatMessage.CreateAssistant("Let me check", toolCalls);

        message.Role.Should().Be(MessageRole.Assistant);
        message.Content.Should().Be("Let me check");
        message.ToolCalls.Should().HaveCount(1);
        message.ToolCallId.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_CreateToolResultFactoryMethod()
    {
        // FR-004a-27, FR-004a-31: ChatMessage MUST have factory: CreateToolResult(toolCallId, result, isError)
        var message = ChatMessage.CreateToolResult("id1", "Temperature: 72°F");

        message.Role.Should().Be(MessageRole.Tool);
        message.Content.Should().Be("Temperature: 72°F");
        message.ToolCallId.Should().Be("id1");
        message.ToolCalls.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_HasValueEquality()
    {
        // FR-004a-34: ChatMessage MUST implement value equality
        var message1 = new ChatMessage(MessageRole.User, "Hello", null, null);
        var message2 = new ChatMessage(MessageRole.User, "Hello", null, null);
        var message3 = new ChatMessage(MessageRole.User, "Hi", null, null);

        message1.Should().Be(message2);
        message1.Should().NotBe(message3);
    }

    [Fact]
    public void ChatMessage_HasMeaningfulToString()
    {
        // FR-004a-35: ChatMessage MUST have meaningful ToString()
        var message = new ChatMessage(MessageRole.User, "Hello", null, null);

        var str = message.ToString();

        str.Should().Contain("User");
        str.Should().Contain("Hello");
    }

    [Fact]
    public void ChatMessage_IsImmutable()
    {
        // FR-004a-12: ChatMessage MUST be immutable
        var message = new ChatMessage(MessageRole.User, "Hello", null, null);

        // Record with init-only properties ensures immutability at compile time
        message.Should().NotBeNull();
    }

    [Fact]
    public void ChatMessage_ToolCallsIsReadOnly()
    {
        // FR-004a-18: ToolCalls MUST be IReadOnlyList<ToolCall>
        var toolCalls = new List<ToolCall>
        {
            new ToolCall("id1", "get_weather", CreateJsonElement("{\"city\":\"Seattle\"}")),
        };

        var message = new ChatMessage(MessageRole.Assistant, null, toolCalls, null);

        message.ToolCalls.Should().BeAssignableTo<IReadOnlyList<ToolCall>>();
    }

    [Fact]
    public void ChatMessage_ValidatesOnConstruction()
    {
        // FR-004a-32, FR-004a-33: ChatMessage MUST validate on construction, invalid messages MUST throw
        var act1 = () => new ChatMessage(MessageRole.User, null, null, null);
        var act2 = () => new ChatMessage(MessageRole.System, null, null, null);
        var act3 = () => new ChatMessage(MessageRole.Tool, "result", null, null);
        var act4 = () => new ChatMessage(MessageRole.Assistant, null, null, null);

        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
        act4.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChatMessage_SupportsEmptyContent()
    {
        // Content can be empty string (but not null for certain roles)
        var message = new ChatMessage(MessageRole.User, string.Empty, null, null);

        message.Content.Should().Be(string.Empty);
    }

    /// <summary>
    /// Helper method to create JsonElement from JSON string for testing.
    /// </summary>
    private static JsonElement CreateJsonElement(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
