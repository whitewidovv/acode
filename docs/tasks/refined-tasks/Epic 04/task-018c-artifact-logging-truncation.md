# Task 018.c: Artifact Logging + Truncation

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 018 (Structured Command Runner), Task 018.a (Output Capture), Task 050 (Workspace Database)  

---

## Description

### Overview

Task 018.c implements comprehensive artifact logging and intelligent truncation for the Agentic Coding Bot's execution layer. Artifacts are the byproducts of command execution—stdout, stderr, log files, build outputs, test results—and represent the primary debugging and analysis resource for understanding what happened during execution. This subtask creates the infrastructure to store, manage, retrieve, and intelligently truncate these artifacts while protecting sensitive content through redaction.

### Business Value

1. **Debugging Enablement**: Stored artifacts allow developers to analyze command failures hours or days after execution, without needing to reproduce the issue
2. **Audit Trail**: Artifacts provide a complete record of what the bot produced, essential for compliance and troubleshooting
3. **Storage Efficiency**: Intelligent truncation preserves important content (errors, warnings) while managing disk space, preventing storage exhaustion
4. **Security Compliance**: Automatic redaction of sensitive patterns (passwords, API keys) ensures secrets never persist to disk or logs
5. **Operational Intelligence**: Artifact metadata enables querying patterns—which commands produce the most output, what errors are common, storage trends
6. **Export Capability**: Stored artifacts can be bundled for sharing with team members or support, enabling collaborative debugging

### Scope

This task delivers:

1. **Artifact Domain Model**: Type-safe representation of artifacts with metadata (size, type, timestamps, correlation IDs, truncation info)
2. **Artifact Store**: Persistence layer with inline storage for small artifacts and file references for large ones, with compression support
3. **Truncation Strategies**: Head (keep beginning), tail (keep end), and smart (preserve errors/warnings with context) truncation
4. **Sensitive Redaction**: Pattern-based detection and replacement of secrets before storage
5. **Retention Management**: Age-based, size-based, and count-based cleanup policies with background job
6. **CLI Commands**: Artifact listing, viewing, deletion, cleanup, and statistics commands
7. **Database Integration**: Metadata stored in workspace database for efficient querying

### Integration Points

| Component | Integration Type | Purpose |
|-----------|------------------|---------|
| Task-018a (Output Capture) | Upstream | Provides raw stdout/stderr content to be stored as artifacts |
| Task-050 (Workspace Database) | Storage | Artifact metadata persisted for querying |
| Task-021a (Directory Standards) | Convention | Artifacts stored in standard .acode/artifacts directory |
| Task-021c (Export Format) | Downstream | Artifacts included in export bundles |
| Task-018b (Environment) | Related | Environment context stored as artifact metadata |
| Task-005 (CLI Architecture) | Integration | CLI commands for artifact management |

### Failure Modes

| Failure | Detection | Recovery |
|---------|-----------|----------|
| Disk full during storage | IOException | Store truncated version, log warning, continue execution |
| Database write failure | SqlException | Retry with exponential backoff, fail gracefully with warning |
| Compression failure | InvalidOperationException | Store uncompressed, log warning |
| Redaction pattern invalid | RegexParseException | Skip pattern, log error, continue with remaining patterns |
| Artifact file missing | FileNotFoundException | Mark as unavailable in metadata, suggest cleanup |
| Cleanup deletes active artifact | Optimistic lock check | Skip artifact, retry on next cleanup cycle |

### Assumptions

1. Workspace database (Task-050) is available for metadata storage
2. File system has sufficient permissions for artifact directory
3. Artifacts are primarily text-based (binary artifacts are out of scope)
4. UTF-8 encoding is standard for artifact content
5. Compression uses GZip for broad compatibility
6. Correlation IDs from Task-018a are available for linking

### Security Considerations

1. **Redaction First**: Sensitive content is redacted BEFORE storage, never after
2. **Pattern Updates**: Redaction patterns loaded at runtime to allow security updates
3. **No Decryption**: Redacted content cannot be recovered—placeholder replaces actual value
4. **Access Control**: Artifacts respect repository permissions (enforced by file system)
5. **Cleanup Verification**: Deleted artifacts are verified removed from both database and file system

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

### Artifact Model (FR-018C-01 to FR-018C-20)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-018C-01 | Define `IArtifact` interface with core properties | Must Have |
| FR-018C-02 | Define `Artifact` sealed record implementing interface | Must Have |
| FR-018C-03 | Store artifact unique identifier (GUID) | Must Have |
| FR-018C-04 | Store artifact type enum (Stdout, Stderr, LogFile, BuildOutput, TestResult, GenericFile) | Must Have |
| FR-018C-05 | Store artifact name for human identification | Must Have |
| FR-018C-06 | Store content or file reference path | Must Have |
| FR-018C-07 | Store size in bytes (after truncation/compression) | Must Have |
| FR-018C-08 | Store original size in bytes (before truncation) | Must Have |
| FR-018C-09 | Store creation timestamp with UTC offset | Must Have |
| FR-018C-10 | Store correlation IDs dictionary (runId, sessionId, taskId) | Must Have |
| FR-018C-11 | Store truncation flag and strategy used | Must Have |
| FR-018C-12 | Store compression flag | Must Have |
| FR-018C-13 | Store inline flag (content in DB vs file reference) | Must Have |
| FR-018C-14 | Store redaction count | Must Have |
| FR-018C-15 | Store MIME type for content | Should Have |
| FR-018C-16 | Store checksum for integrity verification | Should Have |
| FR-018C-17 | Support artifact tagging for categorization | Should Have |
| FR-018C-18 | Store encoding used (UTF-8, UTF-16, etc.) | Should Have |
| FR-018C-19 | Store line count for quick display | Should Have |
| FR-018C-20 | Artifact implements IEquatable for comparison | Should Have |

### Artifact Types (FR-018C-21 to FR-018C-32)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-018C-21 | Define `ArtifactType` enum | Must Have |
| FR-018C-22 | Support `Stdout` type for command stdout | Must Have |
| FR-018C-23 | Support `Stderr` type for command stderr | Must Have |
| FR-018C-24 | Support `CombinedOutput` type for interleaved stdout/stderr | Must Have |
| FR-018C-25 | Support `LogFile` type for captured log files | Must Have |
| FR-018C-26 | Support `BuildOutput` type for build results | Should Have |
| FR-018C-27 | Support `TestResult` type for test output | Should Have |
| FR-018C-28 | Support `GenericFile` type for miscellaneous artifacts | Must Have |
| FR-018C-29 | Support `ErrorDump` type for exception details | Should Have |
| FR-018C-30 | Type determines default truncation strategy | Should Have |
| FR-018C-31 | Type determines icon in CLI display | Could Have |
| FR-018C-32 | Type is filterable in queries | Must Have |

### Artifact Store (FR-018C-33 to FR-018C-55)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-018C-33 | Define `IArtifactStore` interface | Must Have |
| FR-018C-34 | Implement `StoreAsync` method accepting content and metadata | Must Have |
| FR-018C-35 | Implement `GetAsync` method by artifact ID | Must Have |
| FR-018C-36 | Implement `GetContentAsync` method returning artifact content | Must Have |
| FR-018C-37 | Implement `ListAsync` method with filtering options | Must Have |
| FR-018C-38 | Implement `DeleteAsync` method by artifact ID | Must Have |
| FR-018C-39 | Implement `DeleteManyAsync` method for batch deletion | Should Have |
| FR-018C-40 | Store artifacts smaller than threshold inline in database | Must Have |
| FR-018C-41 | Store artifacts larger than threshold as file references | Must Have |
| FR-018C-42 | Configurable inline threshold (default 64KB) | Must Have |
| FR-018C-43 | Compress artifacts using GZip before storage | Must Have |
| FR-018C-44 | Compression configurable (enabled by default) | Should Have |
| FR-018C-45 | Skip compression if compressed size > original size | Should Have |
| FR-018C-46 | Store files in organized directory structure (by date) | Should Have |
| FR-018C-47 | Use content-addressable naming for deduplication | Could Have |
| FR-018C-48 | Transaction support for metadata + file consistency | Must Have |
| FR-018C-49 | Implement `GetStatisticsAsync` for storage metrics | Should Have |
| FR-018C-50 | Query by correlation ID | Must Have |
| FR-018C-51 | Query by date range | Should Have |
| FR-018C-52 | Query by artifact type | Should Have |
| FR-018C-53 | Query by size range | Should Have |
| FR-018C-54 | Pagination support for listing | Should Have |
| FR-018C-55 | Sort by creation date, size, or name | Should Have |

### Truncation Strategy (FR-018C-56 to FR-018C-75)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-018C-56 | Define `ITruncator` interface | Must Have |
| FR-018C-57 | Define `TruncationResult` record with content, original size, truncated flag | Must Have |
| FR-018C-58 | Implement `HeadTruncator` keeping first N bytes | Must Have |
| FR-018C-59 | Implement `TailTruncator` keeping last N bytes | Must Have |
| FR-018C-60 | Implement `SmartTruncator` preserving errors and warnings | Must Have |
| FR-018C-61 | Configurable maximum artifact size (default 1MB) | Must Have |
| FR-018C-62 | Truncation preserves UTF-8 character boundaries | Must Have |
| FR-018C-63 | Truncation adds marker showing bytes removed | Must Have |
| FR-018C-64 | SmartTruncator detects error patterns (ERROR, FAIL, Exception) | Must Have |
| FR-018C-65 | SmartTruncator detects warning patterns (WARN, Warning:) | Must Have |
| FR-018C-66 | SmartTruncator includes N context lines before/after errors | Must Have |
| FR-018C-67 | Configurable context lines (default 10) | Should Have |
| FR-018C-68 | SmartTruncator prioritizes errors over warnings | Should Have |
| FR-018C-69 | SmartTruncator always includes first 5 and last 5 lines | Should Have |
| FR-018C-70 | Custom error/warning patterns configurable | Should Have |
| FR-018C-71 | Truncation strategy selectable per artifact type | Should Have |
| FR-018C-72 | Implement `NullTruncator` for no truncation | Should Have |
| FR-018C-73 | Return full content if under size limit | Must Have |
| FR-018C-74 | Store truncation metadata (strategy, bytes removed, sections count) | Must Have |
| FR-018C-75 | Truncation is idempotent | Should Have |

### Sensitive Redaction (FR-018C-76 to FR-018C-90)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-018C-76 | Define `IContentRedactor` interface | Must Have |
| FR-018C-77 | Apply redaction BEFORE storage | Must Have |
| FR-018C-78 | Define default patterns for password detection | Must Have |
| FR-018C-79 | Define default patterns for API key detection | Must Have |
| FR-018C-80 | Define default patterns for token detection | Must Have |
| FR-018C-81 | Define default patterns for secret detection | Must Have |
| FR-018C-82 | Replace matches with `[REDACTED]` placeholder | Must Have |
| FR-018C-83 | Track redaction count per artifact | Must Have |
| FR-018C-84 | Patterns configurable via config.yml | Must Have |
| FR-018C-85 | Support regex patterns | Must Have |
| FR-018C-86 | Support glob patterns for variable names | Should Have |
| FR-018C-87 | Case-insensitive matching by default | Should Have |
| FR-018C-88 | Redaction includes surrounding context for identification | Could Have |
| FR-018C-89 | Log redaction events (count only, not values) | Should Have |
| FR-018C-90 | Redaction is irreversible—original value not stored | Must Have |

### Retention Management (FR-018C-91 to FR-018C-110)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-018C-91 | Define `IRetentionPolicy` interface | Must Have |
| FR-018C-92 | Define `RetentionConfiguration` record | Must Have |
| FR-018C-93 | Implement age-based retention (delete older than N days) | Must Have |
| FR-018C-94 | Implement size-based retention (delete when exceeds N MB total) | Must Have |
| FR-018C-95 | Implement count-based retention (keep only last N artifacts) | Must Have |
| FR-018C-96 | Configurable retention thresholds | Must Have |
| FR-018C-97 | Retention evaluated in order: age, size, count | Should Have |
| FR-018C-98 | Oldest artifacts deleted first when reducing | Must Have |
| FR-018C-99 | Skip artifacts with active references | Should Have |
| FR-018C-100 | Implement `RetentionManager` background service | Must Have |
| FR-018C-101 | Retention job runs on configurable schedule | Should Have |
| FR-018C-102 | Retention job logs actions taken | Must Have |
| FR-018C-103 | Manual cleanup via CLI command | Must Have |
| FR-018C-104 | Dry-run mode for cleanup preview | Should Have |
| FR-018C-105 | Cleanup returns count of deleted artifacts | Must Have |
| FR-018C-106 | Cleanup returns bytes reclaimed | Should Have |
| FR-018C-107 | Cleanup deletes both metadata and files | Must Have |
| FR-018C-108 | Orphan file cleanup (files without metadata) | Should Have |
| FR-018C-109 | Orphan metadata cleanup (metadata without files) | Should Have |
| FR-018C-110 | Retention configurable per artifact type | Could Have |

### Logging and Audit (FR-018C-111 to FR-018C-120)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-018C-111 | Log artifact creation with ID and type | Must Have |
| FR-018C-112 | Log truncation events with strategy and size reduction | Must Have |
| FR-018C-113 | Log redaction events with pattern match count | Must Have |
| FR-018C-114 | Include correlation IDs in all log entries | Must Have |
| FR-018C-115 | Persist artifact metadata to workspace database | Must Have |
| FR-018C-116 | Log compression ratio achieved | Should Have |
| FR-018C-117 | Log storage location (inline vs file) | Should Have |
| FR-018C-118 | Log cleanup actions with artifact IDs deleted | Must Have |
| FR-018C-119 | Log errors during artifact operations | Must Have |
| FR-018C-120 | Structured logging with artifact context | Should Have |

---

## Non-Functional Requirements

### Performance (NFR-018C-01 to NFR-018C-12)

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-018C-01 | Store 1MB artifact (inline) | 5ms | 10ms |
| NFR-018C-02 | Store 1MB artifact (file) | 10ms | 25ms |
| NFR-018C-03 | Store 10MB artifact (file + compression) | 100ms | 250ms |
| NFR-018C-04 | Truncation of 10MB content | 20ms | 50ms |
| NFR-018C-05 | Smart truncation with pattern matching | 50ms | 100ms |
| NFR-018C-06 | Redaction scan per MB | 30ms | 75ms |
| NFR-018C-07 | GZip compression per MB | 30ms | 50ms |
| NFR-018C-08 | Retrieve artifact metadata | 2ms | 5ms |
| NFR-018C-09 | Retrieve artifact content (1MB) | 10ms | 25ms |
| NFR-018C-10 | List artifacts (100 items) | 5ms | 15ms |
| NFR-018C-11 | Delete single artifact | 3ms | 10ms |
| NFR-018C-12 | Cleanup job (1000 artifacts) | 5s | 15s |

### Storage Efficiency (NFR-018C-13 to NFR-018C-20)

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-018C-13 | GZip compression ratio (text) | 70-80% reduction |
| NFR-018C-14 | Metadata overhead per artifact | < 500 bytes |
| NFR-018C-15 | Inline storage efficiency | No file system overhead |
| NFR-018C-16 | File reference path length | < 200 characters |
| NFR-018C-17 | Directory structure depth | Maximum 3 levels |
| NFR-018C-18 | Maximum artifacts per directory | 1000 |
| NFR-018C-19 | Deduplication savings (if enabled) | 20-40% typical |
| NFR-018C-20 | Index size for querying | < 1% of total artifact size |

### Reliability (NFR-018C-21 to NFR-018C-28)

| ID | Requirement | Description |
|----|-------------|-------------|
| NFR-018C-21 | No data loss | Artifact content never partially stored |
| NFR-018C-22 | Transactional consistency | Metadata and file always in sync |
| NFR-018C-23 | Crash recovery | Incomplete writes cleaned up on startup |
| NFR-018C-24 | Cleanup atomicity | Delete is all-or-nothing per artifact |
| NFR-018C-25 | Concurrent access safety | Multiple threads can store simultaneously |
| NFR-018C-26 | File locking | Prevent concurrent writes to same artifact |
| NFR-018C-27 | Integrity verification | Checksum validates content on retrieval |
| NFR-018C-28 | Graceful degradation | Continue operation if non-critical features fail |

### Security (NFR-018C-29 to NFR-018C-36)

| ID | Requirement | Description |
|----|-------------|-------------|
| NFR-018C-29 | Redaction before storage | Sensitive content never written to disk/DB |
| NFR-018C-30 | No plain text secrets | All secrets replaced with placeholder |
| NFR-018C-31 | Pattern coverage | Default patterns catch common secret formats |
| NFR-018C-32 | Custom pattern support | Organization-specific patterns configurable |
| NFR-018C-33 | Secure deletion | Deleted artifact files overwritten (optional) |
| NFR-018C-34 | Access control | File permissions restrict to current user |
| NFR-018C-35 | Audit trail | All operations logged with actor |
| NFR-018C-36 | No sensitive logging | Redacted values never appear in logs |

### Maintainability (NFR-018C-37 to NFR-018C-42)

| ID | Requirement | Description |
|----|-------------|-------------|
| NFR-018C-37 | Interface-driven design | All components behind interfaces |
| NFR-018C-38 | Strategy pattern for truncation | Easy to add new truncation strategies |
| NFR-018C-39 | Configuration-driven behavior | No code changes for policy updates |
| NFR-018C-40 | Comprehensive logging | Operations traceable in logs |
| NFR-018C-41 | Testable components | All dependencies injectable |
| NFR-018C-42 | Clear error messages | Problems diagnosable from messages alone |

### Observability (NFR-018C-43 to NFR-018C-48)

| ID | Requirement | Description |
|----|-------------|-------------|
| NFR-018C-43 | Metrics: artifact count | Track total artifacts stored |
| NFR-018C-44 | Metrics: storage bytes | Track total and per-type storage |
| NFR-018C-45 | Metrics: truncation rate | Track percentage of artifacts truncated |
| NFR-018C-46 | Metrics: redaction rate | Track artifacts with redactions |
| NFR-018C-47 | Metrics: compression ratio | Track actual compression achieved |
| NFR-018C-48 | Statistics command | CLI provides storage statistics |

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

### Artifact Model (AC-018C-01 to AC-018C-12)

- [ ] AC-018C-01: `IArtifact` interface defined with all required properties
- [ ] AC-018C-02: `Artifact` sealed record implements interface correctly
- [ ] AC-018C-03: Artifact ID is GUID and globally unique
- [ ] AC-018C-04: `ArtifactType` enum includes all required types (Stdout, Stderr, CombinedOutput, LogFile, BuildOutput, TestResult, GenericFile, ErrorDump)
- [ ] AC-018C-05: Artifact stores size in bytes accurately
- [ ] AC-018C-06: Artifact stores original size before truncation
- [ ] AC-018C-07: Artifact stores creation timestamp with UTC offset
- [ ] AC-018C-08: Artifact stores correlation IDs dictionary
- [ ] AC-018C-09: Artifact stores truncation metadata (flag, strategy, bytes removed)
- [ ] AC-018C-10: Artifact stores compression flag
- [ ] AC-018C-11: Artifact stores redaction count
- [ ] AC-018C-12: Artifact equality based on ID

### Artifact Store (AC-018C-13 to AC-018C-30)

- [ ] AC-018C-13: `IArtifactStore` interface defined with Store, Get, GetContent, List, Delete methods
- [ ] AC-018C-14: `StoreAsync` returns stored artifact with populated ID
- [ ] AC-018C-15: Artifacts smaller than threshold stored inline in database
- [ ] AC-018C-16: Artifacts larger than threshold stored as file references
- [ ] AC-018C-17: Inline threshold is configurable (default 64KB)
- [ ] AC-018C-18: Compression applied before storage when enabled
- [ ] AC-018C-19: Compression skipped when result would be larger
- [ ] AC-018C-20: Files stored in organized directory structure
- [ ] AC-018C-21: `GetAsync` retrieves artifact metadata by ID
- [ ] AC-018C-22: `GetContentAsync` retrieves decompressed content
- [ ] AC-018C-23: `ListAsync` supports filtering by type
- [ ] AC-018C-24: `ListAsync` supports filtering by correlation ID
- [ ] AC-018C-25: `ListAsync` supports filtering by date range
- [ ] AC-018C-26: `ListAsync` supports pagination
- [ ] AC-018C-27: `DeleteAsync` removes both metadata and file
- [ ] AC-018C-28: `DeleteManyAsync` handles batch deletion efficiently
- [ ] AC-018C-29: Transaction ensures metadata and file consistency
- [ ] AC-018C-30: `GetStatisticsAsync` returns storage metrics

### Truncation (AC-018C-31 to AC-018C-48)

- [ ] AC-018C-31: `ITruncator` interface defined with Truncate method
- [ ] AC-018C-32: `TruncationResult` includes content, original size, truncated flag, strategy
- [ ] AC-018C-33: `HeadTruncator` keeps first N bytes of content
- [ ] AC-018C-34: `TailTruncator` keeps last N bytes of content
- [ ] AC-018C-35: `SmartTruncator` preserves lines matching error patterns
- [ ] AC-018C-36: `SmartTruncator` preserves lines matching warning patterns
- [ ] AC-018C-37: `SmartTruncator` includes configurable context lines around matches
- [ ] AC-018C-38: Maximum artifact size is configurable (default 1MB)
- [ ] AC-018C-39: Truncation respects UTF-8 character boundaries
- [ ] AC-018C-40: Truncation adds marker showing bytes/lines removed
- [ ] AC-018C-41: Content under size limit returned unchanged
- [ ] AC-018C-42: SmartTruncator always includes first 5 lines
- [ ] AC-018C-43: SmartTruncator always includes last 5 lines
- [ ] AC-018C-44: Error patterns are configurable
- [ ] AC-018C-45: Warning patterns are configurable
- [ ] AC-018C-46: Errors prioritized over warnings when space limited
- [ ] AC-018C-47: Truncation strategy selectable per artifact type
- [ ] AC-018C-48: Truncation metadata stored with artifact

### Redaction (AC-018C-49 to AC-018C-62)

- [ ] AC-018C-49: `IContentRedactor` interface defined
- [ ] AC-018C-50: Redaction applied BEFORE storage (never after)
- [ ] AC-018C-51: Default patterns detect `password=value` format
- [ ] AC-018C-52: Default patterns detect `api_key=value` format
- [ ] AC-018C-53: Default patterns detect `token=value` format
- [ ] AC-018C-54: Default patterns detect `secret=value` format
- [ ] AC-018C-55: Matches replaced with `[REDACTED]` placeholder
- [ ] AC-018C-56: Redaction count tracked per artifact
- [ ] AC-018C-57: Custom patterns configurable via config.yml
- [ ] AC-018C-58: Regex patterns supported
- [ ] AC-018C-59: Pattern matching is case-insensitive
- [ ] AC-018C-60: Redaction is irreversible—no recovery
- [ ] AC-018C-61: Redaction events logged (count only, not values)
- [ ] AC-018C-62: Invalid patterns logged and skipped

### Retention (AC-018C-63 to AC-018C-78)

- [ ] AC-018C-63: `IRetentionPolicy` interface defined
- [ ] AC-018C-64: Age-based retention deletes artifacts older than N days
- [ ] AC-018C-65: Size-based retention deletes when total exceeds N MB
- [ ] AC-018C-66: Count-based retention keeps only last N artifacts
- [ ] AC-018C-67: Retention thresholds are configurable
- [ ] AC-018C-68: Oldest artifacts deleted first
- [ ] AC-018C-69: `RetentionManager` runs on configurable schedule
- [ ] AC-018C-70: Retention job logs actions taken
- [ ] AC-018C-71: CLI `acode artifacts cleanup` triggers manual cleanup
- [ ] AC-018C-72: Cleanup supports `--older-than` option
- [ ] AC-018C-73: Cleanup supports `--dry-run` for preview
- [ ] AC-018C-74: Cleanup returns count of deleted artifacts
- [ ] AC-018C-75: Cleanup returns bytes reclaimed
- [ ] AC-018C-76: Cleanup deletes both metadata and files
- [ ] AC-018C-77: Orphan files (no metadata) cleaned up
- [ ] AC-018C-78: Orphan metadata (no files) cleaned up

### Logging and Audit (AC-018C-79 to AC-018C-88)

- [ ] AC-018C-79: Artifact creation logged with ID and type
- [ ] AC-018C-80: Truncation events logged with strategy and size reduction
- [ ] AC-018C-81: Redaction events logged with count
- [ ] AC-018C-82: Correlation IDs included in all log entries
- [ ] AC-018C-83: Artifact metadata persisted to workspace database
- [ ] AC-018C-84: Compression ratio logged
- [ ] AC-018C-85: Storage location (inline/file) logged
- [ ] AC-018C-86: Cleanup actions logged with artifact IDs
- [ ] AC-018C-87: Errors during operations logged with context
- [ ] AC-018C-88: Statistics available via CLI command

---

## Testing Requirements

### Unit Tests

#### ArtifactTests
- Artifact_Create_HasUniqueId
- Artifact_Create_SetsCreatedAt
- Artifact_Equality_BasedOnId
- Artifact_CorrelationIds_Immutable
- Artifact_ToString_IncludesIdAndType
- ArtifactType_AllValues_Defined

#### ArtifactStoreTests
- StoreAsync_SmallContent_InlinesInDatabase
- StoreAsync_LargeContent_CreatesFile
- StoreAsync_ThresholdBoundary_CorrectDecision
- StoreAsync_WithCompression_ReducesSize
- StoreAsync_CompressionDisabled_StoresRaw
- StoreAsync_CompressionLarger_StoresUncompressed
- GetAsync_ExistingId_ReturnsArtifact
- GetAsync_NonExistentId_ReturnsNull
- GetContentAsync_InlineArtifact_ReturnsContent
- GetContentAsync_FileArtifact_ReturnsContent
- GetContentAsync_CompressedArtifact_Decompresses
- ListAsync_NoFilter_ReturnsAll
- ListAsync_ByType_FiltersCorrectly
- ListAsync_ByCorrelationId_FiltersCorrectly
- ListAsync_ByDateRange_FiltersCorrectly
- ListAsync_Pagination_Works
- DeleteAsync_ExistingArtifact_RemovesMetadataAndFile
- DeleteAsync_InlineArtifact_RemovesMetadataOnly
- DeleteAsync_NonExistent_ReturnsFalse
- DeleteManyAsync_BatchDeletion_Efficient
- GetStatisticsAsync_ReturnsAccurateMetrics

#### HeadTruncatorTests
- Truncate_UnderLimit_ReturnsUnchanged
- Truncate_OverLimit_KeepsFirstNBytes
- Truncate_RespectUtf8Boundaries
- Truncate_AddsMarker_ShowingBytesRemoved
- Truncate_EmptyContent_ReturnsEmpty
- Truncate_ExactlyAtLimit_ReturnsUnchanged
- Truncate_Result_IncludesOriginalSize
- Truncate_Result_SetsTruncatedFlag

#### TailTruncatorTests
- Truncate_UnderLimit_ReturnsUnchanged
- Truncate_OverLimit_KeepsLastNBytes
- Truncate_RespectUtf8Boundaries
- Truncate_AddsMarker_ShowingBytesRemoved
- Truncate_EmptyContent_ReturnsEmpty
- Truncate_ExactlyAtLimit_ReturnsUnchanged
- Truncate_Result_IncludesOriginalSize
- Truncate_Result_SetsTruncatedFlag

#### SmartTruncatorTests
- Truncate_UnderLimit_ReturnsUnchanged
- Truncate_PreservesErrorLines
- Truncate_PreservesWarningLines
- Truncate_IncludesContextLines
- Truncate_PrioritizesErrorsOverWarnings
- Truncate_AlwaysIncludesFirst5Lines
- Truncate_AlwaysIncludesLast5Lines
- Truncate_AddsMarkersForGaps
- Truncate_CustomErrorPatterns_Work
- Truncate_CustomWarningPatterns_Work
- Truncate_NoMatches_FallsBackToTail
- Truncate_MultipleErrors_IncludesAll
- Truncate_SpaceLimited_IncludesHighestPriority
- Truncate_ContextOverlap_NoDuplicateLines

#### ContentRedactorTests
- Redact_PasswordPattern_Matches
- Redact_ApiKeyPattern_Matches
- Redact_TokenPattern_Matches
- Redact_SecretPattern_Matches
- Redact_ReplacesWithPlaceholder
- Redact_CountsRedactions
- Redact_CaseInsensitive
- Redact_MultipleMatches_AllRedacted
- Redact_CustomPatterns_Work
- Redact_RegexPatterns_Work
- Redact_NoMatches_ReturnsUnchanged
- Redact_InvalidPattern_SkipsAndLogs
- Redact_NestedPatterns_AllCaught
- Redact_EmptyContent_ReturnsEmpty

#### RetentionPolicyTests
- AgePolicy_OlderThanThreshold_MarksForDeletion
- AgePolicy_YoungerThanThreshold_Keeps
- SizePolicy_ExceedsThreshold_MarksOldestForDeletion
- SizePolicy_UnderThreshold_KeepsAll
- CountPolicy_ExceedsThreshold_MarksOldestForDeletion
- CountPolicy_UnderThreshold_KeepsAll
- CombinedPolicy_EvaluatesInOrder
- CombinedPolicy_StopsWhenUnderAllThresholds

#### RetentionManagerTests
- Cleanup_DeletesMarkedArtifacts
- Cleanup_LogsActions
- Cleanup_ReturnsDeletedCount
- Cleanup_ReturnsBytesReclaimed
- Cleanup_DryRun_NoActualDeletion
- Cleanup_OrphanFiles_Removed
- Cleanup_OrphanMetadata_Removed

### Integration Tests

#### ArtifactStoreIntegrationTests
- Store_AndRetrieve_RoundTripsCorrectly
- Store_LargeArtifact_FileCreatedCorrectly
- Store_Compressed_DecompressesCorrectly
- List_ByCorrelationId_FindsRelatedArtifacts
- Delete_RemovesFromDatabaseAndFileSystem

#### RetentionIntegrationTests
- Cleanup_AgeBasedDeletion_Works
- Cleanup_SizeBasedDeletion_Works
- Cleanup_CountBasedDeletion_Works

### End-to-End Tests

#### ArtifactE2ETests
- Execute_Command_ArtifactsStored
- Execute_LargeOutput_TruncatedCorrectly
- Execute_WithSecrets_RedactedCorrectly
- CLI_ListArtifacts_Works
- CLI_ShowArtifact_DisplaysContent
- CLI_DeleteArtifact_Removes
- CLI_Cleanup_RemovesOld
- CLI_Stats_ShowsMetrics

### Performance Benchmarks

| Benchmark | Target | Maximum | Notes |
|-----------|--------|---------|-------|
| Store 1KB artifact (inline) | 1ms | 3ms | Database write |
| Store 1MB artifact (file) | 10ms | 25ms | File write + metadata |
| Store 10MB artifact (compressed) | 100ms | 250ms | Includes compression |
| Truncate 10MB (head) | 10ms | 25ms | Simple slice |
| Truncate 10MB (tail) | 10ms | 25ms | Simple slice |
| Truncate 10MB (smart) | 50ms | 100ms | Pattern matching |
| Redact 1MB content | 30ms | 75ms | Regex scanning |
| Compress 1MB content | 30ms | 50ms | GZip level 6 |
| Retrieve 1MB artifact | 10ms | 25ms | Decompress + read |
| List 100 artifacts | 5ms | 15ms | Database query |
| Cleanup 1000 artifacts | 5s | 15s | Batch deletion |

### Coverage Requirements

| Component | Minimum Coverage |
|-----------|-----------------|
| Artifact (model) | 95% |
| ArtifactStore | 90% |
| HeadTruncator | 95% |
| TailTruncator | 95% |
| SmartTruncator | 90% |
| ContentRedactor | 95% |
| RetentionPolicy | 90% |
| RetentionManager | 85% |

---

## User Verification Steps

### Scenario 1: Store Command Output as Artifact
```powershell
# Step 1: Execute a command that produces output
agentic-coding exec "dotnet build" --working-dir "src/MyProject"

# Step 2: List artifacts for the execution
agentic-coding artifacts list --last

# Expected Output:
# ID           Type    Size     Truncated  Created
# art-abc123   stdout  45.2 KB  No         2024-01-15 10:30:00
# art-abc124   stderr  1.2 KB   No         2024-01-15 10:30:00

# Step 3: View artifact content
agentic-coding artifacts show art-abc123

# Expected: Full stdout content displayed

# Verification: Command output automatically stored as artifacts
```

### Scenario 2: Large Output Truncation
```powershell
# Step 1: Execute command with large output
agentic-coding exec "dotnet test --logger:console -v detailed" --working-dir "tests"

# Step 2: Check artifact status
agentic-coding artifacts list --last

# Expected Output:
# ID           Type    Size      Truncated  Original Size  Strategy
# art-def456   stdout  1024 KB   Yes        15.3 MB        smart

# Step 3: View truncated artifact
agentic-coding artifacts show art-def456

# Expected Output shows:
# [TRUNCATED: 14.3 MB removed]
# ... first 5 lines of output ...
# [GAP: 1000 lines removed]
# error CS1002: ; expected at Program.cs:45
# error context line 1
# error context line 2
# [GAP: 500 lines removed]
# ... last 5 lines of output ...

# Verification: Smart truncation preserves errors with context
```

### Scenario 3: Sensitive Content Redaction
```powershell
# Step 1: Execute command that outputs secrets
agentic-coding exec "dotnet user-secrets list" --working-dir "src/MyApp"

# Step 2: List artifacts
agentic-coding artifacts list --last

# Expected Output:
# ID           Type    Size    Redactions
# art-ghi789   stdout  2.1 KB  3

# Step 3: View artifact content
agentic-coding artifacts show art-ghi789

# Expected Output:
# ConnectionStrings:Default = [REDACTED]
# ApiKeys:OpenAI = [REDACTED]
# Auth:ClientSecret = [REDACTED]

# Verification: Sensitive values automatically redacted before storage
```

### Scenario 4: Artifact Compression
```powershell
# Step 1: Execute command with compressible output
agentic-coding exec "Get-Content large-log.txt"

# Step 2: Check artifact details
agentic-coding artifacts show --metadata art-jkl012

# Expected Output:
# Artifact Details:
#   ID: art-jkl012
#   Type: stdout
#   Compressed: Yes
#   Original Size: 5.2 MB
#   Stored Size: 850 KB
#   Compression Ratio: 83.7%
#   Storage: File (.acode/artifacts/2024/01/15/art-jkl012.gz)

# Verification: Compression significantly reduces storage
```

### Scenario 5: Inline vs File Storage
```powershell
# Step 1: Execute command with small output
agentic-coding exec "echo 'Hello World'"

# Step 2: Check storage location
agentic-coding artifacts show --metadata art-small01

# Expected Output (small artifact):
# Storage: Inline (database)
# Size: 12 bytes

# Step 3: Execute command with large output
agentic-coding exec "dir -Recurse C:\Windows\System32"

# Step 4: Check storage location
agentic-coding artifacts show --metadata art-large01

# Expected Output (large artifact):
# Storage: File (.acode/artifacts/2024/01/15/art-large01.gz)
# Size: 2.5 MB

# Verification: Small artifacts inline, large artifacts as files
```

### Scenario 6: Artifact Cleanup by Age
```powershell
# Step 1: Check current artifacts
agentic-coding artifacts stats

# Expected Output:
# Total Artifacts: 1,523
# Total Size: 425 MB
# Oldest: 2023-12-01

# Step 2: Preview cleanup (dry run)
agentic-coding artifacts cleanup --older-than 7d --dry-run

# Expected Output:
# Dry Run - No artifacts will be deleted
# Would delete: 847 artifacts
# Would reclaim: 312 MB

# Step 3: Execute cleanup
agentic-coding artifacts cleanup --older-than 7d

# Expected Output:
# Deleted: 847 artifacts
# Reclaimed: 312 MB
# Remaining: 676 artifacts (113 MB)

# Verification: Age-based cleanup works correctly
```

### Scenario 7: Artifact Cleanup by Size
```powershell
# Step 1: Set size limit and run cleanup
agentic-coding artifacts cleanup --max-size 100MB

# Expected Output:
# Deleted: 234 artifacts (oldest first)
# Reclaimed: 45 MB
# Total size now: 98 MB (under 100 MB limit)

# Verification: Size-based cleanup deletes oldest until under limit
```

### Scenario 8: Query Artifacts by Correlation ID
```powershell
# Step 1: Execute multiple commands in a session
agentic-coding exec "dotnet restore"
agentic-coding exec "dotnet build"
agentic-coding exec "dotnet test"

# Step 2: Note the session/run ID from output
# Run ID: run-xyz789

# Step 3: List all artifacts for that run
agentic-coding artifacts list --run-id run-xyz789

# Expected Output:
# ID           Type    Command         Size
# art-001      stdout  dotnet restore  125 KB
# art-002      stderr  dotnet restore  0 bytes
# art-003      stdout  dotnet build    45 KB
# art-004      stderr  dotnet build    2.1 KB
# art-005      stdout  dotnet test     1.2 MB
# art-006      stderr  dotnet test     0 bytes

# Verification: Artifacts queryable by correlation ID
```

### Scenario 9: Artifact Statistics
```powershell
# Step 1: View storage statistics
agentic-coding artifacts stats

# Expected Output:
# Artifact Statistics:
#   Total Artifacts: 1,523
#   Total Storage: 425 MB
#   
#   By Type:
#     stdout: 1,200 (350 MB)
#     stderr: 300 (25 MB)
#     log: 23 (50 MB)
#   
#   Truncation:
#     Truncated: 145 (9.5%)
#     Redacted: 89 (5.8%)
#   
#   Compression:
#     Average Ratio: 72.3%
#     Space Saved: 1.1 GB
#   
#   Age Distribution:
#     Last 24h: 45
#     Last 7d: 312
#     Older: 1,166

# Verification: Comprehensive statistics available
```

### Scenario 10: Custom Truncation Strategy
```yaml
# Step 1: Configure smart truncation in .agent/config.yml
execution:
  artifacts:
    truncation:
      max_size_kb: 512
      strategy: smart
      error_patterns:
        - "error"
        - "fail"
        - "exception"
        - "CUSTOM_ERROR_CODE"
      warning_patterns:
        - "warn"
        - "CUSTOM_WARNING"
      context_lines: 15
```

```powershell
# Step 2: Execute command that triggers custom patterns
agentic-coding exec "run-custom-tool.exe"

# Step 3: Verify custom patterns detected
agentic-coding artifacts show art-custom01

# Expected: Lines containing CUSTOM_ERROR_CODE preserved with 15 context lines

# Verification: Custom error/warning patterns work correctly
```

---

## Implementation Prompt

You are implementing Task-018c: Artifact Logging + Truncation for the Agentic Coding Bot. This subtask creates the infrastructure to store, manage, and intelligently truncate command execution artifacts (stdout, stderr, log files) while protecting sensitive content through redaction and managing storage through retention policies.

### File Structure

```
src/
├── AgenticCoder.Domain/
│   └── Execution/
│       └── Artifacts/
│           ├── IArtifact.cs
│           ├── Artifact.cs
│           ├── ArtifactType.cs
│           ├── TruncationResult.cs
│           ├── TruncationStrategy.cs
│           ├── ArtifactMetadata.cs
│           └── RetentionConfiguration.cs
│
├── AgenticCoder.Application/
│   └── Execution/
│       └── Artifacts/
│           ├── IArtifactService.cs
│           ├── ArtifactService.cs
│           ├── ITruncator.cs
│           ├── IContentRedactor.cs
│           ├── IRetentionPolicy.cs
│           └── IArtifactStore.cs
│
├── AgenticCoder.Infrastructure/
│   └── Execution/
│       └── Artifacts/
│           ├── ArtifactStore.cs
│           ├── HeadTruncator.cs
│           ├── TailTruncator.cs
│           ├── SmartTruncator.cs
│           ├── ContentRedactor.cs
│           ├── RetentionManager.cs
│           ├── CompressionHelper.cs
│           └── DependencyInjection/
│               └── ArtifactServiceExtensions.cs
│
└── tests/
    ├── AgenticCoder.Domain.Tests/
    │   └── Execution/
    │       └── Artifacts/
    │           └── ArtifactTests.cs
    │
    └── AgenticCoder.Infrastructure.Tests/
        └── Execution/
            └── Artifacts/
                ├── ArtifactStoreTests.cs
                ├── HeadTruncatorTests.cs
                ├── TailTruncatorTests.cs
                ├── SmartTruncatorTests.cs
                ├── ContentRedactorTests.cs
                └── RetentionManagerTests.cs
```

### Domain Models

```csharp
namespace AgenticCoder.Domain.Execution.Artifacts;

/// <summary>
/// Types of artifacts that can be stored
/// </summary>
public enum ArtifactType
{
    Stdout,
    Stderr,
    CombinedOutput,
    LogFile,
    BuildOutput,
    TestResult,
    GenericFile,
    ErrorDump
}

/// <summary>
/// Truncation strategy identifiers
/// </summary>
public enum TruncationStrategy
{
    None,
    Head,
    Tail,
    Smart
}

/// <summary>
/// Artifact interface for dependency inversion
/// </summary>
public interface IArtifact
{
    Guid Id { get; }
    ArtifactType Type { get; }
    string Name { get; }
    long SizeBytes { get; }
    long? OriginalSizeBytes { get; }
    bool Truncated { get; }
    TruncationStrategy TruncationStrategy { get; }
    bool Compressed { get; }
    bool Inline { get; }
    string? FilePath { get; }
    int RedactionCount { get; }
    DateTimeOffset CreatedAt { get; }
    IReadOnlyDictionary<string, string> CorrelationIds { get; }
}

/// <summary>
/// Immutable artifact record
/// </summary>
public sealed record Artifact : IArtifact
{
    public required Guid Id { get; init; }
    public required ArtifactType Type { get; init; }
    public required string Name { get; init; }
    public required long SizeBytes { get; init; }
    public long? OriginalSizeBytes { get; init; }
    public bool Truncated { get; init; }
    public TruncationStrategy TruncationStrategy { get; init; } = TruncationStrategy.None;
    public bool Compressed { get; init; }
    public bool Inline { get; init; }
    public string? FilePath { get; init; }
    public int RedactionCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyDictionary<string, string> CorrelationIds { get; init; } 
        = new Dictionary<string, string>();
    
    // Inline content (only populated if Inline = true)
    public byte[]? Content { get; init; }
}

/// <summary>
/// Result of truncation operation
/// </summary>
public sealed record TruncationResult
{
    public required string Content { get; init; }
    public required long OriginalSizeBytes { get; init; }
    public required bool WasTruncated { get; init; }
    public required TruncationStrategy StrategyUsed { get; init; }
    public int LinesRemoved { get; init; }
    public int SectionsCount { get; init; }
}

/// <summary>
/// Retention configuration
/// </summary>
public sealed record RetentionConfiguration
{
    public int MaxAgeDays { get; init; } = 30;
    public long MaxTotalSizeBytes { get; init; } = 500 * 1024 * 1024; // 500 MB
    public int MaxCount { get; init; } = 10000;
    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromHours(24);
}
```

### Core Interfaces

```csharp
namespace AgenticCoder.Application.Execution.Artifacts;

public interface IArtifactStore
{
    /// <summary>
    /// Store artifact content with metadata
    /// </summary>
    Task<Artifact> StoreAsync(
        string content,
        ArtifactType type,
        string name,
        IReadOnlyDictionary<string, string> correlationIds,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get artifact metadata by ID
    /// </summary>
    Task<Artifact?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get artifact content (decompressed)
    /// </summary>
    Task<string?> GetContentAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// List artifacts with optional filtering
    /// </summary>
    Task<IReadOnlyList<Artifact>> ListAsync(
        ArtifactFilter? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete artifact by ID
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete multiple artifacts
    /// </summary>
    Task<int> DeleteManyAsync(
        IEnumerable<Guid> ids, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get storage statistics
    /// </summary>
    Task<ArtifactStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default);
}

public interface ITruncator
{
    /// <summary>
    /// Truncate content to maximum size
    /// </summary>
    TruncationResult Truncate(string content, int maxBytes);
    
    /// <summary>
    /// Strategy this truncator implements
    /// </summary>
    TruncationStrategy Strategy { get; }
}

public interface IContentRedactor
{
    /// <summary>
    /// Redact sensitive content based on patterns
    /// </summary>
    (string RedactedContent, int RedactionCount) Redact(
        string content, 
        IEnumerable<string> patterns);
}

public interface IRetentionPolicy
{
    /// <summary>
    /// Get artifacts that should be deleted based on policy
    /// </summary>
    Task<IReadOnlyList<Guid>> GetArtifactsToDeleteAsync(
        RetentionConfiguration config,
        CancellationToken cancellationToken = default);
}
```

### Infrastructure Implementations

```csharp
namespace AgenticCoder.Infrastructure.Execution.Artifacts;

public sealed class SmartTruncator : ITruncator
{
    private readonly SmartTruncatorOptions _options;
    private readonly ILogger<SmartTruncator> _logger;
    
    public TruncationStrategy Strategy => TruncationStrategy.Smart;
    
    public SmartTruncator(
        IOptions<SmartTruncatorOptions> options,
        ILogger<SmartTruncator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }
    
    public TruncationResult Truncate(string content, int maxBytes)
    {
        var originalSize = Encoding.UTF8.GetByteCount(content);
        
        if (originalSize <= maxBytes)
        {
            return new TruncationResult
            {
                Content = content,
                OriginalSizeBytes = originalSize,
                WasTruncated = false,
                StrategyUsed = Strategy
            };
        }
        
        var lines = content.Split('\n');
        var importantLines = FindImportantLines(lines);
        
        var result = new StringBuilder();
        var sections = new List<(int Start, int End)>();
        
        // Always include first N lines
        var firstLines = Math.Min(_options.AlwaysIncludeFirstLines, lines.Length);
        for (int i = 0; i < firstLines; i++)
        {
            result.AppendLine(lines[i]);
        }
        sections.Add((0, firstLines - 1));
        
        // Add important lines with context
        foreach (var important in importantLines.OrderBy(i => i))
        {
            var sectionStart = Math.Max(firstLines, important - _options.ContextLines);
            var sectionEnd = Math.Min(lines.Length - 1, important + _options.ContextLines);
            
            // Check if we overlap with previous section
            if (sections.Count > 0 && sectionStart <= sections[^1].End + 1)
            {
                // Extend previous section
                sections[^1] = (sections[^1].Start, sectionEnd);
            }
            else
            {
                // Add gap marker
                if (sections.Count > 0)
                {
                    result.AppendLine($"[... {sectionStart - sections[^1].End - 1} lines removed ...]");
                }
                sections.Add((sectionStart, sectionEnd));
            }
        }
        
        // Add section lines (skip first section, already added)
        for (int s = 1; s < sections.Count; s++)
        {
            var (start, end) = sections[s];
            for (int i = start; i <= end; i++)
            {
                result.AppendLine(lines[i]);
            }
        }
        
        // Always include last N lines
        var lastStart = Math.Max(0, lines.Length - _options.AlwaysIncludeLastLines);
        if (sections.Count == 0 || sections[^1].End < lastStart - 1)
        {
            result.AppendLine($"[... {lastStart - (sections.Count > 0 ? sections[^1].End : firstLines) - 1} lines removed ...]");
            for (int i = lastStart; i < lines.Length; i++)
            {
                result.AppendLine(lines[i]);
            }
        }
        
        // Enforce byte limit with final check
        var resultStr = result.ToString();
        var resultBytes = Encoding.UTF8.GetByteCount(resultStr);
        if (resultBytes > maxBytes)
        {
            // Fall back to tail truncation for final trim
            var tailTruncator = new TailTruncator();
            return tailTruncator.Truncate(resultStr, maxBytes);
        }
        
        return new TruncationResult
        {
            Content = resultStr,
            OriginalSizeBytes = originalSize,
            WasTruncated = true,
            StrategyUsed = Strategy,
            LinesRemoved = lines.Length - resultStr.Split('\n').Length,
            SectionsCount = sections.Count
        };
    }
    
    private List<int> FindImportantLines(string[] lines)
    {
        var important = new List<int>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Check error patterns (highest priority)
            if (_options.ErrorPatterns.Any(p => 
                line.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                important.Add(i);
                continue;
            }
            
            // Check warning patterns (lower priority)
            if (_options.WarningPatterns.Any(p => 
                line.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                important.Add(i);
            }
        }
        
        return important;
    }
}

public sealed class ContentRedactor : IContentRedactor
{
    private const string RedactedPlaceholder = "[REDACTED]";
    private readonly ILogger<ContentRedactor> _logger;
    
    public ContentRedactor(ILogger<ContentRedactor> logger)
    {
        _logger = logger;
    }
    
    public (string RedactedContent, int RedactionCount) Redact(
        string content, 
        IEnumerable<string> patterns)
    {
        var redactionCount = 0;
        var result = content;
        
        foreach (var pattern in patterns)
        {
            try
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                var matches = regex.Matches(result);
                redactionCount += matches.Count;
                result = regex.Replace(result, RedactedPlaceholder);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, 
                    "Invalid redaction pattern skipped: {Pattern}", pattern);
            }
        }
        
        if (redactionCount > 0)
        {
            _logger.LogDebug(
                "Redacted {Count} sensitive values from content", 
                redactionCount);
        }
        
        return (result, redactionCount);
    }
}

public sealed class ArtifactStore : IArtifactStore
{
    private readonly IWorkspaceDatabase _database;
    private readonly IContentRedactor _redactor;
    private readonly Func<TruncationStrategy, ITruncator> _truncatorFactory;
    private readonly ArtifactStoreOptions _options;
    private readonly ILogger<ArtifactStore> _logger;
    
    public async Task<Artifact> StoreAsync(
        string content,
        ArtifactType type,
        string name,
        IReadOnlyDictionary<string, string> correlationIds,
        CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        
        // Step 1: Redact sensitive content (BEFORE any storage)
        var (redactedContent, redactionCount) = _redactor.Redact(
            content, 
            _options.RedactionPatterns);
        
        // Step 2: Truncate if needed
        var truncator = _truncatorFactory(_options.TruncationStrategy);
        var truncationResult = truncator.Truncate(
            redactedContent, 
            _options.MaxArtifactSizeBytes);
        
        var finalContent = truncationResult.Content;
        var contentBytes = Encoding.UTF8.GetBytes(finalContent);
        
        // Step 3: Compress if enabled
        byte[] storedBytes;
        bool compressed = false;
        
        if (_options.CompressionEnabled)
        {
            var compressedBytes = CompressionHelper.Compress(contentBytes);
            if (compressedBytes.Length < contentBytes.Length)
            {
                storedBytes = compressedBytes;
                compressed = true;
                _logger.LogDebug(
                    "Compressed artifact {Id}: {Original} -> {Compressed} bytes", 
                    id, contentBytes.Length, compressedBytes.Length);
            }
            else
            {
                storedBytes = contentBytes;
            }
        }
        else
        {
            storedBytes = contentBytes;
        }
        
        // Step 4: Decide inline vs file storage
        bool inline = storedBytes.Length <= _options.InlineThresholdBytes;
        string? filePath = null;
        
        if (!inline)
        {
            filePath = await StoreToFileAsync(id, storedBytes, cancellationToken);
        }
        
        // Step 5: Create and persist artifact
        var artifact = new Artifact
        {
            Id = id,
            Type = type,
            Name = name,
            SizeBytes = storedBytes.Length,
            OriginalSizeBytes = truncationResult.OriginalSizeBytes,
            Truncated = truncationResult.WasTruncated,
            TruncationStrategy = truncationResult.StrategyUsed,
            Compressed = compressed,
            Inline = inline,
            FilePath = filePath,
            Content = inline ? storedBytes : null,
            RedactionCount = redactionCount,
            CorrelationIds = correlationIds
        };
        
        await _database.InsertArtifactAsync(artifact, cancellationToken);
        
        _logger.LogInformation(
            "Stored artifact {Id}: Type={Type}, Size={Size}, Truncated={Truncated}, Redactions={Redactions}",
            id, type, storedBytes.Length, truncationResult.WasTruncated, redactionCount);
        
        return artifact;
    }
    
    private async Task<string> StoreToFileAsync(
        Guid id, 
        byte[] content, 
        CancellationToken cancellationToken)
    {
        var date = DateTime.UtcNow;
        var directory = Path.Combine(
            _options.ArtifactDirectory,
            date.Year.ToString(),
            date.Month.ToString("00"),
            date.Day.ToString("00"));
        
        Directory.CreateDirectory(directory);
        
        var fileName = $"{id}.bin";
        var filePath = Path.Combine(directory, fileName);
        
        await File.WriteAllBytesAsync(filePath, content, cancellationToken);
        
        return filePath;
    }
}
```

### CLI Commands

```csharp
// CLI command implementations for artifact management
[Command("artifacts", Description = "Manage execution artifacts")]
public class ArtifactCommands
{
    [Command("list", Description = "List artifacts")]
    public async Task<int> ListAsync(
        [Option("--run-id", "Filter by run ID")] string? runId = null,
        [Option("--type", "Filter by type")] ArtifactType? type = null,
        [Option("--last", "Show artifacts from last execution")] bool last = false)
    
    [Command("show", Description = "Show artifact content")]
    public async Task<int> ShowAsync(
        [Argument] string artifactId,
        [Option("--metadata", "Show metadata only")] bool metadataOnly = false)
    
    [Command("delete", Description = "Delete artifact")]
    public async Task<int> DeleteAsync([Argument] string artifactId)
    
    [Command("cleanup", Description = "Clean up old artifacts")]
    public async Task<int> CleanupAsync(
        [Option("--older-than", "Delete older than (e.g., 7d, 30d)")] string? olderThan = null,
        [Option("--max-size", "Delete until under size (e.g., 100MB)")] string? maxSize = null,
        [Option("--dry-run", "Preview only, don't delete")] bool dryRun = false)
    
    [Command("stats", Description = "Show artifact statistics")]
    public async Task<int> StatsAsync()
}
```

### Error Codes

| Code | Category | Description |
|------|----------|-------------|
| ART-018C-01 | Storage | Failed to store artifact |
| ART-018C-02 | Storage | Failed to retrieve artifact |
| ART-018C-03 | Storage | Artifact not found |
| ART-018C-04 | Storage | Database write failed |
| ART-018C-05 | Storage | File write failed |
| ART-018C-06 | Truncation | Truncation failed |
| ART-018C-07 | Redaction | Invalid redaction pattern |
| ART-018C-08 | Compression | Compression failed |
| ART-018C-09 | Compression | Decompression failed |
| ART-018C-10 | Cleanup | Cleanup failed |
| ART-018C-11 | Cleanup | Orphan detection failed |
| ART-018C-12 | Query | Invalid filter criteria |

### Implementation Checklist

1. [ ] Create domain models (Artifact, ArtifactType, TruncationResult, RetentionConfiguration)
2. [ ] Create interfaces (IArtifactStore, ITruncator, IContentRedactor, IRetentionPolicy)
3. [ ] Implement HeadTruncator
4. [ ] Implement TailTruncator
5. [ ] Implement SmartTruncator with error/warning pattern detection
6. [ ] Implement ContentRedactor with regex pattern support
7. [ ] Implement CompressionHelper using GZip
8. [ ] Implement ArtifactStore with inline/file storage decision
9. [ ] Implement RetentionManager with age/size/count policies
10. [ ] Integrate with workspace database (Task-050)
11. [ ] Add CLI commands (list, show, delete, cleanup, stats)
12. [ ] Write unit tests for all truncators
13. [ ] Write unit tests for ContentRedactor
14. [ ] Write unit tests for ArtifactStore
15. [ ] Write unit tests for RetentionManager
16. [ ] Write integration tests
17. [ ] Add performance benchmarks
18. [ ] Update documentation

### Rollout Plan

| Phase | Components | Validation |
|-------|------------|------------|
| 1 | Domain models and interfaces | Compiles, tests pass |
| 2 | HeadTruncator, TailTruncator | Basic truncation works |
| 3 | SmartTruncator | Error preservation works |
| 4 | ContentRedactor | Secrets redacted |
| 5 | ArtifactStore (inline) | Small artifacts stored |
| 6 | ArtifactStore (file + compression) | Large artifacts stored |
| 7 | RetentionManager | Cleanup works |
| 8 | CLI commands | Full functionality |

### Dependencies

- **Task-018a**: Provides stdout/stderr content to store
- **Task-050**: Provides workspace database for metadata
- **Task-021a**: Provides artifact directory standards
- **.NET 8.0**: GZip compression, file I/O

---

**End of Task 018.c Specification**