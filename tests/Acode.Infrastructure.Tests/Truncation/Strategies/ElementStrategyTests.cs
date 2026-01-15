namespace Acode.Infrastructure.Tests.Truncation.Strategies;

using System.Text.Json;
using Acode.Application.Truncation;
using Acode.Infrastructure.Truncation.Strategies;
using FluentAssertions;

/// <summary>
/// Tests for ElementStrategy truncation.
/// </summary>
/// <remarks>
/// Task-007c: Truncation + Artifact Attachment Rules.
/// Spec Reference: Testing Requirements lines 1408-1458.
/// Tests element-based truncation for JSON arrays/objects.
/// </remarks>
public sealed class ElementStrategyTests
{
    private readonly ElementStrategy strategy = new();

    [Fact]
    public void StrategyType_ShouldBeElement()
    {
        // Assert
        this.strategy.StrategyType.Should().Be(TruncationStrategy.Element);
    }

    [Fact]
    public void Truncate_ContentUnderLimit_ShouldNotTruncate()
    {
        // Arrange
        var content = "[1, 2, 3]";
        var limits = new TruncationLimits { InlineLimit = 100 };

        // Act
        var result = this.strategy.Truncate(content, limits);

        // Assert
        result.WasTruncated.Should().BeFalse();
        result.Content.Should().Be(content);
    }

    [Fact]
    public void Truncate_ShouldPreserveValidJsonArray()
    {
        // Arrange - 20 item array
        var items = Enumerable.Range(1, 20).Select(i => new { id = i, name = $"Item{i}" }).ToArray();
        var content = JsonSerializer.Serialize(items);
        var limits = new TruncationLimits { InlineLimit = 200, FirstElements = 2, LastElements = 2 };

        // Act
        var result = this.strategy.Truncate(content, limits);

        // Assert
        result.WasTruncated.Should().BeTrue();
        result.Content.Should().StartWith("[");
        result.Content.Should().EndWith("]");

        // Verify it's valid JSON
        var action = () => JsonDocument.Parse(result.Content);
        action.Should().NotThrow();

        // Verify it's an array
        using var doc = JsonDocument.Parse(result.Content);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public void Truncate_ShouldShowOmittedCount()
    {
        // Arrange - 100 integers
        var items = Enumerable.Range(1, 100).ToArray();
        var content = JsonSerializer.Serialize(items);
        var limits = new TruncationLimits { InlineLimit = 50, FirstElements = 2, LastElements = 2 };

        // Act
        var result = this.strategy.Truncate(content, limits);

        // Assert
        result.Content.Should().Contain("96"); // 100 - 2 - 2 = 96 items omitted
        result.Content.Should().Contain("omitted");
        result.Metadata.OmittedElements.Should().Be(96);
    }

    [Fact]
    public void Truncate_ShouldPreserveFirstAndLastElements()
    {
        // Arrange
        var items = new[] { "first", "second", "middle1", "middle2", "middle3", "fourth", "last" };
        var content = JsonSerializer.Serialize(items);
        var limits = new TruncationLimits { InlineLimit = 50, FirstElements = 2, LastElements = 2 };

        // Act
        var result = this.strategy.Truncate(content, limits);

        // Assert
        result.Content.Should().Contain("first");
        result.Content.Should().Contain("second");
        result.Content.Should().Contain("fourth");
        result.Content.Should().Contain("last");
        result.Content.Should().NotContain("middle1");
        result.Content.Should().NotContain("middle2");
        result.Content.Should().NotContain("middle3");
    }

    [Fact]
    public void Truncate_WithNonJsonContent_ShouldFallbackToHeadTail()
    {
        // Arrange - Plain text, not JSON
        var content = new string('x', 1000);
        var limits = new TruncationLimits { InlineLimit = 100 };

        // Act
        var result = this.strategy.Truncate(content, limits);

        // Assert
        result.WasTruncated.Should().BeTrue();

        // Falls back to HeadTail strategy for non-JSON content
        result.Metadata.StrategyUsed.Should().Be(TruncationStrategy.HeadTail);
    }

    [Fact]
    public void Truncate_WithJsonObject_ShouldFallbackToHeadTail()
    {
        // Arrange - JSON object (not array)
        var content = JsonSerializer.Serialize(new { a = 1, b = 2, c = new string('x', 200) });
        var limits = new TruncationLimits { InlineLimit = 100 };

        // Act
        var result = this.strategy.Truncate(content, limits);

        // Assert
        // Element strategy only handles arrays, objects fall back to HeadTail
        result.WasTruncated.Should().BeTrue();
        result.Metadata.StrategyUsed.Should().Be(TruncationStrategy.HeadTail);
    }

    [Fact]
    public void Truncate_WithSmallArray_ShouldNotTruncate()
    {
        // Arrange - Small array that fits within first+last count
        var items = new[] { 1, 2, 3, 4 };
        var content = JsonSerializer.Serialize(items);
        var limits = new TruncationLimits { InlineLimit = 10, FirstElements = 2, LastElements = 2 };

        // Act
        var result = this.strategy.Truncate(content, limits);

        // Assert - 4 items <= firstElements + lastElements, no truncation
        result.WasTruncated.Should().BeFalse();
    }

    [Fact]
    public void Truncate_WithNullContent_ShouldThrow()
    {
        // Arrange
        string content = null!;
        var limits = new TruncationLimits();

        // Act & Assert
        var action = () => this.strategy.Truncate(content, limits);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Truncate_WithNullLimits_ShouldThrow()
    {
        // Arrange
        var content = "[1,2,3]";
        TruncationLimits limits = null!;

        // Act & Assert
        var action = () => this.strategy.Truncate(content, limits);
        action.Should().Throw<ArgumentNullException>();
    }
}
