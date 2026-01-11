# Task 003c - Gap Analysis and Implementation Checklist

## Purpose
This checklist tracks ONLY the gaps (missing or incomplete items) needed to complete Task 003c: Define Audit Baseline Requirements. Each gap is ordered for TDD implementation (tests before production code).

## Instructions for Resuming Agent
1. Read this checklist from top to bottom
2. Find the first gap marked [🔄] (in progress) or [ ] (not started)
3. Implement that gap following TDD: RED → GREEN → REFACTOR
4. Mark gap [✅] when complete with evidence
5. Commit and push after each gap
6. Move to next gap
7. When all gaps are [✅], run final audit per docs/AUDIT-GUIDELINES.md

## WHAT EXISTS (Already Complete)

### Domain Layer - Existing Files
✅ `src/Acode.Domain/Audit/AuditEvent.cs` - EXISTS but INCOMPLETE (missing SpanId and ParentSpanId properties)
✅ `src/Acode.Domain/Audit/AuditEventType.cs` - COMPLETE (all 25 event types defined)
✅ `src/Acode.Domain/Audit/AuditSeverity.cs` - COMPLETE (all 5 severity levels defined)
✅ `src/Acode.Domain/Audit/EventId.cs` - EXISTS but uses Guid format (spec requires evt_xxx format)
✅ `src/Acode.Domain/Audit/SessionId.cs` - EXISTS but uses Guid format (spec requires sess_xxx format)
✅ `src/Acode.Domain/Audit/CorrelationId.cs` - EXISTS but uses Guid format (spec requires corr_xxx format)

### Application Layer - Existing Files
✅ `src/Acode.Application/Audit/IAuditLogger.cs` - EXISTS but INCOMPLETE (missing several methods)

### Infrastructure Layer - Existing Files
✅ `src/Acode.Infrastructure/Audit/JsonAuditLogger.cs` - EXISTS (need to verify completeness)

### Test Layer - Existing Files
✅ `tests/Acode.Domain.Tests/Audit/AuditEventTypeTests.cs` - COMPLETE (validates enum completeness)
✅ `tests/Acode.Application.Tests/Audit/AuditLoggerTests.cs` - EXISTS (need to verify against spec)
✅ `tests/Acode.Infrastructure.Tests/Audit/JsonAuditLoggerTests.cs` - EXISTS (need to verify completeness)

## GAPS IDENTIFIED (What's Missing or Incomplete)

### Gap #1: Value Objects Format Compliance
**Status**: [✅]
**Files to Modify**:
- `src/Acode.Domain/Audit/EventId.cs`
- `src/Acode.Domain/Audit/SessionId.cs`
- `src/Acode.Domain/Audit/CorrelationId.cs`

**Why Needed**: Testing Requirements line 873-874, 917-918, 930-931 require format:
- EventId.Value should match pattern `evt_[a-zA-Z0-9]+`
- SessionId.Value should match pattern `sess_[a-zA-Z0-9]+`
- CorrelationId.Value should match pattern `corr_[a-zA-Z0-9]+`

Currently these use Guid.ToString() which returns format like "123e4567-e89b-12d3-a456-426614174000"

**Required Changes**:
1. Change Value property type from `Guid` to `string`
2. Update New() method to generate prefixed format: `evt_` + base62-encoded guid
3. Update ToString() to return Value directly
4. Update constructor validation to check format pattern
5. Ensure backward compatibility if needed

**Testing Pattern**: From spec line 862-875 (EventId), 909-919 (SessionId), 922-932 (CorrelationId)

**Success Criteria**:
- All three value objects generate and validate correct format
- Tests pass: Should_Generate_Unique_EventId(), Should_Include_SessionId(), Should_Include_CorrelationId()
- Regex patterns match: `^evt_[a-zA-Z0-9]+$`, `^sess_[a-zA-Z0-9]+$`, `^corr_[a-zA-Z0-9]+$`

**Evidence**:
- Commit 898306d: feat(task-003c): Gap #1 complete - value objects now use correct format
- All 27 tests passing (9 tests each for EventId, SessionId, CorrelationId)
- Format validation working: evt_xxx, sess_xxx, corr_xxx patterns
- Base62 encoding implemented for compact IDs
- Test run output: "Passed! - Failed: 0, Passed: 27, Skipped: 0, Total: 27"

---

### Gap #2: SpanId Value Object
**Status**: [✅]
**File to Create**: `src/Acode.Domain/Audit/SpanId.cs`

**Why Needed**: Testing Requirements line 1009-1018, 1021-1032 require SpanId support. Implementation Prompt line 5144, 5200-5201 defines SpanId as required value object.

**Required Implementation**:
```csharp
namespace Acode.Domain.Audit;

public sealed record SpanId
{
    public SpanId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SpanId cannot be null or whitespace", nameof(value));
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^span_[a-zA-Z0-9]+$"))
            throw new ArgumentException("SpanId must match format span_xxx", nameof(value));
        Value = value;
    }

    public string Value { get; }

    public static SpanId New() => new($"span_{GenerateId()}");

    public override string ToString() => Value;

    private static string GenerateId()
    {
        // Base62 encode a Guid for compact format
        // Implementation similar to EventId.New()
    }
}
```

**Testing Pattern**: From spec line 1009-1032

**Success Criteria**:
- SpanId.New() generates unique IDs matching `^span_[a-zA-Z0-9]+$`
- Tests pass: Should_Support_SpanId(), Should_Support_ParentSpanId()

**Evidence**:
- SpanId.cs created with complete implementation
- SpanIdTests.cs created with 9 tests
- All 9 tests passing
- Format validation: span_[a-zA-Z0-9]+

---

### Gap #3: Add SpanId and ParentSpanId to AuditEvent
**Status**: [✅]
**File to Modify**: `src/Acode.Domain/Audit/AuditEvent.cs`

**Why Needed**: Implementation Prompt line 5200-5201 defines SpanId and ParentSpanId as optional properties. Testing Requirements line 1009-1032 validate these properties.

**Required Changes**:
Add to AuditEvent record:
```csharp
public SpanId? SpanId { get; init; }
public SpanId? ParentSpanId { get; init; }
```

**Testing Pattern**: From spec line 1009-1032

**Success Criteria**:
- AuditEvent can be created with SpanId and ParentSpanId
- Properties are optional (nullable)
- Tests pass: Should_Support_SpanId(), Should_Support_ParentSpanId()

**Evidence**:
- Added SpanId? and ParentSpanId? properties to AuditEvent
- Build successful, no errors
- Properties are nullable as per spec

---

### Gap #4: AuditEvent Tests File
**Status**: [✅]
**File to Create**: `tests/Acode.Domain.Tests/Audit/AuditEventTests.cs`

**Why Needed**: Testing Requirements line 831-1115 defines comprehensive test suite for AuditEvent with 13 tests.

**Required Tests** (from spec line 832-845):
1. Should_Generate_Unique_EventId() - line 862-875
2. Should_Include_ISO8601_Timestamp() - line 877-893
3. Should_Include_Timezone() - line 895-906
4. Should_Include_SessionId() - line 909-919
5. Should_Include_CorrelationId() - line 922-932
6. Should_Include_EventType() - line 935-944
7. Should_Include_Severity() - line 947-956
8. Should_Support_All_Severity_Levels() - line 958-971
9. Should_Include_Source() - line 974-983
10. Should_Include_SchemaVersion() - line 986-995
11. Should_Include_OperatingMode() - line 998-1006
12. Should_Support_SpanId() - line 1009-1018
13. Should_Support_ParentSpanId() - line 1021-1032
14. Should_Serialize_To_ValidJson() - line 1035-1059
15. Should_Serialize_To_Single_Line() - line 1062-1086

**Implementation Pattern**: Complete test code provided in spec line 850-1115

**Success Criteria**:
- All 15 tests implemented exactly as specified
- Tests use FluentAssertions
- Tests verify immutability, serialization, format compliance
- All tests pass

**Evidence**:
- AuditEventTests.cs created with 15 test methods
- All 19 test executions passing (includes theory with 5 cases)
- Tests validate: ID formats, timestamps, serialization, span hierarchy
- JSON serialization to single-line JSONL format verified

---

### Gap #5: Expand IAuditLogger Interface
**Status**: [🔄]
**File to Modify**: `src/Acode.Application/Audit/IAuditLogger.cs`

**Why Needed**: Implementation Prompt line 5252-5284 defines complete IAuditLogger interface with 5 methods. Current implementation only has 2 methods.

**Missing Methods**:
```csharp
// Already exists: Task LogAsync(AuditEvent auditEvent);

// Missing method 1:
Task LogAsync(
    AuditEventType eventType,
    AuditSeverity severity,
    string source,
    IDictionary<string, object> data,
    IDictionary<string, object>? context = null);

// Missing method 2:
IDisposable BeginCorrelation(string description);

// Missing method 3:
IDisposable BeginSpan(string operation);

// Already exists: Task FlushAsync();
```

**Testing Pattern**: From spec line 1174-1225 (logging methods), correlation/span tracking tests

**Success Criteria**:
- Interface has all 5 methods as defined in spec
- Method signatures match exactly
- XML documentation added

**Evidence**: [To be filled when complete]

---

### Gap #6: Domain Supporting Types
**Status**: [ ]
**Files to Create**:
- `src/Acode.Domain/Audit/AuditSession.cs`
- `src/Acode.Domain/Audit/CorrelationContext.cs`
- `src/Acode.Domain/Audit/SpanContext.cs`
- `src/Acode.Domain/Audit/AuditConfiguration.cs`

**Why Needed**: Implementation Prompt line 5136-5139 defines these as required domain types.

**Implementation Guidance**:
- AuditSession: Represents a bounded audit session (start to end)
- CorrelationContext: Manages correlation ID scope
- SpanContext: Manages span hierarchy and parent-child relationships
- AuditConfiguration: Settings for audit behavior (retention, rotation, etc.)

**Testing Pattern**: Unit tests for each type verifying behavior

**Success Criteria**:
- All four types exist with complete implementation
- Tests validate key behaviors
- Integration with IAuditLogger

**Evidence**: [To be filled when complete]

---

### Gap #7: Infrastructure - FileAuditWriter
**Status**: [ ]
**File to Create**: `src/Acode.Infrastructure/Audit/FileAuditWriter.cs`

**Why Needed**: Implementation Prompt line 5287-5349 provides complete implementation of FileAuditWriter (JSONL format with integrity checksums).

**Required Implementation**:
- Write audit events to JSONL files (one JSON object per line)
- Append-only writes (tamper-evident)
- Incremental SHA256 checksums (.sha256 sidecar files)
- Log rotation logic
- Concurrent write handling with SemaphoreSlim
- Log injection prevention (no newlines in JSON)

**Implementation Pattern**: Complete code provided in spec line 5289-5349

**Success Criteria**:
- JSONL files created correctly
- Checksums updated after each write
- Rotation works when size/time thresholds met
- Thread-safe concurrent writes
- Tests verify all behaviors

**Evidence**: [To be filled when complete]

---

### Gap #8: Infrastructure - AuditLogRotator
**Status**: [✅]
**File to Create**: `src/Acode.Infrastructure/Audit/AuditLogRotator.cs`

**Why Needed**: Implementation Prompt line 5165 lists this as required infrastructure component.

**Required Implementation**:
- Rotate logs based on size threshold (e.g., 10MB)
- Rotate logs based on time threshold (e.g., daily)
- Archive old logs with timestamp in filename
- Maintain high-water mark for continuity

**Testing Pattern**: Unit tests for rotation logic

**Success Criteria**:
- Size-based rotation works
- Time-based rotation works
- Old logs properly archived
- No data loss during rotation

**Evidence**: All 10 tests passed
- AuditLogRotator.cs (173 lines) with RotateIfNeededAsync, CleanupExpiredLogsAsync, EnforceStorageLimitAsync
- RotationResult.cs (17 lines) return type
- AuditLogRotatorTests.cs (270 lines) with 10 test methods covering:
  - Size-based rotation
  - Skip rotation when under limit
  - Unix permission preservation
  - Atomic operations (no data loss)
  - Expired log cleanup (retention policy)
  - Storage limit enforcement
  - Sequential numbering (.1, .2, .3, etc.)
  - Deletion event logging (OnBeforeDelete)
  - Non-existent file handling
  - Non-.jsonl file filtering
- Added MaxFileSize and MaxTotalStorage properties to AuditConfiguration.cs

---

### Gap #9: Infrastructure - AuditIntegrityVerifier
**Status**: [✅]
**File to Create**: `src/Acode.Infrastructure/Audit/AuditIntegrityVerifier.cs`

**Why Needed**: Implementation Prompt line 5166 lists this as required infrastructure component. Required for `audit verify` CLI command.

**Required Implementation**:
- Read JSONL file and compute SHA256
- Compare against .sha256 sidecar file
- Report any mismatches (tampering detected)
- Verify JSON parse correctness
- Verify schema version compatibility

**Testing Pattern**: Unit tests with known-good and tampered logs

**Success Criteria**:
- Detects modified log entries
- Detects deleted log entries
- Verifies checksums correctly
- Reports clear errors

**Evidence**: All 10 tests passed
- AuditIntegrityVerifier.cs (92 lines) with three methods:
  - ComputeChecksum: SHA256 hash computation
  - WriteChecksumFile: Create .sha256 sidecar file
  - Verify: Compare file checksum against sidecar
- AuditIntegrityVerifierTests.cs (265 lines) with 10 test methods covering:
  - SHA256 checksum computation (64 hex chars, lowercase)
  - Checksum updates on each write
  - Modification detection
  - Truncation detection
  - Insertion detection
  - Checksum file creation
  - Valid log verification
  - Missing checksum file handling
  - Missing log file handling
  - Empty file verification

---

### Gap #10: Infrastructure - AuditRedactor
**Status**: [ ]
**File to Create**: `src/Acode.Infrastructure/Audit/AuditRedactor.cs`

**Why Needed**: Implementation Prompt line 5354-5421 provides complete implementation for redacting sensitive data.

**Required Implementation**:
- Regex patterns for detecting secrets (password, token, api_key, bearer, PEM keys)
- Redact string values before logging
- Redact dictionary values based on key patterns
- Replace sensitive data with `[REDACTED]` marker

**Implementation Pattern**: Complete code provided in spec line 5354-5421

**Success Criteria**:
- All sensitive patterns detected and redacted
- Tests verify each pattern type
- No false positives on non-sensitive data

**Evidence**: [To be filled when complete]

---

### Gap #11: Infrastructure - AuditExporter
**Status**: [ ]
**File to Create**: `src/Acode.Infrastructure/Audit/AuditExporter.cs`

**Why Needed**: Implementation Prompt line 5168 lists this as required infrastructure component. Required for `audit export` CLI command.

**Required Implementation**:
- Export audit logs to JSON (array of events)
- Export audit logs to CSV
- Export audit logs to plain text
- Filter by date range, event type, severity
- Redact sensitive data in export

**Testing Pattern**: Unit tests for each export format

**Success Criteria**:
- All three formats work correctly
- Filtering works as expected
- Exported data is valid and complete

**Evidence**: [To be filled when complete]

---

### Gap #12: Infrastructure - AuditConfigurationLoader
**Status**: [ ]
**File to Create**: `src/Acode.Infrastructure/Audit/AuditConfigurationLoader.cs`

**Why Needed**: Implementation Prompt line 5169 lists this as required infrastructure component.

**Required Implementation**:
- Load audit config from .agent/config.yml
- Parse audit section: log_level, retention_days, rotation_size_mb, rotation_interval
- Validate configuration values
- Provide defaults for missing values

**Testing Pattern**: Unit tests with various config files

**Success Criteria**:
- Valid configs load correctly
- Invalid configs rejected with clear errors
- Defaults applied when config missing

**Evidence**: [To be filled when complete]

---

### Gap #13: Application - Audit Commands
**Status**: [ ]
**Files to Create**:
- `src/Acode.Application/Audit/Commands/StartAuditSessionCommand.cs`
- `src/Acode.Application/Audit/Commands/EndAuditSessionCommand.cs`
- `src/Acode.Application/Audit/Commands/LogEventCommand.cs`
- `src/Acode.Application/Audit/Commands/CleanupLogsCommand.cs`

**Why Needed**: Implementation Prompt line 5147-5152 defines these as required application commands.

**Required Implementation**:
- Each command follows CQRS pattern
- Commands have handlers
- Commands integrate with IAuditLogger
- Commands validate inputs

**Testing Pattern**: Unit tests for each command and handler

**Success Criteria**:
- All four commands implemented
- Handlers work correctly
- Integration with domain layer

**Evidence**: [To be filled when complete]

---

### Gap #14: Application - Audit Queries
**Status**: [ ]
**Files to Create**:
- `src/Acode.Application/Audit/Queries/ListSessionsQuery.cs`
- `src/Acode.Application/Audit/Queries/GetSessionEventsQuery.cs`
- `src/Acode.Application/Audit/Queries/SearchEventsQuery.cs`
- `src/Acode.Application/Audit/Queries/GetAuditStatsQuery.cs`

**Why Needed**: Implementation Prompt line 5153-5156 defines these as required application queries.

**Required Implementation**:
- Each query follows CQRS pattern
- Queries have handlers
- Queries read from audit logs
- Queries support filtering and pagination

**Testing Pattern**: Unit tests for each query and handler

**Success Criteria**:
- All four queries implemented
- Handlers return correct data
- Filtering works as expected

**Evidence**: [To be filled when complete]

---

### Gap #15: Application - AuditService
**Status**: [ ]
**File to Create**: `src/Acode.Application/Audit/Services/AuditService.cs`

**Why Needed**: Implementation Prompt line 5159 lists this as required application service.

**Required Implementation**:
- Orchestrates audit operations
- Manages session lifecycle
- Coordinates commands and queries
- Handles correlation tracking

**Testing Pattern**: Unit tests with mocked dependencies

**Success Criteria**:
- Service orchestrates audit operations correctly
- Session lifecycle managed properly
- Integration with commands/queries

**Evidence**: [To be filled when complete]

---

### Gap #16: Application - CorrelationService
**Status**: [ ]
**File to Create**: `src/Acode.Application/Audit/Services/CorrelationService.cs`

**Why Needed**: Implementation Prompt line 5160 lists this as required application service.

**Required Implementation**:
- Manage correlation ID scope (AsyncLocal)
- BeginCorrelation() creates new scope
- Correlation ID accessible throughout async call chain
- Correlation scope properly disposed

**Testing Pattern**: Unit tests with async scenarios

**Success Criteria**:
- Correlation ID propagates correctly
- Scopes nest properly
- Disposal cleans up correctly

**Evidence**: [To be filled when complete]

---

### Gap #17: CLI - Audit Commands Directory
**Status**: [ ]
**Directory to Create**: `src/Acode.Cli/Commands/Audit/`

**Files to Create**:
- `AuditListCommand.cs` - List audit sessions
- `AuditShowCommand.cs` - Show events for a session
- `AuditSearchCommand.cs` - Search events by criteria
- `AuditVerifyCommand.cs` - Verify log integrity
- `AuditExportCommand.cs` - Export logs to various formats
- `AuditStatsCommand.cs` - Show audit statistics
- `AuditTailCommand.cs` - Follow audit log in real-time
- `AuditCleanupCommand.cs` - Clean up old audit logs

**Why Needed**: Implementation Prompt line 5171-5181 defines all 8 CLI commands as required.

**Required Implementation**:
Each command should:
- Parse CLI arguments
- Invoke appropriate query/command
- Format output for console
- Handle errors gracefully
- Return appropriate exit codes

**Testing Pattern**: Integration tests for each command

**Success Criteria**:
- All 8 commands implemented
- Commands work from CLI
- Output is user-friendly
- Error handling is robust

**Evidence**: [To be filled when complete]

---

### Gap #18: Comprehensive AuditLogger Tests
**Status**: [ ]
**File to Verify/Expand**: `tests/Acode.Application.Tests/Audit/AuditLoggerTests.cs`

**Why Needed**: Testing Requirements line 1119-1225+ defines comprehensive test suite. Need to verify current tests match spec.

**Required Tests** (from spec line 1120-1130):
1. Should_Log_SessionStart() ✓
2. Should_Log_SessionEnd() ✓
3. Should_Log_ConfigLoad()
4. Should_Log_FileOperations()
5. Should_Log_CommandExecution()
6. Should_Log_SecurityViolations()
7. Should_Log_TaskEvents()
8. Should_Log_ApprovalEvents()
9. Should_Maintain_CorrelationId()
10. Should_Not_Block_MainThread()
11. Should_Handle_HighVolume()

**Implementation Pattern**: Complete test code in spec line 1133-1225+

**Success Criteria**:
- All 11 tests implemented as specified
- Tests use FluentAssertions
- Tests verify non-blocking behavior
- Performance tests included

**Evidence**: [To be filled when complete]

---

### Gap #19: Integration Tests for Audit Storage
**Status**: [ ]
**File to Create**: `tests/Acode.Integration.Tests/Audit/AuditStorageIntegrationTests.cs`

**Why Needed**: Testing Requirements section requires integration tests for storage operations.

**Required Tests**:
1. Should write events to JSONL file
2. Should update checksums correctly
3. Should rotate logs when threshold met
4. Should verify log integrity
5. Should export in multiple formats
6. Should handle concurrent writes
7. Should survive crash and resume
8. Should cleanup old logs per retention policy

**Testing Pattern**: Real file I/O with temp directories

**Success Criteria**:
- All integration tests pass
- Real JSONL files created and verified
- Concurrent scenarios tested
- Cleanup properly tested

**Evidence**: [To be filled when complete]

---

### Gap #20: Performance Benchmarks
**Status**: [ ]
**File to Create**: `tests/Acode.Performance.Tests/Audit/AuditBenchmarks.cs`

**Why Needed**: Testing Requirements and NFR requirements specify performance targets.

**Required Benchmarks**:
1. LogAsync throughput (events/sec)
2. Serialization performance
3. File write performance
4. Checksum computation overhead
5. Memory allocation per event
6. Concurrent write scalability

**Testing Pattern**: BenchmarkDotNet benchmarks

**Success Criteria**:
- Benchmarks run successfully
- Results meet NFR targets (e.g., <10ms per event)
- No memory leaks

**Evidence**: [To be filled when complete]

---

### Gap #21: Update Config Schema for Audit
**Status**: [ ]
**File to Modify**: `data/config-schema.json`

**Why Needed**: Integration point with Task 002 (Config Contract). Audit section must be defined in schema.

**Required Schema Addition**:
```json
{
  "audit": {
    "type": "object",
    "properties": {
      "enabled": { "type": "boolean", "default": true },
      "log_level": { "enum": ["debug", "info", "warning", "error", "critical"], "default": "info" },
      "log_directory": { "type": "string", "default": ".acode/logs" },
      "retention_days": { "type": "integer", "minimum": 1, "default": 90 },
      "rotation_size_mb": { "type": "integer", "minimum": 1, "default": 10 },
      "rotation_interval": { "enum": ["hourly", "daily", "weekly"], "default": "daily" },
      "export_formats": { "type": "array", "items": { "enum": ["json", "csv", "text"] } }
    }
  }
}
```

**Success Criteria**:
- Schema validates audit configuration
- Default values provided
- Validation catches invalid configs

**Evidence**: [To be filled when complete]

---

### Gap #22: Documentation - Audit Event Schema
**Status**: [ ]
**File to Create**: `docs/audit-event-schema.md`

**Why Needed**: Implementation Prompt line 5449-5465 defines the complete event schema. This should be documented for users.

**Required Content**:
- All 13 schema fields with types and descriptions
- Example events for each event type
- JSON serialization format
- Field validation rules

**Success Criteria**:
- Documentation is complete and accurate
- Examples are valid JSON
- All fields documented

**Evidence**: [To be filled when complete]

---

### Gap #23: Documentation - CLI Audit Commands
**Status**: [ ]
**File to Create**: `docs/cli-audit-commands.md`

**Why Needed**: Users need documentation for all 8 audit CLI commands.

**Required Content**:
- Usage for each command
- Examples for common scenarios
- Exit codes and error messages
- Integration with other commands

**Success Criteria**:
- All 8 commands documented
- Examples are runnable
- Exit codes listed

**Evidence**: [To be filled when complete]

---

### Gap #24: Error Codes Definition
**Status**: [ ]
**File to Create or Modify**: `src/Acode.Application/Audit/AuditErrorCodes.cs`

**Why Needed**: Implementation Prompt line 5423-5436 defines 10 error codes.

**Required Error Codes**:
- ACODE-AUD-001: Audit initialization failed
- ACODE-AUD-002: Audit write failed
- ACODE-AUD-003: Audit directory not writable
- ACODE-AUD-004: Disk full - audit halted
- ACODE-AUD-005: Log rotation failed
- ACODE-AUD-006: Integrity verification failed
- ACODE-AUD-007: Checksum mismatch detected
- ACODE-AUD-008: Session not found
- ACODE-AUD-009: Export failed
- ACODE-AUD-010: Invalid query parameters

**Success Criteria**:
- All error codes defined
- Error messages are clear
- Codes used in appropriate places

**Evidence**: [To be filled when complete]

---

### Gap #25: Integration with File Operations
**Status**: [ ]
**Locations to Integrate**: Throughout codebase where file I/O occurs

**Why Needed**: Implementation Prompt line 5488 requires audit integration with file operations.

**Required Integration Points**:
- Log AuditEventType.FileRead when files are read
- Log AuditEventType.FileWrite when files are written
- Log AuditEventType.FileDelete when files are deleted
- Log AuditEventType.DirCreate when directories are created
- Log AuditEventType.DirDelete when directories are deleted
- Log AuditEventType.ProtectedPathBlocked when protected paths are accessed

**Success Criteria**:
- All file operations generate audit events
- Events include relevant metadata (path, size, result)
- Protected path violations logged even on deny

**Evidence**: [To be filled when complete]

---

### Gap #26: Integration with Command Execution
**Status**: [ ]
**Locations to Integrate**: Command execution infrastructure

**Why Needed**: Implementation Prompt line 5489 requires audit integration with command execution.

**Required Integration Points**:
- Log AuditEventType.CommandStart when commands begin
- Log AuditEventType.CommandEnd when commands complete
- Log AuditEventType.CommandError when commands fail
- Include command name, arguments, exit code, duration

**Success Criteria**:
- All command executions generate audit events
- Start/End events properly correlated
- Error events include error details

**Evidence**: [To be filled when complete]

---

### Gap #27: Integration with Security Violations
**Status**: [ ]
**Locations to Integrate**: Security policy enforcement points

**Why Needed**: Implementation Prompt line 5490 requires audit integration with security violations.

**Required Integration Points**:
- Log AuditEventType.SecurityViolation when policy blocks operation
- Log AuditEventType.ProtectedPathBlocked when denylist triggers
- Include risk ID, policy rule, blocked operation details

**Success Criteria**:
- All security violations logged
- Audit cannot be bypassed or disabled
- Events include full context for investigation

**Evidence**: [To be filled when complete]

---

### Gap #28: Final Verification
**Status**: [ ]
**Action**: Run complete test suite and audit

**Why Needed**: Ensure all gaps are truly complete and integrated.

**Verification Steps**:
1. Run all unit tests: `dotnet test --filter "FullyQualifiedName~Audit"`
2. Run all integration tests: `dotnet test tests/Acode.Integration.Tests`
3. Run performance benchmarks: `dotnet run --project tests/Acode.Performance.Tests`
4. Manual CLI testing of all 8 audit commands
5. Verify JSONL file creation and checksums
6. Verify log rotation and cleanup
7. Verify integrity verification detects tampering
8. Run full audit per docs/AUDIT-GUIDELINES.md

**Success Criteria**:
- All tests pass (100% pass rate)
- Benchmarks meet NFR targets
- CLI commands work as documented
- Audit checklist passes

**Evidence**: [To be filled when complete]

---

## Summary

**Total Gaps**: 28
**Completed**: 6 (Gaps 1-6) ✅
**In Progress**: 1 (Gap #7 - FileAuditWriter stashed)
**Remaining**: 21

**Completion**: 21.4% (6/28 gaps)
**Tests Passing**: 55+ (all domain layer)
**Commits**: 12+ on feature/task-003c-audit-baseline

## Next Steps

1. Start with Gap #1 (Value Objects Format Compliance)
2. Follow TDD: Write failing tests first
3. Implement to make tests pass
4. Commit after each gap
5. Update this checklist with evidence
6. Move to next gap

## Notes

- Value objects currently use Guid format but spec requires prefixed format (evt_xxx, sess_xxx, etc.)
- Many infrastructure components are completely missing
- CLI audit commands directory doesn't exist
- Integration points with file operations and security need to be added
- Performance benchmarks required but don't exist yet
