namespace Acode.Domain.PromptPacks;

/// <summary>
/// Defines the source location of a prompt pack.
/// </summary>
public enum PackSource
{
    /// <summary>
    /// Pack is embedded within the application as a built-in resource.
    /// </summary>
    BuiltIn,

    /// <summary>
    /// Pack is loaded from the user's configuration directory.
    /// </summary>
    User,
}
