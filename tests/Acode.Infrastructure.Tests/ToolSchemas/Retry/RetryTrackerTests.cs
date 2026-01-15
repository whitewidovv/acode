namespace Acode.Infrastructure.Tests.ToolSchemas.Retry;

using Acode.Infrastructure.ToolSchemas.Retry;
using FluentAssertions;

/// <summary>
/// Unit tests for RetryTracker class.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3858-3955.
/// Tests attempt tracking, history storage, thread safety, and memory limits.
/// </remarks>
public sealed class RetryTrackerTests
{
    [Fact]
    public void Should_Track_Attempts()
    {
        // Arrange
        var tracker = new RetryTracker(maxAttempts: 3);
        var toolCallId = "test-call-1";

        // Act
        var attempt1 = tracker.IncrementAttempt(toolCallId);
        var attempt2 = tracker.IncrementAttempt(toolCallId);
        var attempt3 = tracker.IncrementAttempt(toolCallId);

        // Assert
        attempt1.Should().Be(1);
        attempt2.Should().Be(2);
        attempt3.Should().Be(3);
    }

    [Fact]
    public void Should_Store_History()
    {
        // Arrange
        var tracker = new RetryTracker(maxAttempts: 3);
        var toolCallId = "test-call-1";

        // Act
        tracker.RecordError(toolCallId, "Error 1");
        tracker.RecordError(toolCallId, "Error 2");
        var history = tracker.GetHistory(toolCallId);

        // Assert
        history.Should().HaveCount(2);
        history[0].Should().Be("Error 1");
        history[1].Should().Be("Error 2");
    }

    [Fact]
    public void Should_Check_Max_Retries()
    {
        // Arrange
        var tracker = new RetryTracker(maxAttempts: 3);
        var toolCallId = "test-call-1";

        // Act & Assert
        tracker.IncrementAttempt(toolCallId);
        tracker.HasExceededMaxRetries(toolCallId).Should().BeFalse();

        tracker.IncrementAttempt(toolCallId);
        tracker.HasExceededMaxRetries(toolCallId).Should().BeFalse();

        tracker.IncrementAttempt(toolCallId);
        tracker.HasExceededMaxRetries(toolCallId).Should().BeTrue(); // 3 >= 3
    }

    [Fact]
    public async Task Should_Be_Thread_Safe()
    {
        // Arrange
        var tracker = new RetryTracker(maxAttempts: 1000);
        var toolCallId = "concurrent-call";
        const int taskCount = 10;
        const int incrementsPerTask = 100;

        // Act
        var tasks = Enumerable.Range(0, taskCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < incrementsPerTask; i++)
                {
                    tracker.IncrementAttempt(toolCallId);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert
        var finalCount = tracker.GetCurrentAttempt(toolCallId);
        finalCount.Should().Be(taskCount * incrementsPerTask, "All concurrent increments should be counted");
    }

    [Fact]
    public void Should_Return_Zero_For_Unknown_Tool_Call()
    {
        // Arrange
        var tracker = new RetryTracker(maxAttempts: 3);

        // Act
        var attempt = tracker.GetCurrentAttempt("unknown-call");

        // Assert
        attempt.Should().Be(0);
    }

    [Fact]
    public void Should_Clear_History_After_Success()
    {
        // Arrange
        var tracker = new RetryTracker(maxAttempts: 3);
        var toolCallId = "test-call-1";

        tracker.IncrementAttempt(toolCallId);
        tracker.RecordError(toolCallId, "Error");

        // Act
        tracker.Clear(toolCallId);

        // Assert
        tracker.GetCurrentAttempt(toolCallId).Should().Be(0);
        tracker.GetHistory(toolCallId).Should().BeEmpty();
    }

    [Fact]
    public void Should_Limit_Memory_Per_History()
    {
        // Arrange
        var tracker = new RetryTracker(maxAttempts: 100);
        var toolCallId = "test-call-1";

        // Act - record more than limit
        for (int i = 0; i < 20; i++)
        {
            tracker.RecordError(toolCallId, $"Error {i}");
        }

        var history = tracker.GetHistory(toolCallId);

        // Assert - should be limited to ~10 entries
        history.Should().HaveCountLessOrEqualTo(10);
    }

    [Fact]
    public void Should_Track_Multiple_Tool_Calls_Independently()
    {
        // Arrange
        var tracker = new RetryTracker(maxAttempts: 3);
        var call1 = "call-1";
        var call2 = "call-2";

        // Act
        tracker.IncrementAttempt(call1);
        tracker.IncrementAttempt(call1);
        tracker.IncrementAttempt(call2);

        // Assert
        tracker.GetCurrentAttempt(call1).Should().Be(2);
        tracker.GetCurrentAttempt(call2).Should().Be(1);
    }

    [Fact]
    public void GetHistory_Should_Return_Copy()
    {
        // Arrange
        var tracker = new RetryTracker(maxAttempts: 3);
        var toolCallId = "test-call-1";
        tracker.RecordError(toolCallId, "Error");

        // Act
        var history1 = tracker.GetHistory(toolCallId);
        var history2 = tracker.GetHistory(toolCallId);

        // Assert - should be different list instances
        history1.Should().NotBeSameAs(history2);
    }
}
