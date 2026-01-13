namespace Acode.Cli.Commands;

using System;
using System.Linq;
using System.Threading.Tasks;
using Acode.Application.Audit.Commands;
using Acode.Application.Audit.Queries;
using Acode.Domain.Audit;
using Acode.Infrastructure.Audit;

/// <summary>
/// Implements audit CLI commands for viewing, searching, and managing audit logs.
/// </summary>
public sealed class AuditCommand : ICommand
{
    private readonly ListSessionsQueryHandler? _listHandler;
    private readonly GetSessionEventsQueryHandler? _getEventsHandler;
    private readonly SearchEventsQueryHandler? _searchHandler;
    private readonly GetAuditStatsQueryHandler? _statsHandler;
    private readonly CleanupLogsCommandHandler? _cleanupHandler;
    private readonly AuditExporter? _exporter;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditCommand"/> class.
    /// </summary>
    /// <param name="listHandler">Handler for listing sessions.</param>
    /// <param name="getEventsHandler">Handler for getting session events.</param>
    /// <param name="searchHandler">Handler for searching events.</param>
    /// <param name="statsHandler">Handler for getting statistics.</param>
    /// <param name="cleanupHandler">Handler for cleanup operations.</param>
    /// <param name="exporter">Audit log exporter.</param>
    public AuditCommand(
        ListSessionsQueryHandler? listHandler = null,
        GetSessionEventsQueryHandler? getEventsHandler = null,
        SearchEventsQueryHandler? searchHandler = null,
        GetAuditStatsQueryHandler? statsHandler = null,
        CleanupLogsCommandHandler? cleanupHandler = null,
        AuditExporter? exporter = null)
    {
        _listHandler = listHandler;
        _getEventsHandler = getEventsHandler;
        _searchHandler = searchHandler;
        _statsHandler = statsHandler;
        _cleanupHandler = cleanupHandler;
        _exporter = exporter;
    }

    /// <inheritdoc/>
    public string Name => "audit";

    /// <inheritdoc/>
    public string[]? Aliases => null;

    /// <inheritdoc/>
    public string Description => "View and manage audit logs";

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Args.Length == 0)
        {
            await context.Output.WriteLineAsync("Error: Missing subcommand. Use 'acode audit help' for usage.").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var subcommand = context.Args[0];

        return subcommand.ToLowerInvariant() switch
        {
            "list" => await ListAsync(context).ConfigureAwait(false),
            "show" => await ShowAsync(context).ConfigureAwait(false),
            "search" => await SearchAsync(context).ConfigureAwait(false),
            "verify" => await VerifyAsync(context).ConfigureAwait(false),
            "export" => await ExportAsync(context).ConfigureAwait(false),
            "stats" => await StatsAsync(context).ConfigureAwait(false),
            "tail" => await TailAsync(context).ConfigureAwait(false),
            "cleanup" => await CleanupAsync(context).ConfigureAwait(false),
            "help" => await ShowHelpAsync(context).ConfigureAwait(false),
            _ => await WriteUnknownSubcommandAsync(context, subcommand).ConfigureAwait(false),
        };
    }

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"Usage: acode audit <subcommand> [options]

Subcommands:
  list        List audit sessions
  show        Show events for a specific session
  search      Search audit events by criteria
  verify      Verify audit log integrity
  export      Export logs to various formats
  stats       Show audit statistics
  tail        Follow audit log in real-time
  cleanup     Clean up old audit logs

Examples:
  acode audit list
  acode audit list --from 2026-01-01
  acode audit show <session-id>
  acode audit search --type CommandStart --severity Warning
  acode audit verify
  acode audit export --format json --output audit.json
  acode audit stats
  acode audit tail
  acode audit cleanup --retention-days 30

Options vary by subcommand. Use 'acode audit <subcommand> --help' for details.";
    }

    private async Task<ExitCode> ListAsync(CommandContext context)
    {
        if (_listHandler == null)
        {
            await context.Output.WriteLineAsync("Error: List handler not configured.").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }

        DateTimeOffset? fromDate = null;
        DateTimeOffset? toDate = null;

        // Parse optional --from and --to flags
        for (int i = 1; i < context.Args.Length; i++)
        {
            if (context.Args[i] == "--from"
                && i + 1 < context.Args.Length
                && DateTimeOffset.TryParse(context.Args[i + 1], out var parsedFrom))
            {
                fromDate = parsedFrom;
                i++;
            }
            else if (context.Args[i] == "--to"
                && i + 1 < context.Args.Length
                && DateTimeOffset.TryParse(context.Args[i + 1], out var parsedTo))
            {
                toDate = parsedTo;
                i++;
            }
        }

        var query = new ListSessionsQuery(fromDate, toDate);
        var sessions = await _listHandler.HandleAsync(query).ConfigureAwait(false);

        await context.Output.WriteLineAsync($"Found {sessions.Count} session(s):").ConfigureAwait(false);
        foreach (var session in sessions)
        {
            await context.Output.WriteLineAsync($"  {session.SessionId.Value} | {session.OperatingMode} | {session.StartedAt:yyyy-MM-dd HH:mm:ss}").ConfigureAwait(false);
        }

        return ExitCode.Success;
    }

    private async Task<ExitCode> ShowAsync(CommandContext context)
    {
        if (_getEventsHandler == null)
        {
            await context.Output.WriteLineAsync("Error: Get events handler not configured.").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }

        if (context.Args.Length < 2)
        {
            await context.Output.WriteLineAsync("Error: Missing session ID. Usage: acode audit show <session-id>").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var sessionIdStr = context.Args[1];
        var sessionId = new SessionId(sessionIdStr);
        var query = new GetSessionEventsQuery(sessionId);
        var events = await _getEventsHandler.HandleAsync(query).ConfigureAwait(false);

        await context.Output.WriteLineAsync($"Session {sessionId.Value} - {events.Count} event(s):").ConfigureAwait(false);
        foreach (var evt in events)
        {
            await context.Output.WriteLineAsync($"  [{evt.Timestamp:HH:mm:ss}] {evt.EventType} | {evt.Severity} | {evt.Source}").ConfigureAwait(false);
        }

        return ExitCode.Success;
    }

    private async Task<ExitCode> SearchAsync(CommandContext context)
    {
        if (_searchHandler == null)
        {
            await context.Output.WriteLineAsync("Error: Search handler not configured.").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }

        DateTimeOffset? fromDate = null;
        DateTimeOffset? toDate = null;
        AuditEventType? eventType = null;
        AuditSeverity? minSeverity = null;
        string? searchText = null;

        // Parse search flags
        for (int i = 1; i < context.Args.Length; i++)
        {
            if (context.Args[i] == "--from" && i + 1 < context.Args.Length)
            {
                if (DateTimeOffset.TryParse(context.Args[i + 1], out var parsed))
                {
                    fromDate = parsed;
                }

                i++;
            }
            else if (context.Args[i] == "--to" && i + 1 < context.Args.Length)
            {
                if (DateTimeOffset.TryParse(context.Args[i + 1], out var parsed))
                {
                    toDate = parsed;
                }

                i++;
            }
            else if (context.Args[i] == "--type" && i + 1 < context.Args.Length)
            {
                if (Enum.TryParse<AuditEventType>(context.Args[i + 1], true, out var parsed))
                {
                    eventType = parsed;
                }

                i++;
            }
            else if (context.Args[i] == "--severity" && i + 1 < context.Args.Length)
            {
                if (Enum.TryParse<AuditSeverity>(context.Args[i + 1], true, out var parsed))
                {
                    minSeverity = parsed;
                }

                i++;
            }
            else if (context.Args[i] == "--text" && i + 1 < context.Args.Length)
            {
                searchText = context.Args[i + 1];
                i++;
            }
        }

        var query = new SearchEventsQuery(fromDate, toDate, eventType, minSeverity, searchText);
        var events = await _searchHandler.HandleAsync(query).ConfigureAwait(false);

        await context.Output.WriteLineAsync($"Found {events.Count} matching event(s):").ConfigureAwait(false);
        foreach (var evt in events)
        {
            await context.Output.WriteLineAsync($"  [{evt.Timestamp:yyyy-MM-dd HH:mm:ss}] {evt.EventType} | {evt.Severity} | Session: {evt.SessionId.Value}").ConfigureAwait(false);
        }

        return ExitCode.Success;
    }

    private async Task<ExitCode> VerifyAsync(CommandContext context)
    {
        // CRITICAL: This is a placeholder - NOT a working integrity control
        // Returns RuntimeError to prevent automation from assuming logs are verified
        await context.Output.WriteLineAsync("ERROR: Audit log verification is not yet implemented.").ConfigureAwait(false);
        await context.Output.WriteLineAsync("This command performs NO integrity validation and MUST NOT be used as a security control.").ConfigureAwait(false);
        await context.Output.WriteLineAsync("Logs are NOT verified for tampering or corruption.").ConfigureAwait(false);
        return ExitCode.RuntimeError;
    }

    private async Task<ExitCode> ExportAsync(CommandContext context)
    {
        if (_exporter == null)
        {
            await context.Output.WriteLineAsync("Error: Exporter not configured.").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }

        var format = ExportFormat.Json; // Default
        string? outputPath = null;
        string logDirectory = ".acode/logs"; // Default

        // Parse export flags
        for (int i = 1; i < context.Args.Length; i++)
        {
            if (context.Args[i] == "--format" && i + 1 < context.Args.Length)
            {
                if (Enum.TryParse<ExportFormat>(context.Args[i + 1], true, out var parsed))
                {
                    format = parsed;
                }

                i++;
            }
            else if (context.Args[i] == "--output" && i + 1 < context.Args.Length)
            {
                outputPath = context.Args[i + 1];
                i++;
            }
            else if (context.Args[i] == "--log-dir" && i + 1 < context.Args.Length)
            {
                logDirectory = context.Args[i + 1];
                i++;
            }
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            await context.Output.WriteLineAsync("Error: Missing --output path.").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var options = new AuditExportOptions
        {
            Format = format,
        };

        await _exporter.ExportAsync(logDirectory, outputPath, options).ConfigureAwait(false);
        await context.Output.WriteLineAsync($"Exported audit logs to {outputPath} ({format} format)").ConfigureAwait(false);

        return ExitCode.Success;
    }

    private async Task<ExitCode> StatsAsync(CommandContext context)
    {
        if (_statsHandler == null)
        {
            await context.Output.WriteLineAsync("Error: Stats handler not configured.").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }

        var query = new GetAuditStatsQuery();
        var stats = await _statsHandler.HandleAsync(query).ConfigureAwait(false);

        await context.Output.WriteLineAsync("Audit Statistics:").ConfigureAwait(false);
        await context.Output.WriteLineAsync($"  Total Sessions: {stats.TotalSessions}").ConfigureAwait(false);
        await context.Output.WriteLineAsync($"  Total Events: {stats.TotalEvents}").ConfigureAwait(false);
        await context.Output.WriteLineAsync($"  Events by Type:").ConfigureAwait(false);
        foreach (var kvp in stats.EventsByType.OrderByDescending(x => x.Value))
        {
            await context.Output.WriteLineAsync($"    {kvp.Key}: {kvp.Value}").ConfigureAwait(false);
        }

        await context.Output.WriteLineAsync($"  Events by Severity:").ConfigureAwait(false);
        foreach (var kvp in stats.EventsBySeverity.OrderByDescending(x => x.Value))
        {
            await context.Output.WriteLineAsync($"    {kvp.Key}: {kvp.Value}").ConfigureAwait(false);
        }

        if (stats.OldestEventTimestamp.HasValue)
        {
            await context.Output.WriteLineAsync($"  Oldest Event: {stats.OldestEventTimestamp.Value:yyyy-MM-dd HH:mm:ss}").ConfigureAwait(false);
        }

        if (stats.NewestEventTimestamp.HasValue)
        {
            await context.Output.WriteLineAsync($"  Newest Event: {stats.NewestEventTimestamp.Value:yyyy-MM-dd HH:mm:ss}").ConfigureAwait(false);
        }

        return ExitCode.Success;
    }

    private async Task<ExitCode> TailAsync(CommandContext context)
    {
        // Placeholder for real-time log following
        await context.Output.WriteLineAsync("Real-time audit log tail not yet implemented.").ConfigureAwait(false);
        return ExitCode.Success;
    }

    private async Task<ExitCode> CleanupAsync(CommandContext context)
    {
        if (_cleanupHandler == null)
        {
            await context.Output.WriteLineAsync("Error: Cleanup handler not configured.").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }

        int retentionDays = 90; // Default
        string logDirectory = ".acode/logs"; // Default

        // Parse cleanup flags
        for (int i = 1; i < context.Args.Length; i++)
        {
            if (context.Args[i] == "--retention-days" && i + 1 < context.Args.Length)
            {
                if (int.TryParse(context.Args[i + 1], out var parsed))
                {
                    retentionDays = parsed;
                }

                i++;
            }
            else if (context.Args[i] == "--log-dir" && i + 1 < context.Args.Length)
            {
                logDirectory = context.Args[i + 1];
                i++;
            }
        }

        var command = new CleanupLogsCommand(logDirectory, retentionDays);
        var result = await _cleanupHandler.HandleAsync(command).ConfigureAwait(false);

        await context.Output.WriteLineAsync($"Cleanup complete:").ConfigureAwait(false);
        await context.Output.WriteLineAsync($"  Files deleted: {result.FilesDeleted}").ConfigureAwait(false);
        await context.Output.WriteLineAsync($"  Bytes freed: {result.BytesFreed:N0}").ConfigureAwait(false);

        return ExitCode.Success;
    }

    private async Task<ExitCode> ShowHelpAsync(CommandContext context)
    {
        await context.Output.WriteLineAsync(GetHelp()).ConfigureAwait(false);
        return ExitCode.Success;
    }

    private async Task<ExitCode> WriteUnknownSubcommandAsync(CommandContext context, string subcommand)
    {
        await context.Output.WriteLineAsync($"Error: Unknown subcommand '{subcommand}'. Use 'acode audit help' for usage.").ConfigureAwait(false);
        return ExitCode.InvalidArguments;
    }
}
