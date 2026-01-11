// tests/Acode.Infrastructure.Benchmarks/ChatRepositoryBenchmarks.cs
namespace Acode.Infrastructure.Benchmarks;

using System.IO;
using Acode.Application.Conversation.Persistence;
using Acode.Domain.Conversation;
using Acode.Infrastructure.Persistence.Conversation;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;

/// <summary>
/// Benchmarks for Chat repository CRUD operations.
/// Validates AC-095: create &lt;5ms, read &lt;3ms, update &lt;5ms
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ChatRepositoryBenchmarks
{
    private string _dbPath = string.Empty;
    private IChatRepository _repository = null!;
    private ChatId _testChatId;

    [GlobalSetup]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bench_chats_{Guid.NewGuid()}.db");
        _repository = new SqliteChatRepository(_dbPath);

        // Initialize schema
        var schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "Migrations", "001_InitialSchema.sql");
        var schema = File.ReadAllText(schemaPath);

        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = schema;
        cmd.ExecuteNonQuery();

        // Create a chat for read/update benchmarks
        var chat = Chat.Create("Benchmark Chat");
        _testChatId = chat.Id;
        _repository.CreateAsync(chat, CancellationToken.None).GetAwaiter().GetResult();
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
    /// Benchmark chat creation. Target: &lt;5ms
    /// </summary>
    [Benchmark]
    public async Task<ChatId> CreateChat()
    {
        var chat = Chat.Create("New benchmark chat");
        return await _repository.CreateAsync(chat, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark chat retrieval by ID. Target: &lt;3ms
    /// </summary>
    [Benchmark]
    public async Task<Chat?> GetChatById()
    {
        return await _repository.GetByIdAsync(_testChatId, false, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark chat update. Target: &lt;5ms
    /// </summary>
    [Benchmark]
    public async Task UpdateChat()
    {
        var chat = await _repository.GetByIdAsync(_testChatId, false, CancellationToken.None);
        chat!.UpdateTitle("Updated benchmark chat");
        await _repository.UpdateAsync(chat, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark chat soft delete. Target: &lt;5ms
    /// </summary>
    [Benchmark]
    public async Task SoftDeleteChat()
    {
        var chat = Chat.Create("Chat to delete");
        var chatId = await _repository.CreateAsync(chat, CancellationToken.None);
        await _repository.SoftDeleteAsync(chatId, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark listing all chats. Target: &lt;10ms
    /// </summary>
    [Benchmark]
    public async Task<PagedResult<Chat>> ListAllChats()
    {
        var filter = new ChatFilter();
        return await _repository.ListAsync(filter, CancellationToken.None);
    }
}
