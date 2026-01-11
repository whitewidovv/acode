#pragma warning disable IDE0005 // Using directive is unnecessary

using Acode.Domain.Configuration;
using Acode.Infrastructure.Search;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Search;

public class SnippetGeneratorTests
{
    [Fact]
    public void GenerateSnippet_WithMatchingTerm_HighlightsTerm()
    {
        // Arrange
        var generator = new SnippetGenerator();
        var content = "This is a test message with important information";
        var query = "test";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert
        snippet.Should().Contain("<mark>test</mark>");
    }

    [Fact]
    public void GenerateSnippet_WithMultipleMatches_HighlightsAll()
    {
        // Arrange
        var generator = new SnippetGenerator();
        var content = "This test is a test of the test highlighting system";
        var query = "test";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert
        snippet.Should().Contain("<mark>test</mark>");
        var count = System.Text.RegularExpressions.Regex.Matches(snippet, "<mark>test</mark>").Count;
        count.Should().Be(3);
    }

    [Fact]
    public void GenerateSnippet_IsCaseInsensitive()
    {
        // Arrange
        var generator = new SnippetGenerator();
        var content = "This TEST is a Test of highlighting";
        var query = "test";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert
        snippet.Should().Contain("<mark>TEST</mark>");
        snippet.Should().Contain("<mark>Test</mark>");
    }

    [Fact]
    public void GenerateSnippet_WithMultipleQueryTerms_HighlightsAll()
    {
        // Arrange
        var generator = new SnippetGenerator();
        var content = "The quick brown fox jumps over the lazy dog";
        var query = "quick fox dog";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert
        snippet.Should().Contain("<mark>quick</mark>");
        snippet.Should().Contain("<mark>fox</mark>");
        snippet.Should().Contain("<mark>dog</mark>");
    }

    [Fact]
    public void GenerateSnippet_WithLongContent_TruncatesToMaxLength()
    {
        // Arrange
        var generator = new SnippetGenerator();
        var content = new string('a', 500); // 500 characters
        var query = "test";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert - should be truncated to around 200 chars (plus ellipsis)
        snippet.Length.Should().BeLessThan(250);
    }

    [Fact]
    public void GenerateSnippet_CentersAroundFirstMatch()
    {
        // Arrange
        var generator = new SnippetGenerator();
        var prefix = new string('x', 100);
        var suffix = new string('y', 100);
        var content = $"{prefix} test message {suffix}";
        var query = "test";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert
        snippet.Should().Contain("<mark>test</mark>");
        snippet.Should().Contain("message");

        // Should have context before and after the match
        snippet.Should().Contain("x");
        snippet.Should().Contain("y");
    }

    [Fact]
    public void GenerateSnippet_WithNoMatch_ReturnsBeginningOfContent()
    {
        // Arrange
        var generator = new SnippetGenerator();
        var content = "This is some content without the matching term";
        var query = "zebra";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert
        snippet.Should().Contain("This is some content");
        snippet.Should().NotContain("<mark>");
    }

    [Fact]
    public void GenerateSnippet_WithEmptyQuery_ReturnsPlainSnippet()
    {
        // Arrange
        var generator = new SnippetGenerator();
        var content = "This is some content";
        var query = string.Empty;

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert
        snippet.Should().Contain("This is some content");
        snippet.Should().NotContain("<mark>");
    }

    [Fact]
    public void GenerateSnippet_WithEmptyContent_ReturnsEmpty()
    {
        // Arrange
        var generator = new SnippetGenerator();
        var content = string.Empty;
        var query = "test";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert
        snippet.Should().BeEmpty();
    }

    [Fact]
    public void GenerateSnippet_PreservesWordBoundaries()
    {
        // Arrange
        var generator = new SnippetGenerator();
        var content = "testing test tested tester";
        var query = "test";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert
        snippet.Should().Contain("<mark>test</mark>");

        // Should only highlight exact word "test", not partial matches in "testing", "tested", "tester"
        snippet.Should().NotContain("<mark>testing</mark>");
        snippet.Should().NotContain("<mark>tested</mark>");
        snippet.Should().NotContain("<mark>tester</mark>");
    }

    [Fact]
    public void GenerateSnippet_DefaultMaxLengthIs150Characters()
    {
        // Arrange
        var generator = new SnippetGenerator();
        var content = new string('a', 200); // 200 chars, no matches
        var query = "test"; // No match, will return beginning of content

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert - should be truncated to 150 chars + "..." (153 total)
        snippet.Should().HaveLength(153, "default max snippet length is 150 characters");
        snippet.Should().EndWith("...");
    }

    [Fact]
    public void GenerateSnippet_WithCustomMaxLength_TruncatesAtConfiguredLength()
    {
        // Arrange - AC-059: Snippet length is configurable
        var customSettings = new SearchSettings
        {
            SnippetMaxLength = 100 // Custom: 100 chars (vs default 150)
        };
        var generator = new SnippetGenerator(customSettings);
        var content = new string('a', 200); // 200 chars, no matches
        var query = "test";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert - should be truncated to 100 chars + "..." (103 total)
        snippet.Should().HaveLength(103);
        snippet.Should().EndWith("...");
    }

    [Fact]
    public void GenerateSnippet_BelowMinLength_UsesMinLength()
    {
        // Arrange - AC-059: Validates snippet length lower bound (min 50)
        var invalidSettings = new SearchSettings
        {
            SnippetMaxLength = 10, // Too low!
            SnippetMinLength = 50 // Minimum enforced
        };
        var generator = new SnippetGenerator(invalidSettings);
        var content = new string('a', 200);
        var query = "test";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert - should use min length of 50, not the requested 10
        snippet.Should().HaveLength(53); // 50 + "..." = 53
    }

    [Fact]
    public void GenerateSnippet_AboveMaxLimit_UsesMaxLimit()
    {
        // Arrange - AC-059: Validates snippet length upper bound (max 500)
        var invalidSettings = new SearchSettings
        {
            SnippetMaxLength = 600, // Too high!
            SnippetMaxLengthLimit = 500 // Maximum enforced
        };
        var generator = new SnippetGenerator(invalidSettings);
        var content = new string('a', 1000);
        var query = "test";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert - should use max limit of 500, not the requested 600
        snippet.Should().HaveLength(503); // 500 + "..." = 503
    }

    [Fact]
    public void GenerateSnippet_WithCustomHighlightTags_UsesConfiguredTags()
    {
        // Arrange - AC-065: Highlight tags are configurable (HTML example)
        var customSettings = new SearchSettings
        {
            HighlightOpenTag = "<em>",
            HighlightCloseTag = "</em>"
        };
        var generator = new SnippetGenerator(customSettings);
        var content = "This is a test message";
        var query = "test";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert - should use <em> tags instead of default <mark>
        snippet.Should().Contain("<em>test</em>");
        snippet.Should().NotContain("<mark>");
    }

    [Fact]
    public void GenerateSnippet_WithAnsiHighlightTags_RendersColorCodes()
    {
        // Arrange - AC-065: Highlight tags support ANSI escape codes
        var ansiSettings = new SearchSettings
        {
            HighlightOpenTag = "\u001b[43m", // ANSI yellow background
            HighlightCloseTag = "\u001b[0m" // ANSI reset
        };
        var generator = new SnippetGenerator(ansiSettings);
        var content = "This is a test message";
        var query = "test";

        // Act
        var snippet = generator.GenerateSnippet(content, query);

        // Assert - should contain ANSI codes
        snippet.Should().Contain("\u001b[43mtest\u001b[0m");
    }
}
