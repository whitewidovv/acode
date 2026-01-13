using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Ollama.Mapping;
using Acode.Infrastructure.Ollama.Models;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Ollama.Mapping;

/// <summary>
/// Tests for OllamaResponseMapper.
/// FR-052 to FR-061 from Task 005.a.
/// </summary>
#pragma warning disable CA2007 // ConfigureAwait not needed in test methods
public sealed class OllamaResponseMapperTests
{
    [Fact]
    public void Map_Should_Convert_OllamaResponse_To_ChatResponse()
    {
        // FR-052: ResponseParser MUST convert OllamaResponse to ChatResponse
        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello!"),
            done: true,
            doneReason: "stop",
            totalDuration: 1500000000,
            promptEvalCount: 25,
            evalCount: 42);

        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        chatResponse.Should().NotBeNull();
        chatResponse.Message.Should().NotBeNull();
    }

    [Fact]
    public void Map_Should_Map_Message_Content()
    {
        // FR-053: ResponseParser MUST map message content to ChatMessage
        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "The answer is 42."),
            done: true);

        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        chatResponse.Message.Content.Should().Be("The answer is 42.");
        chatResponse.Message.Role.Should().Be(MessageRole.Assistant);
    }

    [Fact]
    public void Map_Should_Map_DoneReason_Stop_To_FinishReason_Stop()
    {
        // FR-055: ResponseParser MUST map "stop" to FinishReason.Stop
        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Done"),
            done: true,
            doneReason: "stop");

        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        chatResponse.FinishReason.Should().Be(FinishReason.Stop);
    }

    [Fact]
    public void Map_Should_Map_DoneReason_Length_To_FinishReason_Length()
    {
        // FR-056: ResponseParser MUST map "length" to FinishReason.Length
        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Truncated..."),
            done: true,
            doneReason: "length");

        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        chatResponse.FinishReason.Should().Be(FinishReason.Length);
    }

    [Fact]
    public void Map_Should_Map_DoneReason_ToolCalls_To_FinishReason_ToolCalls()
    {
        // FR-057: ResponseParser MUST map "tool_calls" to FinishReason.ToolCalls
        var toolCalls = new[]
        {
            new OllamaToolCall(
                id: "call_123",
                function: new OllamaFunction(
                    name: "read_file",
                    description: "Reads a file",
                    parameters: new { path = "test.txt" })),
        };

        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: null, toolCalls: toolCalls),
            done: true,
            doneReason: "tool_calls");

        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        chatResponse.FinishReason.Should().Be(FinishReason.ToolCalls);
    }

    [Fact]
    public void Map_Should_Calculate_UsageInfo_From_Token_Counts()
    {
        // FR-058: ResponseParser MUST calculate UsageInfo from token counts
        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true,
            promptEvalCount: 30,
            evalCount: 50);

        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        chatResponse.Usage.PromptTokens.Should().Be(30);
        chatResponse.Usage.CompletionTokens.Should().Be(50);
        chatResponse.Usage.TotalTokens.Should().Be(80);
    }

    [Fact]
    public void Map_Should_Calculate_ResponseMetadata_From_Timing()
    {
        // FR-059: ResponseParser MUST calculate ResponseMetadata from timing
        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true,
            totalDuration: 2500000000); // 2.5 seconds in nanoseconds

        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        chatResponse.Metadata.Should().NotBeNull();
        chatResponse.Metadata.ModelId.Should().Be("llama3.2:8b");
        chatResponse.Metadata.RequestDuration.TotalSeconds.Should().BeApproximately(2.5, 0.01);
    }

    [Fact]
    public void Map_Should_Handle_Missing_Optional_Fields_Gracefully()
    {
        // FR-061: ResponseParser MUST handle missing optional fields gracefully
        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true);

        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        chatResponse.Should().NotBeNull();
        chatResponse.Usage.PromptTokens.Should().Be(0);
        chatResponse.Usage.CompletionTokens.Should().Be(0);
    }

    [Fact]
    public void Map_Should_Set_Response_Id()
    {
        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true);

        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        chatResponse.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Map_Should_Set_Model_Property()
    {
        var ollamaResponse = new OllamaResponse(
            model: "codellama:13b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Code here"),
            done: true);

        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        chatResponse.Model.Should().Be("codellama:13b");
    }

    [Fact]
    public void Map_Should_Parse_CreatedAt_Timestamp()
    {
        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-06-15T14:30:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true);

        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        chatResponse.Created.Year.Should().Be(2024);
        chatResponse.Created.Month.Should().Be(6);
        chatResponse.Created.Day.Should().Be(15);
    }

    [Fact]
    public void Map_Should_Default_FinishReason_To_Stop_When_Missing()
    {
        // FR-054: ResponseParser MUST map done_reason to FinishReason
        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true,
            doneReason: null);

        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        chatResponse.FinishReason.Should().Be(FinishReason.Stop);
    }

    // ==================== Tool Call Integration Tests (Gap #7) ====================
    [Fact]
    public void Map_ResponseWithToolCalls_ParsesCorrectly()
    {
        // Arrange - Response with single tool call
        var toolCalls = new[]
        {
            new OllamaToolCall(
                id: "call_123",
                function: new OllamaFunction(
                    name: "read_file",
                    description: "Reads a file",
                    parameters: new { path = "test.txt" })),
        };

        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: null, toolCalls: toolCalls),
            done: true);

        // Act
        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        // Assert
        chatResponse.Message.ToolCalls.Should().NotBeNull();
        chatResponse.Message.ToolCalls.Should().HaveCount(1);
        chatResponse.Message.ToolCalls![0].Id.Should().Be("call_123");
        chatResponse.Message.ToolCalls[0].Name.Should().Be("read_file");
    }

    [Fact]
    public void Map_ResponseWithMultipleToolCalls_ParsesAll()
    {
        // Arrange - Response with multiple tool calls (FR-055)
        var toolCalls = new[]
        {
            new OllamaToolCall(
                id: "call_1",
                function: new OllamaFunction(
                    name: "read_file",
                    description: "Reads a file",
                    parameters: new { path = "a.txt" })),
            new OllamaToolCall(
                id: "call_2",
                function: new OllamaFunction(
                    name: "write_file",
                    description: "Writes a file",
                    parameters: new { path = "b.txt", content = "test" })),
        };

        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: null, toolCalls: toolCalls),
            done: true);

        // Act
        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        // Assert
        chatResponse.Message.ToolCalls.Should().HaveCount(2);
        chatResponse.Message.ToolCalls![0].Name.Should().Be("read_file");
        chatResponse.Message.ToolCalls[1].Name.Should().Be("write_file");
    }

    [Fact]
    public void Map_ResponseWithToolCalls_SetsFinishReasonToolCalls()
    {
        // Arrange - FR-054: When tool calls present, FinishReason should be ToolCalls
        var toolCalls = new[]
        {
            new OllamaToolCall(
                id: "call_123",
                function: new OllamaFunction(
                    name: "read_file",
                    description: "Reads a file",
                    parameters: new { path = "test.txt" })),
        };

        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: null, toolCalls: toolCalls),
            done: true,
            doneReason: "stop");

        // Act
        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        // Assert - FinishReason should prefer ToolCalls over done_reason
        chatResponse.FinishReason.Should().Be(FinishReason.ToolCalls);
    }

    [Fact]
    public void Map_ResponseWithNoToolCalls_ReturnsNormalMessage()
    {
        // Arrange - Response without tool calls
        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello, how can I help?"),
            done: true,
            doneReason: "stop");

        // Act
        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        // Assert
        chatResponse.Message.ToolCalls.Should().BeNull();
        chatResponse.Message.Content.Should().Be("Hello, how can I help?");
        chatResponse.FinishReason.Should().Be(FinishReason.Stop);
    }

    [Fact]
    public void Map_ResponseWithToolCalls_PreservesOtherFields()
    {
        // Arrange - Verify tool call parsing doesn't break other fields
        var toolCalls = new[]
        {
            new OllamaToolCall(
                id: "call_123",
                function: new OllamaFunction(
                    name: "read_file",
                    description: "Reads a file",
                    parameters: new { path = "test.txt" })),
        };

        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "I'll read that file", toolCalls: toolCalls),
            done: true,
            promptEvalCount: 30,
            evalCount: 50);

        // Act
        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        // Assert
        chatResponse.Message.Content.Should().Be("I'll read that file");
        chatResponse.Usage.PromptTokens.Should().Be(30);
        chatResponse.Usage.CompletionTokens.Should().Be(50);
        chatResponse.Model.Should().Be("llama3.2:8b");
    }

    [Fact]
    public void Map_ResponseWithEmptyToolCallsArray_ReturnsNullToolCalls()
    {
        // Arrange - Empty tool calls array should be treated as no tool calls
        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello", toolCalls: Array.Empty<OllamaToolCall>()),
            done: true);

        // Act
        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        // Assert
        chatResponse.Message.ToolCalls.Should().BeNull();
        chatResponse.FinishReason.Should().Be(FinishReason.Stop);
    }

    [Fact]
    public void Map_ResponseWithNullParameters_UsesEmptyObject()
    {
        // Arrange - Tool call with null parameters should use empty object
        var toolCalls = new[]
        {
            new OllamaToolCall(
                id: "call_123",
                function: new OllamaFunction(
                    name: "git_status",
                    description: "Get git status",
                    parameters: null)),
        };

        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: null, toolCalls: toolCalls),
            done: true);

        // Act
        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        // Assert
        chatResponse.Message.ToolCalls.Should().NotBeNull();
        chatResponse.Message.ToolCalls.Should().HaveCount(1);
        chatResponse.Message.ToolCalls![0].Arguments.GetRawText().Should().Be("{}");
    }

    [Fact]
    public void Map_ResponseWithMissingToolCallId_GeneratesId()
    {
        // Arrange - Tool call without ID should have one generated
        var toolCalls = new[]
        {
            new OllamaToolCall(
                id: null,
                function: new OllamaFunction(
                    name: "read_file",
                    description: "Reads a file",
                    parameters: new { path = "test.txt" })),
        };

        var ollamaResponse = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: null, toolCalls: toolCalls),
            done: true);

        // Act
        var chatResponse = OllamaResponseMapper.Map(ollamaResponse);

        // Assert
        chatResponse.Message.ToolCalls.Should().NotBeNull();
        chatResponse.Message.ToolCalls.Should().HaveCount(1);
        chatResponse.Message.ToolCalls![0].Id.Should().NotBeNullOrEmpty();
        chatResponse.Message.ToolCalls[0].Id.Should().StartWith("gen_");
    }
}
