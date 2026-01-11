using Acode.Application.Configuration;
using Acode.Domain.Configuration;
using FluentAssertions;

namespace Acode.Application.Tests.Configuration;

/// <summary>
/// Tests for ConfigRedactor.
/// Verifies sensitive field redaction per NFR-002b-06 through NFR-002b-10.
/// </summary>
public sealed class ConfigRedactorTests
{
    [Fact]
    public void Redact_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Arrange
        var redactor = new ConfigRedactor();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => redactor.Redact(null!));
    }

    [Fact]
    public void Redact_WithDsnInPostgresConfig_ShouldRedactDsn()
    {
        // Arrange - NFR-002b-07: dsn is a sensitive field
        var redactor = new ConfigRedactor();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Storage = new StorageConfig
            {
                Remote = new StorageRemoteConfig
                {
                    Postgres = new StoragePostgresConfig
                    {
                        Dsn = "postgresql://user:password@localhost:5432/db"
                    }
                }
            }
        };

        // Act
        var redacted = redactor.Redact(config);

        // Assert - NFR-002b-08: Format MUST be "[REDACTED:dsn]"
        redacted.Storage.Should().NotBeNull();
        redacted.Storage!.Remote.Should().NotBeNull();
        redacted.Storage.Remote!.Postgres.Should().NotBeNull();
        redacted.Storage.Remote.Postgres!.Dsn.Should().Be("[REDACTED:dsn]");
    }

    [Fact]
    public void Redact_WithNullDsn_ShouldNotModifyConfig()
    {
        // Arrange
        var redactor = new ConfigRedactor();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Storage = new StorageConfig
            {
                Remote = new StorageRemoteConfig
                {
                    Postgres = new StoragePostgresConfig
                    {
                        Dsn = null
                    }
                }
            }
        };

        // Act
        var redacted = redactor.Redact(config);

        // Assert
        redacted.Storage.Should().NotBeNull();
        redacted.Storage!.Remote.Should().NotBeNull();
        redacted.Storage.Remote!.Postgres.Should().NotBeNull();
        redacted.Storage.Remote.Postgres!.Dsn.Should().BeNull();
    }

    [Fact]
    public void Redact_WithNoSensitiveFields_ShouldReturnUnchangedConfig()
    {
        // Arrange
        var redactor = new ConfigRedactor();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Project = new ProjectConfig
            {
                Name = "test-project",
                Type = "dotnet"
            },
            Mode = new ModeConfig
            {
                Default = "local-only"
            }
        };

        // Act
        var redacted = redactor.Redact(config);

        // Assert
        redacted.SchemaVersion.Should().Be("1.0.0");
        redacted.Project.Should().NotBeNull();
        redacted.Project!.Name.Should().Be("test-project");
        redacted.Project.Type.Should().Be("dotnet");
        redacted.Mode.Should().NotBeNull();
        redacted.Mode!.Default.Should().Be("local-only");
    }

    [Fact]
    public void Redact_ShouldNotMutateOriginalConfig()
    {
        // Arrange
        var redactor = new ConfigRedactor();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Storage = new StorageConfig
            {
                Remote = new StorageRemoteConfig
                {
                    Postgres = new StoragePostgresConfig
                    {
                        Dsn = "postgresql://localhost/db"
                    }
                }
            }
        };

        var originalDsn = config.Storage.Remote.Postgres.Dsn;

        // Act
        var redacted = redactor.Redact(config);

        // Assert - Original should be unchanged
        config.Storage.Remote.Postgres.Dsn.Should().Be(originalDsn);
        redacted.Storage!.Remote!.Postgres!.Dsn.Should().Be("[REDACTED:dsn]");
    }

    [Fact]
    public void Redact_WithEmptyDsn_ShouldNotRedact()
    {
        // Arrange
        var redactor = new ConfigRedactor();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Storage = new StorageConfig
            {
                Remote = new StorageRemoteConfig
                {
                    Postgres = new StoragePostgresConfig
                    {
                        Dsn = string.Empty
                    }
                }
            }
        };

        // Act
        var redacted = redactor.Redact(config);

        // Assert - Empty strings are not redacted (no sensitive data)
        redacted.Storage!.Remote!.Postgres!.Dsn.Should().Be(string.Empty);
    }

    [Fact]
    public void Redact_WithNullStorage_ShouldHandleGracefully()
    {
        // Arrange
        var redactor = new ConfigRedactor();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Storage = null
        };

        // Act
        var redacted = redactor.Redact(config);

        // Assert
        redacted.Storage.Should().BeNull();
    }

    [Fact]
    public void Redact_WithNullRemote_ShouldHandleGracefully()
    {
        // Arrange
        var redactor = new ConfigRedactor();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Storage = new StorageConfig
            {
                Remote = null
            }
        };

        // Act
        var redacted = redactor.Redact(config);

        // Assert
        redacted.Storage.Should().NotBeNull();
        redacted.Storage!.Remote.Should().BeNull();
    }

    [Fact]
    public void Redact_WithNullPostgres_ShouldHandleGracefully()
    {
        // Arrange
        var redactor = new ConfigRedactor();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Storage = new StorageConfig
            {
                Remote = new StorageRemoteConfig
                {
                    Postgres = null
                }
            }
        };

        // Act
        var redacted = redactor.Redact(config);

        // Assert
        redacted.Storage!.Remote.Should().NotBeNull();
        redacted.Storage!.Remote!.Postgres.Should().BeNull();
    }

    [Fact]
    public void Redact_WithComplexConfig_ShouldOnlyRedactDsn()
    {
        // Arrange
        var redactor = new ConfigRedactor();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Project = new ProjectConfig
            {
                Name = "my-project",
                Type = "dotnet",
                Description = "A test project with storage"
            },
            Mode = new ModeConfig
            {
                Default = "local-only",
                AllowBurst = false
            },
            Model = new ModelConfig
            {
                Provider = "ollama",
                Name = "codellama:7b",
                Parameters = new ModelParametersConfig
                {
                    Temperature = 0.7,
                    MaxTokens = 4096
                }
            },
            Storage = new StorageConfig
            {
                Local = new StorageLocalConfig
                {
                    Type = "sqlite",
                    SqlitePath = ".acode/workspace.db"
                },
                Remote = new StorageRemoteConfig
                {
                    Type = "postgres",
                    Postgres = new StoragePostgresConfig
                    {
                        Dsn = "postgresql://user:pass@host:5432/dbname"
                    }
                }
            }
        };

        // Act
        var redacted = redactor.Redact(config);

        // Assert - All other fields unchanged, only DSN redacted
        redacted.SchemaVersion.Should().Be("1.0.0");
        redacted.Project!.Name.Should().Be("my-project");
        redacted.Project.Type.Should().Be("dotnet");
        redacted.Project.Description.Should().Be("A test project with storage");
        redacted.Mode!.Default.Should().Be("local-only");
        redacted.Mode.AllowBurst.Should().BeFalse();
        redacted.Model!.Provider.Should().Be("ollama");
        redacted.Model.Name.Should().Be("codellama:7b");
        redacted.Model.Parameters.Temperature.Should().Be(0.7);
        redacted.Model.Parameters.MaxTokens.Should().Be(4096);
        redacted.Storage!.Local!.Type.Should().Be("sqlite");
        redacted.Storage.Local.SqlitePath.Should().Be(".acode/workspace.db");
        redacted.Storage.Remote!.Type.Should().Be("postgres");
        redacted.Storage.Remote.Postgres!.Dsn.Should().Be("[REDACTED:dsn]");
    }
}
