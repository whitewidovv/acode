namespace Acode.Application.Tests.Providers;

using System;
using Acode.Application.Providers;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for RetryPolicy record.
/// Gap #19 from task-004c completion checklist.
/// </summary>
public sealed class RetryPolicyTests
{
    [Fact]
    public void Should_Have_Default_MaxAttempts()
    {
        // Arrange & Act
        var policy = new RetryPolicy();

        // Assert
        policy.MaxAttempts.Should().Be(3);
    }

    [Fact]
    public void Should_Have_Default_InitialDelay()
    {
        // Arrange & Act
        var policy = new RetryPolicy();

        // Assert
        policy.InitialDelay.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Should_Have_Default_MaxDelay()
    {
        // Arrange & Act
        var policy = new RetryPolicy();

        // Assert
        policy.MaxDelay.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Should_Have_Default_BackoffMultiplier()
    {
        // Arrange & Act
        var policy = new RetryPolicy();

        // Assert
        policy.BackoffMultiplier.Should().Be(2.0);
    }

    [Fact]
    public void Should_Allow_Custom_MaxAttempts()
    {
        // Arrange & Act
        var policy = new RetryPolicy(maxAttempts: 5);

        // Assert
        policy.MaxAttempts.Should().Be(5);
    }

    [Fact]
    public void Should_Allow_Custom_InitialDelay()
    {
        // Arrange & Act
        var policy = new RetryPolicy(initialDelay: TimeSpan.FromMilliseconds(500));

        // Assert
        policy.InitialDelay.Should().Be(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void Should_Allow_Custom_MaxDelay()
    {
        // Arrange & Act
        var policy = new RetryPolicy(maxDelay: TimeSpan.FromMinutes(1));

        // Assert
        policy.MaxDelay.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Should_Allow_Custom_BackoffMultiplier()
    {
        // Arrange & Act
        var policy = new RetryPolicy(backoffMultiplier: 1.5);

        // Assert
        policy.BackoffMultiplier.Should().Be(1.5);
    }

    [Fact]
    public void Should_Validate_MaxAttempts_NonNegative()
    {
        // Arrange & Act
        Action act = () => new RetryPolicy(maxAttempts: -1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*MaxAttempts must be >= 0*");
    }

    [Fact]
    public void Should_Allow_Zero_MaxAttempts()
    {
        // Arrange & Act
        var policy = new RetryPolicy(maxAttempts: 0);

        // Assert
        policy.MaxAttempts.Should().Be(0);
    }

    [Fact]
    public void Should_Validate_InitialDelay_Positive()
    {
        // Arrange & Act
        Action actZero = () => new RetryPolicy(initialDelay: TimeSpan.Zero);
        Action actNegative = () => new RetryPolicy(initialDelay: TimeSpan.FromSeconds(-1));

        // Assert
        actZero.Should().Throw<ArgumentException>()
            .WithMessage("*InitialDelay must be positive*");

        actNegative.Should().Throw<ArgumentException>()
            .WithMessage("*InitialDelay must be positive*");
    }

    [Fact]
    public void Should_Validate_MaxDelay_Positive()
    {
        // Arrange & Act
        Action actZero = () => new RetryPolicy(maxDelay: TimeSpan.Zero);
        Action actNegative = () => new RetryPolicy(maxDelay: TimeSpan.FromSeconds(-1));

        // Assert
        actZero.Should().Throw<ArgumentException>()
            .WithMessage("*MaxDelay must be positive*");

        actNegative.Should().Throw<ArgumentException>()
            .WithMessage("*MaxDelay must be positive*");
    }

    [Fact]
    public void Should_Validate_BackoffMultiplier_GreaterThanOrEqualOne()
    {
        // Arrange & Act
        Action actZero = () => new RetryPolicy(backoffMultiplier: 0.0);
        Action actNegative = () => new RetryPolicy(backoffMultiplier: -1.0);
        Action actBelowOne = () => new RetryPolicy(backoffMultiplier: 0.5);

        // Assert
        actZero.Should().Throw<ArgumentException>()
            .WithMessage("*BackoffMultiplier must be >= 1.0*");

        actNegative.Should().Throw<ArgumentException>()
            .WithMessage("*BackoffMultiplier must be >= 1.0*");

        actBelowOne.Should().Throw<ArgumentException>()
            .WithMessage("*BackoffMultiplier must be >= 1.0*");
    }

    [Fact]
    public void Should_Allow_BackoffMultiplier_EqualOne()
    {
        // Arrange & Act - 1.0 = linear backoff
        var policy = new RetryPolicy(backoffMultiplier: 1.0);

        // Assert
        policy.BackoffMultiplier.Should().Be(1.0);
    }

    [Fact]
    public void Should_Be_Immutable()
    {
        // Arrange
        var original = new RetryPolicy();

        // Act - use 'with' to create modified copy
        var modified = original with { MaxAttempts = 10 };

        // Assert - original unchanged
        original.MaxAttempts.Should().Be(3);
        modified.MaxAttempts.Should().Be(10);
        original.Should().NotBeSameAs(modified);
    }

    [Fact]
    public void Should_Support_All_Properties()
    {
        // Arrange & Act
        var policy = new RetryPolicy(
            maxAttempts: 5,
            initialDelay: TimeSpan.FromMilliseconds(100),
            maxDelay: TimeSpan.FromSeconds(10),
            backoffMultiplier: 2.5);

        // Assert
        policy.MaxAttempts.Should().Be(5);
        policy.InitialDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        policy.MaxDelay.Should().Be(TimeSpan.FromSeconds(10));
        policy.BackoffMultiplier.Should().Be(2.5);
    }

    [Fact]
    public void None_Should_Disable_Retries()
    {
        // Arrange & Act - RetryPolicy.None should have 0 max attempts
        var policy = RetryPolicy.None;

        // Assert
        policy.Should().NotBeNull();
        policy.MaxAttempts.Should().Be(0);
    }

    [Fact]
    public void None_Should_Be_Singleton()
    {
        // Arrange & Act - Multiple accesses should return same instance
        var first = RetryPolicy.None;
        var second = RetryPolicy.None;

        // Assert
        first.Should().BeSameAs(second);
    }
}
