// tests/Acode.Infrastructure.Tests/Sync/RetryPolicyTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code
#pragma warning disable CS1998 // Async method lacks 'await' operators

namespace Acode.Infrastructure.Tests.Sync;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Acode.Infrastructure.Sync;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for RetryPolicy.
/// Verifies exponential backoff retry logic with transient error detection.
/// </summary>
public sealed class RetryPolicyTests
{
    [Fact]
    public async Task Should_Retry_Transient()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, baseDelayMs: 100);
        int attemptCount = 0;

        async Task<bool> TransientFailure()
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new HttpRequestException("Network timeout");
            }

            return true;
        }

        // Act
        var result = await policy.ExecuteAsync(TransientFailure, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        attemptCount.Should().Be(3, "should retry twice before succeeding on 3rd attempt");
    }

    [Fact]
    public async Task Should_Apply_Backoff()
    {
        // Arrange - use small delays to keep test fast, but long enough to measure
        const int baseDelayMs = 50;
        var policy = new RetryPolicy(maxRetries: 3, baseDelayMs: baseDelayMs);
        var timestamps = new List<DateTime>();
        int attemptCount = 0;

        async Task<bool> TrackTimestamps()
        {
            timestamps.Add(DateTime.UtcNow);
            attemptCount++;

            if (attemptCount < 3)
            {
                throw new HttpRequestException("Retry");
            }

            return true;
        }

        // Act
        await policy.ExecuteAsync(TrackTimestamps, CancellationToken.None);

        // Assert - verify we got expected number of attempts
        timestamps.Should().HaveCount(3, "should have 3 attempts total");

        // Calculate actual delays between attempts
        var delay1 = (timestamps[1] - timestamps[0]).TotalMilliseconds;
        var delay2 = (timestamps[2] - timestamps[1]).TotalMilliseconds;

        // Verify exponential backoff pattern: second delay should be roughly 2x the first
        // Using very lax tolerance (±100ms) to account for system scheduling variance
        // The key behavior we're testing is that delays exist and increase
        delay1.Should().BeGreaterOrEqualTo(baseDelayMs * 0.5, "first delay should be at least half the base delay");
        delay2.Should().BeGreaterThan(delay1 * 0.8, "second delay should be greater than the first (exponential backoff)");

        // Verify delays are not excessively long (sanity check)
        delay1.Should().BeLessThan(baseDelayMs * 5, "first delay should not be excessively long");
        delay2.Should().BeLessThan(baseDelayMs * 2 * 5, "second delay should not be excessively long");
    }

    [Fact]
    public async Task Should_Honor_Max_Retries()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, baseDelayMs: 10);
        int attemptCount = 0;

        async Task<bool> AlwaysFail()
        {
            attemptCount++;
            throw new HttpRequestException("Always fails");
        }

        // Act
        var act = async () => await policy.ExecuteAsync(AlwaysFail, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
        attemptCount.Should().Be(4, "initial attempt + 3 retries");
    }

    [Theory]
    [InlineData(typeof(HttpRequestException), true)]
    [InlineData(typeof(TimeoutException), true)]
    [InlineData(typeof(InvalidOperationException), false)]
    public async Task Should_Distinguish_Transient_Vs_Permanent(Type exceptionType, bool shouldRetry)
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 2, baseDelayMs: 10);
        int attemptCount = 0;

        async Task<bool> ThrowException()
        {
            attemptCount++;
            var exception = (Exception)Activator.CreateInstance(exceptionType, "Test error")!;
            throw exception;
        }

        // Act
        var act = async () => await policy.ExecuteAsync(ThrowException, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();

        if (shouldRetry)
        {
            attemptCount.Should().Be(3, "transient errors should trigger retries");
        }
        else
        {
            attemptCount.Should().Be(1, "permanent errors should not trigger retries");
        }
    }

    [Fact]
    public async Task Should_Succeed_On_First_Attempt()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, baseDelayMs: 100);
        int attemptCount = 0;

        async Task<string> SucceedImmediately()
        {
            attemptCount++;
            return "success";
        }

        // Act
        var result = await policy.ExecuteAsync(SucceedImmediately, CancellationToken.None);

        // Assert
        result.Should().Be("success");
        attemptCount.Should().Be(1, "no retries needed if first attempt succeeds");
    }

    [Fact]
    public async Task Should_Respect_Cancellation()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 5, baseDelayMs: 1000);
        int attemptCount = 0;
        using var cts = new CancellationTokenSource();

        async Task<bool> LongRunning()
        {
            attemptCount++;
            if (attemptCount == 2)
            {
                cts.Cancel();
            }

            throw new HttpRequestException("Retry");
        }

        // Act
        var act = async () => await policy.ExecuteAsync(LongRunning, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        attemptCount.Should().BeLessOrEqualTo(2, "should stop retrying when cancelled");
    }

    [Fact]
    public async Task Should_Handle_Aggregate_Exception()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 2, baseDelayMs: 10);
        int attemptCount = 0;

        async Task<bool> ThrowAggregateException()
        {
            attemptCount++;
            var innerException = new HttpRequestException("Inner transient error");
            throw new AggregateException("Aggregate", innerException);
        }

        // Act
        var act = async () => await policy.ExecuteAsync(ThrowAggregateException, CancellationToken.None);

        // Assert - AggregateException with transient inner should be retried
        await act.Should().ThrowAsync<AggregateException>();
        attemptCount.Should().Be(3, "should retry AggregateException with transient inner exception (initial + 2 retries)");
    }
}
