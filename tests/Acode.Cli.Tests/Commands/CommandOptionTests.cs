namespace Acode.Cli.Tests.Commands;

using Acode.Cli.Commands;
using FluentAssertions;

/// <summary>
/// Tests for <see cref="CommandOption"/>.
/// </summary>
public sealed class CommandOptionTests
{
    [Fact]
    public void CommandOption_WithRequiredParameters_ShouldBeCreated()
    {
        // Act.
        var option = new CommandOption("verbose", 'v', "Enable verbose output");

        // Assert.
        option.LongName.Should().Be("verbose");
        option.ShortName.Should().Be('v');
        option.Description.Should().Be("Enable verbose output");
        option.ValuePlaceholder.Should().BeNull();
        option.DefaultValue.Should().BeNull();
        option.IsRequired.Should().BeFalse();
        option.Group.Should().BeNull();
    }

    [Fact]
    public void CommandOption_WithAllParameters_ShouldBeCreated()
    {
        // Act.
        var option = new CommandOption(
            "config",
            'c',
            "Configuration file path",
            ValuePlaceholder: "PATH",
            DefaultValue: "./config.yml",
            IsRequired: true,
            Group: "Configuration"
        );

        // Assert.
        option.LongName.Should().Be("config");
        option.ShortName.Should().Be('c');
        option.Description.Should().Be("Configuration file path");
        option.ValuePlaceholder.Should().Be("PATH");
        option.DefaultValue.Should().Be("./config.yml");
        option.IsRequired.Should().BeTrue();
        option.Group.Should().Be("Configuration");
    }

    [Fact]
    public void AcceptsValue_WhenValuePlaceholderIsNull_ShouldBeFalse()
    {
        // Arrange.
        var option = new CommandOption("verbose", 'v', "Verbose output");

        // Act & Assert.
        option.AcceptsValue.Should().BeFalse();
    }

    [Fact]
    public void AcceptsValue_WhenValuePlaceholderIsSet_ShouldBeTrue()
    {
        // Arrange.
        var option = new CommandOption("config", 'c', "Config file", ValuePlaceholder: "PATH");

        // Act & Assert.
        option.AcceptsValue.Should().BeTrue();
    }

    [Fact]
    public void GetFormattedOption_WithBothShortAndLong_ShouldFormatCorrectly()
    {
        // Arrange.
        var option = new CommandOption("verbose", 'v', "Verbose output");

        // Act.
        var formatted = option.GetFormattedOption();

        // Assert.
        formatted.Should().Be("-v, --verbose");
    }

    [Fact]
    public void GetFormattedOption_WithLongOnly_ShouldFormatCorrectly()
    {
        // Arrange.
        var option = new CommandOption("verbose", null, "Verbose output");

        // Act.
        var formatted = option.GetFormattedOption();

        // Assert.
        formatted.Should().Be("--verbose");
    }

    [Fact]
    public void GetFormattedOption_WithValue_ShouldIncludePlaceholder()
    {
        // Arrange.
        var option = new CommandOption("config", 'c', "Config file", ValuePlaceholder: "PATH");

        // Act.
        var formatted = option.GetFormattedOption();

        // Assert.
        formatted.Should().Be("-c, --config <PATH>");
    }

    [Fact]
    public void GetFormattedOption_LongOnlyWithValue_ShouldFormatCorrectly()
    {
        // Arrange.
        var option = new CommandOption("output", null, "Output file", ValuePlaceholder: "FILE");

        // Act.
        var formatted = option.GetFormattedOption();

        // Assert.
        formatted.Should().Be("--output <FILE>");
    }
}
