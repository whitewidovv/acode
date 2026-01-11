namespace Acode.Infrastructure.Tests.Ollama;

using System.Net;
using System.Net.Http;
using System.Text.Json;
using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Ollama;
using Acode.Infrastructure.Ollama.Exceptions;
using Acode.Infrastructure.Tests.Ollama.Http;
using FluentAssertions;

/// <summary>
/// Tests for <see cref="OllamaProvider"/> ChatAsync method.
/// </summary>
/// <remarks>
/// FR-005-062 to FR-005-095: OllamaProvider implementation.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "xUnit1030: Test methods should not use ConfigureAwait(false)")]
public sealed class OllamaProviderTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var httpClient = new HttpClient();
        var config = new OllamaConfiguration();

        // Act
        var provider = new OllamaProvider(httpClient, config);

        // Assert
        provider.ProviderName.Should().Be("ollama");
        provider.Capabilities.SupportsStreaming.Should().BeTrue();
        provider.Capabilities.SupportsTools.Should().BeTrue();
        provider.Capabilities.SupportsSystemMessages.Should().BeTrue();
        provider.Capabilities.DefaultModel.Should().Be("llama3.2:latest");
    }

    [Fact]
    public async Task ChatAsync_WithSimpleRequest_ReturnsResponse()
    {
        // Arrange
        var ollamaResponse = new
        {
            model = "llama3.2:latest",
            created_at = "2024-01-01T12:00:00Z",
            message = new
            {
                role = "assistant",
                content = "Hello! How can I help you?",
            },
            done = true,
            done_reason = "stop",
            total_duration = 1000000000L, // 1 second in nanoseconds
            prompt_eval_count = 10,
            eval_count = 20,
        };

        var httpClient = CreateHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(ollamaResponse));
        var config = new OllamaConfiguration();
        var provider = new OllamaProvider(httpClient, config);

        var request = new ChatRequest(
            messages: new[]
            {
                ChatMessage.CreateUser("Hello"),
            });

        // Act
        var response = await provider.ChatAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Message.Content.Should().Be("Hello! How can I help you?");
        response.Message.Role.Should().Be(MessageRole.Assistant);
        response.FinishReason.Should().Be(FinishReason.Stop);
        response.Usage.PromptTokens.Should().Be(10);
        response.Usage.CompletionTokens.Should().Be(20);
        response.Usage.TotalTokens.Should().Be(30);
        response.Model.Should().Be("llama3.2:latest");
    }

    [Fact]
    public async Task ChatAsync_WithModelParameters_PassesParameters()
    {
        // Arrange
        var ollamaResponse = new
        {
            model = "qwen2.5:latest",
            created_at = "2024-01-01T12:00:00Z",
            message = new { role = "assistant", content = "Response" },
            done = true,
            done_reason = "stop",
        };

        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(ollamaResponse)),
        });
        var httpClient = new HttpClient(handler);
        var config = new OllamaConfiguration();
        var provider = new OllamaProvider(httpClient, config);

        var request = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") },
            modelParameters: new ModelParameters(
                model: "qwen2.5:latest",
                temperature: 0.7,
                maxTokens: 100,
                topP: 0.9));

        // Act
        await provider.ChatAsync(request);

        // Assert
        handler.LastRequestContent.Should().NotBeNull();
        var sentRequest = JsonSerializer.Deserialize<JsonElement>(handler.LastRequestContent!);
        sentRequest.GetProperty("model").GetString().Should().Be("qwen2.5:latest");
        sentRequest.GetProperty("options").GetProperty("temperature").GetDouble().Should().BeApproximately(0.7, 0.01);
        sentRequest.GetProperty("options").GetProperty("top_p").GetDouble().Should().BeApproximately(0.9, 0.01);
        sentRequest.GetProperty("options").GetProperty("num_ctx").GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task ChatAsync_WhenServerReturns500_ThrowsOllamaServerException()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.InternalServerError, "Server error");
        var config = new OllamaConfiguration();
        var provider = new OllamaProvider(httpClient, config);

        var request = new ChatRequest(messages: new[] { ChatMessage.CreateUser("Hello") });

        // Act
        var act = async () => await provider.ChatAsync(request);

        // Assert
        await act.Should().ThrowAsync<OllamaServerException>()
            .Where(ex => ex.StatusCode == 500);
    }

    [Fact]
    public async Task ChatAsync_WhenConnectionFails_ThrowsOllamaConnectionException()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.NotFound, "Not found");
        var config = new OllamaConfiguration(baseUrl: "http://invalid-host");
        var provider = new OllamaProvider(httpClient, config);

        var request = new ChatRequest(messages: new[] { ChatMessage.CreateUser("Hello") });

        // Act
        var act = async () => await provider.ChatAsync(request);

        // Assert
        await act.Should().ThrowAsync<OllamaConnectionException>();
    }

    [Fact]
    public async Task IsHealthyAsync_WhenServerHealthy_ReturnsTrue()
    {
        // Arrange
        var tagsResponse = new { models = new[] { new { name = "llama3.2:latest" } } };
        var httpClient = CreateHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(tagsResponse));
        var config = new OllamaConfiguration();
        var provider = new OllamaProvider(httpClient, config);

        // Act
        var isHealthy = await provider.IsHealthyAsync();

        // Assert
        isHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_WhenServerUnhealthy_ReturnsFalse()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.InternalServerError, "Error");
        var config = new OllamaConfiguration();
        var provider = new OllamaProvider(httpClient, config);

        // Act
        var isHealthy = await provider.IsHealthyAsync();

        // Assert
        isHealthy.Should().BeFalse();
    }

    [Fact]
    public void GetSupportedModels_ReturnsCommonModels()
    {
        // Arrange
        var httpClient = new HttpClient();
        var config = new OllamaConfiguration();
        var provider = new OllamaProvider(httpClient, config);

        // Act
        var models = provider.GetSupportedModels();

        // Assert
        models.Should().NotBeEmpty();
        models.Should().Contain("llama3.2:latest");
        models.Should().Contain("qwen2.5:latest");
    }

    [Fact]
    public async Task StreamChatAsync_WithSimpleRequest_ReturnsDeltas()
    {
        // Arrange
        var chunks = new[]
        {
            "{\"model\":\"llama3.2:latest\",\"message\":{\"role\":\"assistant\",\"content\":\"Hello\"},\"done\":false}\n",
            "{\"model\":\"llama3.2:latest\",\"message\":{\"role\":\"assistant\",\"content\":\" there\"},\"done\":false}\n",
            "{\"model\":\"llama3.2:latest\",\"message\":{\"role\":\"assistant\",\"content\":\"!\"},\"done\":true,\"done_reason\":\"stop\",\"prompt_eval_count\":10,\"eval_count\":3}\n",
        };

        var streamContent = string.Concat(chunks);
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(streamContent),
        });
        var httpClient = new HttpClient(handler);
        var config = new OllamaConfiguration();
        var provider = new OllamaProvider(httpClient, config);

        var request = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") },
            stream: true);

        // Act
        var deltas = new System.Collections.Generic.List<ResponseDelta>();
        await foreach (var delta in provider.StreamChatAsync(request))
        {
            deltas.Add(delta);
        }

        // Assert
        deltas.Should().HaveCount(3);
        deltas[0].ContentDelta.Should().Be("Hello");
        deltas[0].IsComplete.Should().BeFalse();
        deltas[1].ContentDelta.Should().Be(" there");
        deltas[1].IsComplete.Should().BeFalse();
        deltas[2].ContentDelta.Should().Be("!");
        deltas[2].IsComplete.Should().BeTrue();
        deltas[2].FinishReason.Should().Be(FinishReason.Stop);
        deltas[2].Usage.Should().NotBeNull();
        deltas[2].Usage!.PromptTokens.Should().Be(10);
        deltas[2].Usage!.CompletionTokens.Should().Be(3);
    }

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string content)
    {
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content),
        });

        return new HttpClient(handler);
    }
}
