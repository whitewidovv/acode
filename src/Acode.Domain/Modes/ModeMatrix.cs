using System.Collections.Frozen;

namespace Acode.Domain.Modes;

/// <summary>
/// Static mode matrix defining permissions for all mode-capability combinations.
/// This is the authoritative source for operating mode enforcement.
/// </summary>
/// <remarks>
/// Per Task 001.a, this matrix implements:
/// - HC-01: No external LLM APIs in LocalOnly/Airgapped
/// - HC-02: No network in Airgapped
/// - HC-03: Consent required for Burst external APIs
/// Uses FrozenDictionary for O(1) lookups with minimal memory overhead.
/// </remarks>
public static class ModeMatrix
{
    private static readonly FrozenDictionary<(OperatingMode, Capability), MatrixEntry> _matrix;

    static ModeMatrix()
    {
        var entries = BuildMatrix();
        _matrix = entries.ToFrozenDictionary(e => (e.Mode, e.Capability));
    }

    /// <summary>
    /// Get permission level for a mode-capability combination.
    /// </summary>
    /// <param name="mode">Operating mode.</param>
    /// <param name="capability">Capability to check.</param>
    /// <returns>Permission level.</returns>
    public static Permission GetPermission(OperatingMode mode, Capability capability)
    {
        return _matrix[(mode, capability)].Permission;
    }

    /// <summary>
    /// Get full matrix entry with rationale.
    /// </summary>
    /// <param name="mode">Operating mode.</param>
    /// <param name="capability">Capability to check.</param>
    /// <returns>Complete matrix entry.</returns>
    public static MatrixEntry GetEntry(OperatingMode mode, Capability capability)
    {
        return _matrix[(mode, capability)];
    }

    /// <summary>
    /// Get all entries for a specific mode.
    /// </summary>
    /// <param name="mode">Operating mode.</param>
    /// <returns>All entries for the mode.</returns>
    public static IReadOnlyList<MatrixEntry> GetEntriesForMode(OperatingMode mode)
    {
        return _matrix.Values
            .Where(e => e.Mode == mode)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Get all matrix entries.
    /// </summary>
    /// <returns>All mode-capability entries.</returns>
    public static IReadOnlyList<MatrixEntry> GetAllEntries()
    {
        return _matrix.Values.ToList().AsReadOnly();
    }

    private static List<MatrixEntry> BuildMatrix()
    {
        var entries = new List<MatrixEntry>();

        // ===== LocalOnly Mode =====

        // Network - localhost only for Ollama
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.LocalhostNetwork,
            Permission.Allowed,
            "Localhost access required for local Ollama instance"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.LocalAreaNetwork,
            Permission.Denied,
            "LAN access denied in LocalOnly mode per privacy-first design"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.ExternalNetwork,
            Permission.Denied,
            "External network denied in LocalOnly mode per HC-01"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.DnsLookup,
            Permission.Allowed,
            "DNS lookup allowed for localhost resolution"));

        // LLM Providers - local only
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.OllamaLocal,
            Permission.Allowed,
            "Local Ollama is the primary inference provider in LocalOnly mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.OpenAiApi,
            Permission.Denied,
            "HC-01: External LLM APIs denied in LocalOnly mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.AnthropicApi,
            Permission.Denied,
            "HC-01: External LLM APIs denied in LocalOnly mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.AzureOpenAiApi,
            Permission.Denied,
            "HC-01: External LLM APIs denied in LocalOnly mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.CustomLlmApi,
            Permission.Denied,
            "HC-01: External LLM APIs denied in LocalOnly mode"));

        // File System
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.ReadProjectFiles,
            Permission.Allowed,
            "Reading project files is core functionality"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.WriteProjectFiles,
            Permission.Allowed,
            "Writing project files is core functionality"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.ReadSystemFiles,
            Permission.LimitedScope,
            "System file reads allowed only for specific allowlisted paths"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.WriteSystemFiles,
            Permission.Denied,
            "System file writes denied for safety"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.ReadHomeDirectory,
            Permission.LimitedScope,
            "Home directory reads limited to ~/.acode and config files"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.WriteAcodeDirectory,
            Permission.Allowed,
            "Writing to ~/.acode required for configuration and cache"));

        // Tools
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.DotnetCli,
            Permission.Allowed,
            "Dotnet CLI execution is core functionality"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.GitOperations,
            Permission.Allowed,
            "Git operations are core functionality"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.NpmYarn,
            Permission.ConditionalOnConfig,
            "NPM/Yarn may attempt network access, requires explicit consent"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.CustomTools,
            Permission.ConditionalOnConfig,
            "Custom tools require repo contract allowlist"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.ShellCommands,
            Permission.LimitedScope,
            "Shell commands sandboxed to project directory"));

        // Data Transmission - all denied
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.SendPrompts,
            Permission.Denied,
            "HC-01: No external data transmission in LocalOnly mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.SendCodeSnippets,
            Permission.Denied,
            "HC-01: No external data transmission in LocalOnly mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.SendFullFiles,
            Permission.Denied,
            "HC-01: No external data transmission in LocalOnly mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.SendRepositoryData,
            Permission.Denied,
            "HC-01: No external data transmission in LocalOnly mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.SendTelemetry,
            Permission.Denied,
            "Privacy-first: no telemetry in LocalOnly mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.SendCrashReports,
            Permission.Denied,
            "Privacy-first: no crash reports in LocalOnly mode"));

        // ===== Burst Mode =====

        // Network - allowed for cloud compute
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.LocalhostNetwork,
            Permission.Allowed,
            "Localhost access for local Ollama if available"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.LocalAreaNetwork,
            Permission.Allowed,
            "LAN access allowed in Burst mode for local infrastructure"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.ExternalNetwork,
            Permission.Allowed,
            "External network required for cloud compute in Burst mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.DnsLookup,
            Permission.Allowed,
            "DNS required for cloud service discovery"));

        // LLM Providers - external APIs require consent
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.OllamaLocal,
            Permission.Allowed,
            "Local Ollama available as fallback in Burst mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.OpenAiApi,
            Permission.ConditionalOnConsent,
            "HC-03: External LLM APIs require explicit user consent in Burst"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.AnthropicApi,
            Permission.ConditionalOnConsent,
            "HC-03: External LLM APIs require explicit user consent in Burst"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.AzureOpenAiApi,
            Permission.ConditionalOnConsent,
            "HC-03: Azure OpenAI requires consent in Burst"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.CustomLlmApi,
            Permission.ConditionalOnConfig,
            "Custom APIs require config allowlist and consent"));

        // File System - same as LocalOnly
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.ReadProjectFiles,
            Permission.Allowed,
            "Reading project files is core functionality"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.WriteProjectFiles,
            Permission.Allowed,
            "Writing project files is core functionality"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.ReadSystemFiles,
            Permission.LimitedScope,
            "System file reads allowed only for specific allowlisted paths"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.WriteSystemFiles,
            Permission.Denied,
            "System file writes denied for safety"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.ReadHomeDirectory,
            Permission.LimitedScope,
            "Home directory reads limited to ~/.acode and config files"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.WriteAcodeDirectory,
            Permission.Allowed,
            "Writing to ~/.acode required for configuration and cache"));

        // Tools - all allowed
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.DotnetCli,
            Permission.Allowed,
            "Dotnet CLI execution is core functionality"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.GitOperations,
            Permission.Allowed,
            "Git operations are core functionality"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.NpmYarn,
            Permission.Allowed,
            "NPM/Yarn allowed with network access in Burst"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.CustomTools,
            Permission.ConditionalOnConfig,
            "Custom tools require repo contract allowlist"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.ShellCommands,
            Permission.LimitedScope,
            "Shell commands sandboxed to project directory"));

        // Data Transmission - conditional on consent
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.SendPrompts,
            Permission.ConditionalOnConsent,
            "HC-03: Sending prompts requires consent even in Burst"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.SendCodeSnippets,
            Permission.ConditionalOnConsent,
            "HC-03: Code snippets require consent and redaction"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.SendFullFiles,
            Permission.Denied,
            "Privacy: full file transmission denied even in Burst"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.SendRepositoryData,
            Permission.Denied,
            "Privacy: bulk repository data transmission always denied"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.SendTelemetry,
            Permission.ConditionalOnConfig,
            "Telemetry requires explicit opt-in in config"));
        entries.Add(new MatrixEntry(
            OperatingMode.Burst,
            Capability.SendCrashReports,
            Permission.ConditionalOnConfig,
            "Crash reports require explicit opt-in in config"));

        // ===== Airgapped Mode =====

        // Network - all denied
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.LocalhostNetwork,
            Permission.Denied,
            "HC-02: Complete network isolation in Airgapped mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.LocalAreaNetwork,
            Permission.Denied,
            "HC-02: Complete network isolation in Airgapped mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.ExternalNetwork,
            Permission.Denied,
            "HC-02: Complete network isolation in Airgapped mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.DnsLookup,
            Permission.Denied,
            "HC-02: No DNS in Airgapped mode"));

        // LLM Providers - all denied (no network)
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.OllamaLocal,
            Permission.Denied,
            "HC-02: No localhost network means no Ollama access"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.OpenAiApi,
            Permission.Denied,
            "HC-02: No network means no external APIs"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.AnthropicApi,
            Permission.Denied,
            "HC-02: No network means no external APIs"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.AzureOpenAiApi,
            Permission.Denied,
            "HC-02: No network means no external APIs"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.CustomLlmApi,
            Permission.Denied,
            "HC-02: No network means no external APIs"));

        // File System - same as LocalOnly
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.ReadProjectFiles,
            Permission.Allowed,
            "Reading project files is core functionality"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.WriteProjectFiles,
            Permission.Allowed,
            "Writing project files is core functionality"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.ReadSystemFiles,
            Permission.LimitedScope,
            "System file reads allowed only for specific allowlisted paths"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.WriteSystemFiles,
            Permission.Denied,
            "System file writes denied for safety"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.ReadHomeDirectory,
            Permission.LimitedScope,
            "Home directory reads limited to ~/.acode and config files"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.WriteAcodeDirectory,
            Permission.Allowed,
            "Writing to ~/.acode required for configuration and cache"));

        // Tools - local only
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.DotnetCli,
            Permission.Allowed,
            "Dotnet CLI execution is core functionality"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.GitOperations,
            Permission.Allowed,
            "Git operations are core functionality (local only)"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.NpmYarn,
            Permission.Denied,
            "NPM/Yarn denied in Airgapped due to network requirements"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.CustomTools,
            Permission.ConditionalOnConfig,
            "Custom tools allowed if they don't require network"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.ShellCommands,
            Permission.LimitedScope,
            "Shell commands sandboxed to project directory"));

        // Data Transmission - all denied
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.SendPrompts,
            Permission.Denied,
            "HC-02: No data transmission in Airgapped mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.SendCodeSnippets,
            Permission.Denied,
            "HC-02: No data transmission in Airgapped mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.SendFullFiles,
            Permission.Denied,
            "HC-02: No data transmission in Airgapped mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.SendRepositoryData,
            Permission.Denied,
            "HC-02: No data transmission in Airgapped mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.SendTelemetry,
            Permission.Denied,
            "HC-02: No data transmission in Airgapped mode"));
        entries.Add(new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.SendCrashReports,
            Permission.Denied,
            "HC-02: No data transmission in Airgapped mode"));

        return entries;
    }
}
