namespace Acode.Domain.Security;

/// <summary>
/// Represents a specific attack vector in the threat model.
/// An attack vector describes how a threat actor might exploit a weakness
/// when crossing a trust boundary.
/// </summary>
public sealed record AttackVector
{
    /// <summary>
    /// Gets the unique identifier for this attack vector.
    /// Format: VEC-NNN (e.g., VEC-001, VEC-042).
    /// </summary>
    public required string VectorId { get; init; }

    /// <summary>
    /// Gets the human-readable description of the attack vector.
    /// Should explain the attack method, potential impact, and context.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the threat actor capable of exploiting this vector.
    /// </summary>
    public required ThreatActor ThreatActor { get; init; }

    /// <summary>
    /// Gets the trust boundary crossed by this attack.
    /// </summary>
    public required TrustBoundary Boundary { get; init; }
}
