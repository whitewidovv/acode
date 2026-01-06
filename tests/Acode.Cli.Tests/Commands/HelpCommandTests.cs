using Acode.Cli.Commands;
using FluentAssertions;
using NSubstitute;

namespace Acode.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="HelpCommand"/>.
/// </summary>
public class HelpCommandTests
{
    [Fact]
    public void Name_ReturnsHelp()
    {
        // Arrange
        var router = Substitute.For<ICommandRouter>();
        var command = new HelpCommand(router);

        // Act
        var name = command.Name;

        // Assert
        name.Should().Be("help");
    }

    [Fact]
    public void Aliases_ReturnsExpectedAliases()
    {
        // Arrange
        var router = Substitute.For<ICommandRouter>();
        var command = new HelpCommand(router);

        // Act
        var aliases = command.Aliases;

        // Assert
        aliases.Should().NotBeNull();
        aliases.Should().Contain("--help");
        aliases.Should().Contain("-h");
        aliases.Should().Contain("?");
    }

    [Fact]
    public void Description_ReturnsNonEmpty()
    {
        // Arrange
        var router = Substitute.For<ICommandRouter>();
        var command = new HelpCommand(router);

        // Act
        var description = command.Description;

        // Assert
        description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoCommands_ReturnsSuccess()
    {
        // Arrange
        var router = Substitute.For<ICommandRouter>();
        router.ListCommands().Returns(new List<ICommand>());

        var command = new HelpCommand(router);
        var context = CreateMockContext();

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithCommands_WritesCommandList()
    {
        // Arrange
        var mockCommand = Substitute.For<ICommand>();
        mockCommand.Name.Returns("test");
        mockCommand.Description.Returns("Test command");

        var router = Substitute.For<ICommandRouter>();
        router.ListCommands().Returns(new List<ICommand> { mockCommand });

        var command = new HelpCommand(router);
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = Array.Empty<string>(),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("test");
        outputText.Should().Contain("Test command");
    }

    [Fact]
    public async Task ExecuteAsync_FormatsCommandsAsTable()
    {
        // Arrange
        var cmd1 = Substitute.For<ICommand>();
        cmd1.Name.Returns("config");
        cmd1.Description.Returns("Manage configuration");

        var cmd2 = Substitute.For<ICommand>();
        cmd2.Name.Returns("chat");
        cmd2.Description.Returns("Start chat session");

        var router = Substitute.For<ICommandRouter>();
        router.ListCommands().Returns(new List<ICommand> { cmd1, cmd2 });

        var command = new HelpCommand(router);
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = Array.Empty<string>(),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("config");
        outputText.Should().Contain("chat");
        outputText.Should().Contain("Manage configuration");
        outputText.Should().Contain("Start chat session");
    }

    [Fact]
    public void GetHelp_ReturnsUsageInformation()
    {
        // Arrange
        var router = Substitute.For<ICommandRouter>();
        var command = new HelpCommand(router);

        // Act
        var help = command.GetHelp();

        // Assert
        help.Should().NotBeNullOrWhiteSpace();
        help.Should().Contain("help");
    }

    [Fact]
    public void Constructor_WithNullRouter_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new HelpCommand(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("router");
    }

    private static CommandContext CreateMockContext()
    {
        return new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = Array.Empty<string>(),
            Output = new StringWriter(),
            CancellationToken = CancellationToken.None,
        };
    }
}
