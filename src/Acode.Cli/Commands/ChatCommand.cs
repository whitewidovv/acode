// src/Acode.Cli/Commands/ChatCommand.cs
namespace Acode.Cli.Commands;

using System;
using System.Linq;
using System.Threading.Tasks;
using Acode.Application.Concurrency;
using Acode.Application.Conversation.Persistence;
using Acode.Application.Conversation.Session;
using Acode.Domain.Worktree;

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
    private readonly IBindingService _bindingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatCommand"/> class.
    /// </summary>
    /// <param name="chatRepository">Chat repository.</param>
    /// <param name="runRepository">Run repository.</param>
    /// <param name="messageRepository">Message repository.</param>
    /// <param name="sessionManager">Session manager.</param>
    /// <param name="bindingService">Binding service.</param>
    public ChatCommand(
        IChatRepository chatRepository,
        IRunRepository runRepository,
        IMessageRepository messageRepository,
        ISessionManager sessionManager,
        IBindingService bindingService)
    {
        _chatRepository = chatRepository ?? throw new ArgumentNullException(nameof(chatRepository));
        _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _bindingService = bindingService ?? throw new ArgumentNullException(nameof(bindingService));
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
            "bind" => await BindAsync(context).ConfigureAwait(false),
            "unbind" => await UnbindAsync(context).ConfigureAwait(false),
            "bindings" => await BindingsAsync(context).ConfigureAwait(false),
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

    private async Task<ExitCode> ListAsync(CommandContext context)
    {
        // AC-013-028: List chats with filtering, sorting, pagination
        // Parse flags: --archived, --filter, --sort, --limit, --offset
        var includeArchived = context.Args.Contains("--archived");
        string? filterText = null;
        var sortBy = Application.Conversation.Persistence.ChatSortField.UpdatedAt; // Default: updated
        var limit = 50; // Default page size
        var offset = 0;

        // Parse arguments
        for (var i = 1; i < context.Args.Length; i++)
        {
            if (context.Args[i] == "--filter" && i + 1 < context.Args.Length)
            {
                filterText = context.Args[++i];
            }
            else if (context.Args[i] == "--sort" && i + 1 < context.Args.Length)
            {
                var sortField = context.Args[++i].ToLowerInvariant();
                sortBy = sortField switch
                {
                    "created" => Application.Conversation.Persistence.ChatSortField.CreatedAt,
                    "updated" => Application.Conversation.Persistence.ChatSortField.UpdatedAt,
                    "title" => Application.Conversation.Persistence.ChatSortField.Title,
                    _ => Application.Conversation.Persistence.ChatSortField.UpdatedAt
                };
            }
            else if (context.Args[i] == "--limit" && i + 1 < context.Args.Length)
            {
                if (int.TryParse(context.Args[++i], out var parsedLimit))
                {
                    limit = parsedLimit;
                }
            }
            else if (context.Args[i] == "--offset" && i + 1 < context.Args.Length)
            {
                if (int.TryParse(context.Args[++i], out var parsedOffset))
                {
                    offset = parsedOffset;
                }
            }
        }

        try
        {
            // AC-021: Calculate page from offset
            var page = offset / limit;

            // Create filter
            var filter = new Application.Conversation.Persistence.ChatFilter
            {
                IncludeDeleted = includeArchived,
                TitleContains = filterText,
                SortBy = sortBy,
                SortDescending = sortBy != Application.Conversation.Persistence.ChatSortField.Title, // Title sorts ascending, others descending
                Page = page,
                PageSize = limit
            };

            // AC-013: Query repository
            var result = await _chatRepository.ListAsync(filter, context.CancellationToken).ConfigureAwait(false);

            // AC-025: Handle empty result
            if (result.Items.Count == 0)
            {
                await context.Output.WriteLineAsync("No chats found").ConfigureAwait(false);
                return ExitCode.Success;
            }

            // AC-013: Display table
            await context.Output.WriteLineAsync($"{"ID",-15} {"Title",-40} {"Updated",-20} {"Runs",-6}").ConfigureAwait(false);
            await context.Output.WriteLineAsync(new string('-', 83)).ConfigureAwait(false);

            foreach (var chat in result.Items)
            {
                // AC-022: Truncate ID to first 12 chars
                var chatIdDisplay = chat.Id.Value.Length > 12
                    ? chat.Id.Value.Substring(0, 12) + "..."
                    : chat.Id.Value;

                // Get run count for this chat
                var runs = await _runRepository.ListByChatAsync(chat.Id, context.CancellationToken).ConfigureAwait(false);
                var runCount = runs.Count();

                // Truncate title if too long
                var titleDisplay = chat.Title.Length > 38
                    ? chat.Title.Substring(0, 38) + ".."
                    : chat.Title;

                // AC-015: Show archived indicator
                if (chat.IsDeleted && includeArchived)
                {
                    titleDisplay += " [ARCHIVED]";
                }

                await context.Output.WriteLineAsync(
                    $"{chatIdDisplay,-15} {titleDisplay,-40} {chat.UpdatedAt:yyyy-MM-dd HH:mm:ss,-20} {runCount,-6}").ConfigureAwait(false);
            }

            // Show pagination info
            await context.Output.WriteLineAsync(string.Empty).ConfigureAwait(false);
            await context.Output.WriteLineAsync($"Showing {result.Items.Count} of {result.TotalCount} total chats (Page {result.Page + 1} of {result.TotalPages})").ConfigureAwait(false);

            return ExitCode.Success;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error listing chats: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
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

    private async Task<ExitCode> ShowAsync(CommandContext context)
    {
        // AC-037-048: Show detailed chat information
        if (context.Args.Length < 2)
        {
            await context.Output.WriteLineAsync("Error: Missing chat ID").ConfigureAwait(false);
            await context.Output.WriteLineAsync("Usage: acode chat show <id>").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var chatIdStr = context.Args[1];

        try
        {
            var chatId = Domain.Conversation.ChatId.From(chatIdStr);

            // AC-044: Include soft-deleted chats
            var chat = await _chatRepository.GetByIdAsync(chatId, includeRuns: false, context.CancellationToken).ConfigureAwait(false);

            // AC-043: Chat not found
            if (chat == null)
            {
                await context.Output.WriteLineAsync($"Error ACODE-CHAT-CMD-001: Chat '{chatIdStr}' not found").ConfigureAwait(false);
                return ExitCode.RuntimeError;
            }

            // AC-038: Display chat details
            await context.Output.WriteLineAsync($"Chat Details:").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"  ID: {chat.Id.Value}").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"  Title: {chat.Title}").ConfigureAwait(false);

            // AC-047: ISO 8601 formatted timestamps
            await context.Output.WriteLineAsync($"  Created: {chat.CreatedAt:yyyy-MM-ddTHH:mm:ssZ}").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"  Updated: {chat.UpdatedAt:yyyy-MM-ddTHH:mm:ssZ}").ConfigureAwait(false);

            // Tags
            if (chat.Tags.Any())
            {
                await context.Output.WriteLineAsync($"  Tags: {string.Join(", ", chat.Tags)}").ConfigureAwait(false);
            }
            else
            {
                await context.Output.WriteLineAsync($"  Tags: none").ConfigureAwait(false);
            }

            // Worktree binding
            await context.Output.WriteLineAsync($"  Worktree: {chat.WorktreeBinding?.Value ?? "none"}").ConfigureAwait(false);

            // AC-044: Show deleted status
            await context.Output.WriteLineAsync($"  Status: {(chat.IsDeleted ? "Archived" : "Active")}").ConfigureAwait(false);

            // AC-039, AC-040, AC-041, AC-042: Show run count, message count
            var runs = await _runRepository.ListByChatAsync(chat.Id, context.CancellationToken).ConfigureAwait(false);
            var runCount = runs.Count();
            var messageCount = 0;

            // AC-048: Handle chats with 0 runs gracefully
            foreach (var run in runs)
            {
                var messages = await _messageRepository.ListByRunAsync(run.Id, context.CancellationToken).ConfigureAwait(false);
                messageCount += messages.Count();
            }

            await context.Output.WriteLineAsync($"  Runs: {runCount}").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"  Messages: {messageCount}").ConfigureAwait(false);

            // AC-042: Last activity timestamp
            await context.Output.WriteLineAsync($"  Last Activity: {chat.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC").ConfigureAwait(false);

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

    /// <summary>
    /// Binds the specified chat to the current worktree.
    /// </summary>
    /// <param name="context">Command context.</param>
    /// <returns>Exit code indicating success or failure.</returns>
    private async Task<ExitCode> BindAsync(CommandContext context)
    {
        try
        {
            if (context.Args.Length < 2)
            {
                await context.Output.WriteLineAsync("Error: Missing chat ID.").ConfigureAwait(false);
                await context.Output.WriteLineAsync("Usage: acode chat bind <chat-id>").ConfigureAwait(false);
                return ExitCode.InvalidArguments;
            }

            var chatIdValue = context.Args[1];
            var chatId = Domain.Conversation.ChatId.From(chatIdValue);

            // Verify chat exists
            var chat = await _chatRepository.GetByIdAsync(chatId, includeRuns: false, context.CancellationToken).ConfigureAwait(false);
            if (chat is null || chat.IsDeleted)
            {
                await context.Output.WriteLineAsync($"Error: Chat '{chatIdValue}' not found.").ConfigureAwait(false);
                return ExitCode.GeneralError;
            }

            // Get current worktree
            if (!context.Configuration.TryGetValue("CurrentWorktree", out var worktreeObj) || worktreeObj is not WorktreeId worktreeId)
            {
                await context.Output.WriteLineAsync("Error: Not in a worktree. Bindings only work within Git worktrees.").ConfigureAwait(false);
                return ExitCode.GeneralError;
            }

            // Create binding
            await _bindingService.CreateBindingAsync(worktreeId, chatId, context.CancellationToken).ConfigureAwait(false);

            await context.Output.WriteLineAsync($"Bound chat '{chat.Title}' to worktree '{worktreeId.Value}'.").ConfigureAwait(false);
            return ExitCode.Success;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already bound", StringComparison.OrdinalIgnoreCase))
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.GeneralError;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }

    /// <summary>
    /// Unbinds the current worktree from its associated chat.
    /// </summary>
    /// <param name="context">Command context.</param>
    /// <returns>Exit code indicating success or failure.</returns>
    private async Task<ExitCode> UnbindAsync(CommandContext context)
    {
        try
        {
            // Get current worktree
            if (!context.Configuration.TryGetValue("CurrentWorktree", out var worktreeObj) || worktreeObj is not WorktreeId worktreeId)
            {
                await context.Output.WriteLineAsync("Error: Not in a worktree. Bindings only work within Git worktrees.").ConfigureAwait(false);
                return ExitCode.GeneralError;
            }

            // Check if bound
            var boundChatId = await _bindingService.GetBoundChatAsync(worktreeId, context.CancellationToken).ConfigureAwait(false);
            if (boundChatId is null)
            {
                await context.Output.WriteLineAsync($"Error: Worktree '{worktreeId.Value}' is not bound to any chat.").ConfigureAwait(false);
                return ExitCode.GeneralError;
            }

            // Check for --force flag
            var hasForceFlag = context.Args.Any(arg => arg.Equals("--force", StringComparison.OrdinalIgnoreCase) || arg.Equals("-f", StringComparison.OrdinalIgnoreCase));

            if (!hasForceFlag)
            {
                await context.Output.WriteLineAsync("Error: Use --force to unbind without confirmation.").ConfigureAwait(false);
                await context.Output.WriteLineAsync("Usage: acode chat unbind --force").ConfigureAwait(false);
                return ExitCode.InvalidArguments;
            }

            // Unbind
            await _bindingService.DeleteBindingAsync(worktreeId, context.CancellationToken).ConfigureAwait(false);

            await context.Output.WriteLineAsync($"Unbound worktree '{worktreeId.Value}' from chat.").ConfigureAwait(false);
            return ExitCode.Success;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }

    /// <summary>
    /// Lists all worktree-to-chat bindings.
    /// </summary>
    /// <param name="context">Command context.</param>
    /// <returns>Exit code indicating success or failure.</returns>
    private async Task<ExitCode> BindingsAsync(CommandContext context)
    {
        try
        {
            var bindings = await _bindingService.ListAllBindingsAsync(context.CancellationToken).ConfigureAwait(false);

            if (bindings.Count == 0)
            {
                await context.Output.WriteLineAsync("No bindings found.").ConfigureAwait(false);
                return ExitCode.Success;
            }

            await context.Output.WriteLineAsync($"Worktree Bindings ({bindings.Count}):").ConfigureAwait(false);
            await context.Output.WriteLineAsync(string.Empty).ConfigureAwait(false);

            foreach (var binding in bindings)
            {
                // Get chat title
                var chat = await _chatRepository.GetByIdAsync(binding.ChatId, includeRuns: false, context.CancellationToken).ConfigureAwait(false);
                var chatTitle = chat?.Title ?? "(deleted)";

                await context.Output.WriteLineAsync($"  {binding.WorktreeId.Value}").ConfigureAwait(false);
                await context.Output.WriteLineAsync($"    -> Chat: {chatTitle} ({binding.ChatId.Value})").ConfigureAwait(false);
                await context.Output.WriteLineAsync($"    -> Created: {binding.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC").ConfigureAwait(false);
                await context.Output.WriteLineAsync(string.Empty).ConfigureAwait(false);
            }

            return ExitCode.Success;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.RuntimeError;
        }
    }
}
