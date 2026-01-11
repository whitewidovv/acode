namespace Acode.Application.Tests.Truncation;

using Acode.Application.Truncation;
using FluentAssertions;

/// <summary>
/// Tests for TruncationLimits validation and defaults.
/// </summary>
public sealed class TruncationLimitsTests
{
    [Fact]
    public void DefaultInlineLimit_ShouldBe8000()
    {
        // Assert
        TruncationLimits.DefaultInlineLimit.Should().Be(8000);
    }

    [Fact]
    public void DefaultArtifactThreshold_ShouldBe50000()
    {
        // Assert
        TruncationLimits.DefaultArtifactThreshold.Should().Be(50000);
    }

    [Fact]
    public void DefaultMaxArtifactSize_ShouldBe10MB()
    {
        // Assert
        TruncationLimits.DefaultMaxArtifactSize.Should().Be(10 * 1024 * 1024);
    }

    [Fact]
    public void NewInstance_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var limits = new TruncationLimits();

        // Assert
        limits.InlineLimit.Should().Be(TruncationLimits.DefaultInlineLimit);
        limits.ArtifactThreshold.Should().Be(TruncationLimits.DefaultArtifactThreshold);
        limits.MaxArtifactSize.Should().Be(TruncationLimits.DefaultMaxArtifactSize);
        limits.HeadRatio.Should().Be(0.6);
        limits.TailLines.Should().Be(200);
        limits.HeadLines.Should().Be(300);
        limits.FirstElements.Should().Be(5);
        limits.LastElements.Should().Be(5);
    }

    [Fact]
    public void Validate_WithValidLimits_ShouldNotThrow()
    {
        // Arrange
        var limits = new TruncationLimits();

        // Act & Assert
        var action = () => limits.Validate();
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNegativeInlineLimit_ShouldThrow()
    {
        // Arrange
        var limits = new TruncationLimits { InlineLimit = -1 };

        // Act & Assert
        var action = () => limits.Validate();
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("InlineLimit");
    }

    [Fact]
    public void Validate_WithZeroInlineLimit_ShouldThrow()
    {
        // Arrange
        var limits = new TruncationLimits { InlineLimit = 0 };

        // Act & Assert
        var action = () => limits.Validate();
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("InlineLimit");
    }

    [Fact]
    public void Validate_WithNegativeArtifactThreshold_ShouldThrow()
    {
        // Arrange
        var limits = new TruncationLimits { ArtifactThreshold = -1 };

        // Act & Assert
        var action = () => limits.Validate();
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("ArtifactThreshold");
    }

    [Fact]
    public void Validate_WithInlineLimitExceedingThreshold_ShouldThrow()
    {
        // Arrange
        var limits = new TruncationLimits
        {
            InlineLimit = 100000,
            ArtifactThreshold = 50000
        };

        // Act & Assert
        var action = () => limits.Validate();
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("InlineLimit");
    }

    [Fact]
    public void Validate_WithHeadRatioLessThanZero_ShouldThrow()
    {
        // Arrange
        var limits = new TruncationLimits { HeadRatio = -0.1 };

        // Act & Assert
        var action = () => limits.Validate();
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("HeadRatio");
    }

    [Fact]
    public void Validate_WithHeadRatioGreaterThanOne_ShouldThrow()
    {
        // Arrange
        var limits = new TruncationLimits { HeadRatio = 1.1 };

        // Act & Assert
        var action = () => limits.Validate();
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("HeadRatio");
    }

    [Fact]
    public void Validate_WithZeroTailLines_ShouldThrow()
    {
        // Arrange
        var limits = new TruncationLimits { TailLines = 0 };

        // Act & Assert
        var action = () => limits.Validate();
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("TailLines");
    }

    [Fact]
    public void Validate_WithNegativeFirstElements_ShouldThrow()
    {
        // Arrange
        var limits = new TruncationLimits { FirstElements = -1 };

        // Act & Assert
        var action = () => limits.Validate();
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("FirstElements");
    }
}
