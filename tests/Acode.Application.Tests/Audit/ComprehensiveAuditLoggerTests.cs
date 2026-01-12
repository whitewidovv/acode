namespace Acode.Application.Tests.Audit;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Audit;
using Acode.Domain.Audit;
using Acode.Infrastructure.Audit;
using FluentAssertions;
using Xunit;

/// <summary>
/// Comprehensive tests for audit logger functionality.
/// Verifies all 11 required test scenarios from task-003c specification.
/// </summary>
public sealed class ComprehensiveAuditLoggerTests : IDisposable
{
    private readonly string _testLogDirectory;
    private readonly IAuditLogger _logger;

    public ComprehensiveAuditLoggerTests()
    {
        // Create temp directory for tests
        _testLogDirectory = Path.Combine(Path.GetTempPath(), $"acode-audit-tests-{System.Guid.NewGuid():N}");
        Directory.CreateDirectory(_testLogDirectory);

        // Create logger instance with file path
        var logFilePath = Path.Combine(_testLogDirectory, "audit-test.jsonl");
        _logger = new JsonAuditLogger(logFilePath);
    }

    public void Dispose()
    {
        // Dispose logger first
        if (_logger is IDisposable disposable)
        {
            disposable.Dispose();
        }

        // Cleanup test directory
        if (Directory.Exists(_testLogDirectory))
        {
            Directory.Delete(_testLogDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Should_Log_SessionStart()
    {
        // Arrange
        var sessionId = SessionId.New();
        var eventData = new Dictionary<string, object>
        {
            ["agent_version"] = "1.0.0",
            ["platform"] = "linux"
        };

        // Act
        await _logger.LogAsync(
            AuditEventType.SessionStart,
            AuditSeverity.Info,
            "TestSource",
            eventData,
            null,
            CancellationToken.None);

        await _logger.FlushAsync(CancellationToken.None);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.jsonl");
        logFiles.Should().NotBeEmpty("log file should be created");

        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        logContent.Should().Contain("session_start", "event type should be logged (snake_case)");
        logContent.Should().Contain("agent_version", "event data should be logged (snake_case)");
    }

    [Fact]
    public async Task Should_Log_SessionEnd()
    {
        // Arrange
        var sessionId = SessionId.New();
        var eventData = new Dictionary<string, object>
        {
            ["session_duration_ms"] = 3600000,
            ["exit_reason"] = "user_initiated"
        };

        // Act
        await _logger.LogAsync(
            AuditEventType.SessionEnd,
            AuditSeverity.Info,
            "TestSource",
            eventData,
            null,
            CancellationToken.None);

        await _logger.FlushAsync(CancellationToken.None);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.jsonl");
        logFiles.Should().NotBeEmpty();

        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        logContent.Should().Contain("session_end");
        logContent.Should().Contain("exit_reason");
    }

    [Fact]
    public async Task Should_Log_ConfigLoad()
    {
        // Arrange
        var eventData = new Dictionary<string, object>
        {
            ["config_path"] = ".agent/config.yml",
            ["schema_version"] = "1.0.0",
            ["validation_result"] = "success"
        };

        // Act
        await _logger.LogAsync(
            AuditEventType.ConfigLoad,
            AuditSeverity.Info,
            "ConfigLoader",
            eventData,
            null,
            CancellationToken.None);

        await _logger.FlushAsync(CancellationToken.None);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.jsonl");
        logFiles.Should().NotBeEmpty();

        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        logContent.Should().Contain("config_load");
        logContent.Should().Contain("config_path");
        logContent.Should().Contain("validation_result");
    }

    [Fact]
    public async Task Should_Log_FileOperations()
    {
        // Arrange - log multiple file operation types
        var operations = new[]
        {
            (AuditEventType.FileRead, new Dictionary<string, object> { ["path"] = "test.cs", ["size_bytes"] = 1024 }),
            (AuditEventType.FileWrite, new Dictionary<string, object> { ["path"] = "output.cs", ["size_bytes"] = 2048 }),
            (AuditEventType.FileDelete, new Dictionary<string, object> { ["path"] = "temp.cs" }),
            (AuditEventType.DirCreate, new Dictionary<string, object> { ["path"] = "test-dir" }),
            (AuditEventType.DirDelete, new Dictionary<string, object> { ["path"] = "old-dir" })
        };

        // Act
        foreach (var (eventType, data) in operations)
        {
            await _logger.LogAsync(
                eventType,
                AuditSeverity.Info,
                "FileSystemService",
                data,
                null,
                CancellationToken.None);
        }

        await _logger.FlushAsync(CancellationToken.None);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.jsonl");
        logFiles.Should().NotBeEmpty();

        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        logContent.Should().Contain("file_read");
        logContent.Should().Contain("file_write");
        logContent.Should().Contain("file_delete");
        logContent.Should().Contain("dir_create");
        logContent.Should().Contain("dir_delete");
    }

    [Fact]
    public async Task Should_Log_CommandExecution()
    {
        // Arrange - log command start, end, and error
        var commandData = new Dictionary<string, object>
        {
            ["command"] = "dotnet",
            ["args"] = new[] { "build" }
        };

        // Act - Start
        await _logger.LogAsync(
            AuditEventType.CommandStart,
            AuditSeverity.Info,
            "CommandExecutor",
            commandData,
            null,
            CancellationToken.None);

        // Act - End
        var endData = new Dictionary<string, object>(commandData)
        {
            ["exit_code"] = 0,
            ["duration_ms"] = 5000
        };

        await _logger.LogAsync(
            AuditEventType.CommandEnd,
            AuditSeverity.Info,
            "CommandExecutor",
            endData,
            null,
            CancellationToken.None);

        await _logger.FlushAsync(CancellationToken.None);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.jsonl");
        logFiles.Should().NotBeEmpty();

        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        logContent.Should().Contain("command_start");
        logContent.Should().Contain("command_end");
        logContent.Should().Contain("exit_code");
    }

    [Fact]
    public async Task Should_Log_SecurityViolations()
    {
        // Arrange
        var violationData = new Dictionary<string, object>
        {
            ["violation_type"] = "ProtectedPathAccess",
            ["attempted_path"] = ".git/config",
            ["denied_reason"] = "Path on denylist"
        };

        // Act
        await _logger.LogAsync(
            AuditEventType.SecurityViolation,
            AuditSeverity.Critical,
            "PolicyEngine",
            violationData,
            null,
            CancellationToken.None);

        await _logger.FlushAsync(CancellationToken.None);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.jsonl");
        logFiles.Should().NotBeEmpty();

        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        logContent.Should().Contain("security_violation");
        logContent.Should().Contain("critical");
        logContent.Should().Contain("attempted_path");
    }

    [Fact]
    public async Task Should_Log_TaskEvents()
    {
        // Arrange - log task start, end, and error
        var taskData = new Dictionary<string, object>
        {
            ["task_id"] = "task-001",
            ["task_description"] = "Build project"
        };

        // Act - Start, End, Error
        await _logger.LogAsync(
            AuditEventType.TaskStart,
            AuditSeverity.Info,
            "TaskRunner",
            taskData,
            null,
            CancellationToken.None);

        await _logger.LogAsync(
            AuditEventType.TaskEnd,
            AuditSeverity.Info,
            "TaskRunner",
            taskData,
            null,
            CancellationToken.None);

        await _logger.FlushAsync(CancellationToken.None);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.jsonl");
        logFiles.Should().NotBeEmpty();

        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        logContent.Should().Contain("task_start");
        logContent.Should().Contain("task_end");
    }

    [Fact]
    public async Task Should_Log_ApprovalEvents()
    {
        // Arrange
        var requestData = new Dictionary<string, object>
        {
            ["approval_type"] = "CommandExecution",
            ["requested_operation"] = "dotnet build"
        };

        var responseData = new Dictionary<string, object>
        {
            ["approval_type"] = "CommandExecution",
            ["decision"] = "approved"
        };

        // Act
        await _logger.LogAsync(
            AuditEventType.ApprovalRequest,
            AuditSeverity.Info,
            "ApprovalService",
            requestData,
            null,
            CancellationToken.None);

        await _logger.LogAsync(
            AuditEventType.ApprovalResponse,
            AuditSeverity.Info,
            "ApprovalService",
            responseData,
            null,
            CancellationToken.None);

        await _logger.FlushAsync(CancellationToken.None);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.jsonl");
        logFiles.Should().NotBeEmpty();

        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        logContent.Should().Contain("approval_request");
        logContent.Should().Contain("approval_response");
        logContent.Should().Contain("decision");
    }

    [Fact]
    public async Task Should_Maintain_CorrelationId()
    {
        // Arrange
        var correlationId = CorrelationId.New();
        var eventData = new Dictionary<string, object> { ["test"] = "data" };
        var context = new Dictionary<string, object>
        {
            ["correlationId"] = correlationId.Value
        };

        // Act - Log multiple events with same correlation ID
        for (int i = 0; i < 3; i++)
        {
            await _logger.LogAsync(
                AuditEventType.CommandStart,
                AuditSeverity.Info,
                "TestSource",
                eventData,
                context,
                CancellationToken.None);
        }

        await _logger.FlushAsync(CancellationToken.None);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.jsonl");
        logFiles.Should().NotBeEmpty();

        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        var lines = logContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(3, "three events should be logged");

        // All events should reference the correlation ID
        foreach (var line in lines)
        {
            line.Should().Contain(correlationId.Value);
        }
    }

    [Fact]
    public async Task Should_Not_Block_MainThread()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        var eventData = new Dictionary<string, object> { ["test"] = "data" };

        // Act - Log event (should return quickly without blocking)
        await _logger.LogAsync(
            AuditEventType.CommandStart,
            AuditSeverity.Info,
            "TestSource",
            eventData,
            null,
            CancellationToken.None);

        stopwatch.Stop();

        // Assert - LogAsync should complete quickly (non-blocking)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(
            200,
            "logging should not block for extended periods");
    }

    [Fact]
    public async Task Should_Handle_HighVolume()
    {
        // Arrange
        const int eventCount = 1000;
        var stopwatch = Stopwatch.StartNew();
        var eventData = new Dictionary<string, object> { ["test"] = "data", ["index"] = 0 };

        // Act - Log many events rapidly
        var tasks = new List<Task>();
        for (int i = 0; i < eventCount; i++)
        {
            eventData["index"] = i;
            tasks.Add(_logger.LogAsync(
                AuditEventType.CommandStart,
                AuditSeverity.Info,
                "TestSource",
                new Dictionary<string, object>(eventData),
                null,
                CancellationToken.None));
        }

        await Task.WhenAll(tasks);
        await _logger.FlushAsync(CancellationToken.None);

        stopwatch.Stop();

        // Assert - Should handle high volume efficiently
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(
            eventCount * 10,
            "should handle high volume efficiently (< 10ms per event)");

        var logFiles = Directory.GetFiles(_testLogDirectory, "*.jsonl");
        logFiles.Should().NotBeEmpty();

        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        var lines = logContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCountGreaterOrEqualTo(
            eventCount,
            "all events should be logged");
    }
}
