using System.Text.Json;
using System.Text.Json.Serialization;
using Acode.Application.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Acode.Cli.Commands;

/// <summary>
/// Implements config validate and config show CLI commands.
/// </summary>
public sealed class ConfigCommand : ICommand
{
    private readonly IConfigLoader _loader;
    private readonly IConfigValidator _validator;
    private readonly ConfigRedactor _redactor;
    private readonly IConfigCache? _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigCommand"/> class.
    /// </summary>
    /// <param name="loader">Configuration loader.</param>
    /// <param name="validator">Configuration validator.</param>
    /// <param name="cache">Optional configuration cache for reload command.</param>
    public ConfigCommand(IConfigLoader loader, IConfigValidator validator, IConfigCache? cache = null)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _redactor = new ConfigRedactor();
        _cache = cache;
    }

    /// <inheritdoc/>
    public string Name => "config";

    /// <inheritdoc/>
    public string[]? Aliases => null;

    /// <inheritdoc/>
    public string Description => "Manage configuration file";

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Args.Length == 0)
        {
            await context.Output.WriteLineAsync("Error: Missing subcommand. Use 'acode config validate' or 'acode config show'.").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var subcommand = context.Args[0];
        var repositoryRoot = Directory.GetCurrentDirectory();

        return subcommand.ToLowerInvariant() switch
        {
            "validate" => await ValidateAsync(context, repositoryRoot).ConfigureAwait(false),
            "show" => await ShowAsync(context, repositoryRoot).ConfigureAwait(false),
            "init" => await InitAsync(context, repositoryRoot).ConfigureAwait(false),
            "reload" => await ReloadAsync(context).ConfigureAwait(false),
            _ => await WriteUnknownSubcommandAsync(context, subcommand).ConfigureAwait(false),
        };
    }

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"Usage: acode config <subcommand> [options]

Subcommands:
  init        Create a minimal configuration file
  validate    Validate the configuration file
  show        Display the configuration file
  reload      Invalidate configuration cache

Examples:
  acode config init
  acode config validate
  acode config show
  acode config show --format json
  acode config reload";
    }

    private static async Task<ExitCode> WriteUnknownSubcommandAsync(CommandContext context, string subcommand)
    {
        await context.Output.WriteLineAsync($"Error: Unknown subcommand '{subcommand}'.").ConfigureAwait(false);
        await context.Output.WriteLineAsync("Use 'acode config validate' or 'acode config show'.").ConfigureAwait(false);
        return ExitCode.InvalidArguments;
    }

    private async Task<ExitCode> ValidateAsync(CommandContext context, string repositoryRoot)
    {
        try
        {
            await context.Output.WriteLineAsync("Validating configuration...").ConfigureAwait(false);
            await context.Output.WriteLineAsync().ConfigureAwait(false);

            var config = await _loader.LoadAsync(repositoryRoot, context.CancellationToken).ConfigureAwait(false);
            var result = _validator.Validate(config);

            // Display validation results
            await context.Output.WriteLineAsync($"  ✓ Schema version: {config.SchemaVersion ?? "default"}").ConfigureAwait(false);

            if (config.Project != null)
            {
                await context.Output.WriteLineAsync($"  ✓ Project: {config.Project.Name ?? "unnamed"} ({config.Project.Type ?? "unknown"})").ConfigureAwait(false);
            }

            if (config.Mode != null)
            {
                await context.Output.WriteLineAsync($"  ✓ Mode: {config.Mode.Default}").ConfigureAwait(false);
            }

            if (config.Model != null)
            {
                await context.Output.WriteLineAsync($"  ✓ Model: {config.Model.Provider ?? "default"}/{config.Model.Name ?? "default"}").ConfigureAwait(false);
            }

            await context.Output.WriteLineAsync().ConfigureAwait(false);

            if (result.IsValid)
            {
                await context.Output.WriteLineAsync("  ✓ Configuration valid").ConfigureAwait(false);
                return ExitCode.Success;
            }
            else
            {
                await context.Output.WriteLineAsync("  ✗ Configuration invalid").ConfigureAwait(false);
                await context.Output.WriteLineAsync().ConfigureAwait(false);
                await context.Output.WriteLineAsync("Errors:").ConfigureAwait(false);

                foreach (var error in result.Errors)
                {
                    var severity = error.Severity == ValidationSeverity.Error ? "ERROR" : "WARNING";
                    await context.Output.WriteLineAsync($"  [{severity}] {error.Path}: {error.Message}").ConfigureAwait(false);
                }

                return ExitCode.GeneralError;
            }
        }
        catch (FileNotFoundException)
        {
            await context.Output.WriteLineAsync("Error: Configuration validation failed:").ConfigureAwait(false);
            await context.Output.WriteLineAsync("CFG001: Configuration file not found: .agent/config.yml").ConfigureAwait(false);
            return ExitCode.ConfigurationError;
        }
        catch (InvalidOperationException ex)
        {
            await context.Output.WriteLineAsync("Error: Configuration invalid:").ConfigureAwait(false);
            await context.Output.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return ExitCode.ConfigurationError;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }

    private async Task<ExitCode> ShowAsync(CommandContext context, string repositoryRoot)
    {
        try
        {
            var format = "yaml";

            // Parse --format option
            for (int i = 1; i < context.Args.Length; i++)
            {
                if (context.Args[i] == "--format" && i + 1 < context.Args.Length)
                {
                    format = context.Args[i + 1];
                    break;
                }
            }

            var config = await _loader.LoadAsync(repositoryRoot, context.CancellationToken).ConfigureAwait(false);

            // Redact sensitive fields per NFR-002b-06
            var redactedConfig = _redactor.Redact(config);

            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                };

                var json = JsonSerializer.Serialize(redactedConfig, options);
                await context.Output.WriteLineAsync(json).ConfigureAwait(false);
            }
            else
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .Build();

                var yaml = serializer.Serialize(redactedConfig);
                await context.Output.WriteLineAsync(yaml).ConfigureAwait(false);
            }

            return ExitCode.Success;
        }
        catch (FileNotFoundException)
        {
            await context.Output.WriteLineAsync("Error: Configuration file not found: .agent/config.yml").ConfigureAwait(false);
            return ExitCode.ConfigurationError;
        }
        catch (InvalidOperationException ex)
        {
            await context.Output.WriteLineAsync("Error: Configuration validation failed:").ConfigureAwait(false);
            await context.Output.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return ExitCode.ConfigurationError;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }

    private async Task<ExitCode> InitAsync(CommandContext context, string repositoryRoot)
    {
        try
        {
            var agentDir = Path.Combine(repositoryRoot, ".agent");
            var configPath = Path.Combine(agentDir, "config.yml");

            // Check if config already exists
            if (File.Exists(configPath))
            {
                await context.Output.WriteLineAsync("Error: Configuration file already exists at .agent/config.yml").ConfigureAwait(false);
                return ExitCode.GeneralError;
            }

            // Create .agent directory if it doesn't exist
            if (!Directory.Exists(agentDir))
            {
                Directory.CreateDirectory(agentDir);
            }

            // Create minimal configuration
            var minimalConfig = @"schema_version: ""1.0.0""

# This is a minimal Acode configuration file.
# For full configuration options, see: https://github.com/whitewidovv/acode

# project:
#   name: my-project
#   type: dotnet

# mode:
#   default: local-only
#   allow_burst: false

# model:
#   provider: ollama
#   name: codellama:7b
";

            await File.WriteAllTextAsync(configPath, minimalConfig).ConfigureAwait(false);

            await context.Output.WriteLineAsync("Created .agent/config.yml with minimal configuration").ConfigureAwait(false);
            await context.Output.WriteLineAsync("Edit the file to customize settings for your project.").ConfigureAwait(false);

            return ExitCode.Success;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: Failed to create configuration file: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }

    private async Task<ExitCode> ReloadAsync(CommandContext context)
    {
        try
        {
            if (_cache == null)
            {
                await context.Output.WriteLineAsync("Configuration cache is not available").ConfigureAwait(false);
                return ExitCode.Success;
            }

            _cache.InvalidateAll();

            await context.Output.WriteLineAsync("Configuration cache invalidated successfully").ConfigureAwait(false);
            await context.Output.WriteLineAsync("Next config load will re-parse .agent/config.yml").ConfigureAwait(false);

            return ExitCode.Success;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: Failed to invalidate cache: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }
}
