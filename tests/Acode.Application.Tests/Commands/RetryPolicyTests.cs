using Acode.Application.Commands;
using FluentAssertions;

namespace Acode.Application.Tests.Commands;

/// <summary>
/// Tests for RetryPolicy class.
/// Covers retry logic per Task 002.c spec lines 1032, FR-002c-103 through FR-002c-110.
/// </summary>
public class RetryPolicyTests
{
    // UT-002c-20: Calculate backoff delay → Exponential backoff correct
    [Theory]
    [InlineData(1, 1)] // First attempt: 2^0 = 1 second
    [InlineData(2, 2)] // Second attempt: 2^1 = 2 seconds
    [InlineData(3, 4)] // Third attempt: 2^2 = 4 seconds
    [InlineData(4, 8)] // Fourth attempt: 2^3 = 8 seconds
    [InlineData(5, 16)] // Fifth attempt: 2^4 = 16 seconds
    [InlineData(6, 30)] // Sixth attempt: 2^5 = 32, capped at 30 seconds
    [InlineData(7, 30)] // Seventh attempt: capped at 30 seconds
    [InlineData(10, 30)] // Tenth attempt: capped at 30 seconds
    public void CalculateDelay_WithAttemptNumber_ReturnsExponentialBackoff(int attemptNumber, int expectedSeconds)
    {
        // Act
        var delay = RetryPolicy.CalculateDelay(attemptNumber);

        // Assert
        delay.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public void CalculateDelay_WithAttempt1_Returns1Second()
    {
        // Act
        var delay = RetryPolicy.CalculateDelay(1);

        // Assert
        delay.Should().Be(TimeSpan.FromSeconds(1), "first retry should wait 1 second");
    }

    [Fact]
    public void CalculateDelay_WithHighAttempt_IsCappedAt30Seconds()
    {
        // Act
        var delay = RetryPolicy.CalculateDelay(100);

        // Assert
        delay.Should().Be(TimeSpan.FromSeconds(30), "delay should be capped at 30 seconds");
    }

    [Fact]
    public void ShouldRetry_WithExitCode0_ReturnsFalse()
    {
        // Arrange
        var exitCode = 0; // Success
        var attemptCount = 1;
        var maxRetries = 3;

        // Act
        var shouldRetry = RetryPolicy.ShouldRetry(exitCode, attemptCount, maxRetries);

        // Assert
        shouldRetry.Should().BeFalse("should not retry on success");
    }

    [Fact]
    public void ShouldRetry_WithNonZeroExitCodeAndRetriesRemaining_ReturnsTrue()
    {
        // Arrange
        var exitCode = 1; // Failure
        var attemptCount = 1; // First attempt
        var maxRetries = 3; // Allow 3 retries

        // Act
        var shouldRetry = RetryPolicy.ShouldRetry(exitCode, attemptCount, maxRetries);

        // Assert
        shouldRetry.Should().BeTrue("should retry on failure when retries remaining");
    }

    [Fact]
    public void ShouldRetry_WithNonZeroExitCodeAndNoRetriesRemaining_ReturnsFalse()
    {
        // Arrange
        var exitCode = 1; // Failure
        var attemptCount = 4; // Already made 4 attempts (initial + 3 retries)
        var maxRetries = 3; // Max 3 retries

        // Act
        var shouldRetry = RetryPolicy.ShouldRetry(exitCode, attemptCount, maxRetries);

        // Assert
        shouldRetry.Should().BeFalse("should not retry when max retries exceeded");
    }

    [Fact]
    public void ShouldRetry_WithZeroMaxRetries_ReturnsFalse()
    {
        // Arrange
        var exitCode = 1; // Failure
        var attemptCount = 1;
        var maxRetries = 0; // No retries allowed

        // Act
        var shouldRetry = RetryPolicy.ShouldRetry(exitCode, attemptCount, maxRetries);

        // Assert
        shouldRetry.Should().BeFalse("should not retry when max retries is 0");
    }
}
