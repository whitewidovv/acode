// tests/Acode.Infrastructure.Tests/Persistence/Conversation/SqliteMessageRepositoryTests.cs
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

public sealed class SqliteMessageRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly IMessageRepository _repository;

    public SqliteMessageRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_messages_{Guid.NewGuid()}.db");
        _repository = new SqliteMessageRepository(_dbPath);

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
    public async Task CreateAsync_ValidMessage_ReturnsMessageId()
    {
        // Arrange
        var (chatId, runId) = await CreateTestChatAndRunAsync();
        var message = Message.Create(runId, "user", "Hello world", 1);

        // Act
        var result = await _repository.CreateAsync(message, CancellationToken.None);

        // Assert
        result.Should().Be(message.Id);
    }

    [Fact]
    public async Task CreateAsync_AndGetByIdAsync_RoundTrip_Success()
    {
        // Arrange
        var (chatId, runId) = await CreateTestChatAndRunAsync();
        var message = Message.Create(runId, "assistant", "Hello! How can I help?", 1);

        await _repository.CreateAsync(message, CancellationToken.None);

        // Act
        var retrieved = await _repository.GetByIdAsync(message.Id, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(message.Id);
        retrieved.RunId.Should().Be(runId);
        retrieved.Role.Should().Be("assistant");
        retrieved.Content.Should().Be("Hello! How can I help?");
        retrieved.SequenceNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentMessage_ReturnsNull()
    {
        // Arrange
        var nonExistentId = MessageId.NewId();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ModifySyncStatus_PersistsChanges()
    {
        // Arrange
        var (chatId, runId) = await CreateTestChatAndRunAsync();
        var message = Message.Create(runId, "user", "Test message", 1);
        await _repository.CreateAsync(message, CancellationToken.None);

        // Reload and modify
        var loaded = await _repository.GetByIdAsync(message.Id, CancellationToken.None);
        loaded!.MarkSynced();

        // Act
        await _repository.UpdateAsync(loaded, CancellationToken.None);

        // Assert
        var retrieved = await _repository.GetByIdAsync(message.Id, CancellationToken.None);
        retrieved!.SyncStatus.Should().Be(SyncStatus.Synced);
    }

    [Fact]
    public async Task ListByRunAsync_ReturnsMessagesOrderedBySequence()
    {
        // Arrange
        var (chatId, runId) = await CreateTestChatAndRunAsync();

        var msg1 = Message.Create(runId, "user", "First message", 1);
        var msg2 = Message.Create(runId, "assistant", "Second message", 2);
        var msg3 = Message.Create(runId, "user", "Third message", 3);

        await _repository.CreateAsync(msg1, CancellationToken.None);
        await _repository.CreateAsync(msg3, CancellationToken.None); // Out of order
        await _repository.CreateAsync(msg2, CancellationToken.None); // Out of order

        // Act
        var result = await _repository.ListByRunAsync(runId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].SequenceNumber.Should().Be(1);
        result[0].Content.Should().Be("First message");
        result[1].SequenceNumber.Should().Be(2);
        result[1].Content.Should().Be("Second message");
        result[2].SequenceNumber.Should().Be(3);
        result[2].Content.Should().Be("Third message");
    }

    [Fact]
    public async Task ListByRunAsync_EmptyRun_ReturnsEmptyList()
    {
        // Arrange
        var (chatId, runId) = await CreateTestChatAndRunAsync();

        // Act
        var result = await _repository.ListByRunAsync(runId, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListByRunAsync_MultipleRuns_ReturnsOnlyRequestedRun()
    {
        // Arrange
        var (chat1Id, run1Id) = await CreateTestChatAndRunAsync();
        var (chat2Id, run2Id) = await CreateTestChatAndRunAsync();

        var msg1 = Message.Create(run1Id, "user", "Run 1 Message 1", 1);
        var msg2 = Message.Create(run2Id, "user", "Run 2 Message 1", 1);
        var msg3 = Message.Create(run1Id, "user", "Run 1 Message 2", 2);

        await _repository.CreateAsync(msg1, CancellationToken.None);
        await _repository.CreateAsync(msg2, CancellationToken.None);
        await _repository.CreateAsync(msg3, CancellationToken.None);

        // Act
        var result = await _repository.ListByRunAsync(run1Id, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(m => m.RunId.Should().Be(run1Id));
    }

    [Fact]
    public async Task DeleteByRunAsync_DeletesAllMessagesForRun()
    {
        // Arrange
        var (chatId, runId) = await CreateTestChatAndRunAsync();

        var msg1 = Message.Create(runId, "user", "Message 1", 1);
        var msg2 = Message.Create(runId, "assistant", "Message 2", 2);
        var msg3 = Message.Create(runId, "user", "Message 3", 3);

        await _repository.CreateAsync(msg1, CancellationToken.None);
        await _repository.CreateAsync(msg2, CancellationToken.None);
        await _repository.CreateAsync(msg3, CancellationToken.None);

        // Act
        await _repository.DeleteByRunAsync(runId, CancellationToken.None);

        // Assert
        var retrieved1 = await _repository.GetByIdAsync(msg1.Id, CancellationToken.None);
        var retrieved2 = await _repository.GetByIdAsync(msg2.Id, CancellationToken.None);
        var retrieved3 = await _repository.GetByIdAsync(msg3.Id, CancellationToken.None);

        retrieved1.Should().BeNull();
        retrieved2.Should().BeNull();
        retrieved3.Should().BeNull();
    }

    [Fact]
    public async Task DeleteByRunAsync_NonExistentRun_DoesNotThrow()
    {
        // Arrange
        var nonExistentRunId = RunId.NewId();

        // Act
        Func<Task> act = async () => await _repository.DeleteByRunAsync(nonExistentRunId, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateAsync_WithToolCalls_PersistsToolCallsJson()
    {
        // Arrange
        var (chatId, runId) = await CreateTestChatAndRunAsync();
        var message = Message.Create(runId, "assistant", "Using tools...", 1);

        var toolCall1 = new ToolCall("call_001", "search_files", "{\"query\":\"main.cs\"}");
        var toolCall2 = new ToolCall("call_002", "read_file", "{\"path\":\"main.cs\"}");
        message.AddToolCalls(new[] { toolCall1, toolCall2 });

        await _repository.CreateAsync(message, CancellationToken.None);

        // Act
        var retrieved = await _repository.GetByIdAsync(message.Id, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        var toolCallsJson = retrieved!.GetToolCallsJson();
        toolCallsJson.Should().NotBeNullOrEmpty();
        toolCallsJson.Should().Contain("call_001");
        toolCallsJson.Should().Contain("search_files");
    }

    [Fact]
    public async Task CreateAsync_DifferentRoles_AllPersistCorrectly()
    {
        // Arrange
        var (chatId, runId) = await CreateTestChatAndRunAsync();

        var userMsg = Message.Create(runId, "user", "User message", 1);
        var assistantMsg = Message.Create(runId, "assistant", "Assistant message", 2);
        var systemMsg = Message.Create(runId, "system", "System message", 3);
        var toolMsg = Message.Create(runId, "tool", "Tool result", 4);

        await _repository.CreateAsync(userMsg, CancellationToken.None);
        await _repository.CreateAsync(assistantMsg, CancellationToken.None);
        await _repository.CreateAsync(systemMsg, CancellationToken.None);
        await _repository.CreateAsync(toolMsg, CancellationToken.None);

        // Act
        var result = await _repository.ListByRunAsync(runId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(4);
        result[0].Role.Should().Be("user");
        result[1].Role.Should().Be("assistant");
        result[2].Role.Should().Be("system");
        result[3].Role.Should().Be("tool");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSyncStatus()
    {
        // Arrange
        var (chatId, runId) = await CreateTestChatAndRunAsync();
        var message = Message.Create(runId, "user", "Test", 1);
        await _repository.CreateAsync(message, CancellationToken.None);

        var loaded = await _repository.GetByIdAsync(message.Id, CancellationToken.None);
        loaded!.MarkConflict();

        // Act
        await _repository.UpdateAsync(loaded, CancellationToken.None);

        // Assert
        var retrieved = await _repository.GetByIdAsync(message.Id, CancellationToken.None);
        retrieved!.SyncStatus.Should().Be(SyncStatus.Conflict);
    }

    private async Task<(ChatId ChatId, RunId RunId)> CreateTestChatAndRunAsync()
    {
        var chatId = ChatId.NewId();
        var runId = RunId.NewId();

        using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        // Create chat
        using var chatCmd = conn.CreateCommand();
        chatCmd.CommandText = @"
            INSERT INTO chats (id, title, tags, worktree_id, is_deleted, deleted_at,
                              sync_status, version, created_at, updated_at)
            VALUES (@Id, @Title, '[]', NULL, 0, NULL, 'Pending', 1, @CreatedAt, @UpdatedAt)";
        chatCmd.Parameters.AddWithValue("@Id", chatId.Value);
        chatCmd.Parameters.AddWithValue("@Title", "Test Chat");
        chatCmd.Parameters.AddWithValue("@CreatedAt", DateTimeOffset.UtcNow.ToString("O"));
        chatCmd.Parameters.AddWithValue("@UpdatedAt", DateTimeOffset.UtcNow.ToString("O"));
        await chatCmd.ExecuteNonQueryAsync();

        // Create run
        using var runCmd = conn.CreateCommand();
        runCmd.CommandText = @"
            INSERT INTO runs (id, chat_id, model_id, status, started_at, completed_at,
                             tokens_in, tokens_out, sequence_number, error_message, sync_status)
            VALUES (@Id, @ChatId, @ModelId, @Status, @StartedAt, NULL, 0, 0, 1, NULL, 'Pending')";
        runCmd.Parameters.AddWithValue("@Id", runId.Value);
        runCmd.Parameters.AddWithValue("@ChatId", chatId.Value);
        runCmd.Parameters.AddWithValue("@ModelId", "test-model");
        runCmd.Parameters.AddWithValue("@Status", "Running");
        runCmd.Parameters.AddWithValue("@StartedAt", DateTimeOffset.UtcNow.ToString("O"));
        await runCmd.ExecuteNonQueryAsync();

        return (chatId, runId);
    }
}
