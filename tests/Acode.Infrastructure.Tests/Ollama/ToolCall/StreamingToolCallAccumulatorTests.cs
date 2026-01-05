namespace Acode.Infrastructure.Tests.Ollama.ToolCall;

using Acode.Infrastructure.Ollama.ToolCall;
using Acode.Infrastructure.Ollama.ToolCall.Models;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for StreamingToolCallAccumulator functionality.
/// </summary>
public sealed class StreamingToolCallAccumulatorTests
{
    [Fact]
    public void AccumulateDelta_FirstDelta_CreatesNewToolCall()
    {
        // Arrange
        var accumulator = new StreamingToolCallAccumulator();
        var delta = new ToolCallDelta
        {
            Index = 0,
            Id = "call_123",
            FunctionName = "read_file",
            ArgumentsFragment = string.Empty,
        };

        // Act
        accumulator.AccumulateDelta(delta);

        // Assert
        accumulator.HasPendingToolCalls.Should().BeTrue();
        accumulator.PendingCount.Should().Be(1);
    }

    [Fact]
    public void AccumulateDelta_MultipleFragments_AccumulatesArguments()
    {
        // Arrange
        var accumulator = new StreamingToolCallAccumulator();

        // Act - simulate streaming fragments
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, Id = "call_1", FunctionName = "read_file" });
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, ArgumentsFragment = "{\"path\":" });
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, ArgumentsFragment = " \"test.txt\"}" });

        // Assert
        var accumulated = accumulator.GetAccumulatedArguments(0);
        accumulated.Should().Be("{\"path\": \"test.txt\"}");
    }

    [Fact]
    public void AccumulateDelta_MultipleToolCalls_TracksSeparately()
    {
        // Arrange
        var accumulator = new StreamingToolCallAccumulator();

        // Act - two tool calls streaming in parallel
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, Id = "call_1", FunctionName = "read_file" });
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 1, Id = "call_2", FunctionName = "write_file" });
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, ArgumentsFragment = "{\"path\": \"a.txt\"}" });
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 1, ArgumentsFragment = "{\"path\": \"b.txt\"}" });

        // Assert
        accumulator.PendingCount.Should().Be(2);
        accumulator.GetAccumulatedArguments(0).Should().Be("{\"path\": \"a.txt\"}");
        accumulator.GetAccumulatedArguments(1).Should().Be("{\"path\": \"b.txt\"}");
    }

    [Fact]
    public void MarkComplete_SetsIsComplete()
    {
        // Arrange
        var accumulator = new StreamingToolCallAccumulator();
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, Id = "call_1", FunctionName = "test" });
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, ArgumentsFragment = "{}" });

        // Act
        accumulator.MarkComplete(0);

        // Assert
        accumulator.IsComplete(0).Should().BeTrue();
    }

    [Fact]
    public void Flush_ReturnsCompletedToolCalls()
    {
        // Arrange
        var accumulator = new StreamingToolCallAccumulator();
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, Id = "call_1", FunctionName = "read_file" });
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, ArgumentsFragment = "{\"path\": \"test.txt\"}" });
        accumulator.MarkComplete(0);

        // Act
        var result = accumulator.Flush();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("call_1");
        result[0].Function.Should().NotBeNull();
        result[0].Function!.Name.Should().Be("read_file");
        result[0].Function!.Arguments.Should().Be("{\"path\": \"test.txt\"}");
    }

    [Fact]
    public void Flush_ClearsCompletedToolCalls()
    {
        // Arrange
        var accumulator = new StreamingToolCallAccumulator();
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, Id = "call_1", FunctionName = "test" });
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, ArgumentsFragment = "{}" });
        accumulator.MarkComplete(0);

        // Act
        _ = accumulator.Flush();

        // Assert
        accumulator.PendingCount.Should().Be(0);
        accumulator.HasPendingToolCalls.Should().BeFalse();
    }

    [Fact]
    public void Flush_DoesNotReturnIncompleteToolCalls()
    {
        // Arrange
        var accumulator = new StreamingToolCallAccumulator();
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, Id = "call_1", FunctionName = "complete_tool" });
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, ArgumentsFragment = "{}" });
        accumulator.MarkComplete(0);

        accumulator.AccumulateDelta(new ToolCallDelta { Index = 1, Id = "call_2", FunctionName = "incomplete_tool" });

        // Index 1 not marked complete

        // Act
        var result = accumulator.Flush();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("call_1");
        accumulator.PendingCount.Should().Be(1); // Incomplete still pending
    }

    [Fact]
    public void AccumulateDelta_EmptyArgumentFragment_DoesNotAppend()
    {
        // Arrange
        var accumulator = new StreamingToolCallAccumulator();
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, Id = "call_1", FunctionName = "test" });

        // Act
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, ArgumentsFragment = string.Empty });
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, ArgumentsFragment = null });

        // Assert
        accumulator.GetAccumulatedArguments(0).Should().Be(string.Empty);
    }

    [Fact]
    public void AccumulateDelta_NullArgumentFragment_DoesNotAppend()
    {
        // Arrange
        var accumulator = new StreamingToolCallAccumulator();
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, Id = "call_1", FunctionName = "test" });

        // Act
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, ArgumentsFragment = null });

        // Assert
        accumulator.GetAccumulatedArguments(0).Should().Be(string.Empty);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        // Arrange
        var accumulator = new StreamingToolCallAccumulator();
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, Id = "call_1", FunctionName = "test" });
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, ArgumentsFragment = "{}" });

        // Act
        accumulator.Reset();

        // Assert
        accumulator.HasPendingToolCalls.Should().BeFalse();
        accumulator.PendingCount.Should().Be(0);
    }

    [Fact]
    public void GetAccumulatedArguments_UnknownIndex_ReturnsEmpty()
    {
        // Arrange
        var accumulator = new StreamingToolCallAccumulator();

        // Act
        var result = accumulator.GetAccumulatedArguments(999);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void IsComplete_UnknownIndex_ReturnsFalse()
    {
        // Arrange
        var accumulator = new StreamingToolCallAccumulator();

        // Act
        var result = accumulator.IsComplete(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FunctionNamePartial_AccumulatesName()
    {
        // Arrange
        var accumulator = new StreamingToolCallAccumulator();

        // Act - name comes in fragments (rare but possible)
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, Id = "call_1", FunctionName = "read" });
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, FunctionNameFragment = "_file" });
        accumulator.AccumulateDelta(new ToolCallDelta { Index = 0, ArgumentsFragment = "{}" });
        accumulator.MarkComplete(0);

        // Assert
        var result = accumulator.Flush();
        result[0].Function!.Name.Should().Be("read_file");
    }
}
