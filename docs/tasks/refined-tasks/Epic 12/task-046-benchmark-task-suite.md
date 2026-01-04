# Task 046: Benchmark Task Suite

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 045 (Performance Harness), Task 030 (CLI), Task 025 (Tool Executor), Task 020 (Sandbox)  

---

## Description

### Overview

Task 046 establishes the benchmark task suite—a collection of standardized coding tasks that measure Acode's capabilities across real-world scenarios. Unlike synthetic microbenchmarks (Task 045.a) that measure isolated operations, the benchmark task suite tests end-to-end capabilities: file operations, code generation, refactoring, debugging, and multi-step workflows. The suite serves as the objective measure of "does Acode work?"

The benchmark framework is essential for continuous quality assurance. Without objective measurement, there is no way to detect regressions, compare model configurations, or validate that changes improve rather than degrade capability. Every agent release MUST pass the benchmark suite before deployment.

### Business Value

The benchmark task suite provides critical value across multiple dimensions:

1. **Objective Capability Measurement:** The suite establishes a quantitative baseline for Acode's capabilities. Rather than subjective assessments ("it seems to work"), benchmarks provide concrete metrics: pass rate, runtime, iteration count. This objectivity enables data-driven decisions about agent configuration and model selection.

2. **Regression Detection:** When prompts change, models are updated, or code is refactored, regressions can occur silently. The benchmark suite catches these regressions immediately. A drop in pass rate from 92% to 85% is visible, measurable, and actionable. Without benchmarks, regressions go undetected until users report problems.

3. **Model Comparison:** Different local models (Llama, Mistral, CodeLlama) have different capabilities. The benchmark suite enables objective comparison: which model performs best for code generation? Which is fastest? Which has the highest first-attempt success rate? These comparisons inform model selection and configuration.

4. **Configuration Validation:** Agent behavior depends on many configuration parameters: temperature, timeout, retry limits, prompt templates. The benchmark suite validates that configuration changes improve results. Before deploying a new configuration, run the suite and verify pass rate improves or holds steady.

5. **Release Confidence:** The benchmark suite is the final gate before release. A passing suite means the agent works as expected across a comprehensive set of scenarios. This confidence enables faster, safer releases.

6. **Continuous Improvement:** By tracking benchmark results over time, teams can measure improvement. Pass rate trending from 85% to 90% to 95% demonstrates concrete progress. This measurement enables goal-setting and progress tracking.

### Scope Boundaries

This task establishes the core benchmark framework including suite definition, task specification model, runner execution engine, and result collection. Related concerns are delegated to subtasks:

- **Task 046.a:** Task spec storage format (JSON schema, file organization)
- **Task 046.b:** Runner CLI interface (`acode bench run`, options, output)
- **Task 046.c:** JSON result format (structured output schema)
- **Task 047:** Scoring and promotion gates (pass/fail thresholds, gating rules)
- **Task 048:** Baseline management (golden baselines, regression detection)

### Integration Points

| Component | Integration Type | Interface | Data Flow | Notes |
|-----------|------------------|-----------|-----------|-------|
| Task 045 Performance Harness | Upstream | IPerformanceHarness | Metrics collection | Runtime, memory, tokens |
| Task 025 Tool Executor | Internal | IToolExecutor | Tool call execution | Validates tool usage |
| Task 030 CLI Framework | Surface | ICommand | User interface | `acode bench` commands |
| Task 024 Local LLM | Internal | IModelClient | Model inference | Target under test |
| Task 020 Sandbox | Internal | ISandbox | Isolation | Task execution container |
| Task 047 Scoring | Downstream | IScorer | Result evaluation | Pass/fail determination |
| Task 048 Baseline | Downstream | IBaselineStore | Comparison | Regression detection |
| Task 003 DI Container | Infrastructure | IServiceProvider | Dependency injection | Service resolution |
| Task 002 Config | Configuration | IConfiguration | Settings | Suite configuration |

### Failure Modes

| Failure | Detection | Impact | Recovery | User Impact |
|---------|-----------|--------|----------|-------------|
| Spec parse error | JSON schema validation | Cannot load task | Reject spec with error | Task skipped, clear error |
| Task timeout | Timer expiration | Partial execution | Abort, capture partial | Result = timeout |
| Tool failure | Exception during tool call | Task fails | Record error, continue | Result = error |
| LLM unavailable | Health check fails | Cannot execute | Retry with backoff | Blocks until available |
| Sandbox creation fails | Container start error | Cannot isolate | Fall back to local | Warning, reduced isolation |
| Disk full | Write fails | Cannot save results | Alert, pause | Results may be lost |
| Memory exhaustion | OOM exception | Runner crashes | Limit task parallelism | Suite aborted |
| Network timeout | Connection timeout | Tool calls fail | Retry with backoff | Increased latency |
| Invalid expected | Schema validation | Cannot evaluate | Reject spec | Task skipped |
| Concurrent modification | File lock | Suite file changed | Reload or fail | Inconsistent results |

### Assumptions

1. **Task Determinism:** Given the same input, a well-designed benchmark task has a predictable expected outcome. Non-deterministic tasks (those depending on external state) should be marked as such and evaluated with relaxed criteria.

2. **Sandbox Availability:** The execution sandbox (Task 020) is available and functional. Tasks execute in isolated environments to prevent cross-contamination and ensure reproducibility.

3. **LLM Availability:** The local LLM (Task 024) is running and responsive. The benchmark suite validates LLM health before starting and reports clearly if the model is unavailable.

4. **Schema Stability:** The task specification schema (Task 046.a) is stable. Schema changes require migration of existing task specs.

5. **Category Taxonomy:** The task category taxonomy is defined and stable. Categories enable filtering, reporting, and analysis by capability area.

6. **Timeout Reasonableness:** Default timeouts are reasonable for the task category. Simple file operations complete in seconds; complex multi-step workflows may need minutes.

7. **Tool Availability:** All tools referenced in task specs are implemented and available. Missing tools cause task failures.

8. **Sufficient Resources:** The system has sufficient CPU, memory, and disk for benchmark execution. Resource exhaustion causes suite failures.

9. **Isolation Guarantee:** Tasks do not interfere with each other. Side effects from one task do not affect subsequent tasks.

10. **Result Persistence:** Results are persisted reliably. Partial results are saved even if the suite is interrupted.

### Security Considerations

1. **Task Sandboxing:** All benchmark tasks execute in sandboxed environments. Tasks MUST NOT have access to the host filesystem outside the designated workspace. This prevents malicious or buggy tasks from causing damage.

2. **Network Isolation:** Benchmark tasks SHOULD execute with network isolation where possible. Tasks that require network access MUST be explicitly marked and reviewed.

3. **Resource Limits:** Each task has resource limits (memory, CPU, disk). Tasks exceeding limits are terminated. This prevents resource exhaustion attacks or runaway tasks.

4. **Input Validation:** Task specifications are validated against the JSON schema before execution. Malformed specs are rejected. This prevents injection attacks via crafted task files.

5. **Output Sanitization:** Task output is captured but sanitized before storage. Potential secrets (API keys, passwords) are redacted using pattern matching (Task 038).

6. **Timeout Enforcement:** All tasks have mandatory timeouts. Infinite loops or hung processes are terminated. This ensures the suite always completes.

7. **Audit Trail:** All benchmark executions are logged with full context: who ran the suite, when, what configuration, what results. This enables forensic analysis and compliance.

8. **Privilege Minimization:** The benchmark runner executes with minimum necessary privileges. It does not require root/admin access. Tasks inherit these restricted privileges.

9. **Reproducibility:** Benchmark results include environment checksums and configuration snapshots. This enables verification that results came from a known, trusted configuration.

10. **Access Control:** Custom task specs can only be added by authorized users. Malicious task injection is prevented through file permission controls.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Benchmark Task | A standardized coding scenario with defined input, expected outcome, and evaluation criteria |
| Task Spec | The declarative JSON definition of a benchmark task including prompt, files, and expectations |
| Runner | The execution engine that loads specs, runs tasks, and collects results |
| Suite | A collection of benchmark tasks grouped for batch execution |
| Expected Output | The success criteria defining what constitutes a passing task |
| Actual Output | The captured result of task execution including tool calls and outputs |
| Evaluation | The process of comparing actual vs expected to determine pass/fail |
| Category | A classification of task type (file-ops, code-gen, refactor, debug, multi-step) |
| Pass | Task execution met all expected criteria |
| Fail | Task execution did not meet one or more expected criteria |
| Timeout | Task exceeded its time limit and was aborted |
| Skip | Task was intentionally not run (filtered, disabled, or prerequisite failed) |
| Error | Task encountered an unexpected exception during execution |
| Pass Rate | Percentage of tasks that passed: passed / (passed + failed) |
| First-Attempt Rate | Percentage of tasks that passed without retries |
| Iteration | A single attempt to complete a task (first attempt = iteration 1) |
| Baseline | A recorded set of results used as the reference for comparison |
| Regression | A decrease in pass rate or increase in runtime compared to baseline |

---

## Out of Scope

The following items are explicitly excluded from Task 046:

- **Scoring and Gates** — See Task 047 for pass/fail thresholds, gating rules, and promotion logic
- **Baseline Management** — See Task 048 for golden baselines, change tracking, and triage workflow
- **Performance Optimization** — This task defines the framework; optimization is ongoing work
- **Task Generation** — Tasks are manually authored; automatic generation is future work
- **Dynamic Task Creation** — Tasks are static specs; runtime generation is not supported
- **Multi-Model Comparison** — Single-model runs only; A/B comparison is future work
- **Distributed Execution** — Single-machine execution; cluster support is future work
- **Real-time Streaming** — Results are batch; streaming updates are Task 046.b concern

---

## Functional Requirements

### FR-001 to FR-020: Suite Definition

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046-01 | System MUST define a BenchmarkSuite entity | P0 |
| FR-046-02 | BenchmarkSuite MUST have a unique ID (string, e.g., "default-v1") | P0 |
| FR-046-03 | BenchmarkSuite MUST have a semantic version (major.minor.patch) | P0 |
| FR-046-04 | BenchmarkSuite MUST have a human-readable name | P0 |
| FR-046-05 | BenchmarkSuite MUST have a description (optional but recommended) | P1 |
| FR-046-06 | BenchmarkSuite MUST contain a list of BenchmarkTask entities | P0 |
| FR-046-07 | BenchmarkSuite MUST specify task execution order | P0 |
| FR-046-08 | BenchmarkSuite MUST be loadable from filesystem (JSON files) | P0 |
| FR-046-09 | BenchmarkSuite file format MUST be JSON | P0 |
| FR-046-10 | BenchmarkSuite MUST be validated against JSON schema on load | P0 |
| FR-046-11 | Invalid BenchmarkSuite MUST be rejected with clear error message | P0 |
| FR-046-12 | System MUST support multiple suites in the benchmark directory | P0 |
| FR-046-13 | System MUST support suite selection by ID or path | P0 |
| FR-046-14 | System MUST provide a default suite (default-suite.json) | P0 |
| FR-046-15 | System MUST support custom suites in user-specified locations | P1 |
| FR-046-16 | BenchmarkSuite MUST have metadata (author, created date, modified date) | P1 |
| FR-046-17 | BenchmarkSuite MUST support task filtering by category | P0 |
| FR-046-18 | BenchmarkSuite MUST support task filtering by ID pattern | P0 |
| FR-046-19 | BenchmarkSuite MUST support task filtering by tag | P1 |
| FR-046-20 | BenchmarkSuite MUST be immutable during execution | P0 |

### FR-021 to FR-045: Task Specification

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046-21 | System MUST define a BenchmarkTask entity | P0 |
| FR-046-22 | BenchmarkTask MUST have a unique ID within the suite | P0 |
| FR-046-23 | BenchmarkTask ID MUST follow pattern BENCH-NNN or category-NNN | P0 |
| FR-046-24 | BenchmarkTask MUST have a human-readable name | P0 |
| FR-046-25 | BenchmarkTask MUST have a category | P0 |
| FR-046-26 | Categories MUST include "file-ops" (file operations) | P0 |
| FR-046-27 | Categories MUST include "code-gen" (code generation) | P0 |
| FR-046-28 | Categories MUST include "refactor" (code refactoring) | P0 |
| FR-046-29 | Categories MUST include "debug" (debugging and fixing) | P0 |
| FR-046-30 | Categories MUST include "multi-step" (complex workflows) | P0 |
| FR-046-31 | Categories MAY include additional custom categories | P1 |
| FR-046-32 | BenchmarkTask MUST have input specification | P0 |
| FR-046-33 | Input MUST include a prompt (string, the user request) | P0 |
| FR-046-34 | Input MAY include files (map of path → content) | P0 |
| FR-046-35 | Input MAY include context (additional context strings) | P1 |
| FR-046-36 | Input MAY include configuration overrides | P1 |
| FR-046-37 | BenchmarkTask MUST have expected specification | P0 |
| FR-046-38 | Expected MUST define outcome (success, failure, error) | P0 |
| FR-046-39 | Expected MAY define required tool calls (list of tool names) | P0 |
| FR-046-40 | Expected MAY define forbidden tool calls (tools that must NOT be called) | P0 |
| FR-046-41 | Expected MAY define output assertions (regex, contains, exact) | P1 |
| FR-046-42 | BenchmarkTask MUST have timeout (ISO 8601 duration) | P0 |
| FR-046-43 | Default timeout MUST be 60 seconds (PT60S) | P0 |
| FR-046-44 | Timeout MUST be overridable per-task | P0 |
| FR-046-45 | BenchmarkTask MAY have tags for filtering and grouping | P1 |

### FR-046 to FR-070: Execution Engine

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046-46 | System MUST implement IEvaluationRunner interface | P0 |
| FR-046-47 | Runner MUST execute tasks according to suite order | P0 |
| FR-046-48 | Runner MUST support single-task execution by ID | P0 |
| FR-046-49 | Runner MUST support batch execution of entire suite | P0 |
| FR-046-50 | Runner MUST support filtered execution (by category, tag, pattern) | P0 |
| FR-046-51 | Execution MUST be sequential by default | P0 |
| FR-046-52 | Parallel execution MAY be supported with --parallel flag | P2 |
| FR-046-53 | Each task MUST execute in an isolated sandbox | P0 |
| FR-046-54 | Sandbox MUST be created before task execution | P0 |
| FR-046-55 | Sandbox MUST be destroyed after task execution | P0 |
| FR-046-56 | Sandbox cleanup MUST occur even on task failure | P0 |
| FR-046-57 | Timeout MUST be enforced during task execution | P0 |
| FR-046-58 | Timeout expiration MUST abort task gracefully | P0 |
| FR-046-59 | Graceful abort MUST send SIGINT, wait 5s, then SIGKILL | P0 |
| FR-046-60 | Errors during execution MUST be captured, not thrown | P0 |
| FR-046-61 | Exceptions MUST NOT crash the runner | P0 |
| FR-046-62 | Progress MUST be reported during execution | P0 |
| FR-046-63 | Progress MUST include current task, completed count, remaining count | P0 |
| FR-046-64 | Cancellation MUST be supported via CancellationToken | P0 |
| FR-046-65 | Cancellation MUST stop after current task completes | P0 |
| FR-046-66 | Cancellation MUST save partial results | P0 |
| FR-046-67 | Runner MUST validate LLM health before starting | P0 |
| FR-046-68 | Runner MUST validate sandbox availability before starting | P0 |
| FR-046-69 | Runner MUST log execution start and end events | P0 |
| FR-046-70 | Runner MUST emit metrics for observability | P0 |

### FR-071 to FR-090: Evaluation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046-71 | System MUST implement ITaskEvaluator interface | P0 |
| FR-046-72 | Evaluator MUST capture actual task output | P0 |
| FR-046-73 | Actual output MUST include all tool calls made | P0 |
| FR-046-74 | Actual output MUST include tool call results | P0 |
| FR-046-75 | Actual output MUST include final response text | P0 |
| FR-046-76 | Evaluator MUST compare actual vs expected | P0 |
| FR-046-77 | Outcome matching MUST check expected outcome (success/failure) | P0 |
| FR-046-78 | Tool call matching MUST verify required tools were called | P0 |
| FR-046-79 | Tool call matching MUST verify forbidden tools were NOT called | P0 |
| FR-046-80 | Tool call comparison MUST be semantic (order-independent) | P0 |
| FR-046-81 | Output assertion matching MUST apply regex/contains/exact checks | P0 |
| FR-046-82 | Pass/fail MUST be determined from all criteria | P0 |
| FR-046-83 | Pass = ALL criteria met | P0 |
| FR-046-84 | Fail = ANY criteria not met | P0 |
| FR-046-85 | Partial credit MAY apply for multi-criteria tasks | P2 |
| FR-046-86 | Failure reason MUST be captured and reported | P0 |
| FR-046-87 | Failure reason MUST be specific (which criterion failed) | P0 |
| FR-046-88 | Runtime MUST be captured (start to finish, milliseconds) | P0 |
| FR-046-89 | Iteration count MUST be captured (attempts needed) | P0 |
| FR-046-90 | Token usage MUST be captured (prompt tokens, completion tokens) | P0 |

### FR-091 to FR-110: Results

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046-91 | System MUST define EvaluationResult entity | P0 |
| FR-046-92 | EvaluationResult MUST include task ID | P0 |
| FR-046-93 | EvaluationResult MUST include status (pass/fail/timeout/error/skip) | P0 |
| FR-046-94 | EvaluationResult MUST include runtime (milliseconds) | P0 |
| FR-046-95 | EvaluationResult MUST include iteration count | P0 |
| FR-046-96 | EvaluationResult MUST include token usage | P0 |
| FR-046-97 | EvaluationResult MUST include failure reason (if applicable) | P0 |
| FR-046-98 | EvaluationResult MUST include actual output summary | P0 |
| FR-046-99 | EvaluationResult MUST include timestamp | P0 |
| FR-046-100 | System MUST define SuiteResult entity for batch results | P0 |
| FR-046-101 | SuiteResult MUST include suite ID and version | P0 |
| FR-046-102 | SuiteResult MUST include run ID (unique per execution) | P0 |
| FR-046-103 | SuiteResult MUST include start and end timestamps | P0 |
| FR-046-104 | SuiteResult MUST include list of EvaluationResult | P0 |
| FR-046-105 | SuiteResult MUST include summary statistics | P0 |
| FR-046-106 | Summary MUST include total count | P0 |
| FR-046-107 | Summary MUST include pass count | P0 |
| FR-046-108 | Summary MUST include fail count | P0 |
| FR-046-109 | Summary MUST include timeout count | P0 |
| FR-046-110 | Summary MUST include pass rate (percentage) | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Maximum | Priority |
|----|-------------|--------|---------|----------|
| NFR-046-01 | Suite loading time (100 tasks) | <200ms | 500ms | P0 |
| NFR-046-02 | Task spec parsing time (single task) | <5ms | 10ms | P0 |
| NFR-046-03 | Sandbox setup time per task | <500ms | 1000ms | P0 |
| NFR-046-04 | Sandbox teardown time per task | <500ms | 1000ms | P0 |
| NFR-046-05 | Runner overhead per task | <2% | 5% | P0 |
| NFR-046-06 | Memory usage per task | <50MB | 100MB | P0 |
| NFR-046-07 | Full suite completion (50 tasks) | <30min | 60min | P0 |
| NFR-046-08 | Progress update latency | <50ms | 100ms | P0 |
| NFR-046-09 | Result write latency | <20ms | 50ms | P0 |
| NFR-046-10 | Cancellation response time | <500ms | 1000ms | P0 |
| NFR-046-11 | JSON schema validation time | <10ms | 50ms | P0 |
| NFR-046-12 | Result serialization time | <5ms | 20ms | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046-13 | Task isolation guarantee | 100% | P0 |
| NFR-046-14 | Sandbox cleanup success rate | 99.9% | P0 |
| NFR-046-15 | Crash recovery (resume from partial) | Full | P0 |
| NFR-046-16 | Partial results saved on interrupt | Always | P0 |
| NFR-046-17 | Cross-platform compatibility | Windows, Linux, macOS | P0 |
| NFR-046-18 | Result reproducibility | Deterministic | P0 |
| NFR-046-19 | Timeout enforcement accuracy | ±100ms | P0 |
| NFR-046-20 | Error capture rate | 100% | P0 |
| NFR-046-21 | Schema validation accuracy | 100% | P0 |
| NFR-046-22 | Suite file integrity verification | Checksum | P0 |
| NFR-046-23 | Concurrent execution safety | Thread-safe | P0 |
| NFR-046-24 | Resource cleanup on exception | Guaranteed | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046-25 | Suite start event logged | Info level | P0 |
| NFR-046-26 | Task start event logged | Debug level | P0 |
| NFR-046-27 | Task result event logged | Info level | P0 |
| NFR-046-28 | Suite completion event logged | Info level | P0 |
| NFR-046-29 | Errors logged with context | Error level | P0 |
| NFR-046-30 | Metric: tasks_executed_total | Counter | P0 |
| NFR-046-31 | Metric: tasks_passed_total | Counter | P0 |
| NFR-046-32 | Metric: tasks_failed_total | Counter | P0 |
| NFR-046-33 | Metric: task_duration_seconds | Histogram | P0 |
| NFR-046-34 | Metric: suite_pass_rate | Gauge | P0 |
| NFR-046-35 | Structured logging format | JSON | P0 |
| NFR-046-36 | Trace ID per suite run | UUID | P1 |

---

## User Manual Documentation

### Overview

The Benchmark Task Suite is Acode's quality assurance system. It runs a collection of standardized coding tasks against the agent and measures pass rate, runtime, and other metrics. Use benchmarks to validate agent behavior, detect regressions, and compare configurations.

### Quick Start

```bash
# Run the default benchmark suite
acode bench run

# Run with verbose output
acode bench run --verbose

# Run specific category only
acode bench run --category code-gen

# Run specific task by ID
acode bench run --task BENCH-001

# Run custom suite
acode bench run --suite ./my-suite.json
```

### Configuration

Configure benchmark behavior in `.agent/config.yml`:

```yaml
# .agent/config.yml
benchmark:
  # Directory containing benchmark suites and tasks
  # Default: .benchmarks in repository root
  directory: .benchmarks
  
  # Default suite to run when no --suite specified
  # Must be a suite ID or relative path
  default_suite: default-v1
  
  # Default timeout for tasks (ISO 8601 duration)
  # Individual tasks can override this
  default_timeout: PT60S
  
  # Maximum timeout allowed for any task
  max_timeout: PT300S
  
  # Number of retries for flaky tasks
  # Set to 0 for strict first-attempt evaluation
  max_retries: 2
  
  # Parallel execution settings
  parallel:
    # Enable parallel task execution
    enabled: false
    # Maximum concurrent tasks
    max_concurrent: 4
  
  # Sandbox settings
  sandbox:
    # Use Docker sandbox for isolation
    use_docker: true
    # Docker image for benchmark sandbox
    image: acode-bench:latest
    # Memory limit per task
    memory_limit: 512m
    # CPU limit per task
    cpu_limit: 1.0
  
  # Output settings
  output:
    # Directory for result files
    results_dir: .benchmarks/results
    # Include full logs in results (can be large)
    include_logs: false
    # Retention for old results (days)
    retention_days: 30
  
  # Observability settings
  observability:
    # Emit structured log events
    structured_logging: true
    # Log level for benchmark events
    log_level: info
    # Emit metrics
    metrics_enabled: true
```

### CLI Commands

#### Run Benchmarks

```bash
# Run the default suite
acode bench run

# Run a specific suite by ID
acode bench run --suite my-custom-suite

# Run a suite from a file path
acode bench run --suite ./benchmarks/regression-suite.json

# Run only tasks in a specific category
acode bench run --category file-ops
acode bench run --category code-gen
acode bench run --category refactor
acode bench run --category debug
acode bench run --category multi-step

# Run a single task by ID
acode bench run --task BENCH-042

# Run tasks matching a pattern
acode bench run --pattern "BENCH-00*"

# Run tasks with specific tags
acode bench run --tag regression
acode bench run --tag smoke-test

# Run with timeout override
acode bench run --timeout 120

# Run in parallel mode
acode bench run --parallel --max-concurrent 4

# Run with verbose output (shows each task)
acode bench run --verbose

# Run with quiet output (only summary)
acode bench run --quiet

# Save results to specific file
acode bench run --output ./results/run-$(date +%Y%m%d).json

# Dry run (validate without executing)
acode bench run --dry-run
```

#### List Suites and Tasks

```bash
# List available suites
acode bench list

# List tasks in default suite
acode bench list --tasks

# List tasks in specific suite
acode bench list --suite my-suite --tasks

# List tasks by category
acode bench list --tasks --category code-gen

# Show task details
acode bench show BENCH-042

# Show suite details
acode bench show --suite my-suite
```

#### View Results

```bash
# Show latest run results
acode bench results

# Show results for specific run
acode bench results --run-id run-2026-01-04-001

# Show results in table format
acode bench results --format table

# Show results in JSON format
acode bench results --format json

# Show only failures
acode bench results --failed

# Show only timeouts
acode bench results --timeout

# Compare two runs
acode bench diff run-2026-01-03-001 run-2026-01-04-001
```

### Output Format

#### Console Output (Default)

```
Benchmark Suite: default-v1 (Standard Acode Capability Evaluation)
Running 50 tasks...

[1/50] BENCH-001 Read file contents ................ PASS (1.2s)
[2/50] BENCH-002 Write new file .................... PASS (2.4s)
[3/50] BENCH-003 Create directory structure ........ PASS (1.8s)
[4/50] BENCH-004 Generate TypeScript function ...... FAIL (45.2s)
        Reason: Missing required tool call: write_file
[5/50] BENCH-005 Refactor extract method ........... PASS (12.3s)
...
[50/50] BENCH-050 Multi-step workflow .............. TIMEOUT (60.0s)

═══════════════════════════════════════════════════════════════════
                         BENCHMARK RESULTS
═══════════════════════════════════════════════════════════════════
Suite:     default-v1
Run ID:    run-2026-01-04-001
Duration:  14m 32s
───────────────────────────────────────────────────────────────────
Status     Count    Percentage
───────────────────────────────────────────────────────────────────
PASS       42       84.0%
FAIL       6        12.0%
TIMEOUT    2        4.0%
ERROR      0        0.0%
SKIP       0        0.0%
───────────────────────────────────────────────────────────────────
TOTAL      50       Pass Rate: 84.0%
═══════════════════════════════════════════════════════════════════
```

#### JSON Output Format

See Task 046.c for complete JSON schema specification.

### Best Practices

1. **Run Before Commits:** Run the benchmark suite before committing prompt or configuration changes. Catch regressions before they reach the main branch.

2. **Use Categories:** For quick validation, run only the relevant category. Use `--category code-gen` when working on code generation prompts.

3. **Create Custom Suites:** Create focused suites for specific features or regressions. A smoke-test suite with 10 critical tasks runs in minutes.

4. **Review Failures:** When tasks fail, review the failure reason and actual output. Failures are learning opportunities.

5. **Track Trends:** Compare results over time. A gradually declining pass rate indicates accumulating technical debt.

6. **Set Appropriate Timeouts:** Complex tasks need longer timeouts. Multi-step workflows may need 2-5 minutes.

7. **Tag for Organization:** Use tags like `regression`, `smoke-test`, `p0` to organize tasks. Filter by tags for targeted runs.

8. **Isolate Flaky Tasks:** If a task is inherently non-deterministic, tag it as `flaky` and consider excluding from gate checks.

### Troubleshooting

#### Problem: "Suite not found"

**Symptoms:**
- Error: "Suite 'my-suite' not found"
- Error: "Cannot load suite from path"

**Causes:**
1. Suite ID doesn't match any file
2. Suite file path is incorrect
3. Suite file is not valid JSON

**Solutions:**
```bash
# List available suites to check names
acode bench list

# Use full path to suite file
acode bench run --suite ./benchmarks/my-suite.json

# Validate suite file
acode bench validate ./benchmarks/my-suite.json
```

#### Problem: "Task timeout"

**Symptoms:**
- Task shows TIMEOUT status
- Task was aborted before completion

**Causes:**
1. Timeout too short for complex task
2. LLM is slow to respond
3. Task is stuck in a loop

**Solutions:**
```bash
# Increase timeout for specific run
acode bench run --timeout 120

# Check task definition for timeout override
cat .benchmarks/tasks/my-task.json | jq '.timeout'

# Update config for permanent change
# benchmark.default_timeout: PT120S
```

#### Problem: "Sandbox creation failed"

**Symptoms:**
- Error: "Failed to create sandbox"
- Error: "Docker not available"

**Causes:**
1. Docker not installed or not running
2. Docker image not available
3. Insufficient permissions

**Solutions:**
```bash
# Check Docker is running
docker ps

# Pull benchmark image
docker pull acode-bench:latest

# Run without sandbox (less isolation)
acode bench run --no-sandbox

# Check Docker permissions
docker run hello-world
```

#### Problem: "LLM not available"

**Symptoms:**
- Error: "LLM health check failed"
- Error: "Model not responding"

**Causes:**
1. Local LLM service not running
2. Model not loaded
3. Port conflict

**Solutions:**
```bash
# Check LLM service status
acode model status

# Start LLM service
acode model start

# Check configured endpoint
cat .agent/config.yml | grep -A5 "llm:"
```

#### Problem: "Unexpected failures"

**Symptoms:**
- Tasks that previously passed now fail
- No configuration changes made

**Causes:**
1. LLM model was updated
2. Prompt templates changed
3. Tool implementation changed
4. Non-deterministic task behavior

**Solutions:**
```bash
# Compare with previous run
acode bench diff <old-run-id> <new-run-id>

# Check for recent changes
git log --oneline -10

# Re-run failed tasks only
acode bench run --failed

# Check task is deterministic
acode bench run --task BENCH-042 --repeat 3
```

### FAQs

**Q: How long does the full suite take?**
A: The default suite with 50 tasks typically takes 15-30 minutes depending on task complexity and model speed.

**Q: Can I run benchmarks in CI/CD?**
A: Yes. Use `acode bench run --quiet --output results.json` and check the exit code. Exit 0 = all passed.

**Q: How do I add a custom task?**
A: Create a JSON file following the task spec schema (Task 046.a) and add it to your suite's tasks list.

**Q: What's a good pass rate target?**
A: Production-ready agents should achieve 90%+ on the default suite. 95%+ indicates excellent quality.

**Q: How do I debug a failing task?**
A: Run the task individually with `--verbose` and examine the actual output vs expected output in the result.

**Q: Can I skip flaky tasks?**
A: Yes. Add `"skip": true` to the task spec or use `--exclude-tag flaky` when running.

---

## Acceptance Criteria / Definition of Done

### Suite Definition
- [ ] AC-001: BenchmarkSuite entity defined with all properties
- [ ] AC-002: Suite ID uniqueness enforced
- [ ] AC-003: Suite version follows semantic versioning
- [ ] AC-004: Suite loading from JSON file works
- [ ] AC-005: Suite validation against schema works
- [ ] AC-006: Invalid suites rejected with clear errors
- [ ] AC-007: Multiple suites supported simultaneously
- [ ] AC-008: Default suite exists and is valid
- [ ] AC-009: Custom suite paths supported
- [ ] AC-010: Suite metadata (author, dates) preserved

### Task Specification
- [ ] AC-011: BenchmarkTask entity defined with all properties
- [ ] AC-012: Task ID uniqueness within suite enforced
- [ ] AC-013: All five categories implemented
- [ ] AC-014: Custom categories supported
- [ ] AC-015: Input with prompt required
- [ ] AC-016: Input with files optional
- [ ] AC-017: Expected outcome required
- [ ] AC-018: Expected tool calls optional
- [ ] AC-019: Timeout with default value
- [ ] AC-020: Tags for filtering supported

### Execution Engine
- [ ] AC-021: IEvaluationRunner interface implemented
- [ ] AC-022: Single task execution works
- [ ] AC-023: Batch suite execution works
- [ ] AC-024: Category filtering works
- [ ] AC-025: Pattern filtering works
- [ ] AC-026: Tag filtering works
- [ ] AC-027: Sandbox isolation enforced
- [ ] AC-028: Sandbox cleanup reliable
- [ ] AC-029: Timeout enforcement accurate
- [ ] AC-030: Graceful abort on timeout
- [ ] AC-031: Error capture without crash
- [ ] AC-032: Progress reporting works
- [ ] AC-033: Cancellation supported
- [ ] AC-034: Partial results saved on cancel
- [ ] AC-035: LLM health check before start

### Evaluation
- [ ] AC-036: Actual output captured completely
- [ ] AC-037: Tool calls captured
- [ ] AC-038: Outcome comparison works
- [ ] AC-039: Tool call matching works
- [ ] AC-040: Output assertions work
- [ ] AC-041: Pass determination correct
- [ ] AC-042: Fail determination correct
- [ ] AC-043: Failure reason specific
- [ ] AC-044: Runtime captured accurately
- [ ] AC-045: Iteration count tracked
- [ ] AC-046: Token usage captured

### Results
- [ ] AC-047: EvaluationResult entity complete
- [ ] AC-048: SuiteResult entity complete
- [ ] AC-049: Summary statistics calculated
- [ ] AC-050: Pass rate correct
- [ ] AC-051: Results persisted to file
- [ ] AC-052: JSON format valid
- [ ] AC-053: Results queryable by run ID

### Cross-Cutting
- [ ] AC-054: Unit tests achieve 80%+ coverage
- [ ] AC-055: Integration tests pass
- [ ] AC-056: Cross-platform tested (Windows, Linux, macOS)
- [ ] AC-057: Documentation complete
- [ ] AC-058: Error messages user-friendly
- [ ] AC-059: Logging structured and complete
- [ ] AC-060: Metrics emitted correctly

---

## User Verification Scenarios

### Scenario 1: Run Full Suite
**Persona:** Developer validating a new agent configuration  
**Preconditions:** Default suite exists, LLM is running, sandbox is available  
**Steps:**
1. Open terminal in repository root
2. Run `acode bench run`
3. Wait for suite to complete
4. Review summary output
5. Check pass rate

**Verification Checklist:**
- [ ] Suite loads without error
- [ ] Progress shows for each task
- [ ] All tasks execute (no skips)
- [ ] Summary displays correctly
- [ ] Pass rate is calculated
- [ ] Results file is created

### Scenario 2: Run Single Task for Debugging
**Persona:** Developer investigating a failing task  
**Preconditions:** Task BENCH-042 exists in suite  
**Steps:**
1. Run `acode bench run --task BENCH-042 --verbose`
2. Observe detailed execution output
3. Review actual vs expected
4. Identify failure reason

**Verification Checklist:**
- [ ] Single task runs
- [ ] Verbose output shows details
- [ ] Tool calls are visible
- [ ] Failure reason is clear
- [ ] Actual output is accessible

### Scenario 3: Category-Filtered Execution
**Persona:** Developer working on code generation prompts  
**Preconditions:** Suite contains multiple categories  
**Steps:**
1. Run `acode bench run --category code-gen`
2. Verify only code-gen tasks execute
3. Review focused results
4. Iterate on code-gen tasks

**Verification Checklist:**
- [ ] Only code-gen tasks run
- [ ] Other categories skipped
- [ ] Pass rate for category shown
- [ ] Execution time reduced

### Scenario 4: Timeout Handling
**Persona:** Developer with a slow task  
**Preconditions:** Task has a long timeout requirement  
**Steps:**
1. Run task with default timeout
2. Observe timeout
3. Run with `--timeout 180`
4. Task completes successfully

**Verification Checklist:**
- [ ] First run times out
- [ ] Timeout status reported
- [ ] Increased timeout works
- [ ] Task completes with more time

### Scenario 5: Cancellation and Partial Results
**Persona:** Developer who needs to interrupt a long run  
**Preconditions:** Suite is running  
**Steps:**
1. Start `acode bench run`
2. Wait for some tasks to complete
3. Press Ctrl+C
4. Verify partial results saved
5. Review what completed

**Verification Checklist:**
- [ ] Cancellation is graceful
- [ ] Current task completes
- [ ] Partial results saved
- [ ] Results file is valid
- [ ] Resume is possible

### Scenario 6: Custom Suite Creation
**Persona:** Developer creating a regression test suite  
**Preconditions:** Need to test specific scenarios  
**Steps:**
1. Create new suite JSON file
2. Add task definitions
3. Run `acode bench validate ./my-suite.json`
4. Run `acode bench run --suite ./my-suite.json`
5. Verify results

**Verification Checklist:**
- [ ] Custom suite loads
- [ ] Validation catches errors
- [ ] Tasks execute correctly
- [ ] Results are captured
- [ ] Suite is reusable

### Scenario 7: Compare Runs for Regression
**Persona:** Tech lead reviewing changes  
**Preconditions:** Two benchmark runs exist  
**Steps:**
1. Run benchmarks before change
2. Apply code/prompt changes
3. Run benchmarks after change
4. Use `acode bench diff` to compare
5. Review delta

**Verification Checklist:**
- [ ] Both runs have results
- [ ] Diff shows comparison
- [ ] Regressions highlighted
- [ ] Improvements highlighted
- [ ] Delta statistics accurate

### Scenario 8: CI/CD Integration
**Persona:** DevOps engineer setting up pipeline  
**Preconditions:** CI environment configured  
**Steps:**
1. Add `acode bench run --quiet --output results.json` to pipeline
2. Run pipeline
3. Check exit code
4. Parse results.json for metrics
5. Gate on pass rate

**Verification Checklist:**
- [ ] Works in headless environment
- [ ] Exit code reflects pass/fail
- [ ] JSON output is valid
- [ ] Metrics are extractable
- [ ] Gate logic works

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-046-01 | Load suite from valid JSON | FR-046-08, FR-046-09 |
| UT-046-02 | Reject invalid suite JSON | FR-046-11 |
| UT-046-03 | Parse task specification | FR-046-21-45 |
| UT-046-04 | Validate category enum | FR-046-26-31 |
| UT-046-05 | Extract input prompt and files | FR-046-33-35 |
| UT-046-06 | Parse expected specification | FR-046-38-41 |
| UT-046-07 | Apply default timeout | FR-046-43 |
| UT-046-08 | Override timeout per-task | FR-046-44 |
| UT-046-09 | Create EvaluationResult | FR-046-91-99 |
| UT-046-10 | Determine pass from all criteria | FR-046-83 |
| UT-046-11 | Determine fail from any criteria | FR-046-84 |
| UT-046-12 | Calculate summary statistics | FR-046-105-110 |
| UT-046-13 | Calculate pass rate correctly | FR-046-110 |
| UT-046-14 | Validate task ID uniqueness | FR-046-22 |
| UT-046-15 | Validate suite ID format | FR-046-02 |
| UT-046-16 | Handle empty suite | Edge case |
| UT-046-17 | Handle single-task suite | Edge case |
| UT-046-18 | Filter by category | FR-046-50 |
| UT-046-19 | Filter by pattern | FR-046-50 |
| UT-046-20 | Filter by tag | FR-046-45 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-046-01 | Full suite execution E2E | E2E |
| IT-046-02 | Single task execution | FR-046-48 |
| IT-046-03 | Batch execution | FR-046-49 |
| IT-046-04 | Timeout enforcement | FR-046-57-59 |
| IT-046-05 | Sandbox isolation | FR-046-53-56 |
| IT-046-06 | Error capture | FR-046-60-61 |
| IT-046-07 | Cancellation handling | FR-046-64-66 |
| IT-046-08 | Cross-platform (Windows) | NFR-046-17 |
| IT-046-09 | Cross-platform (Linux) | NFR-046-17 |
| IT-046-10 | Cross-platform (macOS) | NFR-046-17 |
| IT-046-11 | Partial results on interrupt | FR-046-66 |
| IT-046-12 | Custom suite loading | FR-046-15 |
| IT-046-13 | Tool executor integration | Task 025 |
| IT-046-14 | LLM integration | Task 024 |
| IT-046-15 | Logging output | NFR-046-25-29 |
| IT-046-16 | Metrics emission | NFR-046-30-34 |
| IT-046-17 | Cleanup on failure | FR-046-56 |
| IT-046-18 | Progress reporting | FR-046-62-63 |
| IT-046-19 | Result persistence | FR-046-91 |
| IT-046-20 | Health check before start | FR-046-67-68 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Evaluation/
│       ├── BenchmarkSuite.cs           # Suite entity
│       ├── BenchmarkTask.cs            # Task entity
│       ├── TaskInput.cs                # Input specification
│       ├── TaskExpected.cs             # Expected specification
│       ├── TaskCategory.cs             # Category enum
│       ├── EvaluationResult.cs         # Single task result
│       ├── EvaluationStatus.cs         # Status enum (pass/fail/timeout/error/skip)
│       ├── SuiteResult.cs              # Batch result with summary
│       └── SuiteSummary.cs             # Summary statistics
├── Acode.Application/
│   └── Evaluation/
│       ├── IEvaluationRunner.cs        # Runner interface
│       ├── ISuiteLoader.cs             # Loader interface
│       ├── ITaskEvaluator.cs           # Evaluator interface
│       ├── ITaskSandbox.cs             # Sandbox interface
│       ├── EvaluationOptions.cs        # Execution options
│       └── RunProgress.cs              # Progress reporting
├── Acode.Infrastructure/
│   └── Evaluation/
│       ├── EvaluationRunner.cs         # Runner implementation
│       ├── JsonSuiteLoader.cs          # JSON loader implementation
│       ├── TaskEvaluator.cs            # Evaluator implementation
│       ├── TaskSandbox.cs              # Sandbox implementation
│       ├── ResultPersister.cs          # Result file writer
│       └── SuiteValidator.cs           # JSON schema validator
├── Acode.Cli/
│   └── Commands/
│       └── Bench/
│           ├── BenchCommand.cs         # Parent command
│           ├── BenchRunCommand.cs      # Run subcommand
│           ├── BenchListCommand.cs     # List subcommand
│           ├── BenchShowCommand.cs     # Show subcommand
│           ├── BenchResultsCommand.cs  # Results subcommand
│           └── BenchDiffCommand.cs     # Diff subcommand
├── data/
│   └── benchmarks/
│       ├── default-suite.json          # Default benchmark suite
│       ├── schema/
│       │   ├── suite.schema.json       # Suite JSON schema
│       │   └── task.schema.json        # Task JSON schema
│       └── tasks/
│           ├── file-ops/               # File operation tasks
│           ├── code-gen/               # Code generation tasks
│           ├── refactor/               # Refactoring tasks
│           ├── debug/                  # Debugging tasks
│           └── multi-step/             # Multi-step workflow tasks
```

### Suite Format Example

```json
{
  "id": "default-v1",
  "version": "1.0.0",
  "name": "Default Benchmark Suite",
  "description": "Standard Acode capability evaluation covering file operations, code generation, refactoring, debugging, and multi-step workflows.",
  "metadata": {
    "author": "Acode Team",
    "created": "2026-01-01T00:00:00Z",
    "modified": "2026-01-04T00:00:00Z"
  },
  "tasks": [
    {
      "id": "BENCH-001",
      "name": "Read file contents",
      "category": "file-ops",
      "tags": ["smoke-test", "p0"],
      "input": {
        "prompt": "Read the contents of README.md and summarize it in 2 sentences.",
        "files": {
          "README.md": "# My Project\n\nThis is a sample project for testing Acode capabilities.\n\n## Features\n- Fast\n- Reliable\n- Extensible"
        }
      },
      "expected": {
        "outcome": "success",
        "toolCalls": ["read_file"],
        "forbiddenCalls": ["write_file", "delete_file"]
      },
      "timeout": "PT30S"
    },
    {
      "id": "BENCH-002",
      "name": "Create new TypeScript function",
      "category": "code-gen",
      "tags": ["core"],
      "input": {
        "prompt": "Create a TypeScript function called 'add' that takes two numbers and returns their sum. Write it to src/math.ts."
      },
      "expected": {
        "outcome": "success",
        "toolCalls": ["write_file"],
        "outputAssertions": [
          {"type": "contains", "value": "function add"},
          {"type": "regex", "pattern": ":\\s*number\\s*=>?"}
        ]
      },
      "timeout": "PT60S"
    }
  ]
}
```

### Error Codes

| Code | Name | Description |
|------|------|-------------|
| BENCH_SUITE_NOT_FOUND | Suite not found | Specified suite ID or path does not exist |
| BENCH_SUITE_INVALID | Invalid suite | Suite JSON does not conform to schema |
| BENCH_TASK_NOT_FOUND | Task not found | Specified task ID does not exist in suite |
| BENCH_TASK_INVALID | Invalid task | Task specification is malformed |
| BENCH_SANDBOX_FAILED | Sandbox failure | Could not create or manage sandbox |
| BENCH_LLM_UNAVAILABLE | LLM unavailable | Model is not responding to health check |
| BENCH_TIMEOUT | Execution timeout | Task exceeded its time limit |
| BENCH_CANCELLED | Execution cancelled | User cancelled the run |
| BENCH_IO_ERROR | I/O error | File system operation failed |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All tasks passed |
| 1 | One or more tasks failed |
| 2 | Configuration or validation error |
| 3 | Runtime error (crash, resource issue) |
| 130 | User cancelled (Ctrl+C) |

**End of Task 046 Specification**
