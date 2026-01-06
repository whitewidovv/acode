using Acode.Domain.Audit;
using Acode.Infrastructure.Audit;
using FluentAssertions;

#pragma warning disable CA2007 // ConfigureAwait not needed in test methods

namespace Acode.Infrastructure.Tests.Audit;

/// <summary>
/// Tests for JsonAuditLogger implementation.
/// </summary>
public sealed class JsonAuditLoggerTests : IDisposable
{
    private readonly string _testLogPath;
    private readonly JsonAuditLogger _logger;

    public JsonAuditLoggerTests()
    {
        _testLogPath = Path.Combine(Path.GetTempPath(), $"test-audit-{Guid.NewGuid()}.jsonl");
        _logger = new JsonAuditLogger(_testLogPath);
    }

    public void Dispose()
    {
        _logger.Dispose();
        if (File.Exists(_testLogPath))
        {
            File.Delete(_testLogPath);
        }
    }

    [Fact]
    public async Task LogAsync_WritesEventToFile()
    {
        // Arrange
        var auditEvent = new AuditEvent
        {
            SchemaVersion = "1.0.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.SessionStart,
            Severity = AuditSeverity.Info,
            Source = "Test",
            OperatingMode = "local-only",
            Data = new Dictionary<string, object> { ["test"] = "value" }
        };

        // Act
        await _logger.LogAsync(auditEvent);
        await _logger.FlushAsync();

        // Assert
        File.Exists(_testLogPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(_testLogPath);
        content.Should().Contain("session_start"); // snake_case from JSON serializer
        content.Should().Contain("\"test\":\"value\"");
    }

    [Fact]
    public async Task LogAsync_MultipleEvents_WritesMultipleLines()
    {
        // Arrange
        var event1 = CreateTestEvent(AuditEventType.SessionStart);
        var event2 = CreateTestEvent(AuditEventType.ConfigLoad);

        // Act
        await _logger.LogAsync(event1);
        await _logger.LogAsync(event2);
        await _logger.FlushAsync();

        // Assert
        var lines = (await File.ReadAllLinesAsync(_testLogPath)).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task FlushAsync_EnsuresDataWritten()
    {
        // Arrange
        var auditEvent = CreateTestEvent(AuditEventType.ConfigLoad);

        // Act
        await _logger.LogAsync(auditEvent);
        await _logger.FlushAsync();

        // Assert
        var fileInfo = new FileInfo(_testLogPath);
        fileInfo.Length.Should().BeGreaterThan(0);
    }

    private static AuditEvent CreateTestEvent(AuditEventType eventType)
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = eventType,
            Severity = AuditSeverity.Info,
            Source = "Test",
            OperatingMode = "local-only",
            Data = new Dictionary<string, object>()
        };
    }
}
