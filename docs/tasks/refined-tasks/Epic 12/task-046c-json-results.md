# Task 046.c: JSON Results

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 046 (Benchmark Suite), Task 046.b (Runner)  

---

## Description

Task 046.c defines the JSON results format—the structured output from benchmark runs. Machine-readable results enable: (1) CI pipeline integration, (2) historical tracking, (3) automated scoring (Task 047), (4) comparison reports, and (5) external tooling. The format must be stable, versioned, and complete.

The results JSON includes: run metadata (timestamp, version, environment), summary statistics (pass/fail counts, duration), per-task results (status, runtime, details), and error information (if any). The format supports both human review (when rendered) and machine processing.

### Business Value

JSON results provide:
- CI integration
- Automated processing
- Historical comparison
- Tooling ecosystem
- Reproducibility

### Scope Boundaries

This task covers results format. Runner is Task 046.b. Scoring is Task 047. Reports are Task 047.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Runner | Task 046.b | Produces results | Source |
| Scoring | Task 047 | Consumes results | Downstream |
| Reports | Task 047.c | Renders results | Downstream |
| Storage | File | Persists results | Persistence |
| CI | Pipeline | Reads results | Integration |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Write error | IO exception | Retry, fail | No results |
| Invalid JSON | Validation | Error | Corrupt file |
| Missing fields | Schema check | Error | Incomplete |
| Overflow | Size check | Truncate | Partial data |

### Assumptions

1. **JSON standard**: RFC 8259
2. **UTF-8 encoding**: Universal
3. **Schema versioned**: Forward compatible
4. **Results complete**: All data captured
5. **Files accessible**: Write permissions

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Results | Benchmark output |
| Schema | Structure definition |
| Metadata | Run information |
| Summary | Aggregated statistics |
| Task Result | Per-task outcome |
| Status | Pass/fail/timeout/skip |
| Runtime | Execution duration |
| Details | Additional data |
| Truncation | Size limiting |
| Versioning | Schema evolution |

---

## Out of Scope

- Report generation (Task 047.c)
- Scoring logic (Task 047)
- Visualization
- Database storage
- Cloud upload
- Real-time streaming

---

## Functional Requirements

### FR-001 to FR-020: Run Metadata

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046c-01 | Results MUST be JSON | P0 |
| FR-046c-02 | Schema version MUST exist | P0 |
| FR-046c-03 | Version format: semver | P0 |
| FR-046c-04 | Run ID MUST exist | P0 |
| FR-046c-05 | Run ID MUST be unique | P0 |
| FR-046c-06 | Timestamp MUST exist | P0 |
| FR-046c-07 | Timestamp format: ISO 8601 | P0 |
| FR-046c-08 | Suite ID MUST exist | P0 |
| FR-046c-09 | Suite version MUST exist | P0 |
| FR-046c-10 | Acode version MUST exist | P0 |
| FR-046c-11 | Model info MUST exist | P0 |
| FR-046c-12 | Model name MUST be captured | P0 |
| FR-046c-13 | Model version MUST be captured | P0 |
| FR-046c-14 | Environment MUST be captured | P0 |
| FR-046c-15 | OS MUST be captured | P0 |
| FR-046c-16 | Runtime MUST be captured | P0 |
| FR-046c-17 | Hostname MAY be captured | P2 |
| FR-046c-18 | User MAY be captured | P2 |
| FR-046c-19 | Config MUST be captured | P1 |
| FR-046c-20 | Duration MUST be captured | P0 |

### FR-021 to FR-035: Summary

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046c-21 | Summary MUST exist | P0 |
| FR-046c-22 | Total tasks MUST show | P0 |
| FR-046c-23 | Passed count MUST show | P0 |
| FR-046c-24 | Failed count MUST show | P0 |
| FR-046c-25 | Timeout count MUST show | P0 |
| FR-046c-26 | Skipped count MUST show | P0 |
| FR-046c-27 | Pass rate MUST show | P0 |
| FR-046c-28 | Pass rate as percentage | P0 |
| FR-046c-29 | Total duration MUST show | P0 |
| FR-046c-30 | Duration in milliseconds | P0 |
| FR-046c-31 | By-category breakdown MAY exist | P1 |
| FR-046c-32 | By-difficulty breakdown MAY exist | P2 |
| FR-046c-33 | Slowest tasks MAY list | P2 |
| FR-046c-34 | Failed tasks MUST list | P0 |
| FR-046c-35 | Error count MUST show | P0 |

### FR-036 to FR-055: Task Results

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046c-36 | Results array MUST exist | P0 |
| FR-046c-37 | Each task MUST have entry | P0 |
| FR-046c-38 | Task ID MUST be included | P0 |
| FR-046c-39 | Task name MUST be included | P0 |
| FR-046c-40 | Task category MUST be included | P0 |
| FR-046c-41 | Status MUST be included | P0 |
| FR-046c-42 | Status: passed | P0 |
| FR-046c-43 | Status: failed | P0 |
| FR-046c-44 | Status: timeout | P0 |
| FR-046c-45 | Status: skipped | P0 |
| FR-046c-46 | Status: error | P0 |
| FR-046c-47 | Runtime MUST be included | P0 |
| FR-046c-48 | Runtime in milliseconds | P0 |
| FR-046c-49 | Iterations MUST be included | P0 |
| FR-046c-50 | Token usage MUST be included | P0 |
| FR-046c-51 | Details object MUST exist | P0 |
| FR-046c-52 | Details: tool calls made | P0 |
| FR-046c-53 | Details: expected vs actual | P0 |
| FR-046c-54 | Details: error message | P0 |
| FR-046c-55 | Details: stack trace (if error) | P1 |

### FR-056 to FR-065: Output

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046c-56 | File output MUST work | P0 |
| FR-046c-57 | Stdout output MUST work | P0 |
| FR-046c-58 | Path MUST be configurable | P0 |
| FR-046c-59 | Default path: ./results/ | P0 |
| FR-046c-60 | Filename: run-{timestamp}.json | P0 |
| FR-046c-61 | Overwrite MUST be configurable | P0 |
| FR-046c-62 | Append MUST NOT be supported | P0 |
| FR-046c-63 | Pretty print MUST be option | P1 |
| FR-046c-64 | Minified MUST be default | P0 |
| FR-046c-65 | UTF-8 encoding MUST be used | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046c-01 | Serialization | <100ms | P0 |
| NFR-046c-02 | File write | <200ms | P0 |
| NFR-046c-03 | 100 task results | <50ms | P0 |
| NFR-046c-04 | 1000 task results | <500ms | P0 |
| NFR-046c-05 | Max file size | 10MB | P0 |
| NFR-046c-06 | Memory for large | <50MB | P0 |
| NFR-046c-07 | Streaming for large | Supported | P1 |
| NFR-046c-08 | Parse time | <200ms | P0 |
| NFR-046c-09 | Validation time | <50ms | P0 |
| NFR-046c-10 | Compression ratio | 5:1 | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046c-11 | JSON validity | 100% | P0 |
| NFR-046c-12 | Schema compliance | 100% | P0 |
| NFR-046c-13 | UTF-8 validity | 100% | P0 |
| NFR-046c-14 | Cross-platform | All OS | P0 |
| NFR-046c-15 | Backward compatible | 2 versions | P0 |
| NFR-046c-16 | Forward compatible | Ignored fields | P0 |
| NFR-046c-17 | Partial write | Atomic | P0 |
| NFR-046c-18 | File corruption | Detectable | P0 |
| NFR-046c-19 | Error recovery | Graceful | P0 |
| NFR-046c-20 | Validation errors | Clear | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046c-21 | Write logged | Info | P0 |
| NFR-046c-22 | Path logged | Debug | P0 |
| NFR-046c-23 | Size logged | Debug | P0 |
| NFR-046c-24 | Errors logged | Error | P0 |
| NFR-046c-25 | Schema version | Logged | P0 |
| NFR-046c-26 | Structured logging | JSON | P0 |
| NFR-046c-27 | Metrics: writes | Counter | P1 |
| NFR-046c-28 | Metrics: size | Histogram | P1 |
| NFR-046c-29 | Metrics: errors | Counter | P0 |
| NFR-046c-30 | Trace ID | Included | P1 |

---

## Acceptance Criteria / Definition of Done

### Metadata
- [ ] AC-001: Schema version
- [ ] AC-002: Run ID unique
- [ ] AC-003: Timestamp ISO
- [ ] AC-004: Suite info
- [ ] AC-005: Acode version
- [ ] AC-006: Model info
- [ ] AC-007: Environment
- [ ] AC-008: Duration

### Summary
- [ ] AC-009: Total count
- [ ] AC-010: Pass count
- [ ] AC-011: Fail count
- [ ] AC-012: Pass rate
- [ ] AC-013: Duration
- [ ] AC-014: Failed list
- [ ] AC-015: Error count
- [ ] AC-016: By-category

### Results
- [ ] AC-017: All tasks
- [ ] AC-018: Task ID
- [ ] AC-019: Status enum
- [ ] AC-020: Runtime ms
- [ ] AC-021: Iterations
- [ ] AC-022: Token usage
- [ ] AC-023: Details
- [ ] AC-024: Errors

### Output
- [ ] AC-025: File works
- [ ] AC-026: Stdout works
- [ ] AC-027: Path config
- [ ] AC-028: Pretty print
- [ ] AC-029: UTF-8
- [ ] AC-030: Atomic write
- [ ] AC-031: Tests pass
- [ ] AC-032: Documented

---

## User Verification Scenarios

### Scenario 1: Run and Save
**Persona:** Developer  
**Preconditions:** Suite exists  
**Steps:**
1. Run benchmark
2. Results saved
3. Open file
4. Verify JSON

**Verification Checklist:**
- [ ] File created
- [ ] Valid JSON
- [ ] All fields
- [ ] Correct data

### Scenario 2: CI Integration
**Persona:** CI Pipeline  
**Preconditions:** Benchmark complete  
**Steps:**
1. Read results
2. Parse JSON
3. Extract pass rate
4. Gate decision

**Verification Checklist:**
- [ ] Parseable
- [ ] Pass rate present
- [ ] Correct format
- [ ] Machine readable

### Scenario 3: Historical Compare
**Persona:** Developer  
**Preconditions:** Multiple runs  
**Steps:**
1. Load run A
2. Load run B
3. Compare results
4. Identify changes

**Verification Checklist:**
- [ ] Both load
- [ ] Same schema
- [ ] Comparable
- [ ] Diff works

### Scenario 4: Debug Failure
**Persona:** Developer  
**Preconditions:** Task failed  
**Steps:**
1. Find failed task
2. Read details
3. See error
4. Debug issue

**Verification Checklist:**
- [ ] Task found
- [ ] Details present
- [ ] Error clear
- [ ] Actionable

### Scenario 5: Pretty Print
**Persona:** Developer  
**Preconditions:** Run complete  
**Steps:**
1. Run with --pretty
2. Open file
3. Human readable
4. Review

**Verification Checklist:**
- [ ] Formatted
- [ ] Readable
- [ ] Valid JSON
- [ ] Same data

### Scenario 6: Large Suite
**Persona:** Developer  
**Preconditions:** 500 tasks  
**Steps:**
1. Run large suite
2. Results saved
3. File size OK
4. Load works

**Verification Checklist:**
- [ ] File written
- [ ] Size reasonable
- [ ] Load works
- [ ] All tasks

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-046c-01 | Schema version | FR-046c-02 |
| UT-046c-02 | Run ID generation | FR-046c-04 |
| UT-046c-03 | Timestamp format | FR-046c-07 |
| UT-046c-04 | Summary calculation | FR-046c-21 |
| UT-046c-05 | Pass rate | FR-046c-27 |
| UT-046c-06 | Task status enum | FR-046c-41 |
| UT-046c-07 | Runtime format | FR-046c-48 |
| UT-046c-08 | Token usage | FR-046c-50 |
| UT-046c-09 | Details object | FR-046c-51 |
| UT-046c-10 | JSON validity | NFR-046c-11 |
| UT-046c-11 | Pretty print | FR-046c-63 |
| UT-046c-12 | UTF-8 encoding | FR-046c-65 |
| UT-046c-13 | File path | FR-046c-58 |
| UT-046c-14 | Default path | FR-046c-59 |
| UT-046c-15 | Filename format | FR-046c-60 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-046c-01 | Full run output | E2E |
| IT-046c-02 | File write | FR-046c-56 |
| IT-046c-03 | Stdout output | FR-046c-57 |
| IT-046c-04 | Large suite | NFR-046c-04 |
| IT-046c-05 | Cross-platform | NFR-046c-14 |
| IT-046c-06 | Schema validation | NFR-046c-12 |
| IT-046c-07 | Parse round-trip | NFR-046c-08 |
| IT-046c-08 | Backward compat | NFR-046c-15 |
| IT-046c-09 | Atomic write | NFR-046c-17 |
| IT-046c-10 | Logging | NFR-046c-21 |
| IT-046c-11 | Error details | FR-046c-54 |
| IT-046c-12 | Model info | FR-046c-11 |
| IT-046c-13 | Environment | FR-046c-14 |
| IT-046c-14 | Category breakdown | FR-046c-31 |
| IT-046c-15 | Failed list | FR-046c-34 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Evaluation/
│       ├── EvaluationResults.cs
│       ├── ResultMetadata.cs
│       ├── ResultSummary.cs
│       └── TaskResult.cs
├── Acode.Application/
│   └── Evaluation/
│       ├── IResultsWriter.cs
│       └── ResultsOptions.cs
├── Acode.Infrastructure/
│   └── Evaluation/
│       ├── JsonResultsWriter.cs
│       └── ResultsSerializer.cs
├── data/
│   └── schemas/
│       └── results-v1.schema.json
```

### JSON Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["schemaVersion", "runId", "timestamp", "suite", "summary", "results"],
  "properties": {
    "schemaVersion": { "type": "string", "pattern": "^\\d+\\.\\d+\\.\\d+$" },
    "runId": { "type": "string", "format": "uuid" },
    "timestamp": { "type": "string", "format": "date-time" },
    "suite": {
      "type": "object",
      "required": ["id", "version"],
      "properties": {
        "id": { "type": "string" },
        "version": { "type": "string" }
      }
    },
    "acode": {
      "type": "object",
      "properties": {
        "version": { "type": "string" }
      }
    },
    "model": {
      "type": "object",
      "properties": {
        "name": { "type": "string" },
        "version": { "type": "string" }
      }
    },
    "environment": {
      "type": "object",
      "properties": {
        "os": { "type": "string" },
        "runtime": { "type": "string" }
      }
    },
    "summary": {
      "type": "object",
      "required": ["total", "passed", "failed", "passRate", "durationMs"],
      "properties": {
        "total": { "type": "integer" },
        "passed": { "type": "integer" },
        "failed": { "type": "integer" },
        "timeout": { "type": "integer" },
        "skipped": { "type": "integer" },
        "passRate": { "type": "number" },
        "durationMs": { "type": "integer" }
      }
    },
    "results": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["taskId", "name", "status", "runtimeMs"],
        "properties": {
          "taskId": { "type": "string" },
          "name": { "type": "string" },
          "category": { "type": "string" },
          "status": { "enum": ["passed", "failed", "timeout", "skipped", "error"] },
          "runtimeMs": { "type": "integer" },
          "iterations": { "type": "integer" },
          "tokenUsage": { "type": "integer" },
          "details": { "type": "object" }
        }
      }
    }
  }
}
```

### Example Output

```json
{
  "schemaVersion": "1.0.0",
  "runId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-01-16T10:30:00Z",
  "suite": {
    "id": "default-suite-v1",
    "version": "1.0.0"
  },
  "acode": {
    "version": "0.1.0"
  },
  "model": {
    "name": "qwen2.5-coder",
    "version": "7b-instruct"
  },
  "environment": {
    "os": "Windows 11",
    "runtime": ".NET 8.0"
  },
  "summary": {
    "total": 50,
    "passed": 42,
    "failed": 5,
    "timeout": 2,
    "skipped": 1,
    "passRate": 84.0,
    "durationMs": 125000
  },
  "results": [
    {
      "taskId": "BENCH-001",
      "name": "Read file contents",
      "category": "file-ops",
      "status": "passed",
      "runtimeMs": 1250,
      "iterations": 1,
      "tokenUsage": 450,
      "details": {
        "toolCalls": ["read_file"],
        "expected": "success",
        "actual": "success"
      }
    }
  ]
}
```

**End of Task 046.c Specification**
