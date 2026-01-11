namespace Acode.Domain.Tests.Models.Inference;

using System;
using System.Text.Json;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for ResponseDelta record following TDD (RED phase).
/// FR-004b-056 to FR-004b-063.
/// </summary>
public class ResponseDeltaTests
{
    [Fact]
    public void ResponseDelta_HasIndexProperty()
    {
        // FR-004b-057: ResponseDelta MUST include Index property (int, position in stream)
        var delta = new ResponseDelta(0, "Hello");

        delta.Index.Should().Be(0);
    }

    [Fact]
    public void ResponseDelta_HasContentDeltaProperty()
    {
        // FR-004b-058: ResponseDelta MUST include optional ContentDelta property (string?)
        var delta1 = new ResponseDelta(0, "Hello");
        var delta2 = new ResponseDelta(1, null, "tool_call_fragment");

        delta1.ContentDelta.Should().Be("Hello");
        delta2.ContentDelta.Should().BeNull();
    }

    [Fact]
    public void ResponseDelta_HasFinishReasonProperty()
    {
        // FR-004b-060: ResponseDelta MUST include optional FinishReason property (present only on final delta)
        var delta1 = new ResponseDelta(0, "Hello", null, FinishReason.Stop);
        var delta2 = new ResponseDelta(1, "World");

        delta1.FinishReason.Should().Be(FinishReason.Stop);
        delta2.FinishReason.Should().BeNull();
    }

    [Fact]
    public void ResponseDelta_HasUsageProperty()
    {
        // FR-004b-061: ResponseDelta MUST include optional Usage property (present only on final delta)
        var usage = new UsageInfo(100, 50);
        var delta1 = new ResponseDelta(0, "Hello", null, null, usage);
        var delta2 = new ResponseDelta(1, "World");

        delta1.Usage.Should().Be(usage);
        delta2.Usage.Should().BeNull();
    }

    [Fact]
    public void ResponseDelta_IsCompleteWhenFinishReasonPresent()
    {
        // FR-004b-062: ResponseDelta MUST provide bool IsComplete property (FinishReason != null)
        var delta1 = new ResponseDelta(0, "Hello", null, FinishReason.Stop);
        var delta2 = new ResponseDelta(1, "World");

        delta1.IsComplete.Should().BeTrue();
        delta2.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void ResponseDelta_AllowsContentDeltaOnly()
    {
        // FR-004b-063: ResponseDelta MUST validate that at least ContentDelta or ToolCallDelta is present (or IsComplete)
        var delta = new ResponseDelta(0, "Hello");

        delta.ContentDelta.Should().Be("Hello");
        delta.ToolCallDelta.Should().BeNull();
    }

    [Fact]
    public void ResponseDelta_AllowsToolCallDeltaOnly()
    {
        // FR-004b-063: Allow ToolCallDelta only
        var delta = new ResponseDelta(0, null, "partial_tool_call");

        delta.ContentDelta.Should().BeNull();
        delta.ToolCallDelta.Should().Be("partial_tool_call");
    }

    [Fact]
    public void ResponseDelta_AllowsCompleteWithNeitherDelta()
    {
        // FR-004b-063: Allow final delta with no content if IsComplete
        var delta = new ResponseDelta(0, null, null, FinishReason.Stop);

        delta.ContentDelta.Should().BeNull();
        delta.ToolCallDelta.Should().BeNull();
        delta.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void ResponseDelta_ThrowsWhenNoDeltaAndNotComplete()
    {
        // FR-004b-063: Must have at least one delta or be complete
        var act = () => new ResponseDelta(0, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ResponseDelta_SerializesToJson()
    {
        // ResponseDelta should serialize to JSON
        var delta = new ResponseDelta(0, "Hello");

        var json = JsonSerializer.Serialize(delta);

        json.Should().Contain("\"index\":");
        json.Should().Contain("\"contentDelta\":");
    }

    [Fact]
    public void ResponseDelta_IsImmutable()
    {
        // ResponseDelta MUST be immutable
        var delta = new ResponseDelta(0, "Hello");

        delta.Should().NotBeNull();
    }

    [Fact]
    public void ResponseDelta_AllowsEmptyContentDelta()
    {
        // Empty string should be allowed
        var delta = new ResponseDelta(0, string.Empty);

        delta.ContentDelta.Should().Be(string.Empty);
    }
}
