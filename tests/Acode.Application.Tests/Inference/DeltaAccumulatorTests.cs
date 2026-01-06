namespace Acode.Application.Tests.Inference;

using System;
using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for DeltaAccumulator class following TDD (RED phase).
/// FR-066 to FR-077.
/// </summary>
public class DeltaAccumulatorTests
{
    [Fact]
    public void DeltaAccumulator_CanBeConstructed()
    {
        // FR-066: DeltaAccumulator MUST be defined as a mutable class for building responses
        var accumulator = new DeltaAccumulator();

        accumulator.Should().NotBeNull();
    }

    [Fact]
    public void DeltaAccumulator_AppendConcatenatesContent()
    {
        // FR-067, FR-068: Append method concatenates ContentDelta strings efficiently
        var accumulator = new DeltaAccumulator();

        accumulator.Append(new ResponseDelta(0, "Hello"));
        accumulator.Append(new ResponseDelta(1, " "));
        accumulator.Append(new ResponseDelta(2, "world"));

        var current = accumulator.Current;
        current.Should().NotBeNull();
        current!.Content.Should().Be("Hello world");
    }

    [Fact]
    public void DeltaAccumulator_CapturesFinishReason()
    {
        // FR-070: Capture final FinishReason from last delta
        var accumulator = new DeltaAccumulator();

        accumulator.Append(new ResponseDelta(0, "Hello"));
        accumulator.Append(new ResponseDelta(1, null, null, FinishReason.Stop));

        var current = accumulator.Current;
        current!.FinishReason.Should().Be(FinishReason.Stop);
    }

    [Fact]
    public void DeltaAccumulator_CapturesUsage()
    {
        // FR-071: Capture final Usage from last delta
        var usage = new UsageInfo(100, 50);
        var accumulator = new DeltaAccumulator();

        accumulator.Append(new ResponseDelta(0, "Hello"));
        accumulator.Append(new ResponseDelta(1, null, null, FinishReason.Stop, usage));

        var current = accumulator.Current;
        current!.Usage.Should().Be(usage);
    }

    [Fact]
    public void DeltaAccumulator_BuildReturnsCompleteResponse()
    {
        // FR-072: Build() returns complete ChatResponse
        var accumulator = new DeltaAccumulator();

        accumulator.Append(new ResponseDelta(0, "Hello"));
        accumulator.Append(new ResponseDelta(1, " world"));
        accumulator.Append(new ResponseDelta(2, null, null, FinishReason.Stop));

        var response = accumulator.Build();

        response.Should().NotBeNull();
        response.Message.Content.Should().Be("Hello world");
        response.FinishReason.Should().Be(FinishReason.Stop);
    }

    [Fact]
    public void DeltaAccumulator_TracksDeltaCount()
    {
        // FR-074: Track delta count for debugging
        var accumulator = new DeltaAccumulator();

        accumulator.Append(new ResponseDelta(0, "Hello"));
        accumulator.Append(new ResponseDelta(1, " world"));
        accumulator.Append(new ResponseDelta(2, null, null, FinishReason.Stop));

        accumulator.DeltaCount.Should().Be(3);
    }

    [Fact]
    public void DeltaAccumulator_CurrentReturnsPartialResponse()
    {
        // FR-075: Current property for partial response access
        var accumulator = new DeltaAccumulator();

        accumulator.Append(new ResponseDelta(0, "Hello"));

        var current = accumulator.Current;
        current.Should().NotBeNull();
        current!.Content.Should().Be("Hello");
        current.FinishReason.Should().BeNull();
    }

    [Fact]
    public void DeltaAccumulator_ThrowsIfBuildCalledBeforeFinalDelta()
    {
        // FR-077: Throw if Build() called before final delta received
        var accumulator = new DeltaAccumulator();

        accumulator.Append(new ResponseDelta(0, "Hello"));

        var act = () => accumulator.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*final delta*");
    }

    [Fact]
    public void DeltaAccumulator_HandlesEmptyContent()
    {
        // Empty strings should be handled
        var accumulator = new DeltaAccumulator();

        accumulator.Append(new ResponseDelta(0, string.Empty));
        accumulator.Append(new ResponseDelta(1, null, null, FinishReason.Stop));

        var response = accumulator.Build();

        response.Message.Content.Should().Be(string.Empty);
    }

    [Fact]
    public void DeltaAccumulator_HandlesToolCallDeltas()
    {
        // FR-069: Merge ToolCallDelta by Index into complete ToolCalls
        var accumulator = new DeltaAccumulator();

        accumulator.Append(new ResponseDelta(0, null, "{\"id\":\""));
        accumulator.Append(new ResponseDelta(1, null, "call_123\",\"name\":\""));
        accumulator.Append(new ResponseDelta(2, null, "get_weather\"}"));
        accumulator.Append(new ResponseDelta(3, null, null, FinishReason.ToolCalls));

        var response = accumulator.Build();

        response.FinishReason.Should().Be(FinishReason.ToolCalls);
        accumulator.ToolCallContent.Should().Be("{\"id\":\"call_123\",\"name\":\"get_weather\"}");
    }

    [Fact]
    public void DeltaAccumulator_AllowsMixedContentAndToolDeltas()
    {
        // Some providers send both content and tool calls
        var accumulator = new DeltaAccumulator();

        accumulator.Append(new ResponseDelta(0, "Calling function: "));
        accumulator.Append(new ResponseDelta(1, null, "{\"id\":\"call_1\"}"));
        accumulator.Append(new ResponseDelta(2, null, null, FinishReason.ToolCalls));

        var response = accumulator.Build();

        accumulator.Current!.Content.Should().Be("Calling function: ");
        accumulator.ToolCallContent.Should().Be("{\"id\":\"call_1\"}");
    }

    [Fact]
    public void DeltaAccumulator_HandlesMultipleAppends()
    {
        // FR-073: Track TotalTokens across all deltas
        var accumulator = new DeltaAccumulator();

        for (int i = 0; i < 10; i++)
        {
            accumulator.Append(new ResponseDelta(i, "x"));
        }

        accumulator.Append(new ResponseDelta(10, null, null, FinishReason.Stop));

        var response = accumulator.Build();

        response.Message.Content.Should().Be("xxxxxxxxxx");
        accumulator.DeltaCount.Should().Be(11);
    }
}
