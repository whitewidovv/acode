# Task 043.a: Summarize Failures

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 11 – Optimization  
**Dependencies:** Task 043 (Summarization Pipeline)  

---

## Description

Task 043.a implements failure-focused summarization—the specialized extraction and highlighting of errors, exceptions, and failures from raw output. While Task 043 provides the general summarization framework, failure summarization requires specific strategies that understand error patterns across different tools and contexts.

When builds fail, tests crash, or commands error, users need immediate visibility into what went wrong. Failure summarization prioritizes error content, extracts stack traces, identifies root causes, and presents failures in a structured, actionable format.

### Business Value

Failure summarization provides:
- Immediate problem visibility
- Faster debugging
- Root cause identification
- Reduced cognitive load
- Actionable error reports

### Scope Boundaries

This task covers failure detection, extraction, and formatting. General summarization is Task 043. Log attachment is Task 043.b. Size limits are Task 043.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Pipeline | Task 043 | Strategy integration | Parent |
| Compiler | Output | Error extraction | Source |
| Test Runner | Output | Failure extraction | Source |
| Stack Trace | Parser | Structure | Analysis |
| Event Log | Task 040 | Failure record | Storage |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Pattern mismatch | No match | Fallback | Generic extract |
| Stack trace corrupt | Parse fail | Raw output | Less structured |
| Multi-error overflow | Count | Limit | Truncated list |
| Nested errors | Depth check | Flatten | May lose context |
| Encoding issues | Decode fail | Best effort | Some garbled |

### Assumptions

1. **Errors have patterns**: Recognizable
2. **Stack traces parseable**: Standard formats
3. **Priority determinable**: Error severity
4. **Grouping possible**: By source
5. **Root cause extractable**: Often first error

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Failure | Error/exception/crash |
| Root Cause | Primary error |
| Cascade | Errors from root cause |
| Stack Trace | Call stack at error |
| Severity | Error importance |
| Extraction | Pull from output |
| Pattern | Error recognition regex |
| Grouping | Organize by source |
| Deduplication | Remove duplicates |
| Highlight | Emphasize error |

---

## Out of Scope

- AI error classification
- Automatic fix suggestions
- Error probability scoring
- Historical error correlation
- External error databases
- Machine learning patterns

---

## Functional Requirements

### FR-001 to FR-015: Failure Detection

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043a-01 | Failure patterns MUST be detected | P0 |
| FR-043a-02 | Error keywords MUST match | P0 |
| FR-043a-03 | Exception patterns MUST match | P0 |
| FR-043a-04 | Stack traces MUST be detected | P0 |
| FR-043a-05 | Exit codes MUST be checked | P0 |
| FR-043a-06 | Non-zero exit MUST flag failure | P0 |
| FR-043a-07 | Pattern registry MUST exist | P0 |
| FR-043a-08 | Custom patterns MUST be addable | P1 |
| FR-043a-09 | Case sensitivity MUST be configurable | P1 |
| FR-043a-10 | Pattern priority MUST be supported | P0 |
| FR-043a-11 | Multi-line patterns MUST work | P0 |
| FR-043a-12 | Context lines MUST be captured | P0 |
| FR-043a-13 | Default context MUST be 3 lines | P0 |
| FR-043a-14 | Context MUST be configurable | P1 |
| FR-043a-15 | Binary content MUST be skipped | P0 |

### FR-016 to FR-035: Error Extraction

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043a-16 | Errors MUST be extracted | P0 |
| FR-043a-17 | Extraction MUST include message | P0 |
| FR-043a-18 | Extraction MUST include location | P0 |
| FR-043a-19 | Location MUST include file | P0 |
| FR-043a-20 | Location MUST include line | P0 |
| FR-043a-21 | Location MUST include column | P1 |
| FR-043a-22 | Extraction MUST include severity | P0 |
| FR-043a-23 | Extraction MUST include code | P1 |
| FR-043a-24 | Stack traces MUST be extracted | P0 |
| FR-043a-25 | Stack MUST include method | P0 |
| FR-043a-26 | Stack MUST include file | P0 |
| FR-043a-27 | Stack MUST include line | P0 |
| FR-043a-28 | Inner exceptions MUST be handled | P0 |
| FR-043a-29 | Aggregate exceptions MUST work | P0 |
| FR-043a-30 | Exception type MUST be extracted | P0 |
| FR-043a-31 | Extraction MUST be language-aware | P0 |
| FR-043a-32 | C# patterns MUST be supported | P0 |
| FR-043a-33 | Python patterns MUST be supported | P0 |
| FR-043a-34 | JavaScript patterns MUST be supported | P0 |
| FR-043a-35 | Generic patterns MUST exist | P0 |

### FR-036 to FR-055: Error Processing

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043a-36 | Errors MUST be deduplicated | P0 |
| FR-043a-37 | Dedup MUST use message hash | P0 |
| FR-043a-38 | Dedup MUST keep first occurrence | P0 |
| FR-043a-39 | Dedup MUST track count | P0 |
| FR-043a-40 | Errors MUST be grouped | P0 |
| FR-043a-41 | Grouping MUST be by file | P0 |
| FR-043a-42 | Grouping MUST be configurable | P1 |
| FR-043a-43 | Errors MUST be sorted | P0 |
| FR-043a-44 | Sort MUST be by severity | P0 |
| FR-043a-45 | Cascade errors MUST be marked | P1 |
| FR-043a-46 | Root cause MUST be identified | P0 |
| FR-043a-47 | Root cause MUST be highlighted | P0 |
| FR-043a-48 | Error limit MUST be enforced | P0 |
| FR-043a-49 | Default limit MUST be 25 | P0 |
| FR-043a-50 | Limit MUST be configurable | P1 |
| FR-043a-51 | Overflow MUST show count | P0 |
| FR-043a-52 | Priority errors MUST not be cut | P0 |
| FR-043a-53 | Warnings MUST be lower priority | P0 |
| FR-043a-54 | Info MUST be lowest priority | P0 |
| FR-043a-55 | Severity hierarchy MUST be clear | P0 |

### FR-056 to FR-065: Failure Format

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043a-56 | Format MUST show error count | P0 |
| FR-043a-57 | Format MUST show warning count | P0 |
| FR-043a-58 | Format MUST list errors | P0 |
| FR-043a-59 | Each error MUST show message | P0 |
| FR-043a-60 | Each error MUST show location | P0 |
| FR-043a-61 | Stack traces MUST be collapsible | P1 |
| FR-043a-62 | Format MUST be markdown | P1 |
| FR-043a-63 | Format MUST be copy-paste ready | P1 |
| FR-043a-64 | Format MUST be consistent | P0 |
| FR-043a-65 | Format MUST show truncation | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-043a-01 | Detection time | <50ms typical | P0 |
| NFR-043a-02 | Large output | <500ms for 1MB | P0 |
| NFR-043a-03 | Pattern matching | <10ms/pattern | P0 |
| NFR-043a-04 | Stack parsing | <20ms/trace | P0 |
| NFR-043a-05 | Grouping | <30ms | P0 |
| NFR-043a-06 | Formatting | <20ms | P0 |
| NFR-043a-07 | Memory usage | <1.5x input | P0 |
| NFR-043a-08 | Concurrent | 10+ parallel | P1 |
| NFR-043a-09 | Pattern cache | Hit >90% | P2 |
| NFR-043a-10 | Throughput | 50/s | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-043a-11 | Never miss fatal | 100% | P0 |
| NFR-043a-12 | Parsing recovery | Always | P0 |
| NFR-043a-13 | Encoding support | UTF-8 | P0 |
| NFR-043a-14 | Cross-platform | All OS | P0 |
| NFR-043a-15 | Pattern timeout | 1s max | P0 |
| NFR-043a-16 | Regex safety | No ReDoS | P0 |
| NFR-043a-17 | Stack overflow | No | P0 |
| NFR-043a-18 | Null handling | Safe | P0 |
| NFR-043a-19 | Empty handling | Safe | P0 |
| NFR-043a-20 | Binary skip | Safe | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-043a-21 | Errors found logged | Info | P0 |
| NFR-043a-22 | Pattern matched logged | Debug | P0 |
| NFR-043a-23 | Root cause logged | Info | P0 |
| NFR-043a-24 | Duration logged | Debug | P0 |
| NFR-043a-25 | Metrics: error count | Counter | P1 |
| NFR-043a-26 | Metrics: by severity | Counter | P2 |
| NFR-043a-27 | Metrics: by type | Counter | P2 |
| NFR-043a-28 | Structured logging | JSON | P0 |
| NFR-043a-29 | Trace propagation | Always | P1 |
| NFR-043a-30 | Alert on fatal | Optional | P2 |

---

## Acceptance Criteria / Definition of Done

### Detection
- [ ] AC-001: Error patterns detected
- [ ] AC-002: Exception patterns detected
- [ ] AC-003: Stack traces detected
- [ ] AC-004: Exit codes checked
- [ ] AC-005: Context captured
- [ ] AC-006: Multi-line works
- [ ] AC-007: Pattern registry works
- [ ] AC-008: Custom patterns work

### Extraction
- [ ] AC-009: Messages extracted
- [ ] AC-010: Locations extracted
- [ ] AC-011: Severity extracted
- [ ] AC-012: Stack traces parsed
- [ ] AC-013: Inner exceptions work
- [ ] AC-014: C# patterns work
- [ ] AC-015: Python patterns work
- [ ] AC-016: JavaScript patterns work

### Processing
- [ ] AC-017: Deduplication works
- [ ] AC-018: Grouping works
- [ ] AC-019: Sorting works
- [ ] AC-020: Root cause identified
- [ ] AC-021: Limits enforced
- [ ] AC-022: Overflow shown
- [ ] AC-023: Priority preserved
- [ ] AC-024: Hierarchy correct

### Format
- [ ] AC-025: Count shown
- [ ] AC-026: Errors listed
- [ ] AC-027: Location shown
- [ ] AC-028: Markdown valid
- [ ] AC-029: Consistent format
- [ ] AC-030: Truncation shown
- [ ] AC-031: Copy-paste ready
- [ ] AC-032: Tests pass

---

## User Verification Scenarios

### Scenario 1: Compiler Errors
**Persona:** Developer with syntax errors  
**Preconditions:** Code has errors  
**Steps:**
1. Build fails
2. Errors detected
3. Grouped by file
4. Root cause shown

**Verification Checklist:**
- [ ] All errors found
- [ ] Location accurate
- [ ] Grouped correctly
- [ ] Actionable

### Scenario 2: Test Failures
**Persona:** Developer with test failures  
**Preconditions:** Tests fail  
**Steps:**
1. Tests run
2. Failures detected
3. Stack traces shown
4. Message clear

**Verification Checklist:**
- [ ] Failures found
- [ ] Stack readable
- [ ] Message helpful
- [ ] Location shown

### Scenario 3: Cascading Errors
**Persona:** Developer with cascade  
**Preconditions:** Root error causes cascade  
**Steps:**
1. Build fails
2. Many errors
3. Root identified
4. Cascade marked

**Verification Checklist:**
- [ ] Root cause first
- [ ] Cascade identified
- [ ] Not overwhelming
- [ ] Actionable

### Scenario 4: Exception Stack
**Persona:** Developer with exception  
**Preconditions:** Runtime exception  
**Steps:**
1. Exception thrown
2. Stack extracted
3. Inner extracted
4. Type shown

**Verification Checklist:**
- [ ] Stack parsed
- [ ] Inner shown
- [ ] Type clear
- [ ] Location accurate

### Scenario 5: Multi-Language
**Persona:** Developer with multi-language project  
**Preconditions:** Errors from multiple languages  
**Steps:**
1. Build runs
2. C# errors
3. Python errors
4. All detected

**Verification Checklist:**
- [ ] C# works
- [ ] Python works
- [ ] All found
- [ ] Correctly parsed

### Scenario 6: Error Limit
**Persona:** Developer with many errors  
**Preconditions:** 100+ errors  
**Steps:**
1. Build fails badly
2. Limit applied
3. Count shown
4. Priority kept

**Verification Checklist:**
- [ ] Limited output
- [ ] Count shown
- [ ] Important kept
- [ ] Overflow noted

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-043a-01 | Error keyword detection | FR-043a-02 |
| UT-043a-02 | Exception pattern | FR-043a-03 |
| UT-043a-03 | Stack trace detection | FR-043a-04 |
| UT-043a-04 | Message extraction | FR-043a-17 |
| UT-043a-05 | Location extraction | FR-043a-18 |
| UT-043a-06 | C# stack parsing | FR-043a-32 |
| UT-043a-07 | Python stack parsing | FR-043a-33 |
| UT-043a-08 | JavaScript stack parsing | FR-043a-34 |
| UT-043a-09 | Deduplication | FR-043a-36 |
| UT-043a-10 | Grouping | FR-043a-40 |
| UT-043a-11 | Sorting | FR-043a-43 |
| UT-043a-12 | Root cause | FR-043a-46 |
| UT-043a-13 | Limit enforcement | FR-043a-48 |
| UT-043a-14 | Inner exceptions | FR-043a-28 |
| UT-043a-15 | Format output | FR-043a-56 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-043a-01 | Pipeline integration | Task 043 |
| IT-043a-02 | Real compiler output | E2E |
| IT-043a-03 | Real test output | E2E |
| IT-043a-04 | Large error set | NFR-043a-02 |
| IT-043a-05 | Pattern registry | FR-043a-07 |
| IT-043a-06 | Custom patterns | FR-043a-08 |
| IT-043a-07 | Cross-platform | NFR-043a-14 |
| IT-043a-08 | Unicode errors | NFR-043a-13 |
| IT-043a-09 | Performance | NFR-043a-01 |
| IT-043a-10 | Logging | NFR-043a-21 |
| IT-043a-11 | Concurrent | NFR-043a-08 |
| IT-043a-12 | Binary skip | NFR-043a-20 |
| IT-043a-13 | Regex safety | NFR-043a-16 |
| IT-043a-14 | Timeout | NFR-043a-15 |
| IT-043a-15 | Recovery | NFR-043a-12 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Summary/
│       ├── ExtractedError.cs
│       ├── ErrorSeverity.cs
│       └── StackFrame.cs
├── Acode.Application/
│   └── Summary/
│       ├── IFailureExtractor.cs
│       ├── IStackTraceParser.cs
│       └── IErrorPattern.cs
├── Acode.Infrastructure/
│   └── Summary/
│       ├── FailureExtractor.cs
│       ├── PatternRegistry.cs
│       └── Patterns/
│           ├── CSharpPatterns.cs
│           ├── PythonPatterns.cs
│           ├── JavaScriptPatterns.cs
│           └── GenericPatterns.cs
```

### Error Patterns

```csharp
public static class CSharpPatterns
{
    // CS1002: ; expected
    public static readonly Regex CompilerError = new(
        @"^(?<file>.+?)\((?<line>\d+),(?<col>\d+)\):\s*(?<severity>error|warning)\s+(?<code>\w+):\s*(?<message>.+)$",
        RegexOptions.Compiled | RegexOptions.Multiline);
    
    // at Namespace.Class.Method(Args) in File:line N
    public static readonly Regex StackFrame = new(
        @"^\s*at\s+(?<method>.+?)\s+in\s+(?<file>.+?):line\s+(?<line>\d+)",
        RegexOptions.Compiled | RegexOptions.Multiline);
}
```

**End of Task 043.a Specification**
