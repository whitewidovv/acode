// tests/Acode.Cli.Tests/Formatting/JsonSearchFormatterTests.cs
namespace Acode.Cli.Tests.Formatting;

using System;
using System.IO;
using System.Text.Json;
using Acode.Cli.Formatting;
using Acode.Domain.Conversation;
using Acode.Domain.Models.Inference;
using Acode.Domain.Search;
using FluentAssertions;
using Xunit;

public class JsonSearchFormatterTests
{
    [Fact]
    public void WriteSearchResults_SerializesAsValidJson()
    {
        // Arrange
        var formatter = new JsonSearchFormatter();
        var results = CreateTestResults();
        var output = new StringWriter();

        // Act
        formatter.WriteSearchResults(results, output);

        // Assert
        var json = output.ToString();
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var deserialized = JsonSerializer.Deserialize<SearchResults>(json, options);
        deserialized.Should().NotBeNull();
    }

    [Fact]
    public void WriteSearchResults_IncludesAllFields()
    {
        // Arrange
        var formatter = new JsonSearchFormatter();
        var results = CreateTestResults();
        var output = new StringWriter();

        // Act
        formatter.WriteSearchResults(results, output);

        // Assert
        var json = output.ToString();
        json.Should().Contain("\"totalCount\"");
        json.Should().Contain("\"pageNumber\"");
        json.Should().Contain("\"totalPages\"");
        json.Should().Contain("\"queryTimeMs\"");
        json.Should().Contain("\"results\"");
        json.Should().Contain("\"chatTitle\"");
        json.Should().Contain("\"snippet\"");
        json.Should().Contain("\"score\"");
    }

    [Fact]
    public void WriteSearchResults_UsesIndentation()
    {
        // Arrange
        var formatter = new JsonSearchFormatter();
        var results = CreateTestResults();
        var output = new StringWriter();

        // Act
        formatter.WriteSearchResults(results, output);

        // Assert
        var json = output.ToString();
        json.Should().Contain(Environment.NewLine);
        json.Should().Contain("  "); // Indentation spaces
    }

    [Fact]
    public void WriteSearchResults_PreservesMarkTags()
    {
        // Arrange
        var formatter = new JsonSearchFormatter();
        var results = CreateTestResults();
        var output = new StringWriter();

        // Act
        formatter.WriteSearchResults(results, output);

        // Assert
        var json = output.ToString();

        // JSON serializer escapes < and > as \u003C and \u003E
        json.Should().Contain("mark");  // Tag name preserved
        json.Should().Contain("test");  // Content preserved
        (json.Contains("<mark>", StringComparison.Ordinal) || json.Contains("\\u003Cmark\\u003E", StringComparison.Ordinal)).Should().BeTrue("mark tags should be preserved in some form");
    }

    private static SearchResults CreateTestResults()
    {
        var chatId = ChatId.NewId();
        var messageId = MessageId.NewId();

        var result = new SearchResult
        {
            MessageId = messageId,
            ChatId = chatId,
            ChatTitle = "Test Chat",
            Role = MessageRole.User,
            CreatedAt = DateTime.UtcNow,
            Snippet = "This is a <mark>test</mark> snippet",
            Score = 12.34,
            Matches = Array.Empty<MatchLocation>()
        };

        return new SearchResults
        {
            Results = new[] { result },
            TotalCount = 1,
            PageSize = 20,
            PageNumber = 1,
            QueryTimeMs = 42.5
        };
    }
}
