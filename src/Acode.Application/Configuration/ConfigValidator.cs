using Acode.Domain.Configuration;

namespace Acode.Application.Configuration;

/// <summary>
/// Validates configuration against schema and business rules.
/// </summary>
public sealed class ConfigValidator : IConfigValidator
{
    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateFileAsync(string configFilePath, CancellationToken cancellationToken = default)
    {
        // Check file exists
        if (!File.Exists(configFilePath))
        {
            return ValidationResult.Failure(new ValidationError
            {
                Code = ConfigErrorCodes.FileNotFound,
                Message = $"Configuration file not found: {configFilePath}",
                Severity = ValidationSeverity.Error,
                Suggestion = "Create a .agent/config.yml file in your repository root"
            });
        }

        // Check file size (max 1MB)
        var fileInfo = new FileInfo(configFilePath);
        if (fileInfo.Length > 1_048_576)
        {
            return ValidationResult.Failure(new ValidationError
            {
                Code = ConfigErrorCodes.FileTooBig,
                Message = $"Configuration file is too large: {fileInfo.Length} bytes (max 1MB)",
                Severity = ValidationSeverity.Error
            });
        }

        // For now, this is a basic implementation
        // Full implementation would use JsonSchemaValidator from Infrastructure
        await Task.CompletedTask.ConfigureAwait(false);

        return ValidationResult.Success();
    }

    /// <inheritdoc/>
    public ValidationResult Validate(AcodeConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var errors = new List<ValidationError>();

        // Validate schema version
        if (string.IsNullOrWhiteSpace(config.SchemaVersion))
        {
            errors.Add(new ValidationError
            {
                Code = ConfigErrorCodes.MissingRequiredField,
                Message = "schema_version is required",
                Severity = ValidationSeverity.Error,
                Path = "schema_version"
            });
        }

        // Add more semantic validation as needed
        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }
}
