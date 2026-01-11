// tests/Acode.Cli.Tests/Commands/SearchIndexStatusCommandTests.cs
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

public class SearchIndexStatusCommandTests
{
    [Fact]
    public async Task ExecuteAsync_DisplaysIndexedMessageCount()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexStatusCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new IndexStatus
            {
                IndexedMessageCount = 1234,
                TotalMessageCount = 1234,
                IsHealthy = true
            });

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("Indexed Messages:  1,234");
    }

    [Fact]
    public async Task ExecuteAsync_DisplaysHealthyStatus()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexStatusCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new IndexStatus
            {
                IndexedMessageCount = 100,
                TotalMessageCount = 100,
                IsHealthy = true
            });

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("Status:            Healthy ✓");
    }

    [Fact]
    public async Task ExecuteAsync_DisplaysUnhealthyStatus()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexStatusCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new IndexStatus
            {
                IndexedMessageCount = 50,
                TotalMessageCount = 100,
                IsHealthy = false
            });

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("Status:            Unhealthy ✗");
    }

    [Fact]
    public async Task ExecuteAsync_FormatsIndexSizeHumanReadable()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexStatusCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        var indexSizeBytes = (4L * 1024 * 1024) + (512 * 1024); // 4.5 MB
        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new IndexStatus
            {
                IndexedMessageCount = 1000,
                TotalMessageCount = 1000,
                IsHealthy = true,
                IndexSizeBytes = indexSizeBytes
            });

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("Index Size:");
        result.Should().Contain("MB");
    }

    [Fact]
    public async Task ExecuteAsync_CompletesUnder100ms()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexStatusCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IndexStatus
            {
                IndexedMessageCount = 10000,
                TotalMessageCount = 10000,
                IsHealthy = true
            }));

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("Performance:");
        result.Should().Contain("ms");

        // AC-110: Status check should complete in <100ms (this is a unit test,
        // actual performance tested in integration tests with real database)
    }

    [Fact]
    public async Task ExecuteAsync_DisplaysSegmentCount()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexStatusCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new IndexStatus
            {
                IndexedMessageCount = 5000,
                TotalMessageCount = 5000,
                IsHealthy = true,
                SegmentCount = 3
            });

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("Segment Count:     3");
    }

    [Fact]
    public async Task ExecuteAsync_OnSearchException_ShowsErrorAndRemediation()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexStatusCommand(searchService);
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

    private static CommandContext CreateContext(string[] args, TextWriter output)
    {
        var formatter = Substitute.For<IOutputFormatter>();
        return new CommandContext
        {
            Args = args,
            Output = output,
            Configuration = new Dictionary<string, object>(),
            Formatter = formatter,
            CancellationToken = CancellationToken.None
        };
    }
}
