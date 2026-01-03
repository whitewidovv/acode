# Task 018.c: Artifact Logging + Truncation

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 018 (Structured Command Runner), Task 018.a (Output Capture), Task 050 (Workspace Database)  

---

## Description

Task 018.c implements artifact logging and truncation. Command output and related files are artifacts. They must be stored, managed, and retrievable.

Artifacts are execution byproducts. Stdout, stderr, log files, build outputs are artifacts. They enable debugging and analysis after execution completes.

Storage is essential. Artifacts must survive the execution. They must be queryable. They must be exportable.

Truncation manages size. Commands can produce massive output. Storing everything is impractical. Intelligent truncation preserves important content.

Truncation strategies vary. Head truncation keeps the beginning. Tail truncation keeps the end. Smart truncation keeps errors and warnings.

Retention policies control lifecycle. Artifacts cannot be stored forever. Old artifacts are cleaned up. Retention is configurable.

Sensitive content requires redaction. Artifacts may contain secrets. Redaction patterns hide sensitive data. Redaction happens before storage.

Artifact metadata enables querying. Size, type, timestamp, correlation IDs are metadata. Metadata is stored in the workspace database.

Artifact content is stored efficiently. Small artifacts inline in database. Large artifacts reference files. Compression reduces storage.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Artifact | Execution output or file |
| Truncation | Reducing artifact size |
| Head | Beginning of content |
| Tail | End of content |
| Smart | Intelligent selection |
| Retention | How long to keep |
| Redaction | Hiding sensitive data |
| Inline | Stored in database |
| Reference | Pointer to file |
| Compression | Size reduction |
| Metadata | Artifact attributes |
| Correlation ID | Tracing identifier |

---

## Out of Scope

The following items are explicitly excluded from Task 018.c:

- **Output capture mechanics** - See Task 018.a
- **Environment setup** - See Task 018.b
- **Directory standards** - See Task 021.a
- **Export format** - See Task 021.c
- **Binary artifact analysis** - Text only
- **Artifact diffing** - See Task 021.b
- **Real-time log viewing** - Batch only

---

## Functional Requirements

### Artifact Model

- FR-001: Define IArtifact interface
- FR-002: Define Artifact record
- FR-003: Store artifact type
- FR-004: Store artifact name
- FR-005: Store content or reference
- FR-006: Store size bytes
- FR-007: Store creation time
- FR-008: Store correlation IDs
- FR-009: Store truncation info

### Artifact Types

- FR-010: Support stdout type
- FR-011: Support stderr type
- FR-012: Support log file type
- FR-013: Support build output type
- FR-014: Support test result type
- FR-015: Support generic file type

### Storage

- FR-016: Define IArtifactStore interface
- FR-017: Store artifact method
- FR-018: Retrieve artifact method
- FR-019: List artifacts method
- FR-020: Delete artifact method
- FR-021: Inline storage for small
- FR-022: File reference for large
- FR-023: Configurable threshold
- FR-024: Compression option

### Truncation

- FR-025: Define ITruncator interface
- FR-026: Implement head truncation
- FR-027: Implement tail truncation
- FR-028: Implement smart truncation
- FR-029: Configurable max size
- FR-030: Preserve error lines
- FR-031: Preserve warning lines
- FR-032: Mark truncated artifacts
- FR-033: Store original size

### Smart Truncation

- FR-034: Detect error patterns
- FR-035: Detect warning patterns
- FR-036: Prioritize errors
- FR-037: Include context lines
- FR-038: Add truncation markers

### Redaction

- FR-039: Define sensitive patterns
- FR-040: Apply before storage
- FR-041: Replace with placeholder
- FR-042: Track redaction count
- FR-043: Configurable patterns

### Retention

- FR-044: Define retention policy
- FR-045: Age-based retention
- FR-046: Size-based retention
- FR-047: Count-based retention
- FR-048: Cleanup job
- FR-049: Manual cleanup CLI

### Logging

- FR-050: Log artifact creation
- FR-051: Log truncation events
- FR-052: Log redaction counts
- FR-053: Include correlation IDs
- FR-054: Persist to workspace DB

---

## Non-Functional Requirements

### Performance

- NFR-001: Store < 10ms for 1MB
- NFR-002: Truncate < 5ms
- NFR-003: Compress < 50ms/MB

### Storage

- NFR-004: Efficient compression
- NFR-005: Minimal overhead
- NFR-006: Handle large artifacts

### Reliability

- NFR-007: No data loss
- NFR-008: Consistent state
- NFR-009: Cleanup safe

### Security

- NFR-010: Redaction applied
- NFR-011: No secret leaks
- NFR-012: Access controlled

---

## User Manual Documentation

### Overview

Artifact logging stores command outputs and related files. Truncation manages size. Retention controls lifecycle.

### Configuration

```yaml
# .agent/config.yml
execution:
  artifacts:
    # Storage location
    directory: ".acode/artifacts"
    
    # Inline threshold (KB)
    inline_threshold_kb: 64
    
    # Enable compression
    compress: true
    
    # Truncation settings
    truncation:
      # Maximum artifact size (KB)
      max_size_kb: 1024
      
      # Strategy: head, tail, smart
      strategy: smart
      
      # Lines to keep with smart
      error_context_lines: 10
      
    # Retention settings
    retention:
      # Days to keep
      max_age_days: 30
      
      # Maximum total size (MB)
      max_total_size_mb: 500
      
      # Maximum artifact count
      max_count: 10000
      
    # Redaction patterns
    redaction:
      patterns:
        - "password\\s*[:=]\\s*\\S+"
        - "api[_-]?key\\s*[:=]\\s*\\S+"
        - "secret\\s*[:=]\\s*\\S+"
```

### Truncation Strategies

| Strategy | Description |
|----------|-------------|
| head | Keep first N bytes |
| tail | Keep last N bytes |
| smart | Keep errors/warnings + context |

### Smart Truncation Example

```
Original output (10MB):
Line 1: Starting build...
Line 2: Compiling file1.cs
... (millions of lines)
Line 5000000: error CS1002: ; expected
Line 5000001: at Program.cs:45
... (more lines)
Line 10000000: Build failed

Smart truncated (1KB):
[TRUNCATED: 9.5MB removed from beginning]
Line 4999990: Processing Program.cs
...
Line 5000000: error CS1002: ; expected
Line 5000001: at Program.cs:45
Line 5000002: (context continues)
...
[TRUNCATED: 4.5MB removed from end]
Line 10000000: Build failed
```

### CLI Commands

```bash
# List artifacts
acode artifacts list

# List for specific run
acode artifacts list --run-id run-123

# Show artifact content
acode artifacts show <artifact-id>

# Delete artifact
acode artifacts delete <artifact-id>

# Cleanup old artifacts
acode artifacts cleanup

# Cleanup by age
acode artifacts cleanup --older-than 7d

# Show artifact stats
acode artifacts stats
```

### Artifact Record

```json
{
  "id": "art-001",
  "type": "stdout",
  "name": "dotnet-build-stdout",
  "sizeBytes": 45000,
  "truncated": true,
  "originalSizeBytes": 10000000,
  "redactionCount": 2,
  "compressed": true,
  "inline": false,
  "filePath": ".acode/artifacts/art-001.gz",
  "createdAt": "2024-01-15T10:30:00Z",
  "correlationIds": {
    "runId": "run-123",
    "sessionId": "sess-456",
    "taskId": "task-789"
  }
}
```

### Troubleshooting

#### Artifact Too Large

**Problem:** Artifact exceeds limits

**Solutions:**
1. Increase max_size_kb
2. Use smart truncation
3. Redirect to file instead

#### Missing Content

**Problem:** Important content truncated

**Solutions:**
1. Increase context lines
2. Add error patterns
3. Use tail truncation for logs

#### Disk Full

**Problem:** Artifacts filling disk

**Solutions:**
1. Run cleanup
2. Reduce retention period
3. Reduce max_total_size_mb

---

## Acceptance Criteria

### Model

- [ ] AC-001: Artifact model complete
- [ ] AC-002: Types supported
- [ ] AC-003: Metadata stored

### Storage

- [ ] AC-004: Inline works
- [ ] AC-005: File reference works
- [ ] AC-006: Compression works

### Truncation

- [ ] AC-007: Head works
- [ ] AC-008: Tail works
- [ ] AC-009: Smart works
- [ ] AC-010: Marked as truncated

### Redaction

- [ ] AC-011: Patterns detected
- [ ] AC-012: Values replaced
- [ ] AC-013: Count tracked

### Retention

- [ ] AC-014: Age cleanup works
- [ ] AC-015: Size cleanup works
- [ ] AC-016: Count cleanup works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Execution/Artifacts/
├── ArtifactTests.cs
│   ├── Should_Store_Metadata()
│   └── Should_Support_Types()
│
├── ArtifactStoreTests.cs
│   ├── Should_Store_Inline()
│   ├── Should_Store_File()
│   └── Should_Compress()
│
├── TruncatorTests.cs
│   ├── Should_Truncate_Head()
│   ├── Should_Truncate_Tail()
│   └── Should_Truncate_Smart()
│
├── RedactorTests.cs
│   ├── Should_Detect_Patterns()
│   └── Should_Replace_Values()
│
└── RetentionTests.cs
    ├── Should_Cleanup_By_Age()
    └── Should_Cleanup_By_Size()
```

### Integration Tests

```
Tests/Integration/Execution/Artifacts/
├── ArtifactStoreIntegrationTests.cs
│   └── Should_Persist_And_Retrieve()
```

### E2E Tests

```
Tests/E2E/Execution/Artifacts/
├── ArtifactE2ETests.cs
│   └── Should_Store_Via_Execution()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Store 1MB | 5ms | 10ms |
| Truncate 10MB | 20ms | 50ms |
| Compress 1MB | 30ms | 50ms |

---

## User Verification Steps

### Scenario 1: Store Artifact

1. Execute command
2. Verify: Stdout stored

### Scenario 2: Truncation

1. Execute with large output
2. Verify: Output truncated

### Scenario 3: Smart Truncation

1. Execute with errors
2. Verify: Errors preserved

### Scenario 4: Cleanup

1. Run `acode artifacts cleanup`
2. Verify: Old artifacts removed

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Execution/
│   └── Artifacts/
│       ├── IArtifact.cs
│       ├── Artifact.cs
│       └── ArtifactType.cs
│
src/AgenticCoder.Infrastructure/
├── Execution/
│   └── Artifacts/
│       ├── ArtifactStore.cs
│       ├── HeadTruncator.cs
│       ├── TailTruncator.cs
│       ├── SmartTruncator.cs
│       ├── ContentRedactor.cs
│       └── RetentionManager.cs
```

### IArtifact Interface

```csharp
namespace AgenticCoder.Domain.Execution.Artifacts;

public interface IArtifact
{
    Guid Id { get; }
    ArtifactType Type { get; }
    string Name { get; }
    long SizeBytes { get; }
    bool Truncated { get; }
    long? OriginalSizeBytes { get; }
    int RedactionCount { get; }
    DateTimeOffset CreatedAt { get; }
}
```

### SmartTruncator Class

```csharp
public class SmartTruncator : ITruncator
{
    private readonly SmartTruncatorOptions _options;
    
    public TruncationResult Truncate(string content, int maxBytes)
    {
        var lines = content.Split('\n');
        var importantLines = FindImportantLines(lines);
        
        var result = new StringBuilder();
        var bytesRemaining = maxBytes;
        
        // Always include errors with context
        foreach (var important in importantLines)
        {
            var section = GetWithContext(lines, important, _options.ContextLines);
            var sectionBytes = Encoding.UTF8.GetByteCount(section);
            
            if (sectionBytes <= bytesRemaining)
            {
                result.Append(section);
                bytesRemaining -= sectionBytes;
            }
        }
        
        return new TruncationResult
        {
            Content = result.ToString(),
            Truncated = true,
            OriginalSize = Encoding.UTF8.GetByteCount(content)
        };
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-ART-001 | Store failed |
| ACODE-ART-002 | Retrieve failed |
| ACODE-ART-003 | Artifact not found |
| ACODE-ART-004 | Cleanup failed |

### Implementation Checklist

1. [ ] Create artifact model
2. [ ] Create artifact store
3. [ ] Implement truncators
4. [ ] Implement redaction
5. [ ] Implement retention
6. [ ] Add CLI commands
7. [ ] Add compression
8. [ ] Add unit tests

### Rollout Plan

1. **Phase 1:** Artifact model
2. **Phase 2:** Storage
3. **Phase 3:** Truncation
4. **Phase 4:** Redaction
5. **Phase 5:** Retention

---

**End of Task 018.c Specification**