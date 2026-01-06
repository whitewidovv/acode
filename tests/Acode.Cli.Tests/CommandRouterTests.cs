using FluentAssertions;
using NSubstitute;

namespace Acode.Cli.Tests;

/// <summary>
/// Tests for <see cref="CommandRouter"/>.
/// </summary>
public class CommandRouterTests
{
    [Fact]
    public void RegisterCommand_AddsCommandToRegistry()
    {
        // Arrange
        var router = new CommandRouter();
        var command = CreateMockCommand("test", "Test command");

        // Act
        router.RegisterCommand(command);

        // Assert
        var retrieved = router.GetCommand("test");
        retrieved.Should().BeSameAs(command);
    }

    [Fact]
    public void RegisterCommand_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var router = new CommandRouter();
        var command1 = CreateMockCommand("test", "First");
        var command2 = CreateMockCommand("test", "Second");

        router.RegisterCommand(command1);

        // Act
        var act = () => router.RegisterCommand(command2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public void RegisterCommand_WithAlias_AllowsLookupByAlias()
    {
        // Arrange
        var router = new CommandRouter();
        var command = CreateMockCommand("config", "Config command", aliases: new[] { "cfg" });

        // Act
        router.RegisterCommand(command);

        // Assert
        var retrieved = router.GetCommand("cfg");
        retrieved.Should().BeSameAs(command);
    }

    [Fact]
    public void GetCommand_WithUnknownName_ReturnsNull()
    {
        // Arrange
        var router = new CommandRouter();

        // Act
        var result = router.GetCommand("unknown");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ListCommands_ReturnsAllRegisteredCommands()
    {
        // Arrange
        var router = new CommandRouter();
        var command1 = CreateMockCommand("test1", "First");
        var command2 = CreateMockCommand("test2", "Second");

        router.RegisterCommand(command1);
        router.RegisterCommand(command2);

        // Act
        var commands = router.ListCommands();

        // Assert
        commands.Should().HaveCount(2);
        commands.Should().Contain(command1);
        commands.Should().Contain(command2);
    }

    [Fact]
    public void SuggestCommands_WithSimilarName_ReturnsSuggestions()
    {
        // Arrange
        var router = new CommandRouter();
        router.RegisterCommand(CreateMockCommand("chat", "Chat command"));
        router.RegisterCommand(CreateMockCommand("config", "Config command"));

        // Act
        var suggestions = router.SuggestCommands("chatt");

        // Assert
        suggestions.Should().Contain("chat");
    }

    [Fact]
    public void SuggestCommands_WithVeryDifferentName_ReturnsEmpty()
    {
        // Arrange
        var router = new CommandRouter();
        router.RegisterCommand(CreateMockCommand("chat", "Chat command"));

        // Act
        var suggestions = router.SuggestCommands("xyz");

        // Assert
        suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task RouteAsync_WithValidCommand_ExecutesCommand()
    {
        // Arrange
        var router = new CommandRouter();
        var command = CreateMockCommand("test", "Test command");
        command.ExecuteAsync(Arg.Any<CommandContext>()).Returns(ExitCode.Success);

        router.RegisterCommand(command);

        var context = CreateMockContext();
        var args = new[] { "test" };

        // Act
        var exitCode = await router.RouteAsync(args, context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        await command.Received(1).ExecuteAsync(Arg.Any<CommandContext>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task RouteAsync_WithUnknownCommand_ReturnsInvalidArguments()
    {
        // Arrange
        var router = new CommandRouter();
        var context = CreateMockContext();
        var args = new[] { "unknown" };

        // Act
        var exitCode = await router.RouteAsync(args, context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.InvalidArguments);
    }

    [Fact]
    public async Task RouteAsync_WithEmptyArgs_ReturnsInvalidArguments()
    {
        // Arrange
        var router = new CommandRouter();
        var context = CreateMockContext();
        var args = Array.Empty<string>();

        // Act
        var exitCode = await router.RouteAsync(args, context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.InvalidArguments);
    }

    [Fact]
    public void Constructor_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var router = new CommandRouter();

        // Act
        var act = () => router.RegisterCommand(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("command");
    }

    private static ICommand CreateMockCommand(string name, string description, string[]? aliases = null)
    {
        var command = Substitute.For<ICommand>();
        command.Name.Returns(name);
        command.Description.Returns(description);
        command.Aliases.Returns(aliases);
        command.GetHelp().Returns($"Help for {name}");
        return command;
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
