namespace Acode.Infrastructure.Heuristics;

/// <summary>
/// The source of a model override.
/// </summary>
public enum OverrideSource
{
    /// <summary>Override from --model CLI flag.</summary>
    Request,

    /// <summary>Override from session (env var or CLI setting).</summary>
    Session,

    /// <summary>Override from configuration file.</summary>
    Config,
}
