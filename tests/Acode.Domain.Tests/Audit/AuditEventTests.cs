namespace Acode.Domain.Tests.Audit;

using System.Text.Json;
using System.Text.RegularExpressions;
using Acode.Domain.Audit;
using FluentAssertions;

/// <summary>
/// Tests for AuditEvent record.
/// Per spec lines 832-1086, validates all required properties and serialization.
/// </summary>
public class AuditEventTests
{
    [Fact]
    public void Should_Generate_Unique_EventId()
    {
        // Arrange & Act
        var event1 = CreateTestEvent();
        var event2 = CreateTestEvent();

        // Assert
        event1.EventId.Should().NotBeNull();
        event2.EventId.Should().NotBeNull();
        event1.EventId.Value.Should().NotBe(
            event2.EventId.Value,
            because: "each event must have unique ID");
        event1.EventId.Value.Should().MatchRegex(
            @"^evt_[a-zA-Z0-9]+$",
            because: "event ID must follow evt_xxx format");
    }

    [Fact]
    public void Should_Include_ISO8601_Timestamp()
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent();

        // Assert
        auditEvent.Timestamp.Should().BeCloseTo(
            DateTimeOffset.UtcNow,
            TimeSpan.FromSeconds(5));

        // Verify ISO 8601 format when serialized
        var json = JsonSerializer.Serialize(auditEvent);
        var iso8601Pattern = @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}";
        Regex.IsMatch(json, iso8601Pattern).Should().BeTrue(
            because: "timestamp must be ISO 8601 format");
    }

    [Fact]
    public void Should_Include_Timezone()
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent();
        var json = JsonSerializer.Serialize(auditEvent);

        // Assert - timestamp should end with Z (UTC) or offset
        var timezonePattern = @"(\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2}))";
        Regex.IsMatch(json, timezonePattern).Should().BeTrue(
            because: "timestamp must include timezone");
    }

    [Fact]
    public void Should_Include_SessionId()
    {
        // Arrange
        var sessionId = SessionId.New();
        var auditEvent = CreateTestEvent(sessionId: sessionId);

        // Assert
        auditEvent.SessionId.Should().Be(sessionId);
        auditEvent.SessionId.Value.Should().MatchRegex(
            @"^sess_[a-zA-Z0-9]+$",
            because: "session ID must follow sess_xxx format");
    }

    [Fact]
    public void Should_Include_CorrelationId()
    {
        // Arrange
        var correlationId = CorrelationId.New();
        var auditEvent = CreateTestEvent(correlationId: correlationId);

        // Assert
        auditEvent.CorrelationId.Should().Be(correlationId);
        auditEvent.CorrelationId.Value.Should().MatchRegex(
            @"^corr_[a-zA-Z0-9]+$",
            because: "correlation ID must follow corr_xxx format");
    }

    [Fact]
    public void Should_Include_EventType()
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent(eventType: AuditEventType.FileWrite);

        // Assert
        auditEvent.EventType.Should().Be(AuditEventType.FileWrite);
        Enum.IsDefined(typeof(AuditEventType), auditEvent.EventType).Should().BeTrue(
            because: "event type must be from defined enumeration");
    }

    [Fact]
    public void Should_Include_Severity()
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent(severity: AuditSeverity.Warning);

        // Assert
        auditEvent.Severity.Should().Be(AuditSeverity.Warning);
        Enum.IsDefined(typeof(AuditSeverity), auditEvent.Severity).Should().BeTrue(
            because: "severity must be from defined enumeration");
    }

    [Theory]
    [InlineData(AuditSeverity.Debug)]
    [InlineData(AuditSeverity.Info)]
    [InlineData(AuditSeverity.Warning)]
    [InlineData(AuditSeverity.Error)]
    [InlineData(AuditSeverity.Critical)]
    public void Should_Support_All_Severity_Levels(AuditSeverity severity)
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent(severity: severity);

        // Assert
        auditEvent.Severity.Should().Be(severity);
    }

    [Fact]
    public void Should_Include_Source()
    {
        // Arrange
        var source = "Acode.Infrastructure.FileSystem";
        var auditEvent = CreateTestEvent(source: source);

        // Assert
        auditEvent.Source.Should().Be(source);
        auditEvent.Source.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Should_Include_SchemaVersion()
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent();

        // Assert
        auditEvent.SchemaVersion.Should().NotBeNullOrWhiteSpace();
        auditEvent.SchemaVersion.Should().MatchRegex(
            @"^\d+\.\d+$",
            because: "schema version must be in X.Y format");
    }

    [Fact]
    public void Should_Include_OperatingMode()
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent(operatingMode: "local_only");

        // Assert
        auditEvent.OperatingMode.Should().Be("local_only");
        auditEvent.OperatingMode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Should_Support_SpanId()
    {
        // Arrange
        var spanId = SpanId.New();
        var auditEvent = CreateTestEvent(spanId: spanId);

        // Assert
        auditEvent.SpanId.Should().Be(spanId);
        auditEvent.SpanId!.Value.Should().MatchRegex(@"^span_[a-zA-Z0-9]+$");
    }

    [Fact]
    public void Should_Support_ParentSpanId()
    {
        // Arrange
        var parentSpanId = SpanId.New();
        var spanId = SpanId.New();
        var auditEvent = CreateTestEvent(spanId: spanId, parentSpanId: parentSpanId);

        // Assert
        auditEvent.ParentSpanId.Should().Be(parentSpanId);
        auditEvent.SpanId.Should().Be(spanId);
        auditEvent.ParentSpanId!.Value.Should().NotBe(auditEvent.SpanId!.Value);
    }

    [Fact]
    public void Should_Serialize_To_ValidJson()
    {
        // Arrange
        var auditEvent = CreateTestEvent(
            data: new Dictionary<string, object>
            {
                ["path"] = "src/Program.cs",
                ["bytes_written"] = 1234,
            });

        // Act
        var json = JsonSerializer.Serialize(auditEvent);

        // Assert
        json.Should().NotBeNullOrWhiteSpace();

        // Should be parseable
        var parsed = JsonDocument.Parse(json);
        parsed.RootElement.GetProperty("EventId").GetProperty("Value").GetString()
            .Should().NotBeNullOrWhiteSpace();
        parsed.RootElement.GetProperty("Timestamp").GetString()
            .Should().NotBeNullOrWhiteSpace();
        parsed.RootElement.GetProperty("EventType").GetInt32()
            .Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Should_Serialize_To_Single_Line()
    {
        // Arrange
        var auditEvent = CreateTestEvent(
            data: new Dictionary<string, object>
            {
                ["multiline"] = "line1\nline2\nline3",
            });

        // Act
        var json = JsonSerializer.Serialize(
            auditEvent,
            new JsonSerializerOptions
            {
                WriteIndented = false,
            });

        // Assert
        json.Should().NotContain(
            "\n",
            because: "JSONL format requires single-line entries");
        json.Should().NotContain(
            "\r",
            because: "JSONL format requires single-line entries");

        // Newlines in data should be escaped
        json.Should().Contain(
            "\\n",
            because: "embedded newlines must be escaped");
    }

    private static AuditEvent CreateTestEvent(
        SessionId? sessionId = null,
        CorrelationId? correlationId = null,
        SpanId? spanId = null,
        SpanId? parentSpanId = null,
        AuditEventType eventType = AuditEventType.FileWrite,
        AuditSeverity severity = AuditSeverity.Info,
        string source = "TestSource",
        string operatingMode = "local_only",
        IDictionary<string, object>? data = null)
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = sessionId ?? SessionId.New(),
            CorrelationId = correlationId ?? CorrelationId.New(),
            SpanId = spanId,
            ParentSpanId = parentSpanId,
            EventType = eventType,
            Severity = severity,
            Source = source,
            OperatingMode = operatingMode,
            Data = (data ?? new Dictionary<string, object>()).AsReadOnly(),
        };
    }
}
