// src/Acode.Cli/Commands/SearchIndexOptimizeCommand.cs
namespace Acode.Cli.Commands;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Acode.Application.Interfaces;
using Acode.Domain.Search;

/// <summary>
/// Implements the search index optimize command to merge FTS5 segments for better performance.
/// </summary>
public sealed class SearchIndexOptimizeCommand : ICommand
{
    private readonly ISearchService _searchService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchIndexOptimizeCommand"/> class.
    /// </summary>
    /// <param name="searchService">Search service.</param>
    public SearchIndexOptimizeCommand(ISearchService searchService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
    }

    /// <inheritdoc/>
    public string Name => "search-index-optimize";

    /// <inheritdoc/>
    public string[]? Aliases => new[] { "index-optimize" };

    /// <inheritdoc/>
    public string Description => "Optimize search index by merging FTS5 segments";

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"
USAGE:
    acode search index optimize

DESCRIPTION:
    Optimize the full-text search index by merging FTS5 segments.
    This improves query performance by reducing segment fragmentation.

OUTPUT:
    - Before/after segment count
    - Optimization duration
    - Last optimized timestamp

EXAMPLES:
    # Optimize search index
    acode search index optimize
";
    }

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            // Get initial status for before/after comparison
            var beforeStatus = await _searchService.GetIndexStatusAsync(context.CancellationToken).ConfigureAwait(false);

            await context.Output.WriteLineAsync("Optimizing search index...").ConfigureAwait(false);
            await context.Output.WriteLineAsync(string.Empty).ConfigureAwait(false);
            await context.Output.WriteLineAsync($"Segment count before: {beforeStatus.SegmentCount}").ConfigureAwait(false);

            // Run optimization with timing
            var stopwatch = Stopwatch.StartNew();
            await _searchService.OptimizeIndexAsync(context.CancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            // Get updated status
            var afterStatus = await _searchService.GetIndexStatusAsync(context.CancellationToken).ConfigureAwait(false);

            // Display results
            await context.Output.WriteLineAsync($"Segment count after:  {afterStatus.SegmentCount}").ConfigureAwait(false);
            await context.Output.WriteLineAsync(string.Empty).ConfigureAwait(false);

            var reduction = beforeStatus.SegmentCount - afterStatus.SegmentCount;
            if (reduction > 0)
            {
                await context.Output.WriteLineAsync($"✓ Reduced segments by {reduction} ({reduction / (double)beforeStatus.SegmentCount * 100:F0}%)").ConfigureAwait(false);
            }
            else if (afterStatus.SegmentCount == beforeStatus.SegmentCount)
            {
                await context.Output.WriteLineAsync("✓ Index was already optimized (no segment reduction needed)").ConfigureAwait(false);
            }

            await context.Output.WriteLineAsync($"Optimization completed in {stopwatch.ElapsedMilliseconds:N0}ms").ConfigureAwait(false);

            if (afterStatus.LastOptimizedAt.HasValue)
            {
                await context.Output.WriteLineAsync($"Last optimized: {afterStatus.LastOptimizedAt.Value:yyyy-MM-dd HH:mm:ss} UTC").ConfigureAwait(false);
            }

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
