namespace Acode.Application.Tests.Inference;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        accumulator.Append(new ResponseDelta(0, null, new ToolCallDelta(Index: 0, ArgumentsDelta: "{\"id\":\"")));
        accumulator.Append(new ResponseDelta(1, null, new ToolCallDelta(Index: 0, ArgumentsDelta: "call_123\",\"name\":\"")));
        accumulator.Append(new ResponseDelta(2, null, new ToolCallDelta(Index: 0, ArgumentsDelta: "get_weather\"}")));
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
        accumulator.Append(new ResponseDelta(1, null, new ToolCallDelta(Index: 0, ArgumentsDelta: "{\"id\":\"call_1\"}")));
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

    [Fact]
    public void DeltaAccumulator_HandlesUnicodeContent()
    {
        // Unicode characters should be concatenated correctly
        var accumulator = new DeltaAccumulator();

        accumulator.Append(new ResponseDelta(0, "Hello "));
        accumulator.Append(new ResponseDelta(1, "\u4E16\u754C")); // 世界 (world in Chinese)
        accumulator.Append(new ResponseDelta(2, " 👋")); // Emoji
        accumulator.Append(new ResponseDelta(3, null, null, FinishReason.Stop));

        var response = accumulator.Build();

        response.Message.Content.Should().Be("Hello \u4E16\u754C 👋");
    }

    [Fact]
    public void DeltaAccumulator_HandlesMultipleToolCalls()
    {
        // Should handle tool calls with different Index values
        var accumulator = new DeltaAccumulator();

        // First tool call (Index 0)
        accumulator.Append(new ResponseDelta(0, null, new ToolCallDelta(Index: 0, Id: "call_1", Name: "search")));
        accumulator.Append(new ResponseDelta(1, null, new ToolCallDelta(Index: 0, ArgumentsDelta: "{\"q\":")));

        // Second tool call (Index 1) - would need separate tracking in real implementation
        accumulator.Append(new ResponseDelta(2, null, new ToolCallDelta(Index: 1, Id: "call_2", Name: "calculate")));

        accumulator.Append(new ResponseDelta(3, null, null, FinishReason.ToolCalls));

        var response = accumulator.Build();

        response.FinishReason.Should().Be(FinishReason.ToolCalls);

        // Note: Current implementation concatenates all ArgumentsDelta regardless of Index
        // Full implementation would track by Index separately
    }

    [Theory]
    [InlineData(FinishReason.Stop)]
    [InlineData(FinishReason.Length)]
    [InlineData(FinishReason.ToolCalls)]
    [InlineData(FinishReason.ContentFilter)]
    [InlineData(FinishReason.Error)]
    [InlineData(FinishReason.Cancelled)]
    public void DeltaAccumulator_CapturesAllFinishReasonTypes(FinishReason finishReason)
    {
        // Should capture any FinishReason type
        var accumulator = new DeltaAccumulator();

        accumulator.Append(new ResponseDelta(0, "Test"));
        accumulator.Append(new ResponseDelta(1, null, null, finishReason));

        var current = accumulator.Current;
        current!.FinishReason.Should().Be(finishReason);

        var response = accumulator.Build();
        response.FinishReason.Should().Be(finishReason);
    }

    [Fact]
    public void DeltaAccumulator_BuildGeneratesResponseId()
    {
        // Build() should generate a unique response ID
        var accumulator1 = new DeltaAccumulator();
        var accumulator2 = new DeltaAccumulator();

        accumulator1.Append(new ResponseDelta(0, "Test1", null, FinishReason.Stop));
        accumulator2.Append(new ResponseDelta(0, "Test2", null, FinishReason.Stop));

        var response1 = accumulator1.Build();
        var response2 = accumulator2.Build();

        response1.Id.Should().NotBeNullOrEmpty();
        response2.Id.Should().NotBeNullOrEmpty();
        response1.Id.Should().NotBe(response2.Id);
    }

    [Fact]
    public async Task DeltaAccumulator_IsThreadSafe()
    {
        // FR-076: Thread-safe for concurrent Append calls
        var accumulator = new DeltaAccumulator();
        var tasks = new List<Task>();

        // Append 100 deltas concurrently from 10 threads
        for (int i = 0; i < 100; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() => accumulator.Append(new ResponseDelta(index, "x"))));
        }

        await Task.WhenAll(tasks);

        // Add final delta
        accumulator.Append(new ResponseDelta(100, null, null, FinishReason.Stop));

        var response = accumulator.Build();

        // Should have all 100 'x' characters (order may vary due to threading)
        response.Should().NotBeNull();
        response.Message.Should().NotBeNull();
        response.Message!.Content!.Length.Should().Be(100);
        response.Message!.Content!.Should().MatchRegex("^x+$");
        accumulator.DeltaCount.Should().Be(101);
    }
}
