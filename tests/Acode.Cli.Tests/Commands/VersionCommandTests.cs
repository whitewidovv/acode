using Acode.Cli.Commands;
using FluentAssertions;

namespace Acode.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="VersionCommand"/>.
/// </summary>
public class VersionCommandTests
{
    [Fact]
    public void Name_ReturnsVersion()
    {
        // Arrange
        var command = new VersionCommand();

        // Act
        var name = command.Name;

        // Assert
        name.Should().Be("version");
    }

    [Fact]
    public void Aliases_ReturnsExpectedAliases()
    {
        // Arrange
        var command = new VersionCommand();

        // Act
        var aliases = command.Aliases;

        // Assert
        aliases.Should().NotBeNull();
        aliases.Should().Contain("--version");
        aliases.Should().Contain("-v");
    }

    [Fact]
    public void Description_ReturnsNonEmpty()
    {
        // Arrange
        var command = new VersionCommand();

        // Act
        var description = command.Description;

        // Assert
        description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExecuteAsync_WritesVersionInformation()
    {
        // Arrange
        var command = new VersionCommand();
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("Acode");
        outputText.Should().MatchRegex(@"\d+\.\d+\.\d+"); // Semantic version pattern
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccess()
    {
        // Arrange
        var command = new VersionCommand();
        var context = CreateMockContext();

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
    }

    [Fact]
    public void GetHelp_ReturnsUsageInformation()
    {
        // Arrange
        var command = new VersionCommand();

        // Act
        var help = command.GetHelp();

        // Assert
        help.Should().NotBeNullOrWhiteSpace();
        help.Should().Contain("version");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var command = new VersionCommand();

        // Act
        Func<Task> act = async () => await command.ExecuteAsync(null!).ConfigureAwait(true);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context")
            .ConfigureAwait(true);
    }

    private static CommandContext CreateMockContext()
    {
        return new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Output = new StringWriter(),
            CancellationToken = CancellationToken.None,
        };
    }
}
