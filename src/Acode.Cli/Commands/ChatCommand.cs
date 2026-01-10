// src/Acode.Cli/Commands/ChatCommand.cs
namespace Acode.Cli.Commands;

using System;
using System.Threading.Tasks;
using Acode.Application.Conversation.Persistence;
using Acode.Application.Conversation.Session;

/// <summary>
/// Implements chat management CLI commands (CRUSD operations).
/// Routes to subcommands: new, list, open, show, rename, delete, restore, purge, status.
/// </summary>
public sealed class ChatCommand : ICommand
{
    private readonly IChatRepository _chatRepository;
    private readonly IRunRepository _runRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ISessionManager _sessionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatCommand"/> class.
    /// </summary>
    /// <param name="chatRepository">Chat repository.</param>
    /// <param name="runRepository">Run repository.</param>
    /// <param name="messageRepository">Message repository.</param>
    /// <param name="sessionManager">Session manager.</param>
    public ChatCommand(
        IChatRepository chatRepository,
        IRunRepository runRepository,
        IMessageRepository messageRepository,
        ISessionManager sessionManager)
    {
        _chatRepository = chatRepository ?? throw new ArgumentNullException(nameof(chatRepository));
        _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    }

    /// <inheritdoc/>
    public string Name => "chat";

    /// <inheritdoc/>
    public string[]? Aliases => null;

    /// <inheritdoc/>
    public string Description => "Manage conversation chats";

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Args.Length == 0)
        {
            await context.Output.WriteLineAsync("Error: Missing subcommand.").ConfigureAwait(false);
            await context.Output.WriteLineAsync("Use 'acode chat --help' to see available subcommands.").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var subcommand = context.Args[0].ToLowerInvariant();

        return subcommand switch
        {
            "new" or "create" => await NewAsync(context).ConfigureAwait(false),
            "list" or "ls" => await ListAsync(context).ConfigureAwait(false),
            "open" => await OpenAsync(context).ConfigureAwait(false),
            "show" => await ShowAsync(context).ConfigureAwait(false),
            "rename" => await RenameAsync(context).ConfigureAwait(false),
            "delete" or "del" => await DeleteAsync(context).ConfigureAwait(false),
            "restore" => await RestoreAsync(context).ConfigureAwait(false),
            "purge" => await PurgeAsync(context).ConfigureAwait(false),
            "status" => await StatusAsync(context).ConfigureAwait(false),
            "--help" or "-h" or "help" => await WriteHelpAsync(context).ConfigureAwait(false),
            _ => await WriteUnknownSubcommandAsync(context, subcommand).ConfigureAwait(false),
        };
    }

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"Usage: acode chat <subcommand> [options]

Subcommands:
  new <title>         Create a new chat with the specified title
  list                List all active chats
  open <id>           Open and set a chat as active
  show <id>           Display detailed chat information
  rename <id> <title> Rename a chat
  delete <id>         Soft-delete a chat (recoverable)
  restore <id>        Restore a soft-deleted chat
  purge <id>          Permanently delete a chat (irreversible)
  status              Show current active chat

Examples:
  acode chat new ""Implement feature X""
  acode chat list
  acode chat list --archived
  acode chat open 01HQABC123
  acode chat show 01HQABC123
  acode chat rename 01HQABC123 ""New Title""
  acode chat delete 01HQABC123
  acode chat restore 01HQABC123
  acode chat purge 01HQABC123 --force
  acode chat status";
    }

    private static async Task<ExitCode> WriteUnknownSubcommandAsync(CommandContext context, string subcommand)
    {
        await context.Output.WriteLineAsync($"Error: Unknown subcommand '{subcommand}'.").ConfigureAwait(false);
        await context.Output.WriteLineAsync("Use 'acode chat --help' to see available subcommands.").ConfigureAwait(false);
        return ExitCode.InvalidArguments;
    }

    private async Task<ExitCode> WriteHelpAsync(CommandContext context)
    {
        await context.Output.WriteLineAsync(GetHelp()).ConfigureAwait(false);
        return ExitCode.Success;
    }

    // Subcommand implementations to be added
    private Task<ExitCode> NewAsync(CommandContext context)
    {
        // TODO: Implement NewChatCommand logic
        return Task.FromResult(ExitCode.RuntimeError);
    }

    private Task<ExitCode> ListAsync(CommandContext context)
    {
        // TODO: Implement ListChatsCommand logic
        return Task.FromResult(ExitCode.RuntimeError);
    }

    private Task<ExitCode> OpenAsync(CommandContext context)
    {
        // TODO: Implement OpenChatCommand logic
        return Task.FromResult(ExitCode.RuntimeError);
    }

    private Task<ExitCode> ShowAsync(CommandContext context)
    {
        // TODO: Implement ShowChatCommand logic
        return Task.FromResult(ExitCode.RuntimeError);
    }

    private Task<ExitCode> RenameAsync(CommandContext context)
    {
        // TODO: Implement RenameChatCommand logic
        return Task.FromResult(ExitCode.RuntimeError);
    }

    private Task<ExitCode> DeleteAsync(CommandContext context)
    {
        // TODO: Implement DeleteChatCommand logic
        return Task.FromResult(ExitCode.RuntimeError);
    }

    private Task<ExitCode> RestoreAsync(CommandContext context)
    {
        // TODO: Implement RestoreChatCommand logic
        return Task.FromResult(ExitCode.RuntimeError);
    }

    private Task<ExitCode> PurgeAsync(CommandContext context)
    {
        // TODO: Implement PurgeChatCommand logic
        return Task.FromResult(ExitCode.RuntimeError);
    }

    private Task<ExitCode> StatusAsync(CommandContext context)
    {
        // TODO: Implement StatusChatCommand logic
        return Task.FromResult(ExitCode.RuntimeError);
    }
}
