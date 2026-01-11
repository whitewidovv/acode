namespace Acode.Domain.Roles;

/// <summary>
/// Defines the complete specification for an agent role.
/// Immutable value object that describes role capabilities, constraints, and behavior.
/// </summary>
/// <remarks>
/// <para>AC-008 to AC-014: RoleDefinition properties defined.</para>
/// <para>Each role has:</para>
/// <list type="bullet">
/// <item>Capabilities: Operations the role is allowed to perform</item>
/// <item>Constraints: Explicit limitations on what the role cannot do</item>
/// <item>PromptKey: Reference to role-specific prompt in the prompt pack</item>
/// <item>ContextStrategy: Determines context window assembly approach</item>
/// </list>
/// </remarks>
public sealed class RoleDefinition
{
    /// <summary>
    /// Gets the role enum value this definition describes.
    /// </summary>
    /// <remarks>AC-009: Role property exists.</remarks>
    public required AgentRole Role { get; init; }

    /// <summary>
    /// Gets the human-readable display name for the role.
    /// </summary>
    /// <remarks>AC-010: Name property exists.</remarks>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the detailed description of the role's purpose and responsibilities.
    /// </summary>
    /// <remarks>AC-011: Description property exists.</remarks>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the list of capabilities (tools/operations) the role is allowed to use.
    /// </summary>
    /// <remarks>
    /// <para>AC-012: Capabilities property exists.</para>
    /// <para>Example: ["read_file", "write_file", "execute_command"].</para>
    /// </remarks>
    public required IReadOnlyList<string> Capabilities { get; init; }

    /// <summary>
    /// Gets the list of explicit constraints defining what the role cannot do.
    /// </summary>
    /// <remarks>
    /// <para>AC-013: Constraints property exists.</para>
    /// <para>Example: ["Cannot modify files", "Cannot execute commands"].</para>
    /// </remarks>
    public required IReadOnlyList<string> Constraints { get; init; }

    /// <summary>
    /// Gets the key identifying the role-specific prompt in the active prompt pack.
    /// </summary>
    /// <remarks>
    /// <para>AC-014: PromptKey property exists.</para>
    /// <para>Example: "roles/planner.md".</para>
    /// </remarks>
    public required string PromptKey { get; init; }

    /// <summary>
    /// Gets the context assembly strategy that determines what information is provided to the role.
    /// </summary>
    public required ContextStrategy ContextStrategy { get; init; }

    /// <summary>
    /// Validates that the role definition is complete and consistent.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Role name cannot be empty", nameof(Name));
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            throw new ArgumentException("Role description cannot be empty", nameof(Description));
        }

        if (Capabilities == null || Capabilities.Count == 0)
        {
            throw new ArgumentException(
                "Role must have at least one capability",
                nameof(Capabilities)
            );
        }

        if (Constraints == null)
        {
            throw new ArgumentException(
                "Constraints list cannot be null (use empty list if no constraints)",
                nameof(Constraints)
            );
        }

        if (string.IsNullOrWhiteSpace(PromptKey))
        {
            throw new ArgumentException("Prompt key cannot be empty", nameof(PromptKey));
        }
    }
}
