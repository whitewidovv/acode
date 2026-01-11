using System.Text.RegularExpressions;
using Acode.Domain.PromptPacks;
using Acode.Domain.PromptPacks.Exceptions;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Validates that a PackManifest conforms to the schema requirements.
/// </summary>
/// <remarks>
/// Validates:
/// - Required fields are present and non-empty.
/// - Id format is lowercase with hyphens only.
/// - FormatVersion is exactly "1.0".
/// - Component paths do not contain traversal sequences or absolute paths.
/// </remarks>
public sealed class ManifestSchemaValidator
{
    private static readonly Regex IdPattern = new Regex(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    /// <summary>
    /// Validates a PackManifest against schema requirements.
    /// </summary>
    /// <param name="manifest">The manifest to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when manifest is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    /// <exception cref="PathTraversalException">Thrown when a component path contains traversal or is absolute.</exception>
    public void Validate(PackManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        ValidateRequiredFields(manifest);
        ValidateFormatVersion(manifest);
        ValidateIdFormat(manifest);
        ValidateComponentPaths(manifest);
    }

    private static void ValidateRequiredFields(PackManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            throw new ArgumentException("Manifest 'id' field is required and cannot be empty.", nameof(manifest));
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            throw new ArgumentException("Manifest 'name' field is required and cannot be empty.", nameof(manifest));
        }

        if (string.IsNullOrWhiteSpace(manifest.Description))
        {
            throw new ArgumentException("Manifest 'description' field is required and cannot be empty.", nameof(manifest));
        }
    }

    private static void ValidateFormatVersion(PackManifest manifest)
    {
        if (manifest.FormatVersion != "1.0")
        {
            throw new ArgumentException(
                $"Manifest 'format_version' must be '1.0'. Got: '{manifest.FormatVersion}'.",
                nameof(manifest));
        }
    }

    private static void ValidateIdFormat(PackManifest manifest)
    {
        if (!IdPattern.IsMatch(manifest.Id))
        {
            throw new ArgumentException(
                $"Manifest 'id' must be lowercase with hyphens only (e.g., 'acode-standard'). Got: '{manifest.Id}'.",
                nameof(manifest));
        }
    }

    private static void ValidateComponentPaths(PackManifest manifest)
    {
        foreach (var component in manifest.Components)
        {
            ValidatePath(component.Path);
        }
    }

    private static void ValidatePath(string path)
    {
        // Check for directory traversal (..)
        if (path.Contains("..", StringComparison.Ordinal))
        {
            throw new PathTraversalException(
                path,
                $"Path traversal attempt detected: '{path}'. Paths must be relative and not contain directory traversal sequences.");
        }

        // Check for absolute paths (/ or C:\)
        if (path.StartsWith('/') || (path.Length >= 3 && path[1] == ':' && (path[2] == '/' || path[2] == '\\')))
        {
            throw new PathTraversalException(
                path,
                $"Absolute path not allowed: '{path}'. Paths must be relative to pack root.");
        }
    }
}
