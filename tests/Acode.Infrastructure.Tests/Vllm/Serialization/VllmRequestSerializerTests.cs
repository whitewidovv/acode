using Acode.Infrastructure.Vllm.Models;
using Acode.Infrastructure.Vllm.Serialization;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Vllm.Serialization;

public class VllmRequestSerializerTests
{
    [Fact]
    public void Serialize_Should_UseSnakeCasePropertyNames()
    {
        // Arrange
        var request = new VllmRequest
        {
            Model = "meta-llama/Llama-3.2-8B-Instruct",
            Messages = new List<VllmMessage>
            {
                new() { Role = "user", Content = "Hello" }
            },
            MaxTokens = 2048,
            Temperature = 0.7,
            TopP = 0.9
        };

        // Act
        var json = VllmRequestSerializer.Serialize(request);

        // Assert
        json.Should().Contain("\"max_tokens\":");
        json.Should().Contain("\"top_p\":");
        json.Should().NotContain("\"MaxTokens\":");
        json.Should().NotContain("\"TopP\":");
    }

    [Fact]
    public void Serialize_Should_OmitNullOptionalFields()
    {
        // Arrange
        var request = new VllmRequest
        {
            Model = "test-model",
            Messages = new List<VllmMessage>
            {
                new() { Role = "user", Content = "Hi" }
            }
        };

        // Act
        var json = VllmRequestSerializer.Serialize(request);

        // Assert
        json.Should().NotContain("\"tools\":");
        json.Should().NotContain("\"response_format\":");
        json.Should().NotContain("\"stop\":");
    }

    [Fact]
    public void Deserialize_Should_ParseValidResponse()
    {
        // Arrange
        var json = """
        {
            "id": "chatcmpl-123",
            "object": "chat.completion",
            "created": 1677652288,
            "model": "meta-llama/Llama-3.2-8B-Instruct",
            "choices": [
                {
                    "index": 0,
                    "message": {
                        "role": "assistant",
                        "content": "Hello! How can I help you?"
                    },
                    "finish_reason": "stop"
                }
            ],
            "usage": {
                "prompt_tokens": 9,
                "completion_tokens": 12,
                "total_tokens": 21
            }
        }
        """;

        // Act
        var response = VllmRequestSerializer.DeserializeResponse(json);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("chatcmpl-123");
        response.Model.Should().Be("meta-llama/Llama-3.2-8B-Instruct");
        response.Choices.Should().HaveCount(1);
        response.Choices[0].Message.Content.Should().Be("Hello! How can I help you?");
        response.Usage.Should().NotBeNull();
        response.Usage!.TotalTokens.Should().Be(21);
    }

    [Fact]
    public void DeserializeStreamChunk_Should_ParseValidChunk()
    {
        // Arrange
        var json = """
        {
            "id": "chatcmpl-123",
            "object": "chat.completion.chunk",
            "created": 1677652288,
            "model": "test-model",
            "choices": [
                {
                    "index": 0,
                    "delta": {
                        "content": "Hello"
                    },
                    "finish_reason": null
                }
            ]
        }
        """;

        // Act
        var chunk = VllmRequestSerializer.DeserializeStreamChunk(json);

        // Assert
        chunk.Should().NotBeNull();
        chunk.Id.Should().Be("chatcmpl-123");
        chunk.Object.Should().Be("chat.completion.chunk");
        chunk.Choices.Should().HaveCount(1);
        chunk.Choices[0].Delta.Content.Should().Be("Hello");
    }

    [Fact]
    public void DeserializeStreamChunk_Should_HandleToolCalls()
    {
        // Arrange
        var json = """
        {
            "id": "chatcmpl-456",
            "object": "chat.completion.chunk",
            "created": 1677652288,
            "model": "test-model",
            "choices": [
                {
                    "index": 0,
                    "delta": {
                        "tool_calls": [
                            {
                                "id": "call_abc123",
                                "type": "function",
                                "function": {
                                    "name": "get_weather",
                                    "arguments": "{\"location\":\"Boston\"}"
                                }
                            }
                        ]
                    },
                    "finish_reason": "tool_calls"
                }
            ]
        }
        """;

        // Act
        var chunk = VllmRequestSerializer.DeserializeStreamChunk(json);

        // Assert
        chunk.Choices[0].Delta.ToolCalls.Should().NotBeNull();
        chunk.Choices[0].Delta.ToolCalls.Should().HaveCount(1);
        chunk.Choices[0].Delta.ToolCalls![0].Function.Name.Should().Be("get_weather");
        chunk.Choices[0].FinishReason.Should().Be("tool_calls");
    }

    [Fact]
    public void Serialize_Should_EscapeSpecialCharacters()
    {
        // Arrange
        var request = new VllmRequest
        {
            Model = "test-model",
            Messages = new List<VllmMessage>
            {
                new() { Role = "user", Content = "Hello \"world\"\nNew line\tTab" }
            }
        };

        // Act
        var json = VllmRequestSerializer.Serialize(request);

        // Assert - System.Text.Json escapes quotes as \u0022
        json.Should().Contain("\\u0022world\\u0022");
        json.Should().Contain("\\n");
        json.Should().Contain("\\t");
    }
}
