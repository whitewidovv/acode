namespace Acode.Application.Truncation;

/// <summary>
/// Defines the available truncation strategies.
/// </summary>
public enum TruncationStrategy
{
    /// <summary>
    /// No truncation - pass through unchanged.
    /// </summary>
    None = 0,

    /// <summary>
    /// Keep head only - first N characters/lines.
    /// Best for documentation files where key info is at the top.
    /// </summary>
    Head,

    /// <summary>
    /// Keep tail only - last N characters/lines.
    /// Best for logs and command output where recent info matters.
    /// </summary>
    Tail,

    /// <summary>
    /// Keep head and tail, omit middle.
    /// Best for code files showing imports and main functions.
    /// </summary>
    HeadTail,

    /// <summary>
    /// Element-based truncation for structured data (JSON arrays).
    /// Preserves first/last elements with valid structure.
    /// </summary>
    Element
}
