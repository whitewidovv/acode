using System.Diagnostics;
using FluentAssertions;

namespace Acode.Integration.Tests;

/// <summary>
/// End-to-end tests for config commands via CLI.
/// </summary>
public class ConfigE2ETests : IDisposable
{
    private readonly string _testDir;
    private readonly string _configPath;

    public ConfigE2ETests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        _configPath = Path.Combine(_testDir, ".agent", "config.yml");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ConfigValidate_WithValidConfig_SucceedsWithCheckmarks()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        var configYaml = @"schema_version: ""1.0.0""
project:
  name: test-project
  type: dotnet
mode:
  default: local-only
";
        File.WriteAllText(_configPath, configYaml);

        // Act
        var (exitCode, output, _) = RunAcodeCli(_testDir, "config", "validate");

        // Assert
        exitCode.Should().Be(0, "valid config should succeed");
        output.Should().Contain("✓", "output should contain success markers");
        output.Should().Contain("valid", "output should confirm validation");
        output.Should().Contain("1.0.0", "output should show schema version");
    }

    [Fact]
    public void ConfigValidate_WithInvalidConfig_FailsWithErrors()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        var configYaml = @"schema_version: ""999.0.0""
";
        File.WriteAllText(_configPath, configYaml);

        // Act
        var (exitCode, output, _) = RunAcodeCli(_testDir, "config", "validate");

        // Assert
        exitCode.Should().Be(1, "invalid config should fail");
        output.Should().Contain("invalid", "output should show validation failure");
    }

    [Fact]
    public void ConfigValidate_WithMissingFile_FailsWithError()
    {
        // Act (no config file created)
        var (exitCode, output, _) = RunAcodeCli(_testDir, "config", "validate");

        // Assert
        exitCode.Should().Be(1, "missing file should fail");
        output.Should().Contain("not found", "output should explain file not found");
    }

    [Fact]
    public void ConfigShow_WithValidConfig_DisplaysYaml()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        var configYaml = @"schema_version: ""1.0.0""
project:
  name: test-project
";
        File.WriteAllText(_configPath, configYaml);

        // Act
        var (exitCode, output, _) = RunAcodeCli(_testDir, "config", "show");

        // Assert
        exitCode.Should().Be(0, "show should succeed");
        output.Should().Contain("schema_version", "output should contain config");
        output.Should().Contain("test-project", "output should contain project name");
    }

    [Fact]
    public void ConfigShow_WithJsonFormat_DisplaysJson()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        var configYaml = @"schema_version: ""1.0.0""
project:
  name: test-project
";
        File.WriteAllText(_configPath, configYaml);

        // Act
        var (exitCode, output, _) = RunAcodeCli(_testDir, "config", "show", "--format", "json");

        // Assert
        exitCode.Should().Be(0, "show JSON should succeed");
        output.Should().Contain("{", "output should be JSON");
        output.Should().Contain("\"schema_version\"", "output should contain JSON keys");
        output.Should().Contain("\"test-project\"", "output should contain project name");
    }

    private static (int ExitCode, string StdOut, string StdErr) RunAcodeCli(string workingDir, params string[] args)
    {
        // Find the CLI project by searching up from test assembly location
        var testAssembly = typeof(ConfigE2ETests).Assembly.Location;
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
