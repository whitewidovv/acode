namespace Acode.Infrastructure.Tests.Truncation.Strategies;

using Acode.Application.Truncation;
using Acode.Infrastructure.Truncation.Strategies;
using FluentAssertions;

/// <summary>
/// Tests for TailStrategy truncation.
/// </summary>
/// <remarks>
/// Task-007c: Truncation + Artifact Attachment Rules.
/// Spec Reference: Testing Requirements lines 1359-1406.
/// Tests tail-only truncation strategy used for logs/commands.
/// </remarks>
public sealed class TailStrategyTests
{
    private readonly TailStrategy strategy = new();

    [Fact]
    public void StrategyType_ShouldBeTail()
    {
        // Assert
        this.strategy.StrategyType.Should().Be(TruncationStrategy.Tail);
    }

    [Fact]
    public void Truncate_ContentUnderLimit_ShouldNotTruncate()
    {
        // Arrange
        var content = "Short content";
        var limits = new TruncationLimits { InlineLimit = 100 };

        // Act
        var result = this.strategy.Truncate(content, limits);

        // Assert
        result.WasTruncated.Should().BeFalse();
        result.Content.Should().Be(content);
        result.Metadata.OriginalSize.Should().Be(content.Length);
    }

    [Fact]
    public void Truncate_ShouldKeepOnlyTailLines()
    {
        // Arrange - Create 20 lines
        var lines = new List<string>();
        for (int i = 1; i <= 20; i++)
        {
            lines.Add($"Line {i:D2}");
        }

        var content = string.Join('\n', lines);
        var limits = new TruncationLimits { InlineLimit = 100, TailLines = 5 };

        // Act
        var result = this.strategy.Truncate(content, limits);

        // Assert
        result.WasTruncated.Should().BeTrue();
        result.Content.Should().Contain("Line 16");
        result.Content.Should().Contain("Line 17");
        result.Content.Should().Contain("Line 18");
        result.Content.Should().Contain("Line 19");
        result.Content.Should().Contain("Line 20");
        result.Content.Should().NotContain("Line 01");
        result.Content.Should().NotContain("Line 10");
        result.Content.Should().Contain("omitted"); // Omission marker
    }

    [Fact]
    public void Truncate_ShouldPreserveCompleteLines()
    {
        // Arrange - 20 lines, keep last 3 (content exceeds InlineLimit)
        var lines = new List<string>();
        for (int i = 1; i <= 20; i++)
        {
            lines.Add($"Line {i}: {new string('x', 20)}"); // Make lines long enough
        }

        var content = string.Join('\n', lines);
        var limits = new TruncationLimits { InlineLimit = 100, TailLines = 3 };

        // Act
        var result = this.strategy.Truncate(content, limits);

        // Assert
        result.WasTruncated.Should().BeTrue();

        // Check that preserved lines are complete (contain "Line" prefix)
        var resultLines = result.Content.Split('\n')
            .Where(l => l.StartsWith("Line", StringComparison.Ordinal))
            .ToList();

        resultLines.Should().Contain(l => l.Contains("Line 18"));
        resultLines.Should().Contain(l => l.Contains("Line 20"));
    }

    [Fact]
    public void Truncate_ShouldReportCorrectMetadata()
    {
        // Arrange
        var content = string.Join('\n', Enumerable.Range(1, 100).Select(i => $"Line {i}"));
        var limits = new TruncationLimits { InlineLimit = 500, TailLines = 10 };

        // Act
        var result = this.strategy.Truncate(content, limits);

        // Assert
        result.Metadata.StrategyUsed.Should().Be(TruncationStrategy.Tail);
        result.Metadata.OmittedLines.Should().Be(90); // 100 - 10 = 90 lines omitted
        result.Metadata.OmittedCharacters.Should().BeGreaterThan(0);
        result.Metadata.OriginalSize.Should().Be(content.Length);
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
        var content = "test";
        TruncationLimits limits = null!;

        // Act & Assert
        var action = () => this.strategy.Truncate(content, limits);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Truncate_ShouldStartWithOmissionMarker()
    {
        // Arrange
        var content = string.Join('\n', Enumerable.Range(1, 50).Select(i => $"Log entry {i}"));
        var limits = new TruncationLimits { InlineLimit = 300, TailLines = 5 };

        // Act
        var result = this.strategy.Truncate(content, limits);

        // Assert
        result.Content.Should().StartWith("...");
    }
}
