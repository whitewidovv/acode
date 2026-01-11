namespace Acode.Application.Tests.Inference;

using System;
using System.Text.Json;
using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for ChatRequest record following TDD (RED phase).
/// FR-004-66 to FR-004-72.
/// </summary>
public class ChatRequestTests
{
    [Fact]
    public void ChatRequest_HasMessagesProperty()
    {
        // FR-004-66: ChatRequest MUST include Messages property (array of ChatMessage)
        var messages = new[] { ChatMessage.CreateUser("Hello") };
        var request = new ChatRequest(messages);

        request.Messages.Should().BeEquivalentTo(messages);
    }

    [Fact]
    public void ChatRequest_ValidatesNonEmptyMessages()
    {
        // FR-004-67: Messages array must be non-empty
        var act = () => new ChatRequest(Array.Empty<ChatMessage>());

        act.Should().Throw<ArgumentException>().WithParameterName("Messages");
    }

    [Fact]
    public void ChatRequest_HasModelParametersProperty()
    {
        // FR-004-68: ChatRequest MUST include ModelParameters property
        var messages = new[] { ChatMessage.CreateUser("Hello") };
        var parameters = new ModelParameters("llama2:7b", temperature: 0.5);
        var request = new ChatRequest(messages, parameters);

        request.ModelParameters.Should().Be(parameters);
    }

    [Fact]
    public void ChatRequest_ModelParametersDefaultsToNull()
    {
        // FR-004-69: ModelParameters is nullable (use provider defaults if null)
        var messages = new[] { ChatMessage.CreateUser("Hello") };
        var request = new ChatRequest(messages);

        request.ModelParameters.Should().BeNull();
    }

    [Fact]
    public void ChatRequest_HasToolsProperty()
    {
        // FR-004-70: ChatRequest MUST include Tools property (nullable array)
        var messages = new[] { ChatMessage.CreateUser("Run the tests") };
        var parameters = JsonSerializer.Deserialize<JsonElement>("{\"type\":\"object\"}");
        var tool = new ToolDefinition("run_tests", "Runs the test suite", parameters, Strict: false);
        var request = new ChatRequest(messages, tools: new[] { tool });

        request.Tools.Should().BeEquivalentTo(new[] { tool });
    }

    [Fact]
    public void ChatRequest_ToolsDefaultsToNull()
    {
        // FR-004-70: Tools is nullable (no tool use if null)
        var messages = new[] { ChatMessage.CreateUser("Hello") };
        var request = new ChatRequest(messages);

        request.Tools.Should().BeNull();
    }

    [Fact]
    public void ChatRequest_HasStreamProperty()
    {
        // FR-004-71: ChatRequest MUST include Stream property (bool)
        var messages = new[] { ChatMessage.CreateUser("Hello") };
        var streamingRequest = new ChatRequest(messages, stream: true);
        var nonStreamingRequest = new ChatRequest(messages, stream: false);

        streamingRequest.Stream.Should().BeTrue();
        nonStreamingRequest.Stream.Should().BeFalse();
    }

    [Fact]
    public void ChatRequest_StreamDefaultsToFalse()
    {
        // FR-004-72: Stream defaults to false (non-streaming by default)
        var messages = new[] { ChatMessage.CreateUser("Hello") };
        var request = new ChatRequest(messages);

        request.Stream.Should().BeFalse();
    }

    [Fact]
    public void ChatRequest_SerializesToJson()
    {
        // ChatRequest should serialize to JSON
        var messages = new[] { ChatMessage.CreateUser("Hello") };
        var parameters = new ModelParameters("llama2", temperature: 0.8);
        var request = new ChatRequest(messages, parameters, stream: true);

        var json = JsonSerializer.Serialize(request);

        json.Should().Contain("\"messages\":");
        json.Should().Contain("\"modelParameters\":");
        json.Should().Contain("\"stream\":");
    }

    [Fact]
    public void ChatRequest_ImplementsValueEquality()
    {
        // Records have value equality
        var messages = new[] { ChatMessage.CreateUser("Hello") };
        var parameters = new ModelParameters("llama2");
        var request1 = new ChatRequest(messages, parameters, stream: true);
        var request2 = new ChatRequest(messages, parameters, stream: true);

        request1.Should().Be(request2);
    }

    [Fact]
    public void ChatRequest_ValidatesNullMessages()
    {
        // Messages cannot be null
        var act = () => new ChatRequest(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("Messages");
    }

    [Fact]
    public void ChatRequest_AcceptsMultipleMessages()
    {
        // ChatRequest can have multiple messages (conversation history)
        var messages = new[]
        {
            ChatMessage.CreateUser("What is 2+2?"),
            ChatMessage.CreateAssistant("4"),
            ChatMessage.CreateUser("What about 3+3?"),
        };
        var request = new ChatRequest(messages);

        request.Messages.Should().HaveCount(3);
        request.Messages.Should().BeEquivalentTo(messages, options => options.WithStrictOrdering());
    }
}
