namespace Acode.Domain.Tests.Models.Inference;

using System;
using System.Text.Json;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for ChatResponse record following TDD (RED phase).
/// FR-004b-001 to FR-004b-018.
/// </summary>
public class ChatResponseTests
{
    [Fact]
    public void ChatResponse_HasIdProperty()
    {
        // FR-004b-002: ChatResponse MUST include an Id property as a unique response identifier
        var message = ChatMessage.CreateAssistant("Hello");
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Stop,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2");

        response.Id.Should().Be("resp-123");
    }

    [Fact]
    public void ChatResponse_HasMessageProperty()
    {
        // FR-004b-003: ChatResponse MUST include a Message property of type ChatMessage
        var message = ChatMessage.CreateAssistant("Hello");
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Stop,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2");

        response.Message.Should().Be(message);
    }

    [Fact]
    public void ChatResponse_HasFinishReasonProperty()
    {
        // FR-004b-004: ChatResponse MUST include a FinishReason property
        var message = ChatMessage.CreateAssistant("Hello");
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Length,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2");

        response.FinishReason.Should().Be(FinishReason.Length);
    }

    [Fact]
    public void ChatResponse_HasUsageProperty()
    {
        // FR-004b-005: ChatResponse MUST include a Usage property of type UsageInfo
        var message = ChatMessage.CreateAssistant("Hello");
        var usage = new UsageInfo(100, 50);
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Stop,
            usage,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2");

        response.Usage.Should().Be(usage);
    }

    [Fact]
    public void ChatResponse_HasMetadataProperty()
    {
        // FR-004b-006: ChatResponse MUST include a Metadata property of type ResponseMetadata
        var message = ChatMessage.CreateAssistant("Hello");
        var metadata = new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1));
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Stop,
            UsageInfo.Empty,
            metadata,
            DateTimeOffset.UtcNow,
            "llama2");

        response.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void ChatResponse_HasCreatedProperty()
    {
        // FR-004b-007: ChatResponse MUST include a Created timestamp (DateTimeOffset, UTC)
        var message = ChatMessage.CreateAssistant("Hello");
        var created = DateTimeOffset.UtcNow;
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Stop,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            created,
            "llama2");

        response.Created.Should().Be(created);
    }

    [Fact]
    public void ChatResponse_HasModelProperty()
    {
        // FR-004b-008: ChatResponse MUST include a Model property identifying the model
        var message = ChatMessage.CreateAssistant("Hello");
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Stop,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2:7b", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2:7b");

        response.Model.Should().Be("llama2:7b");
    }

    [Fact]
    public void ChatResponse_HasRefusalProperty()
    {
        // FR-004b-009: ChatResponse MUST include an optional Refusal property
        var message = ChatMessage.CreateAssistant("I cannot help with that");
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Stop,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2",
            "Content policy violation");

        response.Refusal.Should().Be("Content policy violation");
    }

    [Fact]
    public void ChatResponse_IsCompleteWhenFinishReasonIsStop()
    {
        // FR-004b-010: ChatResponse MUST provide a bool IsComplete property (FinishReason == Stop)
        var message = ChatMessage.CreateAssistant("Hello");
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Stop,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2");

        response.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void ChatResponse_IsNotCompleteWhenFinishReasonIsNotStop()
    {
        // FR-004b-010: IsComplete should be false for non-Stop reasons
        var message = ChatMessage.CreateAssistant("Hello");
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Length,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2");

        response.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void ChatResponse_IsTruncatedWhenFinishReasonIsLength()
    {
        // FR-004b-011: ChatResponse MUST provide a bool IsTruncated property (FinishReason == Length)
        var message = ChatMessage.CreateAssistant("Hello");
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Length,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2");

        response.IsTruncated.Should().BeTrue();
    }

    [Fact]
    public void ChatResponse_HasToolCallsWhenMessageHasToolCalls()
    {
        // FR-004b-012: ChatResponse MUST provide a bool HasToolCalls property
        var toolCalls = new[] { new ToolCall("id1", "get_weather", "{\"city\":\"Seattle\"}") };
        var message = ChatMessage.CreateAssistant(null, toolCalls);
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.ToolCalls,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2");

        response.HasToolCalls.Should().BeTrue();
    }

    [Fact]
    public void ChatResponse_ImplementsValueEquality()
    {
        // FR-004b-013: ChatResponse MUST implement value equality comparing Id
        var message = ChatMessage.CreateAssistant("Hello");
        var created = DateTimeOffset.UtcNow;
        var response1 = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Stop,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            created,
            "llama2");
        var response2 = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Stop,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            created,
            "llama2");

        response1.Should().Be(response2);
    }

    [Fact]
    public void ChatResponse_SerializesToJson()
    {
        // FR-004b-014: ChatResponse MUST be serializable to JSON
        var message = ChatMessage.CreateAssistant("Hello");
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Stop,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2");

        var json = JsonSerializer.Serialize(response);

        json.Should().Contain("\"id\":");
        json.Should().Contain("\"message\":");
    }

    [Fact]
    public void ChatResponse_ThrowsOnEmptyId()
    {
        // FR-004b-016: ChatResponse MUST validate that Id is non-empty
        var message = ChatMessage.CreateAssistant("Hello");
        var act = () => new ChatResponse(
            string.Empty,
            message,
            FinishReason.Stop,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2");

        act.Should().Throw<ArgumentException>().WithParameterName("Id");
    }

    [Fact]
    public void ChatResponse_ThrowsOnNullMessage()
    {
        // FR-004b-017: ChatResponse MUST validate that Message is not null
        var act = () => new ChatResponse(
            "resp-123",
            null!,
            FinishReason.Stop,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2");

        act.Should().Throw<ArgumentException>().WithParameterName("Message");
    }

    [Fact]
    public void ChatResponse_AllowsNullRefusal()
    {
        // FR-004b-015: ChatResponse MUST support null Refusal when request was not declined
        var message = ChatMessage.CreateAssistant("Hello");
        var response = new ChatResponse(
            "resp-123",
            message,
            FinishReason.Stop,
            UsageInfo.Empty,
            new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1)),
            DateTimeOffset.UtcNow,
            "llama2");

        response.Refusal.Should().BeNull();
    }
}
