# Task 045: Model Performance Harness

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 11 – Optimization  
**Dependencies:** Task 024 (Local LLM), Task 025 (Tool Executor)  

---

## Description

Task 045 implements the model performance harness—a benchmarking framework that measures local LLM performance across multiple dimensions: inference speed, throughput, memory usage, and response quality. This harness is essential for comparing different local models, tuning configuration, and detecting performance regressions.

The harness runs standardized benchmarks against the local LLM, capturing metrics that inform model selection and configuration decisions. Unlike external benchmarks, this harness measures performance in the context of actual agent workloads, providing relevant, actionable data.

### Business Value

Performance harness provides:
- Model comparison data
- Configuration tuning
- Regression detection
- Optimization guidance
- Quality assurance

### Scope Boundaries

This task covers the core harness framework. Microbenchmark metrics are Task 045.a. Tool-call correctness is Task 045.b. Report comparisons are Task 045.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Local LLM | Task 024 | Benchmark target | Consumer |
| Tool Executor | Task 025 | Correctness | Measurement |
| Event Log | Task 040 | Results storage | Persistence |
| CLI | Commands | Harness control | Interface |
| Config | Settings | Harness config | Input |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| LLM crash | Process exit | Restart | Partial results |
| Timeout | Timer | Skip case | Incomplete |
| OOM | Memory check | Skip case | Incomplete |
| Invalid response | Parse error | Log + continue | Data point lost |
| Disk full | Write error | Stop | Partial results |

### Assumptions

1. **LLM available**: Local model running
2. **Resources sufficient**: CPU/RAM
3. **Benchmarks relevant**: Real workloads
4. **Reproducible**: Deterministic mode
5. **Time available**: Benchmarks take time

### Security Considerations

1. **No data leak**: Benchmark data only
2. **Resource limits**: Prevent DoS
3. **Results safe**: No secrets
4. **Isolated**: From production
5. **Logged**: Audit trail

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Harness | Benchmark framework |
| Benchmark | Standardized test |
| Metric | Measured value |
| Baseline | Reference point |
| Regression | Performance drop |
| Throughput | Ops per second |
| Latency | Time per op |
| Warmup | Pre-test runs |
| Iteration | Single run |
| Suite | Set of benchmarks |

---

## Out of Scope

- Distributed benchmarking
- GPU-specific benchmarks
- Model training metrics
- External model comparison
- Automated tuning
- Continuous benchmarking service

---

## Functional Requirements

### FR-001 to FR-020: Harness Core

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045-01 | Harness MUST exist | P0 |
| FR-045-02 | Harness MUST run benchmarks | P0 |
| FR-045-03 | Harness MUST collect metrics | P0 |
| FR-045-04 | Harness MUST store results | P0 |
| FR-045-05 | Harness MUST be configurable | P0 |
| FR-045-06 | Harness MUST support suites | P0 |
| FR-045-07 | Suite MUST group benchmarks | P0 |
| FR-045-08 | Benchmark MUST be selectable | P0 |
| FR-045-09 | Warmup MUST be supported | P0 |
| FR-045-10 | Default warmup MUST be 3 | P0 |
| FR-045-11 | Iterations MUST be configurable | P0 |
| FR-045-12 | Default iterations MUST be 10 | P0 |
| FR-045-13 | Timeout MUST be per-benchmark | P0 |
| FR-045-14 | Default timeout MUST be 5 min | P0 |
| FR-045-15 | Cancel MUST be supported | P0 |
| FR-045-16 | Progress MUST be shown | P0 |
| FR-045-17 | Errors MUST be captured | P0 |
| FR-045-18 | Errors MUST not stop suite | P0 |
| FR-045-19 | Summary MUST be generated | P0 |
| FR-045-20 | Results MUST be exportable | P0 |

### FR-021 to FR-040: Benchmark Types

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045-21 | Inference benchmark MUST exist | P0 |
| FR-045-22 | Throughput benchmark MUST exist | P0 |
| FR-045-23 | Latency benchmark MUST exist | P0 |
| FR-045-24 | Memory benchmark MUST exist | P0 |
| FR-045-25 | Context window benchmark MUST exist | P0 |
| FR-045-26 | Prompt length benchmark MUST exist | P1 |
| FR-045-27 | Token generation benchmark MUST exist | P0 |
| FR-045-28 | First token benchmark MUST exist | P0 |
| FR-045-29 | Streaming benchmark MUST exist | P1 |
| FR-045-30 | Concurrent benchmark MUST exist | P1 |
| FR-045-31 | Cold start benchmark MUST exist | P0 |
| FR-045-32 | Warm start benchmark MUST exist | P0 |
| FR-045-33 | Quality benchmark MUST exist | P0 |
| FR-045-34 | Tool call benchmark MUST exist | P0 |
| FR-045-35 | Code generation benchmark MUST exist | P0 |
| FR-045-36 | Explanation benchmark MUST exist | P1 |
| FR-045-37 | Small prompt benchmark MUST exist | P0 |
| FR-045-38 | Large prompt benchmark MUST exist | P0 |
| FR-045-39 | Mixed benchmark MUST exist | P0 |
| FR-045-40 | Custom benchmark MUST be addable | P1 |

### FR-041 to FR-060: Metrics Collection

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045-41 | Time MUST be measured | P0 |
| FR-045-42 | Wall clock MUST be used | P0 |
| FR-045-43 | CPU time MUST be measured | P1 |
| FR-045-44 | Memory MUST be measured | P0 |
| FR-045-45 | Peak memory MUST be tracked | P0 |
| FR-045-46 | Average memory MUST be tracked | P1 |
| FR-045-47 | Token count MUST be measured | P0 |
| FR-045-48 | Input tokens MUST be counted | P0 |
| FR-045-49 | Output tokens MUST be counted | P0 |
| FR-045-50 | Tokens/second MUST be calculated | P0 |
| FR-045-51 | First token latency MUST be measured | P0 |
| FR-045-52 | Total latency MUST be measured | P0 |
| FR-045-53 | P50 MUST be calculated | P0 |
| FR-045-54 | P95 MUST be calculated | P0 |
| FR-045-55 | P99 MUST be calculated | P0 |
| FR-045-56 | Min MUST be recorded | P0 |
| FR-045-57 | Max MUST be recorded | P0 |
| FR-045-58 | Mean MUST be calculated | P0 |
| FR-045-59 | Std dev MUST be calculated | P0 |
| FR-045-60 | Error rate MUST be tracked | P0 |

### FR-061 to FR-075: Results Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045-61 | Results MUST be stored | P0 |
| FR-045-62 | Results MUST be timestamped | P0 |
| FR-045-63 | Results MUST include config | P0 |
| FR-045-64 | Results MUST include environment | P0 |
| FR-045-65 | Environment MUST include CPU | P0 |
| FR-045-66 | Environment MUST include RAM | P0 |
| FR-045-67 | Environment MUST include OS | P0 |
| FR-045-68 | Environment MUST include model | P0 |
| FR-045-69 | Results MUST be queryable | P0 |
| FR-045-70 | Historical results MUST be kept | P0 |
| FR-045-71 | Retention MUST be configurable | P0 |
| FR-045-72 | Default retention MUST be 90 days | P0 |
| FR-045-73 | Export MUST support JSON | P0 |
| FR-045-74 | Export MUST support CSV | P1 |
| FR-045-75 | Import MUST work | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-045-01 | Harness overhead | <1% | P0 |
| NFR-045-02 | Metrics collection | <10ms | P0 |
| NFR-045-03 | Results storage | <100ms | P0 |
| NFR-045-04 | Progress update | 1/s | P0 |
| NFR-045-05 | Suite completion | Bounded | P0 |
| NFR-045-06 | Memory overhead | <100MB | P0 |
| NFR-045-07 | Concurrent harness | 1 only | P0 |
| NFR-045-08 | Cancel response | <1s | P0 |
| NFR-045-09 | Export speed | <1s | P0 |
| NFR-045-10 | Historical query | <500ms | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-045-11 | Measurement accuracy | ±1% | P0 |
| NFR-045-12 | Reproducibility | High | P0 |
| NFR-045-13 | Crash recovery | Resume | P0 |
| NFR-045-14 | Partial results | Saved | P0 |
| NFR-045-15 | Cross-platform | All OS | P0 |
| NFR-045-16 | LLM independence | Any local | P0 |
| NFR-045-17 | Isolation | From production | P0 |
| NFR-045-18 | Deterministic mode | Supported | P0 |
| NFR-045-19 | Error recovery | Per benchmark | P0 |
| NFR-045-20 | Data integrity | 100% | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-045-21 | Run start logged | Info | P0 |
| NFR-045-22 | Run end logged | Info | P0 |
| NFR-045-23 | Benchmark logged | Debug | P0 |
| NFR-045-24 | Error logged | Warning | P0 |
| NFR-045-25 | Metrics logged | Debug | P0 |
| NFR-045-26 | Progress logged | Debug | P0 |
| NFR-045-27 | Structured logging | JSON | P0 |
| NFR-045-28 | Trace ID | Per run | P1 |
| NFR-045-29 | Event log | Results | P0 |
| NFR-045-30 | Alerts | On regression | P2 |

---

## Acceptance Criteria / Definition of Done

### Core
- [ ] AC-001: Harness runs
- [ ] AC-002: Benchmarks execute
- [ ] AC-003: Metrics collected
- [ ] AC-004: Results stored
- [ ] AC-005: Suites work
- [ ] AC-006: Warmup works
- [ ] AC-007: Iterations configurable
- [ ] AC-008: Cancel works

### Benchmarks
- [ ] AC-009: Inference bench
- [ ] AC-010: Throughput bench
- [ ] AC-011: Latency bench
- [ ] AC-012: Memory bench
- [ ] AC-013: Token bench
- [ ] AC-014: First token bench
- [ ] AC-015: Tool call bench
- [ ] AC-016: Code gen bench

### Metrics
- [ ] AC-017: Time measured
- [ ] AC-018: Memory measured
- [ ] AC-019: Tokens counted
- [ ] AC-020: Percentiles
- [ ] AC-021: Error rate
- [ ] AC-022: Environment
- [ ] AC-023: Statistical
- [ ] AC-024: Accurate

### Results
- [ ] AC-025: Stored
- [ ] AC-026: Timestamped
- [ ] AC-027: Queryable
- [ ] AC-028: Exportable
- [ ] AC-029: Cross-platform
- [ ] AC-030: Tests pass
- [ ] AC-031: Documented
- [ ] AC-032: Reviewed

---

## User Verification Scenarios

### Scenario 1: Run Benchmark Suite
**Persona:** Developer evaluating model  
**Preconditions:** Local LLM running  
**Steps:**
1. Run benchmark suite
2. Progress shown
3. Metrics collected
4. Summary displayed

**Verification Checklist:**
- [ ] Suite runs
- [ ] Progress visible
- [ ] Metrics accurate
- [ ] Summary clear

### Scenario 2: Compare Models
**Persona:** Developer choosing model  
**Preconditions:** Two models available  
**Steps:**
1. Benchmark model A
2. Benchmark model B
3. Compare results
4. Choose winner

**Verification Checklist:**
- [ ] Both complete
- [ ] Results stored
- [ ] Comparison works
- [ ] Decision clear

### Scenario 3: Detect Regression
**Persona:** Developer after change  
**Preconditions:** Baseline exists  
**Steps:**
1. Run benchmarks
2. Compare to baseline
3. Regression detected
4. Investigate

**Verification Checklist:**
- [ ] Comparison works
- [ ] Regression found
- [ ] Delta shown
- [ ] Actionable

### Scenario 4: Custom Benchmark
**Persona:** Developer with specific needs  
**Preconditions:** Custom workload  
**Steps:**
1. Create custom benchmark
2. Add to suite
3. Run
4. Results captured

**Verification Checklist:**
- [ ] Custom works
- [ ] Integrated
- [ ] Metrics collected
- [ ] Results stored

### Scenario 5: Export Results
**Persona:** Developer sharing data  
**Preconditions:** Results exist  
**Steps:**
1. Export to JSON
2. File created
3. Data complete
4. Parseable

**Verification Checklist:**
- [ ] Export works
- [ ] File valid
- [ ] Data complete
- [ ] Portable

### Scenario 6: Historical Query
**Persona:** Developer analyzing trends  
**Preconditions:** Multiple runs  
**Steps:**
1. Query last 30 days
2. Results returned
3. Trends visible
4. Analysis done

**Verification Checklist:**
- [ ] Query works
- [ ] Results accurate
- [ ] Trends clear
- [ ] Time range works

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-045-01 | Harness creation | FR-045-01 |
| UT-045-02 | Benchmark registration | FR-045-06 |
| UT-045-03 | Warmup execution | FR-045-09 |
| UT-045-04 | Iteration counting | FR-045-11 |
| UT-045-05 | Time measurement | FR-045-41 |
| UT-045-06 | Memory measurement | FR-045-44 |
| UT-045-07 | Token counting | FR-045-47 |
| UT-045-08 | Percentile calc | FR-045-53 |
| UT-045-09 | Statistics | FR-045-58 |
| UT-045-10 | Results storage | FR-045-61 |
| UT-045-11 | Environment capture | FR-045-64 |
| UT-045-12 | Export JSON | FR-045-73 |
| UT-045-13 | Cancel handling | FR-045-15 |
| UT-045-14 | Error handling | FR-045-17 |
| UT-045-15 | Progress tracking | FR-045-16 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-045-01 | LLM integration | Task 024 |
| IT-045-02 | Tool integration | Task 025 |
| IT-045-03 | Event log | Task 040 |
| IT-045-04 | CLI integration | Task 000 |
| IT-045-05 | Full suite run | E2E |
| IT-045-06 | Cross-platform | NFR-045-15 |
| IT-045-07 | Reproducibility | NFR-045-12 |
| IT-045-08 | Crash recovery | NFR-045-13 |
| IT-045-09 | Large suite | NFR-045-05 |
| IT-045-10 | Historical query | FR-045-69 |
| IT-045-11 | Concurrent prevention | NFR-045-07 |
| IT-045-12 | Deterministic mode | NFR-045-18 |
| IT-045-13 | Logging | NFR-045-21 |
| IT-045-14 | Export/import | FR-045-75 |
| IT-045-15 | Performance overhead | NFR-045-01 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Performance/
│       ├── BenchmarkResult.cs
│       ├── BenchmarkMetrics.cs
│       └── BenchmarkEnvironment.cs
├── Acode.Application/
│   └── Performance/
│       ├── IBenchmarkHarness.cs
│       ├── IBenchmark.cs
│       └── HarnessOptions.cs
├── Acode.Infrastructure/
│   └── Performance/
│       ├── BenchmarkHarness.cs
│       ├── MetricsCollector.cs
│       ├── ResultsStore.cs
│       └── Benchmarks/
│           ├── InferenceBenchmark.cs
│           ├── ThroughputBenchmark.cs
│           ├── LatencyBenchmark.cs
│           ├── MemoryBenchmark.cs
│           ├── TokenBenchmark.cs
│           └── ToolCallBenchmark.cs
├── Acode.Cli/
│   └── Commands/
│       └── BenchmarkCommand.cs
```

### CLI Commands

```bash
# Run all benchmarks
acode benchmark run

# Run specific suite
acode benchmark run --suite inference

# Run with config
acode benchmark run --iterations 20 --warmup 5

# List available benchmarks
acode benchmark list

# Show results
acode benchmark results
acode benchmark results --last 5
acode benchmark results --from 2024-01-01

# Export
acode benchmark export <run-id> --format json
```

### Benchmark Interface

```csharp
public interface IBenchmark
{
    string Name { get; }
    string Suite { get; }
    string Description { get; }
    TimeSpan DefaultTimeout { get; }
    
    Task<BenchmarkMetrics> RunAsync(
        IBenchmarkContext context,
        CancellationToken ct = default);
}

public interface IBenchmarkHarness
{
    Task<BenchmarkRunResult> RunAsync(
        HarnessOptions options,
        IProgress<BenchmarkProgress>? progress = null,
        CancellationToken ct = default);
}
```

**End of Task 045 Specification**
