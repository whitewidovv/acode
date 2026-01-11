// tests/Acode.Cli.Tests/Commands/SearchIndexOptimizeCommandTests.cs
namespace Acode.Cli.Tests.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Interfaces;
using Acode.Cli;
using Acode.Cli.Commands;
using Acode.Domain.Search;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class SearchIndexOptimizeCommandTests
{
    [Fact]
    public async Task ExecuteAsync_OptimizesIndex()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexOptimizeCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        var beforeStatus = new IndexStatus
        {
            IndexedMessageCount = 10000,
            TotalMessageCount = 10000,
            IsHealthy = true,
            SegmentCount = 5
        };

        var afterStatus = new IndexStatus
        {
            IndexedMessageCount = 10000,
            TotalMessageCount = 10000,
            IsHealthy = true,
            SegmentCount = 1,
            LastOptimizedAt = DateTime.UtcNow
        };

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(beforeStatus, afterStatus);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        await searchService.Received(1).OptimizeIndexAsync(Arg.Any<CancellationToken>()).ConfigureAwait(true);
        var result = output.ToString();
        result.Should().Contain("Optimizing search index");
        result.Should().Contain("Optimization completed");
    }

    [Fact]
    public async Task ExecuteAsync_DisplaysSegmentCountReduction()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexOptimizeCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        var beforeStatus = new IndexStatus
        {
            IndexedMessageCount = 10000,
            TotalMessageCount = 10000,
            IsHealthy = true,
            SegmentCount = 8
        };

        var afterStatus = new IndexStatus
        {
            IndexedMessageCount = 10000,
            TotalMessageCount = 10000,
            IsHealthy = true,
            SegmentCount = 2,
            LastOptimizedAt = DateTime.UtcNow
        };

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(beforeStatus, afterStatus);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("Segment count before: 8");
        result.Should().Contain("Segment count after:  2");
        result.Should().Contain("Reduced segments by 6");
        result.Should().Contain("75%");
    }

    [Fact]
    public async Task ExecuteAsync_CompletesUnder10Seconds()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexOptimizeCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        var status = new IndexStatus
        {
            IndexedMessageCount = 10000,
            TotalMessageCount = 10000,
            IsHealthy = true,
            SegmentCount = 3,
            LastOptimizedAt = DateTime.UtcNow
        };

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(status);

        // Simulate fast optimization (should complete in milliseconds in tests)
        searchService.OptimizeIndexAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);
        stopwatch.Stop();

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // <10s
        var result = output.ToString();
        result.Should().Contain("Optimization completed");
        result.Should().Contain("ms");

        // AC-099: Optimization completes in <10 seconds for 10k messages
        // (this is a unit test, actual performance tested with real database in integration tests)
    }

    [Fact]
    public async Task ExecuteAsync_OnSearchException_ShowsErrorAndRemediation()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexOptimizeCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns<IndexStatus>(_ => throw new SearchException(
                SearchErrorCodes.IndexNotInitialized,
                "Search index has not been initialized",
                "Run 'acode search rebuild' to initialize the search index"));

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.GeneralError);
        var result = output.ToString();
        result.Should().Contain("ACODE-SRCH-006");
        result.Should().Contain("Search index has not been initialized");
        result.Should().Contain("How to fix:");
        result.Should().Contain("acode search rebuild");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoSegmentReduction_ShowsAlreadyOptimizedMessage()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexOptimizeCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        var status = new IndexStatus
        {
            IndexedMessageCount = 10000,
            TotalMessageCount = 10000,
            IsHealthy = true,
            SegmentCount = 1,
            LastOptimizedAt = DateTime.UtcNow.AddHours(-1)
        };

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(status);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("already optimized");
    }

    [Fact]
    public async Task ExecuteAsync_DisplaysLastOptimizedTimestamp()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexOptimizeCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        var optimizedAt = new DateTime(2026, 1, 10, 14, 30, 0, DateTimeKind.Utc);
        var beforeStatus = new IndexStatus
        {
            IndexedMessageCount = 5000,
            TotalMessageCount = 5000,
            IsHealthy = true,
            SegmentCount = 3
        };

        var afterStatus = new IndexStatus
        {
            IndexedMessageCount = 5000,
            TotalMessageCount = 5000,
            IsHealthy = true,
            SegmentCount = 1,
            LastOptimizedAt = optimizedAt
        };

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(beforeStatus, afterStatus);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("Last optimized: 2026-01-10 14:30:00 UTC");
    }

    private static CommandContext CreateContext(string[] args, TextWriter output, CancellationToken? cancellationToken = null)
    {
        var formatter = Substitute.For<IOutputFormatter>();
        return new CommandContext
        {
            Args = args,
            Output = output,
            Configuration = new Dictionary<string, object>(),
            Formatter = formatter,
            CancellationToken = cancellationToken ?? CancellationToken.None
        };
    }
}
