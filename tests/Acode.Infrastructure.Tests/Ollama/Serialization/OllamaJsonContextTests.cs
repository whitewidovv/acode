namespace Acode.Infrastructure.Tests.Ollama.Serialization;

using System.Text.Json;
using Acode.Infrastructure.Ollama.Models;
using Acode.Infrastructure.Ollama.Serialization;
using FluentAssertions;

/// <summary>
/// Tests for OllamaJsonContext source generator.
/// Verifies FR-009 (must use source generators) and NFR-008 (no reflection).
/// </summary>
public sealed class OllamaJsonContextTests
{
    [Fact]
    public void OllamaJsonContext_Should_SerializeRequest()
    {
        // Arrange
        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[]
            {
                new OllamaMessage(role: "user", content: "Hello")
            },
            stream: false);

        // Act - use source generator context
        var json = JsonSerializer.Serialize(request, OllamaJsonContext.Default.OllamaRequest);

        // Assert
        json.Should().Contain("\"model\":\"llama3.2:8b\"");
        json.Should().Contain("\"messages\"");
        json.Should().Contain("\"stream\":false");
    }

    [Fact]
    public void OllamaJsonContext_Should_DeserializeResponse()
    {
        // Arrange
        var json = """
        {
            "model": "llama3.2:8b",
            "created_at": "2024-01-01T00:00:00Z",
            "message": {
                "role": "assistant",
                "content": "Hello!"
            },
            "done": true
        }
        """;

        // Act - use source generator context
        var response = JsonSerializer.Deserialize(json, OllamaJsonContext.Default.OllamaResponse);

        // Assert
        response.Should().NotBeNull();
        response!.Model.Should().Be("llama3.2:8b");
        response.Message.Content.Should().Be("Hello!");
        response.Done.Should().BeTrue();
    }

    [Fact]
    public void OllamaJsonContext_Should_UseSnakeCaseNaming()
    {
        // Arrange
        var response = new OllamaResponse(
            model: "test",
            createdAt: "2024-01-01T00:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "test"),
            done: true,
            doneReason: "stop",
            totalDuration: 1000000L);

        // Act
        var json = JsonSerializer.Serialize(response, OllamaJsonContext.Default.OllamaResponse);

        // Assert - verify snake_case naming
        json.Should().Contain("\"created_at\"");
        json.Should().Contain("\"done_reason\"");
        json.Should().Contain("\"total_duration\"");
        json.Should().NotContain("\"CreatedAt\"");
        json.Should().NotContain("\"DoneReason\"");
        json.Should().NotContain("\"TotalDuration\"");
    }

    [Fact]
    public void OllamaJsonContext_Should_OmitNullValues()
    {
        // Arrange
        var request = new OllamaRequest(
            model: "test",
            messages: new[] { new OllamaMessage(role: "user", content: "test") },
            stream: false,
            tools: null,
            format: null,
            options: null,
            keepAlive: null);

        // Act
        var json = JsonSerializer.Serialize(request, OllamaJsonContext.Default.OllamaRequest);

        // Assert - verify null values are omitted
        json.Should().NotContain("\"tools\"");
        json.Should().NotContain("\"format\"");
        json.Should().NotContain("\"options\"");
        json.Should().NotContain("\"keep_alive\"");
    }

    [Fact]
    public void OllamaJsonContext_Should_SerializeStreamChunk()
    {
        // Arrange
        var chunk = new OllamaStreamChunk(
            model: "test",
            message: new OllamaMessage(role: "assistant", content: "chunk"),
            done: false);

        // Act
        var json = JsonSerializer.Serialize(chunk, OllamaJsonContext.Default.OllamaStreamChunk);

        // Assert
        json.Should().Contain("\"model\":\"test\"");
        json.Should().Contain("\"message\"");
        json.Should().Contain("\"done\":false");
    }

    [Fact]
    public void OllamaJsonContext_Should_SerializeComplexRequest()
    {
        // Arrange
        // NOTE: Tool serialization with parameters requires OllamaFunction.Parameters
        // to be JsonElement instead of object. This will be fixed in Gap #7.
        // For now, test without tools to verify other complex serialization.
        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[]
            {
                new OllamaMessage(role: "system", content: "You are helpful."),
                new OllamaMessage(role: "user", content: "Hello")
            },
            stream: false,
            tools: null, // Tool serialization tested separately once model fixed
            format: "json",
            options: new OllamaOptions(
                temperature: 0.7,
                topP: 0.9,
                seed: 42),
            keepAlive: "5m");

        // Act
        var json = JsonSerializer.Serialize(request, OllamaJsonContext.Default.OllamaRequest);

        // Assert
        json.Should().Contain("\"model\":\"llama3.2:8b\"");
        json.Should().Contain("\"format\":\"json\"");
        json.Should().Contain("\"options\"");
        json.Should().Contain("\"temperature\":0.7");
        json.Should().Contain("\"keep_alive\":\"5m\"");
        json.Should().Contain("\"messages\"");
        json.Should().Contain("\"system\"");
        json.Should().Contain("\"user\"");
    }

    [Fact]
    public void OllamaJsonContext_Should_RoundtripRequest()
    {
        // Arrange
        var originalRequest = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[]
            {
                new OllamaMessage(role: "user", content: "Test message")
            },
            stream: true);

        // Act - serialize then deserialize
        var json = JsonSerializer.Serialize(originalRequest, OllamaJsonContext.Default.OllamaRequest);
        var roundtrippedRequest = JsonSerializer.Deserialize(json, OllamaJsonContext.Default.OllamaRequest);

        // Assert
        roundtrippedRequest.Should().NotBeNull();
        roundtrippedRequest!.Model.Should().Be(originalRequest.Model);
        roundtrippedRequest.Stream.Should().Be(originalRequest.Stream);
        roundtrippedRequest.Messages.Should().HaveCount(1);
        roundtrippedRequest.Messages[0].Role.Should().Be("user");
        roundtrippedRequest.Messages[0].Content.Should().Be("Test message");
    }
}
