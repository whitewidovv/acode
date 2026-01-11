// tests/Acode.Infrastructure.Tests/Persistence/DatabaseRetryPolicyTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Persistence;

using Acode.Domain.Exceptions;
using Acode.Infrastructure.Configuration;
using Acode.Infrastructure.Persistence.Retry;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>
/// Tests for DatabaseRetryPolicy exponential backoff and retry logic.
/// Verifies transient error retry, permanent error fail-fast, and retry exhaustion.
/// </summary>
public sealed class DatabaseRetryPolicyTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnImmediately_WhenRetryDisabled()
    {
        // Arrange
        var options = CreateOptions(enabled: false);
        var policy = new DatabaseRetryPolicy(options, NullLogger<DatabaseRetryPolicy>.Instance);
        var callCount = 0;

        // Act
        var result = await policy.ExecuteAsync(
            async _ =>
            {
                callCount++;
                await Task.Yield();
                return 42;
            },
            CancellationToken.None);

        // Assert
        result.Should().Be(42);
        callCount.Should().Be(1); // Should execute only once when disabled
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetryTransientErrors()
    {
        // Arrange
        var options = CreateOptions(maxAttempts: 3);
        var policy = new DatabaseRetryPolicy(options, NullLogger<DatabaseRetryPolicy>.Instance);
        var callCount = 0;

        // Act
        var result = await policy.ExecuteAsync(
            async _ =>
            {
                callCount++;
                await Task.Yield();

                if (callCount < 3)
                {
                    throw DatabaseException.ConnectionFailed("Transient error");
                }

                return 42;
            },
            CancellationToken.None);

        // Assert
        result.Should().Be(42);
        callCount.Should().Be(3); // Should retry twice, succeed on third attempt
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotRetryPermanentErrors()
    {
        // Arrange
        var options = CreateOptions(maxAttempts: 3);
        var policy = new DatabaseRetryPolicy(options, NullLogger<DatabaseRetryPolicy>.Instance);
        var callCount = 0;

        // Act
        var act = async () => await policy.ExecuteAsync(
            async _ =>
            {
                callCount++;
                await Task.Yield();
                throw DatabaseException.SyntaxError("Permanent error");
            },
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DatabaseException>()
            .Where(ex => ex.ErrorCode == "ACODE-DB-ACC-006");
        callCount.Should().Be(1); // Should fail immediately on permanent error
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowAfterMaxAttempts()
    {
        // Arrange
        var options = CreateOptions(maxAttempts: 3);
        var policy = new DatabaseRetryPolicy(options, NullLogger<DatabaseRetryPolicy>.Instance);
        var callCount = 0;

        // Act
        var act = async () => await policy.ExecuteAsync(
            async _ =>
            {
                callCount++;
                await Task.Yield();
                throw DatabaseException.ConnectionFailed("Always fails");
            },
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DatabaseException>();
        callCount.Should().Be(3); // Should attempt exactly 3 times
    }

    [Fact]
    public async Task ExecuteAsync_VoidOverload_ShouldRetryTransientErrors()
    {
        // Arrange
        var options = CreateOptions(maxAttempts: 3);
        var policy = new DatabaseRetryPolicy(options, NullLogger<DatabaseRetryPolicy>.Instance);
        var callCount = 0;

        // Act
        await policy.ExecuteAsync(
            async _ =>
            {
                callCount++;
                await Task.Yield();

                if (callCount < 2)
                {
                    throw DatabaseException.PoolExhausted(TimeSpan.FromSeconds(1));
                }
            },
            CancellationToken.None);

        // Assert
        callCount.Should().Be(2); // Should retry once, succeed on second attempt
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectCancellation()
    {
        // Arrange
        var options = CreateOptions(maxAttempts: 3);
        var policy = new DatabaseRetryPolicy(options, NullLogger<DatabaseRetryPolicy>.Instance);
        var cts = new CancellationTokenSource();
        var callCount = 0;

        // Act
        var act = async () => await policy.ExecuteAsync(
            async _ =>
            {
                callCount++;
                await Task.Yield();

                if (callCount == 2)
                {
                    cts.Cancel();
                }

                throw DatabaseException.ConnectionFailed("Error");
            },
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        callCount.Should().BeLessThan(3); // Should stop before max attempts due to cancellation
    }

    [Fact]
    public async Task ExecuteAsync_ShouldApplyExponentialBackoff()
    {
        // Arrange
        var options = CreateOptions(maxAttempts: 3, baseDelayMs: 100, maxDelayMs: 1000);
        var policy = new DatabaseRetryPolicy(options, NullLogger<DatabaseRetryPolicy>.Instance);
        var callCount = 0;
        var delays = new List<TimeSpan>();
        var lastCallTime = DateTimeOffset.UtcNow;

        // Act
        await policy.ExecuteAsync(
            async _ =>
            {
                var now = DateTimeOffset.UtcNow;
                if (callCount > 0)
                {
                    delays.Add(now - lastCallTime);
                }

                lastCallTime = now;
                callCount++;
                await Task.Yield();

                if (callCount < 3)
                {
                    throw DatabaseException.CommandTimeout(TimeSpan.FromSeconds(30), "timeout");
                }

                return 1;
            },
            CancellationToken.None);

        // Assert
        delays.Should().HaveCount(2); // Two retries = two delays
        delays[0].TotalMilliseconds.Should().BeGreaterThan(100); // Base delay ~100ms with jitter
        delays[1].TotalMilliseconds.Should().BeGreaterThan(delays[0].TotalMilliseconds); // Exponential increase
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenOperationIsNull()
    {
        // Arrange
        var options = CreateOptions();
        var policy = new DatabaseRetryPolicy(options, NullLogger<DatabaseRetryPolicy>.Instance);

        // Act
        var act = async () => await policy.ExecuteAsync<int>(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Arrange & Act
        var act = () => new DatabaseRetryPolicy(null!, NullLogger<DatabaseRetryPolicy>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var act = () => new DatabaseRetryPolicy(options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    private static IOptions<DatabaseOptions> CreateOptions(
        bool enabled = true,
        int maxAttempts = 3,
        int baseDelayMs = 10,
        int maxDelayMs = 100)
    {
        return Options.Create(new DatabaseOptions
        {
            Retry = new RetryOptions
            {
                Enabled = enabled,
                MaxAttempts = maxAttempts,
                BaseDelayMs = baseDelayMs,
                MaxDelayMs = maxDelayMs,
            },
        });
    }
}
