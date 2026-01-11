// src/Acode.Cli/Commands/SearchIndexRebuildCommand.cs
namespace Acode.Cli.Commands;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Interfaces;
using Acode.Domain.Conversation;
using Acode.Domain.Search;

/// <summary>
/// Implements the search index rebuild command to rebuild the full-text search index.
/// </summary>
public sealed class SearchIndexRebuildCommand : ICommand
{
    private readonly ISearchService _searchService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchIndexRebuildCommand"/> class.
    /// </summary>
    /// <param name="searchService">Search service.</param>
    public SearchIndexRebuildCommand(ISearchService searchService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
    }

    /// <inheritdoc/>
    public string Name => "search-index-rebuild";

    /// <inheritdoc/>
    public string[]? Aliases => new[] { "index-rebuild" };

    /// <inheritdoc/>
    public string Description => "Rebuild search index from scratch";

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"
USAGE:
    acode search index rebuild [--chat <id>]

DESCRIPTION:
    Rebuild the full-text search index from scratch.

OPTIONS:
    --chat <id>    Rebuild index only for the specified chat ID (partial rebuild)

EXAMPLES:
    # Rebuild full index
    acode search index rebuild

    # Rebuild index for specific chat
    acode search index rebuild --chat chat_abc123
";
    }

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            // Parse --chat flag if provided
            ChatId? chatIdFilter = null;
            for (int i = 0; i < context.Args.Length; i++)
            {
                if (context.Args[i] == "--chat" && i + 1 < context.Args.Length)
                {
                    chatIdFilter = ChatId.From(context.Args[i + 1]);
                    break;
                }
            }

            // Get initial status to determine total message count
            var initialStatus = await _searchService.GetIndexStatusAsync(context.CancellationToken).ConfigureAwait(false);
            var totalMessages = chatIdFilter.HasValue
                ? await GetChatMessageCountAsync(chatIdFilter.Value, context.CancellationToken).ConfigureAwait(false)
                : initialStatus.TotalMessageCount;

            if (totalMessages == 0)
            {
                await context.Output.WriteLineAsync(chatIdFilter.HasValue
                    ? $"No messages found for chat {chatIdFilter.Value.Value}"
                    : "No messages found to index").ConfigureAwait(false);
                return ExitCode.Success;
            }

            // Display rebuild start message
            await context.Output.WriteLineAsync(chatIdFilter.HasValue
                ? $"Rebuilding index for chat {chatIdFilter.Value.Value}..."
                : "Rebuilding full search index...").ConfigureAwait(false);
            await context.Output.WriteLineAsync(string.Empty).ConfigureAwait(false);

            // Create progress reporter with progress bar
            var stopwatch = Stopwatch.StartNew();
            var progressReporter = new ProgressReporter(context.Output, totalMessages, stopwatch);

            try
            {
                // Execute rebuild with progress tracking
                if (chatIdFilter.HasValue)
                {
                    await _searchService.RebuildIndexAsync(
                        chatIdFilter.Value,
                        progressReporter,
                        context.CancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await _searchService.RebuildIndexAsync(
                        progressReporter,
                        context.CancellationToken).ConfigureAwait(false);
                }

                stopwatch.Stop();

                // Clear progress bar line and show completion message
                await progressReporter.ClearProgressLineAsync().ConfigureAwait(false);
                await context.Output.WriteLineAsync(string.Empty).ConfigureAwait(false);
                await context.Output.WriteLineAsync($"✓ Rebuild complete! Indexed {totalMessages:N0} messages in {stopwatch.Elapsed.TotalSeconds:F1}s").ConfigureAwait(false);

                return ExitCode.Success;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                await progressReporter.ClearProgressLineAsync().ConfigureAwait(false);
                await context.Output.WriteLineAsync(string.Empty).ConfigureAwait(false);
                await context.Output.WriteLineAsync($"✗ Rebuild cancelled after {stopwatch.Elapsed.TotalSeconds:F1}s (indexed {progressReporter.CurrentCount:N0}/{totalMessages:N0} messages)").ConfigureAwait(false);
                return ExitCode.GeneralError;
            }
        }
        catch (SearchException ex)
        {
            // Display structured error
            await context.Output.WriteLineAsync($"Error [{ex.ErrorCode}]: {ex.Message}").ConfigureAwait(false);
            await context.Output.WriteLineAsync(string.Empty).ConfigureAwait(false);
            await context.Output.WriteLineAsync($"How to fix: {ex.Remediation}").ConfigureAwait(false);
            return ExitCode.GeneralError;
        }
        catch (Exception ex)
        {
            await context.Output.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ExitCode.GeneralError;
        }
    }

    private async Task<int> GetChatMessageCountAsync(ChatId chatId, CancellationToken cancellationToken)
    {
        // Use SearchAsync to get message count for this chat
        // This is a workaround since we don't have a direct chat repository method
        var query = new SearchQuery
        {
            QueryText = string.Empty,
            ChatId = chatId,
            PageSize = 1,
            PageNumber = 1
        };

        var results = await _searchService.SearchAsync(query, cancellationToken).ConfigureAwait(false);
        return results.TotalCount;
    }

    /// <summary>
    /// Progress reporter that displays a real-time progress bar with percentage, counts, and ETA.
    /// </summary>
    private sealed class ProgressReporter : IProgress<int>
    {
        private const int ProgressBarWidth = 20;

        private readonly System.IO.TextWriter _output;
        private readonly int _totalMessages;
        private readonly Stopwatch _stopwatch;
        private int _currentCount;
        private DateTime _lastUpdateTime;

        public ProgressReporter(System.IO.TextWriter output, int totalMessages, Stopwatch stopwatch)
        {
            _output = output;
            _totalMessages = totalMessages;
            _stopwatch = stopwatch;
            _currentCount = 0;
            _lastUpdateTime = DateTime.UtcNow;
        }

        public int CurrentCount => _currentCount;

        public void Report(int value)
        {
            _currentCount = value;

            // Throttle updates to avoid excessive console writes (max 10 updates per second)
            var now = DateTime.UtcNow;
            if ((now - _lastUpdateTime).TotalMilliseconds < 100 && value < _totalMessages)
            {
                return;
            }

            _lastUpdateTime = now;

            // Calculate percentage
            var percentage = _totalMessages > 0 ? (double)_currentCount / _totalMessages * 100 : 0;

            // Calculate ETA
            var elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
            var messagesPerSecond = elapsedSeconds > 0 ? _currentCount / elapsedSeconds : 0;
            var remainingMessages = _totalMessages - _currentCount;
            var etaSeconds = messagesPerSecond > 0 ? remainingMessages / messagesPerSecond : 0;

            // Build progress bar
            var filledCount = (int)(percentage / 100 * ProgressBarWidth);
            var progressBar = new string('=', Math.Max(0, filledCount - 1))
                + (filledCount > 0 ? ">" : string.Empty)
                + new string(' ', Math.Max(0, ProgressBarWidth - filledCount));

            // Format output: [=====>     ] 45% (234/520 messages) ETA: 12s
            var progressLine = $"\r[{progressBar}] {percentage:F0}% ({_currentCount:N0}/{_totalMessages:N0} messages) ETA: {FormatEta(etaSeconds)}";

            // Write progress line (using \r to overwrite previous line)
            _output.Write(progressLine);
        }

        public async Task ClearProgressLineAsync()
        {
            // Clear the progress bar line by overwriting with spaces
            var clearLine = "\r" + new string(' ', 80) + "\r";
            await _output.WriteAsync(clearLine).ConfigureAwait(false);
        }

        private static string FormatEta(double seconds)
        {
            if (double.IsInfinity(seconds) || double.IsNaN(seconds) || seconds <= 0)
            {
                return "--s";
            }

            if (seconds < 60)
            {
                return $"{seconds:F0}s";
            }

            if (seconds < 3600)
            {
                var minutes = (int)(seconds / 60);
                var secs = (int)(seconds % 60);
                return $"{minutes}m {secs}s";
            }

            var hours = (int)(seconds / 3600);
            var mins = (int)((seconds % 3600) / 60);
            return $"{hours}h {mins}m";
        }
    }
}
