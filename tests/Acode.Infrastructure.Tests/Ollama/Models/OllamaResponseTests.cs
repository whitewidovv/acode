using System.Text.Json;
using Acode.Infrastructure.Ollama.Models;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Ollama.Models;

/// <summary>
/// Tests for OllamaResponse model.
/// FR-041 to FR-051 from Task 005.a.
/// </summary>
#pragma warning disable CA2007 // ConfigureAwait not needed in test methods
public sealed class OllamaResponseTests
{
    [Fact]
    public void OllamaResponse_Should_Have_Model_Property()
    {
        // FR-041: OllamaResponse MUST include model (string)
        var response = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true);

        response.Model.Should().Be("llama3.2:8b");
    }

    [Fact]
    public void OllamaResponse_Should_Have_CreatedAt_Property()
    {
        // FR-042: OllamaResponse MUST include created_at (string)
        var response = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true);

        response.CreatedAt.Should().Be("2024-01-01T12:00:00Z");
    }

    [Fact]
    public void OllamaResponse_Should_Have_Message_Property()
    {
        // FR-043: OllamaResponse MUST include message (OllamaMessage)
        var message = new OllamaMessage(role: "assistant", content: "Hello");
        var response = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: message,
            done: true);

        response.Message.Should().Be(message);
    }

    [Fact]
    public void OllamaResponse_Should_Have_Done_Property()
    {
        // FR-044: OllamaResponse MUST include done (bool)
        var response = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true);

        response.Done.Should().BeTrue();
    }

    [Fact]
    public void OllamaResponse_Should_Have_DoneReason_Property()
    {
        // FR-045: OllamaResponse MUST include done_reason (string, optional)
        var response = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true,
            doneReason: "stop");

        response.DoneReason.Should().Be("stop");
    }

    [Fact]
    public void OllamaResponse_Should_Have_TotalDuration_Property()
    {
        // FR-046: OllamaResponse MUST include total_duration (long, nanoseconds)
        var response = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true,
            totalDuration: 1500000000);

        response.TotalDuration.Should().Be(1500000000);
    }

    [Fact]
    public void OllamaResponse_Should_Have_PromptEvalCount_Property()
    {
        // FR-047: OllamaResponse MUST include prompt_eval_count (int)
        var response = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true,
            promptEvalCount: 25);

        response.PromptEvalCount.Should().Be(25);
    }

    [Fact]
    public void OllamaResponse_Should_Have_EvalCount_Property()
    {
        // FR-048: OllamaResponse MUST include eval_count (int)
        var response = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true,
            evalCount: 42);

        response.EvalCount.Should().Be(42);
    }

    [Fact]
    public void OllamaResponse_Should_Serialize_To_SnakeCase()
    {
        var response = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: true,
            doneReason: "stop",
            totalDuration: 1500000000,
            promptEvalCount: 25,
            evalCount: 42);

        var json = JsonSerializer.Serialize(response);

        json.Should().Contain("\"model\":");
        json.Should().Contain("\"created_at\":");
        json.Should().Contain("\"message\":");
        json.Should().Contain("\"done\":");
        json.Should().Contain("\"done_reason\":");
        json.Should().Contain("\"total_duration\":");
        json.Should().Contain("\"prompt_eval_count\":");
        json.Should().Contain("\"eval_count\":");
    }

    [Fact]
    public void OllamaResponse_Should_Omit_Null_Optional_Properties()
    {
        var response = new OllamaResponse(
            model: "llama3.2:8b",
            createdAt: "2024-01-01T12:00:00Z",
            message: new OllamaMessage(role: "assistant", content: "Hello"),
            done: false);

        var json = JsonSerializer.Serialize(response);

        json.Should().NotContain("\"done_reason\":");
        json.Should().NotContain("\"total_duration\":");
        json.Should().NotContain("\"prompt_eval_count\":");
        json.Should().NotContain("\"eval_count\":");
    }
}
