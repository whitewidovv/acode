// tests/Acode.Infrastructure.Benchmarks/MessageRepositoryBenchmarks.cs
namespace Acode.Infrastructure.Benchmarks;

using System.IO;
using Acode.Application.Conversation.Persistence;
using Acode.Domain.Conversation;
using Acode.Infrastructure.Persistence.Conversation;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;

/// <summary>
/// Benchmarks for Message repository CRUD operations.
/// Validates AC-095: create &lt;5ms, read &lt;3ms, update &lt;5ms
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class MessageRepositoryBenchmarks
{
    private string _dbPath = string.Empty;
    private IMessageRepository _repository = null!;
    private MessageId _testMessageId;
    private RunId _testRunId;
    private ChatId _testChatId;

    [GlobalSetup]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bench_messages_{Guid.NewGuid()}.db");
        _repository = new SqliteMessageRepository(_dbPath);

        // Initialize schema
        var schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "Migrations", "001_InitialSchema.sql");
        var schema = File.ReadAllText(schemaPath);

        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = schema;
        cmd.ExecuteNonQuery();

        // Create test data (chat + run)
        _testChatId = ChatId.NewId();
        _testRunId = RunId.NewId();

        cmd.CommandText = @"
            INSERT INTO conv_chats (id, title, tags, worktree_id, is_deleted, deleted_at,
                              sync_status, version, created_at, updated_at)
            VALUES (@Id, @Title, '[]', NULL, 0, NULL, 'Pending', 1, @CreatedAt, @UpdatedAt)";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@Id", _testChatId.Value);
        cmd.Parameters.AddWithValue("@Title", "Benchmark Chat");
        cmd.Parameters.AddWithValue("@CreatedAt", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            INSERT INTO conv_runs (id, chat_id, model_id, status, started_at, completed_at,
                             tokens_in, tokens_out, sequence_number, error_message, sync_status)
            VALUES (@Id, @ChatId, @ModelId, @Status, @StartedAt, NULL, 0, 0, 1, NULL, 'Pending')";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@Id", _testRunId.Value);
        cmd.Parameters.AddWithValue("@ChatId", _testChatId.Value);
        cmd.Parameters.AddWithValue("@ModelId", "bench-model");
        cmd.Parameters.AddWithValue("@Status", "Running");
        cmd.Parameters.AddWithValue("@StartedAt", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        // Create a message for read/update benchmarks
        var message = Message.Create(_testRunId, "user", "Benchmark message", 1);
        _testMessageId = message.Id;
        _repository.CreateAsync(message, CancellationToken.None).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    /// <summary>
    /// Benchmark message creation. Target: &lt;5ms
    /// </summary>
    [Benchmark]
    public async Task<MessageId> CreateMessage()
    {
        var message = Message.Create(_testRunId, "user", "New benchmark message", 100);
        return await _repository.CreateAsync(message, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark message retrieval by ID. Target: &lt;3ms
    /// </summary>
    [Benchmark]
    public async Task<Message?> GetMessageById()
    {
        return await _repository.GetByIdAsync(_testMessageId, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark message update. Target: &lt;5ms
    /// </summary>
    [Benchmark]
    public async Task UpdateMessage()
    {
        var message = await _repository.GetByIdAsync(_testMessageId, CancellationToken.None);
        message!.MarkSynced();
        await _repository.UpdateAsync(message, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark listing messages by run. Target: &lt;10ms
    /// </summary>
    [Benchmark]
    public async Task<IReadOnlyList<Message>> ListMessagesByRun()
    {
        return await _repository.ListByRunAsync(_testRunId, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark AppendAsync operation. Target: &lt;5ms
    /// </summary>
    [Benchmark]
    public async Task<MessageId> AppendMessage()
    {
        var message = Message.Create(_testRunId, "assistant", "Appended message");
        return await _repository.AppendAsync(_testRunId, message, CancellationToken.None);
    }
}
