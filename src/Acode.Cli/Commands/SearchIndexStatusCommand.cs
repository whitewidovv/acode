// src/Acode.Cli/Commands/SearchIndexStatusCommand.cs
namespace Acode.Cli.Commands;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Interfaces;
using Acode.Domain.Search;

/// <summary>
/// Implements the search index status command to display index health and statistics.
/// </summary>
public sealed class SearchIndexStatusCommand : ICommand
{
    private readonly ISearchService _searchService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchIndexStatusCommand"/> class.
    /// </summary>
    /// <param name="searchService">Search service.</param>
    public SearchIndexStatusCommand(ISearchService searchService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
    }

    /// <inheritdoc/>
    public string Name => "search-index-status";

    /// <inheritdoc/>
    public string[]? Aliases => new[] { "index-status" };

    /// <inheritdoc/>
    public string Description => "Display search index status and health";

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"
USAGE:
    acode search index status

DESCRIPTION:
    Display search index status, health, and performance metrics.

OUTPUT:
    - Indexed message count
    - Total message count
    - Index health status
    - Index size in MB
    - Segment count
    - Performance metrics

EXAMPLES:
    # Show index status
    acode search index status
";
    }

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var status = await _searchService.GetIndexStatusAsync(CancellationToken.None).ConfigureAwait(false);
            stopwatch.Stop();

            // Display header
            await context.Output.WriteLineAsync("Search Index Status").ConfigureAwait(false);
            await context.Output.WriteLineAsync(string.Empty).ConfigureAwait(false);

            // Display statistics
            await context.Output.WriteLineAsync($"Indexed Messages:  {status.IndexedMessageCount:N0}").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"Total Messages:    {status.TotalMessageCount:N0}").ConfigureAwait(false);

            // Display health status with icon
            var healthIcon = status.IsHealthy ? "✓" : "✗";
            var healthText = status.IsHealthy ? "Healthy" : "Unhealthy";
            await context.Output.WriteLineAsync($"Status:            {healthText} {healthIcon}").ConfigureAwait(false);

            // Display optional metrics if available
            if (status.IndexSizeBytes > 0 || status.SegmentCount > 0 || status.LastOptimizedAt.HasValue)
            {
                await context.Output.WriteLineAsync(string.Empty).ConfigureAwait(false);

                if (status.IndexSizeBytes > 0)
                {
                    var sizeMb = status.IndexSizeBytes / (1024.0 * 1024.0);
                    await context.Output.WriteLineAsync($"Index Size:        {sizeMb:F2} MB").ConfigureAwait(false);
                }

                if (status.LastOptimizedAt.HasValue)
                {
                    await context.Output.WriteLineAsync($"Last Optimized:    {status.LastOptimizedAt.Value:yyyy-MM-dd HH:mm:ss} UTC").ConfigureAwait(false);
                }

                if (status.SegmentCount > 0)
                {
                    await context.Output.WriteLineAsync($"Segment Count:     {status.SegmentCount}").ConfigureAwait(false);
                }
            }

            // Display performance
            await context.Output.WriteLineAsync(string.Empty).ConfigureAwait(false);
            await context.Output.WriteLineAsync($"Performance:       Status check completed in {stopwatch.ElapsedMilliseconds}ms").ConfigureAwait(false);

            return ExitCode.Success;
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
}
