// <copyright file="EventStreamTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.Tests.JSONL;

using System.IO;
using System.Text.Json;
using Acode.Cli.Events;
using Acode.Cli.JSONL;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration tests for event stream functionality.
/// </summary>
public class EventStreamTests : IDisposable
{
    private readonly StringWriter _output;
    private readonly EventEmitter _emitter;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamTests"/> class.
    /// </summary>
    public EventStreamTests()
    {
        _output = new StringWriter();
        var serializer = new EventSerializer();
        _emitter = new EventEmitter(_output, serializer);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _output.Dispose();
    }

    /// <summary>
    /// Verifies session events are properly emitted.
    /// FR-017: "session_start" for session begin.
    /// FR-018: "session_end" for session complete.
    /// </summary>
    [Fact]
    public void Should_Emit_Session_Events()
    {
        _emitter.Emit(new SessionStartEvent { RunId = "test-run-123", Command = "run" });

        _emitter.Emit(new SessionEndEvent { ExitCode = 0, DurationMs = 1000 });

        var output = _output.ToString();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(2);

        // Parse and verify session_start
        var start = JsonDocument.Parse(lines[0]);
        start.RootElement.GetProperty("type").GetString().Should().Be("sessionstart");
        start.RootElement.GetProperty("run_id").GetString().Should().Be("test-run-123");
        start.RootElement.GetProperty("command").GetString().Should().Be("run");

        // Parse and verify session_end
        var end = JsonDocument.Parse(lines[1]);
        end.RootElement.GetProperty("type").GetString().Should().Be("sessionend");
        end.RootElement.GetProperty("exit_code").GetInt32().Should().Be(0);
    }

    /// <summary>
    /// Verifies progress events are properly emitted.
    /// FR-019: "progress" for incremental updates.
    /// </summary>
    [Fact]
    public void Should_Emit_Progress_Events()
    {
        _emitter.Emit(
            new ProgressEvent
            {
                Step = 1,
                Total = 5,
                Percentage = 20,
                Message = "Starting task",
            }
        );

        _emitter.Emit(
            new ProgressEvent
            {
                Step = 2,
                Total = 5,
                Percentage = 40,
                Message = "Processing",
            }
        );

        var output = _output.ToString();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(2);

        var progress1 = JsonDocument.Parse(lines[0]);
        progress1.RootElement.GetProperty("type").GetString().Should().Be("progress");
        progress1.RootElement.GetProperty("step").GetInt32().Should().Be(1);
        progress1.RootElement.GetProperty("percentage").GetInt32().Should().Be(20);
    }

    /// <summary>
    /// Verifies error events include required fields.
    /// FR-024: "error" for error conditions.
    /// </summary>
    [Fact]
    public void Should_Emit_Error_Events()
    {
        _emitter.Emit(
            new ErrorEvent
            {
                Code = "ACODE-FILE-001",
                Message = "File not found: test.cs",
                Component = "FileSystem",
                Remediation = "Check file path",
            }
        );

        var output = _output.ToString();
        var json = JsonDocument.Parse(output.Trim());

        json.RootElement.GetProperty("type").GetString().Should().Be("error");
        json.RootElement.GetProperty("code").GetString().Should().Be("ACODE-FILE-001");
        json.RootElement.GetProperty("message").GetString().Should().Contain("test.cs");
        json.RootElement.GetProperty("component").GetString().Should().Be("FileSystem");
    }

    /// <summary>
    /// Verifies warning events are properly emitted.
    /// FR-025: "warning" for warning conditions.
    /// </summary>
    [Fact]
    public void Should_Emit_Warning_Events()
    {
        _emitter.Emit(
            new WarningEvent
            {
                Code = "ACODE-WARN-001",
                Message = "Large file detected",
                Component = "FileSystem",
            }
        );

        var output = _output.ToString();
        var json = JsonDocument.Parse(output.Trim());

        json.RootElement.GetProperty("type").GetString().Should().Be("warning");
        json.RootElement.GetProperty("code").GetString().Should().Be("ACODE-WARN-001");
    }

    /// <summary>
    /// Verifies approval events can be correlated.
    /// FR-042/FR-046: Approval request and response events.
    /// </summary>
    [Fact]
    public void Should_Correlate_Approval_Events()
    {
        var requestEventId = "evt_123";

        _emitter.Emit(
            new ApprovalRequestEvent
            {
                EventId = requestEventId,
                ActionType = "file_write",
                Context = "Writing to test.cs",
                RiskLevel = "medium",
            }
        );

        _emitter.Emit(
            new ApprovalResponseEvent
            {
                CorrelationId = requestEventId,
                Approved = true,
                Source = "cli_prompt",
            }
        );

        var output = _output.ToString();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(2);

        // Verify correlation
        var request = JsonDocument.Parse(lines[0]);
        var response = JsonDocument.Parse(lines[1]);

        request.RootElement.GetProperty("event_id").GetString().Should().Be(requestEventId);
        response.RootElement.GetProperty("correlation_id").GetString().Should().Be(requestEventId);
    }

    /// <summary>
    /// Verifies action events are properly emitted.
    /// FR-023: "action" for actions taken.
    /// </summary>
    [Fact]
    public void Should_Emit_Action_Events()
    {
        _emitter.Emit(
            new ActionEvent
            {
                ActionType = "file_write",
                Description = "Writing to src/test.cs",
                Success = true,
            }
        );

        var output = _output.ToString();
        var json = JsonDocument.Parse(output.Trim());

        json.RootElement.GetProperty("type").GetString().Should().Be("action");
        json.RootElement.GetProperty("action_type").GetString().Should().Be("file_write");
        json.RootElement.GetProperty("description").GetString().Should().Contain("test.cs");
    }

    /// <summary>
    /// Verifies model events are properly emitted.
    /// FR-026: "model_event" for model operations.
    /// </summary>
    [Fact]
    public void Should_Emit_Model_Events()
    {
        _emitter.Emit(
            new ModelEvent
            {
                ModelId = "llama3.2:7b",
                Operation = "inference",
                TokensUsed = 1500,
                LatencyMs = 2340,
            }
        );

        var output = _output.ToString();
        var json = JsonDocument.Parse(output.Trim());

        json.RootElement.GetProperty("type").GetString().Should().Be("model");
        json.RootElement.GetProperty("model_id").GetString().Should().Be("llama3.2:7b");
    }

    /// <summary>
    /// Verifies file events are properly emitted.
    /// FR-027: "file_event" for file operations.
    /// </summary>
    [Fact]
    public void Should_Emit_File_Events()
    {
        _emitter.Emit(
            new FileEvent
            {
                Operation = "write",
                Path = "src/validation.ts",
                Result = "success",
                Diff = new FileDiff(15, 3),
            }
        );

        var output = _output.ToString();
        var json = JsonDocument.Parse(output.Trim());

        json.RootElement.GetProperty("type").GetString().Should().Be("file");
        json.RootElement.GetProperty("path").GetString().Should().Be("src/validation.ts");
    }

    /// <summary>
    /// Verifies status events include state transitions.
    /// FR-020: "status" for state changes.
    /// </summary>
    [Fact]
    public void Should_Emit_Status_Events()
    {
        _emitter.Emit(new StatusEvent { Status = "EXECUTING", Message = "Plan approved" });

        var output = _output.ToString();
        var json = JsonDocument.Parse(output.Trim());

        json.RootElement.GetProperty("type").GetString().Should().Be("status");
        json.RootElement.GetProperty("status").GetString().Should().Be("EXECUTING");
    }

    /// <summary>
    /// Verifies all events have required base fields.
    /// FR-009/FR-010/FR-012: type, timestamp, event_id required.
    /// </summary>
    [Fact]
    public void Should_Include_Required_Base_Fields()
    {
        _emitter.Emit(new StatusEvent { Status = "test", Message = "test" });

        var output = _output.ToString();
        var json = JsonDocument.Parse(output.Trim());

        json.RootElement.TryGetProperty("type", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("timestamp", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("event_id", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("schema_version", out _).Should().BeTrue();
    }

    /// <summary>
    /// Verifies schema version is included.
    /// FR-015: Events MUST include schema version.
    /// </summary>
    [Fact]
    public void Should_Include_Schema_Version()
    {
        _emitter.Emit(new StatusEvent { Status = "test", Message = "test" });

        var output = _output.ToString();
        var json = JsonDocument.Parse(output.Trim());

        var schemaVersion = json.RootElement.GetProperty("schema_version").GetString();
        schemaVersion.Should().NotBeNullOrEmpty();
        schemaVersion.Should().MatchRegex(@"^\d+\.\d+\.\d+$");
    }

    /// <summary>
    /// Verifies timestamps are ISO 8601 UTC format.
    /// FR-011: Timestamps MUST be ISO 8601 UTC.
    /// </summary>
    [Fact]
    public void Should_Format_Timestamps_ISO8601()
    {
        _emitter.Emit(new StatusEvent { Status = "test", Message = "test" });

        var output = _output.ToString();
        var json = JsonDocument.Parse(output.Trim());

        var timestamp = json.RootElement.GetProperty("timestamp").GetString();
        timestamp.Should().NotBeNull();

        // ISO 8601 format with T separator
        timestamp.Should().Contain("T");
    }

    /// <summary>
    /// Verifies event IDs are unique.
    /// FR-013: Event IDs MUST be unique per session.
    /// </summary>
    [Fact]
    public void Should_Generate_Unique_EventIds()
    {
        var eventIds = new HashSet<string>();

        for (int i = 0; i < 10; i++)
        {
            _emitter.Emit(new StatusEvent { Status = $"test{i}", Message = "msg" });
        }

        var output = _output.ToString();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var json = JsonDocument.Parse(line);
            var eventId = json.RootElement.GetProperty("event_id").GetString();
            eventIds.Add(eventId!);
        }

        eventIds.Should().HaveCount(10);
    }

    /// <summary>
    /// Verifies each event is on a single line.
    /// FR-004: Each event MUST be one line.
    /// </summary>
    [Fact]
    public void Should_Emit_One_Event_Per_Line()
    {
        _emitter.Emit(new StatusEvent { Status = "1", Message = "one" });
        _emitter.Emit(new StatusEvent { Status = "2", Message = "two" });
        _emitter.Emit(new StatusEvent { Status = "3", Message = "three" });

        var output = _output.ToString();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(3);

        // Each line should be valid JSON
        foreach (var line in lines)
        {
            var act = () => JsonDocument.Parse(line);
            act.Should().NotThrow();
        }
    }
}
