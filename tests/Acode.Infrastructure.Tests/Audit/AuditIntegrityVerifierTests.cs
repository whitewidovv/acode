namespace Acode.Infrastructure.Tests.Audit;

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Acode.Domain.Audit;
using Acode.Infrastructure.Audit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Tests for AuditIntegrityVerifier.
/// Verifies tamper-detection via SHA256 checksums.
/// </summary>
public sealed class AuditIntegrityVerifierTests : IDisposable
{
    private readonly AuditIntegrityVerifier _verifier;
    private readonly string _testDir;

    public AuditIntegrityVerifierTests()
    {
        _verifier = new AuditIntegrityVerifier();
        _testDir = Path.Combine(
            Path.GetTempPath(),
            $"audit_integrity_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
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
    public void Should_Compute_SHA256_Checksum()
    {
        // Arrange
        var content = "test log content\n";
        var logPath = Path.Combine(_testDir, "test.jsonl");
        File.WriteAllText(logPath, content);

        // Act
        var checksum = _verifier.ComputeChecksum(logPath);

        // Assert
        checksum.Should().NotBeNullOrWhiteSpace();
        checksum.Should().HaveLength(
            64,
            because: "SHA-256 produces 64 hex characters");
        checksum.Should().MatchRegex(
            "^[a-f0-9]{64}$",
            because: "checksum should be lowercase hex");

        // Verify manually
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        var expectedChecksum = Convert.ToHexString(hash).ToLowerInvariant();

        checksum.Should().Be(expectedChecksum);
    }

    [Fact]
    public async Task Should_Update_Checksum_OnWrite()
    {
        // Arrange
        var config = new AuditConfiguration
        {
            LogDirectory = _testDir,
        };

        var writer = new FileAuditWriter(
            _testDir,
            config,
            NullLogger<FileAuditWriter>.Instance);

        // Act - write first event
        await writer.WriteAsync(CreateTestEvent("event1"));

        // Find the created file
        var logFiles = Directory.GetFiles(_testDir, "audit-*.jsonl");
        logFiles.Should().HaveCount(1);
        var logPath = logFiles[0];
        var checksumPath = logPath + ".sha256";

        var checksum1 = await File.ReadAllTextAsync(checksumPath);

        // Act - write second event
        await writer.WriteAsync(CreateTestEvent("event2"));
        var checksum2 = await File.ReadAllTextAsync(checksumPath);

        await writer.DisposeAsync();

        // Assert
        checksum1.Should().NotBeNullOrWhiteSpace();
        checksum2.Should().NotBeNullOrWhiteSpace();
        checksum1.Should().NotBe(
            checksum2,
            because: "checksum should change after each write");
    }

    [Fact]
    public void Should_Detect_Modification()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "modify_test.jsonl");
        var checksumPath = logPath + ".sha256";

        var originalContent = "{\"event\":\"test1\"}\n{\"event\":\"test2\"}\n";
        File.WriteAllText(logPath, originalContent);

        var checksum = _verifier.ComputeChecksum(logPath);
        File.WriteAllText(checksumPath, checksum);

        // Verify initial state is valid
        _verifier.Verify(logPath).Should().BeTrue();

        // Act - modify content
        File.WriteAllText(logPath, "{\"event\":\"MODIFIED\"}\n{\"event\":\"test2\"}\n");

        // Assert
        var result = _verifier.Verify(logPath);
        result.Should().BeFalse(because: "modification should be detected");
    }

    [Fact]
    public void Should_Detect_Truncation()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "truncate_test.jsonl");
        var checksumPath = logPath + ".sha256";

        var originalContent = "{\"event\":\"test1\"}\n{\"event\":\"test2\"}\n{\"event\":\"test3\"}\n";
        File.WriteAllText(logPath, originalContent);

        var checksum = _verifier.ComputeChecksum(logPath);
        File.WriteAllText(checksumPath, checksum);

        // Verify initial state
        _verifier.Verify(logPath).Should().BeTrue();

        // Act - truncate file
        File.WriteAllText(logPath, "{\"event\":\"test1\"}\n");

        // Assert
        _verifier.Verify(logPath)
            .Should()
            .BeFalse(because: "truncation should be detected");
    }

    [Fact]
    public void Should_Detect_Insertion()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "insert_test.jsonl");
        var checksumPath = logPath + ".sha256";

        var originalContent = "{\"event\":\"test1\"}\n{\"event\":\"test2\"}\n";
        File.WriteAllText(logPath, originalContent);

        var checksum = _verifier.ComputeChecksum(logPath);
        File.WriteAllText(checksumPath, checksum);

        // Verify initial state
        _verifier.Verify(logPath).Should().BeTrue();

        // Act - insert content in middle
        var modifiedContent = "{\"event\":\"test1\"}\n{\"event\":\"INSERTED\"}\n{\"event\":\"test2\"}\n";
        File.WriteAllText(logPath, modifiedContent);

        // Assert
        _verifier.Verify(logPath)
            .Should()
            .BeFalse(because: "insertion should be detected");
    }

    [Fact]
    public void Should_Write_ChecksumFile()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "checksum_file_test.jsonl");
        var checksumPath = logPath + ".sha256";

        File.WriteAllText(logPath, "{\"event\":\"test\"}\n");

        // Act
        _verifier.WriteChecksumFile(logPath);

        // Assert
        File.Exists(checksumPath).Should().BeTrue();

        var savedChecksum = File.ReadAllText(checksumPath).Trim();
        var computedChecksum = _verifier.ComputeChecksum(logPath);

        savedChecksum.Should().Be(computedChecksum);
    }

    [Fact]
    public void Should_Verify_Valid_Log()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "valid_test.jsonl");
        var content = "{\"event\":\"test1\"}\n{\"event\":\"test2\"}\n";
        File.WriteAllText(logPath, content);
        _verifier.WriteChecksumFile(logPath);

        // Act
        var result = _verifier.Verify(logPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_False_WhenChecksumFileMissing()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "no_checksum.jsonl");
        File.WriteAllText(logPath, "{\"event\":\"test\"}\n");

        // Act
        var result = _verifier.Verify(logPath);

        // Assert
        result.Should().BeFalse(
            because: "missing checksum file indicates potential tampering");
    }

    [Fact]
    public void Should_Return_False_WhenLogFileMissing()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "missing.jsonl");

        // Act
        var result = _verifier.Verify(logPath);

        // Assert
        result.Should().BeFalse(because: "cannot verify non-existent file");
    }

    [Fact]
    public void Should_Handle_EmptyFile()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "empty.jsonl");
        File.WriteAllText(logPath, string.Empty);
        _verifier.WriteChecksumFile(logPath);

        // Act
        var result = _verifier.Verify(logPath);

        // Assert
        result.Should().BeTrue(because: "empty file is valid if checksum matches");
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
}
