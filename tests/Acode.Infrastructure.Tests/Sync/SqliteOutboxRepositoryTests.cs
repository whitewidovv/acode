// tests/Acode.Infrastructure.Tests/Sync/SqliteOutboxRepositoryTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Sync;

using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using Acode.Domain.Sync;
using Acode.Infrastructure.Sync;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for SqliteOutboxRepository.
/// Verifies outbox repository operations for reliable sync delivery.
/// </summary>
public sealed class SqliteOutboxRepositoryTests : IDisposable
{
    private readonly SQLiteConnection _connection;
    private readonly SqliteOutboxRepository _sut;

    public SqliteOutboxRepositoryTests()
    {
        _connection = new SQLiteConnection("Data Source=:memory:");
        _connection.Open();

        // Create outbox table schema
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE outbox (
                id TEXT PRIMARY KEY,
                idempotency_key TEXT NOT NULL UNIQUE,
                entity_type TEXT NOT NULL,
                entity_id TEXT NOT NULL,
                operation TEXT NOT NULL,
                payload TEXT NOT NULL,
                status INTEGER NOT NULL,
                retry_count INTEGER NOT NULL DEFAULT 0,
                next_retry_at TEXT,
                processing_started_at TEXT,
                completed_at TEXT,
                created_at TEXT NOT NULL,
                last_error TEXT
            );

            CREATE INDEX idx_outbox_status ON outbox(status);
            CREATE INDEX idx_outbox_next_retry ON outbox(next_retry_at);
        ";
        command.ExecuteNonQuery();

        _sut = new SqliteOutboxRepository(_connection);
    }

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AddAsync_WithValidEntry_InsertsEntry()
    {
        // Arrange
        var entry = OutboxEntry.Create("Chat", "chat-123", "Insert", "{\"id\":\"chat-123\"}");

        // Act
        await _sut.AddAsync(entry, CancellationToken.None);

        // Assert
        var retrieved = await _sut.GetByIdAsync(entry.Id, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.EntityType.Should().Be("Chat");
        retrieved.EntityId.Should().Be("chat-123");
        retrieved.Operation.Should().Be("Insert");
        retrieved.Status.Should().Be(OutboxStatus.Pending);
    }

    [Fact]
    public async Task GetPendingAsync_WithPendingEntries_ReturnsEntries()
    {
        // Arrange
        var entry1 = OutboxEntry.Create("Chat", "chat-1", "Insert", "{}");
        var entry2 = OutboxEntry.Create("Chat", "chat-2", "Insert", "{}");
        var entry3 = OutboxEntry.Create("Chat", "chat-3", "Insert", "{}");

        await _sut.AddAsync(entry1, CancellationToken.None);
        await _sut.AddAsync(entry2, CancellationToken.None);
        await _sut.AddAsync(entry3, CancellationToken.None);

        // Act
        var pending = await _sut.GetPendingAsync(limit: 10, CancellationToken.None);

        // Assert
        pending.Should().HaveCount(3);
        pending.Should().AllSatisfy(e => e.Status.Should().Be(OutboxStatus.Pending));
    }

    [Fact]
    public async Task GetPendingAsync_RespectsLimit()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var entry = OutboxEntry.Create("Chat", $"chat-{i}", "Insert", "{}");
            await _sut.AddAsync(entry, CancellationToken.None);
        }

        // Act
        var pending = await _sut.GetPendingAsync(limit: 5, CancellationToken.None);

        // Assert
        pending.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPendingAsync_WithCompletedEntries_ExcludesThem()
    {
        // Arrange
        var pendingEntry = OutboxEntry.Create("Chat", "chat-1", "Insert", "{}");
        var completedEntry = OutboxEntry.Create("Chat", "chat-2", "Insert", "{}");
        completedEntry.MarkAsProcessing();
        completedEntry.MarkAsCompleted();

        await _sut.AddAsync(pendingEntry, CancellationToken.None);
        await _sut.AddAsync(completedEntry, CancellationToken.None);

        // Act
        var pending = await _sut.GetPendingAsync(limit: 10, CancellationToken.None);

        // Assert
        pending.Should().HaveCount(1);
        pending[0].Id.Should().Be(pendingEntry.Id);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntry()
    {
        // Arrange
        var entry = OutboxEntry.Create("Chat", "chat-123", "Insert", "{}");
        await _sut.AddAsync(entry, CancellationToken.None);

        // Act
        entry.MarkAsProcessing();
        await _sut.UpdateAsync(entry, CancellationToken.None);

        // Assert
        var retrieved = await _sut.GetByIdAsync(entry.Id, CancellationToken.None);
        retrieved!.Status.Should().Be(OutboxStatus.Processing);
        retrieved.ProcessingStartedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithRetryCount_TracksRetries()
    {
        // Arrange
        var entry = OutboxEntry.Create("Chat", "chat-123", "Insert", "{}");
        await _sut.AddAsync(entry, CancellationToken.None);

        // Act
        entry.MarkAsFailed("Network timeout");
        await _sut.UpdateAsync(entry, CancellationToken.None);

        // Assert
        var retrieved = await _sut.GetByIdAsync(entry.Id, CancellationToken.None);
        retrieved!.RetryCount.Should().Be(1);
        retrieved.LastError.Should().Be("Network timeout");
        retrieved.Status.Should().Be(OutboxStatus.Pending);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntry()
    {
        // Arrange
        var entry = OutboxEntry.Create("Chat", "chat-123", "Insert", "{}");
        await _sut.AddAsync(entry, CancellationToken.None);

        // Act
        await _sut.DeleteAsync(entry.Id, CancellationToken.None);

        // Assert
        var retrieved = await _sut.GetByIdAsync(entry.Id, CancellationToken.None);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var retrieved = await _sut.GetByIdAsync("non-existent-id", CancellationToken.None);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithDuplicateIdempotencyKey_ThrowsException()
    {
        // Arrange
        var entry1 = OutboxEntry.Create("Chat", "chat-123", "Insert", "{}");
        await _sut.AddAsync(entry1, CancellationToken.None);

        // Manually create entry with same idempotency key (bypassing OutboxEntry.Create which generates unique keys)
        var entry2 = OutboxEntry.Create("Chat", "chat-456", "Insert", "{}");

        // Use reflection to set the same idempotency key
        var idempotencyKeyProperty = typeof(OutboxEntry).GetProperty("IdempotencyKey");
        idempotencyKeyProperty!.SetValue(entry2, entry1.IdempotencyKey);

        // Act
        var act = async () => await _sut.AddAsync(entry2, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>("duplicate idempotency keys should be rejected");
    }

    [Fact]
    public async Task GetPendingAsync_OrdersByCreatedAtAscending()
    {
        // Arrange
        var entry1 = OutboxEntry.Create("Chat", "chat-1", "Insert", "{}");
        await Task.Delay(10); // Ensure different timestamps
        var entry2 = OutboxEntry.Create("Chat", "chat-2", "Insert", "{}");
        await Task.Delay(10);
        var entry3 = OutboxEntry.Create("Chat", "chat-3", "Insert", "{}");

        await _sut.AddAsync(entry2, CancellationToken.None); // Add in non-sequential order
        await _sut.AddAsync(entry1, CancellationToken.None);
        await _sut.AddAsync(entry3, CancellationToken.None);

        // Act
        var pending = await _sut.GetPendingAsync(limit: 10, CancellationToken.None);

        // Assert
        pending.Should().HaveCount(3);

        // First entry added has earliest CreatedAt
        pending[0].CreatedAt.Should().BeBefore(pending[1].CreatedAt);
        pending[1].CreatedAt.Should().BeBefore(pending[2].CreatedAt);
    }

    [Fact]
    public async Task GetPendingAsync_WithScheduledRetries_ReturnsOnlyReadyEntries()
    {
        // Arrange
        var readyEntry = OutboxEntry.Create("Chat", "chat-1", "Insert", "{}");
        var scheduledEntry = OutboxEntry.Create("Chat", "chat-2", "Insert", "{}");
        scheduledEntry.ScheduleRetry(TimeSpan.FromHours(1)); // Schedule for future

        await _sut.AddAsync(readyEntry, CancellationToken.None);
        await _sut.AddAsync(scheduledEntry, CancellationToken.None);

        // Act
        var pending = await _sut.GetPendingAsync(limit: 10, CancellationToken.None);

        // Assert
        pending.Should().HaveCount(1);
        pending[0].Id.Should().Be(readyEntry.Id);
    }
}
