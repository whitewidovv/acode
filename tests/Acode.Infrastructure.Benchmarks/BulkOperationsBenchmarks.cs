// tests/Acode.Infrastructure.Benchmarks/BulkOperationsBenchmarks.cs
namespace Acode.Infrastructure.Benchmarks;

using System.IO;
using Acode.Application.Conversation.Persistence;
using Acode.Domain.Conversation;
using Acode.Infrastructure.Persistence.Conversation;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;

/// <summary>
/// Benchmarks for bulk insert operations.
/// Validates AC-096: 100 inserts &lt;50ms
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class BulkOperationsBenchmarks
{
    private string _dbPath = string.Empty;
    private IMessageRepository _messageRepository = null!;
    private RunId _testRunId;
    private ChatId _testChatId;

    [GlobalSetup]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bench_bulk_{Guid.NewGuid()}.db");
        _messageRepository = new SqliteMessageRepository(_dbPath);

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
    /// Benchmark bulk insert of 10 messages. Target: &lt;10ms
    /// </summary>
    [Benchmark]
    public async Task BulkInsert10Messages()
    {
        var messages = Enumerable.Range(1, 10)
            .Select(i => Message.Create(_testRunId, "user", $"Message {i}"))
            .ToList();

        await _messageRepository.BulkCreateAsync(messages, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark bulk insert of 100 messages. Target: &lt;50ms (AC-096)
    /// </summary>
    [Benchmark]
    public async Task BulkInsert100Messages()
    {
        var messages = Enumerable.Range(1, 100)
            .Select(i => Message.Create(_testRunId, "user", $"Message {i}"))
            .ToList();

        await _messageRepository.BulkCreateAsync(messages, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark bulk insert of 1000 messages. Target: &lt;500ms
    /// </summary>
    [Benchmark]
    public async Task BulkInsert1000Messages()
    {
        var messages = Enumerable.Range(1, 1000)
            .Select(i => Message.Create(_testRunId, "user", $"Message {i}"))
            .ToList();

        await _messageRepository.BulkCreateAsync(messages, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark sequential AppendAsync for 100 messages (comparison baseline).
    /// </summary>
    [Benchmark]
    public async Task Sequential100Appends()
    {
        for (int i = 1; i <= 100; i++)
        {
            var message = Message.Create(_testRunId, "user", $"Sequential message {i}");
            await _messageRepository.AppendAsync(_testRunId, message, CancellationToken.None);
        }
    }
}
