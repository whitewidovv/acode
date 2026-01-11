namespace Acode.Domain.Roles;

/// <summary>
/// Extension methods for <see cref="AgentRole"/> enum.
/// </summary>
/// <remarks>
/// AC-006: String representations work. AC-007: Unknown roles return Default.
/// </remarks>
public static class AgentRoleExtensions
{
    /// <summary>
    /// Converts the role to a human-readable display string.
    /// </summary>
    /// <param name="role">The role to convert.</param>
    /// <returns>Human-readable display name for the role.</returns>
    public static string ToDisplayString(this AgentRole role) =>
        role switch
        {
            AgentRole.Default => "Default",
            AgentRole.Planner => "Planner",
            AgentRole.Coder => "Coder",
            AgentRole.Reviewer => "Reviewer",
            _ => "Default", // Unknown roles fallback to Default (AC-007)
        };

    /// <summary>
    /// Parses a string to an <see cref="AgentRole"/> enum, case-insensitive.
    /// Returns Default if parsing fails.
    /// </summary>
    /// <param name="roleString">The string to parse.</param>
    /// <returns>The parsed role, or Default if parsing fails.</returns>
    public static AgentRole Parse(string roleString)
    {
        if (Enum.TryParse<AgentRole>(roleString, ignoreCase: true, out var role))
        {
            return role;
        }

        return AgentRole.Default;
    }
}
