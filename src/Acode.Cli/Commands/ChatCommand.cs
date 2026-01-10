// src/Acode.Cli/Commands/ChatCommand.cs
namespace Acode.Cli.Commands;

using System;
using System.Linq;
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

    // Subcommand implementations
    private async Task<ExitCode> NewAsync(CommandContext context)
    {
        // Parse arguments: acode chat new ["title"] [--auto-title]
        string? title = null;
        var autoTitle = false;

        // Check for flags and get title from remaining args
        for (var i = 1; i < context.Args.Length; i++)
        {
            if (context.Args[i] == "--auto-title")
            {
                autoTitle = true;
            }
            else if (title == null)
            {
                title = context.Args[i];
            }
        }

        // AC-002: Auto-generate title if not provided
        if (string.IsNullOrWhiteSpace(title))
        {
            title = autoTitle || title == null
                ? $"Chat {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}"
                : title;
        }

        try
        {
            // AC-003, AC-004, AC-005: Create chat (domain validation handles title rules)
            var chat = Domain.Conversation.Chat.Create(title);

            // AC-010, AC-011: IsDeleted=false, timestamps set by domain
            await _chatRepository.CreateAsync(chat, context.CancellationToken).ConfigureAwait(false);

            // Set as active chat
            await _sessionManager.SetActiveChatAsync(chat.Id, context.CancellationToken).ConfigureAwait(false);

            // AC-009: Human-readable success message
            await context.Output.WriteLineAsync($"Chat created: {chat.Id.Value}").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"Title: {chat.Title}").ConfigureAwait(false);

            // AC-001: Returns success
            return ExitCode.Success;
        }
        catch (ArgumentException ex)
        {
            // AC-005: Invalid title characters or length
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error creating chat: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }

    private Task<ExitCode> ListAsync(CommandContext context)
    {
        // TODO: Implement ListChatsCommand logic
        return Task.FromResult(ExitCode.RuntimeError);
    }

    private async Task<ExitCode> OpenAsync(CommandContext context)
    {
        // AC-029: acode chat open <id>
        if (context.Args.Length < 2)
        {
            await context.Output.WriteLineAsync("Error: Missing chat ID").ConfigureAwait(false);
            await context.Output.WriteLineAsync("Usage: acode chat open <id>").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var chatIdStr = context.Args[1];

        try
        {
            // AC-030, AC-031: Parse chat ID (full ULID or partial prefix)
            var chatId = Domain.Conversation.ChatId.From(chatIdStr);

            // AC-032: Check if chat exists and is not deleted
            var chat = await _chatRepository.GetByIdAsync(chatId, includeRuns: false, context.CancellationToken).ConfigureAwait(false);

            if (chat == null)
            {
                await context.Output.WriteLineAsync($"Error ACODE-CHAT-CMD-001: Chat '{chatIdStr}' not found").ConfigureAwait(false);
                return ExitCode.RuntimeError;
            }

            // AC-034: Cannot open soft-deleted chat
            if (chat.IsDeleted)
            {
                await context.Output.WriteLineAsync($"Error: Chat '{chatIdStr}' is deleted. Use 'acode chat restore {chatIdStr}' first.").ConfigureAwait(false);
                return ExitCode.RuntimeError;
            }

            // AC-035: Update session state
            await _sessionManager.SetActiveChatAsync(chat.Id, context.CancellationToken).ConfigureAwait(false);

            await context.Output.WriteLineAsync($"Opened chat: {chat.Id.Value}").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"Title: {chat.Title}").ConfigureAwait(false);

            // AC-036: Operation completes successfully
            return ExitCode.Success;
        }
        catch (FormatException)
        {
            await context.Output.WriteLineAsync($"Error: Invalid chat ID format '{chatIdStr}'").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }

    private Task<ExitCode> ShowAsync(CommandContext context)
    {
        // TODO: Implement ShowChatCommand logic
        return Task.FromResult(ExitCode.RuntimeError);
    }

    private async Task<ExitCode> RenameAsync(CommandContext context)
    {
        // AC-049: acode chat rename <id> "New Title"
        if (context.Args.Length < 3)
        {
            await context.Output.WriteLineAsync("Error: Missing chat ID or new title").ConfigureAwait(false);
            await context.Output.WriteLineAsync("Usage: acode chat rename <id> \"New Title\"").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var chatIdStr = context.Args[1];
        var newTitle = context.Args[2];

        try
        {
            var chatId = Domain.Conversation.ChatId.From(chatIdStr);

            // AC-053: Chat not found
            var chat = await _chatRepository.GetByIdAsync(chatId, includeRuns: false, context.CancellationToken).ConfigureAwait(false);

            if (chat == null)
            {
                await context.Output.WriteLineAsync($"Error ACODE-CHAT-CMD-001: Chat '{chatIdStr}' not found").ConfigureAwait(false);
                return ExitCode.RuntimeError;
            }

            var oldTitle = chat.Title;

            // AC-050: Validate new title (domain method handles this)
            // AC-055: Works on soft-deleted chats (UpdateTitle throws if deleted, so remove that check in domain or handle here)
            chat.UpdateTitle(newTitle);

            // AC-051, AC-052: UpdatedAt updated, other properties preserved
            await _chatRepository.UpdateAsync(chat, context.CancellationToken).ConfigureAwait(false);

            // AC-058: Display confirmation with old and new titles
            await context.Output.WriteLineAsync($"Chat renamed successfully").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"Old Title: {oldTitle}").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"New Title: {newTitle}").ConfigureAwait(false);

            return ExitCode.Success;
        }
        catch (FormatException)
        {
            await context.Output.WriteLineAsync($"Error: Invalid chat ID format '{chatIdStr}'").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }
        catch (ArgumentException ex)
        {
            // AC-054: Invalid title
            await context.Output.WriteLineAsync($"Error ACODE-CHAT-CMD-002: {ex.Message}").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }
        catch (InvalidOperationException ex)
        {
            // Chat is deleted
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }

    private async Task<ExitCode> DeleteAsync(CommandContext context)
    {
        // AC-059: acode chat delete <id>
        if (context.Args.Length < 2)
        {
            await context.Output.WriteLineAsync("Error: Missing chat ID").ConfigureAwait(false);
            await context.Output.WriteLineAsync("Usage: acode chat delete <id> [--force]").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var chatIdStr = context.Args[1];
        var force = context.Args.Contains("--force");

        try
        {
            var chatId = Domain.Conversation.ChatId.From(chatIdStr);

            // AC-064: Chat not found
            var chat = await _chatRepository.GetByIdAsync(chatId, includeRuns: false, context.CancellationToken).ConfigureAwait(false);

            if (chat == null)
            {
                await context.Output.WriteLineAsync($"Error ACODE-CHAT-CMD-001: Chat '{chatIdStr}' not found").ConfigureAwait(false);
                return ExitCode.RuntimeError;
            }

            // AC-066: Idempotent - already deleted
            if (chat.IsDeleted)
            {
                await context.Output.WriteLineAsync($"Chat '{chat.Title}' is already deleted").ConfigureAwait(false);
                return ExitCode.Success;
            }

            // AC-060, AC-061, AC-062: Confirmation prompt unless --force
            if (!force)
            {
                await context.Output.WriteLineAsync($"Are you sure you want to delete chat '{chat.Title}'? [y/N]").ConfigureAwait(false);

                // Note: In real CLI, would read from Console.ReadLine(), but for now we'll require --force
                // AC-065: Confirmation declined
                await context.Output.WriteLineAsync("Error ACODE-CHAT-CMD-003: Operation cancelled (use --force to skip confirmation)").ConfigureAwait(false);
                return ExitCode.UserCancellation;
            }

            // AC-059: Soft delete
            chat.Delete();

            // AC-067, AC-068, AC-069: Sets DeletedAt, preserves data, updates UpdatedAt
            await _chatRepository.UpdateAsync(chat, context.CancellationToken).ConfigureAwait(false);

            await context.Output.WriteLineAsync($"Chat '{chat.Title}' deleted successfully").ConfigureAwait(false);

            return ExitCode.Success;
        }
        catch (FormatException)
        {
            await context.Output.WriteLineAsync($"Error: Invalid chat ID format '{chatIdStr}'").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }

    private async Task<ExitCode> RestoreAsync(CommandContext context)
    {
        // AC-071: acode chat restore <id>
        if (context.Args.Length < 2)
        {
            await context.Output.WriteLineAsync("Error: Missing chat ID").ConfigureAwait(false);
            await context.Output.WriteLineAsync("Usage: acode chat restore <id>").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var chatIdStr = context.Args[1];

        try
        {
            var chatId = Domain.Conversation.ChatId.From(chatIdStr);

            // AC-074: Chat not found
            var chat = await _chatRepository.GetByIdAsync(chatId, includeRuns: false, context.CancellationToken).ConfigureAwait(false);

            if (chat == null)
            {
                await context.Output.WriteLineAsync($"Error ACODE-CHAT-CMD-001: Chat '{chatIdStr}' not found").ConfigureAwait(false);
                return ExitCode.RuntimeError;
            }

            // AC-075: Idempotent - already active
            if (!chat.IsDeleted)
            {
                await context.Output.WriteLineAsync($"Chat '{chat.Title}' is already active").ConfigureAwait(false);
                return ExitCode.Success;
            }

            // AC-071: Restore (clears IsDeleted, DeletedAt)
            chat.Restore();

            // AC-072, AC-073, AC-077: Clears DeletedAt, updates UpdatedAt, preserves data
            await _chatRepository.UpdateAsync(chat, context.CancellationToken).ConfigureAwait(false);

            await context.Output.WriteLineAsync($"Chat '{chat.Title}' restored successfully").ConfigureAwait(false);

            return ExitCode.Success;
        }
        catch (FormatException)
        {
            await context.Output.WriteLineAsync($"Error: Invalid chat ID format '{chatIdStr}'").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }
        catch (InvalidOperationException ex)
        {
            // Chat.Restore() throws if not deleted
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }

    private async Task<ExitCode> PurgeAsync(CommandContext context)
    {
        // AC-079: acode chat purge <id>
        if (context.Args.Length < 2)
        {
            await context.Output.WriteLineAsync("Error: Missing chat ID").ConfigureAwait(false);
            await context.Output.WriteLineAsync("Usage: acode chat purge <id> [--force]").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var chatIdStr = context.Args[1];
        var force = context.Args.Contains("--force");

        try
        {
            var chatId = Domain.Conversation.ChatId.From(chatIdStr);

            // AC-087: Chat not found
            var chat = await _chatRepository.GetByIdAsync(chatId, includeRuns: false, context.CancellationToken).ConfigureAwait(false);

            if (chat == null)
            {
                await context.Output.WriteLineAsync($"Error ACODE-CHAT-CMD-001: Chat '{chatIdStr}' not found").ConfigureAwait(false);
                return ExitCode.RuntimeError;
            }

            // AC-080, AC-081: Double confirmation unless --force
            if (!force)
            {
                await context.Output.WriteLineAsync($"WARNING: This will permanently delete chat '{chat.Title}' and all associated data.").ConfigureAwait(false);
                await context.Output.WriteLineAsync($"Type the chat ID to confirm permanent deletion: {chat.Id.Value}").ConfigureAwait(false);

                // Note: In real CLI, would read from Console.ReadLine() and compare
                // AC-088: Confirmation doesn't match
                await context.Output.WriteLineAsync("Error ACODE-CHAT-CMD-003: Operation cancelled (use --force to skip confirmation)").ConfigureAwait(false);
                return ExitCode.UserCancellation;
            }

            // Get run count for audit log
            var runs = await _runRepository.ListByChatAsync(chat.Id, context.CancellationToken).ConfigureAwait(false);

            // AC-083, AC-084, AC-085: Cascade delete messages → runs → chat
            // Note: SQLite foreign key CASCADE DELETE handles this automatically
            // But we'll do it explicitly for clarity and cross-database compatibility
            foreach (var run in runs)
            {
                await _messageRepository.DeleteByRunAsync(run.Id, context.CancellationToken).ConfigureAwait(false);
                await _runRepository.DeleteAsync(run.Id, context.CancellationToken).ConfigureAwait(false);
            }

            // AC-086: Remove chat record
            await _chatRepository.DeleteAsync(chat.Id, context.CancellationToken).ConfigureAwait(false);

            // AC-089: Log CRITICAL level audit entry (would use ILogger in real implementation)
            await context.Output.WriteLineAsync($"AUDIT: Chat permanently purged: {chat.Id.Value}, Title='{chat.Title}', RunCount={runs.Count}").ConfigureAwait(false);

            await context.Output.WriteLineAsync($"Chat '{chat.Title}' permanently deleted").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"Deleted {runs.Count} runs and associated messages").ConfigureAwait(false);

            // AC-093: Cannot be undone
            return ExitCode.Success;
        }
        catch (FormatException)
        {
            await context.Output.WriteLineAsync($"Error: Invalid chat ID format '{chatIdStr}'").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }

    private async Task<ExitCode> StatusAsync(CommandContext context)
    {
        // AC-095: Display current active chat details
        var activeChatId = await _sessionManager.GetActiveChatAsync(context.CancellationToken).ConfigureAwait(false);

        // AC-096: No active chat
        if (activeChatId == null)
        {
            await context.Output.WriteLineAsync("No active chat").ConfigureAwait(false);
            return ExitCode.GeneralError; // AC-102: Exit code 1 when no chat active
        }

        try
        {
            // activeChatId is guaranteed non-null here (checked above)
            var chat = await _chatRepository.GetByIdAsync(activeChatId.Value, includeRuns: false, context.CancellationToken).ConfigureAwait(false);

            if (chat == null)
            {
                await context.Output.WriteLineAsync($"Error: Active chat {activeChatId.Value} not found").ConfigureAwait(false);
                return ExitCode.RuntimeError;
            }

            // AC-097: Display chat ID, title, and worktree binding
            await context.Output.WriteLineAsync($"Active Chat: {chat.Id.Value}").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"Title: {chat.Title}").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"Worktree: {chat.WorktreeBinding?.Value ?? "none"}").ConfigureAwait(false);

            // AC-098, AC-099: Show run count, message count, last activity
            var runs = await _runRepository.ListByChatAsync(chat.Id, context.CancellationToken).ConfigureAwait(false);
            var messageCount = 0;
            foreach (var run in runs)
            {
                var messages = await _messageRepository.ListByRunAsync(run.Id, context.CancellationToken).ConfigureAwait(false);
                messageCount += messages.Count();
            }

            await context.Output.WriteLineAsync($"Runs: {runs.Count()}").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"Messages: {messageCount}").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"Last Activity: {chat.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC").ConfigureAwait(false);

            // AC-101: Exit code 0 when chat is active
            return ExitCode.Success;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }
}
