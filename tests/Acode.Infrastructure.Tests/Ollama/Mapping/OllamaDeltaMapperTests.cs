using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Ollama.Mapping;
using Acode.Infrastructure.Ollama.Models;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Ollama.Mapping;

/// <summary>
/// Tests for OllamaDeltaMapper.
/// FR-079 to FR-092 from Task 005.a.
/// </summary>
#pragma warning disable CA2007 // ConfigureAwait not needed in test methods
public sealed class OllamaDeltaMapperTests
{
    [Fact]
    public void MapToDelta_Should_Extract_Content_Delta()
    {
        // FR-079: Extract content delta from chunk
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: false);

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 0);

        delta.Index.Should().Be(0);
        delta.ContentDelta.Should().Be("Hello");
        delta.ToolCallDelta.Should().BeNull();
        delta.FinishReason.Should().BeNull();
    }

    [Fact]
    public void MapToDelta_Should_Map_Final_Chunk_With_FinishReason()
    {
        // FR-081: Detect final chunk (done: true)
        // FR-082: Map done_reason to FinishReason
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: "!"),
            done: true,
            doneReason: "stop");

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 2);

        delta.Index.Should().Be(2);
        delta.ContentDelta.Should().Be("!");
        delta.FinishReason.Should().Be(FinishReason.Stop);
    }

    [Fact]
    public void MapToDelta_Should_Include_Usage_In_Final_Chunk()
    {
        // FR-084: Calculate UsageInfo from token counts
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: string.Empty),
            done: true,
            doneReason: "stop",
            promptEvalCount: 25,
            evalCount: 50);

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 5);

        delta.Usage.Should().NotBeNull();
        delta.Usage!.PromptTokens.Should().Be(25);
        delta.Usage.CompletionTokens.Should().Be(50);
        delta.Usage.TotalTokens.Should().Be(75);
    }

    [Fact]
    public void MapToDelta_Should_Map_Length_FinishReason()
    {
        // FR-083: Map "length" to FinishReason.Length
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: "..."),
            done: true,
            doneReason: "length");

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 10);

        delta.FinishReason.Should().Be(FinishReason.Length);
    }

    [Fact]
    public void MapToDelta_Should_Map_ToolCalls_FinishReason()
    {
        // FR-084: Map "tool_calls" to FinishReason.ToolCalls
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: string.Empty),
            done: true,
            doneReason: "tool_calls");

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 3);

        delta.FinishReason.Should().Be(FinishReason.ToolCalls);
    }

    [Fact]
    public void MapToDelta_Should_Handle_Empty_Content()
    {
        // FR-092: Handle empty content chunks
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: string.Empty),
            done: false);

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 1);

        delta.ContentDelta.Should().Be(string.Empty);
    }

    [Fact]
    public void MapToDelta_Should_Default_Missing_DoneReason_To_Stop()
    {
        // FR-085: Default doneReason to Stop when missing
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: "done"),
            done: true,
            doneReason: null);

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 0);

        delta.FinishReason.Should().Be(FinishReason.Stop);
    }

    [Fact]
    public void MapToDelta_Should_Handle_Null_Content()
    {
        // FR-092: Handle null content chunks
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: null),
            done: true,
            doneReason: "stop");

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 0);

        delta.ContentDelta.Should().BeNull();
        delta.FinishReason.Should().Be(FinishReason.Stop);
    }

    // ==================== Streaming Tool Call Tests (Gap #8) ====================
    [Fact]
    public void MapToDelta_Should_Map_Tool_Call_From_Chunk()
    {
        // Gap #8: Tool call present in streaming chunk
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(
                role: "assistant",
                content: null,
                toolCalls: new[]
                {
                    new OllamaToolCallResponse(
                        id: "call_123",
                        function: new OllamaToolCallFunction(
                            name: "read_file",
                            arguments: "{\"path\": \"test.txt\"}")),
                }),
            done: false);

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 0);

        delta.Index.Should().Be(0);
        delta.ToolCallDelta.Should().NotBeNull();
        delta.ToolCallDelta!.Index.Should().Be(0);
        delta.ToolCallDelta.Id.Should().Be("call_123");
        delta.ToolCallDelta.Name.Should().Be("read_file");
        delta.ToolCallDelta.ArgumentsDelta.Should().Be("{\"path\": \"test.txt\"}");
    }

    [Fact]
    public void MapToDelta_Should_Handle_Multiple_Tool_Calls()
    {
        // Gap #8: Multiple tool calls - map first one
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(
                role: "assistant",
                content: null,
                toolCalls: new[]
                {
                    new OllamaToolCallResponse(
                        id: "call_1",
                        function: new OllamaToolCallFunction(
                            name: "read_file",
                            arguments: "{\"path\": \"a.txt\"}")),
                    new OllamaToolCallResponse(
                        id: "call_2",
                        function: new OllamaToolCallFunction(
                            name: "write_file",
                            arguments: "{\"path\": \"b.txt\"}")),
                }),
            done: false);

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 0);

        // Should map the first tool call
        delta.ToolCallDelta.Should().NotBeNull();
        delta.ToolCallDelta!.Id.Should().Be("call_1");
        delta.ToolCallDelta.Name.Should().Be("read_file");
    }

    [Fact]
    public void MapToDelta_Should_Handle_Tool_Call_With_Content()
    {
        // Gap #8: Tool call can arrive with content
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(
                role: "assistant",
                content: "I'll read that file for you.",
                toolCalls: new[]
                {
                    new OllamaToolCallResponse(
                        id: "call_456",
                        function: new OllamaToolCallFunction(
                            name: "read_file",
                            arguments: "{\"path\": \"data.json\"}")),
                }),
            done: false);

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 0);

        // Both content and tool call should be present
        delta.ContentDelta.Should().Be("I'll read that file for you.");
        delta.ToolCallDelta.Should().NotBeNull();
        delta.ToolCallDelta!.Name.Should().Be("read_file");
    }

    [Fact]
    public void MapToDelta_Should_Handle_Tool_Call_In_Final_Chunk()
    {
        // Gap #8: Tool call in final chunk with finish reason
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(
                role: "assistant",
                content: null,
                toolCalls: new[]
                {
                    new OllamaToolCallResponse(
                        id: "call_789",
                        function: new OllamaToolCallFunction(
                            name: "execute_command",
                            arguments: "{\"command\": \"ls\"}")),
                }),
            done: true,
            doneReason: "tool_calls",
            promptEvalCount: 30,
            evalCount: 10);

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 0);

        // Tool call, finish reason, and usage should all be present
        delta.ToolCallDelta.Should().NotBeNull();
        delta.ToolCallDelta!.Name.Should().Be("execute_command");
        delta.FinishReason.Should().Be(FinishReason.ToolCalls);
        delta.Usage.Should().NotBeNull();
        delta.Usage!.PromptTokens.Should().Be(30);
        delta.Usage.CompletionTokens.Should().Be(10);
    }

    [Fact]
    public void MapToDelta_Should_Handle_Empty_Tool_Calls_Array()
    {
        // Gap #8: Empty tool calls array should be ignored
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(
                role: "assistant",
                content: "Hello",
                toolCalls: Array.Empty<OllamaToolCallResponse>()),
            done: false);

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 0);

        delta.ContentDelta.Should().Be("Hello");
        delta.ToolCallDelta.Should().BeNull();
    }

    [Fact]
    public void MapToDelta_Should_Handle_Tool_Call_With_Null_Function()
    {
        // Gap #8: Tool call with null function should be ignored
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(
                role: "assistant",
                content: "Processing...",
                toolCalls: new[]
                {
                    new OllamaToolCallResponse(id: "call_999", function: null),
                }),
            done: false);

        var delta = OllamaDeltaMapper.MapToDelta(chunk, 0);

        delta.ContentDelta.Should().Be("Processing...");
        delta.ToolCallDelta.Should().BeNull();
    }
}
