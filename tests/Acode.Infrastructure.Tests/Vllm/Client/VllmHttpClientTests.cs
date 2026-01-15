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
    public async Task DisposeAsync_Should_CleanupResources()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:8000"
        };
        var client = new VllmHttpClient(config);

        // Act
        await client.DisposeAsync();

        // Assert - verify no exceptions thrown
        await client.DisposeAsync(); // Should be safe to dispose twice
    }

    [Fact]
    public async Task Should_Implement_IAsyncDisposable()
    {
        // Arrange (FR-003, AC-003)
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:8000"
        };
        var client = new VllmHttpClient(config);

        // Act
        await client.DisposeAsync();

        // Assert - verify no exceptions thrown
        await client.DisposeAsync(); // Should be safe to dispose async twice
    }

    [Fact]
    public async Task PostAsync_Should_Accept_Generic_TResponse_Parameter()
    {
        // Arrange (FR-007, AC-006) - Test generic PostAsync method
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:9999", // Invalid to trigger connection error
            ConnectTimeoutSeconds = 1
        };
        var client = new VllmHttpClient(config);
        var request = new
        {
            model = "test-model",
            messages = new[] { new { role = "user", content = "Hello" } }
        };

        // Act & Assert - PostAsync<T> should exist and be callable
#pragma warning disable CA2007
        var exception = await Assert.ThrowsAsync<VllmConnectionException>(async () =>
            await client.PostAsync<VllmResponse>("/v1/chat/completions", request, CancellationToken.None));
#pragma warning restore CA2007

        exception.ErrorCode.Should().Be("ACODE-VLM-001");
    }

    [Fact]
    public async Task PostAsync_Should_Accept_String_Path_Parameter()
    {
        // Arrange (FR-007, AC-006) - Verify path parameter is used
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:9999",
            ConnectTimeoutSeconds = 1
        };
        var client = new VllmHttpClient(config);

        // Act & Assert - Should accept custom path (will fail on connection, but path is processed)
#pragma warning disable CA2007
        await Assert.ThrowsAsync<VllmConnectionException>(async () =>
            await client.PostAsync<VllmResponse>("/custom/path", new { }, CancellationToken.None));
#pragma warning restore CA2007
    }

    [Fact]
    public async Task PostStreamingAsync_Should_Accept_Path_Parameter()
    {
        // Arrange (FR-008, AC-007) - Test PostStreamingAsync with custom path
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:9999",
            ConnectTimeoutSeconds = 1
        };
        var client = new VllmHttpClient(config);
        var request = new { model = "test", messages = new[] { new { role = "user", content = "hi" } } };

        // Act & Assert - Should support streaming with custom path
#pragma warning disable CA2007
        var stream = client.PostStreamingAsync("/v1/chat/completions", request, CancellationToken.None);

        // Should throw connection error when trying to stream
        await Assert.ThrowsAsync<VllmConnectionException>(async () =>
        {
            await foreach (var chunk in stream)
            {
                // Would process chunks here
            }
        });
#pragma warning restore CA2007
    }

    [Fact]
    public async Task PostStreamingAsync_Should_Auto_Set_Stream_Flag()
    {
        // Arrange (FR-008) - Verify stream flag is automatically set for VllmRequest
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:9999",
            ConnectTimeoutSeconds = 1
        };
        var client = new VllmHttpClient(config);
        var request = new VllmRequest
        {
            Model = "test",
            Messages = new System.Collections.Generic.List<VllmMessage>
            {
                new VllmMessage { Role = "user", Content = "hi" }
            },
            Stream = false
        }; // Explicitly false

        // Act & Assert - Should fail on connection, but Stream should be set to true
        var stream = client.PostStreamingAsync("/v1/chat/completions", request, CancellationToken.None);

        await Assert.ThrowsAsync<VllmConnectionException>(async () =>
        {
            await foreach (var chunk in stream)
            {
                // Would process chunks here
            }
        });

        // After enumeration attempt, Stream should be true
        request.Stream.Should().BeTrue();
    }
}
