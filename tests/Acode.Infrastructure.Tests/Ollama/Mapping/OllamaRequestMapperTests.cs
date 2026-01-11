using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Ollama.Mapping;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Ollama.Mapping;

/// <summary>
/// Tests for OllamaRequestMapper.
/// FR-008 to FR-018 from Task 005.a.
/// </summary>
#pragma warning disable CA2007 // ConfigureAwait not needed in test methods
public sealed class OllamaRequestMapperTests
{
    [Fact]
    public void Map_Should_Set_Model_From_Parameters()
    {
        // FR-010: RequestSerializer MUST set "model" from request or default
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") },
            modelParameters: new ModelParameters(model: "llama3.2:8b"));

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Model.Should().Be("llama3.2:8b");
    }

    [Fact]
    public void Map_Should_Set_Stream_To_False_For_NonStreaming()
    {
        // FR-011: RequestSerializer MUST set "stream" based on request type
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") },
            stream: false);

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Stream.Should().BeFalse();
    }

    [Fact]
    public void Map_Should_Set_Stream_To_True_For_Streaming()
    {
        // FR-011: RequestSerializer MUST set "stream" based on request type
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") },
            stream: true);

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Stream.Should().BeTrue();
    }

    [Fact]
    public void Map_Should_Map_User_Message()
    {
        // FR-012: RequestSerializer MUST map messages array correctly
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("What is 2+2?") });

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Messages.Should().HaveCount(1);
        ollamaRequest.Messages[0].Role.Should().Be("user");
        ollamaRequest.Messages[0].Content.Should().Be("What is 2+2?");
    }

    [Fact]
    public void Map_Should_Map_System_Message()
    {
        // FR-012: RequestSerializer MUST map messages array correctly
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateSystem("You are a helpful assistant") });

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Messages.Should().HaveCount(1);
        ollamaRequest.Messages[0].Role.Should().Be("system");
        ollamaRequest.Messages[0].Content.Should().Be("You are a helpful assistant");
    }

    [Fact]
    public void Map_Should_Map_Assistant_Message()
    {
        // FR-012: RequestSerializer MUST map messages array correctly
        var chatRequest = new ChatRequest(
            messages: new[]
            {
                ChatMessage.CreateUser("Hello"),
                ChatMessage.CreateAssistant("Hi! How can I help?"),
            });

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Messages.Should().HaveCount(2);
        ollamaRequest.Messages[1].Role.Should().Be("assistant");
        ollamaRequest.Messages[1].Content.Should().Be("Hi! How can I help?");
    }

    [Fact]
    public void Map_Should_Map_Multiple_Messages()
    {
        // FR-012: RequestSerializer MUST map messages array correctly
        var chatRequest = new ChatRequest(
            messages: new[]
            {
                ChatMessage.CreateSystem("You are helpful"),
                ChatMessage.CreateUser("Hello"),
                ChatMessage.CreateAssistant("Hi there!"),
                ChatMessage.CreateUser("How are you?"),
            });

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Messages.Should().HaveCount(4);
        ollamaRequest.Messages.Select(m => m.Role).Should().Equal("system", "user", "assistant", "user");
    }

    [Fact]
    public void Map_Should_Include_Temperature_In_Options()
    {
        // FR-014: RequestSerializer MUST include options (temperature, top_p, etc.)
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") },
            modelParameters: new ModelParameters(model: "test", temperature: 0.9));

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Options.Should().NotBeNull();
        ollamaRequest.Options!.Temperature.Should().Be(0.9);
    }

    [Fact]
    public void Map_Should_Include_TopP_In_Options()
    {
        // FR-014: RequestSerializer MUST include options (temperature, top_p, etc.)
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") },
            modelParameters: new ModelParameters(model: "test", topP: 0.95));

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Options.Should().NotBeNull();
        ollamaRequest.Options!.TopP.Should().Be(0.95);
    }

    [Fact]
    public void Map_Should_Include_Seed_In_Options()
    {
        // FR-014: RequestSerializer MUST include options (temperature, top_p, etc.)
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") },
            modelParameters: new ModelParameters(model: "test", seed: 42));

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Options.Should().NotBeNull();
        ollamaRequest.Options!.Seed.Should().Be(42);
    }

    [Fact]
    public void Map_Should_Include_MaxTokens_As_NumCtx_In_Options()
    {
        // FR-014: RequestSerializer MUST include options (temperature, top_p, etc.)
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") },
            modelParameters: new ModelParameters(model: "test", maxTokens: 2048));

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Options.Should().NotBeNull();
        ollamaRequest.Options!.NumCtx.Should().Be(2048);
    }

    [Fact]
    public void Map_Should_Include_StopSequences_In_Options()
    {
        // FR-014: RequestSerializer MUST include options (temperature, top_p, etc.)
        var stopSeq = new[] { "\n\n", "###" };
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") },
            modelParameters: new ModelParameters(model: "test", stopSequences: stopSeq));

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Options.Should().NotBeNull();
        ollamaRequest.Options!.Stop.Should().BeEquivalentTo(stopSeq);
    }

    [Fact]
    public void Map_Should_Omit_Options_When_Parameters_Are_Null()
    {
        // FR-017: RequestSerializer MUST omit null/default values
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") });

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Options.Should().BeNull();
    }

    [Fact]
    public void Map_Should_Map_Tools_When_Provided()
    {
        // FR-013: RequestSerializer MUST map tool definitions when present
        var toolDef = new ToolDefinition(
            Name: "get_weather",
            Description: "Get weather info",
            Parameters: System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>("{\"type\":\"object\"}"),
            Strict: false);

        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("What's the weather?") },
            tools: new[] { toolDef });

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Tools.Should().HaveCount(1);
        ollamaRequest.Tools![0].Type.Should().Be("function");
        ollamaRequest.Tools[0].Function.Name.Should().Be("get_weather");
        ollamaRequest.Tools[0].Function.Description.Should().Be("Get weather info");
    }

    [Fact]
    public void Map_Should_Omit_Tools_When_None_Provided()
    {
        // FR-017: RequestSerializer MUST omit null/default values
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") });

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest);

        ollamaRequest.Tools.Should().BeNull();
    }

    [Fact]
    public void Map_Should_Use_Default_Model_When_Not_Specified()
    {
        // FR-010: RequestSerializer MUST set "model" from request or default
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") });

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest, defaultModel: "llama3.2:8b");

        ollamaRequest.Model.Should().Be("llama3.2:8b");
    }

    [Fact]
    public void Map_Should_Prefer_Request_Model_Over_Default()
    {
        // FR-010: RequestSerializer MUST set "model" from request or default
        var chatRequest = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") },
            modelParameters: new ModelParameters(model: "codellama:13b"));

        var ollamaRequest = OllamaRequestMapper.Map(chatRequest, defaultModel: "llama3.2:8b");

        ollamaRequest.Model.Should().Be("codellama:13b");
    }
}
