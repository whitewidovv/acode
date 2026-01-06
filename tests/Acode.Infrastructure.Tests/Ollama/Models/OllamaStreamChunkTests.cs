using System.Text.Json;
using Acode.Infrastructure.Ollama.Models;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Ollama.Models;

/// <summary>
/// Tests for OllamaStreamChunk model.
/// FR-079 to FR-085 from Task 005.a.
/// </summary>
#pragma warning disable CA2007 // ConfigureAwait not needed in test methods
public sealed class OllamaStreamChunkTests
{
    [Fact]
    public void OllamaStreamChunk_Should_Have_Model_Property()
    {
        // FR-079: OllamaStreamChunk MUST include model (string)
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: "Hi"),
            done: false);

        chunk.Model.Should().Be("llama3.2:8b");
    }

    [Fact]
    public void OllamaStreamChunk_Should_Have_Message_Property()
    {
        // FR-080: OllamaStreamChunk MUST include message (OllamaMessage)
        var message = new OllamaMessage(role: "assistant", content: "Hi");
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: message,
            done: false);

        chunk.Message.Should().Be(message);
    }

    [Fact]
    public void OllamaStreamChunk_Should_Have_Done_Property()
    {
        // FR-081: OllamaStreamChunk MUST include done (bool)
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: "Hi"),
            done: true);

        chunk.Done.Should().BeTrue();
    }

    [Fact]
    public void OllamaStreamChunk_Should_Have_DoneReason_On_Final_Chunk()
    {
        // FR-082: OllamaStreamChunk MUST include done_reason (string, optional, final only)
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: "Hi"),
            done: true,
            doneReason: "stop");

        chunk.DoneReason.Should().Be("stop");
    }

    [Fact]
    public void OllamaStreamChunk_Should_Have_TotalDuration_On_Final_Chunk()
    {
        // FR-083: OllamaStreamChunk MUST include total_duration (long, final only)
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: "Hi"),
            done: true,
            totalDuration: 2000000000);

        chunk.TotalDuration.Should().Be(2000000000);
    }

    [Fact]
    public void OllamaStreamChunk_Should_Have_EvalCount_On_Final_Chunk()
    {
        // FR-084: OllamaStreamChunk MUST include eval_count (int, final only)
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: "Hi"),
            done: true,
            evalCount: 50);

        chunk.EvalCount.Should().Be(50);
    }

    [Fact]
    public void OllamaStreamChunk_Should_Have_PromptEvalCount_On_Final_Chunk()
    {
        // FR-085: OllamaStreamChunk MUST include prompt_eval_count (int, final only)
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: "Hi"),
            done: true,
            promptEvalCount: 30);

        chunk.PromptEvalCount.Should().Be(30);
    }

    [Fact]
    public void OllamaStreamChunk_Should_Serialize_To_SnakeCase()
    {
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: "Hi"),
            done: true,
            doneReason: "stop",
            totalDuration: 2000000000,
            evalCount: 50,
            promptEvalCount: 30);

        var json = JsonSerializer.Serialize(chunk);

        json.Should().Contain("\"model\":");
        json.Should().Contain("\"message\":");
        json.Should().Contain("\"done\":");
        json.Should().Contain("\"done_reason\":");
        json.Should().Contain("\"total_duration\":");
        json.Should().Contain("\"eval_count\":");
        json.Should().Contain("\"prompt_eval_count\":");
    }

    [Fact]
    public void OllamaStreamChunk_Non_Final_Should_Omit_Optional_Properties()
    {
        var chunk = new OllamaStreamChunk(
            model: "llama3.2:8b",
            message: new OllamaMessage(role: "assistant", content: "Hi"),
            done: false);

        var json = JsonSerializer.Serialize(chunk);

        json.Should().NotContain("\"done_reason\":");
        json.Should().NotContain("\"total_duration\":");
        json.Should().NotContain("\"eval_count\":");
        json.Should().NotContain("\"prompt_eval_count\":");
    }
}
