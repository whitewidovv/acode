#pragma warning disable IDE0005
using Acode.Infrastructure.Providers.Vllm.Lifecycle;
using FluentAssertions;
#pragma warning restore IDE0005

namespace Acode.Infrastructure.Tests.Providers.Vllm.Lifecycle;

/// <summary>
/// Tests for VllmHealthCheckWorker background health monitoring.
/// </summary>
public class VllmHealthCheckWorkerTests
{
    [Fact]
    public void Test_VllmHealthCheckWorker_DefaultInterval_Is60Seconds()
    {
        // Arrange & Act
        var worker = new VllmHealthCheckWorker();

        // Assert
        worker.HealthCheckIntervalSeconds.Should().Be(60, "Default interval should be 60 seconds");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_SetInterval_UpdatesCorrectly()
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();

        // Act
        worker.SetHealthCheckInterval(30);

        // Assert
        worker.HealthCheckIntervalSeconds.Should().Be(30, "Interval should be updated");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Test_VllmHealthCheckWorker_SetInterval_RejectsInvalid(int interval)
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();

        // Act
        var act = () => worker.SetHealthCheckInterval(interval);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_ConsecutiveFailures_StartsAtZero()
    {
        // Arrange & Act
        var worker = new VllmHealthCheckWorker();

        // Assert
        worker.ConsecutiveFailures.Should().Be(0, "Should start with no failures");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_RecordFailure_IncrementsCounter()
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();

        // Act
        worker.RecordFailure();
        worker.RecordFailure();

        // Assert
        worker.ConsecutiveFailures.Should().Be(2, "Should have 2 consecutive failures");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_RecordSuccess_ResetsCounter()
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();
        worker.RecordFailure();
        worker.RecordFailure();

        // Act
        worker.RecordSuccess();

        // Assert
        worker.ConsecutiveFailures.Should().Be(0, "Success should reset failure counter");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_ShouldTriggerRestart_False_WithLessThanThreeFailures()
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();
        worker.RecordFailure();
        worker.RecordFailure();

        // Act
        var shouldRestart = worker.ShouldTriggerRestart();

        // Assert
        shouldRestart.Should().BeFalse("Should not restart with only 2 failures");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_ShouldTriggerRestart_True_WithThreeFailures()
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();
        worker.RecordFailure();
        worker.RecordFailure();
        worker.RecordFailure();

        // Act
        var shouldRestart = worker.ShouldTriggerRestart();

        // Assert
        shouldRestart.Should().BeTrue("Should trigger restart after 3 consecutive failures");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_ShouldTriggerRestart_True_WithMoreThanThreeFailures()
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();
        worker.RecordFailure();
        worker.RecordFailure();
        worker.RecordFailure();
        worker.RecordFailure();

        // Act
        var shouldRestart = worker.ShouldTriggerRestart();

        // Assert
        shouldRestart.Should().BeTrue("Should trigger restart after 4+ failures");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_DefaultTimeoutMs_Is5000()
    {
        // Arrange & Act
        var worker = new VllmHealthCheckWorker();

        // Assert
        worker.HealthCheckTimeoutMs.Should().Be(5000, "Default timeout should be 5000ms (5 seconds)");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_SetTimeout_UpdatesCorrectly()
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();

        // Act
        worker.SetHealthCheckTimeout(10000);

        // Assert
        worker.HealthCheckTimeoutMs.Should().Be(10000, "Timeout should be updated");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_LastCheckTime_IsNull_Initially()
    {
        // Arrange & Act
        var worker = new VllmHealthCheckWorker();

        // Assert
        worker.LastHealthCheckUtc.Should().BeNull("Should be null before first check");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_RecordSuccess_UpdatesLastCheckTime()
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();
        var before = DateTime.UtcNow;

        // Act
        worker.RecordSuccess();
        var after = DateTime.UtcNow;

        // Assert
        worker.LastHealthCheckUtc.Should().NotBeNull();
        worker.LastHealthCheckUtc.Should().BeOnOrAfter(before);
        worker.LastHealthCheckUtc.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_RecordFailure_UpdatesLastCheckTime()
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();
        var before = DateTime.UtcNow;

        // Act
        worker.RecordFailure();
        var after = DateTime.UtcNow;

        // Assert
        worker.LastHealthCheckUtc.Should().NotBeNull();
        worker.LastHealthCheckUtc.Should().BeOnOrAfter(before);
        worker.LastHealthCheckUtc.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_LastCheckHealthy_False_Initially()
    {
        // Arrange & Act
        var worker = new VllmHealthCheckWorker();

        // Assert
        worker.LastHealthCheckHealthy.Should().BeFalse("Should be false before first check");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_RecordSuccess_SetsHealthyTrue()
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();

        // Act
        worker.RecordSuccess();

        // Assert
        worker.LastHealthCheckHealthy.Should().BeTrue("Success should set healthy to true");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_RecordFailure_SetsHealthyFalse()
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();
        worker.RecordSuccess(); // First set to healthy

        // Act
        worker.RecordFailure();

        // Assert
        worker.LastHealthCheckHealthy.Should().BeFalse("Failure should set healthy to false");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_ResetAfterRestart_ClearsFailures()
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();
        worker.RecordFailure();
        worker.RecordFailure();
        worker.RecordFailure();

        // Act
        worker.ResetAfterRestart();

        // Assert
        worker.ConsecutiveFailures.Should().Be(0, "Should reset failures after restart");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_ResetAfterRestart_KeepsLastCheckTime()
    {
        // Arrange
        var worker = new VllmHealthCheckWorker();
        worker.RecordSuccess();
        var lastCheck = worker.LastHealthCheckUtc;

        // Act
        worker.ResetAfterRestart();

        // Assert
        worker.LastHealthCheckUtc.Should().Be(lastCheck, "Should keep last check time after restart");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_HealthEndpoint_IsCorrect()
    {
        // Arrange & Act
        var endpoint = VllmHealthCheckWorker.HealthEndpoint;

        // Assert
        endpoint.Should().Be("/health", "Should use /health endpoint");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_ModelsEndpoint_IsCorrect()
    {
        // Arrange & Act
        var endpoint = VllmHealthCheckWorker.ModelsEndpoint;

        // Assert
        endpoint.Should().Be("/v1/models", "Should use /v1/models endpoint");
    }

    [Fact]
    public void Test_VllmHealthCheckWorker_MaxConsecutiveFailuresForRestart_IsThree()
    {
        // Arrange & Act
        var maxFailures = VllmHealthCheckWorker.MaxConsecutiveFailuresForRestart;

        // Assert
        maxFailures.Should().Be(3, "Should restart after 3 consecutive failures");
    }
}
