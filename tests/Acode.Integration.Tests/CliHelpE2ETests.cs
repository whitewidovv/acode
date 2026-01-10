using System.Diagnostics;
using FluentAssertions;

namespace Acode.Integration.Tests;

/// <summary>
/// End-to-end tests for CLI help and routing.
/// </summary>
public sealed class CliHelpE2ETests : IDisposable
{
    private readonly string _testDir;

    public CliHelpE2ETests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void GlobalHelp_ShouldDisplayAvailableCommands()
    {
        // Act
        var (exitCode, output, _) = RunAcodeCli(_testDir, "--help");

        // Assert
        exitCode.Should().Be(0, "help should succeed");
        output.Should().Contain("acode", "output should mention acode");
        output.Should().Contain("Usage", "output should show usage");
    }

    [Fact]
    public void CommandHelp_ForVersion_ShouldDisplayDetails()
    {
        // Act
        var (exitCode, output, _) = RunAcodeCli(_testDir, "version", "--help");

        // Assert
        exitCode.Should().Be(0, "help should succeed");
        output.Should().Contain("version", "output should mention command name");
    }

    [Fact]
    public void UnknownCommand_ShouldReturnError()
    {
        // Act
        var (exitCode, output, _) = RunAcodeCli(_testDir, "unknowncommand");

        // Assert
        exitCode.Should().NotBe(0, "unknown command should fail");
        output.Should().Contain("Unknown", "output should indicate unknown command");
    }

    [Fact]
    public void UnknownCommand_WithTypo_ShouldSuggestCorrection()
    {
        // Act
        var (exitCode, output, _) = RunAcodeCli(_testDir, "versoin");

        // Assert
        exitCode.Should().NotBe(0, "typo should fail");
        output.Should().Contain("version", "output should suggest correction");
    }

    [Fact]
    public void HelpWithNoColor_ShouldNotContainAnsiCodes()
    {
        // Act
        var (exitCode, output, _) = RunAcodeCli(_testDir, "--no-color", "--help");

        // Assert
        exitCode.Should().Be(0, "help should succeed");
        output.Should().NotContain("\u001b[", "output should not contain ANSI codes");
    }

    [Fact]
    public void Version_ShouldDisplayVersionInfo()
    {
        // Act
        var (exitCode, output, _) = RunAcodeCli(_testDir, "version");

        // Assert
        exitCode.Should().Be(0, "version should succeed");
        output.Should().Contain(".", "output should contain version number with dot separator");
    }

    private static (int ExitCode, string StdOut, string StdErr) RunAcodeCli(
        string workingDir,
        params string[] args)
    {
        // Find the CLI project by searching up from test assembly location
        var testAssembly = typeof(CliHelpE2ETests).Assembly.Location;
        var testDir = Path.GetDirectoryName(testAssembly)!;
        var solutionDir = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
        var cliProjectPath = Path.Combine(solutionDir, "src", "Acode.Cli", "Acode.Cli.csproj");

        if (!File.Exists(cliProjectPath))
        {
            throw new InvalidOperationException($"CLI project not found at: {cliProjectPath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{cliProjectPath}\" --no-build -- {string.Join(" ", args)}",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start process");
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, stdout, stderr);
    }
}
