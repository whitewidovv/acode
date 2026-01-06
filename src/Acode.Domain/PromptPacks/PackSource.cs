namespace Acode.Domain.PromptPacks;

/// <summary>
/// Defines the source of a prompt pack.
/// </summary>
public enum PackSource
{
    /// <summary>
    /// Built-in pack shipped with Acode (embedded resources).
    /// </summary>
    BuiltIn = 0,

    /// <summary>
    /// User-provided pack from workspace .acode/prompts/ directory.
    /// </summary>
    User = 1,
}
