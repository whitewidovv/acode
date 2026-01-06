using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Service for validating prompt packs.
/// </summary>
public interface IPackValidator
{
    /// <summary>
    /// Validates a prompt pack against all validation rules.
    /// </summary>
    /// <param name="pack">The pack to validate.</param>
    /// <returns>Validation result containing any errors found.</returns>
    /// <remarks>
    /// Validation checks:
    /// - Manifest required fields present (id, version, name, description).
    /// - Pack ID format (lowercase, hyphens only).
    /// - Version is valid SemVer 2.0.0.
    /// - Component paths are relative and don't contain traversal.
    /// - Template variable syntax is correct ({{variable_name}}).
    /// - Total pack size does not exceed 5MB limit.
    /// - All component files exist and are readable.
    ///
    /// Performance: Validation must complete within 100ms for typical packs.
    /// </remarks>
    ValidationResult Validate(PromptPack pack);
}
