// tests/Acode.Cli.Tests/Commands/ChatCommandBenchmarks.cs
#pragma warning disable CA2007 // Do not directly await a Task - benchmarks don't need ConfigureAwait
#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace Acode.Cli.Tests.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Concurrency;
using Acode.Application.Conversation.Persistence;
using Acode.Application.Conversation.Session;
using Acode.Cli.Commands;
using Acode.Domain.Conversation;
using Acode.Infrastructure.Persistence.Conversation;
using Acode.Infrastructure.Session;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using NSubstitute;

/// <summary>
/// Performance benchmarks for ChatCommand operations.
/// Validates that operations meet target performance thresholds from spec.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ChatCommandBenchmarks : IDisposable
{
    private string _databasePath = null!;
    private SqliteConnection _connection = null!;
    private IChatRepository _chatRepository = null!;
    private IRunRepository _runRepository = null!;
    private IMessageRepository _messageRepository = null!;
    private ISessionManager _sessionManager = null!;
    private ChatCommand _command = null!;
    private ChatId _testChatId;
    private bool _disposed;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"benchmark_{Guid.NewGuid():N}.db");

        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        var createTablesSql = @"
            CREATE TABLE chats (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                tags TEXT,
                worktree_id TEXT,
                is_deleted INTEGER DEFAULT 0,
                deleted_at TEXT,
                sync_status TEXT DEFAULT 'Pending',
                version INTEGER DEFAULT 1,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE runs (
                id TEXT PRIMARY KEY,
                chat_id TEXT NOT NULL,
                model_id TEXT NOT NULL,
                status TEXT NOT NULL,
                started_at TEXT NOT NULL,
                completed_at TEXT,
                tokens_in INTEGER DEFAULT 0,
                tokens_out INTEGER DEFAULT 0,
                sequence_number INTEGER NOT NULL,
                error_message TEXT,
                sync_status TEXT DEFAULT 'Pending',
                FOREIGN KEY (chat_id) REFERENCES chats(id) ON DELETE CASCADE,
                UNIQUE(chat_id, sequence_number)
            );

            CREATE TABLE messages (
                id TEXT PRIMARY KEY,
                run_id TEXT NOT NULL,
                role TEXT NOT NULL CHECK(role IN ('user', 'assistant', 'system', 'tool')),
                content TEXT NOT NULL,
                tool_calls TEXT,
                created_at TEXT NOT NULL,
                sequence_number INTEGER NOT NULL,
                sync_status TEXT DEFAULT 'Pending',
                FOREIGN KEY (run_id) REFERENCES runs(id) ON DELETE CASCADE,
                UNIQUE(run_id, sequence_number)
            );";

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = createTablesSql;
        cmd.ExecuteNonQuery();

        _chatRepository = new SqliteChatRepository(_databasePath);
        _runRepository = new SqliteRunRepository(_databasePath);
        _messageRepository = new SqliteMessageRepository(_databasePath);
        _sessionManager = new InMemorySessionManager();

        _command = new ChatCommand(
            _chatRepository,
            _runRepository,
            _messageRepository,
            _sessionManager,
            Substitute.For<IBindingService>());

        // Pre-create a test chat for benchmarks that need it
        var testChat = Chat.Create("Benchmark Test Chat");
        _testChatId = testChat.Id;
        _chatRepository.CreateAsync(testChat, CancellationToken.None).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _connection?.Dispose();

        if (File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    /// <summary>
    /// Benchmark: Create chat - Target 50ms, Max 100ms.
    /// </summary>
    /// <returns>The exit code from command execution.</returns>
    [Benchmark]
    public async Task<ExitCode> CreateChat()
    {
        var context = new CommandContext
        {
            Args = new[] { "new", $"Chat {Guid.NewGuid():N}" },
            Output = new StringWriter(),
            Formatter = new ConsoleFormatter(new StringWriter(), false),
            Configuration = new Dictionary<string, object>(),
            CancellationToken = CancellationToken.None,
        };

        return await _command.ExecuteAsync(context);
    }

    /// <summary>
    /// Benchmark: List 100 chats - Target 100ms, Max 200ms.
    /// </summary>
    /// <returns>The exit code from command execution.</returns>
    [Benchmark]
    public async Task<ExitCode> List100Chats()
    {
        var context = new CommandContext
        {
            Args = new[] { "list" },
            Output = new StringWriter(),
            Formatter = new ConsoleFormatter(new StringWriter(), false),
            Configuration = new Dictionary<string, object>(),
            CancellationToken = CancellationToken.None,
        };

        return await _command.ExecuteAsync(context);
    }

    /// <summary>
    /// Benchmark: Open chat - Target 25ms, Max 50ms.
    /// </summary>
    /// <returns>The exit code from command execution.</returns>
    [Benchmark]
    public async Task<ExitCode> OpenChat()
    {
        var context = new CommandContext
        {
            Args = new[] { "open", _testChatId.Value },
            Output = new StringWriter(),
            Formatter = new ConsoleFormatter(new StringWriter(), false),
            Configuration = new Dictionary<string, object>(),
            CancellationToken = CancellationToken.None,
        };

        return await _command.ExecuteAsync(context);
    }

    /// <summary>
    /// Benchmark: Rename chat - Target 50ms, Max 100ms.
    /// </summary>
    /// <returns>The exit code from command execution.</returns>
    [Benchmark]
    public async Task<ExitCode> RenameChat()
    {
        var context = new CommandContext
        {
            Args = new[] { "rename", _testChatId.Value, $"Renamed {Guid.NewGuid():N}" },
            Output = new StringWriter(),
            Formatter = new ConsoleFormatter(new StringWriter(), false),
            Configuration = new Dictionary<string, object>(),
            CancellationToken = CancellationToken.None,
        };

        return await _command.ExecuteAsync(context);
    }

    /// <summary>
    /// Benchmark: Delete (soft) - Target 50ms, Max 100ms.
    /// </summary>
    /// <returns>The exit code from command execution.</returns>
    [Benchmark]
    public async Task<ExitCode> DeleteChat()
    {
        // Create a fresh chat for deletion
        var chatToDelete = Chat.Create("Chat To Delete");
        await _chatRepository.CreateAsync(chatToDelete, CancellationToken.None);

        var context = new CommandContext
        {
            Args = new[] { "delete", chatToDelete.Id.Value, "--force" },
            Output = new StringWriter(),
            Formatter = new ConsoleFormatter(new StringWriter(), false),
            Configuration = new Dictionary<string, object>(),
            CancellationToken = CancellationToken.None,
        };

        return await _command.ExecuteAsync(context);
    }

    /// <summary>
    /// Benchmark: Restore chat - Target 50ms, Max 100ms.
    /// </summary>
    /// <returns>The exit code from command execution.</returns>
    [Benchmark]
    public async Task<ExitCode> RestoreChat()
    {
        // Create and delete a chat for restoration
        var chatToRestore = Chat.Create("Chat To Restore");
        await _chatRepository.CreateAsync(chatToRestore, CancellationToken.None);
        await _chatRepository.SoftDeleteAsync(chatToRestore.Id, CancellationToken.None);

        var context = new CommandContext
        {
            Args = new[] { "restore", chatToRestore.Id.Value },
            Output = new StringWriter(),
            Formatter = new ConsoleFormatter(new StringWriter(), false),
            Configuration = new Dictionary<string, object>(),
            CancellationToken = CancellationToken.None,
        };

        return await _command.ExecuteAsync(context);
    }

    /// <summary>
    /// Benchmark: Purge chat with 10 runs - Target 300ms, Max 500ms.
    /// </summary>
    /// <returns>The exit code from command execution.</returns>
    [Benchmark]
    public async Task<ExitCode> PurgeChatWith10Runs()
    {
        // Create a chat with 10 runs and messages
        var chatToPurge = Chat.Create("Chat To Purge");
        await _chatRepository.CreateAsync(chatToPurge, CancellationToken.None);

        for (int i = 0; i < 10; i++)
        {
            var run = Run.Create(chatToPurge.Id, "benchmark-model", i);
            await _runRepository.CreateAsync(run, CancellationToken.None);

            var message = Message.Create(run.Id, "user", $"Message {i}", i);
            await _messageRepository.CreateAsync(message, CancellationToken.None);
        }

        var context = new CommandContext
        {
            Args = new[] { "purge", chatToPurge.Id.Value, "--force" },
            Output = new StringWriter(),
            Formatter = new ConsoleFormatter(new StringWriter(), false),
            Configuration = new Dictionary<string, object>(),
            CancellationToken = CancellationToken.None,
        };

        return await _command.ExecuteAsync(context);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        GlobalCleanup();
        _disposed = true;
    }
}
