using System.Text.Json;
using Acode.Cli.Commands;
using FluentAssertions;

namespace Acode.Cli.Tests.Commands;

/// <summary>
/// Tests for ConfigMatrixCommand.
/// Verifies CLI access to mode matrix per Task 001a IT-001a-04.
/// </summary>
public sealed class ConfigMatrixCommandTests
{
    [Fact]
    public void Name_ReturnsMatrix()
    {
        // Arrange
        var command = new ConfigMatrixCommand();

        // Act
        var name = command.Name;

        // Assert
        name.Should().Be("matrix");
    }

    [Fact]
    public void Aliases_ReturnsEmpty()
    {
        // Arrange
        var command = new ConfigMatrixCommand();

        // Act
        var aliases = command.Aliases;

        // Assert
        aliases.Should().NotBeNull();
        aliases.Should().BeEmpty();
    }

    [Fact]
    public void Description_ReturnsNonEmpty()
    {
        // Arrange
        var command = new ConfigMatrixCommand();

        // Act
        var description = command.Description;

        // Assert
        description.Should().NotBeNullOrWhiteSpace();
        description.Should().Contain("matrix");
    }

    [Fact]
    public async Task ExecuteAsync_NoOptions_DisplaysFullMatrix()
    {
        // Arrange
        var command = new ConfigMatrixCommand();
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = Array.Empty<string>(),
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("LocalOnly");
        result.Should().Contain("Burst");
        result.Should().Contain("Airgapped");
        result.Should().Contain("| Mode | Capability |"); // Table header
    }

    [Fact]
    public async Task ExecuteAsync_WithModeFilter_OnlyDisplaysThatMode()
    {
        // Arrange
        var command = new ConfigMatrixCommand();
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "--mode", "LocalOnly" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("LocalOnly");
        result.Should().NotContain("Burst");
        result.Should().NotContain("Airgapped");
    }

    [Fact]
    public async Task ExecuteAsync_WithCapabilityFilter_ShowsCapabilityAcrossModes()
    {
        // Arrange
        var command = new ConfigMatrixCommand();
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "--capability", "OpenAiApi" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("OpenAiApi");
        result.Should().Contain("LocalOnly");
        result.Should().Contain("Burst");
        result.Should().Contain("Airgapped");
    }

    [Fact]
    public async Task ExecuteAsync_WithJsonFormat_OutputsJson()
    {
        // Arrange
        var command = new ConfigMatrixCommand();
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "--format", "json" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();

        // Should be valid JSON
        var action = () => JsonDocument.Parse(result);
        action.Should().NotThrow("output should be valid JSON");
    }

    [Fact]
    public async Task ExecuteAsync_WithTableFormat_OutputsTable()
    {
        // Arrange
        var command = new ConfigMatrixCommand();
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "--format", "table" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("| Mode | Capability |");
        result.Should().Contain("|------|------------|");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidMode_UsesNoFilter()
    {
        // Arrange
        var command = new ConfigMatrixCommand();
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "--mode", "InvalidMode" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();

        // Should display full matrix (all modes)
        result.Should().Contain("LocalOnly");
        result.Should().Contain("Burst");
        result.Should().Contain("Airgapped");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidCapability_DisplaysDefaultTable()
    {
        // Arrange
        var command = new ConfigMatrixCommand();
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "--capability", "InvalidCapability" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();

        // Should display full matrix table (invalid capability ignored)
        result.Should().Contain("| Mode | Capability |");
    }

    [Fact]
    public void GetHelp_ReturnsUsageInstructions()
    {
        // Arrange
        var command = new ConfigMatrixCommand();

        // Act
        var help = command.GetHelp();

        // Assert
        help.Should().NotBeNullOrWhiteSpace();
        help.Should().Contain("Usage:");
        help.Should().Contain("--mode");
        help.Should().Contain("--capability");
        help.Should().Contain("--format");
        help.Should().Contain("acode matrix");
        help.Should().Contain("Examples:");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var command = new ConfigMatrixCommand();

        // Act
        Func<Task> act = async () => await command.ExecuteAsync(null!).ConfigureAwait(true);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context")
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task ExecuteAsync_CombiningModeAndCapability_ShowsCapabilityComparison()
    {
        // Arrange - Capability filter takes precedence per implementation
        var command = new ConfigMatrixCommand();
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "--mode", "LocalOnly", "--capability", "OllamaLocal" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();

        // Capability filter takes precedence, shows capability across all modes
        result.Should().Contain("OllamaLocal");
        result.Should().Contain("LocalOnly");
        result.Should().Contain("Burst");
        result.Should().Contain("Airgapped");
    }

    [Fact]
    public async Task ExecuteAsync_WithCaseInsensitiveMode_Works()
    {
        // Arrange
        var command = new ConfigMatrixCommand();
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "--mode", "localonly" }, // lowercase
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("LocalOnly");
        result.Should().NotContain("Burst");
        result.Should().NotContain("Airgapped");
    }

    [Fact]
    public async Task ExecuteAsync_WithCaseInsensitiveCapability_Works()
    {
        // Arrange
        var command = new ConfigMatrixCommand();
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "--capability", "openaiaPI" }, // mixed case
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("OpenAiApi");
    }

    [Fact]
    public async Task ExecuteAsync_JsonFormatWithModeFilter_StillOutputsFullJson()
    {
        // Arrange - JSON format ignores mode filter per implementation
        var command = new ConfigMatrixCommand();
        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "--format", "json", "--mode", "LocalOnly" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();

        // JSON format outputs full matrix, mode filter ignored
        var json = JsonDocument.Parse(result);
        json.RootElement.GetArrayLength().Should().Be(78, "JSON should contain all 78 entries");
    }
}
