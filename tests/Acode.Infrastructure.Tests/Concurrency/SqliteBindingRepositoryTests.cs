#pragma warning disable CA2007 // xUnit tests should use ConfigureAwait(true)
#pragma warning disable IDE0005 // Using directive is unnecessary

using Acode.Application.Concurrency;
using Acode.Application.Database;
using Acode.Domain.Concurrency;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;
using Acode.Infrastructure.Concurrency;
using Acode.Infrastructure.Database;
using Acode.Infrastructure.Database.Sqlite;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Acode.Infrastructure.Tests.Concurrency;

/// <summary>
/// Tests for <see cref="SqliteBindingRepository"/>.
/// Verifies worktree-to-chat binding persistence and one-to-one constraints.
/// </summary>
public sealed class SqliteBindingRepositoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _testDbDir;
    private readonly IConnectionFactory _connectionFactory;

    public SqliteBindingRepositoryTests()
    {
        _testDbDir = Path.Combine(Path.GetTempPath(), $"acode-bindings-test-{Guid.NewGuid():N}");
        _testDbPath = Path.Combine(_testDbDir, "bindings.db");

        var options = Options.Create(new DatabaseOptions
        {
            Local = new LocalDatabaseOptions { Path = _testDbPath },
        });
        _connectionFactory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Initialize database schema (migrations need to be applied for foreign keys)
        InitializeDatabaseSchema().Wait();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDbDir))
        {
            Directory.Delete(_testDbDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetByWorktreeAsync_WithNonExistentWorktree_ReturnsNull()
    {
        // Arrange
        var repository = new SqliteBindingRepository(_connectionFactory, NullLogger<SqliteBindingRepository>.Instance);
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");

        // Act
        var binding = await repository.GetByWorktreeAsync(worktreeId, CancellationToken.None).ConfigureAwait(true);

        // Assert
        binding.Should().BeNull("binding should not exist for non-existent worktree");
    }

    [Fact]
    public async Task GetByChatAsync_WithNonExistentChat_ReturnsNull()
    {
        // Arrange
        var repository = new SqliteBindingRepository(_connectionFactory, NullLogger<SqliteBindingRepository>.Instance);
        var chatId = ChatId.From("01HKABC1234567890ABCDEFGHI");

        // Act
        var binding = await repository.GetByChatAsync(chatId, CancellationToken.None).ConfigureAwait(true);

        // Assert
        binding.Should().BeNull("binding should not exist for non-existent chat");
    }

    [Fact]
    public async Task CreateAsync_WithValidBinding_StoresBinding()
    {
        // Arrange
        var repository = new SqliteBindingRepository(_connectionFactory, NullLogger<SqliteBindingRepository>.Instance);
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var chatId = ChatId.From("01HKABC1234567890ABCDEFGHI");

        // Create chat first (required for foreign key)
        await CreateChatAsync(chatId);

        var binding = WorktreeBinding.Create(worktreeId, chatId);

        // Act
        await repository.CreateAsync(binding, CancellationToken.None).ConfigureAwait(true);

        // Assert - verify stored
        var retrieved = await repository.GetByWorktreeAsync(worktreeId, CancellationToken.None).ConfigureAwait(true);
        retrieved.Should().NotBeNull();
        retrieved!.WorktreeId.Should().Be(worktreeId);
        retrieved.ChatId.Should().Be(chatId);
        retrieved.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateWorktree_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = new SqliteBindingRepository(_connectionFactory, NullLogger<SqliteBindingRepository>.Instance);
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var chatId1 = ChatId.From("01HKABC1234567890ABCDEFGHI");
        var chatId2 = ChatId.From("01HKDEF1234567890ABCDEFGHI");

        await CreateChatAsync(chatId1);
        await CreateChatAsync(chatId2);

        var binding1 = WorktreeBinding.Create(worktreeId, chatId1);
        await repository.CreateAsync(binding1, CancellationToken.None).ConfigureAwait(true);

        var binding2 = WorktreeBinding.Create(worktreeId, chatId2);

        // Act
        var act = async () => await repository.CreateAsync(binding2, CancellationToken.None).ConfigureAwait(true);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already bound*", "worktree can only bind to one chat");
    }

    [Fact]
    public async Task DeleteAsync_RemovesBinding()
    {
        // Arrange
        var repository = new SqliteBindingRepository(_connectionFactory, NullLogger<SqliteBindingRepository>.Instance);
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var chatId = ChatId.From("01HKABC1234567890ABCDEFGHI");

        await CreateChatAsync(chatId);
        var binding = WorktreeBinding.Create(worktreeId, chatId);
        await repository.CreateAsync(binding, CancellationToken.None).ConfigureAwait(true);

        // Act
        await repository.DeleteAsync(worktreeId, CancellationToken.None).ConfigureAwait(true);

        // Assert
        var retrieved = await repository.GetByWorktreeAsync(worktreeId, CancellationToken.None).ConfigureAwait(true);
        retrieved.Should().BeNull("binding should be deleted");
    }

    [Fact]
    public async Task DeleteByChatAsync_RemovesBinding()
    {
        // Arrange
        var repository = new SqliteBindingRepository(_connectionFactory, NullLogger<SqliteBindingRepository>.Instance);
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var chatId = ChatId.From("01HKABC1234567890ABCDEFGHI");

        await CreateChatAsync(chatId);
        var binding = WorktreeBinding.Create(worktreeId, chatId);
        await repository.CreateAsync(binding, CancellationToken.None).ConfigureAwait(true);

        // Act
        await repository.DeleteByChatAsync(chatId, CancellationToken.None).ConfigureAwait(true);

        // Assert
        var retrieved = await repository.GetByChatAsync(chatId, CancellationToken.None).ConfigureAwait(true);
        retrieved.Should().BeNull("binding should be deleted");
    }

    [Fact]
    public async Task ListAllAsync_ReturnsAllBindings()
    {
        // Arrange
        var repository = new SqliteBindingRepository(_connectionFactory, NullLogger<SqliteBindingRepository>.Instance);
        var worktreeId1 = WorktreeId.FromPath("/home/user/project/feature/auth");
        var worktreeId2 = WorktreeId.FromPath("/home/user/project/feature/payments");
        var chatId1 = ChatId.From("01HKABC1234567890ABCDEFGHI");
        var chatId2 = ChatId.From("01HKDEF1234567890ABCDEFGHI");

        await CreateChatAsync(chatId1);
        await CreateChatAsync(chatId2);

        var binding1 = WorktreeBinding.Create(worktreeId1, chatId1);
        var binding2 = WorktreeBinding.Create(worktreeId2, chatId2);
        await repository.CreateAsync(binding1, CancellationToken.None).ConfigureAwait(true);
        await repository.CreateAsync(binding2, CancellationToken.None).ConfigureAwait(true);

        // Act
        var bindings = await repository.ListAllAsync(CancellationToken.None).ConfigureAwait(true);

        // Assert
        bindings.Should().HaveCount(2);
        bindings.Should().Contain(b => b.WorktreeId == worktreeId1 && b.ChatId == chatId1);
        bindings.Should().Contain(b => b.WorktreeId == worktreeId2 && b.ChatId == chatId2);
    }

    [Fact]
    public async Task GetByWorktreeAsync_ReturnsCorrectBinding()
    {
        // Arrange
        var repository = new SqliteBindingRepository(_connectionFactory, NullLogger<SqliteBindingRepository>.Instance);
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var chatId = ChatId.From("01HKABC1234567890ABCDEFGHI");

        await CreateChatAsync(chatId);
        var binding = WorktreeBinding.Create(worktreeId, chatId);
        await repository.CreateAsync(binding, CancellationToken.None).ConfigureAwait(true);

        // Act
        var retrieved = await repository.GetByWorktreeAsync(worktreeId, CancellationToken.None).ConfigureAwait(true);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.WorktreeId.Should().Be(worktreeId);
        retrieved.ChatId.Should().Be(chatId);
    }

    [Fact]
    public async Task GetByChatAsync_ReturnsCorrectBinding()
    {
        // Arrange
        var repository = new SqliteBindingRepository(_connectionFactory, NullLogger<SqliteBindingRepository>.Instance);
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var chatId = ChatId.From("01HKABC1234567890ABCDEFGHI");

        await CreateChatAsync(chatId);
        var binding = WorktreeBinding.Create(worktreeId, chatId);
        await repository.CreateAsync(binding, CancellationToken.None).ConfigureAwait(true);

        // Act
        var retrieved = await repository.GetByChatAsync(chatId, CancellationToken.None).ConfigureAwait(true);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.WorktreeId.Should().Be(worktreeId);
        retrieved.ChatId.Should().Be(chatId);
    }

    private async Task CreateChatAsync(ChatId chatId)
    {
        await using var connection = await _connectionFactory.CreateAsync();
        await connection.ExecuteAsync(
            @"
            INSERT INTO conv_chats (id, title, created_at, updated_at, is_deleted, version)
            VALUES (@Id, 'Test Chat', @CreatedAt, @UpdatedAt, 0, 1)",
            new
            {
                Id = chatId.Value,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
    }

    private async Task InitializeDatabaseSchema()
    {
        // Create minimal schema for testing
        await using var connection = await _connectionFactory.CreateAsync();

        // Create conv_chats table (required for foreign key)
        await connection.ExecuteAsync(@"
            CREATE TABLE conv_chats (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                version INTEGER NOT NULL DEFAULT 1
            )");

        // Create worktree_bindings table (from migration)
        await connection.ExecuteAsync(@"
            CREATE TABLE worktree_bindings (
                worktree_id TEXT PRIMARY KEY,
                chat_id TEXT NOT NULL UNIQUE,
                created_at TEXT NOT NULL,
                FOREIGN KEY (chat_id) REFERENCES conv_chats(id) ON DELETE CASCADE
            )");

        // Create index
        await connection.ExecuteAsync(@"
            CREATE INDEX idx_worktree_bindings_chat ON worktree_bindings(chat_id)");
    }
}
