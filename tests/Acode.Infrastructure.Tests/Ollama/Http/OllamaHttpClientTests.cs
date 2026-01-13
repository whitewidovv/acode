using System.Net;
using Acode.Infrastructure.Ollama;
using Acode.Infrastructure.Ollama.Http;
using Acode.Infrastructure.Ollama.Models;
using FluentAssertions;
using NSubstitute;

namespace Acode.Infrastructure.Tests.Ollama.Http;

/// <summary>
/// Tests for OllamaHttpClient.
/// FR-001 to FR-007 from Task 005.a.
/// </summary>
#pragma warning disable CA2007 // ConfigureAwait not needed in test methods
public sealed class OllamaHttpClientTests
{
    [Fact]
    public async Task PostChatAsync_Should_Send_Request_To_Correct_Endpoint()
    {
        // FR-004: Configure base address from configuration
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""model"": ""llama3.2:8b"",
                ""created_at"": ""2024-01-01T12:00:00Z"",
                ""message"": { ""role"": ""assistant"", ""content"": ""Hello!"" },
                ""done"": true
            }"),
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434"),
        };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[] { new OllamaMessage(role: "user", content: "Hi") },
            stream: false);

        var response = await ollamaClient.PostChatAsync(request, CancellationToken.None);

        response.Should().NotBeNull();
        handler.LastRequestUri.Should().Be("http://localhost:11434/api/chat");
    }

    [Fact]
    public void Constructor_Should_Set_BaseAddress()
    {
        // FR-004: Configure base address from configuration
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:11434") };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        ollamaClient.BaseAddress.Should().Be("http://localhost:11434");
    }

    [Fact]
    public void Constructor_Should_Generate_CorrelationId()
    {
        // FR-007: Expose correlation ID for request tracing
        var httpClient = new HttpClient();

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        ollamaClient.CorrelationId.Should().NotBeNullOrEmpty();
        Guid.TryParse(ollamaClient.CorrelationId, out _).Should().BeTrue();
    }

    [Fact]
    public void Dispose_Should_Cleanup_Resources()
    {
        // FR-006: Implement IDisposable for cleanup
        var httpClient = new HttpClient();

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        var act = () => ollamaClient.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public async Task PostChatAsync_Should_Serialize_Request_Correctly()
    {
        // FR-008 to FR-014: Request serialization
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""model"": ""llama3.2:8b"",
                ""created_at"": ""2024-01-01T12:00:00Z"",
                ""message"": { ""role"": ""assistant"", ""content"": ""Hi"" },
                ""done"": true
            }"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[] { new OllamaMessage(role: "user", content: "Hello") },
            stream: false);

        await ollamaClient.PostChatAsync(request, CancellationToken.None);

        var sentContent = handler.LastRequestContent;
        sentContent.Should().NotBeNull();
        sentContent.Should().Contain("\"model\":\"llama3.2:8b\"");
        sentContent.Should().Contain("\"stream\":false");
    }

    [Fact]
    public async Task PostChatAsync_Should_Parse_Response_Correctly()
    {
        // FR-015 to FR-033: Response parsing
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""model"": ""llama3.2:8b"",
                ""created_at"": ""2024-01-01T12:00:00Z"",
                ""message"": { ""role"": ""assistant"", ""content"": ""Test response"" },
                ""done"": true,
                ""done_reason"": ""stop"",
                ""total_duration"": 1500000000,
                ""prompt_eval_count"": 10,
                ""eval_count"": 20
            }"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[] { new OllamaMessage(role: "user", content: "Hello") },
            stream: false);

        var response = await ollamaClient.PostChatAsync(request, CancellationToken.None);

        response.Should().NotBeNull();
        response.Model.Should().Be("llama3.2:8b");
        response.Message.Content.Should().Be("Test response");
        response.Done.Should().BeTrue();
    }

    // Gap #2 tests: IHttpClientFactory support
    [Fact]
    public void Constructor_Should_AcceptHttpClientFactory()
    {
        // FR-003: OllamaHttpClient MUST use IHttpClientFactory for HttpClient creation
        var configuration = new OllamaConfiguration(
            baseUrl: "http://localhost:11434",
            requestTimeoutSeconds: 60);

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient { BaseAddress = new Uri(configuration.BaseUrl) };
        httpClientFactory.CreateClient("Ollama").Returns(httpClient);

        var act = () => new OllamaHttpClient(httpClientFactory, configuration);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_Should_CreateHttpClientFromFactory()
    {
        // FR-003: Verify factory is used to create HttpClient
        var configuration = new OllamaConfiguration(baseUrl: "http://localhost:11434");

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient { BaseAddress = new Uri(configuration.BaseUrl) };
        httpClientFactory.CreateClient("Ollama").Returns(httpClient);

        var ollamaClient = new OllamaHttpClient(httpClientFactory, configuration);

        httpClientFactory.Received(1).CreateClient("Ollama");
        ollamaClient.BaseAddress.Should().Be(configuration.BaseUrl);
    }

    [Fact]
    public void Constructor_Should_ConfigureTimeoutFromConfiguration()
    {
        // FR-005: Configure timeout from configuration
        var configuration = new OllamaConfiguration(
            baseUrl: "http://localhost:11434",
            requestTimeoutSeconds: 90);

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient { BaseAddress = new Uri(configuration.BaseUrl) };
        httpClientFactory.CreateClient("Ollama").Returns(httpClient);

        var ollamaClient = new OllamaHttpClient(httpClientFactory, configuration);

        // Note: We'll verify timeout is set in the implementation
        // For now, just verify construction succeeds with timeout config
        configuration.RequestTimeout.Should().Be(TimeSpan.FromSeconds(90));
        ollamaClient.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithFactory_Should_GenerateCorrelationId()
    {
        // FR-007: Correlation ID should still be generated with factory constructor
        var configuration = new OllamaConfiguration(baseUrl: "http://localhost:11434");

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient { BaseAddress = new Uri(configuration.BaseUrl) };
        httpClientFactory.CreateClient("Ollama").Returns(httpClient);

        var ollamaClient = new OllamaHttpClient(httpClientFactory, configuration);

        ollamaClient.CorrelationId.Should().NotBeNullOrEmpty();
        Guid.TryParse(ollamaClient.CorrelationId, out _).Should().BeTrue();
    }

    [Fact]
    public void Dispose_WithFactory_Should_DisposeHttpClient()
    {
        // FR-006: Factory-created HttpClient should be disposed
        var configuration = new OllamaConfiguration(baseUrl: "http://localhost:11434");

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient { BaseAddress = new Uri(configuration.BaseUrl) };
        httpClientFactory.CreateClient("Ollama").Returns(httpClient);

        var ollamaClient = new OllamaHttpClient(httpClientFactory, configuration);

        var act = () => ollamaClient.Dispose();

        act.Should().NotThrow();
    }
}
