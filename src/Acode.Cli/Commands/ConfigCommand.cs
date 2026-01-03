using System.Text.Json;
using System.Text.Json.Serialization;
using Acode.Application.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Acode.Cli.Commands;

/// <summary>
/// Implements config validate and config show CLI commands.
/// </summary>
public sealed class ConfigCommand
{
    private readonly IConfigLoader _loader;
    private readonly IConfigValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigCommand"/> class.
    /// </summary>
    /// <param name="loader">Configuration loader.</param>
    /// <param name="validator">Configuration validator.</param>
    public ConfigCommand(IConfigLoader loader, IConfigValidator validator)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// Validates the configuration file.
    /// </summary>
    /// <param name="filePath">Path to config file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code (0 for success, 1 for errors).</returns>
    public async Task<int> ValidateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("Validating configuration...");
            Console.WriteLine();

            var config = await _loader.LoadAsync(filePath, cancellationToken).ConfigureAwait(false);
            var result = _validator.Validate(config);

            // Display validation results
            Console.WriteLine($"  ✓ Schema version: {config.SchemaVersion ?? "default"}");

            if (config.Project != null)
            {
                Console.WriteLine($"  ✓ Project: {config.Project.Name ?? "unnamed"} ({config.Project.Type ?? "unknown"})");
            }

            if (config.Mode != null)
            {
                Console.WriteLine($"  ✓ Mode: {config.Mode.Default}");
            }

            if (config.Model != null)
            {
                Console.WriteLine($"  ✓ Model: {config.Model.Provider ?? "default"}/{config.Model.Name ?? "default"}");
            }

            Console.WriteLine();

            if (result.IsValid)
            {
                Console.WriteLine("  ✓ Configuration valid");
                return 0;
            }
            else
            {
                Console.WriteLine("  ✗ Configuration invalid");
                Console.WriteLine();
                Console.WriteLine("Errors:");

                foreach (var error in result.Errors)
                {
                    var severity = error.Severity == ValidationSeverity.Error ? "ERROR" : "WARNING";
                    Console.WriteLine($"  [{severity}] {error.Path}: {error.Message}");
                }

                return 1;
            }
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Error: Configuration file not found: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Shows the configuration file.
    /// </summary>
    /// <param name="filePath">Path to config file.</param>
    /// <param name="format">Output format (yaml or json).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code (0 for success, 1 for errors).</returns>
    public async Task<int> ShowAsync(string filePath, string format = "yaml", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(format);

        try
        {
            var config = await _loader.LoadAsync(filePath, cancellationToken).ConfigureAwait(false);

            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                };

                var json = JsonSerializer.Serialize(config, options);
                Console.WriteLine(json);
            }
            else
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .Build();

                var yaml = serializer.Serialize(config);
                Console.WriteLine(yaml);
            }

            return 0;
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Error: Configuration file not found: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
