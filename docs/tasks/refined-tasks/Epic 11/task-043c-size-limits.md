# Task 043.c: Size Limits

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 11 – Optimization  
**Dependencies:** Task 043 (Pipeline), Task 043.a (Failures), Task 043.b (Logs)  

---

## Description

Task 043.c implements size limit enforcement—the constraints that prevent summaries from growing unbounded and ensure output remains usable. While summarization reduces output, without explicit limits summaries can still become unwieldy for very large inputs.

Size limits apply at multiple levels: individual summary size, total summary size per operation, and aggregate size across a session. When limits are exceeded, content is intelligently truncated, preserving the most important information (failures, warnings) while cutting less critical content.

### Business Value

Size limits provide:
- Predictable output
- LLM context efficiency
- UI performance
- Storage efficiency
- User experience

### Scope Boundaries

This task covers size enforcement and truncation. Summarization is Task 043. Failure handling is Task 043.a. Log storage is Task 043.b.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Pipeline | Task 043 | Size check | Enforcement |
| Failures | Task 043.a | Priority | Preservation |
| Logs | Task 043.b | Reference | Full log link |
| LLM Context | Window | Budget | Limit |
| Config | Settings | Limits | Configuration |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Over limit | Size check | Truncate | Partial info |
| Under-truncate | Check after | Re-truncate | May exceed |
| Over-truncate | Preserve check | Restore | Sparse |
| Priority conflict | Sort | Highest wins | May miss lower |
| Config invalid | Validate | Default | Warn user |

### Assumptions

1. **Limits needed**: Unbounded bad
2. **Truncation OK**: With full log ref
3. **Priority works**: Important preserved
4. **Config available**: Limits set
5. **Unicode safe**: Character boundaries

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Size Limit | Maximum size |
| Truncation | Cut to limit |
| Priority | Importance level |
| Preservation | Keep important |
| Budget | Size allocation |
| Overflow | Exceeds limit |
| Trim | Remove excess |
| Marker | Truncation indicator |
| Boundary | Cut point |
| Allocation | Size per section |

---

## Out of Scope

- Dynamic limit adjustment
- Machine learning for importance
- User-specified per-item limits
- Real-time limit negotiation
- Semantic-aware truncation
- Context-aware limits

---

## Functional Requirements

### FR-001 to FR-015: Limit Configuration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043c-01 | Summary size limit MUST exist | P0 |
| FR-043c-02 | Default MUST be 10KB | P0 |
| FR-043c-03 | Limit MUST be configurable | P0 |
| FR-043c-04 | Min limit MUST be 1KB | P0 |
| FR-043c-05 | Max limit MUST be 1MB | P0 |
| FR-043c-06 | Per-type limits MUST be supported | P1 |
| FR-043c-07 | Session limit MUST exist | P1 |
| FR-043c-08 | Default session MUST be 100KB | P1 |
| FR-043c-09 | CLI output limit MUST exist | P0 |
| FR-043c-10 | Default CLI MUST be 20KB | P0 |
| FR-043c-11 | LLM context limit MUST exist | P0 |
| FR-043c-12 | Default LLM MUST be 8KB | P0 |
| FR-043c-13 | Config MUST be in YAML | P0 |
| FR-043c-14 | Config validation MUST occur | P0 |
| FR-043c-15 | Invalid MUST use default | P0 |

### FR-016 to FR-035: Truncation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043c-16 | Truncation MUST occur at limit | P0 |
| FR-043c-17 | Truncation MUST be clean | P0 |
| FR-043c-18 | Line boundary MUST be respected | P0 |
| FR-043c-19 | Word boundary MUST be preferred | P1 |
| FR-043c-20 | UTF-8 boundary MUST be safe | P0 |
| FR-043c-21 | Truncation marker MUST be added | P0 |
| FR-043c-22 | Marker MUST show remaining | P0 |
| FR-043c-23 | Marker MUST show total | P0 |
| FR-043c-24 | Marker MUST reference full log | P0 |
| FR-043c-25 | Marker format MUST be configurable | P2 |
| FR-043c-26 | Default marker MUST be clear | P0 |
| FR-043c-27 | Truncation MUST be from end | P0 |
| FR-043c-28 | Middle truncation MUST be option | P2 |
| FR-043c-29 | Head/tail preservation MUST work | P1 |
| FR-043c-30 | Truncation MUST log | P0 |
| FR-043c-31 | Log MUST include size before | P0 |
| FR-043c-32 | Log MUST include size after | P0 |
| FR-043c-33 | Log MUST include type | P0 |
| FR-043c-34 | Truncation MUST be fast | P0 |
| FR-043c-35 | No re-truncation loops | P0 |

### FR-036 to FR-055: Priority Preservation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043c-36 | Priority MUST be respected | P0 |
| FR-043c-37 | Errors MUST be preserved | P0 |
| FR-043c-38 | Warnings MUST be lower priority | P0 |
| FR-043c-39 | Info MUST be lowest | P0 |
| FR-043c-40 | Priority budget MUST exist | P0 |
| FR-043c-41 | Errors MUST get 60% min | P0 |
| FR-043c-42 | Warnings MUST get 30% min | P0 |
| FR-043c-43 | Info MUST get remainder | P0 |
| FR-043c-44 | Budget MUST be configurable | P1 |
| FR-043c-45 | Overflow MUST trim low priority | P0 |
| FR-043c-46 | Root cause MUST never trim | P0 |
| FR-043c-47 | Stack trace MUST have minimum | P0 |
| FR-043c-48 | Default stack min MUST be 5 frames | P0 |
| FR-043c-49 | Header MUST be preserved | P0 |
| FR-043c-50 | Footer MUST be preserved | P0 |
| FR-043c-51 | Counts MUST be accurate | P0 |
| FR-043c-52 | Truncated count MUST show | P0 |
| FR-043c-53 | Preserved MUST be complete | P0 |
| FR-043c-54 | Partial items MUST not exist | P0 |
| FR-043c-55 | Item boundary MUST be respected | P0 |

### FR-056 to FR-065: Multi-Level Limits

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043c-56 | Item limit MUST exist | P0 |
| FR-043c-57 | Section limit MUST exist | P0 |
| FR-043c-58 | Summary limit MUST exist | P0 |
| FR-043c-59 | Session limit MUST exist | P1 |
| FR-043c-60 | Limits MUST cascade | P0 |
| FR-043c-61 | Item < Section < Summary | P0 |
| FR-043c-62 | Default item MUST be 2KB | P0 |
| FR-043c-63 | Default section MUST be 5KB | P0 |
| FR-043c-64 | Aggregate MUST track session | P1 |
| FR-043c-65 | Aggregate MUST warn near limit | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-043c-01 | Truncation time | <10ms | P0 |
| NFR-043c-02 | Large truncation | <50ms | P0 |
| NFR-043c-03 | Size check | <1ms | P0 |
| NFR-043c-04 | Priority sort | <5ms | P0 |
| NFR-043c-05 | Budget calc | <1ms | P0 |
| NFR-043c-06 | Memory overhead | <10% | P0 |
| NFR-043c-07 | No copies | Minimize | P1 |
| NFR-043c-08 | Streaming support | Yes | P1 |
| NFR-043c-09 | Concurrent | Safe | P0 |
| NFR-043c-10 | Throughput | 1000/s | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-043c-11 | Under limit | 100% | P0 |
| NFR-043c-12 | Priority preserved | 100% | P0 |
| NFR-043c-13 | UTF-8 safe | 100% | P0 |
| NFR-043c-14 | No partial items | 100% | P0 |
| NFR-043c-15 | Marker present | Always | P0 |
| NFR-043c-16 | Cross-platform | All OS | P0 |
| NFR-043c-17 | Config valid | Always | P0 |
| NFR-043c-18 | Default fallback | Always | P0 |
| NFR-043c-19 | No data loss | Full log | P0 |
| NFR-043c-20 | Consistent | Same input same output | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-043c-21 | Truncation logged | Info | P0 |
| NFR-043c-22 | Size before/after | Debug | P0 |
| NFR-043c-23 | Priority logged | Debug | P0 |
| NFR-043c-24 | Config logged | Debug | P0 |
| NFR-043c-25 | Metrics: truncations | Counter | P1 |
| NFR-043c-26 | Metrics: bytes trimmed | Counter | P1 |
| NFR-043c-27 | Metrics: by type | Counter | P2 |
| NFR-043c-28 | Structured logging | JSON | P0 |
| NFR-043c-29 | Session aggregate | Tracked | P1 |
| NFR-043c-30 | Alerts optional | Near limit | P2 |

---

## Acceptance Criteria / Definition of Done

### Configuration
- [ ] AC-001: Summary limit exists
- [ ] AC-002: Default 10KB
- [ ] AC-003: Configurable
- [ ] AC-004: Validation works
- [ ] AC-005: Invalid uses default
- [ ] AC-006: Per-type works
- [ ] AC-007: YAML config works
- [ ] AC-008: Multi-level works

### Truncation
- [ ] AC-009: Truncation at limit
- [ ] AC-010: Clean truncation
- [ ] AC-011: Line boundary
- [ ] AC-012: UTF-8 safe
- [ ] AC-013: Marker added
- [ ] AC-014: Marker shows count
- [ ] AC-015: Full log ref
- [ ] AC-016: Logging works

### Priority
- [ ] AC-017: Priority respected
- [ ] AC-018: Errors preserved
- [ ] AC-019: Budget works
- [ ] AC-020: Root cause kept
- [ ] AC-021: Stack minimum
- [ ] AC-022: Header preserved
- [ ] AC-023: Counts accurate
- [ ] AC-024: No partial items

### Quality
- [ ] AC-025: <10ms typical
- [ ] AC-026: Cross-platform
- [ ] AC-027: Consistent
- [ ] AC-028: No data loss
- [ ] AC-029: Tests pass
- [ ] AC-030: Documented
- [ ] AC-031: Config examples
- [ ] AC-032: Reviewed

---

## User Verification Scenarios

### Scenario 1: Large Summary Truncated
**Persona:** Developer with huge output  
**Preconditions:** Very large output  
**Steps:**
1. Generate summary
2. Exceeds limit
3. Truncation applied
4. Marker shown

**Verification Checklist:**
- [ ] Under limit
- [ ] Marker present
- [ ] Count shown
- [ ] Full log ref

### Scenario 2: Errors Preserved
**Persona:** Developer with many errors  
**Preconditions:** Many errors, some warnings  
**Steps:**
1. Generate summary
2. Limit exceeded
3. Errors kept
4. Warnings trimmed

**Verification Checklist:**
- [ ] Errors preserved
- [ ] Warnings trimmed
- [ ] Priority correct
- [ ] Count accurate

### Scenario 3: Root Cause Kept
**Persona:** Developer with cascade  
**Preconditions:** Root cause + cascade  
**Steps:**
1. Generate summary
2. Limit tight
3. Root cause kept
4. Some cascade cut

**Verification Checklist:**
- [ ] Root cause present
- [ ] Cascade trimmed
- [ ] Count shown
- [ ] Actionable

### Scenario 4: Custom Limit
**Persona:** Developer with preference  
**Preconditions:** Custom limit set  
**Steps:**
1. Set 5KB limit
2. Generate summary
3. Limit respected
4. Output correct

**Verification Checklist:**
- [ ] Custom used
- [ ] Under 5KB
- [ ] Truncation if needed
- [ ] Config works

### Scenario 5: UTF-8 Safety
**Persona:** Developer with unicode  
**Preconditions:** Unicode in output  
**Steps:**
1. Unicode output
2. Truncation needed
3. Clean cut
4. No corruption

**Verification Checklist:**
- [ ] No corruption
- [ ] Clean boundary
- [ ] Readable
- [ ] Complete chars

### Scenario 6: Session Aggregate
**Persona:** Developer in long session  
**Preconditions:** Many operations  
**Steps:**
1. Multiple summaries
2. Session accumulates
3. Near limit warned
4. Limits respected

**Verification Checklist:**
- [ ] Aggregate tracked
- [ ] Warning shown
- [ ] Limits work
- [ ] Session managed

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-043c-01 | Default limit | FR-043c-02 |
| UT-043c-02 | Custom limit | FR-043c-03 |
| UT-043c-03 | Truncation | FR-043c-16 |
| UT-043c-04 | Line boundary | FR-043c-18 |
| UT-043c-05 | UTF-8 safety | FR-043c-20 |
| UT-043c-06 | Marker format | FR-043c-21 |
| UT-043c-07 | Priority preservation | FR-043c-36 |
| UT-043c-08 | Error budget | FR-043c-41 |
| UT-043c-09 | Root cause | FR-043c-46 |
| UT-043c-10 | Stack minimum | FR-043c-47 |
| UT-043c-11 | Item limit | FR-043c-56 |
| UT-043c-12 | Section limit | FR-043c-57 |
| UT-043c-13 | Config validation | FR-043c-14 |
| UT-043c-14 | No partial items | FR-043c-54 |
| UT-043c-15 | Accurate counts | FR-043c-51 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-043c-01 | Pipeline integration | Task 043 |
| IT-043c-02 | Failure integration | Task 043.a |
| IT-043c-03 | Log integration | Task 043.b |
| IT-043c-04 | Large output E2E | NFR-043c-02 |
| IT-043c-05 | Priority E2E | FR-043c-36 |
| IT-043c-06 | Config loading | FR-043c-13 |
| IT-043c-07 | Cross-platform | NFR-043c-16 |
| IT-043c-08 | Performance | NFR-043c-01 |
| IT-043c-09 | Concurrent | NFR-043c-09 |
| IT-043c-10 | Session aggregate | FR-043c-59 |
| IT-043c-11 | Logging | NFR-043c-21 |
| IT-043c-12 | Unicode content | NFR-043c-13 |
| IT-043c-13 | Multi-level | FR-043c-60 |
| IT-043c-14 | Type-specific | FR-043c-06 |
| IT-043c-15 | Consistency | NFR-043c-20 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Summary/
│       ├── SizeLimits.cs
│       └── PriorityBudget.cs
├── Acode.Application/
│   └── Summary/
│       ├── ITruncator.cs
│       └── TruncationOptions.cs
├── Acode.Infrastructure/
│   └── Summary/
│       ├── Truncator.cs
│       ├── PriorityTruncator.cs
│       └── SizeLimitConfig.cs
```

### Configuration Schema

```yaml
summarization:
  limits:
    summary: 10240        # 10KB default
    item: 2048            # 2KB per item
    section: 5120         # 5KB per section
    session: 102400       # 100KB per session
    cli: 20480            # 20KB for CLI
    llm: 8192             # 8KB for LLM context
  
  priority_budget:
    errors: 0.6           # 60% for errors
    warnings: 0.3         # 30% for warnings
    info: 0.1             # 10% for info
  
  truncation:
    marker: "... ({remaining} more lines, see full log: {logId})"
    stack_minimum: 5      # Minimum stack frames
    preserve_root: true   # Always keep root cause
```

**End of Task 043.c Specification**
