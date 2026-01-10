// tests/Acode.Cli.Tests/Commands/ChatCommandIntegrationTests.cs
#pragma warning disable CA2007 // Do not directly await a Task - test methods don't need ConfigureAwait
#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace Acode.Cli.Tests.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Conversation.Persistence;
using Acode.Application.Conversation.Session;
using Acode.Cli.Commands;
using Acode.Domain.Conversation;
using Acode.Infrastructure.Persistence.Conversation;
using Acode.Infrastructure.Session;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

/// <summary>
/// Integration tests for ChatCommand using real SQLite database.
/// Tests the full stack: ChatCommand -> Repositories -> Database.
/// </summary>
public sealed class ChatCommandIntegrationTests : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteConnection _connection;
    private readonly IChatRepository _chatRepository;
    private readonly IRunRepository _runRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ISessionManager _sessionManager;
    private readonly ChatCommand _command;
    private bool _disposed;

    public ChatCommandIntegrationTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"test_chat_{Guid.NewGuid():N}.db");

        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        // Use exact schema from 001_InitialSchema.sql migration
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
            _sessionManager);
    }

    [Fact]
    public async Task CreateAndListChats_ShouldPersistAndRetrieve()
    {
        // Arrange
        var context1 = CreateContext(new[] { "new", "Chat 1" });
        var context2 = CreateContext(new[] { "new", "Chat 2" });
        var context3 = CreateContext(new[] { "new", "Chat 3" });
        var listContext = CreateContext(new[] { "list" });

        // Act - Create 3 chats
        var result1 = await _command.ExecuteAsync(context1);
        var result2 = await _command.ExecuteAsync(context2);
        var result3 = await _command.ExecuteAsync(context3);

        // Act - List chats
        var listResult = await _command.ExecuteAsync(listContext);

        // Assert
        result1.Should().Be(ExitCode.Success);
        result2.Should().Be(ExitCode.Success);
        result3.Should().Be(ExitCode.Success);
        listResult.Should().Be(ExitCode.Success);

        var listOutput = listContext.Output.ToString();
        listOutput.Should().Contain("Chat 1");
        listOutput.Should().Contain("Chat 2");
        listOutput.Should().Contain("Chat 3");
    }

    [Fact]
    public async Task DeleteAndRestore_ShouldModifyDatabaseState()
    {
        // Arrange - Create a chat directly in database
        var chat = Chat.Create("Test Chat");
        await _chatRepository.CreateAsync(chat, CancellationToken.None);
        var chatId = chat.Id.Value;

        // Act - Delete the chat
        var deleteContext = CreateContext(new[] { "delete", chatId, "--force" });
        var deleteResult = await _command.ExecuteAsync(deleteContext);

        // Act - List without includeDeleted (should not show)
        var listContext1 = CreateContext(new[] { "list" });
        await _command.ExecuteAsync(listContext1);

        // Act - Restore the chat
        var restoreContext = CreateContext(new[] { "restore", chatId });
        var restoreResult = await _command.ExecuteAsync(restoreContext);

        // Act - List again (should show restored)
        var listContext2 = CreateContext(new[] { "list" });
        await _command.ExecuteAsync(listContext2);

        // Assert
        deleteResult.Should().Be(ExitCode.Success);
        restoreResult.Should().Be(ExitCode.Success);

        listContext1.Output.ToString().Should().NotContain("Test Chat");
        listContext2.Output.ToString().Should().Contain("Test Chat");
    }

    [Fact]
    public async Task PurgeChat_ShouldCascadeDelete()
    {
        // Arrange - Create chat directly in database
        var chat = Chat.Create("Chat To Purge");
        await _chatRepository.CreateAsync(chat, CancellationToken.None);
        var chatId = chat.Id.Value;

        // Add a run and message
        var run = Run.Create(chat.Id, "test-model", 1);
        await _runRepository.CreateAsync(run, CancellationToken.None);

        var message = Message.Create(run.Id, "user", "Test message", 1);
        await _messageRepository.CreateAsync(message, CancellationToken.None);

        // Act - Purge
        var purgeContext = CreateContext(new[] { "purge", chatId, "--force" });
        var purgeResult = await _command.ExecuteAsync(purgeContext);

        // Assert
        purgeResult.Should().Be(ExitCode.Success);

        var chatResult = await _chatRepository.GetByIdAsync(ChatId.From(chatId));
        var runResult = await _runRepository.GetByIdAsync(run.Id, CancellationToken.None);
        var messageResult = await _messageRepository.GetByIdAsync(message.Id, CancellationToken.None);

        chatResult.Should().BeNull();
        runResult.Should().BeNull();
        messageResult.Should().BeNull();
    }

    [Fact]
    public async Task RenameChat_ShouldUpdateDatabase()
    {
        // Arrange - Create chat directly in database
        var chat = Chat.Create("Original Title");
        await _chatRepository.CreateAsync(chat, CancellationToken.None);
        var chatId = chat.Id.Value;

        // Act - Rename
        var renameContext = CreateContext(new[] { "rename", chatId, "Updated Title" });
        var renameResult = await _command.ExecuteAsync(renameContext);

        // Act - Show to verify
        var showContext = CreateContext(new[] { "show", chatId });
        await _command.ExecuteAsync(showContext);

        // Assert
        renameResult.Should().Be(ExitCode.Success);
        showContext.Output.ToString().Should().Contain("Updated Title");
        showContext.Output.ToString().Should().NotContain("Original Title");
    }

    [Fact]
    public async Task OpenChat_ShouldSetActiveSession()
    {
        // Arrange - Create chat directly in database
        var chat = Chat.Create("Chat For Opening");
        await _chatRepository.CreateAsync(chat, CancellationToken.None);
        var chatId = chat.Id.Value;

        // Act - Open
        var openContext = CreateContext(new[] { "open", chatId });
        var openResult = await _command.ExecuteAsync(openContext);

        // Act - Status to verify
        var statusContext = CreateContext(new[] { "status" });
        await _command.ExecuteAsync(statusContext);

        // Assert
        openResult.Should().Be(ExitCode.Success);
        statusContext.Output.ToString().Should().Contain("Active Chat:");
        statusContext.Output.ToString().Should().Contain(chatId);
        statusContext.Output.ToString().Should().Contain("Chat For Opening");
    }

    [Fact]
    public async Task FullLifecycle_CreateRenameDeleteRestorePurge()
    {
        // Step 1: Create chat directly in database
        var chat = Chat.Create("Lifecycle Chat");
        await _chatRepository.CreateAsync(chat, CancellationToken.None);
        var chatId = chat.Id.Value;

        // Step 2: Rename
        var renameContext = CreateContext(new[] { "rename", chatId, "Renamed Chat" });
        var renameResult = await _command.ExecuteAsync(renameContext);
        renameResult.Should().Be(ExitCode.Success);

        // Step 3: Delete
        var deleteContext = CreateContext(new[] { "delete", chatId, "--force" });
        var deleteResult = await _command.ExecuteAsync(deleteContext);
        deleteResult.Should().Be(ExitCode.Success);

        // Step 4: Restore
        var restoreContext = CreateContext(new[] { "restore", chatId });
        var restoreResult = await _command.ExecuteAsync(restoreContext);
        restoreResult.Should().Be(ExitCode.Success);

        // Step 5: Purge
        var purgeContext = CreateContext(new[] { "purge", chatId, "--force" });
        var purgeResult = await _command.ExecuteAsync(purgeContext);
        purgeResult.Should().Be(ExitCode.Success);

        // Verify purged
        var finalChat = await _chatRepository.GetByIdAsync(ChatId.From(chatId));
        finalChat.Should().BeNull();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _connection?.Dispose();

        if (File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch
            {
                // Best effort cleanup - ignore errors
            }
        }

        _disposed = true;
    }

    private static CommandContext CreateContext(string[] args)
    {
        return new CommandContext
        {
            Args = args,
            Output = new StringWriter(),
            Formatter = new ConsoleFormatter(new StringWriter(), false),
            Configuration = new Dictionary<string, object>(),
            CancellationToken = CancellationToken.None,
        };
    }
}
