using Acode.Domain.Configuration;

namespace Acode.Application.Configuration;

/// <summary>
/// Applies default values to configuration objects.
/// Creates a new configuration instance with defaults applied for missing fields.
/// </summary>
/// <remarks>
/// Per FR-002b-91: Defaults MUST be applied after parsing, before validation.
/// Per FR-002b-92: Defaults MUST NOT override explicit values.
/// Per FR-002b-93: Defaults MUST be defined in single location (ConfigDefaults).
/// </remarks>
public sealed class DefaultValueApplicator
{
    /// <summary>
    /// Applies default values to a configuration object.
    /// Returns a new instance with defaults applied for missing nested objects.
    /// </summary>
    /// <param name="config">Input configuration (may have null nested objects).</param>
    /// <returns>New configuration with defaults applied, or null if input is null.</returns>
    public AcodeConfig? Apply(AcodeConfig? config)
    {
        if (config == null)
        {
            return null;
        }

        // FR-002b-92: Do not override explicit values
        // C# record default initializers handle field-level defaults
        // This method handles object-level defaults (creating missing nested objects)
        return config with
        {
            SchemaVersion = string.IsNullOrWhiteSpace(config.SchemaVersion)
                ? ConfigDefaults.SchemaVersion
                : config.SchemaVersion,

            Mode = config.Mode ?? new ModeConfig(),

            Model = config.Model ?? new ModelConfig()
        };
    }
}
