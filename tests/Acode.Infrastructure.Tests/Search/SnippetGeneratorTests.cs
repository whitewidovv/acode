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
}
