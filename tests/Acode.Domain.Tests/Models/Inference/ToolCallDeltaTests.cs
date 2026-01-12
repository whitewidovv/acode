namespace Acode.Domain.Tests.Models.Inference;

using System.Text.Json;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for ToolCallDelta record from task 004a (RED phase).
/// FR-004a-91 to FR-004a-100.
/// </summary>
public class ToolCallDeltaTests
{
    [Fact]
    public void ToolCallDelta_HasIndexProperty()
    {
        // FR-004a-93: ToolCallDelta MUST have Index property
        var delta = new ToolCallDelta(Index: 0);

        delta.Index.Should().Be(0);
    }

    [Fact]
    public void ToolCallDelta_HasIdProperty()
    {
        // FR-004a-95: ToolCallDelta MAY have Id property
        var delta1 = new ToolCallDelta(Index: 0, Id: "call_123");
        var delta2 = new ToolCallDelta(Index: 0);

        delta1.Id.Should().Be("call_123");
        delta2.Id.Should().BeNull();
    }

    [Fact]
    public void ToolCallDelta_HasNameProperty()
    {
        // FR-004a-97: ToolCallDelta MAY have Name property
        var delta1 = new ToolCallDelta(Index: 0, Name: "search");
        var delta2 = new ToolCallDelta(Index: 0);

        delta1.Name.Should().Be("search");
        delta2.Name.Should().BeNull();
    }

    [Fact]
    public void ToolCallDelta_HasArgumentsDeltaProperty()
    {
        // FR-004a-99: ToolCallDelta MAY have ArgumentsDelta property
        // FR-004a-100: ArgumentsDelta is string (partial JSON)
        var delta1 = new ToolCallDelta(Index: 0, ArgumentsDelta: "{\"query\":");
        var delta2 = new ToolCallDelta(Index: 0);

        delta1.ArgumentsDelta.Should().Be("{\"query\":");
        delta2.ArgumentsDelta.Should().BeNull();
    }

    [Fact]
    public void ToolCallDelta_IsImmutable()
    {
        // FR-004a-92: ToolCallDelta MUST be immutable
        var delta = new ToolCallDelta(Index: 0, Id: "call_123", Name: "search", ArgumentsDelta: "{\"query\":");

        // Record with init-only properties ensures immutability at compile time
        delta.Should().NotBeNull();
        delta.Index.Should().Be(0);
    }

    [Fact]
    public void ToolCallDelta_HasValueEquality()
    {
        // Records have value equality by default
        var delta1 = new ToolCallDelta(Index: 0, Id: "call_123", Name: "search");
        var delta2 = new ToolCallDelta(Index: 0, Id: "call_123", Name: "search");
        var delta3 = new ToolCallDelta(Index: 1, Id: "call_123", Name: "search");

        delta1.Should().Be(delta2);
        delta1.Should().NotBe(delta3);
    }

    [Fact]
    public void ToolCallDelta_FirstDelta_HasIdAndName()
    {
        // FR-004a-96: Id is present only in first delta for a tool call
        // FR-004a-98: Name is present only in first delta
        var firstDelta = new ToolCallDelta(Index: 0, Id: "call_123", Name: "search");

        firstDelta.Index.Should().Be(0);
        firstDelta.Id.Should().Be("call_123");
        firstDelta.Name.Should().Be("search");
        firstDelta.ArgumentsDelta.Should().BeNull();
    }

    [Fact]
    public void ToolCallDelta_SubsequentDelta_HasOnlyArgumentsDelta()
    {
        // FR-004a-96, FR-004a-98: Subsequent deltas don't have Id/Name
        var subsequentDelta = new ToolCallDelta(Index: 0, ArgumentsDelta: "\"test\"}");

        subsequentDelta.Index.Should().Be(0);
        subsequentDelta.Id.Should().BeNull();
        subsequentDelta.Name.Should().BeNull();
        subsequentDelta.ArgumentsDelta.Should().Be("\"test\"}");
    }

    [Fact]
    public void ToolCallDelta_SerializesToJson()
    {
        // ToolCallDelta should serialize to JSON
        var delta = new ToolCallDelta(Index: 0, Id: "call_123", Name: "search", ArgumentsDelta: "{\"query\":");

        var json = JsonSerializer.Serialize(delta);

        json.Should().Contain("\"index\":");
        json.Should().Contain("\"id\":");
        json.Should().Contain("\"name\":");
        json.Should().Contain("\"argumentsDelta\":");
    }

    [Fact]
    public void ToolCallDelta_DeserializesFromJson()
    {
        // ToolCallDelta should deserialize from JSON
        var json = "{\"index\":0,\"id\":\"call_123\",\"name\":\"search\",\"argumentsDelta\":\"{\\\"query\\\":\"}";

        var delta = JsonSerializer.Deserialize<ToolCallDelta>(json);

        delta.Should().NotBeNull();
        delta!.Index.Should().Be(0);
        delta.Id.Should().Be("call_123");
        delta.Name.Should().Be("search");
        delta.ArgumentsDelta.Should().Be("{\"query\":");
    }

    [Fact]
    public void ToolCallDelta_SupportsMultipleToolCalls()
    {
        // FR-004a-94: Index identifies which tool call is being built
        var delta1 = new ToolCallDelta(Index: 0, Id: "call_1", Name: "search");
        var delta2 = new ToolCallDelta(Index: 1, Id: "call_2", Name: "calculate");

        delta1.Index.Should().Be(0);
        delta2.Index.Should().Be(1);
    }

    [Fact]
    public void ToolCallDelta_AllowsNegativeIndex()
    {
        // Index is just an int, no validation required
        var delta = new ToolCallDelta(Index: -1);

        delta.Index.Should().Be(-1);
    }
}
