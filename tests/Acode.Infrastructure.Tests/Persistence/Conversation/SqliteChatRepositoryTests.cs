// tests/Acode.Infrastructure.Tests/Persistence/Conversation/SqliteChatRepositoryTests.cs
namespace Acode.Infrastructure.Tests.Persistence.Conversation;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Conversation.Persistence;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;
using Acode.Infrastructure.Persistence.Conversation;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

/// <summary>
/// Integration tests for SqliteChatRepository using in-memory SQLite database.
/// </summary>
public sealed class SqliteChatRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteChatRepository _repository;

    public SqliteChatRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.db");
        _repository = new SqliteChatRepository(_dbPath);
        InitializeDatabase();
    }

    public void Dispose()
    {
        // Clear SQLite connection pool to release file locks
        SqliteConnection.ClearAllPools();

        // Small delay to ensure connections are released
        Thread.Sleep(50);

        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch (IOException)
            {
                // Ignore if file still locked - will be cleaned up by OS
            }
        }
    }

    [Fact]
    public async Task CreateAsync_ValidChat_ReturnsId()
    {
        // Arrange
        var chat = Chat.Create("Test Chat");

        // Act
        var id = await _repository.CreateAsync(chat, CancellationToken.None);

        // Assert
        id.Should().Be(chat.Id);
    }

    [Fact]
    public async Task CreateAsync_AndGetByIdAsync_RoundTrip_Success()
    {
        // Arrange
        var worktreeId = WorktreeId.From(Acode.Domain.Common.Ulid.NewUlid());
        var chat = Chat.Create("Integration Test Chat", worktreeId);
        chat.AddTag("test");
        chat.AddTag("integration");

        // Act
        await _repository.CreateAsync(chat, CancellationToken.None);
        var retrieved = await _repository.GetByIdAsync(chat.Id, false, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(chat.Id);
        retrieved.Title.Should().Be("Integration Test Chat");
        retrieved.WorktreeBinding.Should().Be(worktreeId);
        retrieved.Tags.Should().HaveCount(2);
        retrieved.Tags.Should().Contain("test");
        retrieved.Tags.Should().Contain("integration");
        retrieved.IsDeleted.Should().BeFalse();
        retrieved.DeletedAt.Should().BeNull();
        retrieved.SyncStatus.Should().Be(SyncStatus.Pending);
        retrieved.Version.Should().Be(3); // Version incremented by AddTag calls (1 + 2 tags = 3)
    }

    [Fact]
    public async Task GetByIdAsync_NonexistentId_ReturnsNull()
    {
        // Arrange
        var nonexistentId = ChatId.NewId();

        // Act
        var result = await _repository.GetByIdAsync(nonexistentId, false, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ValidChat_UpdatesSuccessfully()
    {
        // Arrange
        var chat = Chat.Create("Original Title");
        await _repository.CreateAsync(chat, CancellationToken.None);

        // Act
        chat.UpdateTitle("Updated Title");
        await _repository.UpdateAsync(chat, CancellationToken.None);

        // Assert
        var retrieved = await _repository.GetByIdAsync(chat.Id, false, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Updated Title");
        retrieved.Version.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_ConcurrentModification_ThrowsConcurrencyException()
    {
        // Arrange
        var chat = Chat.Create("Concurrency Test");
        await _repository.CreateAsync(chat, CancellationToken.None);

        // Simulate concurrent modification by updating directly in DB
        using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE conv_chats SET version = 2 WHERE id = @id";
            command.Parameters.AddWithValue("@id", chat.Id.Value);
            await command.ExecuteNonQueryAsync();
        }

        // Act
        chat.UpdateTitle("This should fail");
        var act = async () => await _repository.UpdateAsync(chat, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConcurrencyException>()
            .WithMessage("*modified by another process*");
    }

    [Fact]
    public async Task SoftDeleteAsync_ExistingChat_MarksAsDeleted()
    {
        // Arrange
        var chat = Chat.Create("Chat to Delete");
        await _repository.CreateAsync(chat, CancellationToken.None);

        // Act
        await _repository.SoftDeleteAsync(chat.Id, CancellationToken.None);

        // Assert
        var retrieved = await _repository.GetByIdAsync(chat.Id, false, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.IsDeleted.Should().BeTrue();
        retrieved.DeletedAt.Should().NotBeNull();
        retrieved.DeletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SoftDeleteAsync_AlreadyDeleted_IsIdempotent()
    {
        // Arrange
        var chat = Chat.Create("Already Deleted");
        await _repository.CreateAsync(chat, CancellationToken.None);
        await _repository.SoftDeleteAsync(chat.Id, CancellationToken.None);

        // Act - delete again
        await _repository.SoftDeleteAsync(chat.Id, CancellationToken.None);

        // Assert - should not throw
        var retrieved = await _repository.GetByIdAsync(chat.Id, false, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task ListAsync_NoFilter_ReturnsAllChats()
    {
        // Arrange
        await _repository.CreateAsync(Chat.Create("Chat 1"), CancellationToken.None);
        await _repository.CreateAsync(Chat.Create("Chat 2"), CancellationToken.None);
        await _repository.CreateAsync(Chat.Create("Chat 3"), CancellationToken.None);

        var filter = new ChatFilter { PageSize = 10 };

        // Act
        var result = await _repository.ListAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
        result.TotalPages.Should().Be(1);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task ListAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            await _repository.CreateAsync(Chat.Create($"Chat {i}"), CancellationToken.None);
        }

        var filter = new ChatFilter { Page = 1, PageSize = 3 };

        // Act
        var result = await _repository.ListAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(10);
        result.Items.Should().HaveCount(3);
        result.TotalPages.Should().Be(4);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListAsync_ExcludeDeleted_FiltersCorrectly()
    {
        // Arrange
        var chat1 = Chat.Create("Active Chat");
        var chat2 = Chat.Create("Deleted Chat");
        await _repository.CreateAsync(chat1, CancellationToken.None);
        await _repository.CreateAsync(chat2, CancellationToken.None);
        await _repository.SoftDeleteAsync(chat2.Id, CancellationToken.None);

        var filter = new ChatFilter { IncludeDeleted = false };

        // Act
        var result = await _repository.ListAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Active Chat");
    }

    [Fact]
    public async Task ListAsync_IncludeDeleted_ReturnsAll()
    {
        // Arrange
        var chat1 = Chat.Create("Active Chat");
        var chat2 = Chat.Create("Deleted Chat");
        await _repository.CreateAsync(chat1, CancellationToken.None);
        await _repository.CreateAsync(chat2, CancellationToken.None);
        await _repository.SoftDeleteAsync(chat2.Id, CancellationToken.None);

        var filter = new ChatFilter { IncludeDeleted = true };

        // Act
        var result = await _repository.ListAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAsync_FilterByWorktree_ReturnsMatchingChats()
    {
        // Arrange
        var worktree1 = WorktreeId.From(Acode.Domain.Common.Ulid.NewUlid());
        var worktree2 = WorktreeId.From(Acode.Domain.Common.Ulid.NewUlid());
        await _repository.CreateAsync(Chat.Create("Chat 1", worktree1), CancellationToken.None);
        await _repository.CreateAsync(Chat.Create("Chat 2", worktree2), CancellationToken.None);
        await _repository.CreateAsync(Chat.Create("Chat 3", worktree1), CancellationToken.None);

        var filter = new ChatFilter { WorktreeId = worktree1 };

        // Act
        var result = await _repository.ListAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(c => c.WorktreeBinding == worktree1);
    }

    [Fact]
    public async Task ListAsync_FilterByCreatedAfter_ReturnsMatchingChats()
    {
        // Arrange
        await _repository.CreateAsync(Chat.Create("Old Chat"), CancellationToken.None);
        await Task.Delay(100); // Ensure timestamp difference

        var cutoffDate = DateTimeOffset.UtcNow; // Set cutoff AFTER old chat created

        var newChat = Chat.Create("New Chat");
        await _repository.CreateAsync(newChat, CancellationToken.None);

        var filter = new ChatFilter { CreatedAfter = cutoffDate };

        // Act
        var result = await _repository.ListAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items[0].Title.Should().Be("New Chat");
    }

    [Fact]
    public async Task ListAsync_FilterByCreatedBefore_ReturnsMatchingChats()
    {
        // Arrange
        var oldChat = Chat.Create("Old Chat");
        await _repository.CreateAsync(oldChat, CancellationToken.None);

        await Task.Delay(100); // Ensure timestamp difference

        var cutoffDate = DateTimeOffset.UtcNow; // Set cutoff AFTER old chat, BEFORE new chat

        await Task.Delay(100); // Ensure timestamp difference

        await _repository.CreateAsync(Chat.Create("New Chat"), CancellationToken.None);

        var filter = new ChatFilter { CreatedBefore = cutoffDate };

        // Act
        var result = await _repository.ListAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items[0].Title.Should().Be("Old Chat");
    }

    [Fact]
    public async Task GetByWorktreeAsync_ReturnsMatchingChats()
    {
        // Arrange
        var worktree = WorktreeId.From(Acode.Domain.Common.Ulid.NewUlid());
        await _repository.CreateAsync(Chat.Create("Chat 1", worktree), CancellationToken.None);
        await _repository.CreateAsync(Chat.Create("Chat 2", worktree), CancellationToken.None);
        await _repository.CreateAsync(Chat.Create("Chat 3", WorktreeId.From(Acode.Domain.Common.Ulid.NewUlid())), CancellationToken.None);

        // Act
        var result = await _repository.GetByWorktreeAsync(worktree, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.WorktreeBinding == worktree);
    }

    [Fact]
    public async Task GetByWorktreeAsync_ExcludesDeletedChats()
    {
        // Arrange
        var worktree = WorktreeId.From(Acode.Domain.Common.Ulid.NewUlid());
        var chat1 = Chat.Create("Active Chat", worktree);
        var chat2 = Chat.Create("Deleted Chat", worktree);
        await _repository.CreateAsync(chat1, CancellationToken.None);
        await _repository.CreateAsync(chat2, CancellationToken.None);
        await _repository.SoftDeleteAsync(chat2.Id, CancellationToken.None);

        // Act
        var result = await _repository.GetByWorktreeAsync(worktree, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Active Chat");
    }

    [Fact]
    public async Task PurgeDeletedAsync_RemovesOldDeletedChats()
    {
        // Arrange
        var oldChat = Chat.Create("Old Deleted Chat");
        var recentChat = Chat.Create("Recent Deleted Chat");
        await _repository.CreateAsync(oldChat, CancellationToken.None);
        await _repository.CreateAsync(recentChat, CancellationToken.None);

        // Manually set old deleted_at timestamp
        using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE conv_chats
                SET is_deleted = 1, deleted_at = @OldDate
                WHERE id = @OldId";
            command.Parameters.AddWithValue("@OldId", oldChat.Id.Value);
            command.Parameters.AddWithValue("@OldDate", DateTimeOffset.UtcNow.AddDays(-31).ToString("O"));
            await command.ExecuteNonQueryAsync();
        }

        await _repository.SoftDeleteAsync(recentChat.Id, CancellationToken.None);

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-30);

        // Act
        var purgedCount = await _repository.PurgeDeletedAsync(cutoffDate, CancellationToken.None);

        // Assert
        purgedCount.Should().Be(1);

        var remaining = await _repository.ListAsync(
            new ChatFilter { IncludeDeleted = true },
            CancellationToken.None);
        remaining.TotalCount.Should().Be(1);
        remaining.Items[0].Id.Should().Be(recentChat.Id);
    }

    [Fact]
    public async Task PurgeDeletedAsync_NoOldChats_ReturnsZero()
    {
        // Arrange
        var chat = Chat.Create("Recent Deleted Chat");
        await _repository.CreateAsync(chat, CancellationToken.None);
        await _repository.SoftDeleteAsync(chat.Id, CancellationToken.None);

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-30);

        // Act
        var purgedCount = await _repository.PurgeDeletedAsync(cutoffDate, CancellationToken.None);

        // Assert
        purgedCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_ChatWithTags_PreservesTags()
    {
        // Arrange
        var chat = Chat.Create("Tagged Chat");
        chat.AddTag("feature");
        chat.AddTag("bugfix");
        chat.AddTag("urgent");

        // Act
        await _repository.CreateAsync(chat, CancellationToken.None);
        var retrieved = await _repository.GetByIdAsync(chat.Id, false, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Tags.Should().HaveCount(3);
        retrieved.Tags.Should().Contain(new[] { "feature", "bugfix", "urgent" });
    }

    [Fact]
    public async Task UpdateAsync_ModifyTags_PersistsChanges()
    {
        // Arrange
        var chat = Chat.Create("Chat with Tags");
        chat.AddTag("initial");
        await _repository.CreateAsync(chat, CancellationToken.None);

        // Reload to get fresh copy with correct version tracking
        var loaded = await _repository.GetByIdAsync(chat.Id, false, CancellationToken.None);

        // Act - single modification on loaded entity
        loaded!.AddTag("added");
        await _repository.UpdateAsync(loaded, CancellationToken.None);

        // Assert
        var retrieved = await _repository.GetByIdAsync(chat.Id, false, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.Tags.Should().HaveCount(2);
        retrieved.Tags.Should().Contain("initial");
        retrieved.Tags.Should().Contain("added");
    }

    [Fact]
    public async Task ListAsync_OrdersByUpdatedAtDescending()
    {
        // Arrange
        var chat1 = Chat.Create("Chat 1");
        var chat2 = Chat.Create("Chat 2");
        var chat3 = Chat.Create("Chat 3");

        await _repository.CreateAsync(chat1, CancellationToken.None);
        await Task.Delay(50);
        await _repository.CreateAsync(chat2, CancellationToken.None);
        await Task.Delay(50);
        await _repository.CreateAsync(chat3, CancellationToken.None);

        // Update chat1 to make it most recent
        chat1.UpdateTitle("Chat 1 Updated");
        await _repository.UpdateAsync(chat1, CancellationToken.None);

        var filter = new ChatFilter();

        // Act
        var result = await _repository.ListAsync(filter, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Id.Should().Be(chat1.Id); // Most recently updated
        result.Items[1].Id.Should().Be(chat3.Id);
        result.Items[2].Id.Should().Be(chat2.Id);
    }

    private void InitializeDatabase()
    {
        var schemaPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Migrations",
            "001_InitialSchema.sql");

        var schema = File.ReadAllText(schemaPath);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = schema;
        command.ExecuteNonQuery();
    }
}
