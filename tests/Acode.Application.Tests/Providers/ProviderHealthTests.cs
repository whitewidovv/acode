namespace Acode.Application.Tests.Providers;

using System;
using Acode.Application.Providers;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ProviderHealth record.
/// Gap #20 from task-004c completion checklist.
/// </summary>
public sealed class ProviderHealthTests
{
    [Fact]
    public void Should_Default_To_Unknown_Status()
    {
        // Arrange & Act
        var health = new ProviderHealth();

        // Assert
        health.Status.Should().Be(HealthStatus.Unknown);
    }

    [Fact]
    public void Should_Track_Healthy_Status()
    {
        // Arrange & Act
        var health = new ProviderHealth(status: HealthStatus.Healthy);

        // Assert
        health.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void Should_Track_Degraded_Status()
    {
        // Arrange & Act
        var health = new ProviderHealth(status: HealthStatus.Degraded);

        // Assert
        health.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public void Should_Track_Unhealthy_Status()
    {
        // Arrange & Act
        var health = new ProviderHealth(status: HealthStatus.Unhealthy);

        // Assert
        health.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public void Should_Track_LastChecked_Timestamp()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var health = new ProviderHealth(
            status: HealthStatus.Healthy,
            lastChecked: timestamp);

        // Assert
        health.LastChecked.Should().Be(timestamp);
    }

    [Fact]
    public void Should_Allow_Null_LastChecked()
    {
        // Arrange & Act
        var health = new ProviderHealth();

        // Assert
        health.LastChecked.Should().BeNull();
    }

    [Fact]
    public void Should_Record_Error_Message()
    {
        // Arrange & Act
        var health = new ProviderHealth(
            status: HealthStatus.Unhealthy,
            lastError: "Connection timeout");

        // Assert
        health.LastError.Should().Be("Connection timeout");
    }

    [Fact]
    public void Should_Allow_Null_Error_Message()
    {
        // Arrange & Act
        var health = new ProviderHealth(status: HealthStatus.Healthy);

        // Assert
        health.LastError.Should().BeNull();
    }

    [Fact]
    public void Should_Record_Consecutive_Failures()
    {
        // Arrange & Act
        var health = new ProviderHealth(
            status: HealthStatus.Unhealthy,
            consecutiveFailures: 3);

        // Assert
        health.ConsecutiveFailures.Should().Be(3);
    }

    [Fact]
    public void Should_Default_To_Zero_Consecutive_Failures()
    {
        // Arrange & Act
        var health = new ProviderHealth();

        // Assert
        health.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void Should_Validate_ConsecutiveFailures_NonNegative()
    {
        // Arrange & Act
        Action act = () => new ProviderHealth(consecutiveFailures: -1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ConsecutiveFailures must be >= 0*");
    }

    [Fact]
    public void Should_Support_Zero_Consecutive_Failures()
    {
        // Arrange & Act - Success resets failures to 0
        var health = new ProviderHealth(
            status: HealthStatus.Healthy,
            consecutiveFailures: 0);

        // Assert
        health.ConsecutiveFailures.Should().Be(0);
        health.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void Should_Be_Immutable()
    {
        // Arrange
        var original = new ProviderHealth(
            status: HealthStatus.Healthy,
            consecutiveFailures: 0);

        // Act - use 'with' to create modified copy
        var modified = original with
        {
            Status = HealthStatus.Unhealthy,
            ConsecutiveFailures = 1
        };

        // Assert - original unchanged
        original.Status.Should().Be(HealthStatus.Healthy);
        original.ConsecutiveFailures.Should().Be(0);

        modified.Status.Should().Be(HealthStatus.Unhealthy);
        modified.ConsecutiveFailures.Should().Be(1);

        original.Should().NotBeSameAs(modified);
    }

    [Fact]
    public void Should_Track_Status_Transitions()
    {
        // Arrange - Start unknown
        var health1 = new ProviderHealth(status: HealthStatus.Unknown);

        // Act - Transition to healthy
        var health2 = health1 with { Status = HealthStatus.Healthy };

        // Assert - Status changed, original unchanged
        health1.Status.Should().Be(HealthStatus.Unknown);
        health2.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void Should_Record_Failure_With_Error()
    {
        // Arrange - Simulate first failure
        var health = new ProviderHealth(
            status: HealthStatus.Unhealthy,
            lastChecked: DateTime.UtcNow,
            lastError: "Connection refused",
            consecutiveFailures: 1);

        // Assert
        health.Status.Should().Be(HealthStatus.Unhealthy);
        health.LastError.Should().Be("Connection refused");
        health.ConsecutiveFailures.Should().Be(1);
    }

    [Fact]
    public void Should_Increment_Consecutive_Failures()
    {
        // Arrange - Start with 1 failure
        var health1 = new ProviderHealth(
            status: HealthStatus.Unhealthy,
            consecutiveFailures: 1);

        // Act - Simulate second failure
        var health2 = health1 with
        {
            ConsecutiveFailures = health1.ConsecutiveFailures + 1
        };

        // Assert
        health2.ConsecutiveFailures.Should().Be(2);
    }

    [Fact]
    public void Should_Reset_Consecutive_Failures_On_Success()
    {
        // Arrange - Start with failures
        var unhealthy = new ProviderHealth(
            status: HealthStatus.Unhealthy,
            lastError: "Previous error",
            consecutiveFailures: 3);

        // Act - Success should reset failures
        var healthy = unhealthy with
        {
            Status = HealthStatus.Healthy,
            LastError = null,
            ConsecutiveFailures = 0
        };

        // Assert
        healthy.Status.Should().Be(HealthStatus.Healthy);
        healthy.LastError.Should().BeNull();
        healthy.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void Should_Support_All_Properties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var errorMessage = "Connection timeout after 5 seconds";

        // Act
        var health = new ProviderHealth(
            status: HealthStatus.Degraded,
            lastChecked: timestamp,
            lastError: errorMessage,
            consecutiveFailures: 2);

        // Assert
        health.Status.Should().Be(HealthStatus.Degraded);
        health.LastChecked.Should().Be(timestamp);
        health.LastError.Should().Be(errorMessage);
        health.ConsecutiveFailures.Should().Be(2);
    }
}
