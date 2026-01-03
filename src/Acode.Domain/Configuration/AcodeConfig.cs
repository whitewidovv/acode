#pragma warning disable SA1402 // File may only contain a single type - Configuration DTOs grouped for cohesion

namespace Acode.Domain.Configuration;

/// <summary>
/// Root configuration model for .agent/config.yml.
/// Immutable record representing the complete Acode configuration.
/// </summary>
/// <remarks>
/// Per Task 002.b FR-002b-71 through FR-002b-90.
/// This is the domain model - no parsing or validation logic here.
/// </remarks>
public sealed record AcodeConfig
{
    /// <summary>
    /// Gets the schema version (semver format).
    /// Required field per schema.
    /// </summary>
    public required string SchemaVersion { get; init; }

    /// <summary>
    /// Gets the project metadata.
    /// Optional - defaults applied if missing.
    /// </summary>
    public ProjectConfig? Project { get; init; }

    /// <summary>
    /// Gets the operating mode configuration.
    /// Optional - defaults applied if missing.
    /// </summary>
    public ModeConfig? Mode { get; init; }

    /// <summary>
    /// Gets the LLM model configuration.
    /// Optional - defaults applied if missing.
    /// </summary>
    public ModelConfig? Model { get; init; }

    /// <summary>
    /// Gets the command groups configuration.
    /// Optional - no commands if missing.
    /// </summary>
    public CommandsConfig? Commands { get; init; }

    /// <summary>
    /// Gets the directory path configurations.
    /// Optional - defaults applied if missing.
    /// </summary>
    public PathsConfig? Paths { get; init; }

    /// <summary>
    /// Gets the ignore patterns configuration.
    /// Optional - defaults applied if missing.
    /// </summary>
    public IgnoreConfig? Ignore { get; init; }

    /// <summary>
    /// Gets the network allowlist configuration (Burst mode only).
    /// Optional - null means no allowlist.
    /// </summary>
    public NetworkConfig? Network { get; init; }

    /// <summary>
    /// Gets the storage and sync configuration.
    /// Optional - defaults to local_cache_only.
    /// </summary>
    public StorageConfig? Storage { get; init; }
}

/// <summary>
/// Project metadata configuration.
/// </summary>
public sealed record ProjectConfig
{
    /// <summary>
    /// Gets the project name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the project type.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Gets the programming languages used.
    /// </summary>
    public IReadOnlyList<string>? Languages { get; init; }

    /// <summary>
    /// Gets the project description.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Operating mode configuration.
/// </summary>
public sealed record ModeConfig
{
    /// <summary>
    /// Gets the default operating mode.
    /// </summary>
    public string Default { get; init; } = ConfigDefaults.DefaultMode;

    /// <summary>
    /// Gets a value indicating whether burst mode is allowed.
    /// </summary>
    public bool AllowBurst { get; init; } = ConfigDefaults.AllowBurst;

    /// <summary>
    /// Gets a value indicating whether airgapped mode is locked.
    /// </summary>
    public bool AirgappedLock { get; init; } = ConfigDefaults.AirgappedLock;
}

/// <summary>
/// LLM model configuration.
/// </summary>
public sealed record ModelConfig
{
    /// <summary>
    /// Gets the LLM provider.
    /// </summary>
    public string Provider { get; init; } = ConfigDefaults.DefaultProvider;

    /// <summary>
    /// Gets the model name.
    /// </summary>
    public string Name { get; init; } = ConfigDefaults.DefaultModel;

    /// <summary>
    /// Gets the model endpoint URL.
    /// </summary>
    public string Endpoint { get; init; } = ConfigDefaults.DefaultEndpoint;

    /// <summary>
    /// Gets the model parameters.
    /// </summary>
    public ModelParametersConfig Parameters { get; init; } = new();

    /// <summary>
    /// Gets the request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = ConfigDefaults.DefaultTimeoutSeconds;

    /// <summary>
    /// Gets the retry count.
    /// </summary>
    public int RetryCount { get; init; } = ConfigDefaults.DefaultRetryCount;
}

/// <summary>
/// Model inference parameters.
/// </summary>
public sealed record ModelParametersConfig
{
    /// <summary>
    /// Gets the temperature parameter.
    /// </summary>
    public double Temperature { get; init; } = ConfigDefaults.DefaultTemperature;

    /// <summary>
    /// Gets the maximum tokens.
    /// </summary>
    public int MaxTokens { get; init; } = ConfigDefaults.DefaultMaxTokens;

    /// <summary>
    /// Gets the top-p parameter.
    /// </summary>
    public double TopP { get; init; } = 0.95;
}

/// <summary>
/// Command groups configuration.
/// </summary>
public sealed record CommandsConfig
{
    /// <summary>
    /// Gets the setup command.
    /// </summary>
    public object? Setup { get; init; }

    /// <summary>
    /// Gets the build command.
    /// </summary>
    public object? Build { get; init; }

    /// <summary>
    /// Gets the test command.
    /// </summary>
    public object? Test { get; init; }

    /// <summary>
    /// Gets the lint command.
    /// </summary>
    public object? Lint { get; init; }

    /// <summary>
    /// Gets the format command.
    /// </summary>
    public object? Format { get; init; }

    /// <summary>
    /// Gets the start command.
    /// </summary>
    public object? Start { get; init; }
}

/// <summary>
/// Directory paths configuration.
/// </summary>
public sealed record PathsConfig
{
    /// <summary>
    /// Gets the source code paths.
    /// </summary>
    public IReadOnlyList<string>? Source { get; init; }

    /// <summary>
    /// Gets the test paths.
    /// </summary>
    public IReadOnlyList<string>? Tests { get; init; }

    /// <summary>
    /// Gets the output paths.
    /// </summary>
    public IReadOnlyList<string>? Output { get; init; }

    /// <summary>
    /// Gets the documentation paths.
    /// </summary>
    public IReadOnlyList<string>? Docs { get; init; }
}

/// <summary>
/// Ignore patterns configuration.
/// </summary>
public sealed record IgnoreConfig
{
    /// <summary>
    /// Gets the ignore patterns.
    /// </summary>
    public IReadOnlyList<string>? Patterns { get; init; }

    /// <summary>
    /// Gets the additional ignore patterns.
    /// </summary>
    public IReadOnlyList<string>? Additional { get; init; }
}

/// <summary>
/// Network allowlist configuration (Burst mode only).
/// </summary>
public sealed record NetworkConfig
{
    /// <summary>
    /// Gets the network allowlist entries.
    /// </summary>
    public IReadOnlyList<NetworkAllowlistEntry>? Allowlist { get; init; }
}

/// <summary>
/// Single allowlist entry for network access.
/// </summary>
public sealed record NetworkAllowlistEntry
{
    /// <summary>
    /// Gets the host name or IP address.
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// Gets the allowed ports.
    /// </summary>
    public IReadOnlyList<int>? Ports { get; init; }

    /// <summary>
    /// Gets the reason for allowing this entry.
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Storage and sync configuration.
/// </summary>
public sealed record StorageConfig
{
    /// <summary>
    /// Gets the storage mode.
    /// </summary>
    public string Mode { get; init; } = "local_cache_only";

    /// <summary>
    /// Gets the local storage configuration.
    /// </summary>
    public StorageLocalConfig? Local { get; init; }

    /// <summary>
    /// Gets the remote storage configuration.
    /// </summary>
    public StorageRemoteConfig? Remote { get; init; }

    /// <summary>
    /// Gets the sync configuration.
    /// </summary>
    public StorageSyncConfig? Sync { get; init; }
}

/// <summary>
/// Local storage configuration.
/// </summary>
public sealed record StorageLocalConfig
{
    /// <summary>
    /// Gets the local storage type.
    /// </summary>
    public string Type { get; init; } = "sqlite";

    /// <summary>
    /// Gets the SQLite database path.
    /// </summary>
    public string SqlitePath { get; init; } = ".acode/workspace.db";
}

/// <summary>
/// Remote storage configuration.
/// </summary>
public sealed record StorageRemoteConfig
{
    /// <summary>
    /// Gets the remote storage type.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Gets the PostgreSQL configuration.
    /// </summary>
    public StoragePostgresConfig? Postgres { get; init; }
}

/// <summary>
/// PostgreSQL storage configuration.
/// </summary>
public sealed record StoragePostgresConfig
{
    /// <summary>
    /// Gets the PostgreSQL Data Source Name (DSN).
    /// </summary>
    public string? Dsn { get; init; }
}

/// <summary>
/// Storage sync configuration.
/// </summary>
public sealed record StorageSyncConfig
{
    /// <summary>
    /// Gets a value indicating whether sync is enabled.
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// Gets the batch size for sync operations.
    /// </summary>
    public int BatchSize { get; init; } = 100;

    /// <summary>
    /// Gets the retry policy for sync operations.
    /// </summary>
    public StorageSyncRetryPolicy? RetryPolicy { get; init; }

    /// <summary>
    /// Gets the conflict resolution policy.
    /// </summary>
    public string ConflictPolicy { get; init; } = "lww";
}

/// <summary>
/// Sync retry policy configuration.
/// </summary>
public sealed record StorageSyncRetryPolicy
{
    /// <summary>
    /// Gets the maximum retry attempts.
    /// </summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>
    /// Gets the backoff time in milliseconds.
    /// </summary>
    public int BackoffMs { get; init; } = 1000;
}
