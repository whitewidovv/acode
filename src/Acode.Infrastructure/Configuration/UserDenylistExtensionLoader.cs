using Acode.Domain.Security.PathProtection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Acode.Infrastructure.Configuration;

/// <summary>
/// Loads user-defined denylist extensions from .agent/config.yml.
/// Merges user extensions with default denylist while ensuring defaults cannot be removed.
/// </summary>
public sealed class UserDenylistExtensionLoader
{
    private readonly IDeserializer _yamlDeserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDenylistExtensionLoader"/> class.
    /// </summary>
    public UserDenylistExtensionLoader()
    {
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
    }

    /// <summary>
    /// Loads user-defined protected path extensions from a config file.
    /// </summary>
    /// <param name="configPath">Path to .agent/config.yml file.</param>
    /// <returns>List of user-defined denylist entries.</returns>
    public IReadOnlyList<DenylistEntry> LoadExtensions(string configPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);

        if (!File.Exists(configPath))
        {
            // No config file = no extensions
            return Array.Empty<DenylistEntry>();
        }

        try
        {
            var yamlContent = File.ReadAllText(configPath);
            var config = _yamlDeserializer.Deserialize<ConfigRoot>(yamlContent);

            if (config?.Security?.AdditionalProtectedPaths == null || config.Security.AdditionalProtectedPaths.Count == 0)
            {
                return Array.Empty<DenylistEntry>();
            }

            var userEntries = new List<DenylistEntry>();
            foreach (var pathConfig in config.Security.AdditionalProtectedPaths)
            {
                if (string.IsNullOrWhiteSpace(pathConfig.Pattern))
                {
                    continue; // Skip invalid entries
                }

                var entry = new DenylistEntry
                {
                    Pattern = pathConfig.Pattern,
                    Reason = pathConfig.Reason ?? "User-defined protected path",
                    RiskId = pathConfig.RiskId ?? "RISK-U-001",
                    Category = ParseCategory(pathConfig.Category),
                    Platforms = ParsePlatforms(pathConfig.Platforms),
                    IsDefault = false // User-defined entries are not defaults
                };

                userEntries.Add(entry);
            }

            return userEntries.AsReadOnly();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load user denylist extensions from {configPath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Merges user-defined entries with default denylist.
    /// Default entries cannot be removed - only additions are supported.
    /// </summary>
    /// <param name="userEntries">User-defined denylist entries.</param>
    /// <returns>Merged denylist containing defaults and user extensions.</returns>
    public IReadOnlyList<DenylistEntry> MergeWithDefaults(IReadOnlyList<DenylistEntry> userEntries)
    {
        ArgumentNullException.ThrowIfNull(userEntries);

        var merged = new List<DenylistEntry>(DefaultDenylist.Entries);
        merged.AddRange(userEntries);
        return merged.AsReadOnly();
    }

    private static PathCategory ParseCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return PathCategory.UserDefined;
        }

        return category.ToLowerInvariant() switch
        {
            "ssh_keys" or "ssh-keys" or "sshkeys" => PathCategory.SshKeys,
            "gpg_keys" or "gpg-keys" or "gpgkeys" => PathCategory.GpgKeys,
            "cloud_credentials" or "cloud-credentials" or "cloudcredentials" => PathCategory.CloudCredentials,
            "environment_files" or "environment-files" or "environmentfiles" => PathCategory.EnvironmentFiles,
            "system_files" or "system-files" or "systemfiles" => PathCategory.SystemFiles,
            "secret_files" or "secret-files" or "secretfiles" => PathCategory.SecretFiles,
            "package_manager_credentials" or "package-manager-credentials" => PathCategory.PackageManagerCredentials,
            "git_credentials" or "git-credentials" or "gitcredentials" => PathCategory.GitCredentials,
            _ => PathCategory.UserDefined
        };
    }

    private static Platform[] ParsePlatforms(List<string>? platforms)
    {
        if (platforms == null || platforms.Count == 0)
        {
            return new[] { Platform.All };
        }

        var result = new List<Platform>();
        foreach (var platform in platforms)
        {
            var parsed = platform.ToLowerInvariant() switch
            {
                "all" => Platform.All,
                "windows" => Platform.Windows,
                "linux" => Platform.Linux,
                "macos" or "mac" or "osx" => Platform.MacOS,
                _ => (Platform?)null
            };

            if (parsed.HasValue)
            {
                result.Add(parsed.Value);
            }
        }

        return result.Count > 0 ? result.ToArray() : new[] { Platform.All };
    }

    // Internal config DTOs for YAML deserialization
    private sealed class ConfigRoot
    {
        public SecurityConfig? Security { get; set; }
    }

    private sealed class SecurityConfig
    {
        public List<ProtectedPathConfig>? AdditionalProtectedPaths { get; set; }
    }

    private sealed class ProtectedPathConfig
    {
        public string? Pattern { get; set; }

        public string? Reason { get; set; }

        public string? RiskId { get; set; }

        public string? Category { get; set; }

        public List<string>? Platforms { get; set; }
    }
}
