using Acode.Domain.Modes;

namespace Acode.Cli.Commands;

/// <summary>
/// Command that displays the mode capability matrix.
/// </summary>
/// <remarks>
/// Per Task 001.a, provides CLI access to view the mode matrix with filtering and formatting options.
/// Shows what capabilities are allowed/denied in each operating mode.
/// </remarks>
public sealed class ConfigMatrixCommand : ICommand
{
    /// <inheritdoc/>
    public string Name => "matrix";

    /// <inheritdoc/>
    public string[] Aliases => Array.Empty<string>();

    /// <inheritdoc/>
    public string Description => "Display the mode capability matrix";

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Parse options from args
        var mode = ParseModeOption(context.Args);
        var capability = ParseCapabilityOption(context.Args);
        var format = ParseFormatOption(context.Args);

        try
        {
            string output;

            if (format == "json")
            {
                output = MatrixExporter.ToJson();
            }
            else if (capability.HasValue)
            {
                output = MatrixExporter.ToCapabilityComparison(capability.Value);
            }
            else
            {
                output = MatrixExporter.ToMarkdownTable(mode);
            }

            context.Formatter.WriteMessage(output);
            await Task.CompletedTask.ConfigureAwait(false);
            return ExitCode.Success;
        }
        catch (Exception ex)
        {
            context.Formatter.WriteError($"Error displaying matrix: {ex.Message}");
            return ExitCode.InternalError;
        }
    }

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"Usage: acode matrix [options]

Display the mode capability matrix showing what capabilities are allowed/denied in each operating mode.

Options:
  --mode <mode>          Filter by operating mode (LocalOnly, Burst, Airgapped)
  --capability <cap>     Show how a capability varies across modes
  --format <format>      Output format: table (default), json

Examples:
  acode matrix
    Display full matrix in table format

  acode matrix --mode LocalOnly
    Display only LocalOnly mode entries

  acode matrix --capability OpenAiApi
    Show OpenAiApi permission across all modes

  acode matrix --format json
    Export full matrix as JSON

The matrix defines what capabilities are Allowed, Denied, or Conditional in each mode.
Use this to understand operating mode constraints before running tasks.

Related commands:
  acode config     View/modify configuration
  acode security   View security policies
";
    }

    private static OperatingMode? ParseModeOption(string[] args)
    {
        var index = Array.IndexOf(args, "--mode");
        if (index >= 0 && index + 1 < args.Length)
        {
            if (Enum.TryParse<OperatingMode>(args[index + 1], ignoreCase: true, out var mode))
                return mode;
        }
        return null;
    }

    private static Capability? ParseCapabilityOption(string[] args)
    {
        var index = Array.IndexOf(args, "--capability");
        if (index >= 0 && index + 1 < args.Length)
        {
            if (Enum.TryParse<Capability>(args[index + 1], ignoreCase: true, out var capability))
                return capability;
        }
        return null;
    }

    private static string ParseFormatOption(string[] args)
    {
        var index = Array.IndexOf(args, "--format");
        if (index >= 0 && index + 1 < args.Length)
        {
            var format = args[index + 1].ToLowerInvariant();
            if (format == "json" || format == "table")
                return format;
        }
        return "table";
    }
}
