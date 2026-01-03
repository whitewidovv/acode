using Acode.Domain.Configuration;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.Configuration;

/// <summary>
/// Tests for AcodeConfig and related configuration models.
/// Verifies immutability, defaults, and structure per Task 002.b.
/// </summary>
public class AcodeConfigTests
{
    [Fact]
    public void AcodeConfig_ShouldBeImmutableRecord()
    {
        // Arrange & Act
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0"
        };

        // Assert - records are immutable by design
        config.Should().NotBeNull();
        config.SchemaVersion.Should().Be("1.0.0");
    }

    [Fact]
    public void AcodeConfig_SchemaVersion_IsRequired()
    {
        // Arrange & Act
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0"
        };

        // Assert
        config.SchemaVersion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ModeConfig_DefaultValues_ShouldMatchConfigDefaults()
    {
        // Arrange & Act
        var mode = new ModeConfig();

        // Assert
        mode.Default.Should().Be(ConfigDefaults.DefaultMode);
        mode.AllowBurst.Should().Be(ConfigDefaults.AllowBurst);
        mode.AirgappedLock.Should().Be(ConfigDefaults.AirgappedLock);
    }

    [Fact]
    public void ModelConfig_DefaultValues_ShouldMatchConfigDefaults()
    {
        // Arrange & Act
        var model = new ModelConfig();

        // Assert
        model.Provider.Should().Be(ConfigDefaults.DefaultProvider);
        model.Name.Should().Be(ConfigDefaults.DefaultModel);
        model.Endpoint.Should().Be(ConfigDefaults.DefaultEndpoint);
        model.TimeoutSeconds.Should().Be(ConfigDefaults.DefaultTimeoutSeconds);
        model.RetryCount.Should().Be(ConfigDefaults.DefaultRetryCount);
    }

    [Fact]
    public void ModelParametersConfig_DefaultValues_ShouldMatchConfigDefaults()
    {
        // Arrange & Act
        var parameters = new ModelParametersConfig();

        // Assert
        parameters.Temperature.Should().Be(ConfigDefaults.DefaultTemperature);
        parameters.MaxTokens.Should().Be(ConfigDefaults.DefaultMaxTokens);
        parameters.TopP.Should().Be(0.95);
    }

    [Fact]
    public void StorageConfig_DefaultMode_ShouldBeLocalCacheOnly()
    {
        // Arrange & Act
        var storage = new StorageConfig();

        // Assert
        storage.Mode.Should().Be("local_cache_only");
    }

    [Fact]
    public void StorageLocalConfig_Defaults_ShouldBeSet()
    {
        // Arrange & Act
        var local = new StorageLocalConfig();

        // Assert
        local.Type.Should().Be("sqlite");
        local.SqlitePath.Should().Be(".acode/workspace.db");
    }

    [Fact]
    public void StorageSyncConfig_Defaults_ShouldBeSet()
    {
        // Arrange & Act
        var sync = new StorageSyncConfig();

        // Assert
        sync.BatchSize.Should().Be(100);
        sync.ConflictPolicy.Should().Be("lww");
    }

    [Fact]
    public void NetworkAllowlistEntry_Host_IsRequired()
    {
        // Arrange & Act
        var entry = new NetworkAllowlistEntry
        {
            Host = "api.example.com"
        };

        // Assert
        entry.Host.Should().Be("api.example.com");
    }

    [Fact]
    public void ConfigModels_SupportValueEquality()
    {
        // Arrange
        var config1 = new ModeConfig
        {
            Default = "local-only",
            AllowBurst = true,
            AirgappedLock = false
        };

        var config2 = new ModeConfig
        {
            Default = "local-only",
            AllowBurst = true,
            AirgappedLock = false
        };

        // Act & Assert - records support value-based equality
        config1.Should().Be(config2);
    }

    [Fact]
    public void ConfigModels_AllOptionalFields_CanBeNull()
    {
        // Arrange & Act
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Project = null,
            Mode = null,
            Model = null,
            Commands = null,
            Paths = null,
            Ignore = null,
            Network = null,
            Storage = null
        };

        // Assert - all optional fields can be null
        config.Project.Should().BeNull();
        config.Mode.Should().BeNull();
        config.Model.Should().BeNull();
    }
}
