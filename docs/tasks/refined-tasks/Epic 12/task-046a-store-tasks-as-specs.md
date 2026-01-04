# Task 046.a: Store Tasks as Specs

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 046 (Benchmark Suite)  

---

## Description

### Overview

Task 046.a defines the task specification format—the declarative JSON schema for benchmark tasks. Instead of hardcoded test logic scattered across code files, each benchmark task is stored as a self-contained JSON specification that describes: what the task is, what input to provide, and what output to expect. This declarative approach enables task management without code changes, version control for benchmark definitions, and non-programmer task authoring.

The spec format is the data model that powers the entire benchmark system. It supports: (1) task metadata (ID, name, category, tags, difficulty), (2) input definition (prompt, files, context, environment), (3) expected output (outcome, tool calls, artifacts, assertions), (4) evaluation criteria (strict vs fuzzy matching, alternatives), and (5) execution parameters (timeout, retries, isolation requirements).

### Business Value

Declarative task specifications provide significant advantages over procedural test definitions:

1. **Version Control:** Task specs are plain JSON files that can be versioned, diffed, and merged using standard Git workflows. Changes to benchmarks are visible in pull requests. History shows exactly when and why tasks changed.

2. **Easy Modification:** Adding a new benchmark task requires writing a JSON file, not code. Modifying expected behavior is a JSON edit, not a recompile. This dramatically lowers the barrier to benchmark maintenance.

3. **Non-Programmer Authoring:** Product managers, QA engineers, and technical writers can contribute benchmark tasks without writing code. The JSON format is structured enough to prevent errors but simple enough for non-developers.

4. **Portability:** Task specs can be shared between teams, exported to other systems, or imported from external sources. The JSON format is universally supported.

5. **Documentation as Code:** The task spec IS the documentation. The prompt shows what the task tests. The expected output shows success criteria. No separate documentation needed.

6. **Validation:** JSON Schema validation catches errors before runtime. Malformed specs are rejected with clear error messages pointing to the exact problem location.

7. **Extensibility:** The schema can evolve to support new capabilities while maintaining backward compatibility. New optional fields can be added without breaking existing specs.

### Scope Boundaries

This task defines the spec format (schema) and parsing/validation logic. The actual task execution is Task 046.b. The results output format is Task 046.c. Scoring and pass/fail thresholds are Task 047.

### Integration Points

| Component | Integration Type | Interface | Data Flow | Notes |
|-----------|------------------|-----------|-----------|-------|
| Task 046 Suite | Parent | Contains specs | Suite → Specs | Suite defines task list |
| Task 046.b Runner | Consumer | Executes specs | Spec → Execution | Runner interprets specs |
| Task 046.c Results | Downstream | Captures output | Execution → Result | Results reference specs |
| JSON Schema | Validation | Validates specs | Spec → Valid/Error | Draft-07 standard |
| File System | Storage | Persists specs | Disk ↔ Memory | JSON files |
| Task 003 DI | Infrastructure | ISpecParser | Service resolution | Dependency injection |
| Task 002 Config | Configuration | IConfiguration | Settings | Parser options |

### Failure Modes

| Failure | Detection | Impact | Recovery | User Impact |
|---------|-----------|--------|----------|-------------|
| Schema validation error | JSON Schema | Cannot load task | Reject with error | Clear error message |
| Missing required field | Schema | Invalid spec | Reject with location | Shows missing field |
| Invalid category | Enum validation | Invalid spec | Reject | Shows valid categories |
| Duplicate task ID | Suite scan | Ambiguous | Reject | Shows duplicate location |
| Invalid timeout format | ISO 8601 parse | Cannot run | Default fallback | Warning logged |
| File reference not found | Resolution | Missing input | Reject | Shows missing path |
| File too large | Size check | Memory issue | Reject | Shows limit |
| Invalid JSON syntax | Parser | Cannot parse | Reject | Shows line/column |
| Encoding error | UTF-8 decode | Garbled content | Reject | Shows encoding issue |
| Circular reference | Graph check | Infinite loop | Reject | Shows cycle |

### Assumptions

1. **JSON Format:** JSON is universally supported across languages and tools. No need for alternative formats like YAML or XML.

2. **Schema Stability:** The schema version is tracked and changes are backward compatible. Major version changes require explicit migration.

3. **Category Taxonomy:** The five core categories (file-ops, code-gen, refactor, debug, multi-step) cover the primary use cases. Custom categories can be added if needed.

4. **ID Uniqueness:** Task IDs are unique within a suite. Global uniqueness is not required.

5. **Files Inline:** For most tasks, file content is embedded inline. External file references are for large files or binary content.

6. **UTF-8 Encoding:** All text content uses UTF-8 encoding. Other encodings are not supported.

7. **Reasonable Sizes:** Individual spec files are under 1MB. Suites with hundreds of tasks are under 10MB.

8. **Schema Validation:** JSON Schema Draft-07 is the validation standard. All compliant validators should produce consistent results.

9. **Timeout Reasonableness:** Timeouts use ISO 8601 duration format. Most tasks are under 60 seconds.

10. **Deterministic Parsing:** The same spec file always produces the same parsed result. Parsing is deterministic.

### Security Considerations

1. **Input Sanitization:** Spec files from untrusted sources MUST be validated against the schema before use. Malformed JSON can cause parser crashes or memory exhaustion.

2. **File Reference Validation:** External file references (@path syntax) MUST be resolved against an allowed base directory. Path traversal attacks (../../etc/passwd) MUST be rejected.

3. **Size Limits:** Specs have maximum size limits to prevent denial-of-service through memory exhaustion. Individual files: 1MB. Suite total: 10MB.

4. **Content Inspection:** File content embedded in specs is treated as data, not code. Content is never executed directly from the spec.

5. **Prompt Injection Prevention:** The prompt field is a user request, not a system instruction. It is passed to the agent as user input, not privileged context.

6. **Secret Detection:** Specs SHOULD NOT contain secrets (API keys, passwords). If detected during validation, a warning is emitted.

7. **Schema Validation:** Only specs conforming to the published schema are accepted. Unknown fields are ignored in forward-compatible mode.

8. **Audit Trail:** Spec loading and validation events are logged for forensic analysis.

9. **Access Control:** Spec files inherit filesystem permissions. Only authorized users can add or modify specs.

10. **Checksum Verification:** Spec files can include optional checksum fields for integrity verification.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Spec | Short for specification; a task definition in JSON format |
| Schema | The JSON Schema defining the structure of valid specs |
| Metadata | Descriptive properties about the task (ID, name, category, tags) |
| Input | The stimulus provided to the agent (prompt, files, context) |
| Expected | The success criteria defining what constitutes passing |
| Evaluation | The comparison method (strict vs fuzzy) |
| Strict Match | Exact match required; no variation allowed |
| Fuzzy Match | Semantic match; equivalent results accepted |
| Inline | Content embedded directly in the spec JSON |
| Reference | Content stored externally and loaded at parse time |
| Assertion | A condition that must be true for the task to pass |
| Alternative | An acceptable variation of the expected output |
| Outcome | The expected result status (success, failure, partial) |
| Tool Call | An invocation of a tool by the agent |
| Artifact | A file or output produced by task execution |
| Timeout | Maximum allowed execution time |
| Isolation | Sandboxed execution environment |

---

## Out of Scope

The following items are explicitly excluded from Task 046.a:

- **Task Execution** — See Task 046.b for the runner that executes specs
- **Results Format** — See Task 046.c for the output JSON schema
- **Scoring Logic** — See Task 047 for pass/fail thresholds and gates
- **Dynamic Task Generation** — Specs are static; runtime generation is future work
- **Task Templates** — Parameterized templates for spec generation are future work
- **Task Inheritance** — Tasks cannot inherit from other tasks
- **YAML Format** — Only JSON is supported
- **Binary Embedding** — Binary content must use base64 encoding

---

## Functional Requirements

### FR-001 to FR-025: Task Metadata

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046a-01 | Spec MUST have an ID field | P0 |
| FR-046a-02 | ID MUST be a non-empty string | P0 |
| FR-046a-03 | ID MUST be unique within the suite | P0 |
| FR-046a-04 | ID SHOULD follow pattern BENCH-NNN or category-NNN | P0 |
| FR-046a-05 | Spec MUST have a name field | P0 |
| FR-046a-06 | Name MUST be a non-empty descriptive string | P0 |
| FR-046a-07 | Name SHOULD be human-readable (2-10 words) | P1 |
| FR-046a-08 | Spec MUST have a category field | P0 |
| FR-046a-09 | Category MUST be one of the defined enum values | P0 |
| FR-046a-10 | Category enum MUST include "file-ops" | P0 |
| FR-046a-11 | Category enum MUST include "code-gen" | P0 |
| FR-046a-12 | Category enum MUST include "refactor" | P0 |
| FR-046a-13 | Category enum MUST include "debug" | P0 |
| FR-046a-14 | Category enum MUST include "multi-step" | P0 |
| FR-046a-15 | Spec MAY have a tags field | P1 |
| FR-046a-16 | Tags MUST be an array of strings | P1 |
| FR-046a-17 | Tags SHOULD be lowercase with hyphens | P1 |
| FR-046a-18 | Spec MAY have a description field | P1 |
| FR-046a-19 | Description MUST be a string (can be multi-line) | P1 |
| FR-046a-20 | Spec MAY have a difficulty field | P1 |
| FR-046a-21 | Difficulty MUST be one of: easy, medium, hard | P1 |
| FR-046a-22 | Spec MAY have an author field | P2 |
| FR-046a-23 | Spec MAY have a created date field (ISO 8601) | P2 |
| FR-046a-24 | Spec MAY have a modified date field (ISO 8601) | P2 |
| FR-046a-25 | Spec MAY have a version field (semver) | P1 |

### FR-026 to FR-050: Input Definition

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046a-26 | Spec MUST have an input field | P0 |
| FR-046a-27 | Input MUST be an object | P0 |
| FR-046a-28 | Input MUST have a prompt field | P0 |
| FR-046a-29 | Prompt MUST be a non-empty string | P0 |
| FR-046a-30 | Prompt represents the user request to the agent | P0 |
| FR-046a-31 | Input MAY have a files field | P0 |
| FR-046a-32 | Files MUST be an object (map of path → content) | P0 |
| FR-046a-33 | File keys MUST be relative paths | P0 |
| FR-046a-34 | File values MUST be strings (content) | P0 |
| FR-046a-35 | File content MAY be inline (embedded) | P0 |
| FR-046a-36 | File content MAY be a reference (@path syntax) | P1 |
| FR-046a-37 | Reference format: "@./relative/path/to/file" | P1 |
| FR-046a-38 | References are resolved relative to spec file | P1 |
| FR-046a-39 | Binary content MUST be base64 encoded | P2 |
| FR-046a-40 | Binary content prefix: "base64:" | P2 |
| FR-046a-41 | Inline file size limit: 1MB | P0 |
| FR-046a-42 | Large files MUST use reference syntax | P1 |
| FR-046a-43 | Input MAY have a context field | P1 |
| FR-046a-44 | Context MUST be an object | P1 |
| FR-046a-45 | Context MAY include previous_results (for chained tasks) | P1 |
| FR-046a-46 | Context MAY include environment variables | P1 |
| FR-046a-47 | Context MAY include workspace configuration | P1 |
| FR-046a-48 | Input MAY have a workspace field | P1 |
| FR-046a-49 | Workspace defines directory structure to create | P1 |
| FR-046a-50 | Workspace enables multi-file scenarios | P1 |

### FR-051 to FR-080: Expected Output

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046a-51 | Spec MUST have an expected field | P0 |
| FR-046a-52 | Expected MUST be an object | P0 |
| FR-046a-53 | Expected MUST have an outcome field | P0 |
| FR-046a-54 | Outcome MUST be one of: success, failure, partial | P0 |
| FR-046a-55 | Outcome "success" = task completed successfully | P0 |
| FR-046a-56 | Outcome "failure" = task expected to fail | P0 |
| FR-046a-57 | Outcome "partial" = partial credit allowed | P1 |
| FR-046a-58 | Expected MAY have a toolCalls field | P0 |
| FR-046a-59 | toolCalls MUST be an array of tool call expectations | P0 |
| FR-046a-60 | Tool call expectation: string (tool name only) | P0 |
| FR-046a-61 | Tool call expectation: object (tool + args) | P1 |
| FR-046a-62 | Tool call matching is order-independent by default | P0 |
| FR-046a-63 | Ordered tool calls can be specified with "ordered": true | P1 |
| FR-046a-64 | Expected MAY have a forbiddenCalls field | P0 |
| FR-046a-65 | forbiddenCalls = tools that MUST NOT be called | P0 |
| FR-046a-66 | Expected MAY have an output field | P0 |
| FR-046a-67 | Output defines expected files/artifacts | P0 |
| FR-046a-68 | Output can specify file existence | P0 |
| FR-046a-69 | Output can specify file content patterns | P1 |
| FR-046a-70 | Expected MAY have an assertions field | P1 |
| FR-046a-71 | Assertions MUST be an array of assertion objects | P1 |
| FR-046a-72 | Assertion types: exists, contains, matches, equals | P1 |
| FR-046a-73 | Assertion "exists": file/path exists | P1 |
| FR-046a-74 | Assertion "contains": output contains string | P1 |
| FR-046a-75 | Assertion "matches": output matches regex | P1 |
| FR-046a-76 | Assertion "equals": output equals expected | P1 |
| FR-046a-77 | Expected MAY have an alternatives field | P1 |
| FR-046a-78 | Alternatives = acceptable variations | P1 |
| FR-046a-79 | Any alternative matching = pass | P1 |
| FR-046a-80 | Alternatives use same structure as expected | P1 |

### FR-081 to FR-100: Execution Parameters

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046a-81 | Spec MUST have a timeout field | P0 |
| FR-046a-82 | Timeout MUST be an ISO 8601 duration string | P0 |
| FR-046a-83 | Timeout format examples: PT30S, PT1M, PT5M | P0 |
| FR-046a-84 | Default timeout: PT60S (60 seconds) | P0 |
| FR-046a-85 | Maximum timeout: PT300S (5 minutes) | P0 |
| FR-046a-86 | Timeouts exceeding max are clamped | P0 |
| FR-046a-87 | Spec MAY have a retries field | P1 |
| FR-046a-88 | Retries MUST be a non-negative integer | P1 |
| FR-046a-89 | Default retries: 0 (no automatic retry) | P0 |
| FR-046a-90 | Maximum retries: 3 | P0 |
| FR-046a-91 | Spec MAY have an environment field | P1 |
| FR-046a-92 | Environment = environment variables to set | P1 |
| FR-046a-93 | Environment MUST be object (key → value) | P1 |
| FR-046a-94 | Spec MAY have a skip field | P2 |
| FR-046a-95 | Skip = true means task is skipped | P2 |
| FR-046a-96 | Skip can include reason string | P2 |
| FR-046a-97 | Spec MAY have a dependsOn field | P2 |
| FR-046a-98 | dependsOn = array of task IDs that must pass first | P2 |
| FR-046a-99 | Spec MAY have an isolated field | P0 |
| FR-046a-100 | Isolated MUST default to true (sandboxed) | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Maximum | Priority |
|----|-------------|--------|---------|----------|
| NFR-046a-01 | Parse time per spec file | <5ms | 10ms | P0 |
| NFR-046a-02 | Schema validation per spec | <10ms | 20ms | P0 |
| NFR-046a-03 | Load 100 specs from files | <500ms | 1000ms | P0 |
| NFR-046a-04 | Load 1000 specs from files | <3s | 5s | P1 |
| NFR-046a-05 | Individual spec file size limit | 1MB | 2MB | P0 |
| NFR-046a-06 | Suite total size limit | 10MB | 20MB | P0 |
| NFR-046a-07 | Memory per loaded spec | <1MB | 2MB | P0 |
| NFR-046a-08 | Inline file extraction time | <50ms | 100ms | P0 |
| NFR-046a-09 | External file reference load | <100ms | 200ms | P0 |
| NFR-046a-10 | ID uniqueness check (1000 specs) | <50ms | 100ms | P0 |
| NFR-046a-11 | JSON parse without validation | <2ms | 5ms | P0 |
| NFR-046a-12 | Full spec validation | <15ms | 30ms | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046a-13 | Schema validation accuracy | 100% | P0 |
| NFR-046a-14 | UTF-8 encoding support | Full | P0 |
| NFR-046a-15 | Cross-platform path normalization | Automatic | P0 |
| NFR-046a-16 | Schema version backward compatibility | Yes | P0 |
| NFR-046a-17 | Missing required field detection | 100% | P0 |
| NFR-046a-18 | Invalid value detection | 100% | P0 |
| NFR-046a-19 | Error messages include location | Yes | P0 |
| NFR-046a-20 | Error location: line and column | Yes | P1 |
| NFR-046a-21 | Duplicate ID detection | 100% | P0 |
| NFR-046a-22 | External reference resolution | Verified | P1 |
| NFR-046a-23 | Path traversal prevention | 100% | P0 |
| NFR-046a-24 | Malformed JSON handling | Graceful | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046a-25 | Spec parse event logged | Debug level | P0 |
| NFR-046a-26 | Spec validation event logged | Debug level | P0 |
| NFR-046a-27 | Validation errors logged | Warning level | P0 |
| NFR-046a-28 | Load summary logged | Info level | P0 |
| NFR-046a-29 | Metric: specs_loaded_total | Counter | P1 |
| NFR-046a-30 | Metric: specs_by_category | Gauge | P1 |
| NFR-046a-31 | Metric: validation_failures_total | Counter | P0 |
| NFR-046a-32 | Metric: parse_duration_ms | Histogram | P1 |
| NFR-046a-33 | Structured logging format | JSON | P0 |
| NFR-046a-34 | Schema version logged | Info level | P0 |
| NFR-046a-35 | External references logged | Debug level | P1 |
| NFR-046a-36 | Trace context propagated | Yes | P1 |

---

## User Manual Documentation

### Overview

Task specifications define benchmark tasks in a declarative JSON format. Instead of writing code, you describe what the task tests, what input to provide, and what success looks like. The benchmark runner reads these specs and executes them automatically.

### Quick Start

Create a minimal task spec:

```json
{
  "id": "BENCH-001",
  "name": "Read file contents",
  "category": "file-ops",
  "input": {
    "prompt": "Read README.md and tell me what the project is about.",
    "files": {
      "README.md": "# My Project\n\nA sample project for testing."
    }
  },
  "expected": {
    "outcome": "success",
    "toolCalls": ["read_file"]
  },
  "timeout": "PT30S"
}
```

### Spec Structure

Every task spec has these sections:

| Section | Required | Purpose |
|---------|----------|---------|
| id | Yes | Unique identifier |
| name | Yes | Human-readable name |
| category | Yes | Task classification |
| input | Yes | Prompt and files |
| expected | Yes | Success criteria |
| timeout | Yes | Time limit |
| tags | No | Labels for filtering |
| description | No | Detailed explanation |
| difficulty | No | easy/medium/hard |

### Categories

The five built-in categories:

| Category | Description | Typical Tasks |
|----------|-------------|---------------|
| file-ops | File operations | Read, write, move, delete files |
| code-gen | Code generation | Write new functions, classes, modules |
| refactor | Code refactoring | Rename, extract, restructure code |
| debug | Debugging | Find and fix bugs |
| multi-step | Complex workflows | Multiple operations in sequence |

### Input Definition

The `input` section defines what the agent receives:

```json
{
  "input": {
    "prompt": "Your instruction to the agent",
    "files": {
      "path/to/file.ts": "file content here",
      "another/file.py": "more content"
    },
    "context": {
      "key": "value"
    }
  }
}
```

#### Inline Files

Embed file content directly in the spec:

```json
{
  "files": {
    "src/utils.ts": "export function add(a: number, b: number): number {\n  return a + b;\n}"
  }
}
```

#### File References

For large files, use external references:

```json
{
  "files": {
    "large-file.ts": "@./fixtures/large-file.ts"
  }
}
```

The `@` prefix indicates a file reference. The path is relative to the spec file location.

### Expected Output

The `expected` section defines success criteria:

```json
{
  "expected": {
    "outcome": "success",
    "toolCalls": ["read_file", "write_file"],
    "forbiddenCalls": ["delete_file"],
    "assertions": [
      { "type": "exists", "path": "output.ts" },
      { "type": "contains", "path": "output.ts", "value": "function" }
    ]
  }
}
```

#### Tool Call Matching

- `toolCalls`: Array of tools that MUST be called
- `forbiddenCalls`: Array of tools that MUST NOT be called
- Matching is order-independent by default

For ordered matching:

```json
{
  "toolCalls": [
    { "name": "read_file", "order": 1 },
    { "name": "write_file", "order": 2 }
  ],
  "ordered": true
}
```

#### Assertions

| Type | Description | Example |
|------|-------------|---------|
| exists | Path exists | `{ "type": "exists", "path": "output.txt" }` |
| contains | Content contains | `{ "type": "contains", "path": "*.ts", "value": "export" }` |
| matches | Matches regex | `{ "type": "matches", "path": "*.ts", "pattern": "function\\s+\\w+" }` |
| equals | Exact match | `{ "type": "equals", "path": "out.txt", "value": "hello" }` |

### Execution Parameters

```json
{
  "timeout": "PT60S",
  "retries": 2,
  "isolated": true,
  "environment": {
    "NODE_ENV": "test"
  }
}
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| timeout | ISO 8601 | PT60S | Max execution time |
| retries | integer | 0 | Automatic retry count |
| isolated | boolean | true | Run in sandbox |
| environment | object | {} | Environment variables |

### Timeout Format

Timeouts use ISO 8601 duration format:

| Duration | Meaning |
|----------|---------|
| PT30S | 30 seconds |
| PT1M | 1 minute |
| PT2M30S | 2 minutes 30 seconds |
| PT5M | 5 minutes (maximum) |

### Complete Example

```json
{
  "id": "BENCH-042",
  "name": "Extract method refactoring",
  "category": "refactor",
  "description": "Test the agent's ability to extract duplicated code into a reusable method.",
  "difficulty": "medium",
  "tags": ["refactor", "core", "p1"],
  "author": "benchmark-team",
  "created": "2026-01-04T00:00:00Z",
  "version": "1.0.0",
  "input": {
    "prompt": "The calculate() function has duplicated validation logic. Extract the validation into a separate validateInput() method.",
    "files": {
      "src/calculator.ts": "export function calculate(a: number, b: number, op: string): number {\n  // Duplicated validation\n  if (typeof a !== 'number') throw new Error('a must be number');\n  if (typeof b !== 'number') throw new Error('b must be number');\n  \n  if (op === 'add') {\n    return a + b;\n  } else if (op === 'subtract') {\n    // Duplicated validation again\n    if (typeof a !== 'number') throw new Error('a must be number');\n    if (typeof b !== 'number') throw new Error('b must be number');\n    return a - b;\n  }\n  throw new Error('Unknown operation');\n}"
    }
  },
  "expected": {
    "outcome": "success",
    "toolCalls": ["read_file", "replace_string_in_file"],
    "forbiddenCalls": ["delete_file"],
    "assertions": [
      { "type": "contains", "path": "src/calculator.ts", "value": "validateInput" },
      { "type": "matches", "path": "src/calculator.ts", "pattern": "function\\s+validateInput" }
    ],
    "alternatives": [
      {
        "assertions": [
          { "type": "contains", "path": "src/calculator.ts", "value": "validate" },
          { "type": "matches", "path": "src/calculator.ts", "pattern": "(validate|check)Input" }
        ]
      }
    ]
  },
  "timeout": "PT90S",
  "retries": 1,
  "isolated": true
}
```

### Best Practices

1. **Use Descriptive IDs:** `code-gen-001` is better than `test1`. IDs appear in reports and logs.

2. **Write Clear Prompts:** The prompt should clearly describe what you want the agent to do. Ambiguous prompts lead to inconsistent results.

3. **Include Minimal Files:** Only include files needed for the task. Extra files confuse the agent and slow execution.

4. **Define Specific Expectations:** Vague expectations lead to false positives/negatives. Be specific about what success looks like.

5. **Use Alternatives for Valid Variations:** If multiple approaches are acceptable, use the `alternatives` field rather than weakening assertions.

6. **Set Appropriate Timeouts:** Simple tasks need 30s. Complex multi-step tasks may need 2-5 minutes. Don't set timeouts too short or too long.

7. **Tag for Organization:** Use tags like `smoke-test`, `regression`, `p0` to enable filtered runs.

8. **Document Complex Tasks:** Use the `description` field to explain non-obvious task requirements.

9. **Version Your Specs:** Use the `version` field when making breaking changes to track compatibility.

10. **Validate Before Committing:** Run `acode bench validate <spec>` before committing new specs.

### Troubleshooting

#### Problem: "Invalid spec: missing required field 'id'"

**Symptoms:**
- Validation error when loading spec
- Error points to missing field

**Causes:**
- Required field not included in spec
- Field name misspelled

**Solutions:**
```json
// Wrong - missing id
{
  "name": "Test task",
  ...
}

// Correct - id present
{
  "id": "BENCH-001",
  "name": "Test task",
  ...
}
```

#### Problem: "Invalid category: 'codegen'"

**Symptoms:**
- Validation error for category field
- Category not in enum

**Causes:**
- Typo in category name
- Using non-standard category

**Solutions:**
Valid categories are: `file-ops`, `code-gen`, `refactor`, `debug`, `multi-step`

```json
// Wrong
{ "category": "codegen" }

// Correct
{ "category": "code-gen" }
```

#### Problem: "Duplicate task ID: BENCH-001"

**Symptoms:**
- Suite loading fails
- Error shows duplicate ID

**Causes:**
- Same ID used in multiple specs
- Copy-paste error

**Solutions:**
- Ensure each task has a unique ID
- Use category prefixes: `file-001`, `gen-001`

#### Problem: "Invalid timeout format"

**Symptoms:**
- Timeout parsing error
- Default timeout applied with warning

**Causes:**
- Wrong format (e.g., "60" instead of "PT60S")
- Invalid ISO 8601 duration

**Solutions:**
```json
// Wrong formats
{ "timeout": "60" }
{ "timeout": "1 minute" }
{ "timeout": "60s" }

// Correct format
{ "timeout": "PT60S" }
{ "timeout": "PT1M30S" }
```

#### Problem: "File reference not found"

**Symptoms:**
- Error loading spec with external reference
- Path not found

**Causes:**
- Referenced file doesn't exist
- Path is incorrect

**Solutions:**
```bash
# Check file exists
ls ./fixtures/large-file.ts

# Use correct relative path from spec location
{ "files": { "data.json": "@./fixtures/data.json" } }
```

#### Problem: "Spec file too large"

**Symptoms:**
- Error when loading spec
- Size limit exceeded

**Causes:**
- Large file content embedded inline
- Too many files in one spec

**Solutions:**
- Use external file references for large content
- Split into multiple specs if needed

### FAQs

**Q: Can I use YAML instead of JSON?**
A: No, only JSON is supported. YAML support may be added in the future.

**Q: How do I add a custom category?**
A: Currently only the five built-in categories are supported. Custom categories require schema modification.

**Q: Can tasks depend on other tasks?**
A: Yes, use the `dependsOn` field to specify prerequisite tasks. Dependent tasks are skipped if prerequisites fail.

**Q: How do I handle non-deterministic tasks?**
A: Use fuzzy matching in assertions, provide alternatives, and tag as `flaky` for special handling.

**Q: What's the maximum spec size?**
A: Individual specs: 1MB. Suite total: 10MB. Use external references for large content.

**Q: Can I include binary files?**
A: Yes, use base64 encoding with the `base64:` prefix.

---

## Acceptance Criteria / Definition of Done

### Metadata Requirements
- [ ] AC-001: ID field is required and validated
- [ ] AC-002: ID uniqueness enforced within suite
- [ ] AC-003: ID format validation (BENCH-NNN or category-NNN)
- [ ] AC-004: Name field is required
- [ ] AC-005: Category field is required
- [ ] AC-006: Category enum validated (5 values)
- [ ] AC-007: Tags field optional, array when present
- [ ] AC-008: Description field optional, string when present
- [ ] AC-009: Difficulty field optional, enum when present
- [ ] AC-010: Author/created/version fields optional

### Input Requirements
- [ ] AC-011: Input field is required
- [ ] AC-012: Prompt field is required within input
- [ ] AC-013: Prompt must be non-empty string
- [ ] AC-014: Files field optional, object when present
- [ ] AC-015: Inline file content works
- [ ] AC-016: External file references work (@path)
- [ ] AC-017: File size limits enforced
- [ ] AC-018: Context field optional, object when present
- [ ] AC-019: Workspace field optional

### Expected Requirements
- [ ] AC-020: Expected field is required
- [ ] AC-021: Outcome field is required
- [ ] AC-022: Outcome enum validated (success/failure/partial)
- [ ] AC-023: toolCalls field optional, array when present
- [ ] AC-024: forbiddenCalls field optional, array when present
- [ ] AC-025: Assertions field optional, array when present
- [ ] AC-026: Assertion types validated
- [ ] AC-027: Alternatives field optional, array when present
- [ ] AC-028: Semantic matching works

### Execution Requirements
- [ ] AC-029: Timeout field is required
- [ ] AC-030: Default timeout applied when missing
- [ ] AC-031: ISO 8601 duration parsing works
- [ ] AC-032: Max timeout enforced
- [ ] AC-033: Retries field optional, default 0
- [ ] AC-034: Environment field optional
- [ ] AC-035: Isolated field defaults to true

### Validation Requirements
- [ ] AC-036: JSON Schema validation works
- [ ] AC-037: Invalid specs rejected with clear error
- [ ] AC-038: Error location (line/column) reported
- [ ] AC-039: Missing field errors specify field name
- [ ] AC-040: Duplicate ID detection works

### Quality Requirements
- [ ] AC-041: Unit tests achieve 80%+ coverage
- [ ] AC-042: Integration tests pass
- [ ] AC-043: Cross-platform tested
- [ ] AC-044: Documentation complete
- [ ] AC-045: Schema file published

---

## User Verification Scenarios

### Scenario 1: Create Minimal Task Spec
**Persona:** Developer creating first benchmark task  
**Preconditions:** JSON editor available, schema known  
**Steps:**
1. Create new JSON file
2. Add required fields (id, name, category, input, expected, timeout)
3. Run validation
4. Task validates successfully

**Verification Checklist:**
- [ ] Minimal spec validates
- [ ] All required fields present
- [ ] No validation errors
- [ ] Spec can be loaded

### Scenario 2: Task with Inline Files
**Persona:** Developer testing file operations  
**Preconditions:** Task needs input files  
**Steps:**
1. Add files section to input
2. Embed file content inline
3. Reference files in expected
4. Validate and run

**Verification Checklist:**
- [ ] Files section works
- [ ] Content embedded correctly
- [ ] Files available during execution
- [ ] Expected can reference files

### Scenario 3: Task with Assertions
**Persona:** Developer defining complex success criteria  
**Preconditions:** Multiple success conditions needed  
**Steps:**
1. Add assertions array
2. Define exists assertion
3. Define contains assertion
4. Define regex assertion
5. Validate and run

**Verification Checklist:**
- [ ] Multiple assertions work
- [ ] exists type works
- [ ] contains type works
- [ ] matches type works
- [ ] All must pass for success

### Scenario 4: Invalid Spec Handling
**Persona:** Developer making mistakes  
**Preconditions:** Spec with errors  
**Steps:**
1. Create spec missing required field
2. Run validation
3. Observe error message
4. Fix error and revalidate

**Verification Checklist:**
- [ ] Error detected
- [ ] Message specifies missing field
- [ ] Line/column shown
- [ ] Fix resolves error

### Scenario 5: External File Reference
**Persona:** Developer with large test files  
**Preconditions:** Large fixture file exists  
**Steps:**
1. Create spec with @path reference
2. Ensure referenced file exists
3. Validate spec
4. Run task, verify file content loaded

**Verification Checklist:**
- [ ] Reference syntax works
- [ ] File loaded correctly
- [ ] Content available in task
- [ ] Missing file error is clear

### Scenario 6: Alternatives for Valid Variations
**Persona:** Developer allowing multiple solutions  
**Preconditions:** Multiple acceptable outcomes  
**Steps:**
1. Define primary expected
2. Add alternatives array
3. Each alternative has assertions
4. Run with different solutions

**Verification Checklist:**
- [ ] Primary match = pass
- [ ] Alternative match = pass
- [ ] No match = fail
- [ ] Any one matching suffices

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-046a-01 | Parse valid minimal spec | FR-046a-01-06 |
| UT-046a-02 | Reject missing ID | FR-046a-01 |
| UT-046a-03 | Reject empty ID | FR-046a-02 |
| UT-046a-04 | Validate ID format | FR-046a-04 |
| UT-046a-05 | Reject missing name | FR-046a-05 |
| UT-046a-06 | Reject invalid category | FR-046a-09 |
| UT-046a-07 | Accept all valid categories | FR-046a-10-14 |
| UT-046a-08 | Parse optional tags | FR-046a-15 |
| UT-046a-09 | Reject non-array tags | FR-046a-16 |
| UT-046a-10 | Reject missing input | FR-046a-26 |
| UT-046a-11 | Reject missing prompt | FR-046a-28 |
| UT-046a-12 | Parse inline files | FR-046a-35 |
| UT-046a-13 | Resolve file references | FR-046a-36 |
| UT-046a-14 | Reject missing expected | FR-046a-51 |
| UT-046a-15 | Reject invalid outcome | FR-046a-54 |
| UT-046a-16 | Parse tool calls | FR-046a-58 |
| UT-046a-17 | Parse assertions | FR-046a-70 |
| UT-046a-18 | Parse timeout ISO 8601 | FR-046a-82 |
| UT-046a-19 | Apply default timeout | FR-046a-84 |
| UT-046a-20 | Enforce max timeout | FR-046a-85 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-046a-01 | Load suite with specs E2E | Task 046 |
| IT-046a-02 | Full spec parsing E2E | E2E |
| IT-046a-03 | Inline file extraction | FR-046a-35 |
| IT-046a-04 | External file loading | FR-046a-36 |
| IT-046a-05 | Large suite (100 specs) | NFR-046a-03 |
| IT-046a-06 | Cross-platform paths | NFR-046a-15 |
| IT-046a-07 | UTF-8 content | NFR-046a-14 |
| IT-046a-08 | Backward compatibility | NFR-046a-16 |
| IT-046a-09 | Duplicate ID detection | NFR-046a-21 |
| IT-046a-10 | Validation logging | NFR-046a-26 |
| IT-046a-11 | Invalid JSON handling | NFR-046a-24 |
| IT-046a-12 | Assertion evaluation | FR-046a-72 |
| IT-046a-13 | Alternatives matching | FR-046a-77 |
| IT-046a-14 | Context propagation | FR-046a-43 |
| IT-046a-15 | Path traversal prevention | NFR-046a-23 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Evaluation/
│       ├── TaskSpec.cs              # Complete spec entity
│       ├── TaskMetadata.cs          # ID, name, category, tags
│       ├── TaskInput.cs             # Prompt, files, context
│       ├── TaskExpected.cs          # Outcome, toolCalls, assertions
│       ├── TaskAssertion.cs         # Assertion types
│       ├── TaskCategory.cs          # Category enum
│       ├── TaskDifficulty.cs        # Difficulty enum
│       ├── ExecutionParams.cs       # Timeout, retries, isolation
│       └── FileReference.cs         # @path resolution
├── Acode.Application/
│   └── Evaluation/
│       ├── ISpecParser.cs           # Parser interface
│       ├── ISpecValidator.cs        # Validator interface
│       ├── IFileResolver.cs         # Reference resolver
│       └── SpecParseResult.cs       # Parse result with errors
├── Acode.Infrastructure/
│   └── Evaluation/
│       ├── JsonSpecParser.cs        # JSON parsing
│       ├── JsonSchemaValidator.cs   # Schema validation
│       ├── FileReferenceResolver.cs # @path resolution
│       └── SpecValidationError.cs   # Error with location
├── data/
│   └── schemas/
│       └── task-spec-v1.schema.json # JSON Schema
```

### JSON Schema (task-spec-v1.schema.json)

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://acode.dev/schemas/task-spec-v1.json",
  "title": "Benchmark Task Specification",
  "description": "Schema for Acode benchmark task specifications",
  "type": "object",
  "required": ["id", "name", "category", "input", "expected", "timeout"],
  "additionalProperties": false,
  "properties": {
    "id": {
      "type": "string",
      "pattern": "^[A-Za-z][A-Za-z0-9-]*[0-9]+$",
      "description": "Unique task identifier"
    },
    "name": {
      "type": "string",
      "minLength": 1,
      "maxLength": 100,
      "description": "Human-readable task name"
    },
    "category": {
      "type": "string",
      "enum": ["file-ops", "code-gen", "refactor", "debug", "multi-step"],
      "description": "Task classification"
    },
    "tags": {
      "type": "array",
      "items": { "type": "string", "pattern": "^[a-z0-9-]+$" },
      "uniqueItems": true,
      "description": "Labels for filtering"
    },
    "description": {
      "type": "string",
      "description": "Detailed task explanation"
    },
    "difficulty": {
      "type": "string",
      "enum": ["easy", "medium", "hard"],
      "description": "Task complexity level"
    },
    "author": { "type": "string" },
    "created": { "type": "string", "format": "date-time" },
    "version": { "type": "string", "pattern": "^[0-9]+\\.[0-9]+\\.[0-9]+$" },
    "input": {
      "type": "object",
      "required": ["prompt"],
      "additionalProperties": false,
      "properties": {
        "prompt": {
          "type": "string",
          "minLength": 1,
          "description": "User request to agent"
        },
        "files": {
          "type": "object",
          "additionalProperties": { "type": "string" },
          "description": "Input files (path → content)"
        },
        "context": {
          "type": "object",
          "description": "Additional context"
        },
        "workspace": {
          "type": "object",
          "description": "Directory structure"
        }
      }
    },
    "expected": {
      "type": "object",
      "required": ["outcome"],
      "additionalProperties": false,
      "properties": {
        "outcome": {
          "type": "string",
          "enum": ["success", "failure", "partial"]
        },
        "toolCalls": {
          "type": "array",
          "items": {
            "oneOf": [
              { "type": "string" },
              {
                "type": "object",
                "required": ["name"],
                "properties": {
                  "name": { "type": "string" },
                  "args": { "type": "object" },
                  "order": { "type": "integer" }
                }
              }
            ]
          }
        },
        "forbiddenCalls": {
          "type": "array",
          "items": { "type": "string" }
        },
        "ordered": { "type": "boolean", "default": false },
        "output": { "type": "object" },
        "assertions": {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["type"],
            "properties": {
              "type": { "enum": ["exists", "contains", "matches", "equals"] },
              "path": { "type": "string" },
              "value": { "type": "string" },
              "pattern": { "type": "string" }
            }
          }
        },
        "alternatives": {
          "type": "array",
          "items": { "$ref": "#/properties/expected" }
        }
      }
    },
    "timeout": {
      "type": "string",
      "pattern": "^PT([0-9]+H)?([0-9]+M)?([0-9]+S)?$",
      "default": "PT60S",
      "description": "ISO 8601 duration"
    },
    "retries": {
      "type": "integer",
      "minimum": 0,
      "maximum": 3,
      "default": 0
    },
    "environment": {
      "type": "object",
      "additionalProperties": { "type": "string" }
    },
    "skip": {
      "oneOf": [
        { "type": "boolean" },
        { "type": "object", "properties": { "reason": { "type": "string" } } }
      ]
    },
    "dependsOn": {
      "type": "array",
      "items": { "type": "string" }
    },
    "isolated": {
      "type": "boolean",
      "default": true
    }
  }
}
```

**End of Task 046.a Specification**
