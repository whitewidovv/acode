using Acode.Infrastructure.Search;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Search;

public class SafeQueryParserTests
{
    [Fact]
    public void ParseQuery_WithSimpleTerms_ReturnsTerms()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "test query example";

        // Act
        var parsed = parser.ParseQuery(query);

        // Assert
        parsed.Should().Be("test query example");
    }

    [Fact]
    public void ParseQuery_WithSpecialFts5Characters_EscapesThem()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "test* query OR example";

        // Act
        var parsed = parser.ParseQuery(query);

        // Assert

        // Should escape FTS5 operators to prevent injection
        parsed.Should().NotContain("OR");
        parsed.Should().NotContain("*");
    }

    [Fact]
    public void ParseQuery_WithQuotes_HandlesCorrectly()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "\"exact phrase\" test";

        // Act
        var parsed = parser.ParseQuery(query);

        // Assert

        // Should preserve quoted phrases or convert appropriately
        parsed.Should().Contain("exact");
        parsed.Should().Contain("phrase");
    }

    [Fact]
    public void ParseQuery_WithEmptyString_ReturnsEmpty()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = string.Empty;

        // Act
        var parsed = parser.ParseQuery(query);

        // Assert
        parsed.Should().BeEmpty();
    }

    [Fact]
    public void ParseQuery_WithWhitespaceOnly_ReturnsEmpty()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "   \t\n  ";

        // Act
        var parsed = parser.ParseQuery(query);

        // Assert
        parsed.Should().BeEmpty();
    }

    [Fact]
    public void ParseQuery_RemovesExcessWhitespace()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "test    query     example";

        // Act
        var parsed = parser.ParseQuery(query);

        // Assert

        // Should normalize whitespace
        parsed.Should().NotContain("  ");
        parsed.Should().Contain("test");
        parsed.Should().Contain("query");
        parsed.Should().Contain("example");
    }

    [Fact]
    public void ParseQuery_WithPunctuation_HandlesCorrectly()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "test, query! example?";

        // Act
        var parsed = parser.ParseQuery(query);

        // Assert

        // Punctuation should be handled appropriately
        parsed.Should().Contain("test");
        parsed.Should().Contain("query");
        parsed.Should().Contain("example");
    }

    [Fact]
    public void ParseQuery_PreservesAlphanumeric()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "test123 query456";

        // Act
        var parsed = parser.ParseQuery(query);

        // Assert
        parsed.Should().Contain("test123");
        parsed.Should().Contain("query456");
    }
}
