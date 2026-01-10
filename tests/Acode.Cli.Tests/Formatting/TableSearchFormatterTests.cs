// tests/Acode.Cli.Tests/Formatting/TableSearchFormatterTests.cs
namespace Acode.Cli.Tests.Formatting;

using System;
using System.IO;
using Acode.Cli.Formatting;
using Acode.Domain.Conversation;
using Acode.Domain.Models.Inference;
using Acode.Domain.Search;
using FluentAssertions;
using Xunit;

public class TableSearchFormatterTests
{
    [Fact]
    public void WriteSearchResults_WithMultipleResults_FormatsAsTable()
    {
        // Arrange
        var formatter = new TableSearchFormatter();
        var results = CreateTestResults(2);
        var output = new StringWriter();

        // Act
        formatter.WriteSearchResults(results, output);

        // Assert
        var table = output.ToString();
        table.Should().Contain("SCORE");
        table.Should().Contain("CHAT");
        table.Should().Contain("DATE");
        table.Should().Contain("ROLE");
        table.Should().Contain("SNIPPET");
        table.Should().Contain("-----"); // Header separator
        table.Should().Contain("Test Chat");
    }

    [Fact]
    public void WriteSearchResults_WithMarkTags_RendersAsAnsiColor()
    {
        // Arrange
        var formatter = new TableSearchFormatter();
        var results = CreateTestResults(1);
        var output = new StringWriter();

        // Act
        formatter.WriteSearchResults(results, output);

        // Assert
        var table = output.ToString();

        // ANSI codes should be present (yellow background: \x1b[43m)
        table.Should().Contain("\x1b[43m"); // Yellow background ANSI code
        table.Should().Contain("\x1b[0m");  // Reset ANSI code
    }

    [Fact]
    public void WriteSearchResults_WithLongSnippet_TruncatesAtWidth()
    {
        // Arrange
        var formatter = new TableSearchFormatter();
        var longSnippet = new string('a', 200);
        var results = CreateTestResultsWithSnippet(longSnippet);
        var output = new StringWriter();

        // Act
        formatter.WriteSearchResults(results, output);

        // Assert
        var table = output.ToString();
        var lines = table.Split(Environment.NewLine);

        // Find the data line (not header, not separator, not pagination)
        var dataLine = lines.FirstOrDefault(l => l.Contains("Test Chat", StringComparison.Ordinal));
        dataLine.Should().NotBeNull();
        dataLine!.Should().Contain(".."); // Truncation indicator
    }

    [Fact]
    public void WriteSearchResults_WithEmptyResults_ShowsNoResultsMessage()
    {
        // Arrange
        var formatter = new TableSearchFormatter();
        var results = new SearchResults
        {
            Results = Array.Empty<SearchResult>(),
            TotalCount = 0,
            PageSize = 20,
            PageNumber = 1,
            QueryTimeMs = 10.5
        };
        var output = new StringWriter();

        // Act
        formatter.WriteSearchResults(results, output);

        // Assert
        var table = output.ToString();
        table.Should().Contain("No results found");
    }

    [Fact]
    public void WriteSearchResults_PreservesColumnAlignment()
    {
        // Arrange
        var formatter = new TableSearchFormatter();
        var results = CreateTestResults(2);
        var output = new StringWriter();

        // Act
        formatter.WriteSearchResults(results, output);

        // Assert
        var table = output.ToString();
        var lines = table.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        // All lines should have consistent spacing (at least the header and data lines)
        lines.Length.Should().BeGreaterThan(2);
    }

    [Fact]
    public void WriteSearchResults_HandlesUnicodeCharacters()
    {
        // Arrange
        var formatter = new TableSearchFormatter();
        var results = CreateTestResultsWithSnippet("Test with émojis 😀 and ñ characters");
        var output = new StringWriter();

        // Act
        formatter.WriteSearchResults(results, output);

        // Assert
        var table = output.ToString();
        table.Should().Contain("émojis");
        table.Should().Contain("😀");
        table.Should().Contain("ñ");
    }

    [Fact]
    public void WriteSearchResults_ShowsPaginationInfo()
    {
        // Arrange
        var formatter = new TableSearchFormatter();
        var results = new SearchResults
        {
            Results = new[] { CreateSearchResult() },
            TotalCount = 42,
            PageSize = 10,
            PageNumber = 2,
            QueryTimeMs = 123.45
        };
        var output = new StringWriter();

        // Act
        formatter.WriteSearchResults(results, output);

        // Assert
        var table = output.ToString();
        table.Should().Contain("Page 2/5");
        table.Should().Contain("Total: 42 results");
    }

    [Fact]
    public void WriteSearchResults_ShowsQueryTime()
    {
        // Arrange
        var formatter = new TableSearchFormatter();
        var results = new SearchResults
        {
            Results = new[] { CreateSearchResult() },
            TotalCount = 1,
            PageSize = 20,
            PageNumber = 1,
            QueryTimeMs = 987.65
        };
        var output = new StringWriter();

        // Act
        formatter.WriteSearchResults(results, output);

        // Assert
        var table = output.ToString();
        table.Should().Contain("Query time: 988ms"); // Rounded to nearest ms
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

    private static SearchResults CreateTestResultsWithSnippet(string snippet)
    {
        var result = new SearchResult
        {
            MessageId = MessageId.NewId(),
            ChatId = ChatId.NewId(),
            ChatTitle = "Test Chat",
            Role = MessageRole.User,
            CreatedAt = DateTime.UtcNow,
            Snippet = snippet, // Set in initializer
            Score = 12.34,
            Matches = Array.Empty<MatchLocation>()
        };

        return new SearchResults
        {
            Results = new[] { result },
            TotalCount = 1,
            PageSize = 20,
            PageNumber = 1,
            QueryTimeMs = 10.0
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
}
