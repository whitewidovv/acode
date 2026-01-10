using Acode.Domain.Search;
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
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Fts5Syntax.Should().Be("test OR query OR example"); // Implicit OR
        result.OperatorCount.Should().Be(2); // Two OR operators
    }

    [Fact]
    public void ParseQuery_WithSpecialFts5Characters_EscapesThem()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "test* query^ example";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();

        // Special chars like * and ^ should be stripped, leaving clean terms
        result.Fts5Syntax.Should().NotContain("*");
        result.Fts5Syntax.Should().NotContain("^");
        result.Fts5Syntax.Should().Contain("test");
        result.Fts5Syntax.Should().Contain("query");
        result.Fts5Syntax.Should().Contain("example");
    }

    [Fact]
    public void ParseQuery_WithQuotes_HandlesCorrectly()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "\"exact phrase\" test";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();

        // Should preserve quoted phrases in FTS5 syntax
        result.Fts5Syntax.Should().Contain("\"exact phrase\"");
        result.Fts5Syntax.Should().Contain("test");
    }

    [Fact]
    public void ParseQuery_WithEmptyString_ReturnsEmpty()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = string.Empty;

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Fts5Syntax.Should().BeEmpty();
        result.OperatorCount.Should().Be(0);
    }

    [Fact]
    public void ParseQuery_WithWhitespaceOnly_ReturnsEmpty()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "   \t\n  ";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Fts5Syntax.Should().BeEmpty();
        result.OperatorCount.Should().Be(0);
    }

    [Fact]
    public void ParseQuery_RemovesExcessWhitespace()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "test    query     example";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();

        // Should normalize whitespace
        result.Fts5Syntax.Should().NotContain("  ");
        result.Fts5Syntax.Should().Contain("test");
        result.Fts5Syntax.Should().Contain("query");
        result.Fts5Syntax.Should().Contain("example");
    }

    [Fact]
    public void ParseQuery_WithPunctuation_HandlesCorrectly()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "test, query! example?";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();

        // Punctuation should be handled appropriately
        result.Fts5Syntax.Should().Contain("test");
        result.Fts5Syntax.Should().Contain("query");
        result.Fts5Syntax.Should().Contain("example");
    }

    [Fact]
    public void ParseQuery_PreservesAlphanumeric()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "test123 query456";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Fts5Syntax.Should().Contain("test123");
        result.Fts5Syntax.Should().Contain("query456");
    }

    // NEW TESTS FOR BOOLEAN OPERATORS (P2.1)

    [Fact]
    public void ParseQuery_WithAND_ReturnsValidFtsQuery()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "JWT AND validation";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Fts5Syntax.Should().Be("JWT AND validation");
        result.OperatorCount.Should().Be(1);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_WithOR_ReturnsValidFtsQuery()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "authentication OR OAuth";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Fts5Syntax.Should().Be("authentication OR OAuth");
        result.OperatorCount.Should().Be(1);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_WithNOT_ReturnsValidFtsQuery()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "token NOT expired";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Fts5Syntax.Should().Be("token NOT expired");
        result.OperatorCount.Should().Be(1);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_WithParentheses_ReturnsValidFtsQuery()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "(JWT OR OAuth) AND validation";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Fts5Syntax.Should().Be("(JWT OR OAuth) AND validation");
        result.OperatorCount.Should().Be(2);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_ImplicitOR_ConvertsToExplicitOR()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "JWT validation";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Fts5Syntax.Should().Be("JWT OR validation");
        result.OperatorCount.Should().Be(1);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_WithPhrase_PreservesQuotes()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "\"JWT authentication\" AND validation";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Fts5Syntax.Should().Contain("\"JWT authentication\"");
        result.Fts5Syntax.Should().Contain("AND");
        result.Fts5Syntax.Should().Contain("validation");
        result.OperatorCount.Should().Be(1);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_MoreThan5Operators_ReturnsInvalid()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "a AND b OR c AND d NOT e OR f AND g";  // 6 operators

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("maximum 5");
        result.OperatorCount.Should().Be(6);
    }

    [Fact]
    public void ParseQuery_UnbalancedParentheses_ReturnsInvalid()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "(JWT AND validation";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("unbalanced");
    }

    [Fact]
    public void ParseQuery_LeadingOperator_ReturnsInvalid()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "AND JWT validation";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cannot start with");
    }

    [Fact]
    public void ParseQuery_TrailingOperator_ReturnsInvalid()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "JWT AND";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cannot end with");
    }

    [Fact]
    public void ParseQuery_CaseInsensitiveOperators_Recognized()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "jwt and validation";  // lowercase

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Fts5Syntax.Should().Be("jwt AND validation");  // Normalized to uppercase
        result.OperatorCount.Should().Be(1);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_ComplexNested_ParsesCorrectly()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "((auth OR oauth) AND (token NOT expired)) OR jwt";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Fts5Syntax.Should().Be("((auth OR oauth) AND (token NOT expired)) OR jwt");
        result.OperatorCount.Should().Be(4); // OR, AND, NOT, OR
        result.ErrorMessage.Should().BeNull();
    }
}
