namespace Acode.Cli.Tests.Commands;

using Acode.Cli.Commands;
using FluentAssertions;

/// <summary>
/// Tests for <see cref="CommandExample"/>.
/// </summary>
public sealed class CommandExampleTests
{
    [Fact]
    public void CommandExample_ShouldStoreValues()
    {
        // Act.
        var example = new CommandExample(
            "acode run \"Add unit tests\"",
            "Start a new agent run with a task description"
        );

        // Assert.
        example.CommandLine.Should().Be("acode run \"Add unit tests\"");
        example.Description.Should().Be("Start a new agent run with a task description");
    }

    [Fact]
    public void GetFormatted_WithZeroIndent_ShouldFormat()
    {
        // Arrange.
        var example = new CommandExample("acode help", "Show help");

        // Act.
        var formatted = example.GetFormatted(0);

        // Assert.
        formatted.Should().Contain("acode help");
        formatted.Should().Contain("Show help");
    }

    [Fact]
    public void GetFormatted_WithIndent_ShouldAddSpaces()
    {
        // Arrange.
        var example = new CommandExample("acode help", "Show help");

        // Act.
        var formatted = example.GetFormatted(4);

        // Assert.
        formatted.Should().StartWith("    $ acode help");
    }

    [Fact]
    public void RecordEquality_ShouldWorkCorrectly()
    {
        // Arrange.
        var example1 = new CommandExample("acode run", "Run agent");
        var example2 = new CommandExample("acode run", "Run agent");

        // Assert.
        example1.Should().Be(example2);
    }

    [Fact]
    public void CommandExample_ShouldBeImmutable()
    {
        // Arrange.
        var example = new CommandExample("cmd", "desc");

        // Act.
        var modified = example with
        {
            CommandLine = "newcmd",
        };

        // Assert.
        example.CommandLine.Should().Be("cmd");
        modified.CommandLine.Should().Be("newcmd");
    }
}
