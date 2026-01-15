using Acode.Domain.Providers.Vllm;
using Acode.Infrastructure.Providers.Vllm.Lifecycle;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Providers.Vllm.Lifecycle;

/// <summary>
/// Tests for VllmServiceStateTracker state machine.
/// </summary>
public class VllmServiceStateTrackerTests
{
    [Fact]
    public void Test_VllmServiceStateTracker_InitialState_IsUnknown()
    {
        // Arrange & Act
        var tracker = new VllmServiceStateTracker();

        // Assert
        tracker.CurrentState.Should().Be(VllmServiceState.Unknown);
        tracker.ProcessId.Should().BeNull();
        tracker.UpSinceUtc.Should().BeNull();
        tracker.LastHealthCheckUtc.Should().BeNull();
        tracker.LastHealthCheckHealthy.Should().BeFalse();
    }

    [Fact]
    public void Test_VllmServiceStateTracker_Transition_ChangesState()
    {
        // Arrange
        var tracker = new VllmServiceStateTracker();

        // Act
        tracker.Transition(VllmServiceState.Starting);

        // Assert
        tracker.CurrentState.Should().Be(VllmServiceState.Starting);
    }

    [Fact]
    public void Test_VllmServiceStateTracker_TransitionToRunning_SetsUpSince()
    {
        // Arrange
        var tracker = new VllmServiceStateTracker();
        var beforeTransition = DateTime.UtcNow;

        // Act
        tracker.Transition(VllmServiceState.Running);
        var afterTransition = DateTime.UtcNow.AddMilliseconds(1);

        // Assert
        tracker.CurrentState.Should().Be(VllmServiceState.Running);
        tracker.UpSinceUtc.Should().NotBeNull();
        tracker.UpSinceUtc.Should().BeOnOrAfter(beforeTransition).And.BeOnOrBefore(afterTransition);
    }

    [Fact]
    public void Test_VllmServiceStateTracker_SetProcessId_TracksProcessId()
    {
        // Arrange
        var tracker = new VllmServiceStateTracker();

        // Act
        tracker.SetProcessId(12345);

        // Assert
        tracker.ProcessId.Should().Be(12345);
    }

    [Fact]
    public void Test_VllmServiceStateTracker_MarkHealthy_UpdatesHealthStatus()
    {
        // Arrange
        var tracker = new VllmServiceStateTracker();
        var beforeHealthCheck = DateTime.UtcNow;

        // Act
        tracker.MarkHealthy();
        var afterHealthCheck = DateTime.UtcNow.AddMilliseconds(1);

        // Assert
        tracker.LastHealthCheckHealthy.Should().BeTrue();
        tracker.LastHealthCheckUtc.Should().NotBeNull();
        tracker.LastHealthCheckUtc.Should().BeOnOrAfter(beforeHealthCheck).And.BeOnOrBefore(afterHealthCheck);
    }

    [Fact]
    public void Test_VllmServiceStateTracker_MarkUnhealthy_UpdatesHealthStatus()
    {
        // Arrange
        var tracker = new VllmServiceStateTracker();

        // Act
        tracker.MarkUnhealthy();

        // Assert
        tracker.LastHealthCheckHealthy.Should().BeFalse();
        tracker.LastHealthCheckUtc.Should().NotBeNull();
    }

    [Fact]
    public void Test_VllmServiceStateTracker_MultipleTransitions_TrackCorrectly()
    {
        // Arrange
        var tracker = new VllmServiceStateTracker();

        // Act
        tracker.Transition(VllmServiceState.Starting);
        tracker.SetProcessId(999);
        tracker.Transition(VllmServiceState.Running);
        var upSinceWhenRunning = tracker.UpSinceUtc;
        tracker.MarkHealthy();
        tracker.Transition(VllmServiceState.Stopping);
        tracker.Transition(VllmServiceState.Stopped);

        // Assert
        tracker.CurrentState.Should().Be(VllmServiceState.Stopped);
        tracker.ProcessId.Should().Be(999);
        tracker.UpSinceUtc.Should().Be(upSinceWhenRunning, "UpSinceUtc should only be set when transitioning to Running");
        tracker.LastHealthCheckHealthy.Should().BeTrue("Should remember last health check");
    }

    [Fact]
    public void Test_VllmServiceStateTracker_CrashTransition_PreservesUpSince()
    {
        // Arrange
        var tracker = new VllmServiceStateTracker();
        tracker.Transition(VllmServiceState.Running);
        var originalUpSince = tracker.UpSinceUtc;

        // Act
        tracker.Transition(VllmServiceState.Crashed);

        // Assert
        tracker.CurrentState.Should().Be(VllmServiceState.Crashed);
        tracker.UpSinceUtc.Should().Be(originalUpSince, "UpSinceUtc should be preserved during crash");
    }

    [Fact]
    public void Test_VllmServiceStateTracker_ThreadSafe_ConcurrentTransitions()
    {
        // Arrange
        var tracker = new VllmServiceStateTracker();

        // Act - Run 10 concurrent operations using threads
        var threads = new System.Threading.Thread[10];
        for (int i = 0; i < 10; i++)
        {
            var threadNum = i;
            threads[i] = new System.Threading.Thread(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    tracker.SetProcessId(1000 + (threadNum * 100) + j);
                    tracker.MarkHealthy();
                    tracker.Transition(VllmServiceState.Running);
                }
            });
            threads[i].Start();
        }

        // Wait for all threads
        foreach (var thread in threads)
        {
            thread.Join(5000);
        }

        // Assert - Final state should be valid
        tracker.CurrentState.Should().Be(VllmServiceState.Running);
        tracker.ProcessId.Should().BeGreaterThanOrEqualTo(1000);
        tracker.ProcessId.Should().BeLessThan(2000);
    }

    [Fact]
    public void Test_VllmServiceStateTracker_HealthCheckTimestamp_Updated()
    {
        // Arrange
        var tracker = new VllmServiceStateTracker();

        // Act
        tracker.MarkHealthy();
        var firstHealthCheckTime = tracker.LastHealthCheckUtc;

        System.Threading.Thread.Sleep(10); // Small delay

        tracker.MarkUnhealthy();
        var secondHealthCheckTime = tracker.LastHealthCheckUtc;

        // Assert
        secondHealthCheckTime.Should().BeAfter(firstHealthCheckTime!.Value);
    }

    [Fact]
    public void Test_VllmServiceStateTracker_StateTransitionSequence_ValidStateFlow()
    {
        // Arrange
        var tracker = new VllmServiceStateTracker();
        var stateSequence = new[]
        {
            VllmServiceState.Starting,
            VllmServiceState.Running,
            VllmServiceState.Stopping,
            VllmServiceState.Stopped
        };

        // Act & Assert - All transitions should succeed
        foreach (var state in stateSequence)
        {
            var exceptionThrown = Record.Exception(() => tracker.Transition(state));
            exceptionThrown.Should().BeNull($"Transition to {state} should not throw");
            tracker.CurrentState.Should().Be(state);
        }
    }
}
