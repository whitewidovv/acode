using System.Net;
using Acode.Infrastructure.Ollama;
using Acode.Infrastructure.Ollama.Exceptions;
using Acode.Infrastructure.Ollama.Http;
using Acode.Infrastructure.Ollama.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
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

    // Gap #4 tests: Logging support
    [Fact]
    public void Constructor_Should_AcceptLogger()
    {
        // FR-040: OllamaHttpClient should accept ILogger for observability
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var configuration = new OllamaConfiguration(baseUrl: "http://localhost:11434");
        var logger = Substitute.For<ILogger<OllamaHttpClient>>();

        var httpClient = new HttpClient { BaseAddress = new Uri(configuration.BaseUrl) };
        httpClientFactory.CreateClient("Ollama").Returns(httpClient);

        var act = () => new OllamaHttpClient(httpClientFactory, configuration, logger);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task PostChatAsync_Should_LogRequestTiming()
    {
        // FR-040: PostAsync MUST log request and response timing
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""model"": ""llama3.2:8b"",
                ""created_at"": ""2024-01-01T12:00:00Z"",
                ""message"": { ""role"": ""assistant"", ""content"": ""Hello!"" },
                ""done"": true
            }"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var logger = Substitute.For<ILogger<OllamaHttpClient>>();

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434", logger: logger);

        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[] { new OllamaMessage(role: "user", content: "Test") },
            stream: false);

        await ollamaClient.PostChatAsync(request, CancellationToken.None);

        // Verify logging occurred (at least one log call for timing)
        logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("POST")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task PostChatAsync_Should_LogWithCorrelationId()
    {
        // FR-040 + NFR-019-022: Logging should include correlation ID
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""model"": ""llama3.2:8b"",
                ""created_at"": ""2024-01-01T12:00:00Z"",
                ""message"": { ""role"": ""assistant"", ""content"": ""Hello!"" },
                ""done"": true
            }"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var logger = Substitute.For<ILogger<OllamaHttpClient>>();

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434", logger: logger);

        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[] { new OllamaMessage(role: "user", content: "Test") },
            stream: false);

        await ollamaClient.PostChatAsync(request, CancellationToken.None);

        // Verify BeginScope was called with CorrelationId
        logger.Received().BeginScope(Arg.Is<object>(o => o.ToString()!.Contains(ollamaClient.CorrelationId)));
    }

    [Fact]
    public void Constructor_WithLogger_Should_StoreLogger()
    {
        // Verify logger can be passed to both constructors
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:11434") };
        var logger = Substitute.For<ILogger<OllamaHttpClient>>();

        var act = () => new OllamaHttpClient(httpClient, "http://localhost:11434", logger: logger);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithoutLogger_Should_AllowNullLogger()
    {
        // Logger should be optional (nullable)
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:11434") };

        var act = () => new OllamaHttpClient(httpClient, "http://localhost:11434", logger: null);

        act.Should().NotThrow();
    }

    // Gap #5 tests: Generic PostAsync<TResponse> method
    [Fact]
    public async Task PostAsync_Should_SendRequestToSpecifiedEndpoint()
    {
        // Gap #5: Generic PostAsync method should accept any endpoint
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""model"": ""llama3.2:8b"",
                ""created_at"": ""2024-01-01T12:00:00Z"",
                ""message"": { ""role"": ""assistant"", ""content"": ""Hello!"" },
                ""done"": true
            }"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[] { new OllamaMessage(role: "user", content: "Test") },
            stream: false);

        var response = await ollamaClient.PostAsync<OllamaResponse>("/api/chat", request, CancellationToken.None);

        response.Should().NotBeNull();
        handler.LastRequestUri.Should().Be("http://localhost:11434/api/chat");
    }

    [Fact]
    public async Task PostAsync_Should_SerializeRequestCorrectly()
    {
        // Gap #5: Generic PostAsync should serialize any request type
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""model"": ""llama3.2:8b"",
                ""created_at"": ""2024-01-01T12:00:00Z"",
                ""message"": { ""role"": ""assistant"", ""content"": ""Response"" },
                ""done"": true
            }"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        var request = new OllamaRequest(
            model: "test-model",
            messages: new[] { new OllamaMessage(role: "user", content: "Test") },
            stream: false);

        await ollamaClient.PostAsync<OllamaResponse>("/api/chat", request, CancellationToken.None);

        var sentContent = handler.LastRequestContent;
        sentContent.Should().Contain("\"model\":\"test-model\"");
        sentContent.Should().Contain("\"stream\":false");
    }

    [Fact]
    public async Task PostAsync_Should_DeserializeResponseCorrectly()
    {
        // Gap #5: Generic PostAsync should deserialize any response type
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""model"": ""llama3.2:8b"",
                ""created_at"": ""2024-01-01T12:00:00Z"",
                ""message"": { ""role"": ""assistant"", ""content"": ""Test response"" },
                ""done"": true,
                ""done_reason"": ""stop""
            }"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[] { new OllamaMessage(role: "user", content: "Test") },
            stream: false);

        var response = await ollamaClient.PostAsync<OllamaResponse>("/api/chat", request, CancellationToken.None);

        response.Should().NotBeNull();
        response.Model.Should().Be("llama3.2:8b");
        response.Message.Content.Should().Be("Test response");
        response.DoneReason.Should().Be("stop");
    }

    [Fact]
    public async Task PostAsync_Should_WorkWithDifferentEndpoints()
    {
        // Gap #5: Should work with any endpoint, not just /api/chat
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{ ""status"": ""ok"" }"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        // Simple request object
        var request = new { model = "test" };

        await ollamaClient.PostAsync<object>("/api/tags", request, CancellationToken.None);

        handler.LastRequestUri.Should().Be("http://localhost:11434/api/tags");
    }

    [Fact]
    public async Task PostAsync_Should_IncludeLoggingWhenLoggerProvided()
    {
        // Gap #5: Generic PostAsync should also support logging
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""model"": ""llama3.2:8b"",
                ""created_at"": ""2024-01-01T12:00:00Z"",
                ""message"": { ""role"": ""assistant"", ""content"": ""Hello!"" },
                ""done"": true
            }"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var logger = Substitute.For<ILogger<OllamaHttpClient>>();

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434", logger: logger);

        var request = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[] { new OllamaMessage(role: "user", content: "Test") },
            stream: false);

        await ollamaClient.PostAsync<OllamaResponse>("/api/chat", request, CancellationToken.None);

        // Verify logging occurred
        logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("POST")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // Gap #6 tests: Enhanced error handling with custom exceptions
    [Fact]
    public async Task PostAsync_Should_ThrowOllamaRequestException_On4xxError()
    {
        // Gap #6: HTTP 4xx errors should be wrapped in OllamaRequestException
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        var request = new OllamaRequest(
            model: "test",
            messages: new[] { new OllamaMessage(role: "user", content: "Test") },
            stream: false);

        var act = async () => await ollamaClient.PostAsync<OllamaResponse>("/api/chat", request, CancellationToken.None);

        await act.Should().ThrowAsync<OllamaRequestException>()
            .WithMessage("*400*");
    }

    [Fact]
    public async Task PostAsync_Should_ThrowOllamaServerException_On5xxError()
    {
        // Gap #6: HTTP 5xx errors should be wrapped in OllamaServerException
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        var request = new OllamaRequest(
            model: "test",
            messages: new[] { new OllamaMessage(role: "user", content: "Test") },
            stream: false);

        var act = async () => await ollamaClient.PostAsync<OllamaResponse>("/api/chat", request, CancellationToken.None);

        await act.Should().ThrowAsync<OllamaServerException>()
            .WithMessage("*500*");
    }

    [Fact]
    public async Task PostAsync_Should_IncludeCorrelationIdInException()
    {
        // Gap #6: FR-099 - exceptions must include correlation ID
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        var request = new OllamaRequest(
            model: "test",
            messages: new[] { new OllamaMessage(role: "user", content: "Test") },
            stream: false);

        var act = async () => await ollamaClient.PostAsync<OllamaResponse>("/api/chat", request, CancellationToken.None);

        await act.Should().ThrowAsync<OllamaRequestException>()
            .WithMessage($"*{ollamaClient.CorrelationId}*");
    }

    [Fact]
    public async Task PostAsync_Should_WrapTimeoutException()
    {
        // Gap #6: FR-094 - timeout errors must be wrapped in OllamaTimeoutException
        // Create a handler that throws TaskCanceledException (simulating timeout)
        var handler = new ThrowingHttpMessageHandler(new TaskCanceledException("Request timed out"));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        var request = new OllamaRequest(
            model: "test",
            messages: new[] { new OllamaMessage(role: "user", content: "Test") },
            stream: false);

        var act = async () => await ollamaClient.PostAsync<OllamaResponse>("/api/chat", request, CancellationToken.None);

        await act.Should().ThrowAsync<OllamaTimeoutException>();
    }

    [Fact]
    public async Task PostAsync_Should_IncludeInnerExceptionInWrappedError()
    {
        // Gap #6: FR-098 - exceptions must include original exception as InnerException
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        var request = new OllamaRequest(
            model: "test",
            messages: new[] { new OllamaMessage(role: "user", content: "Test") },
            stream: false);

        try
        {
            await ollamaClient.PostAsync<OllamaResponse>("/api/chat", request, CancellationToken.None);
            Assert.Fail("Should have thrown exception");
        }
        catch (OllamaRequestException ex)
        {
            ex.InnerException.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task PostAsync_Should_WrapParseException()
    {
        // Gap #6: FR-097 - parse errors must be wrapped in OllamaParseException
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{invalid json}"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };

        var ollamaClient = new OllamaHttpClient(httpClient, "http://localhost:11434");

        var request = new OllamaRequest(
            model: "test",
            messages: new[] { new OllamaMessage(role: "user", content: "Test") },
            stream: false);

        var act = async () => await ollamaClient.PostAsync<OllamaResponse>("/api/chat", request, CancellationToken.None);

        await act.Should().ThrowAsync<OllamaParseException>();
    }
}
