// src/Acode.Cli/Commands/SearchCommand.cs
namespace Acode.Cli.Commands;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Interfaces;
using Acode.Domain.Conversation;
using Acode.Domain.Models.Inference;
using Acode.Domain.Search;

/// <summary>
/// Implements search command for full-text search over conversation history.
/// </summary>
public sealed class SearchCommand : ICommand
{
    private readonly ISearchService _searchService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchCommand"/> class.
    /// </summary>
    /// <param name="searchService">Search service.</param>
    public SearchCommand(ISearchService searchService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
    }

    /// <inheritdoc/>
    public string Name => "search";

    /// <inheritdoc/>
    public string[]? Aliases => new[] { "find", "query" };

    /// <inheritdoc/>
    public string Description => "Search conversation history";

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"
USAGE:
    acode search <query> [options]

DESCRIPTION:
    Search conversation history using full-text search with filters.

OPTIONS:
    --chat <ID>        Filter by chat ID (GUID)
    --since <DATE>     Filter messages after date (ISO 8601 format)
    --until <DATE>     Filter messages before date (ISO 8601 format)
    --role <ROLE>      Filter by message role (user|assistant|system)
    --page-size <N>    Results per page (1-100, default: 20)
    --page <N>         Page number (default: 1)
    --json             Output results as JSON

EXAMPLES:
    # Search for 'bug fix' in all conversations
    acode search ""bug fix""

    # Search within a specific chat
    acode search ""error"" --chat 3fa85f64-5717-4562-b3fc-2c963f66afa6

    # Search for recent user messages
    acode search ""question"" --role user --since 2026-01-01

    # Get JSON output for programmatic processing
    acode search ""api"" --json

    # Search with pagination
    acode search ""test"" --page-size 10 --page 2

RELATED COMMANDS:
    acode chat list    List all chats
    acode chat show    Show chat details
";
    }

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Args.Length == 0)
        {
            await context.Output.WriteLineAsync("Error: Missing search query.").ConfigureAwait(false);
            await context.Output.WriteLineAsync("Usage: acode search <query> [options]").ConfigureAwait(false);
            await context.Output.WriteLineAsync("  Options:").ConfigureAwait(false);
            await context.Output.WriteLineAsync("    --chat <ID>        Filter by chat ID").ConfigureAwait(false);
            await context.Output.WriteLineAsync("    --since <DATE>     Filter messages after date").ConfigureAwait(false);
            await context.Output.WriteLineAsync("    --until <DATE>     Filter messages before date").ConfigureAwait(false);
            await context.Output.WriteLineAsync("    --role <ROLE>      Filter by role (user|assistant|system)").ConfigureAwait(false);
            await context.Output.WriteLineAsync("    --page-size <N>    Results per page (default: 20)").ConfigureAwait(false);
            await context.Output.WriteLineAsync("    --page <N>         Page number (default: 1)").ConfigureAwait(false);
            await context.Output.WriteLineAsync("    --json             Output as JSON").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        // Parse query text (first non-option argument)
        var queryText = context.Args[0];

        // Parse options
        ChatId? chatId = null;
        DateTime? since = null;
        DateTime? until = null;
        MessageRole? roleFilter = null;
        var pageSize = 20;
        var pageNumber = 1;
        var jsonOutput = false;

        for (int i = 1; i < context.Args.Length; i++)
        {
            var arg = context.Args[i];

            if (arg == "--chat" && i + 1 < context.Args.Length)
            {
                chatId = ChatId.From(context.Args[++i]);
            }
            else if (arg == "--since" && i + 1 < context.Args.Length)
            {
                if (DateTime.TryParse(context.Args[++i], out var dateValue))
                {
                    since = dateValue;
                }
            }
            else if (arg == "--until" && i + 1 < context.Args.Length)
            {
                if (DateTime.TryParse(context.Args[++i], out var dateValue))
                {
                    until = dateValue;
                }
            }
            else if (arg == "--role" && i + 1 < context.Args.Length)
            {
                var roleStr = context.Args[++i];
                if (Enum.TryParse<MessageRole>(roleStr, true, out var roleValue))
                {
                    roleFilter = roleValue;
                }
            }
            else if (arg == "--page-size" && i + 1 < context.Args.Length)
            {
                if (int.TryParse(context.Args[++i], out var sizeValue))
                {
                    pageSize = sizeValue;
                }
            }
            else if (arg == "--page" && i + 1 < context.Args.Length)
            {
                if (int.TryParse(context.Args[++i], out var pageValue))
                {
                    pageNumber = pageValue;
                }
            }
            else if (arg == "--json")
            {
                jsonOutput = true;
            }
        }

        // Build query
        var query = new SearchQuery
        {
            QueryText = queryText,
            ChatId = chatId,
            Since = since,
            Until = until,
            RoleFilter = roleFilter,
            PageSize = pageSize,
            PageNumber = pageNumber
        };

        // Validate query
        var validationResult = query.Validate();
        if (!validationResult.IsValid)
        {
            await context.Output.WriteLineAsync($"Error: {string.Join(", ", validationResult.Errors)}").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        // Execute search
        try
        {
            var results = await _searchService.SearchAsync(query, CancellationToken.None).ConfigureAwait(false);

            if (jsonOutput)
            {
                var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
                await context.Output.WriteLineAsync(json).ConfigureAwait(false);
            }
            else
            {
                // Table output
                if (results.Results.Count == 0)
                {
                    await context.Output.WriteLineAsync("No results found.").ConfigureAwait(false);
                }
                else
                {
                    await context.Output.WriteLineAsync($"{"Chat",-20} {"Date",-16} {"Role",-10} {"Snippet",-50} {"Score",8}").ConfigureAwait(false);
                    await context.Output.WriteLineAsync(new string('-', 105)).ConfigureAwait(false);

                    foreach (var result in results.Results)
                    {
                        var chat = result.ChatTitle.Length > 18 ? result.ChatTitle.Substring(0, 18) + ".." : result.ChatTitle;
                        var date = result.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                        var snippet = result.Snippet.Length > 48 ? result.Snippet.Substring(0, 48) + ".." : result.Snippet;

                        // Remove <mark> tags for plain text output
                        snippet = snippet.Replace("<mark>", string.Empty, StringComparison.Ordinal).Replace("</mark>", string.Empty, StringComparison.Ordinal);

                        await context.Output.WriteLineAsync(
                            $"{chat,-20} {date,-16} {result.Role,-10} {snippet,-50} {result.Score,8:F2}").ConfigureAwait(false);
                    }

                    await context.Output.WriteLineAsync(string.Empty).ConfigureAwait(false);
                    await context.Output.WriteLineAsync(
                        $"Page {results.PageNumber}/{results.TotalPages} | Total: {results.TotalCount} results | Query time: {results.QueryTimeMs:F0}ms").ConfigureAwait(false);
                }
            }

            return ExitCode.Success;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.GeneralError;
        }
    }
}
