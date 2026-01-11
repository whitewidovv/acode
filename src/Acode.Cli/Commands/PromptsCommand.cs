using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Acode.Domain.PromptPacks.Exceptions;

namespace Acode.Cli.Commands;

/// <summary>
/// Implements prompts CLI commands for managing prompt packs.
/// </summary>
/// <remarks>
/// Subcommands:
/// - list: Show all available packs with id, version, source, and active flag.
/// - show: Display pack details including components.
/// - validate: Validate a pack and output errors.
/// - reload: Refresh the registry cache.
///
/// Implements AC-065 through AC-073 from Task 008b.
/// </remarks>
public sealed class PromptsCommand : ICommand
{
    private readonly IPromptPackRegistry _registry;
    private readonly IPromptPackLoader _loader;
    private readonly IPackValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptsCommand"/> class.
    /// </summary>
    /// <param name="registry">The prompt pack registry.</param>
    /// <param name="loader">The prompt pack loader.</param>
    /// <param name="validator">The pack validator.</param>
    public PromptsCommand(
        IPromptPackRegistry registry,
        IPromptPackLoader loader,
        IPackValidator validator)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <inheritdoc/>
    public string Name => "prompts";

    /// <inheritdoc/>
    public string[]? Aliases => new[] { "packs" };

    /// <inheritdoc/>
    public string Description => "Manage prompt packs";

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Args.Length == 0)
        {
            await context.Output.WriteLineAsync("Error: Missing subcommand. Use 'acode prompts list', 'show', 'validate', or 'reload'.").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var subcommand = context.Args[0];

        return subcommand.ToLowerInvariant() switch
        {
            "list" => await ListAsync(context).ConfigureAwait(false),
            "show" => await ShowAsync(context).ConfigureAwait(false),
            "validate" => await ValidateAsync(context).ConfigureAwait(false),
            "reload" => await ReloadAsync(context).ConfigureAwait(false),
            _ => await WriteUnknownSubcommandAsync(context, subcommand).ConfigureAwait(false),
        };
    }

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"Usage: acode prompts <subcommand> [options]

Subcommands:
  list                  List all available prompt packs
  show <pack-id>        Show details of a specific pack
  validate <path>       Validate a pack at the specified path
  reload                Reload the prompt pack registry

Examples:
  acode prompts list
  acode prompts show acode-standard
  acode prompts validate .acode/prompts/my-pack
  acode prompts reload";
    }

    private static async Task<ExitCode> WriteUnknownSubcommandAsync(CommandContext context, string subcommand)
    {
        await context.Output.WriteLineAsync($"Error: Unknown subcommand '{subcommand}'.").ConfigureAwait(false);
        await context.Output.WriteLineAsync("Use 'acode prompts list', 'show', 'validate', or 'reload'.").ConfigureAwait(false);
        return ExitCode.InvalidArguments;
    }

    /// <summary>
    /// Lists all available packs with id, version, source, and active flag.
    /// AC-065, AC-066, AC-067.
    /// </summary>
    private async Task<ExitCode> ListAsync(CommandContext context)
    {
        var packs = _registry.ListPacks();
        var activePackId = _registry.GetActivePackId();

        if (packs.Count == 0)
        {
            await context.Output.WriteLineAsync("No prompt packs found.").ConfigureAwait(false);
            return ExitCode.Success;
        }

        await context.Output.WriteLineAsync("Available prompt packs:").ConfigureAwait(false);
        await context.Output.WriteLineAsync().ConfigureAwait(false);

        // Header
        await context.Output.WriteLineAsync("  ID                      VERSION     SOURCE      ACTIVE").ConfigureAwait(false);
        await context.Output.WriteLineAsync("  ─────────────────────   ─────────   ─────────   ──────").ConfigureAwait(false);

        foreach (var pack in packs)
        {
            var isActive = string.Equals(pack.Id, activePackId, StringComparison.OrdinalIgnoreCase);
            var activeMarker = isActive ? "*" : " ";
            var source = pack.Source == PackSource.BuiltIn ? "built-in" : "user";

            await context.Output.WriteLineAsync(
                $"{activeMarker} {pack.Id,-24} {pack.Version,-11} {source,-11} {(isActive ? "active" : string.Empty)}")
                .ConfigureAwait(false);
        }

        await context.Output.WriteLineAsync().ConfigureAwait(false);
        await context.Output.WriteLineAsync($"Total: {packs.Count} pack(s)").ConfigureAwait(false);

        return ExitCode.Success;
    }

    /// <summary>
    /// Shows details of a specific pack including components.
    /// AC-068, AC-069.
    /// </summary>
    private async Task<ExitCode> ShowAsync(CommandContext context)
    {
        if (context.Args.Length < 2)
        {
            await context.Output.WriteLineAsync("Error: Missing pack ID. Usage: acode prompts show <pack-id>").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var packId = context.Args[1];
        var pack = _registry.TryGetPack(packId);

        if (pack is null)
        {
            await context.Output.WriteLineAsync($"Error: Pack '{packId}' not found.").ConfigureAwait(false);
            return ExitCode.GeneralError;
        }

        await context.Output.WriteLineAsync($"Pack: {pack.Name}").ConfigureAwait(false);
        await context.Output.WriteLineAsync($"  ID:          {pack.Id}").ConfigureAwait(false);
        await context.Output.WriteLineAsync($"  Version:     {pack.Version}").ConfigureAwait(false);
        await context.Output.WriteLineAsync($"  Source:      {(pack.Source == PackSource.BuiltIn ? "built-in" : "user")}").ConfigureAwait(false);
        await context.Output.WriteLineAsync($"  Path:        {pack.PackPath}").ConfigureAwait(false);

        if (!string.IsNullOrEmpty(pack.Description))
        {
            await context.Output.WriteLineAsync($"  Description: {pack.Description}").ConfigureAwait(false);
        }

        if (pack.ContentHash is not null)
        {
            await context.Output.WriteLineAsync($"  Hash:        {pack.ContentHash.Value[..16]}...").ConfigureAwait(false);
        }

        await context.Output.WriteLineAsync().ConfigureAwait(false);
        await context.Output.WriteLineAsync("Components:").ConfigureAwait(false);

        foreach (var component in pack.Components)
        {
            var typeLabel = component.Type.ToString().ToLowerInvariant();
            await context.Output.WriteLineAsync($"  - {component.Path} ({typeLabel})").ConfigureAwait(false);
        }

        return ExitCode.Success;
    }

    /// <summary>
    /// Validates a pack and outputs errors.
    /// AC-070, AC-071, AC-072.
    /// </summary>
    private async Task<ExitCode> ValidateAsync(CommandContext context)
    {
        string packPath;

        if (context.Args.Length < 2)
        {
            // Use current directory if no path provided
            packPath = Directory.GetCurrentDirectory();
        }
        else
        {
            packPath = context.Args[1];
        }

        try
        {
            await context.Output.WriteLineAsync($"Validating pack at: {packPath}").ConfigureAwait(false);
            await context.Output.WriteLineAsync().ConfigureAwait(false);

            var pack = await _loader.LoadPackAsync(packPath, context.CancellationToken).ConfigureAwait(false);
            var result = _validator.Validate(pack);

            if (result.IsValid)
            {
                await context.Output.WriteLineAsync($"  ✓ Pack '{pack.Id}' is valid").ConfigureAwait(false);
                await context.Output.WriteLineAsync($"  ✓ Version: {pack.Version}").ConfigureAwait(false);
                await context.Output.WriteLineAsync($"  ✓ Components: {pack.Components.Count}").ConfigureAwait(false);
                return ExitCode.Success;
            }
            else
            {
                await context.Output.WriteLineAsync($"  ✗ Pack validation failed").ConfigureAwait(false);
                await context.Output.WriteLineAsync().ConfigureAwait(false);
                await context.Output.WriteLineAsync("Errors:").ConfigureAwait(false);

                foreach (var error in result.Errors)
                {
                    var location = error.FilePath ?? "manifest";
                    await context.Output.WriteLineAsync($"  [{error.Code}] {location}: {error.Message}").ConfigureAwait(false);
                }

                return ExitCode.GeneralError;
            }
        }
        catch (PackLoadException ex)
        {
            await context.Output.WriteLineAsync($"Error: Failed to load pack: {ex.Message}").ConfigureAwait(false);
            return ExitCode.GeneralError;
        }
        catch (ManifestParseException ex)
        {
            await context.Output.WriteLineAsync($"Error: Invalid manifest: {ex.Message}").ConfigureAwait(false);
            return ExitCode.GeneralError;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }

    /// <summary>
    /// Refreshes the registry cache.
    /// AC-073.
    /// </summary>
    private async Task<ExitCode> ReloadAsync(CommandContext context)
    {
        try
        {
            _registry.Refresh();
            await context.Output.WriteLineAsync("Prompt pack registry reload complete.").ConfigureAwait(false);

            var packs = _registry.ListPacks();
            await context.Output.WriteLineAsync($"Discovered {packs.Count} pack(s).").ConfigureAwait(false);

            return ExitCode.Success;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: Failed to reload registry: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }
}
