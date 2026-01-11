namespace Acode.Cli.Tests.Commands;

using Acode.Cli.Commands;
using FluentAssertions;

/// <summary>
/// Tests for <see cref="CommandMetadata"/> and <see cref="CommandMetadataBuilder"/>.
/// </summary>
public sealed class CommandMetadataTests
{
    [Fact]
    public void Create_ShouldCreateMinimalMetadata()
    {
        // Act.
        var metadata = CommandMetadata.Create("run", "Start an agent run");

        // Assert.
        metadata.Name.Should().Be("run");
        metadata.Description.Should().Be("Start an agent run");
        metadata.Usage.Should().Be("acode run");
        metadata.Aliases.Should().BeEmpty();
        metadata.Options.Should().BeEmpty();
        metadata.Examples.Should().BeEmpty();
        metadata.RelatedCommands.Should().BeEmpty();
        metadata.IsVisible.Should().BeTrue();
        metadata.Group.Should().BeNull();
    }

    [Fact]
    public void Builder_ShouldBuildMetadata()
    {
        // Arrange & Act.
        var metadata = CommandMetadata
            .Builder("run", "Start an agent run")
            .WithUsage("acode run [options] <task>")
            .WithAlias("r")
            .WithOption(new CommandOption("verbose", 'v', "Enable verbose output"))
            .WithExample("acode run \"Add tests\"", "Start a run with task")
            .WithRelatedCommand("resume")
            .WithVisibility(true)
            .InGroup("Core")
            .Build();

        // Assert.
        metadata.Name.Should().Be("run");
        metadata.Description.Should().Be("Start an agent run");
        metadata.Usage.Should().Be("acode run [options] <task>");
        metadata.Aliases.Should().ContainSingle().Which.Should().Be("r");
        metadata.Options.Should().ContainSingle();
        metadata.Examples.Should().ContainSingle();
        metadata.RelatedCommands.Should().ContainSingle().Which.Should().Be("resume");
        metadata.IsVisible.Should().BeTrue();
        metadata.Group.Should().Be("Core");
    }

    [Fact]
    public void Builder_WithMultipleAliases_ShouldAddAll()
    {
        // Act.
        var metadata = CommandMetadata
            .Builder("config", "Configuration")
            .WithAlias("cfg")
            .WithAlias("c")
            .Build();

        // Assert.
        metadata.Aliases.Should().HaveCount(2);
        metadata.Aliases.Should().Contain("cfg");
        metadata.Aliases.Should().Contain("c");
    }

    [Fact]
    public void Builder_WithMultipleOptions_ShouldAddAll()
    {
        // Act.
        var metadata = CommandMetadata
            .Builder("run", "Run")
            .WithOption(new CommandOption("verbose", 'v', "Verbose"))
            .WithOption(new CommandOption("quiet", 'q', "Quiet"))
            .Build();

        // Assert.
        metadata.Options.Should().HaveCount(2);
    }

    [Fact]
    public void Builder_WithExampleStrings_ShouldCreateExamples()
    {
        // Act.
        var metadata = CommandMetadata
            .Builder("run", "Run")
            .WithExample("acode run task", "Run with task")
            .Build();

        // Assert.
        var example = metadata.Examples.Should().ContainSingle().Subject;
        example.CommandLine.Should().Be("acode run task");
        example.Description.Should().Be("Run with task");
    }

    [Fact]
    public void Builder_WithExampleObject_ShouldAddExample()
    {
        // Arrange.
        var example = new CommandExample("acode run task", "Run with task");

        // Act.
        var metadata = CommandMetadata.Builder("run", "Run").WithExample(example).Build();

        // Assert.
        metadata.Examples.Should().ContainSingle().Which.Should().Be(example);
    }

    [Fact]
    public void Builder_WithHiddenVisibility_ShouldSetNotVisible()
    {
        // Act.
        var metadata = CommandMetadata
            .Builder("internal", "Internal command")
            .WithVisibility(false)
            .Build();

        // Assert.
        metadata.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void FullConstructor_ShouldSetAllProperties()
    {
        // Arrange.
        var aliases = new[] { "r" }.ToList();
        var options = new List<CommandOption> { new("verbose", 'v', "Verbose") };
        var examples = new List<CommandExample> { new("acode run", "Run") };
        var related = new[] { "resume" }.ToList();

        // Act.
        var metadata = new CommandMetadata(
            Name: "run",
            Description: "Start run",
            Usage: "acode run <task>",
            Aliases: aliases,
            Options: options,
            Examples: examples,
            RelatedCommands: related,
            IsVisible: false,
            Group: "Core"
        );

        // Assert.
        metadata.Name.Should().Be("run");
        metadata.Description.Should().Be("Start run");
        metadata.Usage.Should().Be("acode run <task>");
        metadata.Aliases.Should().BeEquivalentTo(new[] { "r" });
        metadata.Options.Should().HaveCount(1);
        metadata.Examples.Should().HaveCount(1);
        metadata.RelatedCommands.Should().BeEquivalentTo(new[] { "resume" });
        metadata.IsVisible.Should().BeFalse();
        metadata.Group.Should().Be("Core");
    }

    [Fact]
    public void RecordEquality_ShouldWorkCorrectly()
    {
        // Arrange.
        var metadata1 = CommandMetadata.Create("run", "Run");
        var metadata2 = CommandMetadata.Create("run", "Run");

        // Assert.
        metadata1.Should().Be(metadata2);
    }
}
