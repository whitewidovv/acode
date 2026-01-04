# Task 046.a: Store Tasks as Specs

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 046 (Benchmark Suite)  

---

## Description

Task 046.a defines the task specification format—the declarative schema for benchmark tasks. Instead of hardcoded test logic, each benchmark task is stored as a JSON specification that describes: what the task is, what input to provide, and what output to expect. This enables task management without code changes.

The spec format supports: (1) task metadata (ID, name, category, tags), (2) input definition (prompt, files, context), (3) expected output (outcome, tool calls, artifacts), (4) evaluation criteria (strict vs fuzzy matching), and (5) execution parameters (timeout, retries, environment).

### Business Value

Declarative task specs provide:
- Version control for tasks
- Easy task addition/modification
- Non-programmer task authoring
- Task portability
- Clear documentation

### Scope Boundaries

This task defines the spec format. The runner is Task 046.b. Results format is Task 046.c. Scoring is Task 047.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Suite | Task 046 | Contains specs | Parent |
| Runner | Task 046.b | Executes specs | Consumer |
| Results | Task 046.c | Captures output | Downstream |
| Validation | Schema | Validates specs | Quality |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Invalid schema | Validation | Reject | Cannot run |
| Missing fields | Check | Reject | Cannot run |
| Invalid category | Enum check | Reject | Cannot run |
| Duplicate ID | Scan | Reject | Cannot run |
| Invalid timeout | Parse | Default | Warning |

### Assumptions

1. **JSON format**: Widely supported
2. **Schema stable**: Or versioned
3. **Categories fixed**: Known set
4. **IDs unique**: Within suite
5. **Files inline**: Or referenced

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Spec | Task specification |
| Schema | Structure definition |
| Metadata | Task descriptors |
| Input | Task stimulus |
| Expected | Success criteria |
| Evaluation | Comparison method |
| Strict | Exact match required |
| Fuzzy | Semantic match allowed |
| Inline | Content embedded |
| Reference | Content external |

---

## Out of Scope

- Task execution (Task 046.b)
- Results format (Task 046.c)
- Scoring (Task 047)
- Dynamic task generation
- Task templates
- Task inheritance

---

## Functional Requirements

### FR-001 to FR-020: Metadata

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046a-01 | Spec MUST have ID | P0 |
| FR-046a-02 | ID MUST be unique | P0 |
| FR-046a-03 | ID format: BENCH-NNN | P0 |
| FR-046a-04 | Spec MUST have name | P0 |
| FR-046a-05 | Name MUST be descriptive | P0 |
| FR-046a-06 | Spec MUST have category | P0 |
| FR-046a-07 | Category MUST be enum | P0 |
| FR-046a-08 | Categories: file-ops | P0 |
| FR-046a-09 | Categories: code-gen | P0 |
| FR-046a-10 | Categories: refactor | P0 |
| FR-046a-11 | Categories: debug | P0 |
| FR-046a-12 | Categories: multi-step | P0 |
| FR-046a-13 | Spec MAY have tags | P1 |
| FR-046a-14 | Tags MUST be array | P1 |
| FR-046a-15 | Spec MAY have description | P1 |
| FR-046a-16 | Spec MAY have difficulty | P1 |
| FR-046a-17 | Difficulty: easy/medium/hard | P1 |
| FR-046a-18 | Spec MAY have author | P2 |
| FR-046a-19 | Spec MAY have created date | P2 |
| FR-046a-20 | Spec MAY have version | P1 |

### FR-021 to FR-040: Input Definition

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046a-21 | Input MUST exist | P0 |
| FR-046a-22 | Input MUST have prompt | P0 |
| FR-046a-23 | Prompt MUST be string | P0 |
| FR-046a-24 | Prompt MUST be non-empty | P0 |
| FR-046a-25 | Input MAY have files | P0 |
| FR-046a-26 | Files MUST be object | P0 |
| FR-046a-27 | Files: key = path | P0 |
| FR-046a-28 | Files: value = content | P0 |
| FR-046a-29 | Inline files MUST work | P0 |
| FR-046a-30 | Reference files MUST work | P1 |
| FR-046a-31 | Reference format: @path | P1 |
| FR-046a-32 | Input MAY have context | P1 |
| FR-046a-33 | Context MUST be object | P1 |
| FR-046a-34 | Context: previous results | P1 |
| FR-046a-35 | Context: environment vars | P1 |
| FR-046a-36 | Input MAY have workspace | P1 |
| FR-046a-37 | Workspace: directory structure | P1 |
| FR-046a-38 | Binary files MUST be base64 | P2 |
| FR-046a-39 | Large files MUST reference | P1 |
| FR-046a-40 | File limit: 1MB inline | P0 |

### FR-041 to FR-060: Expected Output

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046a-41 | Expected MUST exist | P0 |
| FR-046a-42 | Expected MUST have outcome | P0 |
| FR-046a-43 | Outcome: success | P0 |
| FR-046a-44 | Outcome: failure | P0 |
| FR-046a-45 | Outcome: partial | P1 |
| FR-046a-46 | Expected MAY have toolCalls | P0 |
| FR-046a-47 | toolCalls MUST be array | P0 |
| FR-046a-48 | toolCall: tool name | P0 |
| FR-046a-49 | toolCall: arguments (optional) | P1 |
| FR-046a-50 | Expected MAY have output | P0 |
| FR-046a-51 | Output: files created | P0 |
| FR-046a-52 | Output: content patterns | P1 |
| FR-046a-53 | Expected MAY have artifacts | P1 |
| FR-046a-54 | Artifacts: generated files | P1 |
| FR-046a-55 | Expected MAY have assertions | P1 |
| FR-046a-56 | Assertions: conditions | P1 |
| FR-046a-57 | Assertions: file exists | P1 |
| FR-046a-58 | Assertions: file contains | P1 |
| FR-046a-59 | Assertions: no errors | P1 |
| FR-046a-60 | Alternatives MAY exist | P1 |

### FR-061 to FR-070: Execution Parameters

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046a-61 | Timeout MUST exist | P0 |
| FR-046a-62 | Timeout format: ISO 8601 | P0 |
| FR-046a-63 | Default timeout: PT60S | P0 |
| FR-046a-64 | Max timeout: PT300S | P0 |
| FR-046a-65 | Retries MAY be specified | P1 |
| FR-046a-66 | Default retries: 0 | P0 |
| FR-046a-67 | Environment MAY be specified | P1 |
| FR-046a-68 | Skip condition MAY exist | P2 |
| FR-046a-69 | Dependency MAY be specified | P2 |
| FR-046a-70 | Isolated MUST default true | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046a-01 | Parse time per spec | <5ms | P0 |
| NFR-046a-02 | Validate time per spec | <10ms | P0 |
| NFR-046a-03 | Load 100 specs | <500ms | P0 |
| NFR-046a-04 | Spec file size limit | 1MB | P0 |
| NFR-046a-05 | Suite file size limit | 10MB | P0 |
| NFR-046a-06 | Memory per spec | <1MB | P0 |
| NFR-046a-07 | Inline file extract | <50ms | P0 |
| NFR-046a-08 | Reference file load | <100ms | P0 |
| NFR-046a-09 | Schema validation | <20ms | P0 |
| NFR-046a-10 | ID uniqueness check | <10ms | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046a-11 | Schema validation | 100% | P0 |
| NFR-046a-12 | UTF-8 support | Full | P0 |
| NFR-046a-13 | Cross-platform paths | Normalized | P0 |
| NFR-046a-14 | Version compatibility | Backward | P0 |
| NFR-046a-15 | Missing field detection | 100% | P0 |
| NFR-046a-16 | Invalid value detection | 100% | P0 |
| NFR-046a-17 | Error messages | Clear | P0 |
| NFR-046a-18 | Error location | Line/column | P1 |
| NFR-046a-19 | Duplicate detection | 100% | P0 |
| NFR-046a-20 | Reference resolution | Verified | P1 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046a-21 | Parse logged | Debug | P0 |
| NFR-046a-22 | Validation logged | Debug | P0 |
| NFR-046a-23 | Errors logged | Warning | P0 |
| NFR-046a-24 | Load summary | Info | P0 |
| NFR-046a-25 | Spec count | Counter | P1 |
| NFR-046a-26 | Category count | Gauge | P1 |
| NFR-046a-27 | Validation failures | Counter | P0 |
| NFR-046a-28 | Structured logging | JSON | P0 |
| NFR-046a-29 | Schema version | Logged | P0 |
| NFR-046a-30 | File references | Logged | P1 |

---

## Acceptance Criteria / Definition of Done

### Metadata
- [ ] AC-001: ID required
- [ ] AC-002: ID unique
- [ ] AC-003: Name required
- [ ] AC-004: Category required
- [ ] AC-005: Categories enum
- [ ] AC-006: Tags optional
- [ ] AC-007: Description optional
- [ ] AC-008: Difficulty optional

### Input
- [ ] AC-009: Input required
- [ ] AC-010: Prompt required
- [ ] AC-011: Files optional
- [ ] AC-012: Inline works
- [ ] AC-013: Reference works
- [ ] AC-014: Context optional
- [ ] AC-015: Workspace optional
- [ ] AC-016: Size limits

### Expected
- [ ] AC-017: Expected required
- [ ] AC-018: Outcome required
- [ ] AC-019: toolCalls optional
- [ ] AC-020: Output optional
- [ ] AC-021: Assertions optional
- [ ] AC-022: Alternatives optional
- [ ] AC-023: Semantic match
- [ ] AC-024: Exact match

### Execution
- [ ] AC-025: Timeout required
- [ ] AC-026: Default applied
- [ ] AC-027: ISO format
- [ ] AC-028: Retries optional
- [ ] AC-029: Environment optional
- [ ] AC-030: Validation works
- [ ] AC-031: Tests pass
- [ ] AC-032: Documented

---

## User Verification Scenarios

### Scenario 1: Create Simple Task
**Persona:** Developer  
**Preconditions:** Schema known  
**Steps:**
1. Create task spec
2. Add metadata
3. Add input/expected
4. Validate spec

**Verification Checklist:**
- [ ] Spec created
- [ ] Valid JSON
- [ ] Schema passes
- [ ] Task runs

### Scenario 2: Task with Files
**Persona:** Developer  
**Preconditions:** Files needed  
**Steps:**
1. Add inline files
2. Set file content
3. Reference in expected
4. Validate

**Verification Checklist:**
- [ ] Files added
- [ ] Content correct
- [ ] References work
- [ ] Task runs

### Scenario 3: Complex Assertions
**Persona:** Developer  
**Preconditions:** Complex expected  
**Steps:**
1. Define multiple assertions
2. Add alternatives
3. Validate spec
4. Run task

**Verification Checklist:**
- [ ] Assertions work
- [ ] Alternatives work
- [ ] Validation passes
- [ ] Evaluation correct

### Scenario 4: Invalid Spec
**Persona:** Developer  
**Preconditions:** Missing fields  
**Steps:**
1. Create incomplete spec
2. Run validation
3. Error shown
4. Fix spec

**Verification Checklist:**
- [ ] Error detected
- [ ] Message clear
- [ ] Location shown
- [ ] Fix works

### Scenario 5: Reference Files
**Persona:** Developer  
**Preconditions:** Large files  
**Steps:**
1. Create reference
2. Point to file
3. Validate
4. Run task

**Verification Checklist:**
- [ ] Reference works
- [ ] File loaded
- [ ] Content correct
- [ ] Task runs

### Scenario 6: Custom Category
**Persona:** Developer  
**Preconditions:** New category needed  
**Steps:**
1. Use invalid category
2. Error shown
3. Use valid category
4. Works

**Verification Checklist:**
- [ ] Invalid rejected
- [ ] Error clear
- [ ] Valid works
- [ ] Enum enforced

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-046a-01 | ID parsing | FR-046a-01 |
| UT-046a-02 | ID uniqueness | FR-046a-02 |
| UT-046a-03 | Name required | FR-046a-04 |
| UT-046a-04 | Category enum | FR-046a-07 |
| UT-046a-05 | Input required | FR-046a-21 |
| UT-046a-06 | Prompt required | FR-046a-22 |
| UT-046a-07 | Files parsing | FR-046a-25 |
| UT-046a-08 | Expected required | FR-046a-41 |
| UT-046a-09 | Outcome enum | FR-046a-42 |
| UT-046a-10 | Timeout default | FR-046a-63 |
| UT-046a-11 | ISO duration parse | FR-046a-62 |
| UT-046a-12 | Reference resolution | FR-046a-30 |
| UT-046a-13 | Schema validation | NFR-046a-11 |
| UT-046a-14 | Error messages | NFR-046a-17 |
| UT-046a-15 | Size limits | FR-046a-40 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-046a-01 | Suite loading | Task 046 |
| IT-046a-02 | Spec parsing E2E | E2E |
| IT-046a-03 | Inline files | FR-046a-29 |
| IT-046a-04 | Reference files | FR-046a-30 |
| IT-046a-05 | Large suite | NFR-046a-03 |
| IT-046a-06 | Cross-platform | NFR-046a-13 |
| IT-046a-07 | UTF-8 content | NFR-046a-12 |
| IT-046a-08 | Backward compat | NFR-046a-14 |
| IT-046a-09 | Duplicate detect | NFR-046a-19 |
| IT-046a-10 | Logging | NFR-046a-21 |
| IT-046a-11 | Invalid spec | FR-046a-10 |
| IT-046a-12 | Complex expected | FR-046a-55 |
| IT-046a-13 | Alternatives | FR-046a-60 |
| IT-046a-14 | Context input | FR-046a-32 |
| IT-046a-15 | Dependencies | FR-046a-69 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Evaluation/
│       ├── TaskSpec.cs
│       ├── TaskMetadata.cs
│       ├── TaskInput.cs
│       ├── TaskExpected.cs
│       ├── TaskCategory.cs
│       ├── TaskDifficulty.cs
│       └── ExecutionParams.cs
├── Acode.Application/
│   └── Evaluation/
│       ├── ISpecParser.cs
│       ├── ISpecValidator.cs
│       └── SpecOptions.cs
├── Acode.Infrastructure/
│   └── Evaluation/
│       ├── JsonSpecParser.cs
│       ├── SchemaValidator.cs
│       └── FileReferenceResolver.cs
├── data/
│   └── schemas/
│       └── task-spec-v1.schema.json
```

### JSON Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["id", "name", "category", "input", "expected", "timeout"],
  "properties": {
    "id": {
      "type": "string",
      "pattern": "^BENCH-[0-9]{3}$"
    },
    "name": {
      "type": "string",
      "minLength": 1
    },
    "category": {
      "type": "string",
      "enum": ["file-ops", "code-gen", "refactor", "debug", "multi-step"]
    },
    "tags": {
      "type": "array",
      "items": { "type": "string" }
    },
    "difficulty": {
      "type": "string",
      "enum": ["easy", "medium", "hard"]
    },
    "input": {
      "type": "object",
      "required": ["prompt"],
      "properties": {
        "prompt": { "type": "string", "minLength": 1 },
        "files": { "type": "object" },
        "context": { "type": "object" }
      }
    },
    "expected": {
      "type": "object",
      "required": ["outcome"],
      "properties": {
        "outcome": {
          "type": "string",
          "enum": ["success", "failure", "partial"]
        },
        "toolCalls": {
          "type": "array",
          "items": { "type": "string" }
        },
        "output": { "type": "object" },
        "assertions": { "type": "array" }
      }
    },
    "timeout": {
      "type": "string",
      "pattern": "^PT[0-9]+[HMS]$"
    }
  }
}
```

**End of Task 046.a Specification**
