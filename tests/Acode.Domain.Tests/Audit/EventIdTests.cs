namespace Acode.Domain.Tests.Audit;

using Acode.Domain.Audit;
using FluentAssertions;

/// <summary>
/// Tests for EventId value object.
/// Spec requirement: EventId.Value must match pattern ^evt_[a-zA-Z0-9]+$.
/// </summary>
public class EventIdTests
{
    [Fact]
    public void New_ShouldGenerateUniqueEventIds()
    {
        // Arrange & Act
        var eventId1 = EventId.New();
        var eventId2 = EventId.New();

        // Assert
        eventId1.Value.Should().NotBe(
            eventId2.Value,
            because: "each EventId must be unique");
    }

    [Fact]
    public void New_ShouldMatchRequiredFormat()
    {
        // Arrange & Act
        var eventId = EventId.New();

        // Assert - From spec line 873-874: must match ^evt_[a-zA-Z0-9]+$
        eventId.Value.Should().MatchRegex(
            @"^evt_[a-zA-Z0-9]+$",
            because: "EventId must follow evt_xxx format per FR-003c spec line 873-874");
    }

    [Fact]
    public void New_ShouldHaveEvtPrefix()
    {
        // Arrange & Act
        var eventId = EventId.New();

        // Assert
        eventId.Value.Should().StartWith(
            "evt_",
            because: "EventId must have evt_ prefix");
    }

    [Fact]
    public void Constructor_ShouldAcceptValidFormat()
    {
        // Arrange
        var validValue = "evt_abc123XYZ";

        // Act
        var eventId = new EventId(validValue);

        // Assert
        eventId.Value.Should().Be(validValue);
    }

    [Fact]
    public void Constructor_ShouldRejectNullOrWhitespace()
    {
        // Arrange & Act & Assert
        Action nullAction = () => new EventId(null!);
        Action emptyAction = () => new EventId(string.Empty);
        Action whitespaceAction = () => new EventId("   ");

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
            "evt",                    // Missing suffix
            "evt_",                   // Empty suffix
            "event_abc123",           // Wrong prefix
            "abc123",                 // No prefix
            "evt abc123",             // Space instead of underscore
            "evt_abc 123",            // Space in suffix
            "evt_abc-123",            // Dash not allowed
            "evt_abc_123",            // Extra underscore
        };

        // Act & Assert
        foreach (var invalid in invalidFormats)
        {
            Action action = () => new EventId(invalid);
            action.Should().Throw<ArgumentException>(
                because: $"'{invalid}' does not match required format evt_[a-zA-Z0-9]+");
        }
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var eventId = new EventId("evt_test123");

        // Act
        var result = eventId.ToString();

        // Assert
        result.Should().Be("evt_test123");
    }

    [Fact]
    public void ValueObjects_ShouldBeEqual_WhenValuesAreEqual()
    {
        // Arrange
        var id1 = new EventId("evt_test123");
        var id2 = new EventId("evt_test123");

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
        var id1 = new EventId("evt_test123");
        var id2 = new EventId("evt_test456");

        // Act & Assert
        id1.Should().NotBe(
            id2,
            because: "records with different values should not be equal");
        (id1 != id2).Should().BeTrue();
    }
}
