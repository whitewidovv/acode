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
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, baseDelayMs: 100);
        var delays = new List<TimeSpan>();
        int attemptCount = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var lastElapsed = TimeSpan.Zero;

        async Task<bool> TrackDelays()
        {
            attemptCount++;
            if (attemptCount > 1)
            {
                var currentElapsed = stopwatch.Elapsed;
                delays.Add(currentElapsed - lastElapsed);
                lastElapsed = currentElapsed;
            }

            if (attemptCount < 3)
            {
                throw new HttpRequestException("Retry");
            }

            return true;
        }

        // Act
        await policy.ExecuteAsync(TrackDelays, CancellationToken.None);

        // Assert
        delays.Should().HaveCount(2);
        delays[0].TotalMilliseconds.Should().BeApproximately(100, 50, "first retry should be ~100ms");
        delays[1].TotalMilliseconds.Should().BeApproximately(200, 50, "second retry should be ~200ms with exponential backoff");
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
