using Acode.Domain.Models.Inference;
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

    // NEW TESTS FOR FIELD-SPECIFIC QUERY SYNTAX (P3.1)
    [Fact]
    public void ParseQuery_WithRoleUserPrefix_ExtractsRoleFilter()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "role:user authentication";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.RoleFilter.Should().Be(MessageRole.User);
        result.Fts5Syntax.Should().Be("authentication");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_WithRoleAssistantPrefix_ExtractsRoleFilter()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "role:assistant JWT";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.RoleFilter.Should().Be(MessageRole.Assistant);
        result.Fts5Syntax.Should().Be("JWT");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_WithChatNamePrefix_ExtractsChatFilter()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "chat:auth-discussion error";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ChatNameFilter.Should().Be("auth-discussion");
        result.Fts5Syntax.Should().Be("error");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_WithTitlePrefix_ExtractsTitleTerms()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "title:JWT title:authentication";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.TitleTerms.Should().BeEquivalentTo(new[] { "JWT", "authentication" });
        result.Fts5Syntax.Should().BeEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_WithTagPrefix_ExtractsTagFilter()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "tag:security vulnerability";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.TagFilter.Should().Be("security");
        result.Fts5Syntax.Should().Be("vulnerability");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_MultipleFieldPrefixes_ExtractsAll()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "role:user chat:test tag:bug error message";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.RoleFilter.Should().Be(MessageRole.User);
        result.ChatNameFilter.Should().Be("test");
        result.TagFilter.Should().Be("bug");
        result.Fts5Syntax.Should().Be("error OR message");
        result.OperatorCount.Should().Be(1);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_FieldPrefixWithBooleanOps_ParsesBoth()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "role:user (JWT AND validation)";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.RoleFilter.Should().Be(MessageRole.User);
        result.Fts5Syntax.Should().Be("(JWT AND validation)");
        result.OperatorCount.Should().Be(1);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParseQuery_InvalidRoleValue_ReturnsInvalid()
    {
        // Arrange
        var parser = new SafeQueryParser();
        var query = "role:invalid JWT";

        // Act
        var result = parser.ParseQuery(query);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("invalid role");
        result.ErrorMessage.Should().Contain("user, assistant, system, or tool");
    }
}
