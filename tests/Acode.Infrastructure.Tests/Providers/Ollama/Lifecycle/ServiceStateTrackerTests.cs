using Acode.Domain.Providers.Ollama;
using Acode.Infrastructure.Providers.Ollama.Lifecycle;

namespace Acode.Infrastructure.Tests.Providers.Ollama.Lifecycle;

/// <summary>
/// Tests for ServiceStateTracker state machine.
/// Validates service state transitions and failure tracking.
/// </summary>
public class ServiceStateTrackerTests
{
    [Fact]
    public void ServiceStateTracker_InitialState_IsUnknown()
    {
        // Arrange & Act
        var tracker = new ServiceStateTracker();

        // Assert
        Assert.Equal(OllamaServiceState.Unknown, tracker.CurrentState);
    }

    [Fact]
    public void ServiceStateTracker_CanUpdateState()
    {
        // Arrange
        var tracker = new ServiceStateTracker();

        // Act
        tracker.UpdateState(OllamaServiceState.Running);

        // Assert
        Assert.Equal(OllamaServiceState.Running, tracker.CurrentState);
    }

    [Theory]
    [InlineData(OllamaServiceState.Running)]
    [InlineData(OllamaServiceState.Starting)]
    [InlineData(OllamaServiceState.Stopping)]
    [InlineData(OllamaServiceState.Stopped)]
    [InlineData(OllamaServiceState.Failed)]
    [InlineData(OllamaServiceState.Crashed)]
    [InlineData(OllamaServiceState.Unknown)]
    public void ServiceStateTracker_CanTransitionToAnyState(OllamaServiceState state)
    {
        // Arrange
        var tracker = new ServiceStateTracker();

        // Act
        tracker.UpdateState(state);

        // Assert
        Assert.Equal(state, tracker.CurrentState);
    }

    [Fact]
    public void ServiceStateTracker_FailureCountStartsAtZero()
    {
        // Arrange & Act
        var tracker = new ServiceStateTracker();

        // Assert
        Assert.Equal(0, tracker.ConsecutiveHealthCheckFailures);
    }

    [Fact]
    public void ServiceStateTracker_IncrementFailureCount()
    {
        // Arrange
        var tracker = new ServiceStateTracker();

        // Act
        tracker.IncrementFailureCount();

        // Assert
        Assert.Equal(1, tracker.ConsecutiveHealthCheckFailures);
    }

    [Fact]
    public void ServiceStateTracker_IncrementFailureCountMultipleTimes()
    {
        // Arrange
        var tracker = new ServiceStateTracker();

        // Act
        tracker.IncrementFailureCount();
        tracker.IncrementFailureCount();
        tracker.IncrementFailureCount();

        // Assert
        Assert.Equal(3, tracker.ConsecutiveHealthCheckFailures);
    }

    [Fact]
    public void ServiceStateTracker_ResetFailureCount()
    {
        // Arrange
        var tracker = new ServiceStateTracker();
        tracker.IncrementFailureCount();
        tracker.IncrementFailureCount();
        Assert.Equal(2, tracker.ConsecutiveHealthCheckFailures);

        // Act
        tracker.ResetFailureCount();

        // Assert
        Assert.Equal(0, tracker.ConsecutiveHealthCheckFailures);
    }

    [Fact]
    public void ServiceStateTracker_RestartCountStartsAtZero()
    {
        // Arrange & Act
        var tracker = new ServiceStateTracker();

        // Assert
        Assert.Equal(0, tracker.RestartCount);
    }

    [Fact]
    public void ServiceStateTracker_CanRecordRestart()
    {
        // Arrange
        var tracker = new ServiceStateTracker();

        // Act
        tracker.RecordRestart();

        // Assert
        Assert.Equal(1, tracker.RestartCount);
    }

    [Fact]
    public void ServiceStateTracker_CanResetRestartCount()
    {
        // Arrange
        var tracker = new ServiceStateTracker();
        tracker.RecordRestart();
        tracker.RecordRestart();
        Assert.Equal(2, tracker.RestartCount);

        // Act
        tracker.ResetRestartCount();

        // Assert
        Assert.Equal(0, tracker.RestartCount);
    }

    [Fact]
    public void ServiceStateTracker_TracksPreviousState()
    {
        // Arrange
        var tracker = new ServiceStateTracker();
        tracker.UpdateState(OllamaServiceState.Running);

        // Act
        tracker.UpdateState(OllamaServiceState.Crashed);

        // Assert
        Assert.Equal(OllamaServiceState.Running, tracker.PreviousState);
        Assert.Equal(OllamaServiceState.Crashed, tracker.CurrentState);
    }

    [Fact]
    public void ServiceStateTracker_LastStateChangeTimeUpdatesOnTransition()
    {
        // Arrange
        var tracker = new ServiceStateTracker();
        var beforeUpdate = DateTime.UtcNow;

        // Act
        tracker.UpdateState(OllamaServiceState.Running);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        Assert.True(tracker.LastStateChangeTime >= beforeUpdate);
        Assert.True(tracker.LastStateChangeTime <= afterUpdate);
    }

    [Fact]
    public void ServiceStateTracker_StateChangeEventFires()
    {
        // Arrange
        var tracker = new ServiceStateTracker();
        var eventFired = false;
        var capturedOldState = OllamaServiceState.Unknown;
        var capturedNewState = OllamaServiceState.Unknown;

        tracker.StateChanged += (oldState, newState) =>
        {
            eventFired = true;
            capturedOldState = oldState;
            capturedNewState = newState;
        };

        // Act
        tracker.UpdateState(OllamaServiceState.Running);

        // Assert
        Assert.True(eventFired);
        Assert.Equal(OllamaServiceState.Unknown, capturedOldState);
        Assert.Equal(OllamaServiceState.Running, capturedNewState);
    }

    [Fact]
    public void ServiceStateTracker_StateChangeEventFiresOnEveryTransition()
    {
        // Arrange
        var tracker = new ServiceStateTracker();
        var eventCount = 0;

        tracker.StateChanged += (_, _) => eventCount++;

        // Act
        tracker.UpdateState(OllamaServiceState.Running);
        tracker.UpdateState(OllamaServiceState.Crashed);
        tracker.UpdateState(OllamaServiceState.Starting);

        // Assert
        Assert.Equal(3, eventCount);
    }
}
