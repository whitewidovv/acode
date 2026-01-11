namespace Acode.Infrastructure.Tests.Audit;

using System.IO;
using Acode.Domain.Audit;
using Acode.Infrastructure.Audit;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for AuditConfigurationLoader.
/// Verifies YAML config loading, validation, and defaults.
/// </summary>
public sealed class AuditConfigurationLoaderTests : IDisposable
{
    private readonly string _testDir;

    public AuditConfigurationLoaderTests()
    {
        _testDir = Path.Combine(
            Path.GetTempPath(),
            $"audit_config_loader_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void Should_LoadValidConfig()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "config.yml");
        var yaml = """
            audit:
              enabled: true
              level: warning
              directory: custom/audit/path
              retention_days: 30
              rotation_size_mb: 25
              rotation_interval: hourly
              max_total_storage_mb: 2048
            """;
        File.WriteAllText(configPath, yaml);

        var loader = new AuditConfigurationLoader();

        // Act
        var config = loader.Load(configPath);

        // Assert
        config.Should().NotBeNull();
        config.Enabled.Should().BeTrue();
        config.LogLevel.Should().Be(AuditSeverity.Warning);
        config.LogDirectory.Should().Be("custom/audit/path");
        config.RetentionDays.Should().Be(30);
        config.RotationSizeMb.Should().Be(25);
        config.RotationInterval.Should().Be(RotationInterval.Hourly);
        config.MaxTotalStorage.Should().Be(2048L * 1024 * 1024); // MB to bytes
    }

    [Fact]
    public void Should_ApplyDefaultsWhenConfigMissing()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "empty-config.yml");
        File.WriteAllText(configPath, "# No audit section");

        var loader = new AuditConfigurationLoader();

        // Act
        var config = loader.Load(configPath);

        // Assert
        config.Should().NotBeNull();
        config.Enabled.Should().BeTrue();
        config.LogLevel.Should().Be(AuditSeverity.Info);
        config.LogDirectory.Should().Be(".acode/logs");
        config.RetentionDays.Should().Be(90);
        config.RotationSizeMb.Should().Be(10);
        config.RotationInterval.Should().Be(RotationInterval.Daily);
        config.MaxTotalStorage.Should().Be(1024L * 1024 * 1024); // 1GB
    }

    [Fact]
    public void Should_ApplyDefaultsForPartialConfig()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "partial-config.yml");
        var yaml = """
            audit:
              enabled: false
              retention_days: 60
            """;
        File.WriteAllText(configPath, yaml);

        var loader = new AuditConfigurationLoader();

        // Act
        var config = loader.Load(configPath);

        // Assert
        config.Enabled.Should().BeFalse();
        config.RetentionDays.Should().Be(60);
        config.LogLevel.Should().Be(AuditSeverity.Info); // Default
        config.RotationSizeMb.Should().Be(10); // Default
    }

    [Fact]
    public void Should_RejectNegativeRetentionDays()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "invalid-retention.yml");
        var yaml = """
            audit:
              retention_days: -10
            """;
        File.WriteAllText(configPath, yaml);

        var loader = new AuditConfigurationLoader();

        // Act & Assert
        var action = () => loader.Load(configPath);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*retention_days*negative*");
    }

    [Fact]
    public void Should_RejectZeroRetentionDays()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "zero-retention.yml");
        var yaml = """
            audit:
              retention_days: 0
            """;
        File.WriteAllText(configPath, yaml);

        var loader = new AuditConfigurationLoader();

        // Act & Assert
        var action = () => loader.Load(configPath);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*retention_days*positive*");
    }

    [Fact]
    public void Should_RejectNegativeRotationSize()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "invalid-rotation.yml");
        var yaml = """
            audit:
              rotation_size_mb: -5
            """;
        File.WriteAllText(configPath, yaml);

        var loader = new AuditConfigurationLoader();

        // Act & Assert
        var action = () => loader.Load(configPath);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*rotation_size_mb*positive*");
    }

    [Fact]
    public void Should_RejectInvalidLogLevel()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "invalid-level.yml");
        var yaml = """
            audit:
              level: invalid_level
            """;
        File.WriteAllText(configPath, yaml);

        var loader = new AuditConfigurationLoader();

        // Act & Assert
        var action = () => loader.Load(configPath);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*level*");
    }

    [Fact]
    public void Should_RejectInvalidRotationInterval()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "invalid-interval.yml");
        var yaml = """
            audit:
              rotation_interval: invalid_interval
            """;
        File.WriteAllText(configPath, yaml);

        var loader = new AuditConfigurationLoader();

        // Act & Assert
        var action = () => loader.Load(configPath);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*rotation_interval*");
    }

    [Fact]
    public void Should_HandleNonExistentFile()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "nonexistent.yml");
        var loader = new AuditConfigurationLoader();

        // Act & Assert
        var action = () => loader.Load(configPath);
        action.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Should_HandleMalformedYaml()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "malformed.yml");
        var yaml = """
            audit:
              level: info
                bad_indentation: true
              more: stuff
            """;
        File.WriteAllText(configPath, yaml);

        var loader = new AuditConfigurationLoader();

        // Act & Assert
        var action = () => loader.Load(configPath);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*YAML*");
    }

    [Fact]
    public void Should_ConvertRotationSizeMbToBytes()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "rotation-mb.yml");
        var yaml = """
            audit:
              rotation_size_mb: 50
            """;
        File.WriteAllText(configPath, yaml);

        var loader = new AuditConfigurationLoader();

        // Act
        var config = loader.Load(configPath);

        // Assert
        config.MaxFileSize.Should().Be(50L * 1024 * 1024);
    }

    [Fact]
    public void Should_ConvertStorageMbToBytes()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "storage-mb.yml");
        var yaml = """
            audit:
              max_total_storage_mb: 512
            """;
        File.WriteAllText(configPath, yaml);

        var loader = new AuditConfigurationLoader();

        // Act
        var config = loader.Load(configPath);

        // Assert
        config.MaxTotalStorage.Should().Be(512L * 1024 * 1024);
    }

    [Fact]
    public void Should_HandleDebugLogLevel()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "debug-level.yml");
        var yaml = """
            audit:
              level: debug
            """;
        File.WriteAllText(configPath, yaml);

        var loader = new AuditConfigurationLoader();

        // Act
        var config = loader.Load(configPath);

        // Assert
        config.LogLevel.Should().Be(AuditSeverity.Debug);
    }

    [Fact]
    public void Should_HandleErrorLogLevel()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "error-level.yml");
        var yaml = """
            audit:
              level: error
            """;
        File.WriteAllText(configPath, yaml);

        var loader = new AuditConfigurationLoader();

        // Act
        var config = loader.Load(configPath);

        // Assert
        config.LogLevel.Should().Be(AuditSeverity.Error);
    }
}
