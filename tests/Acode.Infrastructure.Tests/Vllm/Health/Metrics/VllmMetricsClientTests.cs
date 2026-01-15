using System.Net;
using Acode.Infrastructure.Vllm.Health.Metrics;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Health.Metrics;

/// <summary>
/// Tests for VllmMetricsClient.
/// </summary>
public class VllmMetricsClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler = new(HttpStatusCode.OK, "vllm_num_requests_running 5");
    private readonly HttpClient _httpClient;

    public VllmMetricsClientTests()
    {
        _httpClient = new HttpClient(_mockHandler) { BaseAddress = new Uri("http://localhost:8000") };
    }

    [Fact]
    public async Task Should_Query_Metrics_Endpoint()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "vllm_num_requests_running 5\nvllm_num_requests_waiting 2\nvllm_gpu_cache_usage_perc 45.0");
        var client = new VllmMetricsClient("http://localhost:8000", "/metrics");
        client.SetHttpClientForTesting(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") });

        // Act
        var metrics = await client.GetMetricsAsync(CancellationToken.None);

        // Assert
        metrics.Should().NotBeNullOrEmpty();
        metrics.Should().Contain("vllm_num_requests_running");
        metrics.Should().Contain("5");
    }

    [Fact]
    public async Task Should_Return_Empty_On_Connection_Failure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(null);  // Simulate connection failure
        var client = new VllmMetricsClient("http://localhost:8000", "/metrics");
        client.SetHttpClientForTesting(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") });

        // Act
        var metrics = await client.GetMetricsAsync(CancellationToken.None);

        // Assert
        metrics.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Return_Empty_On_Non_200()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, "Not Found");
        var client = new VllmMetricsClient("http://localhost:8000", "/metrics");
        client.SetHttpClientForTesting(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") });

        // Act
        var metrics = await client.GetMetricsAsync(CancellationToken.None);

        // Assert
        metrics.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Not_Throw_Exceptions()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(null);
        var client = new VllmMetricsClient("http://localhost:8000", "/metrics");
        client.SetHttpClientForTesting(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") });

        // Act
        var action = async () => await client.GetMetricsAsync(CancellationToken.None);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_Use_Custom_Metrics_Endpoint()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "vllm_num_requests_running 10");
        var client = new VllmMetricsClient("http://localhost:8000", "/custom/metrics");
        client.SetHttpClientForTesting(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") });

        // Act
        var metrics = await client.GetMetricsAsync(CancellationToken.None);

        // Assert
        metrics.Should().Contain("10");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHandler?.Dispose();
    }
}

/// <summary>
/// Mock HTTP message handler for testing.
/// </summary>
internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode? _statusCode;
    private readonly string _content;

    public MockHttpMessageHandler(HttpStatusCode? statusCode, string content = "")
    {
        _statusCode = statusCode;
        _content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_statusCode == null)
        {
            throw new HttpRequestException("Connection failed");
        }

        return Task.FromResult(new HttpResponseMessage(_statusCode.Value)
        {
            Content = new StringContent(_content)
        });
    }
}
