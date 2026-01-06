namespace Acode.Application.Tests.Truncation;

using Acode.Application.Truncation;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for TruncationConfiguration.
/// </summary>
public sealed class TruncationConfigurationTests
{
    [Fact]
    public void NewInstance_ShouldHaveHeadTailAsDefaultStrategy()
    {
        // Arrange & Act
        var config = new TruncationConfiguration();

        // Assert
        config.DefaultStrategy.Should().Be(TruncationStrategy.HeadTail);
    }

    [Fact]
    public void NewInstance_ShouldHaveDefaultLimits()
    {
        // Arrange & Act
        var config = new TruncationConfiguration();

        // Assert
        config.DefaultLimits.Should().NotBeNull();
        config.DefaultLimits.InlineLimit.Should().Be(TruncationLimits.DefaultInlineLimit);
    }

    [Fact]
    public void NewInstance_ShouldHaveDefaultArtifactStoragePath()
    {
        // Arrange & Act
        var config = new TruncationConfiguration();

        // Assert
        config.ArtifactStoragePath.Should().Be(".acode/artifacts");
    }

    [Fact]
    public void NewInstance_ShouldHaveCleanupOnSessionEndEnabled()
    {
        // Arrange & Act
        var config = new TruncationConfiguration();

        // Assert
        config.CleanupOnSessionEnd.Should().BeTrue();
    }

    [Fact]
    public void CreateDefault_ShouldConfigureTailForExecuteCommand()
    {
        // Arrange & Act
        var config = TruncationConfiguration.CreateDefault();

        // Assert
        config.GetStrategyForTool("execute_command").Should().Be(TruncationStrategy.Tail);
    }

    [Fact]
    public void CreateDefault_ShouldConfigureTailForExecuteScript()
    {
        // Arrange & Act
        var config = TruncationConfiguration.CreateDefault();

        // Assert
        config.GetStrategyForTool("execute_script").Should().Be(TruncationStrategy.Tail);
    }

    [Fact]
    public void CreateDefault_ShouldConfigureHeadTailForReadFile()
    {
        // Arrange & Act
        var config = TruncationConfiguration.CreateDefault();

        // Assert
        config.GetStrategyForTool("read_file").Should().Be(TruncationStrategy.HeadTail);
    }

    [Fact]
    public void CreateDefault_ShouldConfigureElementForListDirectory()
    {
        // Arrange & Act
        var config = TruncationConfiguration.CreateDefault();

        // Assert
        config.GetStrategyForTool("list_directory").Should().Be(TruncationStrategy.Element);
    }

    [Fact]
    public void CreateDefault_ShouldConfigureElementForSearchFiles()
    {
        // Arrange & Act
        var config = TruncationConfiguration.CreateDefault();

        // Assert
        config.GetStrategyForTool("search_files").Should().Be(TruncationStrategy.Element);
    }

    [Fact]
    public void GetStrategyForTool_WithUnknownTool_ShouldReturnDefault()
    {
        // Arrange
        var config = TruncationConfiguration.CreateDefault();

        // Act
        var strategy = config.GetStrategyForTool("unknown_tool");

        // Assert
        strategy.Should().Be(config.DefaultStrategy);
    }

    [Fact]
    public void GetStrategyForTool_ShouldBeCaseInsensitive()
    {
        // Arrange
        var config = TruncationConfiguration.CreateDefault();

        // Act
        var lower = config.GetStrategyForTool("execute_command");
        var upper = config.GetStrategyForTool("EXECUTE_COMMAND");
        var mixed = config.GetStrategyForTool("Execute_Command");

        // Assert
        lower.Should().Be(upper);
        upper.Should().Be(mixed);
    }

    [Fact]
    public void GetLimitsForTool_WithNoOverride_ShouldReturnDefault()
    {
        // Arrange
        var config = new TruncationConfiguration();

        // Act
        var limits = config.GetLimitsForTool("any_tool");

        // Assert
        limits.Should().Be(config.DefaultLimits);
    }

    [Fact]
    public void GetLimitsForTool_WithOverride_ShouldReturnOverride()
    {
        // Arrange
        var config = new TruncationConfiguration();
        var customLimits = new TruncationLimits { InlineLimit = 5000 };
        config.ToolLimits["custom_tool"] = customLimits;

        // Act
        var limits = config.GetLimitsForTool("custom_tool");

        // Assert
        limits.Should().Be(customLimits);
        limits.InlineLimit.Should().Be(5000);
    }

    [Fact]
    public void GetLimitsForTool_ShouldBeCaseInsensitive()
    {
        // Arrange
        var config = new TruncationConfiguration();
        var customLimits = new TruncationLimits { InlineLimit = 5000 };
        config.ToolLimits["CustomTool"] = customLimits;

        // Act
        var limits = config.GetLimitsForTool("customtool");

        // Assert
        limits.InlineLimit.Should().Be(5000);
    }
}
