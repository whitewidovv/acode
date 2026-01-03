using Acode.Domain.Configuration;

namespace Acode.Application.Configuration;

/// <summary>
/// Service for validating configuration against schema and business rules.
/// </summary>
public interface IConfigValidator
{
    /// <summary>
    /// Validates a configuration file.
    /// </summary>
    /// <param name="configFilePath">Path to the configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with any errors or warnings.</returns>
    Task<ValidationResult> ValidateFileAsync(string configFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an already-loaded configuration object.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>Validation result with any errors or warnings.</returns>
    ValidationResult Validate(AcodeConfig config);
}
