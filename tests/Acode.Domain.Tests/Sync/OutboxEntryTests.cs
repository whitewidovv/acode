// tests/Acode.Domain.Tests/Sync/OutboxEntryTests.cs
namespace Acode.Domain.Tests.Sync;

using Acode.Domain.Sync;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for OutboxEntry domain entity.
/// Verifies outbox entry creation, status transitions, and retry logic.
/// </summary>
public sealed class OutboxEntryTests
{
    [Fact]
    public void Create_WithValidParameters_CreatesEntry()
    {
        // Arrange & Act
        var entry = OutboxEntry.Create(
            entityType: "Chat",
            entityId: "chat-123",
            operation: "Insert",
            payload: "{\"id\":\"chat-123\",\"title\":\"Test\"}");

        // Assert
        entry.Should().NotBeNull();
        entry.Id.Should().NotBeNull();
        entry.EntityType.Should().Be("Chat");
        entry.EntityId.Should().Be("chat-123");
        entry.Operation.Should().Be("Insert");
        entry.Payload.Should().Be("{\"id\":\"chat-123\",\"title\":\"Test\"}");
        entry.Status.Should().Be(OutboxStatus.Pending);
        entry.RetryCount.Should().Be(0);
        entry.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_GeneratesUniqueIdempotencyKey()
    {
        // Arrange & Act
        var entry1 = OutboxEntry.Create("Chat", "chat-123", "Insert", "{}");
        var entry2 = OutboxEntry.Create("Chat", "chat-123", "Insert", "{}");

        // Assert
        entry1.IdempotencyKey.Should().NotBeNullOrEmpty();
        entry2.IdempotencyKey.Should().NotBeNullOrEmpty();
        entry1.IdempotencyKey.Should().NotBe(entry2.IdempotencyKey, "each entry should have unique idempotency key");
    }

    [Fact]
    public void IdempotencyKey_IsUlidFormat()
    {
        // Arrange & Act
        var entry = OutboxEntry.Create("Chat", "chat-123", "Insert", "{}");

        // Assert
        entry.IdempotencyKey.Should().MatchRegex(@"^[0-9A-HJKMNP-TV-Z]{26}$", "should be ULID format (26 characters, base32)");
    }

    [Fact]
    public void MarkAsProcessing_UpdatesStatus()
    {
        // Arrange
        var entry = OutboxEntry.Create("Chat", "chat-123", "Insert", "{}");

        // Act
        entry.MarkAsProcessing();

        // Assert
        entry.Status.Should().Be(OutboxStatus.Processing);
        entry.ProcessingStartedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsCompleted_UpdatesStatus()
    {
        // Arrange
        var entry = OutboxEntry.Create("Chat", "chat-123", "Insert", "{}");
        entry.MarkAsProcessing();

        // Act
        entry.MarkAsCompleted();

        // Assert
        entry.Status.Should().Be(OutboxStatus.Completed);
        entry.CompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsFailed_IncrementsRetryCount()
    {
        // Arrange
        var entry = OutboxEntry.Create("Chat", "chat-123", "Insert", "{}");

        // Act
        entry.MarkAsFailed("Network timeout");

        // Assert
        entry.Status.Should().Be(OutboxStatus.Pending, "failed entries should return to pending for retry");
        entry.RetryCount.Should().Be(1);
        entry.LastError.Should().Be("Network timeout");
    }

    [Fact]
    public void MarkAsFailed_MultipleTimesTracks()
    {
        // Arrange
        var entry = OutboxEntry.Create("Chat", "chat-123", "Insert", "{}");

        // Act
        entry.MarkAsFailed("Error 1");
        entry.MarkAsFailed("Error 2");
        entry.MarkAsFailed("Error 3");

        // Assert
        entry.RetryCount.Should().Be(3);
        entry.LastError.Should().Be("Error 3");
    }

    [Fact]
    public void ScheduleRetry_SetsNextRetryTime()
    {
        // Arrange
        var entry = OutboxEntry.Create("Chat", "chat-123", "Insert", "{}");
        var delay = TimeSpan.FromSeconds(5);

        // Act
        entry.ScheduleRetry(delay);

        // Assert
        entry.NextRetryAt.Should().NotBeNull();
        entry.NextRetryAt.Should().BeCloseTo(DateTimeOffset.UtcNow.Add(delay), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsDeadLetter_UpdatesStatus()
    {
        // Arrange
        var entry = OutboxEntry.Create("Chat", "chat-123", "Insert", "{}");
        entry.MarkAsFailed("Error 1");
        entry.MarkAsFailed("Error 2");

        // Act
        entry.MarkAsDeadLetter("Max retries exceeded");

        // Assert
        entry.Status.Should().Be(OutboxStatus.DeadLetter);
        entry.LastError.Should().Be("Max retries exceeded");
    }

    [Fact]
    public void Create_WithNullEntityType_ThrowsException()
    {
        // Act
        var act = () => OutboxEntry.Create(null!, "chat-123", "Insert", "{}");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyEntityId_ThrowsException()
    {
        // Act
        var act = () => OutboxEntry.Create("Chat", string.Empty, "Insert", "{}");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullPayload_ThrowsException()
    {
        // Act
        var act = () => OutboxEntry.Create("Chat", "chat-123", "Insert", null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
