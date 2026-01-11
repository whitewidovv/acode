using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Interface for validating prompt packs.
/// </summary>
public interface IPackValidator
{
    /// <summary>
    /// Validates a loaded prompt pack.
    /// </summary>
    /// <param name="pack">The pack to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(PromptPack pack);

    /// <summary>
    /// Validates a pack at the specified path without loading it fully.
    /// </summary>
    /// <param name="packPath">The path to the pack directory.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidatePath(string packPath);
}
