using System.Text.RegularExpressions;
using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Validates prompt packs for correctness and security.
/// </summary>
public sealed partial class PackValidator : IPackValidator
{
    private const long MaxPackSizeBytes = 5 * 1024 * 1024; // 5MB
    private const long MaxComponentSizeBytes = 1 * 1024 * 1024; // 1MB

    private readonly ManifestParser _manifestParser;
    private readonly ILogger<PackValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackValidator"/> class.
    /// </summary>
    /// <param name="manifestParser">The manifest parser.</param>
    /// <param name="logger">The logger.</param>
    public PackValidator(ManifestParser manifestParser, ILogger<PackValidator> logger)
    {
        _manifestParser = manifestParser;
        _logger = logger;
    }

    /// <inheritdoc/>
    public ValidationResult Validate(PromptPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        var errors = new List<ValidationError>();

        // Validate pack ID
        if (!PackManifest.IsValidPackId(pack.Id))
        {
            errors.Add(new ValidationError
            {
                Code = "ACODE-VAL-002",
                Message = $"Invalid pack ID: '{pack.Id}'. Must be kebab-case, 3-64 characters.",
            });
        }

        // Validate components exist
        if (pack.Components.Count == 0)
        {
            errors.Add(new ValidationError
            {
                Code = "ACODE-VAL-001",
                Message = "Pack must have at least one component.",
            });
        }

        // Validate total size
        var totalSize = pack.Components.Sum(c => (long)c.Content.Length);
        if (totalSize > MaxPackSizeBytes)
        {
            errors.Add(new ValidationError
            {
                Code = "ACODE-VAL-006",
                Message = $"Pack size {totalSize:N0} bytes exceeds limit of {MaxPackSizeBytes:N0} bytes.",
            });
        }

        // Validate individual components
        foreach (var component in pack.Components)
        {
            ValidateComponent(component, errors);
        }

        // Validate template variables
        ValidateTemplateVariables(pack, errors);

        if (errors.Count > 0)
        {
            _logger.LogWarning("Pack {PackId} has {ErrorCount} validation errors", pack.Id, errors.Count);
        }

        return new ValidationResult(errors);
    }

    /// <inheritdoc/>
    public ValidationResult ValidatePath(string packPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packPath);

        var errors = new List<ValidationError>();

        var manifestPath = Path.Combine(packPath, "manifest.yml");
        if (!File.Exists(manifestPath))
        {
            errors.Add(new ValidationError
            {
                Code = "ACODE-VAL-001",
                Message = "Missing required file: manifest.yml",
                FilePath = manifestPath,
            });
            return new ValidationResult(errors);
        }

        // Try to parse manifest
        PackManifest manifest;
        try
        {
            manifest = _manifestParser.ParseFile(manifestPath, PackSource.User);
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Code = "ACODE-VAL-001",
                Message = $"Failed to parse manifest: {ex.Message}",
                FilePath = manifestPath,
            });
            return new ValidationResult(errors);
        }

        // Validate pack ID
        if (!PackManifest.IsValidPackId(manifest.Id))
        {
            errors.Add(new ValidationError
            {
                Code = "ACODE-VAL-002",
                Message = $"Invalid pack ID: '{manifest.Id}'. Must be kebab-case, 3-64 characters.",
                FilePath = manifestPath,
            });
        }

        // Validate components exist on disk
        foreach (var component in manifest.Components)
        {
            var componentPath = Path.Combine(packPath, component.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(componentPath))
            {
                errors.Add(new ValidationError
                {
                    Code = "ACODE-VAL-004",
                    Message = $"Component not found: {component.Path}",
                    FilePath = componentPath,
                });
            }
            else
            {
                var fileInfo = new FileInfo(componentPath);
                if (fileInfo.Length > MaxComponentSizeBytes)
                {
                    errors.Add(new ValidationError
                    {
                        Code = "ACODE-VAL-006",
                        Message = $"Component {component.Path} exceeds size limit of {MaxComponentSizeBytes:N0} bytes.",
                        FilePath = componentPath,
                    });
                }
            }
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning("Path validation for {Path} found {ErrorCount} errors", packPath, errors.Count);
        }

        return new ValidationResult(errors);
    }

    /// <summary>
    /// Validates template variables strictly, requiring all to be declared.
    /// Used when strict validation is explicitly requested.
    /// </summary>
    /// <param name="pack">The pack to validate.</param>
    /// <param name="errors">The list to add errors to.</param>
    internal void ValidateTemplateVariablesStrict(PromptPack pack, List<ValidationError> errors)
    {
        var variablePattern = TemplateVariableRegex();

        foreach (var component in pack.Components)
        {
            var matches = variablePattern.Matches(component.Content);
            foreach (Match match in matches)
            {
                var varName = match.Groups[1].Value;
                var isDeclared = component.Metadata?.ContainsKey(varName) == true;

                if (!isDeclared)
                {
                    _logger.LogWarning(
                        "Undeclared template variable {VarName} in {Path}",
                        varName,
                        component.Path);

                    errors.Add(new ValidationError
                    {
                        Code = "ACODE-VAL-005",
                        Message = $"Undeclared template variable '{{{{{varName}}}}}' in {component.Path}. Declare it in component metadata.",
                        FilePath = component.Path,
                    });
                }
            }
        }
    }

    private static void ValidateComponent(LoadedComponent component, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(component.Path))
        {
            errors.Add(new ValidationError
            {
                Code = "ACODE-VAL-001",
                Message = "Component path cannot be empty.",
            });
        }

        if (component.Content.Length > MaxComponentSizeBytes)
        {
            errors.Add(new ValidationError
            {
                Code = "ACODE-VAL-006",
                Message = $"Component {component.Path} exceeds size limit of {MaxComponentSizeBytes:N0} bytes.",
                FilePath = component.Path,
            });
        }
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.Compiled)]
    private static partial Regex TemplateVariableRegex();

    private void ValidateTemplateVariables(PromptPack pack, List<ValidationError> errors)
    {
        // Find template variables used in components
        var variablePattern = TemplateVariableRegex();

        foreach (var component in pack.Components)
        {
            var matches = variablePattern.Matches(component.Content);
            foreach (Match match in matches)
            {
                var varName = match.Groups[1].Value;

                // Check if variable is declared in component metadata
                var isDeclared = component.Metadata?.ContainsKey(varName) == true;

                if (!isDeclared)
                {
                    // Log as debug - undeclared variables are expected when values are provided at composition time
                    _logger.LogDebug(
                        "Template variable {VarName} in {Path} has no default value declared - will use composition context",
                        varName,
                        component.Path);
                }
                else
                {
                    _logger.LogDebug(
                        "Found declared template variable {VarName} in {Path}",
                        varName,
                        component.Path);
                }
            }
        }
    }
}
