// tests/Acode.Infrastructure.Tests/Persistence/Conversation/SqliteRunRepositoryTests.cs
namespace Acode.Infrastructure.Tests.Persistence.Conversation;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Conversation.Persistence;
using Acode.Domain.Conversation;
using Acode.Infrastructure.Persistence.Conversation;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

public sealed class SqliteRunRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly IRunRepository _repository;

    public SqliteRunRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_runs_{Guid.NewGuid()}.db");
        _repository = new SqliteRunRepository(_dbPath);

        // Initialize schema
        var schemaPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Migrations",
            "001_InitialSchema.sql");
        var schema = File.ReadAllText(schemaPath);

        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = schema;
        cmd.ExecuteNonQuery();
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
    public async Task CreateAsync_ValidRun_ReturnsRunId()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        var run = Run.Create(chatId, "gpt-4", 1);

        // Act
        var result = await _repository.CreateAsync(run, CancellationToken.None);

        // Assert
        result.Should().Be(run.Id);
    }

    [Fact]
    public async Task CreateAsync_AndGetByIdAsync_RoundTrip_Success()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        var run = Run.Create(chatId, "gpt-4", 1);
        run.Complete(100, 200);

        await _repository.CreateAsync(run, CancellationToken.None);

        // Act
        var retrieved = await _repository.GetByIdAsync(run.Id, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(run.Id);
        retrieved.ChatId.Should().Be(chatId);
        retrieved.ModelId.Should().Be("gpt-4");
        retrieved.Status.Should().Be(RunStatus.Completed);
        retrieved.TokensIn.Should().Be(100);
        retrieved.TokensOut.Should().Be(200);
        retrieved.SequenceNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentRun_ReturnsNull()
    {
        // Arrange
        var nonExistentId = RunId.NewId();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ModifyStatus_PersistsChanges()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        var run = Run.Create(chatId, "gpt-4", 1);
        await _repository.CreateAsync(run, CancellationToken.None);

        // Reload and modify
        var loaded = await _repository.GetByIdAsync(run.Id, CancellationToken.None);
        loaded!.Complete(150, 250);

        // Act
        await _repository.UpdateAsync(loaded, CancellationToken.None);

        // Assert
        var retrieved = await _repository.GetByIdAsync(run.Id, CancellationToken.None);
        retrieved!.Status.Should().Be(RunStatus.Completed);
        retrieved.TokensIn.Should().Be(150);
        retrieved.TokensOut.Should().Be(250);
        retrieved.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ModifyErrorMessage_PersistsChanges()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        var run = Run.Create(chatId, "gpt-4", 1);
        await _repository.CreateAsync(run, CancellationToken.None);

        // Reload and modify
        var loaded = await _repository.GetByIdAsync(run.Id, CancellationToken.None);
        loaded!.Fail("Test error message");

        // Act
        await _repository.UpdateAsync(loaded, CancellationToken.None);

        // Assert
        var retrieved = await _repository.GetByIdAsync(run.Id, CancellationToken.None);
        retrieved!.Status.Should().Be(RunStatus.Failed);
        retrieved.ErrorMessage.Should().Be("Test error message");
    }

    [Fact]
    public async Task ListByChatAsync_ReturnsRunsOrderedBySequence()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        var run1 = Run.Create(chatId, "gpt-4", 1);
        var run2 = Run.Create(chatId, "gpt-3.5-turbo", 2);
        var run3 = Run.Create(chatId, "claude-3", 3);

        await _repository.CreateAsync(run1, CancellationToken.None);
        await _repository.CreateAsync(run3, CancellationToken.None); // Out of order
        await _repository.CreateAsync(run2, CancellationToken.None); // Out of order

        // Act
        var result = await _repository.ListByChatAsync(chatId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].SequenceNumber.Should().Be(1);
        result[1].SequenceNumber.Should().Be(2);
        result[2].SequenceNumber.Should().Be(3);
    }

    [Fact]
    public async Task ListByChatAsync_EmptyChat_ReturnsEmptyList()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        // Act
        var result = await _repository.ListByChatAsync(chatId, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListByChatAsync_MultipleChats_ReturnsOnlyRequestedChat()
    {
        // Arrange
        var chat1Id = ChatId.NewId();
        var chat2Id = ChatId.NewId();
        await CreateTestChatAsync(chat1Id);
        await CreateTestChatAsync(chat2Id);

        var run1 = Run.Create(chat1Id, "gpt-4", 1);
        var run2 = Run.Create(chat2Id, "gpt-4", 1);
        var run3 = Run.Create(chat1Id, "gpt-4", 2);

        await _repository.CreateAsync(run1, CancellationToken.None);
        await _repository.CreateAsync(run2, CancellationToken.None);
        await _repository.CreateAsync(run3, CancellationToken.None);

        // Act
        var result = await _repository.ListByChatAsync(chat1Id, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.ChatId.Should().Be(chat1Id));
    }

    [Fact]
    public async Task GetLatestAsync_SingleRun_ReturnsThatRun()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        var run = Run.Create(chatId, "gpt-4", 1);
        await _repository.CreateAsync(run, CancellationToken.None);

        // Act
        var result = await _repository.GetLatestAsync(chatId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(run.Id);
    }

    [Fact]
    public async Task GetLatestAsync_MultipleRuns_ReturnsHighestSequence()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        var run1 = Run.Create(chatId, "gpt-4", 1);
        var run2 = Run.Create(chatId, "gpt-4", 2);
        var run3 = Run.Create(chatId, "gpt-4", 3);

        await _repository.CreateAsync(run1, CancellationToken.None);
        await _repository.CreateAsync(run2, CancellationToken.None);
        await _repository.CreateAsync(run3, CancellationToken.None);

        // Act
        var result = await _repository.GetLatestAsync(chatId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SequenceNumber.Should().Be(3);
    }

    [Fact]
    public async Task GetLatestAsync_NoRuns_ReturnsNull()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        // Act
        var result = await _repository.GetLatestAsync(chatId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingRun_DeletesSuccessfully()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        var run = Run.Create(chatId, "gpt-4", 1);
        await _repository.CreateAsync(run, CancellationToken.None);

        // Act
        await _repository.DeleteAsync(run.Id, CancellationToken.None);

        // Assert
        var retrieved = await _repository.GetByIdAsync(run.Id, CancellationToken.None);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentRun_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = RunId.NewId();

        // Act
        Func<Task> act = async () => await _repository.DeleteAsync(nonExistentId, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateAsync_PreservesSyncStatus()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        var run = Run.Create(chatId, "gpt-4", 1);
        run.MarkSynced();

        // Act
        await _repository.CreateAsync(run, CancellationToken.None);

        // Assert
        var retrieved = await _repository.GetByIdAsync(run.Id, CancellationToken.None);
        retrieved!.SyncStatus.Should().Be(SyncStatus.Synced);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSyncStatus()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        var run = Run.Create(chatId, "gpt-4", 1);
        await _repository.CreateAsync(run, CancellationToken.None);

        var loaded = await _repository.GetByIdAsync(run.Id, CancellationToken.None);
        loaded!.MarkConflict();

        // Act
        await _repository.UpdateAsync(loaded, CancellationToken.None);

        // Assert
        var retrieved = await _repository.GetByIdAsync(run.Id, CancellationToken.None);
        retrieved!.SyncStatus.Should().Be(SyncStatus.Conflict);
    }

    [Fact]
    public async Task CreateAsync_WithCancelledStatus_PersistsCorrectly()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        var run = Run.Create(chatId, "gpt-4", 1);
        run.Cancel();

        // Act
        await _repository.CreateAsync(run, CancellationToken.None);

        // Assert
        var retrieved = await _repository.GetByIdAsync(run.Id, CancellationToken.None);
        retrieved!.Status.Should().Be(RunStatus.Cancelled);
    }

    [Fact]
    public async Task ListByChatAsync_WithFailedRuns_IncludesAllStatuses()
    {
        // Arrange
        var chatId = ChatId.NewId();
        await CreateTestChatAsync(chatId);

        var run1 = Run.Create(chatId, "gpt-4", 1);
        run1.Complete(100, 200);

        var run2 = Run.Create(chatId, "gpt-4", 2);
        run2.Fail("Error occurred");

        var run3 = Run.Create(chatId, "gpt-4", 3);
        run3.Cancel();

        await _repository.CreateAsync(run1, CancellationToken.None);
        await _repository.CreateAsync(run2, CancellationToken.None);
        await _repository.CreateAsync(run3, CancellationToken.None);

        // Act
        var result = await _repository.ListByChatAsync(chatId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].Status.Should().Be(RunStatus.Completed);
        result[1].Status.Should().Be(RunStatus.Failed);
        result[2].Status.Should().Be(RunStatus.Cancelled);
    }

    private async Task CreateTestChatAsync(ChatId chatId)
    {
        // Create a chat in the database for foreign key constraint
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO conv_chats (id, title, tags, worktree_id, is_deleted, deleted_at,
                              sync_status, version, created_at, updated_at)
            VALUES (@Id, @Title, '[]', NULL, 0, NULL, 'Pending', 1, @CreatedAt, @UpdatedAt)";

        cmd.Parameters.AddWithValue("@Id", chatId.Value);
        cmd.Parameters.AddWithValue("@Title", "Test Chat");
        cmd.Parameters.AddWithValue("@CreatedAt", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTimeOffset.UtcNow.ToString("O"));

        await cmd.ExecuteNonQueryAsync();
    }
}
