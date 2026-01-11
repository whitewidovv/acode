namespace Acode.Domain.Roles;

/// <summary>
/// Defines context assembly strategies for different roles.
/// Context strategy determines what files and information are included in the model's context window.
/// </summary>
/// <remarks>
/// <para>Each role uses a different context strategy optimized for its purpose:</para>
/// <list type="bullet">
/// <item>Adaptive: Adjusts based on request (Default role)</item>
/// <item>Broad: Project-wide information (Planner role)</item>
/// <item>Focused: Specific files only (Coder role)</item>
/// <item>ChangeFocused: Diffs and affected code (Reviewer role)</item>
/// </list>
/// </remarks>
public enum ContextStrategy
{
    /// <summary>
    /// Adaptive strategy that adjusts based on the request.
    /// Used by Default role for general-purpose tasks.
    /// </summary>
    Adaptive = 0,

    /// <summary>
    /// Broad context including project-wide information.
    /// Used by Planner role: project structure, architectural patterns, existing implementations.
    /// Typical size: 12-18K tokens.
    /// </summary>
    Broad = 1,

    /// <summary>
    /// Focused context limited to specific files and related interfaces.
    /// Used by Coder role: files being modified, function signatures, test files.
    /// Typical size: 4-8K tokens.
    /// </summary>
    Focused = 2,

    /// <summary>
    /// Change-focused context centered on diffs and affected code.
    /// Used by Reviewer role: diffs, affected files, related tests, original requirements.
    /// Typical size: 6-10K tokens.
    /// </summary>
    ChangeFocused = 3,
}
