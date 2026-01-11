namespace Acode.Application.Heuristics;

/// <summary>
/// Complexity tier mapped to model selection tiers.
/// </summary>
/// <remarks>
/// <para>AC-016: 0-30 = low.</para>
/// <para>AC-017: 31-70 = medium.</para>
/// <para>AC-018: 71-100 = high.</para>
/// <para>AC-019: Maps to tiers.</para>
/// </remarks>
public enum ComplexityTier
{
    /// <summary>
    /// Low complexity - Route to fast/small models.
    /// Score range: 0-30 (configurable).
    /// </summary>
    Low,

    /// <summary>
    /// Medium complexity - Route to default models.
    /// Score range: 31-70 (configurable).
    /// </summary>
    Medium,

    /// <summary>
    /// High complexity - Route to capable/large models.
    /// Score range: 71-100 (configurable).
    /// </summary>
    High,
}
