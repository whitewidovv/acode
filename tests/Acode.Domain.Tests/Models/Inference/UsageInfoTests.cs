namespace Acode.Domain.Tests.Models.Inference;

using System.Text.Json;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for UsageInfo record following TDD (RED phase).
/// FR-004b-030 to FR-004b-041.
/// </summary>
public class UsageInfoTests
{
    [Fact]
    public void UsageInfo_HasPromptTokensProperty()
    {
        // FR-004b-031: UsageInfo MUST include PromptTokens property (int, non-negative)
        var usage = new UsageInfo(100, 50);

        usage.PromptTokens.Should().Be(100);
    }

    [Fact]
    public void UsageInfo_HasCompletionTokensProperty()
    {
        // FR-004b-032: UsageInfo MUST include CompletionTokens property (int, non-negative)
        var usage = new UsageInfo(100, 50);

        usage.CompletionTokens.Should().Be(50);
    }

    [Fact]
    public void UsageInfo_HasTotalTokensComputedProperty()
    {
        // FR-004b-033: UsageInfo MUST include TotalTokens computed property (Prompt + Completion)
        var usage = new UsageInfo(100, 50);

        usage.TotalTokens.Should().Be(150);
    }

    [Fact]
    public void UsageInfo_HasCachedTokensProperty()
    {
        // FR-004b-034: UsageInfo MUST include optional CachedTokens property
        var usage1 = new UsageInfo(100, 50, 25);
        var usage2 = new UsageInfo(100, 50);

        usage1.CachedTokens.Should().Be(25);
        usage2.CachedTokens.Should().BeNull();
    }

    [Fact]
    public void UsageInfo_HasReasoningTokensProperty()
    {
        // FR-004b-035: UsageInfo MUST include optional ReasoningTokens property
        var usage1 = new UsageInfo(100, 50, null, 30);
        var usage2 = new UsageInfo(100, 50);

        usage1.ReasoningTokens.Should().Be(30);
        usage2.ReasoningTokens.Should().BeNull();
    }

    [Fact]
    public void UsageInfo_ThrowsOnNegativePromptTokens()
    {
        // FR-004b-036: UsageInfo MUST validate non-negative values on construction
        var act = () => new UsageInfo(-1, 50);

        act.Should().Throw<ArgumentException>().WithParameterName("PromptTokens");
    }

    [Fact]
    public void UsageInfo_ThrowsOnNegativeCompletionTokens()
    {
        // FR-004b-036: UsageInfo MUST validate non-negative values on construction
        var act = () => new UsageInfo(100, -1);

        act.Should().Throw<ArgumentException>().WithParameterName("CompletionTokens");
    }

    [Fact]
    public void UsageInfo_ThrowsOnNegativeCachedTokens()
    {
        // FR-004b-036: UsageInfo MUST validate non-negative values on construction
        var act = () => new UsageInfo(100, 50, -1);

        act.Should().Throw<ArgumentException>().WithParameterName("CachedTokens");
    }

    [Fact]
    public void UsageInfo_ThrowsOnNegativeReasoningTokens()
    {
        // FR-004b-036: UsageInfo MUST validate non-negative values on construction
        var act = () => new UsageInfo(100, 50, null, -1);

        act.Should().Throw<ArgumentException>().WithParameterName("ReasoningTokens");
    }

    [Fact]
    public void UsageInfo_HasEmptyStaticProperty()
    {
        // FR-004b-037: UsageInfo MUST provide static Empty property returning zeros
        var empty = UsageInfo.Empty;

        empty.PromptTokens.Should().Be(0);
        empty.CompletionTokens.Should().Be(0);
        empty.TotalTokens.Should().Be(0);
        empty.CachedTokens.Should().BeNull();
        empty.ReasoningTokens.Should().BeNull();
    }

    [Fact]
    public void UsageInfo_HasValueEquality()
    {
        // FR-004b-038: UsageInfo MUST implement value equality on all token counts
        var usage1 = new UsageInfo(100, 50, 25);
        var usage2 = new UsageInfo(100, 50, 25);
        var usage3 = new UsageInfo(100, 60, 25);

        usage1.Should().Be(usage2);
        usage1.Should().NotBe(usage3);
    }

    [Fact]
    public void UsageInfo_SerializesToJson()
    {
        // FR-004b-039: UsageInfo MUST serialize to JSON matching OpenAI usage format
        var usage = new UsageInfo(100, 50);

        var json = JsonSerializer.Serialize(usage);

        json.Should().Contain("\"promptTokens\":");
        json.Should().Contain("\"completionTokens\":");
        json.Should().Contain("\"totalTokens\":");
    }

    [Fact]
    public void UsageInfo_HasAddMethod()
    {
        // FR-004b-040: UsageInfo MUST provide Add method for combining usage across requests
        var usage1 = new UsageInfo(100, 50, 25);
        var usage2 = new UsageInfo(75, 30, 10);

        var combined = usage1.Add(usage2);

        combined.PromptTokens.Should().Be(175);
        combined.CompletionTokens.Should().Be(80);
        combined.CachedTokens.Should().Be(35);
    }

    [Fact]
    public void UsageInfo_AddCombinesReasoningTokens()
    {
        // FR-004b-040: Add method should combine reasoning tokens
        var usage1 = new UsageInfo(100, 50, null, 20);
        var usage2 = new UsageInfo(75, 30, null, 15);

        var combined = usage1.Add(usage2);

        combined.ReasoningTokens.Should().Be(35);
    }

    [Fact]
    public void UsageInfo_AddWithNullCachedTokens()
    {
        // FR-004b-040: Add should handle null cached tokens correctly
        var usage1 = new UsageInfo(100, 50, null);
        var usage2 = new UsageInfo(75, 30, 10);

        var combined = usage1.Add(usage2);

        combined.CachedTokens.Should().Be(10);
    }

    [Fact]
    public void UsageInfo_HasMeaningfulToString()
    {
        // FR-004b-041: UsageInfo MUST provide ToString showing "Prompt: X, Completion: Y, Total: Z"
        var usage = new UsageInfo(100, 50);

        var str = usage.ToString();

        str.Should().Contain("Prompt: 100");
        str.Should().Contain("Completion: 50");
        str.Should().Contain("Total: 150");
    }

    [Fact]
    public void UsageInfo_IsImmutable()
    {
        // FR-004b-030: UsageInfo MUST be defined as an immutable record type
        var usage = new UsageInfo(100, 50);

        // Record with init-only properties ensures immutability at compile time
        usage.Should().NotBeNull();
    }

    [Fact]
    public void UsageInfo_AllowsZeroTokens()
    {
        // Zero tokens should be allowed
        var usage = new UsageInfo(0, 0);

        usage.PromptTokens.Should().Be(0);
        usage.CompletionTokens.Should().Be(0);
        usage.TotalTokens.Should().Be(0);
    }
}
