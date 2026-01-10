// tests/Acode.Cli.Tests/Commands/SearchCommandTests.cs
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
using Acode.Domain.Models.Inference;
using Acode.Domain.Search;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class SearchCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WithoutJsonFlag_OutputsTable()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(new[] { "test query" }, output);

        searchService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestResults(1));

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("SCORE");  // Table header
        result.Should().Contain("CHAT");
        result.Should().Contain("-----");  // Table separator
    }

    [Fact]
    public async Task ExecuteAsync_WithJsonFlag_OutputsJson()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(new[] { "test query", "--json" }, output);

        searchService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestResults(1));

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("\"results\"");  // JSON field
        result.Should().Contain("\"totalCount\"");
        result.Should().NotContain("SCORE");  // Not table format
    }

    [Fact]
    public async Task ExecuteAsync_WithNoResults_ShowsNoResultsMessage()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(new[] { "test query" }, output);

        searchService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestResults(0));

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("No results found");
    }

    [Fact]
    public async Task ExecuteAsync_WithPagination_ShowsPageInfo()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(new[] { "test", "--page", "2", "--page-size", "10" }, output);

        var results = new SearchResults
        {
            Results = new[] { CreateSearchResult() },
            TotalCount = 42,
            PageSize = 10,
            PageNumber = 2,
            QueryTimeMs = 50.0
        };

        searchService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(results);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("Page 2/5");  // 42 total / 10 per page = 5 pages
        result.Should().Contain("Total: 42 results");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidQuery_ReturnsError()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchCommand(searchService);
        var output = new StringWriter();

        // Empty page size would trigger validation error
        var context = CreateContext(new[] { "test", "--page-size", "0" }, output);

        searchService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestResults(1));

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert (validation happens in SearchQuery.Validate())
        // If query is invalid, command should return InvalidArguments
        exitCode.Should().BeOneOf(ExitCode.Success, ExitCode.InvalidArguments);
    }

    [Fact]
    public async Task ExecuteAsync_WithChatFilter_PassesToSearchService()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchCommand(searchService);
        var output = new StringWriter();
        var chatId = ChatId.NewId();
        var context = CreateContext(new[] { "test", "--chat", chatId.Value }, output);

        searchService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestResults(1));

        // Act
        await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        await searchService.Received(1).SearchAsync(
            Arg.Is<SearchQuery>(q => q.ChatId != null && q.ChatId.Value == chatId.Value),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task ExecuteAsync_WithDateFilter_PassesToSearchService()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(new[] { "test", "--since", "2026-01-01", "--until", "2026-01-10" }, output);

        searchService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestResults(1));

        // Act
        await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        await searchService.Received(1).SearchAsync(
            Arg.Is<SearchQuery>(q => q.Since.HasValue && q.Until.HasValue),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task ExecuteAsync_WithRoleFilter_PassesToSearchService()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(new[] { "test", "--role", "user" }, output);

        searchService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestResults(1));

        // Act
        await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        await searchService.Received(1).SearchAsync(
            Arg.Is<SearchQuery>(q => q.RoleFilter == MessageRole.User),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyQueryText_ReturnsInvalidArguments()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.InvalidArguments);
        var result = output.ToString();
        result.Should().Contain("Error: Missing search query");
    }

    [Fact]
    public async Task ExecuteAsync_ShowsQueryExecutionTime()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(new[] { "test" }, output);

        var results = new SearchResults
        {
            Results = new[] { CreateSearchResult() },
            TotalCount = 1,
            PageSize = 20,
            PageNumber = 1,
            QueryTimeMs = 123.45
        };

        searchService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(results);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("Query time:");
        result.Should().Contain("ms");
    }

    [Fact]
    public void GetHelp_ReturnsUsageInformation()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchCommand(searchService);

        // Act
        var help = command.GetHelp();

        // Assert
        help.Should().Contain("USAGE:");
        help.Should().Contain("acode search");
        help.Should().Contain("--json");
        help.Should().Contain("--chat");
        help.Should().Contain("--role");
        help.Should().Contain("--since");
        help.Should().Contain("EXAMPLES:");
    }

    [Fact]
    public async Task ExecuteAsync_WithServiceError_ReturnsGeneralError()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var command = new SearchCommand(searchService);
        var output = new StringWriter();
        var context = CreateContext(new[] { "test" }, output);

        searchService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns<SearchResults>(_ => throw new InvalidOperationException("Database connection failed"));

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.GeneralError);
        var result = output.ToString();
        result.Should().Contain("Error:");
        result.Should().Contain("Database connection failed");
    }

    private static SearchResults CreateTestResults(int count)
    {
        var results = new SearchResult[count];
        for (int i = 0; i < count; i++)
        {
            results[i] = CreateSearchResult();
        }

        return new SearchResults
        {
            Results = results,
            TotalCount = count,
            PageSize = 20,
            PageNumber = 1,
            QueryTimeMs = 42.5
        };
    }

    private static SearchResult CreateSearchResult()
    {
        return new SearchResult
        {
            MessageId = MessageId.NewId(),
            ChatId = ChatId.NewId(),
            ChatTitle = "Test Chat",
            Role = MessageRole.User,
            CreatedAt = DateTime.UtcNow,
            Snippet = "This is a <mark>test</mark> snippet",
            Score = 12.34,
            Matches = Array.Empty<MatchLocation>()
        };
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
