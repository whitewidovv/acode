# Task 021: Artifact Collection + Run Inspection

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 018 (Command Runner), Task 050 (Workspace Database)  

---

## Description

### Overview

Task 021 implements the artifact collection and run inspection subsystem for Agentic Coding Bot. Every command execution, build operation, test run, and code generation produces artifacts that must be captured, organized, stored, and made accessible for later inspection. This system provides the historical record of all agent activities, enabling debugging, auditing, and continuous improvement.

The artifact collection system operates transparently during every agent operation, automatically capturing outputs without requiring explicit user action. Run records create a queryable history of all executions, while artifact storage provides structured access to the actual outputs, logs, and generated files.

### Business Value

Comprehensive artifact collection and run inspection deliver critical value across multiple dimensions:

1. **Debugging Capability** — When builds fail or tests break, developers can inspect exactly what happened, including full command output, exit codes, and timing information
2. **Audit Trail** — Enterprise environments require complete records of all automated operations for compliance and security review
3. **Reproducibility** — Captured artifacts enable reproduction of issues by providing the exact context of failed operations
4. **Performance Analysis** — Historical run data enables identification of performance trends, slow operations, and optimization opportunities
5. **Learning Opportunity** — By reviewing what the agent did, users can understand its decision-making and improve their workflows
6. **Rollback Support** — Artifacts preserve previous states, enabling recovery when changes cause problems
7. **Team Collaboration** — Sharable run bundles allow team members to review and discuss agent operations
8. **Cost Tracking** — Run records with duration and resource usage support cost analysis for agent operations

### Scope

This task delivers the following capabilities:

1. **RunRecord Model** — Immutable record of each execution with unique ID, command, result, timing, and artifact references
2. **IRunStore Interface** — Abstraction for run persistence with CRUD operations and query support
3. **Artifact Collection Pipeline** — Automatic capture of stdout, stderr, logs, test results, and output files
4. **Artifact Storage** — Organized file-based storage with configurable retention policies
5. **Run Queries** — Flexible filtering by time range, status, command pattern, session, and task
6. **Run Inspection CLI** — Commands to list, show, and export runs and their artifacts
7. **Retention Management** — Automatic cleanup of old runs based on configurable policies
8. **Export Bundles** — Portable packages containing run records and artifacts for sharing

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 018 Command Runner | Upstream | Receives execution results and captures artifacts |
| Task 019 Language Runners | Upstream | Collects language-specific build/test artifacts |
| Task 020 Docker Sandbox | Upstream | Extracts artifacts from container execution |
| Task 050 Workspace Database | Downstream | Persists run records in workspace SQLite |
| Task 014 RepoFS | Sibling | Reads generated files for artifact collection |
| Epic 07 Output Modes | Downstream | Uses run data for summary reports |
| Epic 10 Telemetry | Downstream | Aggregates run metrics for dashboards |
| CLI Layer | Downstream | Exposes inspection commands to users |
| Session Manager | Sibling | Correlates runs with sessions |

### Failure Modes

| Failure | Detection | Impact | Recovery |
|---------|-----------|--------|----------|
| Disk full during artifact write | IOException | Artifacts lost for current run | Alert user, skip artifact, continue |
| Database write failure | SqliteException | Run record not persisted | Retry with backoff, log warning |
| Corrupt artifact file | Checksum mismatch | Artifact unreadable | Mark as corrupt, exclude from queries |
| Query timeout | Operation timeout | Results not returned | Return partial results with warning |
| Retention job fails | Job exception | Old artifacts accumulate | Retry next cycle, alert if repeated |
| Export bundle too large | Size limit exceeded | Bundle creation fails | Split into multiple bundles |
| Artifact path collision | Duplicate path | Overwrite risk | Generate unique paths with timestamp |
| Missing referenced artifact | File not found | Incomplete run data | Return run with missing artifact flag |
| Concurrent write conflict | Optimistic lock failure | Write rejected | Retry with new version |
| Invalid query parameters | Validation failure | Query not executed | Return validation error message |

### Assumptions

1. Workspace database (Task 050) is available for run record persistence
2. Local filesystem has sufficient space for artifact storage
3. Command execution (Task 018) provides structured result objects
4. Session management provides correlation IDs
5. Artifact directories are writable by the agent process
6. File paths use forward slashes for cross-platform compatibility
7. Timestamps use UTC for consistency
8. Run IDs are globally unique (GUID-based)
9. Artifact content is treated as binary (no encoding assumptions)
10. Retention policies are configured in agent-config.yml

### Security Considerations

1. **Sensitive Content** — Artifacts may contain secrets; storage must respect .gitignore patterns
2. **Access Control** — Run inspection should respect workspace permissions
3. **Export Sanitization** — Export bundles should have option to redact sensitive content
4. **Path Traversal** — Artifact paths must be validated to prevent directory traversal attacks
5. **Size Limits** — Individual artifacts and total storage must have configurable limits
6. **Retention Compliance** — Some artifacts may need retention for compliance; others deletion
7. **Audit Logging** — All artifact access should be logged for security review
8. **Encryption at Rest** — Sensitive artifacts should support optional encryption

### Subtask Decomposition

| Subtask | Title | Scope |
|---------|-------|-------|
| Task 021.a | Artifact Directory Standards | Directory structure, naming conventions, metadata format |
| Task 021.b | Run Inspection CLI | CLI commands for listing, showing, and filtering runs |
| Task 021.c | Export Bundle Format | Bundle specification, creation, and import commands |

---

## Functional Requirements

### Run Record Model (FR-021-01 through FR-021-20)

| ID | Requirement |
|----|-------------|
| FR-021-01 | RunRecord MUST have a unique GUID identifier |
| FR-021-02 | RunRecord MUST store the executed command string |
| FR-021-03 | RunRecord MUST store command arguments array |
| FR-021-04 | RunRecord MUST store working directory path |
| FR-021-05 | RunRecord MUST store execution result (Success/Failure/Timeout/Cancelled) |
| FR-021-06 | RunRecord MUST store exit code as nullable integer |
| FR-021-07 | RunRecord MUST store start timestamp in UTC |
| FR-021-08 | RunRecord MUST store end timestamp in UTC |
| FR-021-09 | RunRecord MUST store duration in milliseconds |
| FR-021-10 | RunRecord MUST store session ID for correlation |
| FR-021-11 | RunRecord MUST store optional task ID for correlation |
| FR-021-12 | RunRecord MUST store optional parent run ID for nested runs |
| FR-021-13 | RunRecord MUST store run type (Command/Build/Test/Generate) |
| FR-021-14 | RunRecord MUST store execution context (Host/Docker/Remote) |
| FR-021-15 | RunRecord MUST store list of artifact IDs |
| FR-021-16 | RunRecord MUST store optional error message |
| FR-021-17 | RunRecord MUST store optional error code |
| FR-021-18 | RunRecord MUST store tags as key-value pairs |
| FR-021-19 | RunRecord MUST be immutable after creation |
| FR-021-20 | RunRecord MUST serialize to JSON for export |

### Artifact Model (FR-021-21 through FR-021-40)

| ID | Requirement |
|----|-------------|
| FR-021-21 | Artifact MUST have a unique GUID identifier |
| FR-021-22 | Artifact MUST have an artifact type (Stdout/Stderr/Log/TestResult/Output/Diff) |
| FR-021-23 | Artifact MUST store file path relative to artifact root |
| FR-021-24 | Artifact MUST store original file name |
| FR-021-25 | Artifact MUST store content size in bytes |
| FR-021-26 | Artifact MUST store content hash (SHA256) |
| FR-021-27 | Artifact MUST store MIME type |
| FR-021-28 | Artifact MUST store creation timestamp |
| FR-021-29 | Artifact MUST store optional description |
| FR-021-30 | Artifact MUST reference parent run ID |
| FR-021-31 | Artifact MUST support binary content |
| FR-021-32 | Artifact MUST support text content with encoding |
| FR-021-33 | Artifact MUST support streaming for large files |
| FR-021-34 | Artifact MUST track whether content is truncated |
| FR-021-35 | Artifact MUST store truncation reason if applicable |
| FR-021-36 | Artifact MUST support compression for storage |
| FR-021-37 | Artifact MUST validate content hash on read |
| FR-021-38 | Artifact MUST support metadata tags |
| FR-021-39 | Artifact MUST track sensitive content flag |
| FR-021-40 | Artifact MUST serialize to JSON for export |

### Run Store Interface (FR-021-41 through FR-021-60)

| ID | Requirement |
|----|-------------|
| FR-021-41 | IRunStore MUST define CreateAsync method |
| FR-021-42 | IRunStore MUST define GetByIdAsync method |
| FR-021-43 | IRunStore MUST define ListAsync with filtering |
| FR-021-44 | IRunStore MUST define DeleteAsync method |
| FR-021-45 | IRunStore MUST define CountAsync method |
| FR-021-46 | IRunStore MUST define ExistsAsync method |
| FR-021-47 | IRunStore MUST support pagination parameters |
| FR-021-48 | IRunStore MUST support sorting parameters |
| FR-021-49 | IRunStore MUST define GetLatestAsync method |
| FR-021-50 | IRunStore MUST define GetBySessionAsync method |
| FR-021-51 | IRunStore MUST define GetByTaskAsync method |
| FR-021-52 | IRunStore MUST define GetFailedAsync method |
| FR-021-53 | IRunStore MUST define DeleteOlderThanAsync method |
| FR-021-54 | IRunStore MUST define GetStatisticsAsync method |
| FR-021-55 | IRunStore MUST persist to workspace database |
| FR-021-56 | IRunStore MUST support transactions |
| FR-021-57 | IRunStore MUST handle concurrent access |
| FR-021-58 | IRunStore MUST emit events on create/delete |
| FR-021-59 | IRunStore MUST validate run records before save |
| FR-021-60 | IRunStore MUST enforce storage limits |

### Artifact Store Interface (FR-021-61 through FR-021-80)

| ID | Requirement |
|----|-------------|
| FR-021-61 | IArtifactStore MUST define SaveAsync method |
| FR-021-62 | IArtifactStore MUST define GetByIdAsync method |
| FR-021-63 | IArtifactStore MUST define GetByRunIdAsync method |
| FR-021-64 | IArtifactStore MUST define DeleteAsync method |
| FR-021-65 | IArtifactStore MUST define GetContentAsync method |
| FR-021-66 | IArtifactStore MUST define GetContentStreamAsync method |
| FR-021-67 | IArtifactStore MUST define ExistsAsync method |
| FR-021-68 | IArtifactStore MUST store content on filesystem |
| FR-021-69 | IArtifactStore MUST store metadata in database |
| FR-021-70 | IArtifactStore MUST compute and verify hashes |
| FR-021-71 | IArtifactStore MUST compress large artifacts |
| FR-021-72 | IArtifactStore MUST truncate oversized artifacts |
| FR-021-73 | IArtifactStore MUST support artifact size limits |
| FR-021-74 | IArtifactStore MUST cleanup orphaned files |
| FR-021-75 | IArtifactStore MUST handle special characters in names |
| FR-021-76 | IArtifactStore MUST create directories as needed |
| FR-021-77 | IArtifactStore MUST respect filesystem permissions |
| FR-021-78 | IArtifactStore MUST support atomic writes |
| FR-021-79 | IArtifactStore MUST emit events on save/delete |
| FR-021-80 | IArtifactStore MUST provide storage statistics |

### Artifact Collection (FR-021-81 through FR-021-100)

| ID | Requirement |
|----|-------------|
| FR-021-81 | Collector MUST capture stdout from command execution |
| FR-021-82 | Collector MUST capture stderr from command execution |
| FR-021-83 | Collector MUST capture combined output if configured |
| FR-021-84 | Collector MUST capture log files from working directory |
| FR-021-85 | Collector MUST capture test result files (*.trx, *.xml) |
| FR-021-86 | Collector MUST capture coverage reports if generated |
| FR-021-87 | Collector MUST capture build outputs if configured |
| FR-021-88 | Collector MUST capture diff files if generated |
| FR-021-89 | Collector MUST respect .gitignore patterns |
| FR-021-90 | Collector MUST respect artifact exclusion patterns |
| FR-021-91 | Collector MUST limit individual artifact size |
| FR-021-92 | Collector MUST limit total artifacts per run |
| FR-021-93 | Collector MUST detect and skip binary files if configured |
| FR-021-94 | Collector MUST preserve file timestamps |
| FR-021-95 | Collector MUST handle file access errors gracefully |
| FR-021-96 | Collector MUST run asynchronously without blocking |
| FR-021-97 | Collector MUST support custom artifact patterns |
| FR-021-98 | Collector MUST deduplicate identical artifacts |
| FR-021-99 | Collector MUST tag artifacts by source (stdout/file/etc) |
| FR-021-100 | Collector MUST complete within configured timeout |

### Run Queries (FR-021-101 through FR-021-120)

| ID | Requirement |
|----|-------------|
| FR-021-101 | Query MUST support filtering by time range (start/end) |
| FR-021-102 | Query MUST support filtering by relative time (last N hours) |
| FR-021-103 | Query MUST support filtering by status (success/failure) |
| FR-021-104 | Query MUST support filtering by exit code |
| FR-021-105 | Query MUST support filtering by command pattern (wildcard) |
| FR-021-106 | Query MUST support filtering by command pattern (regex) |
| FR-021-107 | Query MUST support filtering by session ID |
| FR-021-108 | Query MUST support filtering by task ID |
| FR-021-109 | Query MUST support filtering by run type |
| FR-021-110 | Query MUST support filtering by execution context |
| FR-021-111 | Query MUST support filtering by tags |
| FR-021-112 | Query MUST support filtering by duration range |
| FR-021-113 | Query MUST support filtering by has-artifacts |
| FR-021-114 | Query MUST support multiple filters (AND logic) |
| FR-021-115 | Query MUST support pagination (skip/take) |
| FR-021-116 | Query MUST support sorting by any field |
| FR-021-117 | Query MUST support ascending/descending order |
| FR-021-118 | Query MUST return total count with paginated results |
| FR-021-119 | Query MUST timeout after configurable duration |
| FR-021-120 | Query MUST return empty result for no matches (not error) |

### Retention Management (FR-021-121 through FR-021-135)

| ID | Requirement |
|----|-------------|
| FR-021-121 | Retention MUST support time-based cleanup (delete after N days) |
| FR-021-122 | Retention MUST support count-based cleanup (keep last N runs) |
| FR-021-123 | Retention MUST support size-based cleanup (max storage size) |
| FR-021-124 | Retention MUST support status-based rules (keep failures longer) |
| FR-021-125 | Retention MUST support tagged-runs exclusion (never delete tagged) |
| FR-021-126 | Retention MUST run on configurable schedule |
| FR-021-127 | Retention MUST run on startup if enabled |
| FR-021-128 | Retention MUST be triggerable manually via CLI |
| FR-021-129 | Retention MUST delete artifacts when deleting runs |
| FR-021-130 | Retention MUST log all deletions for audit |
| FR-021-131 | Retention MUST respect minimum retention period |
| FR-021-132 | Retention MUST handle large cleanup batches efficiently |
| FR-021-133 | Retention MUST not block other operations during cleanup |
| FR-021-134 | Retention MUST report cleanup statistics |
| FR-021-135 | Retention MUST support dry-run mode |

---

## Non-Functional Requirements

### Performance (NFR-021-01 through NFR-021-15)

| ID | Requirement |
|----|-------------|
| NFR-021-01 | Run record creation MUST complete in under 50ms |
| NFR-021-02 | Artifact save MUST achieve 100MB/s throughput |
| NFR-021-03 | Run query with 10,000 records MUST return in under 500ms |
| NFR-021-04 | Artifact content read MUST stream without loading full content |
| NFR-021-05 | Concurrent artifact writes MUST be supported (10+ parallel) |
| NFR-021-06 | Database queries MUST use indexes for all filter fields |
| NFR-021-07 | Retention cleanup MUST process 1000 runs in under 60 seconds |
| NFR-021-08 | Memory usage during collection MUST stay under 100MB |
| NFR-021-09 | Large artifact streaming MUST not buffer entire content |
| NFR-021-10 | Export bundle creation MUST achieve 50MB/s throughput |
| NFR-021-11 | Run list pagination MUST return first page in under 100ms |
| NFR-021-12 | Artifact hash computation MUST be parallelized |
| NFR-021-13 | Compression MUST reduce storage by 50%+ for text artifacts |
| NFR-021-14 | Cold query (no cache) MUST complete in under 1 second |
| NFR-021-15 | Hot query (cached) MUST complete in under 100ms |

### Reliability (NFR-021-16 through NFR-021-28)

| ID | Requirement |
|----|-------------|
| NFR-021-16 | Run records MUST survive application restart |
| NFR-021-17 | Artifacts MUST survive application restart |
| NFR-021-18 | Database corruption MUST be recoverable |
| NFR-021-19 | Orphaned artifacts MUST be detected and cleaned |
| NFR-021-20 | Partial writes MUST be rolled back atomically |
| NFR-021-21 | Disk full errors MUST be handled gracefully |
| NFR-021-22 | Database locked errors MUST be retried with backoff |
| NFR-021-23 | File permission errors MUST be reported clearly |
| NFR-021-24 | Hash verification failures MUST be logged and reported |
| NFR-021-25 | Missing artifacts MUST not break run queries |
| NFR-021-26 | Concurrent access MUST not corrupt data |
| NFR-021-27 | Export bundles MUST include integrity verification |
| NFR-021-28 | Import MUST validate bundle integrity before applying |

### Security (NFR-021-29 through NFR-021-38)

| ID | Requirement |
|----|-------------|
| NFR-021-29 | Artifact paths MUST be validated against traversal attacks |
| NFR-021-30 | Sensitive artifacts MUST be flagged and protected |
| NFR-021-31 | Export bundles MUST support content redaction |
| NFR-021-32 | Database MUST be protected from SQL injection |
| NFR-021-33 | File operations MUST use safe path handling |
| NFR-021-34 | Artifact content MUST not be executed |
| NFR-021-35 | Access to artifacts MUST be logged for audit |
| NFR-021-36 | Deletion events MUST be logged for audit |
| NFR-021-37 | Encrypted artifacts MUST use AES-256 |
| NFR-021-38 | Secrets in stdout/stderr SHOULD be detected and masked |

### Maintainability (NFR-021-39 through NFR-021-48)

| ID | Requirement |
|----|-------------|
| NFR-021-39 | All components MUST have interfaces for testing |
| NFR-021-40 | Database schema MUST support migrations |
| NFR-021-41 | Configuration MUST be external (agent-config.yml) |
| NFR-021-42 | Logging MUST use structured format with correlation |
| NFR-021-43 | Error messages MUST include actionable guidance |
| NFR-021-44 | Storage format MUST be versioned for compatibility |
| NFR-021-45 | Export bundle format MUST be versioned |
| NFR-021-46 | Unit tests MUST achieve 90% coverage |
| NFR-021-47 | Integration tests MUST cover all query patterns |
| NFR-021-48 | Documentation MUST include schema diagrams |

### Observability (NFR-021-49 through NFR-021-60)

| ID | Requirement |
|----|-------------|
| NFR-021-49 | Run creation MUST emit metric |
| NFR-021-50 | Artifact save MUST emit metric with size |
| NFR-021-51 | Query duration MUST emit metric |
| NFR-021-52 | Retention cleanup MUST emit metric with count |
| NFR-021-53 | Storage usage MUST be exposed as metric |
| NFR-021-54 | Failed operations MUST emit error metric |
| NFR-021-55 | Run success rate MUST be calculable from metrics |
| NFR-021-56 | Average run duration MUST be calculable from metrics |
| NFR-021-57 | Health check MUST report storage availability |
| NFR-021-58 | Health check MUST report database connectivity |
| NFR-021-59 | Alerts MUST fire when storage exceeds threshold |
| NFR-021-60 | Alerts MUST fire when retention fails |

---

## Acceptance Criteria

### Run Record Model (AC-021-01 to AC-021-15)

- [ ] AC-021-01: RunRecord MUST have unique GUID Id property
- [ ] AC-021-02: RunRecord MUST have Command string property
- [ ] AC-021-03: RunRecord MUST have Arguments array property
- [ ] AC-021-04: RunRecord MUST have WorkingDirectory property
- [ ] AC-021-05: RunRecord MUST have Result enum property
- [ ] AC-021-06: RunRecord MUST have ExitCode nullable int property
- [ ] AC-021-07: RunRecord MUST have StartTime DateTimeOffset property
- [ ] AC-021-08: RunRecord MUST have EndTime DateTimeOffset property
- [ ] AC-021-09: RunRecord MUST have Duration TimeSpan property
- [ ] AC-021-10: RunRecord MUST have SessionId string property
- [ ] AC-021-11: RunRecord MUST have TaskId optional property
- [ ] AC-021-12: RunRecord MUST have ArtifactIds list property
- [ ] AC-021-13: RunRecord MUST have Tags dictionary property
- [ ] AC-021-14: RunRecord MUST serialize to JSON
- [ ] AC-021-15: RunRecord MUST be immutable (init-only setters)

### Artifact Model (AC-021-16 to AC-021-30)

- [ ] AC-021-16: Artifact MUST have unique GUID Id property
- [ ] AC-021-17: Artifact MUST have Type enum property
- [ ] AC-021-18: Artifact MUST have RelativePath string property
- [ ] AC-021-19: Artifact MUST have FileName string property
- [ ] AC-021-20: Artifact MUST have Size long property
- [ ] AC-021-21: Artifact MUST have Hash string property
- [ ] AC-021-22: Artifact MUST have MimeType string property
- [ ] AC-021-23: Artifact MUST have CreatedAt DateTimeOffset property
- [ ] AC-021-24: Artifact MUST have RunId GUID property
- [ ] AC-021-25: Artifact MUST have IsTruncated bool property
- [ ] AC-021-26: Artifact MUST have IsCompressed bool property
- [ ] AC-021-27: Artifact MUST have IsSensitive bool property
- [ ] AC-021-28: Artifact content MUST be retrievable as stream
- [ ] AC-021-29: Artifact content MUST be retrievable as string for text
- [ ] AC-021-30: Artifact MUST serialize to JSON

### Run Store (AC-021-31 to AC-021-45)

- [ ] AC-021-31: IRunStore MUST define CreateAsync method
- [ ] AC-021-32: IRunStore MUST define GetByIdAsync method
- [ ] AC-021-33: IRunStore MUST define ListAsync with RunQuery parameter
- [ ] AC-021-34: IRunStore MUST define DeleteAsync method
- [ ] AC-021-35: IRunStore MUST define CountAsync method
- [ ] AC-021-36: CreateAsync MUST return created RunRecord
- [ ] AC-021-37: GetByIdAsync MUST return null for non-existent ID
- [ ] AC-021-38: ListAsync MUST return PagedResult with items and total
- [ ] AC-021-39: DeleteAsync MUST return bool indicating success
- [ ] AC-021-40: Implementation MUST persist to SQLite database
- [ ] AC-021-41: Implementation MUST handle concurrent writes
- [ ] AC-021-42: Implementation MUST validate records before save
- [ ] AC-021-43: Implementation MUST create database tables on first use
- [ ] AC-021-44: Implementation MUST support migrations for schema changes
- [ ] AC-021-45: Implementation MUST dispose resources properly

### Artifact Store (AC-021-46 to AC-021-60)

- [ ] AC-021-46: IArtifactStore MUST define SaveAsync method
- [ ] AC-021-47: IArtifactStore MUST define GetByIdAsync method
- [ ] AC-021-48: IArtifactStore MUST define GetByRunIdAsync method
- [ ] AC-021-49: IArtifactStore MUST define DeleteAsync method
- [ ] AC-021-50: IArtifactStore MUST define GetContentStreamAsync method
- [ ] AC-021-51: SaveAsync MUST compute and store hash
- [ ] AC-021-52: SaveAsync MUST compress text artifacts
- [ ] AC-021-53: SaveAsync MUST truncate oversized artifacts
- [ ] AC-021-54: GetContentStreamAsync MUST decompress on read
- [ ] AC-021-55: GetContentStreamAsync MUST verify hash on read
- [ ] AC-021-56: Implementation MUST store files in artifact directory
- [ ] AC-021-57: Implementation MUST create subdirectories by date
- [ ] AC-021-58: Implementation MUST handle special characters in names
- [ ] AC-021-59: Implementation MUST cleanup orphaned files
- [ ] AC-021-60: Implementation MUST respect size limits

### Artifact Collection (AC-021-61 to AC-021-70)

- [ ] AC-021-61: Collector MUST capture stdout automatically
- [ ] AC-021-62: Collector MUST capture stderr automatically
- [ ] AC-021-63: Collector MUST capture log files matching patterns
- [ ] AC-021-64: Collector MUST capture test result files
- [ ] AC-021-65: Collector MUST respect .gitignore patterns
- [ ] AC-021-66: Collector MUST respect exclusion patterns
- [ ] AC-021-67: Collector MUST limit artifact size per item
- [ ] AC-021-68: Collector MUST limit total artifacts per run
- [ ] AC-021-69: Collector MUST handle file access errors gracefully
- [ ] AC-021-70: Collector MUST run without blocking command execution

### Run Queries (AC-021-71 to AC-021-85)

- [ ] AC-021-71: RunQuery MUST support TimeRange filter
- [ ] AC-021-72: RunQuery MUST support Status filter
- [ ] AC-021-73: RunQuery MUST support ExitCode filter
- [ ] AC-021-74: RunQuery MUST support CommandPattern filter
- [ ] AC-021-75: RunQuery MUST support SessionId filter
- [ ] AC-021-76: RunQuery MUST support TaskId filter
- [ ] AC-021-77: RunQuery MUST support RunType filter
- [ ] AC-021-78: RunQuery MUST support Tags filter
- [ ] AC-021-79: RunQuery MUST support Skip/Take pagination
- [ ] AC-021-80: RunQuery MUST support SortBy field
- [ ] AC-021-81: RunQuery MUST support SortDescending flag
- [ ] AC-021-82: Queries MUST use database indexes
- [ ] AC-021-83: Queries MUST return PagedResult with TotalCount
- [ ] AC-021-84: Queries MUST handle empty results gracefully
- [ ] AC-021-85: Queries MUST timeout after configured duration

### Retention (AC-021-86 to AC-021-95)

- [ ] AC-021-86: Retention MUST support MaxAge configuration
- [ ] AC-021-87: Retention MUST support MaxCount configuration
- [ ] AC-021-88: Retention MUST support MaxSize configuration
- [ ] AC-021-89: Retention MUST exclude tagged runs from deletion
- [ ] AC-021-90: Retention MUST delete artifacts when deleting runs
- [ ] AC-021-91: Retention MUST run on configurable schedule
- [ ] AC-021-92: Retention MUST be triggerable via CLI
- [ ] AC-021-93: Retention MUST log all deletions
- [ ] AC-021-94: Retention MUST support dry-run mode
- [ ] AC-021-95: Retention MUST report cleanup statistics

### CLI Commands (AC-021-96 to AC-021-110)

- [ ] AC-021-96: `acode runs list` MUST list recent runs
- [ ] AC-021-97: `acode runs list --status` MUST filter by status
- [ ] AC-021-98: `acode runs list --since` MUST filter by time
- [ ] AC-021-99: `acode runs list --session` MUST filter by session
- [ ] AC-021-100: `acode runs show <id>` MUST show run details
- [ ] AC-021-101: `acode runs show <id>` MUST list artifacts
- [ ] AC-021-102: `acode runs artifacts <id>` MUST list run artifacts
- [ ] AC-021-103: `acode runs artifact <id>` MUST show artifact content
- [ ] AC-021-104: `acode runs delete <id>` MUST delete run and artifacts
- [ ] AC-021-105: `acode runs cleanup` MUST trigger retention
- [ ] AC-021-106: `acode runs cleanup --dry-run` MUST show what would delete
- [ ] AC-021-107: `acode runs export <id>` MUST create export bundle
- [ ] AC-021-108: `acode runs stats` MUST show run statistics
- [ ] AC-021-109: CLI MUST support --json output format
- [ ] AC-021-110: CLI MUST handle errors gracefully with codes

---

## User Manual Documentation

### Overview

The run inspection system provides complete visibility into all agent operations. Every command executed, build performed, and test run creates a run record with associated artifacts. You can query, inspect, and export these records to understand what the agent did and debug any issues.

### Configuration

Configure run collection in `agent-config.yml`:

```yaml
runs:
  enabled: true
  
  # Artifact collection settings
  artifacts:
    enabled: true
    directory: ".acode/artifacts"
    maxSizePerArtifact: "10MB"
    maxArtifactsPerRun: 50
    compressText: true
    collectPatterns:
      - "*.log"
      - "*.trx"
      - "TestResults/**"
      - "coverage/**"
    excludePatterns:
      - "*.exe"
      - "*.dll"
      - "node_modules/**"
  
  # Retention settings
  retention:
    enabled: true
    maxAge: "30d"        # Delete runs older than 30 days
    maxCount: 1000       # Keep at most 1000 runs
    maxSize: "1GB"       # Total storage limit
    keepFailures: "90d"  # Keep failures longer
    preserveTags:        # Never delete runs with these tags
      - "important"
      - "investigation"
    schedule: "0 2 * * *"  # Run cleanup at 2 AM daily
```

### CLI Commands

#### List Runs

```bash
# List the 20 most recent runs
acode runs list

# List with more results
acode runs list --limit 50

# Filter by status
acode runs list --status failed
acode runs list --status success

# Filter by time
acode runs list --since "1 hour ago"
acode runs list --since "2024-01-15"
acode runs list --until "2024-01-16"

# Filter by session
acode runs list --session abc123

# Filter by command pattern
acode runs list --command "dotnet*"
acode runs list --command "*test*"

# Filter by run type
acode runs list --type build
acode runs list --type test

# Combine filters
acode runs list --status failed --since "1 day ago" --type test

# Output as JSON
acode runs list --json

# Sort by duration
acode runs list --sort duration --desc
```

#### Show Run Details

```bash
# Show detailed information about a run
acode runs show <run-id>

# Example output:
# Run: a1b2c3d4-e5f6-7890-abcd-ef1234567890
# ──────────────────────────────────────────
# Status:    Failed
# Command:   dotnet test
# Arguments: --no-build --logger trx
# Directory: /projects/myapp
# Exit Code: 1
# Started:   2024-01-15 14:30:00 UTC
# Ended:     2024-01-15 14:30:45 UTC
# Duration:  45.2 seconds
# Session:   session-abc123
# Task:      task-456
#
# Artifacts (3):
#   1. stdout.txt (12.5 KB) - Standard output
#   2. stderr.txt (1.2 KB) - Standard error
#   3. TestResults/results.trx (45.0 KB) - Test results

# Show with artifact content preview
acode runs show <run-id> --preview
```

#### View Artifacts

```bash
# List artifacts for a run
acode runs artifacts <run-id>

# View artifact content
acode runs artifact <artifact-id>

# Save artifact to file
acode runs artifact <artifact-id> --output ./saved.log

# View stdout from a run
acode runs stdout <run-id>

# View stderr from a run
acode runs stderr <run-id>
```

#### Delete Runs

```bash
# Delete a specific run and its artifacts
acode runs delete <run-id>

# Delete with confirmation prompt
acode runs delete <run-id> --confirm

# Force delete without confirmation
acode runs delete <run-id> --force
```

#### Cleanup and Retention

```bash
# Manually trigger retention cleanup
acode runs cleanup

# Dry run - show what would be deleted
acode runs cleanup --dry-run

# Force cleanup with custom criteria
acode runs cleanup --older-than "7 days"
acode runs cleanup --keep-last 100

# Show storage statistics
acode runs stats

# Example output:
# Run Statistics
# ──────────────
# Total Runs:      1,234
# Successful:      1,100 (89.1%)
# Failed:          134 (10.9%)
# Total Artifacts: 5,678
# Storage Used:    256.7 MB
# Oldest Run:      2024-01-01 10:00:00
# Latest Run:      2024-01-15 14:30:00
# Avg Duration:    23.4 seconds
```

#### Export and Import

```bash
# Export a run to a bundle file
acode runs export <run-id> --output ./run-bundle.zip

# Export multiple runs
acode runs export --since "1 day ago" --output ./daily-runs.zip

# Export with sensitive content redacted
acode runs export <run-id> --redact --output ./safe-bundle.zip

# Import a run bundle (for review, not execution)
acode runs import ./run-bundle.zip
```

#### Tagging Runs

```bash
# Add a tag to a run
acode runs tag <run-id> important

# Add multiple tags
acode runs tag <run-id> important investigation

# Remove a tag
acode runs untag <run-id> investigation

# List runs by tag
acode runs list --tag important
```

### Programmatic Access

Access run data from code:

```csharp
// Inject IRunStore
public class MyService
{
    private readonly IRunStore _runStore;
    private readonly IArtifactStore _artifactStore;
    
    public MyService(IRunStore runStore, IArtifactStore artifactStore)
    {
        _runStore = runStore;
        _artifactStore = artifactStore;
    }
    
    public async Task AnalyzeRecentFailuresAsync()
    {
        // Query failed runs from last 24 hours
        var query = new RunQuery
        {
            Status = RunStatus.Failed,
            Since = DateTimeOffset.UtcNow.AddDays(-1),
            SortBy = "StartTime",
            SortDescending = true,
            Take = 100
        };
        
        var result = await _runStore.ListAsync(query);
        
        foreach (var run in result.Items)
        {
            Console.WriteLine($"{run.StartTime}: {run.Command} (exit {run.ExitCode})");
            
            // Get artifacts for this run
            var artifacts = await _artifactStore.GetByRunIdAsync(run.Id);
            foreach (var artifact in artifacts)
            {
                Console.WriteLine($"  - {artifact.FileName} ({artifact.Size} bytes)");
            }
        }
    }
}
```

### Artifact Directory Structure

Artifacts are stored in a structured directory hierarchy:

```
.acode/artifacts/
├── 2024/
│   ├── 01/
│   │   ├── 15/
│   │   │   ├── a1b2c3d4-e5f6-7890-abcd-ef1234567890/
│   │   │   │   ├── stdout.txt.gz
│   │   │   │   ├── stderr.txt.gz
│   │   │   │   ├── TestResults/
│   │   │   │   │   └── results.trx.gz
│   │   │   │   └── manifest.json
│   │   │   └── [other run directories]
│   │   └── 16/
│   │       └── [...]
│   └── 02/
│       └── [...]
└── manifest.db  # SQLite database with metadata
```

### Troubleshooting

#### "Artifact not found" Error

**Cause:** The artifact file was deleted but the database record remains.

**Solution:**
```bash
# Run cleanup to sync database with filesystem
acode runs cleanup --orphans
```

#### "Storage limit exceeded" Warning

**Cause:** Total artifact storage has reached the configured limit.

**Solution:**
```bash
# Check current usage
acode runs stats

# Manually cleanup old runs
acode runs cleanup --older-than "7 days"

# Or increase limit in configuration
# runs.retention.maxSize: "2GB"
```

#### Slow Queries

**Cause:** Database indexes may need rebuilding after many deletions.

**Solution:**
```bash
# Optimize the database
acode runs optimize
```

#### Missing stdout/stderr

**Cause:** Artifact collection may have failed or been disabled.

**Solution:**
1. Check `runs.artifacts.enabled` is `true` in config
2. Check artifact size limits aren't too small
3. Verify filesystem permissions on artifact directory

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/Runs/
├── RunRecord.cs
├── IRunStore.cs

src/AgenticCoder.Infrastructure/Runs/
├── RunStore.cs
├── RunRepository.cs
```

### RunRecord Model

```csharp
public record RunRecord
{
    public required Guid Id { get; init; }
    public required string Command { get; init; }
    public required int ExitCode { get; init; }
    public required bool Success { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public required string SessionId { get; init; }
    public required string? TaskId { get; init; }
    public IReadOnlyList<Guid> ArtifactIds { get; init; } = [];
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-RUN-001 | Run not found |
| ACODE-RUN-002 | Artifact not found |

---

**End of Task 021 Specification**