namespace Acode.Domain.Modes;

/// <summary>
/// All capabilities that can be checked against operating mode.
/// Each capability maps to a specific action the system can perform.
/// </summary>
/// <remarks>
/// These capabilities are used by the ModeMatrix to determine what actions
/// are permitted in each operating mode. Per Task 001.a mode matrix specification.
/// </remarks>
public enum Capability
{
    // Network capabilities

    /// <summary>
    /// Access to localhost (127.0.0.1, ::1) network.
    /// Required for local Ollama communication.
    /// </summary>
    LocalhostNetwork,

    /// <summary>
    /// Access to local area network (LAN).
    /// </summary>
    LocalAreaNetwork,

    /// <summary>
    /// Access to external network (internet/WAN).
    /// </summary>
    ExternalNetwork,

    /// <summary>
    /// Ability to perform DNS lookups.
    /// </summary>
    DnsLookup,

    // LLM Provider capabilities

    /// <summary>
    /// Access to local Ollama instance.
    /// Allowed in LocalOnly and Burst modes. Denied in Airgapped (no network).
    /// </summary>
    OllamaLocal,

    /// <summary>
    /// Access to OpenAI API (api.openai.com).
    /// Denied in LocalOnly and Airgapped, conditional in Burst.
    /// </summary>
    OpenAiApi,

    /// <summary>
    /// Access to Anthropic API (api.anthropic.com).
    /// Denied in LocalOnly and Airgapped, conditional in Burst.
    /// </summary>
    AnthropicApi,

    /// <summary>
    /// Access to Azure OpenAI endpoints.
    /// Denied in LocalOnly and Airgapped, conditional in Burst.
    /// </summary>
    AzureOpenAiApi,

    /// <summary>
    /// Access to custom/other LLM API endpoints.
    /// Denied in LocalOnly and Airgapped, conditional in Burst.
    /// </summary>
    CustomLlmApi,

    // File system capabilities

    /// <summary>
    /// Read files within the project workspace.
    /// </summary>
    ReadProjectFiles,

    /// <summary>
    /// Write/modify files within the project workspace.
    /// </summary>
    WriteProjectFiles,

    /// <summary>
    /// Read system files outside the project workspace.
    /// Limited scope even when allowed.
    /// </summary>
    ReadSystemFiles,

    /// <summary>
    /// Write/modify system files outside the project workspace.
    /// Generally denied for safety.
    /// </summary>
    WriteSystemFiles,

    /// <summary>
    /// Read files in user home directory.
    /// </summary>
    ReadHomeDirectory,

    /// <summary>
    /// Write to ~/.acode directory.
    /// </summary>
    WriteAcodeDirectory,

    // Tool execution capabilities

    /// <summary>
    /// Execute dotnet CLI commands.
    /// </summary>
    DotnetCli,

    /// <summary>
    /// Execute git operations.
    /// </summary>
    GitOperations,

    /// <summary>
    /// Execute npm/yarn package managers.
    /// May require network for installs.
    /// </summary>
    NpmYarn,

    /// <summary>
    /// Execute custom tools/binaries.
    /// </summary>
    CustomTools,

    /// <summary>
    /// Execute arbitrary shell commands.
    /// Subject to sandboxing per mode.
    /// </summary>
    ShellCommands,

    // Data transmission capabilities

    /// <summary>
    /// Send prompts to external services.
    /// Only allowed in Burst mode with consent.
    /// </summary>
    SendPrompts,

    /// <summary>
    /// Send code snippets to external services.
    /// Only allowed in Burst mode with consent and redaction.
    /// </summary>
    SendCodeSnippets,

    /// <summary>
    /// Send full files to external services.
    /// Generally denied for privacy.
    /// </summary>
    SendFullFiles,

    /// <summary>
    /// Send repository data to external services.
    /// Denied to prevent bulk code exfiltration.
    /// </summary>
    SendRepositoryData,

    /// <summary>
    /// Send telemetry/analytics data.
    /// Denied by default for privacy.
    /// </summary>
    SendTelemetry,

    /// <summary>
    /// Send crash reports to external services.
    /// Optional in Burst mode only.
    /// </summary>
    SendCrashReports,
}
