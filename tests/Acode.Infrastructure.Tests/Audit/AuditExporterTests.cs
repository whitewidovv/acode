namespace Acode.Infrastructure.Tests.Audit;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Acode.Domain.Audit;
using Acode.Infrastructure.Audit;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for AuditExporter.
/// Verifies export to JSON, CSV, and Text formats.
/// </summary>
public sealed class AuditExporterTests : IDisposable
{
    private readonly string _testDir;
    private readonly AuditExporter _exporter;

    public AuditExporterTests()
    {
        _testDir = Path.Combine(
            Path.GetTempPath(),
            $"audit_export_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _exporter = new AuditExporter();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task Should_Export_ToJson()
    {
        // Arrange
        var logPath = CreateTestLog(5);
        var exportPath = Path.Combine(_testDir, "export.json");

        var options = new AuditExportOptions
        {
            Format = ExportFormat.Json,
        };

        // Act
        await _exporter.ExportAsync(logPath, exportPath, options);

        // Assert
        File.Exists(exportPath).Should().BeTrue();

        var json = await File.ReadAllTextAsync(exportPath);
        var doc = JsonDocument.Parse(json);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().Be(5);
    }

    [Fact]
    public async Task Should_Export_ToCsv()
    {
        // Arrange
        var logPath = CreateTestLog(3);
        var exportPath = Path.Combine(_testDir, "export.csv");

        var options = new AuditExportOptions
        {
            Format = ExportFormat.Csv,
        };

        // Act
        await _exporter.ExportAsync(logPath, exportPath, options);

        // Assert
        File.Exists(exportPath).Should().BeTrue();

        var lines = await File.ReadAllLinesAsync(exportPath);
        lines.Length.Should().BeGreaterThan(3); // Header + 3 events
        lines[0].Should().Contain("EventId"); // CSV header
    }

    [Fact]
    public async Task Should_Export_ToText()
    {
        // Arrange
        var logPath = CreateTestLog(2);
        var exportPath = Path.Combine(_testDir, "export.txt");

        var options = new AuditExportOptions
        {
            Format = ExportFormat.Text,
        };

        // Act
        await _exporter.ExportAsync(logPath, exportPath, options);

        // Assert
        File.Exists(exportPath).Should().BeTrue();

        var text = await File.ReadAllTextAsync(exportPath);
        text.Should().NotBeEmpty();
        text.Should().Contain("EventId:");
    }

    [Fact]
    public async Task Should_Filter_ByDateRange()
    {
        // Arrange
        var logPath = CreateTestLogWithDates();
        var exportPath = Path.Combine(_testDir, "filtered.json");

        var options = new AuditExportOptions
        {
            Format = ExportFormat.Json,
            FromDate = DateTimeOffset.UtcNow.AddDays(-5),
            ToDate = DateTimeOffset.UtcNow.AddDays(-2),
        };

        // Act
        await _exporter.ExportAsync(logPath, exportPath, options);

        // Assert
        var json = await File.ReadAllTextAsync(exportPath);
        var doc = JsonDocument.Parse(json);

        // Should only get events in range
        doc.RootElement.GetArrayLength().Should().BeLessThan(10);
    }

    [Fact]
    public async Task Should_Filter_BySeverity()
    {
        // Arrange
        var logPath = CreateTestLogWithSeverities();
        var exportPath = Path.Combine(_testDir, "warnings.json");

        var options = new AuditExportOptions
        {
            Format = ExportFormat.Json,
            MinSeverity = AuditSeverity.Warning,
        };

        // Act
        await _exporter.ExportAsync(logPath, exportPath, options);

        // Assert
        var json = await File.ReadAllTextAsync(exportPath);
        var doc = JsonDocument.Parse(json);

        // Should only get Warning and Error events
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var severity = element.GetProperty("severity").GetString();
            severity.Should().BeOneOf("warning", "error");
        }
    }

    [Fact]
    public async Task Should_Handle_EmptyLog()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "empty.jsonl");
        File.WriteAllText(logPath, string.Empty);

        var exportPath = Path.Combine(_testDir, "empty_export.json");

        // Act
        await _exporter.ExportAsync(logPath, exportPath, new AuditExportOptions());

        // Assert
        File.Exists(exportPath).Should().BeTrue();

        var json = await File.ReadAllTextAsync(exportPath);
        var doc = JsonDocument.Parse(json);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Should_Handle_NonExistentLog()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "nonexistent.jsonl");
        var exportPath = Path.Combine(_testDir, "export.json");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _exporter.ExportAsync(logPath, exportPath, new AuditExportOptions()));
    }

    private static AuditEvent CreateTestEvent(string source)
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.FileRead,
            Severity = AuditSeverity.Info,
            Source = source,
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>().AsReadOnly(),
        };
    }

    private static AuditEvent CreateTestEventWithSeverity(AuditSeverity severity)
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.FileRead,
            Severity = severity,
            Source = "test",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>().AsReadOnly(),
        };
    }

    private string CreateTestLog(int eventCount)
    {
        var logPath = Path.Combine(_testDir, $"test_{Guid.NewGuid():N}.jsonl");
        var events = Enumerable.Range(0, eventCount)
            .Select(i => CreateTestEvent($"event{i}"))
            .ToList();

        var lines = events.Select(e => JsonSerializer.Serialize(e));
        File.WriteAllLines(logPath, lines);

        return logPath;
    }

    private string CreateTestLogWithDates()
    {
        var logPath = Path.Combine(_testDir, $"dated_{Guid.NewGuid():N}.jsonl");
        var events = new List<string>();

        for (int i = 0; i < 10; i++)
        {
            var evt = CreateTestEvent($"event{i}");
            var json = JsonSerializer.Serialize(evt);
            events.Add(json);
        }

        File.WriteAllLines(logPath, events);
        return logPath;
    }

    private string CreateTestLogWithSeverities()
    {
        var logPath = Path.Combine(_testDir, $"severities_{Guid.NewGuid():N}.jsonl");
        var events = new List<AuditEvent>
        {
            CreateTestEventWithSeverity(AuditSeverity.Info),
            CreateTestEventWithSeverity(AuditSeverity.Info),
            CreateTestEventWithSeverity(AuditSeverity.Warning),
            CreateTestEventWithSeverity(AuditSeverity.Warning),
            CreateTestEventWithSeverity(AuditSeverity.Error),
        };

        var lines = events.Select(e => JsonSerializer.Serialize(e));
        File.WriteAllLines(logPath, lines);

        return logPath;
    }
}
