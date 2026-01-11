using Acode.Application.Commands;
using FluentAssertions;

namespace Acode.Application.Tests.Commands;

/// <summary>
/// Tests for PlatformDetector class.
/// Covers platform detection per Task 002.c spec lines FR-002c-111 through FR-002c-120.
/// </summary>
public class PlatformDetectorTests
{
    // UT-002c-16: Detect platform variants → Correct platform selected
    [Fact]
    public void GetCurrentPlatform_ReturnsValidPlatform()
    {
        // Act
        var platform = PlatformDetector.GetCurrentPlatform();

        // Assert
        platform.Should().BeOneOf("windows", "linux", "macos", "per FR-002c-112: platform identifiers must be windows, linux, or macos");
    }

    [Fact]
    public void GetCurrentPlatform_IsDeterministic()
    {
        // Arrange
        var platform1 = PlatformDetector.GetCurrentPlatform();

        // Act
        var platform2 = PlatformDetector.GetCurrentPlatform();

        // Assert
        platform1.Should().Be(platform2, "per FR-002c-117: platform detection must be deterministic");
    }

    // UT-002c-17: Fall back to default → No variant uses default
    [Fact]
    public void SelectCommand_WithNoVariants_ReturnsDefault()
    {
        // Arrange
        var defaultCommand = "build.sh";
        IReadOnlyDictionary<string, string>? variants = null;

        // Act
        var selected = PlatformDetector.SelectCommand(defaultCommand, variants);

        // Assert
        selected.Should().Be(defaultCommand, "per FR-002c-115: missing platform variant should use default");
    }

    [Fact]
    public void SelectCommand_WithEmptyVariants_ReturnsDefault()
    {
        // Arrange
        var defaultCommand = "build.sh";
        var variants = new Dictionary<string, string>();

        // Act
        var selected = PlatformDetector.SelectCommand(defaultCommand, variants);

        // Assert
        selected.Should().Be(defaultCommand);
    }

    [Fact]
    public void SelectCommand_WithMatchingVariant_ReturnsVariant()
    {
        // Arrange
        var defaultCommand = "build.sh";
        var currentPlatform = PlatformDetector.GetCurrentPlatform();
        var variants = new Dictionary<string, string>
        {
            { currentPlatform, $"build-{currentPlatform}.sh" }
        };

        // Act
        var selected = PlatformDetector.SelectCommand(defaultCommand, variants);

        // Assert
        selected.Should().Be($"build-{currentPlatform}.sh", "per FR-002c-114: platform variant should override default");
    }

    [Fact]
    public void SelectCommand_WithoutMatchingVariant_ReturnsDefault()
    {
        // Arrange
        var defaultCommand = "build.sh";
        var variants = new Dictionary<string, string>
        {
            { "nonexistent-platform", "build-other.sh" }
        };

        // Act
        var selected = PlatformDetector.SelectCommand(defaultCommand, variants);

        // Assert
        selected.Should().Be(defaultCommand, "per FR-002c-115: missing platform variant should use default");
    }
}
