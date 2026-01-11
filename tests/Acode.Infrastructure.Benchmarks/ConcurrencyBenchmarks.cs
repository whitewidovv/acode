// tests/Acode.Infrastructure.Benchmarks/ConcurrencyBenchmarks.cs
namespace Acode.Infrastructure.Benchmarks;

using System.IO;
using Acode.Application.Conversation.Persistence;
using Acode.Domain.Conversation;
using Acode.Infrastructure.Persistence.Conversation;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;

/// <summary>
/// Benchmarks for concurrent operations.
/// Validates AC-097: 10 concurrent ops &lt;100ms
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ConcurrencyBenchmarks
{
    private string _dbPath = string.Empty;
    private IMessageRepository _messageRepository = null!;
    private RunId _testRunId;
    private ChatId _testChatId;

    [GlobalSetup]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bench_concurrency_{Guid.NewGuid()}.db");
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
    /// Benchmark 5 concurrent message creations. Target: &lt;50ms
    /// </summary>
    [Benchmark]
    public async Task Concurrent5Creates()
    {
        var tasks = Enumerable.Range(1, 5)
            .Select(async i =>
            {
                var message = Message.Create(_testRunId, "user", $"Concurrent message {i}", i);
                return await _messageRepository.CreateAsync(message, CancellationToken.None);
            })
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmark 10 concurrent message creations. Target: &lt;100ms (AC-097)
    /// </summary>
    [Benchmark]
    public async Task Concurrent10Creates()
    {
        var tasks = Enumerable.Range(1, 10)
            .Select(async i =>
            {
                var message = Message.Create(_testRunId, "user", $"Concurrent message {i}", i);
                return await _messageRepository.CreateAsync(message, CancellationToken.None);
            })
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmark 20 concurrent message creations. Target: &lt;200ms
    /// </summary>
    [Benchmark]
    public async Task Concurrent20Creates()
    {
        var tasks = Enumerable.Range(1, 20)
            .Select(async i =>
            {
                var message = Message.Create(_testRunId, "user", $"Concurrent message {i}", i);
                return await _messageRepository.CreateAsync(message, CancellationToken.None);
            })
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmark 10 concurrent read operations. Target: &lt;50ms
    /// </summary>
    [Benchmark]
    public async Task Concurrent10Reads()
    {
        // Create 10 messages first
        var messageIds = new List<MessageId>();
        for (int i = 1; i <= 10; i++)
        {
            var message = Message.Create(_testRunId, "user", $"Read message {i}", i);
            var messageId = await _messageRepository.CreateAsync(message, CancellationToken.None);
            messageIds.Add(messageId);
        }

        // Now read them concurrently
        var tasks = messageIds
            .Select(async id => await _messageRepository.GetByIdAsync(id, CancellationToken.None))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmark 10 concurrent AppendAsync operations. Target: &lt;100ms
    /// </summary>
    [Benchmark]
    public async Task Concurrent10Appends()
    {
        var tasks = Enumerable.Range(1, 10)
            .Select(async i =>
            {
                var message = Message.Create(_testRunId, "user", $"Append message {i}");
                return await _messageRepository.AppendAsync(_testRunId, message, CancellationToken.None);
            })
            .ToArray();

        await Task.WhenAll(tasks);
    }
}
