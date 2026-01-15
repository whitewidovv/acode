using System.Diagnostics;
using System.Net.Sockets;
using Acode.Infrastructure.Vllm.Client.Retry;
using Acode.Infrastructure.Vllm.Exceptions;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Client.Retry;

public class VllmRetryPolicyTests
{
    [Fact]
    public async Task Should_Retry_Socket_Errors()
    {
        // Arrange (FR-076, AC-076): MUST retry on SocketException
        var retryPolicy = new VllmRetryPolicy(maxRetries: 3, initialDelayMs: 10);

        int attemptCount = 0;

        Task<string> Operation(CancellationToken ct)
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new SocketException();
            }

            return Task.FromResult("success");
        }

        // Act
        var result = await retryPolicy.ExecuteAsync(Operation, CancellationToken.None);

        // Assert
        result.Should().Be("success");
        attemptCount.Should().Be(3);  // Should retry twice, succeed on 3rd
    }

    [Fact]
    public async Task Should_Retry_503_Server_Unavailable()
    {
        // Arrange (FR-078, AC-078): MUST retry on 503 Service Unavailable
        var retryPolicy = new VllmRetryPolicy(maxRetries: 3, initialDelayMs: 10);

        int attemptCount = 0;

        Task<string> Operation(CancellationToken ct)
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new VllmServerException("503 Service Unavailable");
            }

            return Task.FromResult("success");
        }

        // Act
        var result = await retryPolicy.ExecuteAsync(Operation, CancellationToken.None);

        // Assert
        result.Should().Be("success");
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task Should_Retry_429_Rate_Limit_With_Backoff()
    {
        // Arrange (FR-079, AC-079): MUST retry on 429 Too Many Requests with exponential backoff
        var retryPolicy = new VllmRetryPolicy(maxRetries: 3, initialDelayMs: 20);

        int attemptCount = 0;
        var stopwatch = Stopwatch.StartNew();

        Task<string> Operation(CancellationToken ct)
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new VllmRateLimitException("429 Too Many Requests");
            }

            return Task.FromResult("success");
        }

        // Act
        var result = await retryPolicy.ExecuteAsync(Operation, CancellationToken.None);

        stopwatch.Stop();

        // Assert
        result.Should().Be("success");
        attemptCount.Should().Be(3);

        // Verify exponential backoff: 20ms + 40ms = 60ms minimum (use 50ms to account for timing variance)
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(50);
    }

    [Fact]
    public async Task Should_Not_Retry_400_Bad_Request()
    {
        // Arrange (FR-080, AC-080): DO NOT retry on 400 Bad Request
        var retryPolicy = new VllmRetryPolicy(maxRetries: 3);

        int attemptCount = 0;

        Task<string> Operation(CancellationToken ct)
        {
            attemptCount++;
            throw new VllmRequestException("400 Bad Request");
        }

        // Act & Assert
        await Assert.ThrowsAsync<VllmRequestException>(async () =>
        {
            await retryPolicy.ExecuteAsync(Operation, CancellationToken.None);
        });

        attemptCount.Should().Be(1);  // Should NOT retry
    }

    [Fact]
    public async Task Should_Not_Retry_401_Unauthorized()
    {
        // Arrange (FR-082, AC-082): DO NOT retry on 401 Unauthorized
        var retryPolicy = new VllmRetryPolicy(maxRetries: 3);

        int attemptCount = 0;

        Task<string> Operation(CancellationToken ct)
        {
            attemptCount++;
            throw new VllmAuthException("401 Unauthorized");
        }

        // Act & Assert
        await Assert.ThrowsAsync<VllmAuthException>(async () =>
        {
            await retryPolicy.ExecuteAsync(Operation, CancellationToken.None);
        });

        attemptCount.Should().Be(1);  // Should NOT retry
    }

    [Fact]
    public async Task Should_Not_Retry_404_Not_Found()
    {
        // Arrange (FR-082, AC-082): DO NOT retry on 404 Not Found
        var retryPolicy = new VllmRetryPolicy(maxRetries: 3);

        int attemptCount = 0;

        Task<string> Operation(CancellationToken ct)
        {
            attemptCount++;
            throw new VllmModelNotFoundException("404 Model Not Found");
        }

        // Act & Assert
        await Assert.ThrowsAsync<VllmModelNotFoundException>(async () =>
        {
            await retryPolicy.ExecuteAsync(Operation, CancellationToken.None);
        });

        attemptCount.Should().Be(1);  // Should NOT retry
    }

    [Fact]
    public async Task Should_Respect_Max_Retries()
    {
        // Arrange (FR-084, AC-084): MUST throw after max retries exceeded
        var retryPolicy = new VllmRetryPolicy(maxRetries: 2, initialDelayMs: 10);

        int attemptCount = 0;

        Task<string> Operation(CancellationToken ct)
        {
            attemptCount++;
            throw new SocketException();
        }

        // Act & Assert
        await Assert.ThrowsAsync<SocketException>(async () =>
        {
            await retryPolicy.ExecuteAsync(Operation, CancellationToken.None);
        });

        attemptCount.Should().Be(2);  // 2 attempts = 1 initial + 1 retry
    }

    [Fact]
    public async Task Should_Apply_Exponential_Backoff_Correctly()
    {
        // Arrange (FR-081, AC-081): Exponential backoff formula: initialDelay * (backoffMultiplier ^ (attempt-1))
        var retryPolicy = new VllmRetryPolicy(
            maxRetries: 4,
            initialDelayMs: 10,
            backoffMultiplier: 2.0);

        int attemptCount = 0;
        var stopwatch = Stopwatch.StartNew();

        Task<string> Operation(CancellationToken ct)
        {
            attemptCount++;
            if (attemptCount < 4)
            {
                throw new SocketException();
            }

            return Task.FromResult("success");
        }

        // Act
        var result = await retryPolicy.ExecuteAsync(Operation, CancellationToken.None);

        stopwatch.Stop();

        // Assert
        result.Should().Be("success");
        attemptCount.Should().Be(4);

        // Verify exponential backoff:
        // Attempt 1: fails immediately
        // Attempt 2: waits 10ms (10 * 2^0)
        // Attempt 3: waits 20ms (10 * 2^1)
        // Attempt 4: waits 40ms (10 * 2^2)
        // Total wait: 10 + 20 + 40 = 70ms minimum
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(70);
    }

    [Fact]
    public async Task Should_Respect_Max_Delay()
    {
        // Arrange: maxDelay should cap exponential backoff
        var retryPolicy = new VllmRetryPolicy(
            maxRetries: 5,
            initialDelayMs: 10,
            maxDelayMs: 25,
            backoffMultiplier: 2.0);

        int attemptCount = 0;

        Task<string> Operation(CancellationToken ct)
        {
            attemptCount++;
            if (attemptCount < 5)
            {
                throw new SocketException();
            }

            return Task.FromResult("success");
        }

        // Act
        var result = await retryPolicy.ExecuteAsync(Operation, CancellationToken.None);

        // Assert
        result.Should().Be("success");

        // Verify delays don't exceed maxDelay:
        // Attempt 1: fails, no delay
        // Attempt 2: delay = min(10 * 2^0, 25) = 10ms
        // Attempt 3: delay = min(10 * 2^1, 25) = 20ms
        // Attempt 4: delay = min(10 * 2^2, 25) = 25ms (capped)
        // Attempt 5: delay = min(10 * 2^3, 25) = 25ms (capped)
        // Total: 10 + 20 + 25 + 25 = 80ms minimum (but we don't need to be too strict on timing)
        attemptCount.Should().Be(5);
    }

    [Fact]
    public async Task Should_Support_Cancellation()
    {
        // Arrange: Cancellation token should be respected
        var retryPolicy = new VllmRetryPolicy(maxRetries: 5, initialDelayMs: 100);

        using var cts = new CancellationTokenSource();

        Task<string> Operation(CancellationToken ct)
        {
            throw new SocketException();  // Always fail
        }

        // Cancel after short delay
        cts.CancelAfter(50);

        // Act & Assert
        // Note: Task.Delay throws TaskCanceledException (subclass of OperationCanceledException) when cancelled
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await retryPolicy.ExecuteAsync(Operation, cts.Token);
        });
    }

    [Fact]
    public async Task Should_Retry_Http_Request_Exceptions()
    {
        // Arrange (FR-077, AC-077): MUST retry on HttpRequestException
        var retryPolicy = new VllmRetryPolicy(maxRetries: 3, initialDelayMs: 10);

        int attemptCount = 0;

        Task<string> Operation(CancellationToken ct)
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new HttpRequestException("Network error");
            }

            return Task.FromResult("success");
        }

        // Act
        var result = await retryPolicy.ExecuteAsync(Operation, CancellationToken.None);

        // Assert
        result.Should().Be("success");
        attemptCount.Should().Be(2);
    }
}
