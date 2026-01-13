namespace Acode.Cli.Tests.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Cli;
using Acode.Cli.Commands;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for <see cref="ProvidersCommand"/>.
/// </summary>
public sealed class ProvidersCommandTests
{
    private readonly StringWriter output;
    private readonly CommandContext baseContext;

    public ProvidersCommandTests()
    {
        this.output = new StringWriter();
        this.baseContext = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = Array.Empty<string>(),
            Formatter = new ConsoleFormatter(this.output, enableColors: false),
            Output = this.output,
            CancellationToken = CancellationToken.None
        };
    }

    [Fact]
    public void Name_ReturnsProviders()
    {
        // Arrange
        var command = new ProvidersCommand();

        // Act
        var name = command.Name;

        // Assert
        name.Should().Be("providers");
    }

    [Fact]
    public void Description_ReturnsNonEmpty()
    {
        // Arrange
        var command = new ProvidersCommand();

        // Act
        var description = command.Description;

        // Assert
        description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoArgs_ShowsHelp()
    {
        // Arrange
        var command = new ProvidersCommand();
        var context = this.baseContext with { Args = Array.Empty<string>() };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.InvalidArguments);
        var outputText = this.output.ToString();
        outputText.Should().Contain("Usage:");
        outputText.Should().Contain("smoke-test");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidSubcommand_ShowsError()
    {
        // Arrange
        var command = new ProvidersCommand();
        var context = this.baseContext with { Args = new[] { "invalid-subcommand" } };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.InvalidArguments);
        var outputText = this.output.ToString();
        outputText.Should().Contain("Unknown subcommand");
    }

    [Fact]
    public async Task SmokeTest_WithNoProvider_ShowsError()
    {
        // Arrange
        var command = new ProvidersCommand();
        var context = this.baseContext with { Args = new[] { "smoke-test" } };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.InvalidArguments);
        var outputText = this.output.ToString();
        outputText.Should().Contain("provider");
    }

    [Fact]
    public async Task SmokeTest_WithUnsupportedProvider_ShowsError()
    {
        // Arrange
        var command = new ProvidersCommand();
        var context = this.baseContext with { Args = new[] { "smoke-test", "unsupported" } };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.InvalidArguments);
        var outputText = this.output.ToString();
        outputText.Should().Contain("Only 'ollama' provider is supported");
    }

    [Fact]
    public async Task SmokeTest_Ollama_ParsesVerboseFlag()
    {
        // Arrange
        var command = new ProvidersCommand();
        var context = this.baseContext with
        {
            Args = new[] { "smoke-test", "ollama", "--verbose" }
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert - Verbose flag causes detailed output
        var outputText = this.output.ToString();
        outputText.Should().Contain("Running Ollama Smoke Tests");
    }

    [Fact]
    public async Task SmokeTest_Ollama_ParsesSkipToolTestFlag()
    {
        // Arrange
        var command = new ProvidersCommand();
        var context = this.baseContext with
        {
            Args = new[] { "smoke-test", "ollama", "--skip-tool-test" }
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert - Should complete (may pass or fail depending on Ollama availability)
        // We just verify it parsed the flag without error
        exitCode.Should().BeOneOf(ExitCode.Success, ExitCode.RuntimeError);
    }

    [Fact]
    public async Task SmokeTest_Ollama_ParsesModelFlag()
    {
        // Arrange
        var command = new ProvidersCommand();
        var context = this.baseContext with
        {
            Args = new[] { "smoke-test", "ollama", "--model", "custom-model:latest" }
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().BeOneOf(ExitCode.Success, ExitCode.RuntimeError);
    }

    [Fact]
    public async Task SmokeTest_Ollama_ParsesTimeoutFlag()
    {
        // Arrange
        var command = new ProvidersCommand();
        var context = this.baseContext with
        {
            Args = new[] { "smoke-test", "ollama", "--timeout", "10" }
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().BeOneOf(ExitCode.Success, ExitCode.RuntimeError);
    }

    [Fact]
    public async Task SmokeTest_Ollama_ParsesEndpointFlag()
    {
        // Arrange
        var command = new ProvidersCommand();
        var context = this.baseContext with
        {
            Args = new[] { "smoke-test", "ollama", "--endpoint", "http://custom:11434" }
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().BeOneOf(ExitCode.Success, ExitCode.RuntimeError);
    }

    [Fact]
    public async Task SmokeTest_Ollama_ReturnsSuccessOrError()
    {
        // Arrange
        var command = new ProvidersCommand();
        var context = this.baseContext with
        {
            Args = new[] { "smoke-test", "ollama" }
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert - Will succeed if Ollama is running, error otherwise
        exitCode.Should().BeOneOf(ExitCode.Success, ExitCode.RuntimeError);
        var outputText = this.output.ToString();
        outputText.Should().NotBeEmpty();
    }

    [Fact]
    public void GetHelp_ContainsSmokeTestUsage()
    {
        // Arrange
        var command = new ProvidersCommand();

        // Act
        var help = command.GetHelp();

        // Assert
        help.Should().Contain("smoke-test");
        help.Should().Contain("ollama");
        help.Should().Contain("--verbose");
        help.Should().Contain("--skip-tool-test");
        help.Should().Contain("--model");
        help.Should().Contain("--timeout");
        help.Should().Contain("--endpoint");
    }
}
