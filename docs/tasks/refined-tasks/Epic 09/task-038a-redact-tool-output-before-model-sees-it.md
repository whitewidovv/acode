# Task 038.a: Redact Tool Output Before Model Sees It

**Priority:** P0 – Critical  
**Tier:** L – Feature Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 9 – Safety & Compliance  
**Dependencies:** Task 038, Task 020, Task 050  

---

## Description

Task 038.a implements the tool output redaction pipeline. All output from tool executions MUST be scanned and redacted BEFORE being passed to the LLM context. This prevents secrets from entering the model's reasoning context.

The redaction pipeline sits between tool execution and context assembly. When a tool returns output (file contents, command output, API responses), the pipeline intercepts, scans, redacts, and only then passes the sanitized content forward. The model never sees raw secrets.

This is the primary defense against accidental secret exposure to LLM providers. Even in local-only mode, redaction is mandatory because the model's reasoning (which may contain the secret) could be logged, exported, or displayed.

### DB-Aware Redaction

**Update (DB-aware redaction):** Redaction/secret scanning MUST apply to DB-derived snapshots included in exports, and MUST occur before remote sync writes.

**Update (Workspace DB Foundation):** This task MUST use the Workspace DB abstraction introduced in **Task 050**.

### Business Value

Pre-model redaction provides:
- Secrets never enter LLM context
- Prevents exposure in model reasoning
- Protects against prompt logging
- Enables safe audit export
- Defense in depth layer

### Scope Boundaries

This task covers the tool output → model context pipeline redaction. Core engine is 038. Commit blocking is 038.b. Pattern config is 038.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Tool Executor | Task 020 | Output capture | Intercept all |
| Redaction Engine | Task 038 | Scan + redact | Core logic |
| Context Builder | `IContextBuilder` | Sanitized input | After redact |
| Model Adapter | Task 044 | Clean context | No secrets |
| Workspace DB | Task 050 | Query results | Scan all |
| Audit | Task 039 | Log redactions | Per output |
| CLI Display | Task 000 | Safe output | User sees |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Redaction engine error | Exception | Block output | Tool fails |
| Timeout during scan | Watchdog | Block output | Tool fails |
| Memory overflow | Monitor | Stream mode | Slower |
| Missing pattern | Corpus fail | Add pattern | Risk until fixed |
| Double redaction | Marker detect | Skip | No issue |
| Context too large | Size check | Truncate | Partial |
| Encoding issue | Decode error | Block | Tool fails |
| Performance impact | Timer | Log warning | Slower |

### Assumptions

1. **All tool output intercepted**: No bypass
2. **Synchronous by default**: Block until scan complete
3. **Async for large content**: Streaming mode
4. **Markers are safe**: Models can see markers
5. **Performance acceptable**: <100ms typical
6. **Error = block**: Never pass raw on error
7. **Audit all redactions**: Full trail
8. **Integration with 038**: Uses core engine

### Security Considerations

1. **No bypass path**: All tools go through pipeline
2. **Fail closed**: Error = block, not pass
3. **No caching raw**: Never cache unredacted
4. **Clear after scan**: Memory hygiene
5. **Audit required**: Log all interceptions
6. **No reconstruction**: Markers don't enable recovery
7. **Defense in depth**: Even if other layers fail
8. **Test coverage**: All tool types tested

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Pre-Model Redaction | Scan before LLM sees |
| Tool Output Pipeline | Execution → Context path |
| Interception Point | Where output is captured |
| Sanitized Content | After redaction |
| Context Builder | Assembles LLM input |
| Fail Closed | Error = block output |
| Marker Passthrough | Model sees markers |
| Memory Hygiene | Clear sensitive data |

---

## Out of Scope

- Redacting model responses
- Real-time streaming redaction
- Custom tool-specific redaction
- Secret recovery from markers
- Undo redaction

---

## Functional Requirements

### FR-001 to FR-015: Pipeline Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038A-01 | All tool outputs MUST be intercepted | P0 |
| FR-038A-02 | Interception MUST be before context | P0 |
| FR-038A-03 | No bypass path MUST exist | P0 |
| FR-038A-04 | Pipeline MUST be configurable | P2 |
| FR-038A-05 | Pipeline MUST be extensible | P2 |
| FR-038A-06 | Synchronous mode MUST work | P0 |
| FR-038A-07 | Async mode MUST work for large | P1 |
| FR-038A-08 | Streaming MUST be supported | P1 |
| FR-038A-09 | Batching MUST be supported | P2 |
| FR-038A-10 | Timeout MUST be configurable | P1 |
| FR-038A-11 | Timeout MUST block output | P0 |
| FR-038A-12 | Memory limit MUST be enforced | P1 |
| FR-038A-13 | Memory exceeded MUST stream | P1 |
| FR-038A-14 | Error MUST block output | P0 |
| FR-038A-15 | Error MUST be logged | P0 |

### FR-016 to FR-030: Tool Type Coverage

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038A-16 | File read output MUST be scanned | P0 |
| FR-038A-17 | Command execution MUST be scanned | P0 |
| FR-038A-18 | Git output MUST be scanned | P0 |
| FR-038A-19 | Search results MUST be scanned | P0 |
| FR-038A-20 | Network responses MUST be scanned | P0 |
| FR-038A-21 | DB query results MUST be scanned | P0 |
| FR-038A-22 | Environment output MUST be scanned | P0 |
| FR-038A-23 | Error messages MUST be scanned | P0 |
| FR-038A-24 | Stack traces MUST be scanned | P0 |
| FR-038A-25 | Log output MUST be scanned | P0 |
| FR-038A-26 | Config file reads MUST be scanned | P0 |
| FR-038A-27 | Template output MUST be scanned | P1 |
| FR-038A-28 | API responses MUST be scanned | P0 |
| FR-038A-29 | Test output MUST be scanned | P1 |
| FR-038A-30 | Custom tool output MUST be scanned | P0 |

### FR-031 to FR-045: Context Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038A-31 | Sanitized content MUST pass to context | P0 |
| FR-038A-32 | Markers MUST be model-readable | P0 |
| FR-038A-33 | Context MUST be valid after redaction | P0 |
| FR-038A-34 | Context size MUST be recalculated | P1 |
| FR-038A-35 | Truncation MUST preserve markers | P1 |
| FR-038A-36 | Metadata MUST indicate redaction | P0 |
| FR-038A-37 | Redaction count MUST be tracked | P1 |
| FR-038A-38 | Original size MUST be tracked | P2 |
| FR-038A-39 | Sanitized size MUST be tracked | P2 |
| FR-038A-40 | Context events MUST be published | P1 |
| FR-038A-41 | Model MUST see marker explanation | P1 |
| FR-038A-42 | Marker explanation MUST be configurable | P2 |
| FR-038A-43 | Multiple tools MUST all scan | P0 |
| FR-038A-44 | Parallel tools MUST all scan | P0 |
| FR-038A-45 | Context MUST NOT contain raw secrets | P0 |

### FR-046 to FR-060: Audit Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038A-46 | Every interception MUST be logged | P0 |
| FR-038A-47 | Log MUST include tool ID | P0 |
| FR-038A-48 | Log MUST include execution ID | P0 |
| FR-038A-49 | Log MUST include redaction count | P0 |
| FR-038A-50 | Log MUST include secret types | P0 |
| FR-038A-51 | Log MUST include locations | P1 |
| FR-038A-52 | Log MUST NOT include secret values | P0 |
| FR-038A-53 | Log MUST include scan duration | P1 |
| FR-038A-54 | Structured log format | P0 |
| FR-038A-55 | Events: tool intercepted | P1 |
| FR-038A-56 | Events: redaction applied | P1 |
| FR-038A-57 | Metrics: tool type counts | P2 |
| FR-038A-58 | Metrics: total redactions | P2 |
| FR-038A-59 | Audit export MUST include | P1 |
| FR-038A-60 | Correlation with tool execution | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-038A-01 | Small output scan | <50ms | P1 |
| NFR-038A-02 | Medium output scan | <100ms | P1 |
| NFR-038A-03 | Large output scan | <500ms | P2 |
| NFR-038A-04 | Pipeline overhead | <10% | P1 |
| NFR-038A-05 | Streaming throughput | 10MB/s | P2 |
| NFR-038A-06 | Memory per scan | <50MB | P1 |
| NFR-038A-07 | Concurrent scans | 10+ | P2 |
| NFR-038A-08 | Context rebuild | <20ms | P1 |
| NFR-038A-09 | Batched tools | Parallel | P1 |
| NFR-038A-10 | Audit write | <10ms | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-038A-11 | All tools intercepted | 100% | P0 |
| NFR-038A-12 | No bypass | 100% | P0 |
| NFR-038A-13 | Fail closed | 100% | P0 |
| NFR-038A-14 | Detection rate | >99.9% | P0 |
| NFR-038A-15 | Context validity | 100% | P0 |
| NFR-038A-16 | Thread safety | No races | P0 |
| NFR-038A-17 | Graceful degradation | On error | P0 |
| NFR-038A-18 | Memory bounded | Always | P1 |
| NFR-038A-19 | Encoding support | UTF-8 | P0 |
| NFR-038A-20 | Deterministic | Same output | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-038A-21 | Pipeline logged | Info level | P0 |
| NFR-038A-22 | Redaction logged | Info level | P0 |
| NFR-038A-23 | Error logged | Error level | P0 |
| NFR-038A-24 | Timeout logged | Warning level | P0 |
| NFR-038A-25 | Metrics collected | Prometheus | P2 |
| NFR-038A-26 | Events published | P1 |
| NFR-038A-27 | Structured logging | JSON | P0 |
| NFR-038A-28 | Correlation ID | Required | P0 |
| NFR-038A-29 | Tracing support | OpenTelemetry | P2 |
| NFR-038A-30 | Dashboard data | Exported | P2 |

---

## Acceptance Criteria / Definition of Done

### Pipeline
- [ ] AC-001: All tools intercepted
- [ ] AC-002: Interception before context
- [ ] AC-003: No bypass path
- [ ] AC-004: Synchronous works
- [ ] AC-005: Async works
- [ ] AC-006: Streaming works
- [ ] AC-007: Timeout blocks
- [ ] AC-008: Error blocks

### Tool Coverage
- [ ] AC-009: File read scanned
- [ ] AC-010: Command output scanned
- [ ] AC-011: Git output scanned
- [ ] AC-012: Search results scanned
- [ ] AC-013: DB results scanned
- [ ] AC-014: Environment scanned
- [ ] AC-015: Errors scanned
- [ ] AC-016: Custom tools scanned

### Context
- [ ] AC-017: Sanitized passes
- [ ] AC-018: Markers readable
- [ ] AC-019: Context valid
- [ ] AC-020: Size recalculated
- [ ] AC-021: Metadata indicates
- [ ] AC-022: No raw secrets
- [ ] AC-023: Parallel tools
- [ ] AC-024: All tools scan

### Audit
- [ ] AC-025: Interception logged
- [ ] AC-026: Tool ID included
- [ ] AC-027: Execution ID included
- [ ] AC-028: Count included
- [ ] AC-029: Types included
- [ ] AC-030: Values never
- [ ] AC-031: Structured format
- [ ] AC-032: Events published

---

## User Verification Scenarios

### Scenario 1: File Read with Secret
**Persona:** Developer reading config  
**Preconditions:** Config has API key  
**Steps:**
1. Request file read
2. Tool executes
3. Output intercepted
4. Model sees marker

**Verification Checklist:**
- [ ] File read
- [ ] Secret detected
- [ ] Marker in context
- [ ] Logged to audit

### Scenario 2: Command Output with Token
**Persona:** Developer running env  
**Preconditions:** Env has token  
**Steps:**
1. Run env command
2. Output captured
3. Token redacted
4. Safe in context

**Verification Checklist:**
- [ ] Command ran
- [ ] Token found
- [ ] Redacted
- [ ] Context clean

### Scenario 3: DB Query with Credentials
**Persona:** Developer querying data  
**Preconditions:** DB has creds  
**Steps:**
1. Query workspace DB
2. Results returned
3. Credentials scanned
4. Redacted before context

**Verification Checklist:**
- [ ] Query executed
- [ ] Creds found
- [ ] Markers placed
- [ ] Context safe

### Scenario 4: Pipeline Error
**Persona:** Developer with bad data  
**Preconditions:** Malformed output  
**Steps:**
1. Tool returns bad data
2. Scan fails
3. Output blocked
4. Error logged

**Verification Checklist:**
- [ ] Error occurs
- [ ] Output blocked
- [ ] No raw passed
- [ ] Logged

### Scenario 5: Large Output Streaming
**Persona:** Developer with big file  
**Preconditions:** 10MB file  
**Steps:**
1. Read large file
2. Streaming scan
3. Incremental redaction
4. Context built

**Verification Checklist:**
- [ ] Streaming active
- [ ] No memory blow
- [ ] Complete scan
- [ ] Context valid

### Scenario 6: Parallel Tools
**Persona:** Agent with multiple tools  
**Preconditions:** 3 tools parallel  
**Steps:**
1. Execute 3 tools
2. All intercepted
3. All scanned
4. All safe in context

**Verification Checklist:**
- [ ] All intercepted
- [ ] All scanned
- [ ] No races
- [ ] All logged

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-038A-01 | Interception works | FR-038A-01 |
| UT-038A-02 | Before context | FR-038A-02 |
| UT-038A-03 | No bypass | FR-038A-03 |
| UT-038A-04 | Sync mode | FR-038A-06 |
| UT-038A-05 | Async mode | FR-038A-07 |
| UT-038A-06 | Timeout blocks | FR-038A-11 |
| UT-038A-07 | Error blocks | FR-038A-14 |
| UT-038A-08 | File read scan | FR-038A-16 |
| UT-038A-09 | Command scan | FR-038A-17 |
| UT-038A-10 | Context valid | FR-038A-33 |
| UT-038A-11 | Markers readable | FR-038A-32 |
| UT-038A-12 | No raw secrets | FR-038A-45 |
| UT-038A-13 | Audit logging | FR-038A-46 |
| UT-038A-14 | Memory limit | FR-038A-12 |
| UT-038A-15 | Thread safety | NFR-038A-16 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-038A-01 | Full pipeline | E2E |
| IT-038A-02 | All tool types | FR-038A-16-30 |
| IT-038A-03 | Context integration | FR-038A-31 |
| IT-038A-04 | Parallel tools | FR-038A-44 |
| IT-038A-05 | Streaming mode | FR-038A-08 |
| IT-038A-06 | DB query path | FR-038A-21 |
| IT-038A-07 | Error handling | FR-038A-14 |
| IT-038A-08 | Timeout handling | FR-038A-11 |
| IT-038A-09 | Audit export | FR-038A-59 |
| IT-038A-10 | Performance | NFR-038A-01 |
| IT-038A-11 | Fail closed | NFR-038A-13 |
| IT-038A-12 | Detection rate | NFR-038A-14 |
| IT-038A-13 | Correlation | FR-038A-60 |
| IT-038A-14 | Events | FR-038A-55 |
| IT-038A-15 | Task 050 integration | DB |

**End of Task 038.a Specification**
