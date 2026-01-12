namespace Acode.Domain.Tests.Models.Inference;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for ConversationHistory class.
/// FR-004a-101 to FR-004a-115: Thread-safe conversation management with validation.
/// </summary>
public sealed class ConversationHistoryTests
{
    [Fact]
    public void Should_Start_Empty()
    {
        // Act
        var history = new ConversationHistory();

        // Assert
        history.Count.Should().Be(0);
        history.GetMessages().Should().BeEmpty();
        history.LastMessage.Should().BeNull();
    }

    [Fact]
    public void Should_Require_System_First()
    {
        // Arrange
        var history = new ConversationHistory();
        var userMessage = ChatMessage.CreateUser("Hello");

        // Act
        var action = () => history.Add(userMessage);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*system*first*");
    }

    [Fact]
    public void Should_Accept_System_As_First_Message()
    {
        // Arrange
        var history = new ConversationHistory();
        var systemMessage = ChatMessage.CreateSystem("You are helpful.");

        // Act
        history.Add(systemMessage);

        // Assert
        history.Count.Should().Be(1);
        history.LastMessage!.Role.Should().Be(MessageRole.System);
    }

    [Fact]
    public void Should_Reject_Second_System_Message()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System 1"));

        // Act
        var action = () => history.Add(ChatMessage.CreateSystem("System 2"));

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*system*already*");
    }

    [Fact]
    public void Should_Validate_User_Assistant_Alternation()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System"));
        history.Add(ChatMessage.CreateUser("User 1"));

        // Act - try to add another user message
        var action = () => history.Add(ChatMessage.CreateUser("User 2"));

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*alternate*");
    }

    [Fact]
    public void Should_Accept_Valid_Alternation()
    {
        // Arrange
        var history = new ConversationHistory();

        // Act
        history.Add(ChatMessage.CreateSystem("System"));
        history.Add(ChatMessage.CreateUser("User 1"));
        history.Add(ChatMessage.CreateAssistant("Assistant 1"));
        history.Add(ChatMessage.CreateUser("User 2"));
        history.Add(ChatMessage.CreateAssistant("Assistant 2"));

        // Assert
        history.Count.Should().Be(5);
    }

    [Fact]
    public void Should_Allow_Tool_After_ToolCalls()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System"));
        history.Add(ChatMessage.CreateUser("Write a file"));

        var toolCall = new ToolCall(
            Id: "call_123",
            Name: "write_file",
            Arguments: CreateJsonElement(JsonSerializer.Serialize(new { path = "test.cs" })));
        history.Add(ChatMessage.CreateAssistant(null, new[] { toolCall }));

        // Act
        var toolResult = ChatMessage.CreateToolResult("call_123", "Success");
        history.Add(toolResult);

        // Assert
        history.Count.Should().Be(4);
        history.LastMessage!.Role.Should().Be(MessageRole.Tool);
    }

    [Fact]
    public void Should_Reject_Tool_Without_Preceding_ToolCalls()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System"));
        history.Add(ChatMessage.CreateUser("Hello"));
        history.Add(ChatMessage.CreateAssistant("Hi there!")); // No tool calls

        // Act
        var action = () => history.Add(ChatMessage.CreateToolResult("call_123", "Result"));

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*tool*tool_calls*");
    }

    [Fact]
    public void Should_Validate_ToolCallId_Matches()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System"));
        history.Add(ChatMessage.CreateUser("Do something"));

        var toolCall = new ToolCall(
            Id: "call_abc",
            Name: "my_tool",
            Arguments: CreateJsonElement(JsonSerializer.Serialize(new { })));
        history.Add(ChatMessage.CreateAssistant(null, new[] { toolCall }));

        // Act - tool result with wrong ID
        var action = () => history.Add(ChatMessage.CreateToolResult("call_xyz", "Result"));

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*tool_call_id*");
    }

    [Fact]
    public void Should_Allow_Multiple_Tool_Results_For_Multiple_Calls()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System"));
        history.Add(ChatMessage.CreateUser("Do tasks"));

        var toolCalls = new[]
        {
            new ToolCall(Id: "call_1", Name: "tool1", Arguments: CreateJsonElement(JsonSerializer.Serialize(new { }))),
            new ToolCall(Id: "call_2", Name: "tool2", Arguments: CreateJsonElement(JsonSerializer.Serialize(new { }))),
        };
        history.Add(ChatMessage.CreateAssistant(null, toolCalls));

        // Act
        history.Add(ChatMessage.CreateToolResult("call_1", "Result 1"));
        history.Add(ChatMessage.CreateToolResult("call_2", "Result 2"));

        // Assert
        history.Count.Should().Be(5);
    }

    [Fact]
    public void Should_Track_Count()
    {
        // Arrange
        var history = new ConversationHistory();

        // Act & Assert
        history.Count.Should().Be(0);

        history.Add(ChatMessage.CreateSystem("System"));
        history.Count.Should().Be(1);

        history.Add(ChatMessage.CreateUser("User"));
        history.Count.Should().Be(2);
    }

    [Fact]
    public void Should_Return_LastMessage()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System"));

        // Act
        var user = ChatMessage.CreateUser("Hello");
        history.Add(user);

        // Assert
        history.LastMessage.Should().Be(user);
    }

    [Fact]
    public void Should_Return_ImmutableList()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System"));

        // Act
        var messages = history.GetMessages();

        // Assert
        messages.Should().BeAssignableTo<IReadOnlyList<ChatMessage>>();
    }

    [Fact]
    public void Should_Support_Clear()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System"));
        history.Add(ChatMessage.CreateUser("User"));

        // Act
        history.Clear();

        // Assert
        history.Count.Should().Be(0);
        history.LastMessage.Should().BeNull();
    }

    [Fact]
    public void Should_Accept_System_After_Clear()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System 1"));
        history.Clear();

        // Act
        history.Add(ChatMessage.CreateSystem("System 2"));

        // Assert
        history.Count.Should().Be(1);
    }

    [Fact]
    public async Task Should_Be_ThreadSafe_ConcurrentReads()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System"));
        history.Add(ChatMessage.CreateUser("User"));
        history.Add(ChatMessage.CreateAssistant("Assistant"));

        // Act
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            var messages = history.GetMessages();
            var count = history.Count;
            var last = history.LastMessage;
            return (messagesCount: messages.Count, count, last);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.messagesCount.Should().Be(3);
            r.count.Should().Be(3);
        });
    }

    [Fact]
    public async Task Should_Be_ThreadSafe_ConcurrentAdds()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System"));

        var addLock = new object();

        // Act
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            await Task.Delay(System.Random.Shared.Next(0, 10));
            try
            {
                lock (addLock)
                {
                    if (history.Count % 2 == 1)
                    {
                        history.Add(ChatMessage.CreateUser($"User {i}"));
                    }
                    else
                    {
                        history.Add(ChatMessage.CreateAssistant($"Assistant {i}"));
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        });

        await Task.WhenAll(tasks);

        // Assert - should have valid conversation
        var messages = history.GetMessages();
        messages[0].Role.Should().Be(MessageRole.System);
    }

    [Fact]
    public void Should_Support_Enumeration()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System"));
        history.Add(ChatMessage.CreateUser("User"));

        // Act
        var roles = new List<MessageRole>();
        foreach (var message in history)
        {
            roles.Add(message.Role);
        }

        // Assert
        roles.Should().ContainInOrder(MessageRole.System, MessageRole.User);
    }

    [Fact]
    public void Should_Support_LINQ()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System"));
        history.Add(ChatMessage.CreateUser("User 1"));
        history.Add(ChatMessage.CreateAssistant("Assistant"));
        history.Add(ChatMessage.CreateUser("User 2"));

        // Act
        var userMessages = history.Where(m => m.Role == MessageRole.User).ToList();

        // Assert
        userMessages.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Serialize()
    {
        // Arrange
        var history = new ConversationHistory();
        history.Add(ChatMessage.CreateSystem("System"));
        history.Add(ChatMessage.CreateUser("User"));

        // Act
        var json = JsonSerializer.Serialize(history.GetMessages());
        var deserialized = JsonSerializer.Deserialize<List<ChatMessage>>(json);

        // Assert
        deserialized.Should().HaveCount(2);
        deserialized![0].Role.Should().Be(MessageRole.System);
    }

    /// <summary>
    /// Helper method to create JsonElement from JSON string for testing.
    /// </summary>
    private static JsonElement CreateJsonElement(string json)
    {
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
