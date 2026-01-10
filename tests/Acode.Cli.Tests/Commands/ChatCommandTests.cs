// tests/Acode.Cli.Tests/Commands/ChatCommandTests.cs
#pragma warning disable CA2007 // Do not directly await a Task - test methods don't need ConfigureAwait

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
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Unit tests for ChatCommand CLI command.
/// Tests all 9 CRUSD subcommands: new, list, open, show, rename, delete, restore, purge, status.
/// </summary>
public sealed class ChatCommandTests
{
    private readonly IChatRepository _chatRepository;
    private readonly IRunRepository _runRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ISessionManager _sessionManager;
    private readonly ChatCommand _command;

    public ChatCommandTests()
    {
        _chatRepository = Substitute.For<IChatRepository>();
        _runRepository = Substitute.For<IRunRepository>();
        _messageRepository = Substitute.For<IMessageRepository>();
        _sessionManager = Substitute.For<ISessionManager>();

        _command = new ChatCommand(
            _chatRepository,
            _runRepository,
            _messageRepository,
            _sessionManager);
    }

    [Fact]
    public void Name_ShouldBe_Chat()
    {
        // Assert
        _command.Name.Should().Be("chat");
    }

    [Fact]
    public void Aliases_ShouldBeNull()
    {
        // Assert
        _command.Aliases.Should().BeNull();
    }

    [Fact]
    public void Description_ShouldDescribe_ChatManagement()
    {
        // Assert
        _command.Description.Should().Contain("conversation");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoArgs_ReturnsInvalidArguments()
    {
        // Arrange
        var context = CreateContext(Array.Empty<string>());

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.InvalidArguments);
        context.Output.ToString().Should().Contain("Missing subcommand");
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownSubcommand_ReturnsInvalidArguments()
    {
        // Arrange
        var context = CreateContext(new[] { "unknown" });

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.InvalidArguments);
        context.Output.ToString().Should().Contain("Unknown subcommand");
    }

    [Fact]
    public async Task ExecuteAsync_WithHelpFlag_ReturnsSuccess()
    {
        // Arrange
        var context = CreateContext(new[] { "--help" });

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        context.Output.ToString().Should().Contain("Usage:");
    }

    // NEW COMMAND TESTS (AC-001-012)
    [Fact]
    public async Task NewAsync_WithValidTitle_CreatesChat()
    {
        // Arrange
        var context = CreateContext(new[] { "new", "Test Chat" });

        _chatRepository.CreateAsync(Arg.Any<Chat>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<Chat>().Id));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        context.Output.ToString().Should().Contain("Chat created");
        await _chatRepository.Received(1).CreateAsync(
            Arg.Is<Chat>(c => c.Title == "Test Chat"),
            Arg.Any<CancellationToken>());
        await _sessionManager.Received(1).SetActiveChatAsync(
            Arg.Any<ChatId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NewAsync_WithAutoTitle_GeneratesTimestampedTitle()
    {
        // Arrange
        var context = CreateContext(new[] { "new", "--auto-title" });

        _chatRepository.CreateAsync(Arg.Any<Chat>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<Chat>().Id));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        await _chatRepository.Received(1).CreateAsync(
            Arg.Is<Chat>(c => c.Title.StartsWith("Chat ")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NewAsync_WithEmptyTitle_GeneratesTimestampedTitle()
    {
        // Arrange (no title provided)
        var context = CreateContext(new[] { "new" });

        _chatRepository.CreateAsync(Arg.Any<Chat>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<Chat>().Id));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        await _chatRepository.Received(1).CreateAsync(
            Arg.Is<Chat>(c => c.Title.StartsWith("Chat ")),
            Arg.Any<CancellationToken>());
    }

    // LIST COMMAND TESTS (AC-013-028)
    [Fact]
    public async Task ListAsync_WithNoChats_ShowsNoChatsFound()
    {
        // Arrange
        var context = CreateContext(new[] { "list" });

        _chatRepository.ListAsync(Arg.Any<ChatFilter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PagedResult<Chat>(
                new List<Chat>(),
                0, // totalCount
                0, // page
                50))); // pageSize

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        context.Output.ToString().Should().Contain("No chats found");
    }

    [Fact]
    public async Task ListAsync_WithChats_DisplaysTable()
    {
        // Arrange
        var chat1 = Chat.Create("Chat 1");
        var chat2 = Chat.Create("Chat 2");
        var context = CreateContext(new[] { "list" });

        _chatRepository.ListAsync(Arg.Any<ChatFilter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PagedResult<Chat>(
                new List<Chat> { chat1, chat2 },
                2, // totalCount
                0, // page
                50))); // pageSize

        _runRepository.ListByChatAsync(Arg.Any<ChatId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Run>>(new List<Run>()));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        await _chatRepository.Received(1).ListAsync(
            Arg.Is<ChatFilter>(f => !f.IncludeDeleted && f.SortBy == ChatSortField.UpdatedAt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_WithArchivedFlag_IncludesDeletedChats()
    {
        // Arrange
        var context = CreateContext(new[] { "list", "--archived" });

        _chatRepository.ListAsync(Arg.Any<ChatFilter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PagedResult<Chat>(
                new List<Chat>(),
                0, // totalCount
                0, // page
                50))); // pageSize

        // Act
        await _command.ExecuteAsync(context);

        // Assert
        await _chatRepository.Received(1).ListAsync(
            Arg.Is<ChatFilter>(f => f.IncludeDeleted),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_WithFilterFlag_FiltersTitle()
    {
        // Arrange
        var context = CreateContext(new[] { "list", "--filter", "API" });

        _chatRepository.ListAsync(Arg.Any<ChatFilter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PagedResult<Chat>(
                new List<Chat>(),
                0, // totalCount
                0, // page
                50))); // pageSize

        // Act
        await _command.ExecuteAsync(context);

        // Assert
        await _chatRepository.Received(1).ListAsync(
            Arg.Is<ChatFilter>(f => f.TitleContains == "API"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_WithSortFlag_SortsBySpecifiedField()
    {
        // Arrange
        var context = CreateContext(new[] { "list", "--sort", "title" });

        _chatRepository.ListAsync(Arg.Any<ChatFilter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PagedResult<Chat>(
                new List<Chat>(),
                0, // totalCount
                0, // page
                50))); // pageSize

        // Act
        await _command.ExecuteAsync(context);

        // Assert
        await _chatRepository.Received(1).ListAsync(
            Arg.Is<ChatFilter>(f => f.SortBy == ChatSortField.Title),
            Arg.Any<CancellationToken>());
    }

    // OPEN COMMAND TESTS (AC-029-036)
    [Fact]
    public async Task OpenAsync_WithValidChatId_SetsActiveChat()
    {
        // Arrange
        var chat = Chat.Create("Test Chat");
        var context = CreateContext(new[] { "open", chat.Id.Value });

        _chatRepository.GetByIdAsync(chat.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(chat));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        context.Output.ToString().Should().Contain("Opened chat");
        await _sessionManager.Received(1).SetActiveChatAsync(
            chat.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenAsync_WithInvalidChatId_ReturnsError()
    {
        // Arrange
        var chatId = ChatId.From("01HK0000000000000000000000");
        var context = CreateContext(new[] { "open", chatId.Value });

        _chatRepository.GetByIdAsync(chatId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(null));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.RuntimeError);
        context.Output.ToString().Should().Contain("ACODE-CHAT-CMD-001");
        context.Output.ToString().Should().Contain("not found");
    }

    [Fact]
    public async Task OpenAsync_WithDeletedChat_ReturnsError()
    {
        // Arrange
        var chat = Chat.Create("Deleted Chat");
        chat.Delete();
        var context = CreateContext(new[] { "open", chat.Id.Value });

        _chatRepository.GetByIdAsync(chat.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(chat));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.RuntimeError);
        context.Output.ToString().Should().Contain("deleted");
        context.Output.ToString().Should().Contain("restore");
    }

    [Fact]
    public async Task OpenAsync_WithMissingChatId_ReturnsInvalidArguments()
    {
        // Arrange
        var context = CreateContext(new[] { "open" });

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.InvalidArguments);
        context.Output.ToString().Should().Contain("Missing chat ID");
    }

    // SHOW COMMAND TESTS (AC-037-048)
    [Fact]
    public async Task ShowAsync_WithValidChatId_DisplaysDetails()
    {
        // Arrange
        var chat = Chat.Create("Test Chat");
        var context = CreateContext(new[] { "show", chat.Id.Value });

        _chatRepository.GetByIdAsync(chat.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(chat));

        _runRepository.ListByChatAsync(chat.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Run>>(new List<Run>()));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        var output = context.Output.ToString();
        output.Should().Contain("Chat Details");
        output.Should().Contain(chat.Id.Value);
        output.Should().Contain(chat.Title);
    }

    [Fact]
    public async Task ShowAsync_WithChatNotFound_ReturnsError()
    {
        // Arrange
        var chatId = ChatId.From("01HK0000000000000000000000");
        var context = CreateContext(new[] { "show", chatId.Value });

        _chatRepository.GetByIdAsync(chatId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(null));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.RuntimeError);
        context.Output.ToString().Should().Contain("ACODE-CHAT-CMD-001");
    }

    // RENAME COMMAND TESTS (AC-049-058)
    [Fact]
    public async Task RenameAsync_WithValidInput_UpdatesTitle()
    {
        // Arrange
        var chat = Chat.Create("Old Title");
        var context = CreateContext(new[] { "rename", chat.Id.Value, "New Title" });

        _chatRepository.GetByIdAsync(chat.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(chat));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        chat.Title.Should().Be("New Title");
        context.Output.ToString().Should().Contain("renamed successfully");
        await _chatRepository.Received(1).UpdateAsync(
            Arg.Is<Chat>(c => c.Title == "New Title"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenameAsync_WithChatNotFound_ReturnsError()
    {
        // Arrange
        var chatId = ChatId.From("01HK0000000000000000000000");
        var context = CreateContext(new[] { "rename", chatId.Value, "New Title" });

        _chatRepository.GetByIdAsync(chatId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(null));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.RuntimeError);
        context.Output.ToString().Should().Contain("ACODE-CHAT-CMD-001");
    }

    [Fact]
    public async Task RenameAsync_WithMissingArguments_ReturnsInvalidArguments()
    {
        // Arrange
        var context = CreateContext(new[] { "rename", "someid" }); // Missing new title

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.InvalidArguments);
        context.Output.ToString().Should().Contain("Missing");
    }

    // DELETE COMMAND TESTS (AC-059-070)
    [Fact]
    public async Task DeleteAsync_WithForceFlag_SoftDeletesChat()
    {
        // Arrange
        var chat = Chat.Create("To Delete");
        var context = CreateContext(new[] { "delete", chat.Id.Value, "--force" });

        _chatRepository.GetByIdAsync(chat.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(chat));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        chat.IsDeleted.Should().BeTrue();
        context.Output.ToString().Should().Contain("deleted successfully");
        await _chatRepository.Received(1).UpdateAsync(
            Arg.Is<Chat>(c => c.IsDeleted),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WithoutForceFlag_RequiresConfirmation()
    {
        // Arrange
        var chat = Chat.Create("To Delete");
        var context = CreateContext(new[] { "delete", chat.Id.Value });

        _chatRepository.GetByIdAsync(chat.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(chat));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert (since we can't read from console, it returns UserCancellation)
        result.Should().Be(ExitCode.UserCancellation);
        context.Output.ToString().Should().Contain("confirmation");
    }

    [Fact]
    public async Task DeleteAsync_OnAlreadyDeletedChat_IsIdempotent()
    {
        // Arrange
        var chat = Chat.Create("Already Deleted");
        chat.Delete();
        var context = CreateContext(new[] { "delete", chat.Id.Value, "--force" });

        _chatRepository.GetByIdAsync(chat.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(chat));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        context.Output.ToString().Should().Contain("already deleted");
    }

    // RESTORE COMMAND TESTS (AC-071-078)
    [Fact]
    public async Task RestoreAsync_WithDeletedChat_RestoresChat()
    {
        // Arrange
        var chat = Chat.Create("Deleted Chat");
        chat.Delete();
        var context = CreateContext(new[] { "restore", chat.Id.Value });

        _chatRepository.GetByIdAsync(chat.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(chat));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        chat.IsDeleted.Should().BeFalse();
        context.Output.ToString().Should().Contain("restored successfully");
        await _chatRepository.Received(1).UpdateAsync(
            Arg.Is<Chat>(c => !c.IsDeleted),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreAsync_OnActiveChat_IsIdempotent()
    {
        // Arrange
        var chat = Chat.Create("Active Chat");
        var context = CreateContext(new[] { "restore", chat.Id.Value });

        _chatRepository.GetByIdAsync(chat.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(chat));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        context.Output.ToString().Should().Contain("already active");
    }

    // PURGE COMMAND TESTS (AC-079-094)
    [Fact]
    public async Task PurgeAsync_WithForceFlag_PermanentlyDeletesChat()
    {
        // Arrange
        var chat = Chat.Create("To Purge");
        var context = CreateContext(new[] { "purge", chat.Id.Value, "--force" });

        var run1 = Run.Create(chat.Id, "test-model", 1);
        var run2 = Run.Create(chat.Id, "test-model", 2);

        _chatRepository.GetByIdAsync(chat.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(chat));

        _runRepository.ListByChatAsync(chat.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Run>>(new List<Run> { run1, run2 }));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        context.Output.ToString().Should().Contain("permanently deleted");

        // Verify cascade delete
        await _messageRepository.Received(2).DeleteByRunAsync(
            Arg.Any<RunId>(),
            Arg.Any<CancellationToken>());

        await _runRepository.Received(2).DeleteAsync(
            Arg.Any<RunId>(),
            Arg.Any<CancellationToken>());

        await _chatRepository.Received(1).DeleteAsync(
            chat.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PurgeAsync_WithoutForceFlag_RequiresDoubleConfirmation()
    {
        // Arrange
        var chat = Chat.Create("To Purge");
        var context = CreateContext(new[] { "purge", chat.Id.Value });

        _chatRepository.GetByIdAsync(chat.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(chat));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert (since we can't read from console, it returns UserCancellation)
        result.Should().Be(ExitCode.UserCancellation);
        context.Output.ToString().Should().Contain("WARNING");
        context.Output.ToString().Should().Contain("permanently delete");
    }

    // STATUS COMMAND TESTS (AC-095-102)
    [Fact]
    public async Task StatusAsync_WithActiveChat_DisplaysChatDetails()
    {
        // Arrange
        var chat = Chat.Create("Active Chat");
        var context = CreateContext(new[] { "status" });

        _sessionManager.GetActiveChatAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ChatId?>(chat.Id));

        _chatRepository.GetByIdAsync(chat.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Chat?>(chat));

        _runRepository.ListByChatAsync(chat.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Run>>(new List<Run>()));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        var output = context.Output.ToString();
        output.Should().Contain("Active Chat");
        output.Should().Contain(chat.Title);
    }

    [Fact]
    public async Task StatusAsync_WithNoActiveChat_ReturnsGeneralError()
    {
        // Arrange
        var context = CreateContext(new[] { "status" });

        _sessionManager.GetActiveChatAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ChatId?>(null));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.GeneralError);
        context.Output.ToString().Should().Contain("No active chat");
    }

    [Fact]
    public void GetHelp_ReturnsUsageInformation()
    {
        // Act
        var help = _command.GetHelp();

        // Assert
        help.Should().Contain("acode chat");
        help.Should().Contain("new");
        help.Should().Contain("list");
        help.Should().Contain("open");
        help.Should().Contain("show");
        help.Should().Contain("rename");
        help.Should().Contain("delete");
        help.Should().Contain("restore");
        help.Should().Contain("purge");
        help.Should().Contain("status");
    }

    private static CommandContext CreateContext(string[] args)
    {
        return new CommandContext
        {
            Args = args,
            Output = new StringWriter(),
            Formatter = Substitute.For<IOutputFormatter>(),
            Configuration = new Dictionary<string, object>(),
            CancellationToken = CancellationToken.None,
        };
    }
}
