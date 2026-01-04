# Task 046: Benchmark Task Suite

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 045 (Performance Harness), Task 030 (CLI), Task 025 (Tool Executor)  

---

## Description

Task 046 establishes the benchmark task suite—a collection of standardized coding tasks that measure Acode's capabilities. Unlike synthetic microbenchmarks (Task 045.a), these are real-world scenarios: file operations, code generation, refactoring, debugging, and multi-step workflows. The suite serves as the objective measure of "does Acode work?"

The benchmark suite consists of: (1) task specifications that define input, expected output, and evaluation criteria, (2) a runner that executes tasks and captures results, and (3) structured output that feeds into scoring gates. This is the foundation for regression testing and continuous quality assurance.

### Business Value

Benchmark tasks provide:
- Objective capability measurement
- Regression detection
- Model comparison
- Configuration validation
- Release confidence

### Scope Boundaries

This task establishes the core benchmark framework. Task spec format is 046.a. Runner CLI is 046.b. JSON results are 046.c. Scoring is Task 047.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Performance Harness | Task 045 | Execution | Runtime |
| Tool Executor | Task 025 | Tool calls | Validation |
| CLI Framework | Task 030 | Commands | Runner |
| Local LLM | Task 024 | Responses | Target |
| Scoring | Task 047 | Results | Downstream |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Spec parse error | Schema validation | Reject spec | Cannot run |
| Task timeout | Timer | Abort, record | Partial result |
| Tool failure | Exception | Record error | Task failure |
| Environment missing | Pre-check | Abort early | Cannot run |
| LLM unavailable | Health check | Error result | Task failure |

### Assumptions

1. **Tasks are deterministic**: Given same input, expected outcome is predictable
2. **Sandbox available**: Tasks execute in isolation
3. **LLM running**: Local model is available
4. **Schema defined**: Spec format is stable
5. **Categories defined**: Task taxonomy exists

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Benchmark Task | Standardized coding scenario |
| Task Spec | Declarative task definition |
| Runner | Task execution engine |
| Expected Output | Success criteria |
| Actual Output | Task result |
| Evaluation | Comparing actual vs expected |
| Category | Task classification |
| Suite | Collection of tasks |
| Pass | Task met expectations |
| Fail | Task did not meet expectations |
| Timeout | Task exceeded time limit |
| Skip | Task intentionally not run |

---

## Out of Scope

- Scoring and gates (Task 047)
- Baseline management (Task 048)
- Performance optimization
- Task generation
- Dynamic task creation
- Multi-model comparison in single run

---

## Functional Requirements

### FR-001 to FR-015: Suite Definition

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046-01 | Suite MUST be defined | P0 |
| FR-046-02 | Suite MUST have ID | P0 |
| FR-046-03 | Suite MUST have version | P0 |
| FR-046-04 | Suite MUST have name | P0 |
| FR-046-05 | Suite MUST have description | P1 |
| FR-046-06 | Suite MUST contain tasks | P0 |
| FR-046-07 | Tasks MUST be ordered | P0 |
| FR-046-08 | Suite MUST be file-based | P0 |
| FR-046-09 | Suite format MUST be JSON | P0 |
| FR-046-10 | Suite MUST be validated | P0 |
| FR-046-11 | Invalid suite MUST be rejected | P0 |
| FR-046-12 | Multiple suites MUST work | P0 |
| FR-046-13 | Suite selection MUST work | P0 |
| FR-046-14 | Default suite MUST exist | P0 |
| FR-046-15 | Custom suites MUST work | P1 |

### FR-016 to FR-035: Task Specification

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046-16 | Task MUST have ID | P0 |
| FR-046-17 | Task ID MUST be unique | P0 |
| FR-046-18 | Task MUST have name | P0 |
| FR-046-19 | Task MUST have category | P0 |
| FR-046-20 | Categories MUST include file-ops | P0 |
| FR-046-21 | Categories MUST include code-gen | P0 |
| FR-046-22 | Categories MUST include refactor | P0 |
| FR-046-23 | Categories MUST include debug | P0 |
| FR-046-24 | Categories MUST include multi-step | P0 |
| FR-046-25 | Task MUST have input | P0 |
| FR-046-26 | Input MUST include prompt | P0 |
| FR-046-27 | Input MAY include files | P0 |
| FR-046-28 | Input MAY include context | P1 |
| FR-046-29 | Task MUST have expected | P0 |
| FR-046-30 | Expected MUST define outcome | P0 |
| FR-046-31 | Expected MAY define tool calls | P0 |
| FR-046-32 | Expected MAY define output | P1 |
| FR-046-33 | Task MUST have timeout | P0 |
| FR-046-34 | Default timeout = 60s | P0 |
| FR-046-35 | Timeout MUST be configurable | P0 |

### FR-036 to FR-050: Execution

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046-36 | Runner MUST execute tasks | P0 |
| FR-046-37 | Single task execution MUST work | P0 |
| FR-046-38 | Batch execution MUST work | P0 |
| FR-046-39 | Sequential execution MUST work | P0 |
| FR-046-40 | Parallel execution MAY work | P2 |
| FR-046-41 | Execution MUST be isolated | P0 |
| FR-046-42 | Sandbox MUST be used | P0 |
| FR-046-43 | Cleanup MUST occur | P0 |
| FR-046-44 | Timeout MUST be enforced | P0 |
| FR-046-45 | Timeout MUST abort execution | P0 |
| FR-046-46 | Abort MUST be graceful | P0 |
| FR-046-47 | Error MUST be captured | P0 |
| FR-046-48 | Exception MUST not crash runner | P0 |
| FR-046-49 | Progress MUST be reported | P0 |
| FR-046-50 | Cancellation MUST be supported | P0 |

### FR-051 to FR-065: Evaluation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046-51 | Actual MUST be captured | P0 |
| FR-046-52 | Actual vs expected MUST compare | P0 |
| FR-046-53 | Outcome match MUST check | P0 |
| FR-046-54 | Tool calls MUST compare | P0 |
| FR-046-55 | Semantic comparison MUST work | P0 |
| FR-046-56 | Output MUST compare (if defined) | P0 |
| FR-046-57 | Pass/fail MUST be determined | P0 |
| FR-046-58 | Pass = all criteria met | P0 |
| FR-046-59 | Fail = any criteria unmet | P0 |
| FR-046-60 | Partial credit MAY apply | P2 |
| FR-046-61 | Reason MUST be captured | P0 |
| FR-046-62 | Details MUST be available | P0 |
| FR-046-63 | Runtime MUST be captured | P0 |
| FR-046-64 | Iterations MUST be captured | P0 |
| FR-046-65 | Token usage MUST be captured | P0 |

### FR-066 to FR-075: Results

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046-66 | Result MUST be generated | P0 |
| FR-046-67 | Result MUST include task ID | P0 |
| FR-046-68 | Result MUST include status | P0 |
| FR-046-69 | Result MUST include runtime | P0 |
| FR-046-70 | Result MUST include details | P0 |
| FR-046-71 | Batch result MUST exist | P0 |
| FR-046-72 | Batch MUST include summary | P0 |
| FR-046-73 | Summary MUST include pass count | P0 |
| FR-046-74 | Summary MUST include fail count | P0 |
| FR-046-75 | Summary MUST include pass rate | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046-01 | Suite load time | <500ms | P0 |
| NFR-046-02 | Task parse time | <10ms | P0 |
| NFR-046-03 | Setup per task | <1s | P0 |
| NFR-046-04 | Cleanup per task | <1s | P0 |
| NFR-046-05 | Overhead per task | <5% | P0 |
| NFR-046-06 | Memory per task | <100MB | P0 |
| NFR-046-07 | Full suite | <1 hour | P0 |
| NFR-046-08 | Progress update | <100ms | P0 |
| NFR-046-09 | Result write | <50ms | P0 |
| NFR-046-10 | Cancel response | <1s | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046-11 | Isolation | 100% | P0 |
| NFR-046-12 | Cleanup success | 99.9% | P0 |
| NFR-046-13 | Crash recovery | Full | P0 |
| NFR-046-14 | Partial results | Saved | P0 |
| NFR-046-15 | Cross-platform | All OS | P0 |
| NFR-046-16 | Reproducibility | Deterministic | P0 |
| NFR-046-17 | Timeout enforcement | 100% | P0 |
| NFR-046-18 | Error capture | 100% | P0 |
| NFR-046-19 | Schema validation | 100% | P0 |
| NFR-046-20 | Suite integrity | Verified | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046-21 | Suite start logged | Info | P0 |
| NFR-046-22 | Task start logged | Debug | P0 |
| NFR-046-23 | Task result logged | Info | P0 |
| NFR-046-24 | Suite result logged | Info | P0 |
| NFR-046-25 | Errors logged | Error | P0 |
| NFR-046-26 | Metrics: task count | Counter | P0 |
| NFR-046-27 | Metrics: pass rate | Gauge | P0 |
| NFR-046-28 | Metrics: runtime | Histogram | P0 |
| NFR-046-29 | Structured logging | JSON | P0 |
| NFR-046-30 | Trace ID | Per run | P1 |

---

## Acceptance Criteria / Definition of Done

### Suite Definition
- [ ] AC-001: Suite defined
- [ ] AC-002: Suite versioned
- [ ] AC-003: Suite file-based
- [ ] AC-004: JSON format
- [ ] AC-005: Validation works
- [ ] AC-006: Multiple suites
- [ ] AC-007: Default exists
- [ ] AC-008: Custom works

### Task Specification
- [ ] AC-009: Task ID unique
- [ ] AC-010: Task has category
- [ ] AC-011: Categories defined
- [ ] AC-012: Input has prompt
- [ ] AC-013: Expected defined
- [ ] AC-014: Timeout set
- [ ] AC-015: Schema valid
- [ ] AC-016: 50+ tasks

### Execution
- [ ] AC-017: Runner works
- [ ] AC-018: Single task
- [ ] AC-019: Batch works
- [ ] AC-020: Isolated
- [ ] AC-021: Timeout enforced
- [ ] AC-022: Error captured
- [ ] AC-023: Cancellation
- [ ] AC-024: Progress reported

### Evaluation
- [ ] AC-025: Actual captured
- [ ] AC-026: Comparison works
- [ ] AC-027: Pass/fail determined
- [ ] AC-028: Reason captured
- [ ] AC-029: Runtime captured
- [ ] AC-030: Tokens captured
- [ ] AC-031: Cross-platform
- [ ] AC-032: Tests pass

---

## User Verification Scenarios

### Scenario 1: Run Full Suite
**Persona:** Developer  
**Preconditions:** Suite exists, LLM running  
**Steps:**
1. Run benchmark suite
2. All tasks execute
3. Results captured
4. Summary shown

**Verification Checklist:**
- [ ] Suite loads
- [ ] All tasks run
- [ ] Results captured
- [ ] Summary accurate

### Scenario 2: Run Single Task
**Persona:** Developer  
**Preconditions:** Task exists  
**Steps:**
1. Select single task
2. Execute task
3. Review result
4. Check details

**Verification Checklist:**
- [ ] Task runs
- [ ] Result shown
- [ ] Details available
- [ ] Pass/fail clear

### Scenario 3: Task Timeout
**Persona:** Developer  
**Preconditions:** Slow task  
**Steps:**
1. Run task with timeout
2. Timeout reached
3. Task aborted
4. Result = timeout

**Verification Checklist:**
- [ ] Timeout enforced
- [ ] Task aborted
- [ ] Result captured
- [ ] Reason clear

### Scenario 4: Task Failure
**Persona:** Developer  
**Preconditions:** Failing task  
**Steps:**
1. Run task
2. Task fails
3. Review failure
4. Investigate

**Verification Checklist:**
- [ ] Failure detected
- [ ] Reason captured
- [ ] Details available
- [ ] Actionable

### Scenario 5: Cancel Execution
**Persona:** Developer  
**Preconditions:** Suite running  
**Steps:**
1. Start suite
2. Cancel mid-way
3. Partial results saved
4. Cleanup complete

**Verification Checklist:**
- [ ] Cancel works
- [ ] Partial saved
- [ ] Cleanup done
- [ ] No corruption

### Scenario 6: Custom Suite
**Persona:** Developer  
**Preconditions:** Custom tasks  
**Steps:**
1. Create custom suite
2. Add tasks
3. Run suite
4. Results captured

**Verification Checklist:**
- [ ] Custom loads
- [ ] Tasks valid
- [ ] Suite runs
- [ ] Results correct

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-046-01 | Suite loading | FR-046-08 |
| UT-046-02 | Suite validation | FR-046-10 |
| UT-046-03 | Task parsing | FR-046-16 |
| UT-046-04 | Category validation | FR-046-19 |
| UT-046-05 | Input extraction | FR-046-25 |
| UT-046-06 | Expected parsing | FR-046-29 |
| UT-046-07 | Timeout default | FR-046-34 |
| UT-046-08 | Result creation | FR-046-66 |
| UT-046-09 | Pass determination | FR-046-58 |
| UT-046-10 | Fail determination | FR-046-59 |
| UT-046-11 | Summary calculation | FR-046-72 |
| UT-046-12 | Pass rate | FR-046-75 |
| UT-046-13 | Unique ID check | FR-046-17 |
| UT-046-14 | Schema validation | NFR-046-19 |
| UT-046-15 | Error handling | FR-046-47 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-046-01 | Full suite run | E2E |
| IT-046-02 | Single task run | FR-046-37 |
| IT-046-03 | Batch execution | FR-046-38 |
| IT-046-04 | Timeout enforcement | FR-046-44 |
| IT-046-05 | Sandbox isolation | FR-046-42 |
| IT-046-06 | Error recovery | FR-046-47 |
| IT-046-07 | Cancellation | FR-046-50 |
| IT-046-08 | Cross-platform | NFR-046-15 |
| IT-046-09 | Partial results | NFR-046-14 |
| IT-046-10 | Custom suite | FR-046-15 |
| IT-046-11 | Tool integration | Task 025 |
| IT-046-12 | LLM integration | Task 024 |
| IT-046-13 | Logging | NFR-046-21 |
| IT-046-14 | Cleanup | FR-046-43 |
| IT-046-15 | Progress | FR-046-49 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Evaluation/
│       ├── BenchmarkSuite.cs
│       ├── BenchmarkTask.cs
│       ├── TaskInput.cs
│       ├── TaskExpected.cs
│       ├── TaskCategory.cs
│       ├── EvaluationResult.cs
│       └── EvaluationStatus.cs
├── Acode.Application/
│   └── Evaluation/
│       ├── IEvaluationRunner.cs
│       ├── ISuiteLoader.cs
│       ├── ITaskEvaluator.cs
│       └── EvaluationOptions.cs
├── Acode.Infrastructure/
│   └── Evaluation/
│       ├── EvaluationRunner.cs
│       ├── JsonSuiteLoader.cs
│       ├── TaskEvaluator.cs
│       └── TaskSandbox.cs
├── data/
│   └── benchmarks/
│       ├── default-suite.json
│       └── tasks/
│           ├── file-ops/
│           ├── code-gen/
│           ├── refactor/
│           ├── debug/
│           └── multi-step/
```

### Suite Format

```json
{
  "id": "default-suite-v1",
  "version": "1.0.0",
  "name": "Default Benchmark Suite",
  "description": "Standard Acode capability evaluation",
  "tasks": [
    {
      "id": "BENCH-001",
      "name": "Read file contents",
      "category": "file-ops",
      "input": {
        "prompt": "Read the contents of README.md and summarize it",
        "files": {
          "README.md": "# My Project\n\nThis is a sample project."
        }
      },
      "expected": {
        "outcome": "success",
        "toolCalls": ["read_file"]
      },
      "timeout": "PT30S"
    }
  ]
}
```

**End of Task 046 Specification**
