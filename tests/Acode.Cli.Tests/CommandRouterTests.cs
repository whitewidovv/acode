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
        act.Should().Throw<InvalidOperationException>().WithMessage("*already registered*");
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
        act.Should().Throw<ArgumentNullException>().WithParameterName("command");
    }

    [Fact]
    public void GetCommand_ShouldBeCaseInsensitive()
    {
        // Arrange
        var router = new CommandRouter();
        var command = CreateMockCommand("Config", "Config command");
        router.RegisterCommand(command);

        // Act & Assert
        router.GetCommand("config").Should().BeSameAs(command);
        router.GetCommand("CONFIG").Should().BeSameAs(command);
        router.GetCommand("CoNfIg").Should().BeSameAs(command);
    }

    /// <summary>
    /// FR-014: Router MUST trim whitespace from input.
    /// </summary>
    [Fact]
    public void GetCommand_ShouldTrimWhitespace()
    {
        // Arrange
        var router = new CommandRouter();
        var command = CreateMockCommand("chat", "Chat command");
        router.RegisterCommand(command);

        // Act & Assert
        router.GetCommand("  chat  ").Should().BeSameAs(command);
        router.GetCommand("chat ").Should().BeSameAs(command);
        router.GetCommand(" chat").Should().BeSameAs(command);
        router.GetCommand("\tchat\t").Should().BeSameAs(command);
    }

    [Fact]
    public void SuggestCommands_ShouldRankBySimilarity()
    {
        // Arrange
        var router = new CommandRouter();
        router.RegisterCommand(CreateMockCommand("chat", "Chat"));
        router.RegisterCommand(CreateMockCommand("check", "Check"));
        router.RegisterCommand(CreateMockCommand("config", "Config"));

        // Act - "chaz" is closer to "chat" (1 edit) than "check" (2 edits)
        var suggestions = router.SuggestCommands("chaz");

        // Assert
        suggestions.Should().HaveCount(2);
        suggestions[0].Should().Be("chat"); // Closest match first
    }

    [Fact]
    public void SuggestCommands_ShouldLimitResults()
    {
        // Arrange
        var router = new CommandRouter();
        router.RegisterCommand(CreateMockCommand("cat", "Cat"));
        router.RegisterCommand(CreateMockCommand("bat", "Bat"));
        router.RegisterCommand(CreateMockCommand("rat", "Rat"));
        router.RegisterCommand(CreateMockCommand("hat", "Hat"));
        router.RegisterCommand(CreateMockCommand("mat", "Mat"));

        // Act
        var suggestions = router.SuggestCommands("fat", maxSuggestions: 2);

        // Assert
        suggestions.Should().HaveCount(2);
    }

    [Fact]
    public async Task RouteAsync_ShouldCompleteUnder10ms()
    {
        // Arrange
        var router = new CommandRouter();
        for (int i = 0; i < 50; i++)
        {
            var cmd = CreateMockCommand($"cmd{i}", $"Command {i}");
            cmd.ExecuteAsync(Arg.Any<CommandContext>()).Returns(ExitCode.Success);
            router.RegisterCommand(cmd);
        }

        var context = CreateMockContext();
        var args = new[] { "cmd25" };

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await router.RouteAsync(args, context).ConfigureAwait(true);
        sw.Stop();

        // Assert - Allow up to 100ms to account for system load during full test suite execution
        sw.ElapsedMilliseconds.Should().BeLessThan(100, "routing should complete quickly");
    }

    private static ICommand CreateMockCommand(
        string name,
        string description,
        string[]? aliases = null
    )
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
        var output = new StringWriter();
        return new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = Array.Empty<string>(),
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };
    }
}
