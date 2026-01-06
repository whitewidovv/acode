using System.Text;
using System.Text.RegularExpressions;
using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Validates prompt packs against schema and business rules.
/// </summary>
public sealed partial class PackValidator : IPackValidator
{
    private const int MaxPackSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly Regex PackIdRegex = GetPackIdRegex();
    private static readonly Regex TemplateVariableRegex = GetTemplateVariableRegex();

    /// <inheritdoc/>
    public ValidationResult Validate(PromptPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        var errors = new List<ValidationError>();

        // Validate manifest
        ValidateManifest(pack.Manifest, errors);

        // Validate components
        ValidateComponents(pack, errors);

        // Validate total size
        ValidateTotalSize(pack, errors);

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }

    private static void ValidateManifest(PackManifest manifest, List<ValidationError> errors)
    {
        // Validate ID
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            errors.Add(new ValidationError(
                "PACK_ID_REQUIRED",
                "Pack ID is required"));
        }
        else if (!PackIdRegex.IsMatch(manifest.Id))
        {
            errors.Add(new ValidationError(
                "PACK_ID_INVALID_FORMAT",
                "Pack ID must contain only lowercase letters, numbers, and hyphens"));
        }

        // Validate Name
        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            errors.Add(new ValidationError(
                "PACK_NAME_REQUIRED",
                "Pack name is required"));
        }

        // Validate Description
        if (string.IsNullOrWhiteSpace(manifest.Description))
        {
            errors.Add(new ValidationError(
                "PACK_DESCRIPTION_REQUIRED",
                "Pack description is required"));
        }
    }

    private static void ValidateComponents(PromptPack pack, List<ValidationError> errors)
    {
        foreach (var component in pack.Manifest.Components)
        {
            // Validate path is relative
            if (Path.IsPathRooted(component.Path))
            {
                errors.Add(new ValidationError(
                    "COMPONENT_PATH_ABSOLUTE",
                    $"Component path must be relative, not absolute: {component.Path}",
                    component.Path));
                continue;
            }

            // Validate no path traversal
            if (component.Path.Contains("..", StringComparison.Ordinal))
            {
                errors.Add(new ValidationError(
                    "COMPONENT_PATH_TRAVERSAL",
                    $"Component path contains path traversal: {component.Path}",
                    component.Path));
                continue;
            }

            // Validate template variable syntax in content
            if (!string.IsNullOrEmpty(component.Content))
            {
                ValidateTemplateVariables(component.Path, component.Content, errors);
            }
        }
    }

    private static void ValidateTemplateVariables(string componentPath, string content, List<ValidationError> errors)
    {
        // Find all potential template variables (anything between {{ and }})
        var matches = Regex.Matches(content, @"\{\{([^}]*)\}\}");

        var invalidVariables = matches.Cast<Match>()
            .Select(match => match.Groups[1].Value.Trim())
            .Where(variableName => !TemplateVariableRegex.IsMatch(variableName));

        foreach (var variableName in invalidVariables)
        {
            errors.Add(new ValidationError(
                "INVALID_TEMPLATE_VARIABLE",
                $"Invalid template variable syntax: '{{{{{variableName}}}}}'. Variables must contain only letters, numbers, and underscores.",
                componentPath));
        }
    }

    private static void ValidateTotalSize(PromptPack pack, List<ValidationError> errors)
    {
        long totalBytes = pack.Components.Values
            .Where(component => !string.IsNullOrEmpty(component.Content))
            .Sum(component => (long)Encoding.UTF8.GetByteCount(component.Content!));

        if (totalBytes > MaxPackSizeBytes)
        {
            var sizeMB = totalBytes / (1024.0 * 1024.0);
            errors.Add(new ValidationError(
                "PACK_SIZE_EXCEEDS_LIMIT",
                $"Pack size ({sizeMB:F2} MB) exceeds the maximum allowed size of 5 MB"));
        }
    }

    [GeneratedRegex("^[a-z0-9-]+$", RegexOptions.Compiled)]
    private static partial Regex GetPackIdRegex();

    [GeneratedRegex("^[a-zA-Z0-9_]+$", RegexOptions.Compiled)]
    private static partial Regex GetTemplateVariableRegex();
}
