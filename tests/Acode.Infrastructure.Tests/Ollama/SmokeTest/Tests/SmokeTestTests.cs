namespace Acode.Infrastructure.Tests.Ollama.SmokeTest.Tests;

using System.Net;
using System.Net.Http;
using Acode.Infrastructure.Ollama.SmokeTest;
using Acode.Infrastructure.Ollama.SmokeTest.Tests;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for individual smoke test implementations.
/// </summary>
public sealed class SmokeTestTests
{
    private readonly SmokeTestOptions defaultOptions = new()
    {
        Endpoint = "http://localhost:11434",
        Model = "llama3.2:latest",
        Timeout = TimeSpan.FromSeconds(30)
    };

    [Fact]
    public void HealthCheckTest_Name_IsCorrect()
    {
        // Arrange
        var test = new HealthCheckTest();

        // Act & Assert
        test.Name.Should().Be("Health Check");
    }

    [Fact]
    public async Task HealthCheckTest_Passes_WhenOllamaResponds()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, "{\"models\":[]}");
        var test = new HealthCheckTest(httpClient);

        // Act
        var result = await test.RunAsync(this.defaultOptions, CancellationToken.None);

        // Assert
        result.Passed.Should().BeTrue();
        result.TestName.Should().Be("Health Check");
        result.ElapsedTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task HealthCheckTest_Fails_WhenConnectionRefused()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(new HttpRequestException("Connection refused"));
        var test = new HealthCheckTest(httpClient);

        // Act
        var result = await test.RunAsync(this.defaultOptions, CancellationToken.None);

        // Assert
        result.Passed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection failed");
        result.DiagnosticHint.Should().Contain("ollama serve");
    }

    [Fact]
    public void ModelListTest_Name_IsCorrect()
    {
        // Arrange
        var test = new ModelListTest();

        // Act & Assert
        test.Name.Should().Be("Model List");
    }

    [Fact]
    public async Task ModelListTest_Passes_WhenModelsExist()
    {
        // Arrange
        var responseJson = "{\"models\":[{\"name\":\"llama3.2:latest\"}]}";
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var test = new ModelListTest(httpClient);

        // Act
        var result = await test.RunAsync(this.defaultOptions, CancellationToken.None);

        // Assert
        result.Passed.Should().BeTrue();
        result.TestName.Should().Be("Model List");
    }

    [Fact]
    public async Task ModelListTest_Fails_WhenNoModels()
    {
        // Arrange
        var responseJson = "{\"models\":[]}";
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var test = new ModelListTest(httpClient);

        // Act
        var result = await test.RunAsync(this.defaultOptions, CancellationToken.None);

        // Assert
        result.Passed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No models available");
        result.DiagnosticHint.Should().Contain("ollama pull");
    }

    [Fact]
    public void CompletionTest_Name_IsCorrect()
    {
        // Arrange
        var test = new CompletionTest();

        // Act & Assert
        test.Name.Should().Be("Non-Streaming Completion");
    }

    [Fact]
    public async Task CompletionTest_Passes_WhenCompletionSucceeds()
    {
        // Arrange
        var responseJson = "{\"response\":\"4\",\"done\":true}";
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var test = new CompletionTest(httpClient);

        // Act
        var result = await test.RunAsync(this.defaultOptions, CancellationToken.None);

        // Assert
        result.Passed.Should().BeTrue();
        result.TestName.Should().Be("Non-Streaming Completion");
    }

    [Fact]
    public async Task CompletionTest_Fails_WhenModelNotFound()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.NotFound, "model not found");
        var test = new CompletionTest(httpClient);

        // Act
        var result = await test.RunAsync(this.defaultOptions, CancellationToken.None);

        // Assert
        result.Passed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("404");
        result.DiagnosticHint.Should().Contain("ollama pull");
    }

    [Fact]
    public void StreamingTest_Name_IsCorrect()
    {
        // Arrange
        var test = new StreamingTest();

        // Act & Assert
        test.Name.Should().Be("Streaming Completion");
    }

    [Fact]
    public void ToolCallTest_Name_IsCorrect()
    {
        // Arrange
        var test = new ToolCallTest();

        // Act & Assert
        test.Name.Should().Be("Tool Calling");
    }

    [Fact]
    public async Task ToolCallTest_Returns_SkippedMessage()
    {
        // Arrange
        var test = new ToolCallTest();

        // Act
        var result = await test.RunAsync(this.defaultOptions, CancellationToken.None);

        // Assert
        result.Passed.Should().BeTrue("skipped tests should pass to not fail the suite");
        result.ErrorMessage.Should().Contain("SKIPPED");
        result.ErrorMessage.Should().Contain("Task 007d");
    }

    private static HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string content)
    {
        var handler = new MockHttpMessageHandler(statusCode, content);
        return new HttpClient(handler);
    }

    private static HttpClient CreateMockHttpClient(Exception exception)
    {
        var handler = new MockHttpMessageHandler(exception);
        return new HttpClient(handler);
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode? statusCode;
        private readonly string? content;
        private readonly Exception? exception;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            this.statusCode = statusCode;
            this.content = content;
        }

        public MockHttpMessageHandler(Exception exception)
        {
            this.exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (this.exception != null)
            {
                throw this.exception;
            }

            var response = new HttpResponseMessage(this.statusCode!.Value)
            {
                Content = new StringContent(this.content!)
            };

            return Task.FromResult(response);
        }
    }
}
