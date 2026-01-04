# Task 043: Output Summarization Pipeline

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 11 – Optimization  
**Dependencies:** Task 040 (Event Log), Task 012 (Logging)  

---

## Description

Task 043 implements the output summarization pipeline—a system that transforms raw, verbose tool output into concise, actionable summaries. As the agent performs operations, it generates substantial output: compiler errors, test results, file diffs, command outputs. Without summarization, users drown in noise and critical information gets lost.

The summarization pipeline sits between output generation and presentation. It receives raw output, applies summarization strategies, and produces both a summary and a reference to the full log. Summaries highlight what matters: failures, warnings, key results. Full logs are preserved for forensic investigation.

### Business Value

Raw output is overwhelming:
- Compiler may emit 1000 errors for one missing semicolon
- Test output includes passing tests nobody cares about
- Build output is mostly noise

Summarization provides:
- Time savings
- Error visibility
- Pattern recognition
- Actionable insights

### Scope Boundaries

This task covers the core summarization framework and pipeline. Failure-specific summarization is Task 043.a. Log attachment is Task 043.b. Size limits are Task 043.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Tool Executor | Output source | Raw → Pipeline | Input |
| Event Log | Task 040 | Store summary | Persistence |
| Logging | Task 012 | Log summaries | Audit |
| CLI | Presentation | Show summary | Output |
| LLM Context | Context window | Summary text | Efficiency |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Summarization fails | Try-catch | Show raw | Verbose output |
| Truncation loses info | Size check | Adjust limits | May miss detail |
| Strategy not found | Lookup | Default strategy | Generic summary |
| Memory pressure | OOM | Stream | Slower |
| Regex timeout | Timer | Skip pattern | Partial summary |

### Assumptions

1. **Output is text**: Can be parsed
2. **Patterns exist**: Summarizable
3. **Full logs stored**: Forensics available
4. **Summary sufficient**: For most cases
5. **Strategies extensible**: Pluggable

### Security Considerations

1. **No secrets in summaries**: Redact
2. **Summaries may be shown**: Safe content
3. **Full logs protected**: Access control
4. **Logging safe**: No sensitive data

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Summary | Condensed output |
| Raw Output | Original tool output |
| Strategy | Summarization algorithm |
| Pipeline | Processing chain |
| Truncation | Cutting to size |
| Highlight | Emphasized item |
| Full Log | Complete output |
| Reference | Link to full log |
| Pattern | Recognizable structure |
| Extraction | Pull key info |

---

## Out of Scope

- AI-based summarization
- Natural language generation
- Multi-language summarization
- Real-time streaming summary
- Interactive summary exploration
- Graphical summary visualization

---

## Functional Requirements

### FR-001 to FR-015: Pipeline Core

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043-01 | Pipeline MUST receive raw output | P0 |
| FR-043-02 | Pipeline MUST produce summary | P0 |
| FR-043-03 | Pipeline MUST preserve full log | P0 |
| FR-043-04 | Pipeline MUST return reference | P0 |
| FR-043-05 | Pipeline MUST select strategy | P0 |
| FR-043-06 | Strategy MUST be by output type | P0 |
| FR-043-07 | Default strategy MUST exist | P0 |
| FR-043-08 | Custom strategies MUST be pluggable | P1 |
| FR-043-09 | Pipeline MUST be configurable | P0 |
| FR-043-10 | Pipeline MUST be async | P0 |
| FR-043-11 | Pipeline MUST support streaming | P1 |
| FR-043-12 | Pipeline MUST handle empty input | P0 |
| FR-043-13 | Pipeline MUST handle null | P0 |
| FR-043-14 | Pipeline MUST not throw | P0 |
| FR-043-15 | Pipeline MUST log operations | P0 |

### FR-016 to FR-035: Strategy Framework

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043-16 | Strategy MUST be interface | P0 |
| FR-043-17 | Strategy MUST have type identifier | P0 |
| FR-043-18 | Strategy MUST support priority | P0 |
| FR-043-19 | Multiple strategies MUST chain | P1 |
| FR-043-20 | Strategy MUST receive context | P0 |
| FR-043-21 | Context MUST include output type | P0 |
| FR-043-22 | Context MUST include source | P0 |
| FR-043-23 | Context MUST include size | P0 |
| FR-043-24 | Strategy MUST return SummaryResult | P0 |
| FR-043-25 | Result MUST include summary text | P0 |
| FR-043-26 | Result MUST include highlight count | P0 |
| FR-043-27 | Result MUST include truncated flag | P0 |
| FR-043-28 | Result MUST include full log ref | P0 |
| FR-043-29 | Strategy registry MUST exist | P0 |
| FR-043-30 | Registry MUST auto-discover | P1 |
| FR-043-31 | Registry MUST order by priority | P0 |
| FR-043-32 | Strategy timeout MUST be enforced | P0 |
| FR-043-33 | Default timeout MUST be 5s | P0 |
| FR-043-34 | Timeout MUST be configurable | P0 |
| FR-043-35 | Timeout MUST use fallback | P0 |

### FR-036 to FR-055: Built-in Strategies

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043-36 | Compiler output strategy MUST exist | P0 |
| FR-043-37 | Test output strategy MUST exist | P0 |
| FR-043-38 | Build output strategy MUST exist | P0 |
| FR-043-39 | Git output strategy MUST exist | P0 |
| FR-043-40 | File diff strategy MUST exist | P0 |
| FR-043-41 | Command output strategy MUST exist | P0 |
| FR-043-42 | Default strategy MUST exist | P0 |
| FR-043-43 | Compiler strategy MUST extract errors | P0 |
| FR-043-44 | Compiler strategy MUST count errors | P0 |
| FR-043-45 | Compiler strategy MUST group by file | P1 |
| FR-043-46 | Test strategy MUST show failures | P0 |
| FR-043-47 | Test strategy MUST show pass/fail count | P0 |
| FR-043-48 | Test strategy MUST list failed tests | P0 |
| FR-043-49 | Build strategy MUST show result | P0 |
| FR-043-50 | Build strategy MUST show duration | P1 |
| FR-043-51 | Git strategy MUST show changed files | P0 |
| FR-043-52 | Git strategy MUST show add/del counts | P0 |
| FR-043-53 | Diff strategy MUST show hunks | P0 |
| FR-043-54 | Diff strategy MUST show context | P0 |
| FR-043-55 | Default strategy MUST truncate | P0 |

### FR-056 to FR-070: Summary Format

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043-56 | Summary MUST be text | P0 |
| FR-043-57 | Summary MUST be readable | P0 |
| FR-043-58 | Summary MUST be structured | P0 |
| FR-043-59 | Summary MUST include header | P0 |
| FR-043-60 | Header MUST show type | P0 |
| FR-043-61 | Header MUST show counts | P0 |
| FR-043-62 | Summary MUST include highlights | P0 |
| FR-043-63 | Highlights MUST be ordered | P0 |
| FR-043-64 | Error > Warning > Info order | P0 |
| FR-043-65 | Summary MUST include footer | P1 |
| FR-043-66 | Footer MUST show truncation | P0 |
| FR-043-67 | Footer MUST show log reference | P0 |
| FR-043-68 | Summary MUST be valid markdown | P1 |
| FR-043-69 | Summary MUST use consistent format | P0 |
| FR-043-70 | Summary MUST be copy-pasteable | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-043-01 | Summarization time | <100ms typical | P0 |
| NFR-043-02 | Large output | <1s for 1MB | P0 |
| NFR-043-03 | Memory usage | <2x input size | P0 |
| NFR-043-04 | Strategy lookup | <1ms | P0 |
| NFR-043-05 | Pipeline overhead | <10ms | P0 |
| NFR-043-06 | Regex matching | <500ms | P0 |
| NFR-043-07 | Streaming latency | <50ms/chunk | P1 |
| NFR-043-08 | Concurrent summaries | 10+ | P1 |
| NFR-043-09 | Cache hit ratio | >80% patterns | P2 |
| NFR-043-10 | Throughput | 100/s | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-043-11 | Never throw | 100% | P0 |
| NFR-043-12 | Always produce output | 100% | P0 |
| NFR-043-13 | Full log preserved | 100% | P0 |
| NFR-043-14 | Strategy fallback | Always | P0 |
| NFR-043-15 | Timeout recovery | 100% | P0 |
| NFR-043-16 | Cross-platform | All OS | P0 |
| NFR-043-17 | Unicode support | UTF-8 | P0 |
| NFR-043-18 | Binary handling | Skip/escape | P0 |
| NFR-043-19 | Long line handling | Wrap/truncate | P0 |
| NFR-043-20 | Encoding detection | Auto | P1 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-043-21 | Summary created logged | Info | P0 |
| NFR-043-22 | Strategy used logged | Debug | P0 |
| NFR-043-23 | Duration logged | Debug | P0 |
| NFR-043-24 | Truncation logged | Info | P0 |
| NFR-043-25 | Metrics: count | Counter | P1 |
| NFR-043-26 | Metrics: duration | Histogram | P1 |
| NFR-043-27 | Metrics: by type | Counter | P2 |
| NFR-043-28 | Structured logging | JSON | P0 |
| NFR-043-29 | Trace ID propagation | Always | P1 |
| NFR-043-30 | Error rate tracked | Counter | P1 |

---

## Acceptance Criteria / Definition of Done

### Pipeline
- [ ] AC-001: Pipeline receives output
- [ ] AC-002: Pipeline produces summary
- [ ] AC-003: Full log preserved
- [ ] AC-004: Reference returned
- [ ] AC-005: Strategy selected
- [ ] AC-006: Default works
- [ ] AC-007: Async works
- [ ] AC-008: Empty handled

### Strategies
- [ ] AC-009: Interface defined
- [ ] AC-010: Registry works
- [ ] AC-011: Compiler strategy
- [ ] AC-012: Test strategy
- [ ] AC-013: Build strategy
- [ ] AC-014: Git strategy
- [ ] AC-015: Diff strategy
- [ ] AC-016: Default strategy

### Format
- [ ] AC-017: Summary readable
- [ ] AC-018: Header present
- [ ] AC-019: Highlights ordered
- [ ] AC-020: Footer present
- [ ] AC-021: Truncation shown
- [ ] AC-022: Log ref shown
- [ ] AC-023: Markdown valid
- [ ] AC-024: Consistent

### Quality
- [ ] AC-025: <100ms typical
- [ ] AC-026: Never throws
- [ ] AC-027: Cross-platform
- [ ] AC-028: Unicode works
- [ ] AC-029: Logging works
- [ ] AC-030: Tests pass
- [ ] AC-031: Documented
- [ ] AC-032: Reviewed

---

## User Verification Scenarios

### Scenario 1: Compiler Summary
**Persona:** Developer with build errors  
**Preconditions:** Code has errors  
**Steps:**
1. Build fails
2. Summary generated
3. Errors highlighted
4. Count shown

**Verification Checklist:**
- [ ] Summary concise
- [ ] Errors visible
- [ ] Count accurate
- [ ] Full log available

### Scenario 2: Test Summary
**Persona:** Developer running tests  
**Preconditions:** Some tests fail  
**Steps:**
1. Run tests
2. Summary generated
3. Failures listed
4. Pass/fail shown

**Verification Checklist:**
- [ ] Failed tests listed
- [ ] Count shown
- [ ] Details sufficient
- [ ] Full log available

### Scenario 3: Large Output
**Persona:** Developer with verbose build  
**Preconditions:** Large output generated  
**Steps:**
1. Verbose output
2. Summary generated
3. Truncation applied
4. Key info preserved

**Verification Checklist:**
- [ ] Truncation works
- [ ] Key info present
- [ ] Size reasonable
- [ ] Full log ref

### Scenario 4: Unknown Type
**Persona:** Developer with custom tool  
**Preconditions:** Unknown output type  
**Steps:**
1. Tool runs
2. Default strategy
3. Summary generated
4. Basic truncation

**Verification Checklist:**
- [ ] Default works
- [ ] Output shown
- [ ] Truncated sensibly
- [ ] No error

### Scenario 5: Git Summary
**Persona:** Developer with changes  
**Preconditions:** Git status/diff  
**Steps:**
1. Git command
2. Summary generated
3. Changes shown
4. Counts visible

**Verification Checklist:**
- [ ] Files listed
- [ ] Counts shown
- [ ] Format clear
- [ ] Actionable

### Scenario 6: Empty Output
**Persona:** Developer with silent command  
**Preconditions:** Command succeeds silently  
**Steps:**
1. Command runs
2. Empty output
3. Summary handles
4. Status shown

**Verification Checklist:**
- [ ] No error
- [ ] Status clear
- [ ] Empty handled
- [ ] Graceful

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-043-01 | Pipeline processes output | FR-043-01 |
| UT-043-02 | Strategy selection | FR-043-05 |
| UT-043-03 | Default strategy | FR-043-07 |
| UT-043-04 | Compiler strategy | FR-043-36 |
| UT-043-05 | Test strategy | FR-043-37 |
| UT-043-06 | Build strategy | FR-043-38 |
| UT-043-07 | Git strategy | FR-043-39 |
| UT-043-08 | Diff strategy | FR-043-40 |
| UT-043-09 | Summary format | FR-043-56 |
| UT-043-10 | Header format | FR-043-59 |
| UT-043-11 | Empty input | FR-043-12 |
| UT-043-12 | Null input | FR-043-13 |
| UT-043-13 | Timeout handling | FR-043-32 |
| UT-043-14 | Full log preservation | FR-043-03 |
| UT-043-15 | Reference generation | FR-043-04 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-043-01 | Pipeline E2E | FR-043-01 |
| IT-043-02 | Event log integration | Task 040 |
| IT-043-03 | CLI integration | Presentation |
| IT-043-04 | Strategy registry | FR-043-29 |
| IT-043-05 | Multiple strategies | FR-043-19 |
| IT-043-06 | Large output | NFR-043-02 |
| IT-043-07 | Concurrent summaries | NFR-043-08 |
| IT-043-08 | Cross-platform | NFR-043-16 |
| IT-043-09 | Unicode output | NFR-043-17 |
| IT-043-10 | Logging | NFR-043-21 |
| IT-043-11 | Performance | NFR-043-01 |
| IT-043-12 | Error recovery | NFR-043-11 |
| IT-043-13 | Streaming | FR-043-11 |
| IT-043-14 | Binary handling | NFR-043-18 |
| IT-043-15 | Encoding | NFR-043-20 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Summary/
│       ├── SummaryResult.cs
│       ├── SummaryContext.cs
│       └── OutputType.cs
├── Acode.Application/
│   └── Summary/
│       ├── ISummarizer.cs
│       ├── ISummaryStrategy.cs
│       ├── ISummaryPipeline.cs
│       └── SummaryOptions.cs
├── Acode.Infrastructure/
│   └── Summary/
│       ├── SummaryPipeline.cs
│       ├── StrategyRegistry.cs
│       └── Strategies/
│           ├── CompilerStrategy.cs
│           ├── TestStrategy.cs
│           ├── BuildStrategy.cs
│           ├── GitStrategy.cs
│           ├── DiffStrategy.cs
│           └── DefaultStrategy.cs
```

### Core Interface

```csharp
public interface ISummaryPipeline
{
    Task<SummaryResult> SummarizeAsync(
        string rawOutput,
        SummaryContext context,
        SummaryOptions? options = null,
        CancellationToken ct = default);
}

public interface ISummaryStrategy
{
    string TypeIdentifier { get; }
    int Priority { get; }
    bool CanHandle(SummaryContext context);
    Task<SummaryResult> SummarizeAsync(
        string rawOutput,
        SummaryContext context,
        CancellationToken ct);
}
```

**End of Task 043 Specification**
