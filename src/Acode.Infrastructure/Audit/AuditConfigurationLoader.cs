namespace Acode.Infrastructure.Audit;

using System;
using System.IO;
using Acode.Domain.Audit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// Loads audit configuration from YAML config files.
/// Parses .agent/config.yml and extracts audit settings.
/// </summary>
public sealed class AuditConfigurationLoader
{
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditConfigurationLoader"/> class.
    /// </summary>
    public AuditConfigurationLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
    }

    /// <summary>
    /// Loads audit configuration from the specified YAML file.
    /// </summary>
    /// <param name="configPath">Path to the YAML config file.</param>
    /// <returns>Parsed and validated AuditConfiguration.</returns>
    /// <exception cref="FileNotFoundException">Config file not found.</exception>
    /// <exception cref="InvalidOperationException">Invalid configuration values.</exception>
    public AuditConfiguration Load(string configPath)
    {
        ArgumentNullException.ThrowIfNull(configPath);

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Config file not found: {configPath}");
        }

        // Read YAML file
        string yamlContent;
        try
        {
            yamlContent = File.ReadAllText(configPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read config file: {configPath}", ex);
        }

        // Parse YAML
        ConfigRoot? root;
        try
        {
            root = _deserializer.Deserialize<ConfigRoot>(yamlContent);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse YAML configuration", ex);
        }

        // Extract audit section (may be null if not present)
        var auditSection = root?.Audit;

        // Build configuration with defaults
        return BuildConfiguration(auditSection);
    }

    private static AuditConfiguration BuildConfiguration(AuditConfigSection? section)
    {
        // Use defaults if section is null
        if (section == null)
        {
            return new AuditConfiguration();
        }

        // Parse log level
        var logLevel = ParseLogLevel(section.Level);

        // Parse rotation interval
        var rotationInterval = ParseRotationInterval(section.RotationInterval);

        // Validate numeric values
        ValidateConfiguration(section);

        // Convert MB to bytes for file size and total storage
        var maxFileSize = section.RotationSizeMb.HasValue
            ? section.RotationSizeMb.Value * 1024L * 1024L
            : 10 * 1024L * 1024L; // 10MB default

        var maxTotalStorage = section.MaxTotalStorageMb.HasValue
            ? section.MaxTotalStorageMb.Value * 1024L * 1024L
            : 1024L * 1024L * 1024L; // 1GB default

        return new AuditConfiguration
        {
            Enabled = section.Enabled ?? true,
            LogLevel = logLevel,
            LogDirectory = section.Directory ?? ".acode/logs",
            RetentionDays = section.RetentionDays ?? 90,
            RotationSizeMb = section.RotationSizeMb ?? 10,
            RotationInterval = rotationInterval,
            MaxFileSize = maxFileSize,
            MaxTotalStorage = maxTotalStorage,
        };
    }

    private static AuditSeverity ParseLogLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return AuditSeverity.Info; // Default
        }

        return level.ToLowerInvariant() switch
        {
            "debug" => AuditSeverity.Debug,
            "info" => AuditSeverity.Info,
            "warning" => AuditSeverity.Warning,
            "error" => AuditSeverity.Error,
            _ => throw new InvalidOperationException(
                $"Invalid audit level: {level}. Valid values are: debug, info, warning, error"),
        };
    }

    private static RotationInterval ParseRotationInterval(string? interval)
    {
        if (string.IsNullOrWhiteSpace(interval))
        {
            return RotationInterval.Daily; // Default
        }

        return interval.ToLowerInvariant() switch
        {
            "hourly" => RotationInterval.Hourly,
            "daily" => RotationInterval.Daily,
            "weekly" => RotationInterval.Weekly,
            _ => throw new InvalidOperationException(
                $"Invalid rotation_interval: {interval}. Valid values are: hourly, daily, weekly"),
        };
    }

    private static void ValidateConfiguration(AuditConfigSection section)
    {
        // Validate retention days
        if (section.RetentionDays.HasValue)
        {
            if (section.RetentionDays.Value < 0)
            {
                throw new InvalidOperationException(
                    $"retention_days cannot be negative: {section.RetentionDays.Value}");
            }

            if (section.RetentionDays.Value == 0)
            {
                throw new InvalidOperationException(
                    "retention_days must be positive (at least 1 day)");
            }
        }

        // Validate rotation size
        if (section.RotationSizeMb.HasValue && section.RotationSizeMb.Value <= 0)
        {
            throw new InvalidOperationException(
                $"rotation_size_mb must be positive: {section.RotationSizeMb.Value}");
        }

        // Validate max total storage
        if (section.MaxTotalStorageMb.HasValue && section.MaxTotalStorageMb.Value <= 0)
        {
            throw new InvalidOperationException(
                $"max_total_storage_mb must be positive: {section.MaxTotalStorageMb.Value}");
        }
    }

    /// <summary>
    /// Root configuration object for YAML deserialization.
    /// </summary>
    private sealed class ConfigRoot
    {
        public AuditConfigSection? Audit { get; set; }
    }

    /// <summary>
    /// Audit configuration section from YAML.
    /// All properties are nullable to support partial configs and defaults.
    /// </summary>
    private sealed class AuditConfigSection
    {
        public bool? Enabled { get; set; }

        public string? Level { get; set; }

        public string? Directory { get; set; }

        public int? RetentionDays { get; set; }

        public int? RotationSizeMb { get; set; }

        public string? RotationInterval { get; set; }

        public int? MaxTotalStorageMb { get; set; }
    }
}
