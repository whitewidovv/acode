namespace Acode.Infrastructure.Tests.Ollama.Health;

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Acode.Infrastructure.Ollama.Health;
using Acode.Infrastructure.Tests.Ollama.Http;
using FluentAssertions;

/// <summary>
/// Tests for <see cref="OllamaHealthChecker"/>.
/// </summary>
/// <remarks>
/// FR-005-054 to FR-005-061: Health check implementation and behavior.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "xUnit1030: Test methods should not use ConfigureAwait(false)")]
public sealed class OllamaHealthCheckerTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenServerResponds_ReturnsHealthy()
    {
        // Arrange
        var response = new
        {
            models = new[]
            {
                new { name = "llama3.2:latest", size = 2000000000L },
            },
        };

        var httpClient = CreateHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));
        var healthChecker = new OllamaHealthChecker(httpClient, "http://localhost:11434");

        // Act
        var isHealthy = await healthChecker.CheckHealthAsync(CancellationToken.None);

        // Assert
        isHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenServerReturnsError_ReturnsUnhealthy()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.InternalServerError, "Server error");
        var healthChecker = new OllamaHealthChecker(httpClient, "http://localhost:11434");

        // Act
        var isHealthy = await healthChecker.CheckHealthAsync(CancellationToken.None);

        // Assert
        isHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionFails_ReturnsUnhealthy()
    {
        // Arrange
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
        var httpClient = new HttpClient(handler);
        var healthChecker = new OllamaHealthChecker(httpClient, "http://localhost:11434");

        // Act
        var isHealthy = await healthChecker.CheckHealthAsync(CancellationToken.None);

        // Assert
        isHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTimeout_ReturnsUnhealthy()
    {
        // Arrange
        var handler = new DelayingHttpMessageHandler(TimeSpan.FromSeconds(10));
        var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(100) };
        var healthChecker = new OllamaHealthChecker(httpClient, "http://localhost:11434");

        // Act
        var isHealthy = await healthChecker.CheckHealthAsync(CancellationToken.None);

        // Assert
        isHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ReturnsUnhealthy()
    {
        // Arrange
        var handler = new DelayingHttpMessageHandler(TimeSpan.FromSeconds(10));
        var httpClient = new HttpClient(handler);
        var healthChecker = new OllamaHealthChecker(httpClient, "http://localhost:11434");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var isHealthy = await healthChecker.CheckHealthAsync(cts.Token);

        // Assert
        isHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task CheckHealthAsync_CallsCorrectEndpoint()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"models\": []}"),
        });
        var httpClient = new HttpClient(handler);
        var healthChecker = new OllamaHealthChecker(httpClient, "http://localhost:11434");

        // Act
        await healthChecker.CheckHealthAsync(CancellationToken.None);

        // Assert
        handler.LastRequestUri.Should().NotBeNull();
        handler.LastRequestUri!.ToString().Should().Be("http://localhost:11434/api/tags");
    }

    [Fact]
    public async Task CheckHealthAsync_NeverThrowsException()
    {
        // Arrange
        var handler = new ThrowingHttpMessageHandler(new InvalidOperationException("Unexpected error"));
        var httpClient = new HttpClient(handler);
        var healthChecker = new OllamaHealthChecker(httpClient, "http://localhost:11434");

        // Act
        var act = async () => await healthChecker.CheckHealthAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        var isHealthy = await act();
        isHealthy.Should().BeFalse();
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
