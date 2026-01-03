namespace Acode.Application.Configuration;

/// <summary>
/// Service for validating configuration files against JSON Schema.
/// </summary>
public interface ISchemaValidator
{
    /// <summary>
    /// Validates a YAML configuration file against the JSON Schema.
    /// </summary>
    /// <param name="yamlFilePath">Path to the YAML configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with any schema violations.</returns>
    Task<ValidationResult> ValidateAsync(string yamlFilePath, CancellationToken cancellationToken = default);
}
