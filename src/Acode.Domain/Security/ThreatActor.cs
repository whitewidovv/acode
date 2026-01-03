namespace Acode.Domain.Security;

/// <summary>
/// Represents the source or actor of a potential security threat.
/// Used in threat modeling per STRIDE methodology.
/// </summary>
public enum ThreatActor
{
    /// <summary>
    /// The human user running Acode.
    /// May provide malicious inputs or misuse the tool.
    /// </summary>
    User,

    /// <summary>
    /// The Acode agent itself.
    /// May have bugs or be exploited to perform unintended actions.
    /// </summary>
    Agent,

    /// <summary>
    /// External LLM API (OpenAI, Anthropic, etc.).
    /// May exfiltrate data or inject malicious code.
    /// </summary>
    ExternalLlm,

    /// <summary>
    /// Local model running via Ollama/vLLM.
    /// May be compromised or generate malicious output.
    /// </summary>
    LocalModel,

    /// <summary>
    /// File system access.
    /// May expose sensitive files or allow unauthorized modification.
    /// </summary>
    FileSystem,

    /// <summary>
    /// External process execution.
    /// May execute malicious commands or leak data.
    /// </summary>
    Process,

    /// <summary>
    /// Network access.
    /// May exfiltrate data or download malicious payloads.
    /// </summary>
    Network,

    /// <summary>
    /// Malicious or crafted input from user, files, or external sources.
    /// May exploit parsing, injection vulnerabilities.
    /// </summary>
    MaliciousInput,

    /// <summary>
    /// Compromised third-party dependency (NuGet package, npm module).
    /// May contain backdoors or vulnerabilities.
    /// </summary>
    CompromisedDependency,

    /// <summary>
    /// Insider threat - authorized user with malicious intent.
    /// May abuse legitimate access to exfiltrate or sabotage.
    /// </summary>
    Insider
}
