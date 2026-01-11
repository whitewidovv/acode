// tests/Acode.Infrastructure.Benchmarks/RunRepositoryBenchmarks.cs
namespace Acode.Infrastructure.Benchmarks;

using System.IO;
using Acode.Application.Conversation.Persistence;
using Acode.Domain.Conversation;
using Acode.Infrastructure.Persistence.Conversation;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;

/// <summary>
/// Benchmarks for Run repository CRUD operations.
/// Validates AC-095: create &lt;5ms, read &lt;3ms, update &lt;5ms
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class RunRepositoryBenchmarks
{
    private string _dbPath = string.Empty;
    private IRunRepository _repository = null!;
    private RunId _testRunId;
    private ChatId _testChatId;

    [GlobalSetup]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bench_runs_{Guid.NewGuid()}.db");
        _repository = new SqliteRunRepository(_dbPath);

        // Initialize schema
        var schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "Migrations", "001_InitialSchema.sql");
        var schema = File.ReadAllText(schemaPath);

        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = schema;
        cmd.ExecuteNonQuery();

        // Create test chat
        _testChatId = ChatId.NewId();
        cmd.CommandText = @"
            INSERT INTO conv_chats (id, title, tags, worktree_id, is_deleted, deleted_at,
                              sync_status, version, created_at, updated_at)
            VALUES (@Id, @Title, '[]', NULL, 0, NULL, 'Pending', 1, @CreatedAt, @UpdatedAt)";
        cmd.Parameters.AddWithValue("@Id", _testChatId.Value);
        cmd.Parameters.AddWithValue("@Title", "Benchmark Chat");
        cmd.Parameters.AddWithValue("@CreatedAt", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        // Create a run for read/update benchmarks
        var run = Run.Create(_testChatId, "bench-model", 1);
        _testRunId = run.Id;
        _repository.CreateAsync(run, CancellationToken.None).GetAwaiter().GetResult();
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
    /// Benchmark run creation. Target: &lt;5ms
    /// </summary>
    [Benchmark]
    public async Task<RunId> CreateRun()
    {
        var run = Run.Create(_testChatId, "bench-model-2", 100);
        return await _repository.CreateAsync(run, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark run retrieval by ID. Target: &lt;3ms
    /// </summary>
    [Benchmark]
    public async Task<Run?> GetRunById()
    {
        return await _repository.GetByIdAsync(_testRunId, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark run update. Target: &lt;5ms
    /// </summary>
    [Benchmark]
    public async Task UpdateRun()
    {
        var run = await _repository.GetByIdAsync(_testRunId, CancellationToken.None);
        run!.Complete(100, 200);
        await _repository.UpdateAsync(run, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark listing runs by chat. Target: &lt;10ms
    /// </summary>
    [Benchmark]
    public async Task<IReadOnlyList<Run>> ListRunsByChat()
    {
        return await _repository.ListByChatAsync(_testChatId, CancellationToken.None);
    }
}
