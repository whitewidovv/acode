namespace Acode.Infrastructure.Tests.Truncation.Strategies;

using Acode.Application.Truncation;
using Acode.Infrastructure.Truncation.Strategies;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for HeadTailStrategy truncation.
/// </summary>
public sealed class HeadTailStrategyTests
{
    private readonly HeadTailStrategy strategy = new();

    [Fact]
    public void StrategyType_ShouldBeHeadTail()
    {
        // Assert
        strategy.StrategyType.Should().Be(TruncationStrategy.HeadTail);
    }

    [Fact]
    public void Truncate_ContentUnderLimit_ShouldNotTruncate()
    {
        // Arrange
        var content = "Short content";
        var limits = new TruncationLimits { InlineLimit = 100 };

        // Act
        var result = strategy.Truncate(content, limits);

        // Assert
        result.WasTruncated.Should().BeFalse();
        result.Content.Should().Be(content);
        result.Metadata.OriginalSize.Should().Be(content.Length);
    }

    [Fact]
    public void Truncate_ContentOverLimit_ShouldTruncate()
    {
        // Arrange
        var content = new string('x', 10000);
        var limits = new TruncationLimits { InlineLimit = 1000, HeadRatio = 0.6 };

        // Act
        var result = strategy.Truncate(content, limits);

        // Assert
        result.WasTruncated.Should().BeTrue();
        result.Content.Length.Should().BeLessThan(content.Length);
        result.Metadata.StrategyUsed.Should().Be(TruncationStrategy.HeadTail);
        result.Metadata.OmittedCharacters.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Truncate_ShouldIncludeOmissionMarker()
    {
        // Arrange
        var content = new string('x', 10000);
        var limits = new TruncationLimits { InlineLimit = 1000 };

        // Act
        var result = strategy.Truncate(content, limits);

        // Assert
        result.Content.Should().Contain("omitted");
    }

    [Fact]
    public void Truncate_ShouldPreserveHead()
    {
        // Arrange
        var content = "HEAD_START" + new string('x', 10000) + "TAIL_END";
        var limits = new TruncationLimits { InlineLimit = 500, HeadRatio = 0.5 };

        // Act
        var result = strategy.Truncate(content, limits);

        // Assert
        result.Content.Should().StartWith("HEAD_START");
    }

    [Fact]
    public void Truncate_ShouldPreserveTail()
    {
        // Arrange
        var content = "HEAD_START" + new string('x', 10000) + "TAIL_END";
        var limits = new TruncationLimits { InlineLimit = 500, HeadRatio = 0.5 };

        // Act
        var result = strategy.Truncate(content, limits);

        // Assert
        result.Content.Should().EndWith("TAIL_END");
    }

    [Fact]
    public void Truncate_WithNullContent_ShouldThrow()
    {
        // Arrange
        string content = null!;
        var limits = new TruncationLimits();

        // Act & Assert
        var action = () => strategy.Truncate(content, limits);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Truncate_WithNullLimits_ShouldThrow()
    {
        // Arrange
        var content = "test";
        TruncationLimits limits = null!;

        // Act & Assert
        var action = () => strategy.Truncate(content, limits);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Truncate_ShouldReportTokenEstimates()
    {
        // Arrange
        var content = new string('x', 10000);
        var limits = new TruncationLimits { InlineLimit = 1000 };

        // Act
        var result = strategy.Truncate(content, limits);

        // Assert
        result.Metadata.OriginalTokenEstimate.Should().Be(10000 / 4);
    }
}
