using System.Net.Http.Json;
using Acode.Infrastructure.Ollama.Http;
using Acode.Infrastructure.Ollama.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Acode.Integration.Tests.Providers.Ollama;

/// <summary>
/// Integration tests for OllamaHttpClient.
/// </summary>
/// <remarks>
/// These tests require a running Ollama instance at http://localhost:11434.
/// Tests will be skipped if Ollama is not available.
/// Gap #19: Integration tests per Testing Requirements lines 588-595.
/// </remarks>
[Collection("Ollama Integration Tests")]
public sealed class OllamaHttpIntegrationTests : IAsyncLifetime
{
    private const string OllamaBaseUrl = "http://localhost:11434";
    private const string TestModel = "llama3.2:latest";

    private HttpClient? _httpClient;
    private OllamaHttpClient? _ollamaClient;
    private bool _ollamaAvailable;

    public async Task InitializeAsync()
    {
        // Check if Ollama is available before running tests
        try
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(OllamaBaseUrl) };
            var response = await _httpClient.GetAsync("/api/tags", CancellationToken.None);
            _ollamaAvailable = response.IsSuccessStatusCode;

            if (_ollamaAvailable)
            {
                var logger = Substitute.For<ILogger<OllamaHttpClient>>();
                _ollamaClient = new OllamaHttpClient(_httpClient, OllamaBaseUrl, ownsHttpClient: false, logger: logger);
            }
        }
        catch
        {
            _ollamaAvailable = false;
        }
    }

    public Task DisposeAsync()
    {
        _ollamaClient?.Dispose();
        _httpClient?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Should_Send_Request()
    {
        // Gap #19 Test 1: Verify OllamaHttpClient can send actual requests
        if (!_ollamaAvailable)
        {
            // Skip test if Ollama not available - this is expected in CI/CD
            return;
        }

        // Arrange
        var request = new OllamaRequest(
            model: TestModel,
            messages: new[] { new OllamaMessage(role: "user", content: "Say 'test' and nothing else") },
            stream: false);

        // Act
        var response = await _ollamaClient!.PostChatAsync(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Model.Should().NotBeNullOrEmpty();
        response.Done.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Receive_Response()
    {
        // Gap #19 Test 2: Verify OllamaHttpClient receives valid responses
        if (!_ollamaAvailable)
        {
            return;
        }

        // Arrange
        var request = new OllamaRequest(
            model: TestModel,
            messages: new[] { new OllamaMessage(role: "user", content: "Hello") },
            stream: false);

        // Act
        var response = await _ollamaClient!.PostChatAsync(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Message.Should().NotBeNull();
        response.Message.Content.Should().NotBeNullOrEmpty();
        response.Message.Role.Should().Be("assistant");
        response.Done.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Stream_Response()
    {
        // Gap #19 Test 3: Verify OllamaHttpClient can handle streaming responses
        if (!_ollamaAvailable)
        {
            return;
        }

        // Arrange
        var request = new OllamaRequest(
            model: TestModel,
            messages: new[] { new OllamaMessage(role: "user", content: "Count to 3") },
            stream: true);

        // Act
        var streamResponse = await _httpClient!.PostAsJsonAsync(
            "/api/chat",
            request,
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase },
            CancellationToken.None);

        // Assert
        streamResponse.Should().NotBeNull();
        streamResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        streamResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/x-ndjson");
    }

    [Fact]
    public async Task Should_Handle_Errors()
    {
        // Gap #19 Test 4: Verify OllamaHttpClient properly handles error conditions
        if (!_ollamaAvailable)
        {
            return;
        }

        // Arrange - Request with invalid model
        var request = new OllamaRequest(
            model: "nonexistent-model-12345",
            messages: new[] { new OllamaMessage(role: "user", content: "Test") },
            stream: false);

        // Act & Assert
        var act = async () => await _ollamaClient!.PostChatAsync(request, CancellationToken.None);

        // Should throw an appropriate exception (either OllamaRequestException or OllamaServerException)
        await act.Should().ThrowAsync<Exception>()
            .Where(ex => ex is Infrastructure.Ollama.Exceptions.OllamaException);
    }

    [Fact]
    public async Task Should_Use_Generic_PostAsync()
    {
        // Additional test: Verify generic PostAsync<TResponse> works with real Ollama
        if (!_ollamaAvailable)
        {
            return;
        }

        // Arrange
        var request = new OllamaRequest(
            model: TestModel,
            messages: new[] { new OllamaMessage(role: "user", content: "Say 'integration test passed'") },
            stream: false);

        // Act
        var response = await _ollamaClient!.PostAsync<OllamaResponse>("/api/chat", request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Message.Content.Should().NotBeNullOrEmpty();
        response.Done.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Handle_Cancellation()
    {
        // Additional test: Verify cancellation token is respected
        if (!_ollamaAvailable)
        {
            return;
        }

        // Arrange
        using var cts = new CancellationTokenSource();
        var request = new OllamaRequest(
            model: TestModel,
            messages: new[] { new OllamaMessage(role: "user", content: "Write a long story") },
            stream: false);

        // Cancel immediately
        cts.Cancel();

        // Act & Assert
        var act = async () => await _ollamaClient!.PostChatAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
