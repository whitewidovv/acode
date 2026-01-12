namespace Acode.Integration.Tests.Models.Messages;

using System.Collections.Generic;
using System.Text.Json;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Integration tests for JSON serialization compatibility with LLM providers.
/// Verifies that message types serialize/deserialize correctly for Ollama and vLLM APIs.
/// </summary>
public sealed class SerializationCompatibilityTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public void Should_Match_Ollama_Format()
    {
        // Arrange - Ollama expects camelCase with specific structure
        var message = ChatMessage.CreateUser("Hello, world!");

        // Act
        var json = JsonSerializer.Serialize(message, Options);
        var deserialized = JsonSerializer.Deserialize<ChatMessage>(json, Options);

        // Assert
        json.Should().Contain("\"role\":\"user\"");
        json.Should().Contain("\"content\":\"Hello, world!\"");
        deserialized.Should().NotBeNull();
        deserialized!.Role.Should().Be(MessageRole.User);
        deserialized.Content.Should().Be("Hello, world!");
    }

    [Fact]
    public void Should_Match_Ollama_ToolCall_Format()
    {
        // Arrange - tool call with proper structure
        var args = CreateJsonElement(JsonSerializer.Serialize(new { path = "/test.cs" }, Options));
        var toolCall = new ToolCall(
            Id: "call_123",
            Name: "read_file",
            Arguments: args);

        var message = ChatMessage.CreateAssistant(null, new[] { toolCall });

        // Act
        var json = JsonSerializer.Serialize(message, Options);
        var deserialized = JsonSerializer.Deserialize<ChatMessage>(json, Options);

        // Assert
        json.Should().Contain("\"role\":\"assistant\"");
        json.Should().Contain("\"toolCalls\"");
        json.Should().Contain("\"id\":\"call_123\"");
        json.Should().Contain("\"name\":\"read_file\"");
        deserialized.Should().NotBeNull();
        deserialized!.ToolCalls.Should().HaveCount(1);
        deserialized.ToolCalls![0].Id.Should().Be("call_123");
    }

    [Fact]
    public void Should_Match_vLLM_Format()
    {
        // Arrange - vLLM uses same structure as Ollama (OpenAI compatible)
        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystem("You are helpful."),
            ChatMessage.CreateUser("What is 2+2?"),
            ChatMessage.CreateAssistant("4"),
        };

        // Act
        var json = JsonSerializer.Serialize(messages, Options);
        var deserialized = JsonSerializer.Deserialize<List<ChatMessage>>(json, Options);

        // Assert
        json.Should().Contain("\"role\":\"system\"");
        json.Should().Contain("\"role\":\"user\"");
        json.Should().Contain("\"role\":\"assistant\"");
        deserialized.Should().HaveCount(3);
        deserialized![0].Role.Should().Be(MessageRole.System);
        deserialized[1].Role.Should().Be(MessageRole.User);
        deserialized[2].Role.Should().Be(MessageRole.Assistant);
    }

    [Fact]
    public void Should_Handle_Provider_Extensions()
    {
        // Arrange - message with all optional fields
        var args = CreateJsonElement(JsonSerializer.Serialize(new { param = "value" }, Options));
        var toolCall = new ToolCall(
            Id: "call_abc",
            Name: "my_tool",
            Arguments: args);

        var message = ChatMessage.CreateAssistant("Let me help", new[] { toolCall });

        // Act
        var json = JsonSerializer.Serialize(message, Options);
        var parsed = JsonDocument.Parse(json);

        // Assert - all fields present
        parsed.RootElement.TryGetProperty("role", out _).Should().BeTrue();
        parsed.RootElement.TryGetProperty("content", out _).Should().BeTrue();
        parsed.RootElement.TryGetProperty("toolCalls", out _).Should().BeTrue();

        // Null fields should be omitted (not present)
        parsed.RootElement.TryGetProperty("toolCallId", out _).Should().BeFalse();
    }

    [Fact]
    public void Should_Roundtrip_All_Types()
    {
        // Arrange - create instances of all message types
        var systemMsg = ChatMessage.CreateSystem("System prompt");
        var userMsg = ChatMessage.CreateUser("User input");
        var toolCall = new ToolCall("call_1", "tool_name", CreateJsonElement("{}"));
        var assistantMsg = ChatMessage.CreateAssistant("Response", new[] { toolCall });
        var toolMsg = ChatMessage.CreateToolResult("call_1", "Result");

        var messages = new[] { systemMsg, userMsg, assistantMsg, toolMsg };

        // Act
        var json = JsonSerializer.Serialize(messages, Options);
        var roundtrip = JsonSerializer.Deserialize<ChatMessage[]>(json, Options);

        // Assert
        roundtrip.Should().NotBeNull();
        roundtrip!.Length.Should().Be(4);
        roundtrip[0].Role.Should().Be(MessageRole.System);
        roundtrip[1].Role.Should().Be(MessageRole.User);
        roundtrip[2].Role.Should().Be(MessageRole.Assistant);
        roundtrip[2].ToolCalls.Should().HaveCount(1);
        roundtrip[3].Role.Should().Be(MessageRole.Tool);
        roundtrip[3].ToolCallId.Should().Be("call_1");
    }

    [Fact]
    public void ToolDefinition_Should_Serialize_For_OpenAI_Format()
    {
        // Arrange - tool definition matching OpenAI/Ollama format
        var parameters = JsonDocument.Parse(@"{
            ""type"": ""object"",
            ""properties"": {
                ""path"": {""type"": ""string""},
                ""encoding"": {""type"": ""string""}
            },
            ""required"": [""path""]
        }").RootElement;

        var toolDef = new ToolDefinition("read_file", "Reads a file", parameters);

        // Act
        var json = JsonSerializer.Serialize(toolDef, Options);
        var deserialized = JsonSerializer.Deserialize<ToolDefinition>(json, Options);

        // Assert
        json.Should().Contain("\"name\":\"read_file\"");
        json.Should().Contain("\"description\":\"Reads a file\"");
        json.Should().Contain("\"parameters\"");
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("read_file");
        deserialized.Parameters.GetProperty("type").GetString().Should().Be("object");
    }

    [Fact]
    public void ToolCallDelta_Should_Serialize_For_Streaming()
    {
        // Arrange - streaming deltas
        var delta1 = new ToolCallDelta(Index: 0, Id: "call_1", Name: "search");
        var delta2 = new ToolCallDelta(Index: 0, ArgumentsDelta: "{\"query\":\"");

        // Act
        var json1 = JsonSerializer.Serialize(delta1, Options);
        var json2 = JsonSerializer.Serialize(delta2, Options);

        // Assert
        json1.Should().Contain("\"index\":0");
        json1.Should().Contain("\"id\":\"call_1\"");
        json2.Should().Contain("\"argumentsDelta\"");
        json2.Should().NotContain("\"id\""); // Null properties omitted
    }

    [Fact]
    public void ToolResult_Should_Serialize_With_IsError()
    {
        // Arrange
        var successResult = ToolResult.Success("call_1", "Success output");
        var errorResult = ToolResult.Error("call_2", "Error occurred");

        // Act
        var successJson = JsonSerializer.Serialize(successResult, Options);
        var errorJson = JsonSerializer.Serialize(errorResult, Options);

        // Assert
        successJson.Should().Contain("\"isError\":false");
        errorJson.Should().Contain("\"isError\":true");
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
