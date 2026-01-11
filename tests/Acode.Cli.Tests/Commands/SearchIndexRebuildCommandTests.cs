// tests/Acode.Cli.Tests/Commands/SearchIndexRebuildCommandTests.cs
namespace Acode.Cli.Tests.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Interfaces;
using Acode.Cli;
using Acode.Cli.Commands;
using Acode.Domain.Conversation;
using Acode.Domain.Search;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class SearchIndexRebuildCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WithNoChatFilter_RebuildsFullIndex()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexRebuildCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new IndexStatus
            {
                IndexedMessageCount = 0,
                TotalMessageCount = 1000,
                IsHealthy = false
            });

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        await searchService.Received(1).RebuildIndexAsync(
            Arg.Any<IProgress<int>>(),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
        var result = output.ToString();
        result.Should().Contain("Rebuilding full search index");
        result.Should().Contain("Rebuild complete");
    }

    [Fact]
    public async Task ExecuteAsync_WithChatFilter_RebuildsPartialIndex()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexRebuildCommand(searchService);
        var output = new StringWriter();

        // Use valid ULID format (26 characters)
        var chatIdValue = "01HQZP2XTEST1234567890ABCD";
        var context = CreateContext(new[] { "--chat", chatIdValue }, output);

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new IndexStatus
            {
                IndexedMessageCount = 500,
                TotalMessageCount = 1000,
                IsHealthy = false
            });

        // Mock SearchAsync to return chat-specific count
        searchService.SearchAsync(
                Arg.Is<SearchQuery>(q => q.ChatId != null && q.ChatId.Value.Value == chatIdValue),
                Arg.Any<CancellationToken>())
            .Returns(new SearchResults
            {
                Results = Array.Empty<SearchResult>(),
                TotalCount = 250,
                PageNumber = 1,
                PageSize = 1,
                QueryTimeMs = 10
            });

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        await searchService.Received(1).RebuildIndexAsync(
            Arg.Is<ChatId>(c => c.Value == chatIdValue),
            Arg.Any<IProgress<int>>(),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
        var result = output.ToString();
        result.Should().Contain($"Rebuilding index for chat {chatIdValue}");
        result.Should().Contain("Rebuild complete");
    }

    [Fact]
    public async Task ExecuteAsync_DisplaysProgressBar()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexRebuildCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new IndexStatus
            {
                IndexedMessageCount = 0,
                TotalMessageCount = 500,
                IsHealthy = false
            });

        // Capture progress reporter and simulate progress updates
        IProgress<int>? capturedProgress = null;
        searchService.RebuildIndexAsync(
                Arg.Do<IProgress<int>>(p => capturedProgress = p),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var progress = callInfo.Arg<IProgress<int>>();
                progress?.Report(100);
                progress?.Report(250);
                progress?.Report(500);
                return Task.CompletedTask;
            });

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        capturedProgress.Should().NotBeNull();
        var result = output.ToString();

        // Progress bar should show percentage and ETA format
        // Note: Actual progress bar output is ephemeral (overwritten with \r)
        // but we can verify the completion message
        result.Should().Contain("Rebuild complete");
        result.Should().Contain("500");
    }

    [Fact]
    public async Task ExecuteAsync_OnCancellation_ShowsCancellationMessage()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexRebuildCommand(searchService);
        var output = new StringWriter();

        // Create cancellation token that will be cancelled
        var cts = new CancellationTokenSource();
        var context = CreateContext(Array.Empty<string>(), output, cts.Token);

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new IndexStatus
            {
                IndexedMessageCount = 0,
                TotalMessageCount = 1000,
                IsHealthy = false
            });

        // Simulate cancellation after partial progress
        searchService.RebuildIndexAsync(
                Arg.Any<IProgress<int>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var progress = callInfo.Arg<IProgress<int>>();
                progress?.Report(200);
                throw new OperationCanceledException();
            });

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.GeneralError);
        var result = output.ToString();
        result.Should().Contain("Rebuild cancelled");
        result.Should().Contain("200");
        result.Should().Contain("1,000");
    }

    [Fact]
    public async Task ExecuteAsync_OnSearchException_ShowsErrorAndRemediation()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexRebuildCommand(searchService);
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
    public async Task ExecuteAsync_WithNoMessages_ShowsNoMessagesMessage()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchIndexRebuildCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        searchService.GetIndexStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new IndexStatus
            {
                IndexedMessageCount = 0,
                TotalMessageCount = 0,
                IsHealthy = true
            });

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("No messages found to index");

        // RebuildIndexAsync should NOT be called when there are no messages
        await searchService.DidNotReceive().RebuildIndexAsync(
            Arg.Any<IProgress<int>>(),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
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
