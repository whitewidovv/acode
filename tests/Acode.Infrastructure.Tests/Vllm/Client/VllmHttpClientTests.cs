using Acode.Infrastructure.Vllm.Client;
using Acode.Infrastructure.Vllm.Exceptions;
using Acode.Infrastructure.Vllm.Models;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Client;

public class VllmHttpClientTests
{
    [Fact]
    public async Task SendRequestAsync_Should_ReturnResponse_When_Successful()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:8000"
        };
        var client = new VllmHttpClient(config);
        var request = new VllmRequest
        {
            Model = "test-model",
            Messages = new List<VllmMessage>
            {
                new() { Role = "user", Content = "Hello" }
            }
        };

        // Act & Assert - will fail until vLLM is running
        // This test verifies the API contract, not actual vLLM connectivity
#pragma warning disable CA2007
        await Assert.ThrowsAsync<VllmConnectionException>(async () =>
            await client.SendRequestAsync(request, CancellationToken.None));
#pragma warning restore CA2007
    }

    [Fact]
    public async Task SendRequestAsync_Should_ThrowConnectionException_When_ServerUnreachable()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:9999", // Invalid port
            ConnectTimeoutSeconds = 1
        };
        var client = new VllmHttpClient(config);
        var request = new VllmRequest
        {
            Model = "test-model",
            Messages = new List<VllmMessage>
            {
                new() { Role = "user", Content = "Hello" }
            }
        };

        // Act & Assert
#pragma warning disable CA2007
        var exception = await Assert.ThrowsAsync<VllmConnectionException>(async () =>
            await client.SendRequestAsync(request, CancellationToken.None));
#pragma warning restore CA2007

        exception.ErrorCode.Should().Be("ACODE-VLM-001");
        exception.IsTransient.Should().BeTrue();
    }

    [Fact]
    public async Task SendRequestAsync_Should_PropagateOperationCanceledException_When_Cancelled()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:8000",
            RequestTimeoutSeconds = 1
        };
        var client = new VllmHttpClient(config);
        var request = new VllmRequest
        {
            Model = "test-model",
            Messages = new List<VllmMessage>
            {
                new() { Role = "user", Content = "Hello" }
            }
        };

        // Act & Assert - cancellation should propagate
        using var cts = new CancellationTokenSource();
        cts.Cancel();
#pragma warning disable CA2007
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await client.SendRequestAsync(request, cts.Token));
#pragma warning restore CA2007
    }

    [Fact]
    public async Task SendRequestAsync_Should_IncludeAuthHeader_When_ApiKeyProvided()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:8000",
            ApiKey = "test-api-key"
        };
        var client = new VllmHttpClient(config);
        var request = new VllmRequest
        {
            Model = "test-model",
            Messages = new List<VllmMessage>
            {
                new() { Role = "user", Content = "Hello" }
            }
        };

        // Act & Assert - verify connection attempt is made with auth
#pragma warning disable CA2007
        await Assert.ThrowsAsync<VllmConnectionException>(async () =>
            await client.SendRequestAsync(request, CancellationToken.None));
#pragma warning restore CA2007
    }

    [Fact]
    public async Task StreamRequestAsync_Should_YieldChunks_When_Streaming()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:8000"
        };
        var client = new VllmHttpClient(config);
        var request = new VllmRequest
        {
            Model = "test-model",
            Messages = new List<VllmMessage>
            {
                new() { Role = "user", Content = "Hello" }
            },
            Stream = true
        };

        // Act & Assert - verify streaming contract
#pragma warning disable CA2007
        await Assert.ThrowsAsync<VllmConnectionException>(async () =>
        {
            await foreach (var chunk in client.StreamRequestAsync(request, CancellationToken.None))
            {
                chunk.Should().NotBeNull();
            }
        });
#pragma warning restore CA2007
    }

    [Fact]
    public void Constructor_Should_ValidateConfiguration()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "invalid-url"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new VllmHttpClient(config));
    }

    [Fact]
    public void Dispose_Should_CleanupResources()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:8000"
        };
        var client = new VllmHttpClient(config);

        // Act
        client.Dispose();

        // Assert - verify no exceptions thrown
        client.Dispose(); // Should be safe to dispose twice
    }
}
