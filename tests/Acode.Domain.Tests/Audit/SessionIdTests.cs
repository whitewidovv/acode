namespace Acode.Domain.Tests.Audit;

using Acode.Domain.Audit;
using FluentAssertions;

/// <summary>
/// Tests for SessionId value object.
/// Spec requirement: SessionId.Value must match pattern ^sess_[a-zA-Z0-9]+$.
/// </summary>
public class SessionIdTests
{
    [Fact]
    public void New_ShouldGenerateUniqueSessionIds()
    {
        // Arrange & Act
        var sessionId1 = SessionId.New();
        var sessionId2 = SessionId.New();

        // Assert
        sessionId1.Value.Should().NotBe(
            sessionId2.Value,
            because: "each SessionId must be unique");
    }

    [Fact]
    public void New_ShouldMatchRequiredFormat()
    {
        // Arrange & Act
        var sessionId = SessionId.New();

        // Assert - From spec line 917-918: must match ^sess_[a-zA-Z0-9]+$
        sessionId.Value.Should().MatchRegex(
            @"^sess_[a-zA-Z0-9]+$",
            because: "SessionId must follow sess_xxx format per FR-003c spec line 917-918");
    }

    [Fact]
    public void New_ShouldHaveSessPrefix()
    {
        // Arrange & Act
        var sessionId = SessionId.New();

        // Assert
        sessionId.Value.Should().StartWith(
            "sess_",
            because: "SessionId must have sess_ prefix");
    }

    [Fact]
    public void Constructor_ShouldAcceptValidFormat()
    {
        // Arrange
        var validValue = "sess_abc123XYZ";

        // Act
        var sessionId = new SessionId(validValue);

        // Assert
        sessionId.Value.Should().Be(validValue);
    }

    [Fact]
    public void Constructor_ShouldRejectNullOrWhitespace()
    {
        // Arrange & Act & Assert
        Action nullAction = () => new SessionId(null!);
        Action emptyAction = () => new SessionId(string.Empty);
        Action whitespaceAction = () => new SessionId("   ");

        nullAction.Should().Throw<ArgumentException>();
        emptyAction.Should().Throw<ArgumentException>();
        whitespaceAction.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldRejectInvalidFormat()
    {
        // Arrange - various invalid formats
        var invalidFormats = new[]
        {
            "sess",                   // Missing suffix
            "sess_",                  // Empty suffix
            "session_abc123",         // Wrong prefix
            "abc123",                 // No prefix
            "sess abc123",            // Space instead of underscore
            "sess_abc 123",           // Space in suffix
            "sess_abc-123",           // Dash not allowed
        };

        // Act & Assert
        foreach (var invalid in invalidFormats)
        {
            Action action = () => new SessionId(invalid);
            action.Should().Throw<ArgumentException>(
                because: $"'{invalid}' does not match required format sess_[a-zA-Z0-9]+");
        }
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var sessionId = new SessionId("sess_test123");

        // Act
        var result = sessionId.ToString();

        // Assert
        result.Should().Be("sess_test123");
    }

    [Fact]
    public void ValueObjects_ShouldBeEqual_WhenValuesAreEqual()
    {
        // Arrange
        var id1 = new SessionId("sess_test123");
        var id2 = new SessionId("sess_test123");

        // Act & Assert
        id1.Should().Be(
            id2,
            because: "records with same value should be equal");
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void ValueObjects_ShouldNotBeEqual_WhenValuesDiffer()
    {
        // Arrange
        var id1 = new SessionId("sess_test123");
        var id2 = new SessionId("sess_test456");

        // Act & Assert
        id1.Should().NotBe(
            id2,
            because: "records with different values should not be equal");
        (id1 != id2).Should().BeTrue();
    }
}
