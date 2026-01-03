using Acode.Domain.Configuration;

namespace Acode.Application.Configuration;

/// <summary>
/// Loads configuration from .agent/config.yml files.
/// Orchestrates validation and reading.
/// </summary>
public sealed class ConfigLoader : IConfigLoader
{
    private readonly IConfigValidator _validator;
    private readonly IConfigReader _reader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigLoader"/> class.
    /// </summary>
    /// <param name="validator">The configuration validator.</param>
    /// <param name="reader">The configuration reader.</param>
    public ConfigLoader(IConfigValidator validator, IConfigReader reader)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    /// <inheritdoc/>
    public async Task<AcodeConfig> LoadAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        var configPath = Path.Combine(repositoryRoot, ".agent", "config.yml");
        return await LoadFromPathAsync(configPath, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<AcodeConfig> LoadFromPathAsync(string configFilePath, CancellationToken cancellationToken = default)
    {
        // Validate first
        var validationResult = await _validator.ValidateFileAsync(configFilePath, cancellationToken)
            .ConfigureAwait(false);

        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join(
                Environment.NewLine,
                validationResult.ErrorsOnly.Select(e => $"{e.Code}: {e.Message}"));

            throw new InvalidOperationException(
                $"Configuration validation failed:{Environment.NewLine}{errorMessages}");
        }

        // Read the configuration using the injected reader
        var config = await _reader.ReadAsync(configFilePath, cancellationToken).ConfigureAwait(false);

        return config;
    }
}
