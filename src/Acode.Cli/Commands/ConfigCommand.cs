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
            _ => await WriteUnknownSubcommandAsync(context, subcommand).ConfigureAwait(false),
        };
    }

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"Usage: acode config <subcommand> [options]

Subcommands:
  validate    Validate the configuration file
  show        Display the configuration file

Examples:
  acode config validate
  acode config show
  acode config show --format json";
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
            return ExitCode.GeneralError;
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

            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                };

                var json = JsonSerializer.Serialize(config, options);
                await context.Output.WriteLineAsync(json).ConfigureAwait(false);
            }
            else
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .Build();

                var yaml = serializer.Serialize(config);
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
}
