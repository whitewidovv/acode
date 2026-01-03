using Acode.Domain.Configuration;

namespace Acode.Application.Configuration;

/// <summary>
/// Loads configuration from .agent/config.yml files.
/// </summary>
public sealed class ConfigLoader : IConfigLoader
{
    private readonly IConfigValidator _validator;

    public ConfigLoader(IConfigValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
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

        // For now, this is a placeholder - actual loading would use YamlConfigReader
        // This will be implemented once we wire up the Infrastructure layer
        throw new NotImplementedException(
            "Configuration loading requires Infrastructure layer integration (YamlConfigReader)");
    }
}
