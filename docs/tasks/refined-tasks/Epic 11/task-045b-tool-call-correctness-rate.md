# Task 045.b: Tool-Call Correctness Rate

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 11 – Optimization  
**Dependencies:** Task 045 (Harness), Task 025 (Tool Executor)  

---

## Description

Task 045.b implements tool-call correctness measurement—the benchmark that evaluates how accurately the local LLM generates valid tool calls. Speed means nothing if the model produces incorrect or malformed tool calls. Correctness rate is a critical quality metric.

The correctness benchmark runs a standardized set of prompts that should elicit specific tool calls. It compares the model's output against expected results, measuring: (1) whether a tool call was made when expected, (2) whether the correct tool was chosen, (3) whether arguments were valid, and (4) whether the semantic intent was preserved.

### Business Value

Correctness measurement provides:
- Quality assurance
- Model comparison (accuracy, not just speed)
- Regression detection
- Configuration validation
- User confidence

### Scope Boundaries

This task covers tool-call correctness. Core harness is Task 045. Microbenchmarks are Task 045.a. Report comparisons are Task 045.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Harness | Task 045 | Benchmark integration | Parent |
| Tool Executor | Task 025 | Tool validation | Validation |
| Local LLM | Task 024 | Response source | Target |
| Test Cases | File | Expected results | Reference |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| No tool call | Parse check | Score 0 | Accuracy drop |
| Wrong tool | Compare | Score partial | Accuracy drop |
| Invalid args | Validate | Score partial | Accuracy drop |
| Timeout | Timer | Skip case | Incomplete |
| Parse error | Try-catch | Score 0 | Data point |

### Assumptions

1. **Expected results known**: Test cases
2. **Tool schema available**: For validation
3. **Determinism possible**: For reproducibility
4. **Scoring defined**: Clear criteria
5. **Coverage sufficient**: Representative

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Correctness | Accurate tool call |
| Tool Call | Function invocation |
| Arguments | Tool parameters |
| Schema | Tool definition |
| Expected | Known correct result |
| Score | Correctness measure |
| Accuracy | Correct / Total |
| Precision | Correct / Attempted |
| Recall | Correct / Expected |
| F1 | Harmonic mean |

---

## Out of Scope

- Semantic correctness evaluation
- Multi-turn evaluation
- Subjective quality assessment
- Human evaluation
- A/B testing
- Real-world task evaluation

---

## Functional Requirements

### FR-001 to FR-020: Test Case Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045b-01 | Test cases MUST be defined | P0 |
| FR-045b-02 | Test cases MUST have prompt | P0 |
| FR-045b-03 | Test cases MUST have expected | P0 |
| FR-045b-04 | Expected MUST include tool name | P0 |
| FR-045b-05 | Expected MUST include args | P0 |
| FR-045b-06 | Expected MAY include alternatives | P1 |
| FR-045b-07 | Test cases MUST be categorized | P0 |
| FR-045b-08 | Categories MUST include simple | P0 |
| FR-045b-09 | Categories MUST include complex | P0 |
| FR-045b-10 | Categories MUST include edge | P0 |
| FR-045b-11 | Test cases MUST be versioned | P0 |
| FR-045b-12 | Test cases MUST be file-based | P0 |
| FR-045b-13 | Format MUST be JSON | P0 |
| FR-045b-14 | Custom test cases MUST work | P1 |
| FR-045b-15 | Test case count MUST be 50+ | P0 |
| FR-045b-16 | Coverage MUST span all tools | P0 |
| FR-045b-17 | Difficulty MUST be rated | P1 |
| FR-045b-18 | Tags MUST be supported | P1 |
| FR-045b-19 | Filter by tag MUST work | P1 |
| FR-045b-20 | Subset selection MUST work | P0 |

### FR-021 to FR-040: Correctness Evaluation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045b-21 | Tool call MUST be extracted | P0 |
| FR-045b-22 | Extraction MUST handle formats | P0 |
| FR-045b-23 | JSON format MUST work | P0 |
| FR-045b-24 | XML format MUST work | P1 |
| FR-045b-25 | Function call format MUST work | P0 |
| FR-045b-26 | Tool name MUST be compared | P0 |
| FR-045b-27 | Name comparison MUST be exact | P0 |
| FR-045b-28 | Args MUST be compared | P0 |
| FR-045b-29 | Arg names MUST match | P0 |
| FR-045b-30 | Arg values MUST match | P0 |
| FR-045b-31 | Value match MUST be semantic | P0 |
| FR-045b-32 | Equivalent values MUST match | P0 |
| FR-045b-33 | Extra args MUST be allowed | P1 |
| FR-045b-34 | Missing required MUST fail | P0 |
| FR-045b-35 | Type coercion MUST work | P1 |
| FR-045b-36 | No tool call MUST be detectable | P0 |
| FR-045b-37 | Multiple calls MUST be handled | P0 |
| FR-045b-38 | Order MUST be configurable | P1 |
| FR-045b-39 | Partial credit MUST be option | P0 |
| FR-045b-40 | Scoring rubric MUST be defined | P0 |

### FR-041 to FR-055: Scoring

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045b-41 | Score MUST be calculated | P0 |
| FR-045b-42 | Binary score MUST be option | P0 |
| FR-045b-43 | Partial score MUST be option | P0 |
| FR-045b-44 | Correct = 1.0 | P0 |
| FR-045b-45 | Wrong tool = 0.0 | P0 |
| FR-045b-46 | Right tool, wrong args = 0.5 | P0 |
| FR-045b-47 | No call when expected = 0.0 | P0 |
| FR-045b-48 | Call when not expected = 0.0 | P0 |
| FR-045b-49 | Accuracy MUST be calculated | P0 |
| FR-045b-50 | Accuracy = correct / total | P0 |
| FR-045b-51 | Per-category accuracy MUST exist | P0 |
| FR-045b-52 | Per-tool accuracy MUST exist | P0 |
| FR-045b-53 | Confidence interval MUST exist | P0 |
| FR-045b-54 | Breakdown MUST be available | P0 |
| FR-045b-55 | Failures MUST be listed | P0 |

### FR-056 to FR-065: Reporting

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045b-56 | Report MUST be generated | P0 |
| FR-045b-57 | Overall accuracy MUST show | P0 |
| FR-045b-58 | By-category MUST show | P0 |
| FR-045b-59 | By-tool MUST show | P0 |
| FR-045b-60 | Failures MUST show | P0 |
| FR-045b-61 | Failure MUST include prompt | P0 |
| FR-045b-62 | Failure MUST include expected | P0 |
| FR-045b-63 | Failure MUST include actual | P0 |
| FR-045b-64 | Comparison to baseline MUST work | P0 |
| FR-045b-65 | Regression detection MUST work | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-045b-01 | Test case load | <100ms | P0 |
| NFR-045b-02 | Extraction time | <10ms | P0 |
| NFR-045b-03 | Comparison time | <5ms | P0 |
| NFR-045b-04 | Scoring time | <1ms | P0 |
| NFR-045b-05 | Report generation | <500ms | P0 |
| NFR-045b-06 | Full suite | <30min | P0 |
| NFR-045b-07 | Per-case timeout | 60s | P0 |
| NFR-045b-08 | Parallel cases | Optional | P1 |
| NFR-045b-09 | Memory usage | <200MB | P0 |
| NFR-045b-10 | Results storage | <10MB | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-045b-11 | Extraction accuracy | 100% | P0 |
| NFR-045b-12 | Comparison accuracy | 100% | P0 |
| NFR-045b-13 | Scoring consistency | 100% | P0 |
| NFR-045b-14 | Cross-platform | All OS | P0 |
| NFR-045b-15 | Reproducibility | 100% with seed | P0 |
| NFR-045b-16 | Error recovery | Per case | P0 |
| NFR-045b-17 | Test case validity | Validated | P0 |
| NFR-045b-18 | Schema validation | Always | P0 |
| NFR-045b-19 | Partial results | Saved | P0 |
| NFR-045b-20 | Timeout handling | Graceful | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-045b-21 | Case start logged | Debug | P0 |
| NFR-045b-22 | Case result logged | Info | P0 |
| NFR-045b-23 | Failure logged | Warning | P0 |
| NFR-045b-24 | Summary logged | Info | P0 |
| NFR-045b-25 | Metrics: accuracy | Gauge | P0 |
| NFR-045b-26 | Metrics: by category | Gauge | P1 |
| NFR-045b-27 | Metrics: failures | Counter | P0 |
| NFR-045b-28 | Structured logging | JSON | P0 |
| NFR-045b-29 | Trace ID | Per run | P1 |
| NFR-045b-30 | Detailed failures | Exportable | P0 |

---

## Acceptance Criteria / Definition of Done

### Test Cases
- [ ] AC-001: Cases defined
- [ ] AC-002: Prompts present
- [ ] AC-003: Expected present
- [ ] AC-004: Categories exist
- [ ] AC-005: 50+ cases
- [ ] AC-006: All tools covered
- [ ] AC-007: Versioned
- [ ] AC-008: JSON format

### Evaluation
- [ ] AC-009: Tool call extracted
- [ ] AC-010: Name compared
- [ ] AC-011: Args compared
- [ ] AC-012: Semantic match
- [ ] AC-013: No call detected
- [ ] AC-014: Multiple handled
- [ ] AC-015: Partial credit
- [ ] AC-016: Scoring works

### Metrics
- [ ] AC-017: Accuracy calculated
- [ ] AC-018: By-category
- [ ] AC-019: By-tool
- [ ] AC-020: Confidence interval
- [ ] AC-021: Failures listed
- [ ] AC-022: Breakdown
- [ ] AC-023: Baseline comparison
- [ ] AC-024: Regression detected

### Reporting
- [ ] AC-025: Report generated
- [ ] AC-026: Overall shown
- [ ] AC-027: Details shown
- [ ] AC-028: Failures shown
- [ ] AC-029: Cross-platform
- [ ] AC-030: Tests pass
- [ ] AC-031: Documented
- [ ] AC-032: Reviewed

---

## User Verification Scenarios

### Scenario 1: Run Correctness Suite
**Persona:** Developer evaluating model  
**Preconditions:** LLM running  
**Steps:**
1. Run correctness suite
2. All cases run
3. Accuracy calculated
4. Report shown

**Verification Checklist:**
- [ ] Suite runs
- [ ] All complete
- [ ] Accuracy shown
- [ ] Report clear

### Scenario 2: Compare Models
**Persona:** Developer choosing model  
**Preconditions:** Two models  
**Steps:**
1. Run suite on model A
2. Run suite on model B
3. Compare accuracy
4. Choose better

**Verification Checklist:**
- [ ] Both complete
- [ ] Accuracy comparable
- [ ] Difference clear
- [ ] Decision informed

### Scenario 3: Analyze Failures
**Persona:** Developer debugging  
**Preconditions:** Some failures  
**Steps:**
1. Run suite
2. Failures found
3. Review failures
4. Identify patterns

**Verification Checklist:**
- [ ] Failures listed
- [ ] Prompt shown
- [ ] Expected shown
- [ ] Actual shown

### Scenario 4: Per-Tool Analysis
**Persona:** Developer optimizing  
**Preconditions:** Suite complete  
**Steps:**
1. View by-tool breakdown
2. Identify weak tools
3. Focus on those
4. Improve prompts

**Verification Checklist:**
- [ ] Breakdown works
- [ ] Per-tool shown
- [ ] Weak identified
- [ ] Actionable

### Scenario 5: Regression Detection
**Persona:** Developer after change  
**Preconditions:** Baseline exists  
**Steps:**
1. Run suite
2. Compare to baseline
3. Regression found
4. Investigate

**Verification Checklist:**
- [ ] Comparison works
- [ ] Regression detected
- [ ] Delta shown
- [ ] Actionable

### Scenario 6: Custom Test Cases
**Persona:** Developer with special needs  
**Preconditions:** Custom cases  
**Steps:**
1. Add custom cases
2. Run suite
3. Custom included
4. Results shown

**Verification Checklist:**
- [ ] Custom loaded
- [ ] Included in run
- [ ] Results correct
- [ ] Integrated

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-045b-01 | Test case loading | FR-045b-01 |
| UT-045b-02 | Prompt extraction | FR-045b-02 |
| UT-045b-03 | Tool call extraction | FR-045b-21 |
| UT-045b-04 | JSON format | FR-045b-23 |
| UT-045b-05 | Name comparison | FR-045b-26 |
| UT-045b-06 | Arg comparison | FR-045b-28 |
| UT-045b-07 | Semantic match | FR-045b-31 |
| UT-045b-08 | No call detection | FR-045b-36 |
| UT-045b-09 | Scoring | FR-045b-41 |
| UT-045b-10 | Partial scoring | FR-045b-43 |
| UT-045b-11 | Accuracy calculation | FR-045b-49 |
| UT-045b-12 | By-category | FR-045b-51 |
| UT-045b-13 | Confidence interval | FR-045b-53 |
| UT-045b-14 | Report generation | FR-045b-56 |
| UT-045b-15 | Regression detection | FR-045b-65 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-045b-01 | Harness integration | Task 045 |
| IT-045b-02 | LLM integration | Task 024 |
| IT-045b-03 | Tool integration | Task 025 |
| IT-045b-04 | Full suite run | E2E |
| IT-045b-05 | Large suite | NFR-045b-06 |
| IT-045b-06 | Cross-platform | NFR-045b-14 |
| IT-045b-07 | Reproducibility | NFR-045b-15 |
| IT-045b-08 | Error recovery | NFR-045b-16 |
| IT-045b-09 | Timeout handling | NFR-045b-20 |
| IT-045b-10 | Custom cases | FR-045b-14 |
| IT-045b-11 | Baseline comparison | FR-045b-64 |
| IT-045b-12 | Logging | NFR-045b-21 |
| IT-045b-13 | Results storage | NFR-045b-10 |
| IT-045b-14 | Filter by tag | FR-045b-19 |
| IT-045b-15 | Multiple calls | FR-045b-37 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Performance/
│       ├── CorrectnessTestCase.cs
│       ├── CorrectnessResult.cs
│       └── ToolCallComparison.cs
├── Acode.Application/
│   └── Performance/
│       ├── ICorrectnessEvaluator.cs
│       └── CorrectnessOptions.cs
├── Acode.Infrastructure/
│   └── Performance/
│       ├── CorrectnessEvaluator.cs
│       ├── ToolCallExtractor.cs
│       ├── ToolCallComparer.cs
│       ├── CorrectnessScorer.cs
│       └── TestCases/
│           └── correctness-suite.json
```

### Test Case Format

```json
{
  "version": "1.0",
  "cases": [
    {
      "id": "TC-001",
      "category": "simple",
      "tags": ["file-ops", "read"],
      "difficulty": "easy",
      "prompt": "Read the contents of README.md",
      "expected": {
        "tool": "read_file",
        "arguments": {
          "file_path": "README.md"
        }
      },
      "alternatives": [
        {
          "tool": "read_file",
          "arguments": {
            "file_path": "./README.md"
          }
        }
      ]
    }
  ]
}
```

### Report Output

```
Tool-Call Correctness Report
============================
Total Cases: 50
Passed: 42
Failed: 8
Accuracy: 84.0% (95% CI: 71.3% - 92.4%)

By Category:
  Simple:   95.0% (19/20)
  Complex:  80.0% (16/20)
  Edge:     70.0% (7/10)

By Tool:
  read_file:     92.3% (12/13)
  write_file:    88.9% (8/9)
  search:        75.0% (6/8)
  run_command:   80.0% (8/10)
  
Failures:
  TC-015: Expected search, got read_file
  TC-023: Missing required arg 'pattern'
  ...

vs Baseline (v1.2):
  Delta: -2.0% (was 86.0%)
  Status: Within acceptable variance
```

**End of Task 045.b Specification**
