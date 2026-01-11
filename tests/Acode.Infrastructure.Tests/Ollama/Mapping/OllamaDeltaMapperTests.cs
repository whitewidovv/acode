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
}
