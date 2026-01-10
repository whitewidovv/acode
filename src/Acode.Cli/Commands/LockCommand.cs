// src/Acode.Cli/Commands/LockCommand.cs
#pragma warning disable CA2007 // CLI commands are UI layer, ConfigureAwait(false) not needed

namespace Acode.Cli.Commands;

using System;
using System.Threading.Tasks;
using Acode.Application.Concurrency;
using Acode.Domain.Worktree;

/// <summary>
/// CLI command for worktree lock management.
/// Provides status, unlock, and cleanup operations for concurrency control.
/// </summary>
public sealed class LockCommand : ICommand
{
    private readonly ILockService _lockService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LockCommand"/> class.
    /// </summary>
    /// <param name="lockService">The lock service for worktree lock management.</param>
    public LockCommand(ILockService lockService)
    {
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
    }

    /// <inheritdoc/>
    public string Name => "lock";

    /// <inheritdoc/>
    public string[]? Aliases => null;

    /// <inheritdoc/>
    public string Description => "Manage worktree locks for concurrency control";

    /// <inheritdoc/>
    public string GetHelp()
    {
        return """
            acode lock - Manage worktree locks for concurrency control

            Usage:
              acode lock status          Show lock status for current worktree
              acode lock unlock --force  Force-remove lock (emergency use)
              acode lock cleanup         Remove all stale locks (>5 minutes old)

            Examples:
              # Check if current worktree is locked
              acode lock status

              # Force-unlock after process crash
              acode lock unlock --force

              # Clean up abandoned locks
              acode lock cleanup

            Related commands:
              acode chat bind    Bind chat to worktree
              acode run          Execute agent with automatic locking
            """;
    }

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Args.Length == 0)
        {
            await context.Output.WriteLineAsync("Usage: acode lock <subcommand>");
            await context.Output.WriteLineAsync(string.Empty);
            await context.Output.WriteLineAsync("Subcommands:");
            await context.Output.WriteLineAsync("  status   - Show lock status for current worktree");
            await context.Output.WriteLineAsync("  unlock   - Force-remove lock (requires --force)");
            await context.Output.WriteLineAsync("  cleanup  - Remove all stale locks");
            return ExitCode.InvalidArguments;
        }

        var subcommand = context.Args[0].ToLowerInvariant();

        return subcommand switch
        {
            "status" => await StatusAsync(context),
            "unlock" => await UnlockAsync(context),
            "cleanup" => await CleanupAsync(context),
            _ => await HandleUnknownSubcommandAsync(context, subcommand),
        };
    }

    private static async Task<ExitCode> HandleUnknownSubcommandAsync(CommandContext context, string subcommand)
    {
        await context.Output.WriteLineAsync($"Error: Unknown subcommand '{subcommand}'.");
        await context.Output.WriteLineAsync("Run 'acode lock' for usage.");
        return ExitCode.InvalidArguments;
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalMinutes < 1)
        {
            return $"{(int)age.TotalSeconds}s";
        }

        if (age.TotalHours < 1)
        {
            return $"{(int)age.TotalMinutes}m {age.Seconds}s";
        }

        return $"{(int)age.TotalHours}h {age.Minutes}m";
    }

    private async Task<ExitCode> StatusAsync(CommandContext context)
    {
        // Get current worktree from context
        if (!context.Configuration.TryGetValue("CurrentWorktree", out var worktreeObj) || worktreeObj is not WorktreeId worktreeId)
        {
            await context.Output.WriteLineAsync("Error: Not in a worktree. Lock commands require a Git worktree context.");
            return ExitCode.GeneralError;
        }

        // Get lock status
        var status = await _lockService.GetStatusAsync(worktreeId, context.CancellationToken);

        if (!status.IsLocked)
        {
            await context.Output.WriteLineAsync($"Worktree '{worktreeId.Value}' is not locked.");
            return ExitCode.Success;
        }

        // Display lock details
        await context.Output.WriteLineAsync($"Worktree '{worktreeId.Value}' is locked:");
        await context.Output.WriteLineAsync($"  Process ID: {status.ProcessId}");
        await context.Output.WriteLineAsync($"  Hostname: {status.Hostname}");
        await context.Output.WriteLineAsync($"  Age: {FormatAge(status.Age)}");

        if (status.IsStale)
        {
            await context.Output.WriteLineAsync("  Status: STALE (process may have crashed)");
        }

        return ExitCode.Success;
    }

    private async Task<ExitCode> UnlockAsync(CommandContext context)
    {
        // Get current worktree from context
        if (!context.Configuration.TryGetValue("CurrentWorktree", out var worktreeObj) || worktreeObj is not WorktreeId worktreeId)
        {
            await context.Output.WriteLineAsync("Error: Not in a worktree. Lock commands require a Git worktree context.");
            return ExitCode.GeneralError;
        }

        // Check for --force flag
        var hasForceFlag = Array.Exists(context.Args, arg =>
            arg.Equals("--force", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-f", StringComparison.OrdinalIgnoreCase));

        if (!hasForceFlag)
        {
            await context.Output.WriteLineAsync("Error: Use --force to confirm force-unlock.");
            await context.Output.WriteLineAsync("WARNING: Force-unlock should only be used in emergencies (crashed processes).");
            return ExitCode.InvalidArguments;
        }

        // Force unlock
        await _lockService.ForceUnlockAsync(worktreeId, context.CancellationToken);

        await context.Output.WriteLineAsync($"Worktree '{worktreeId.Value}' unlocked.");
        return ExitCode.Success;
    }

    private async Task<ExitCode> CleanupAsync(CommandContext context)
    {
        // Cleanup stale locks (5 minutes threshold per spec)
        await _lockService.ReleaseStaleLocksAsync(
            TimeSpan.FromMinutes(5),
            context.CancellationToken);

        await context.Output.WriteLineAsync("Stale locks cleaned up (threshold: 5 minutes).");
        return ExitCode.Success;
    }
}
