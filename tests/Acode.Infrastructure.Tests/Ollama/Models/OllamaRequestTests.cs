using System.Text.Json;
using Acode.Infrastructure.Ollama.Models;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Ollama.Models;

/// <summary>
/// Tests for OllamaRequest model.
/// FR-019 to FR-030 from Task 005.a.
/// </summary>
#pragma warning disable CA2007 // ConfigureAwait not needed in test methods
public sealed class OllamaRequestTests
{
    [Fact]
    public void OllamaRequest_Should_Have_Model_Property()
    {
        // FR-019: OllamaRequest MUST include model (string, required)
        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: Array.Empty<OllamaMessage>(),
            stream: false);

        request.Model.Should().Be("llama3.2:8b");
    }

    [Fact]
    public void OllamaRequest_Should_Have_Messages_Property()
    {
        // FR-020: OllamaRequest MUST include messages (array, required)
        var messages = new[]
        {
            new OllamaMessage(role: "user", content: "Hello"),
        };

        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: messages,
            stream: false);

        request.Messages.Should().BeEquivalentTo(messages);
    }

    [Fact]
    public void OllamaRequest_Should_Have_Stream_Property()
    {
        // FR-021: OllamaRequest MUST include stream (bool, required)
        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: Array.Empty<OllamaMessage>(),
            stream: true);

        request.Stream.Should().BeTrue();
    }

    [Fact]
    public void OllamaRequest_Should_Have_Tools_Property()
    {
        // FR-022: OllamaRequest MUST include tools (array, optional)
        var tools = new[] { new OllamaTool(type: "function", function: null!) };

        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: Array.Empty<OllamaMessage>(),
            stream: false,
            tools: tools);

        request.Tools.Should().BeEquivalentTo(tools);
    }

    [Fact]
    public void OllamaRequest_Should_Have_Format_Property()
    {
        // FR-023: OllamaRequest MUST include format (string, optional)
        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: Array.Empty<OllamaMessage>(),
            stream: false,
            format: "json");

        request.Format.Should().Be("json");
    }

    [Fact]
    public void OllamaRequest_Should_Have_Options_Property()
    {
        // FR-024: OllamaRequest MUST include options (object, optional)
        var options = new OllamaOptions(temperature: 0.7, topP: 0.9, seed: null, numCtx: null, stop: null);

        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: Array.Empty<OllamaMessage>(),
            stream: false,
            options: options);

        request.Options.Should().BeEquivalentTo(options);
    }

    [Fact]
    public void OllamaRequest_Should_Have_KeepAlive_Property()
    {
        // FR-025: OllamaRequest MUST include keep_alive (string, optional)
        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: Array.Empty<OllamaMessage>(),
            stream: false,
            keepAlive: "5m");

        request.KeepAlive.Should().Be("5m");
    }

    [Fact]
    public void OllamaRequest_Should_Serialize_To_SnakeCase_Json()
    {
        // FR-018: Serialized JSON MUST use snake_case property names
        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[] { new OllamaMessage(role: "user", content: "Hello") },
            stream: true,
            keepAlive: "5m");

        var json = JsonSerializer.Serialize(request);

        json.Should().Contain("\"model\":");
        json.Should().Contain("\"messages\":");
        json.Should().Contain("\"stream\":");
        json.Should().Contain("\"keep_alive\":");
        json.Should().NotContain("\"Model\":");
        json.Should().NotContain("\"KeepAlive\":");
    }

    [Fact]
    public void OllamaRequest_Should_Omit_Null_Optional_Properties()
    {
        // FR-017: RequestSerializer MUST omit null/default values
        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[] { new OllamaMessage(role: "user", content: "Hello") },
            stream: false);

        var json = JsonSerializer.Serialize(request);

        json.Should().NotContain("\"tools\":");
        json.Should().NotContain("\"format\":");
        json.Should().NotContain("\"options\":");
        json.Should().NotContain("\"keep_alive\":");
    }

    [Fact]
    public void OllamaOptions_Should_Support_Temperature()
    {
        // FR-026: OllamaRequest.options MUST support temperature
        var options = new OllamaOptions(temperature: 0.8, topP: 1.0, seed: null, numCtx: null, stop: null);

        options.Temperature.Should().Be(0.8);
    }

    [Fact]
    public void OllamaOptions_Should_Support_TopP()
    {
        // FR-027: OllamaRequest.options MUST support top_p
        var options = new OllamaOptions(temperature: 0.7, topP: 0.95, seed: null, numCtx: null, stop: null);

        options.TopP.Should().Be(0.95);
    }

    [Fact]
    public void OllamaOptions_Should_Support_Seed()
    {
        // FR-028: OllamaRequest.options MUST support seed
        var options = new OllamaOptions(temperature: 0.7, topP: 1.0, seed: 42, numCtx: null, stop: null);

        options.Seed.Should().Be(42);
    }

    [Fact]
    public void OllamaOptions_Should_Support_NumCtx()
    {
        // FR-029: OllamaRequest.options MUST support num_ctx
        var options = new OllamaOptions(temperature: 0.7, topP: 1.0, seed: null, numCtx: 4096, stop: null);

        options.NumCtx.Should().Be(4096);
    }

    [Fact]
    public void OllamaOptions_Should_Support_Stop_Sequences()
    {
        // FR-030: OllamaRequest.options MUST support stop sequences
        var stopSequences = new[] { "\n", "###" };
        var options = new OllamaOptions(temperature: 0.7, topP: 1.0, seed: null, numCtx: null, stop: stopSequences);

        options.Stop.Should().BeEquivalentTo(stopSequences);
    }

    [Fact]
    public void OllamaOptions_Should_Serialize_To_SnakeCase()
    {
        var options = new OllamaOptions(temperature: 0.7, topP: 0.9, seed: 42, numCtx: 2048, stop: new[] { "\n" });

        var json = JsonSerializer.Serialize(options);

        json.Should().Contain("\"temperature\":");
        json.Should().Contain("\"top_p\":");
        json.Should().Contain("\"seed\":");
        json.Should().Contain("\"num_ctx\":");
        json.Should().Contain("\"stop\":");
    }
}
