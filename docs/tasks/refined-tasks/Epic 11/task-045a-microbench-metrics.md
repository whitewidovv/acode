# Task 045.a: Microbench Metrics

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 11 – Optimization  
**Dependencies:** Task 045 (Performance Harness)  

---

## Description

Task 045.a implements microbenchmark metrics—the fine-grained measurements that capture detailed performance characteristics of specific operations. While the main harness measures overall performance, microbenchmarks focus on isolated operations: single inference calls, token generation rates, memory allocation patterns.

Microbenchmarks provide the resolution needed to identify bottlenecks and validate optimizations. They run in isolation, with controlled conditions, producing highly reproducible results that are directly comparable across runs.

### Business Value

Microbenchmarks provide:
- Bottleneck identification
- Optimization validation
- Fine-grained comparison
- Reproducible data
- Actionable insights

### Scope Boundaries

This task covers microbenchmark implementation. Core harness is Task 045. Tool correctness is Task 045.b. Report comparisons are Task 045.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Harness | Task 045 | Benchmark integration | Parent |
| Local LLM | Task 024 | Target | Measurement |
| Metrics | Collector | Results | Storage |
| CLI | Commands | Control | Interface |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| GC interference | Detect | Retry | Noisy data |
| CPU throttling | Monitor | Wait | Invalid data |
| Memory pressure | Check | Skip | Incomplete |
| Timeout | Timer | Skip case | Partial |
| Variance high | Stats | More iterations | Longer run |

### Assumptions

1. **Isolation possible**: Controlled conditions
2. **GC controllable**: Force before run
3. **Warm JIT**: After warmup
4. **CPU stable**: No throttling
5. **Memory available**: Sufficient

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Microbenchmark | Isolated operation test |
| GC | Garbage collection |
| JIT | Just-in-time compilation |
| Warmup | Pre-measurement runs |
| Cold | First run after start |
| Warm | After warmup |
| Isolation | Controlled conditions |
| Allocation | Memory usage |
| TTFT | Time to first token |
| TPS | Tokens per second |

---

## Out of Scope

- GPU microbenchmarks
- Network latency benchmarks
- Disk I/O benchmarks
- External service benchmarks
- Container overhead benchmarks
- Distributed benchmarks

---

## Functional Requirements

### FR-001 to FR-020: Microbenchmark Core

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045a-01 | Microbenchmark MUST be isolated | P0 |
| FR-045a-02 | GC MUST be forced before | P0 |
| FR-045a-03 | Warmup MUST run | P0 |
| FR-045a-04 | Default warmup MUST be 5 | P0 |
| FR-045a-05 | Iterations MUST be configurable | P0 |
| FR-045a-06 | Default iterations MUST be 100 | P0 |
| FR-045a-07 | Outlier detection MUST exist | P0 |
| FR-045a-08 | Outliers MUST be removable | P0 |
| FR-045a-09 | Default outlier MUST be 3σ | P0 |
| FR-045a-10 | Statistics MUST be calculated | P0 |
| FR-045a-11 | Mean MUST be calculated | P0 |
| FR-045a-12 | Median MUST be calculated | P0 |
| FR-045a-13 | StdDev MUST be calculated | P0 |
| FR-045a-14 | Min/Max MUST be recorded | P0 |
| FR-045a-15 | Percentiles MUST be calculated | P0 |
| FR-045a-16 | P50/P90/P95/P99 MUST exist | P0 |
| FR-045a-17 | Variance MUST be detected | P0 |
| FR-045a-18 | High variance MUST warn | P0 |
| FR-045a-19 | Baseline comparison MUST work | P0 |
| FR-045a-20 | Regression detection MUST work | P0 |

### FR-021 to FR-040: Specific Benchmarks

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045a-21 | TTFT benchmark MUST exist | P0 |
| FR-045a-22 | TTFT MUST measure first token time | P0 |
| FR-045a-23 | TPS benchmark MUST exist | P0 |
| FR-045a-24 | TPS MUST measure tokens/second | P0 |
| FR-045a-25 | Memory benchmark MUST exist | P0 |
| FR-045a-26 | Memory MUST measure allocation | P0 |
| FR-045a-27 | Memory MUST measure peak | P0 |
| FR-045a-28 | Prompt encode benchmark MUST exist | P0 |
| FR-045a-29 | Encode MUST measure tokenization | P0 |
| FR-045a-30 | Context load benchmark MUST exist | P1 |
| FR-045a-31 | Load MUST measure context setup | P1 |
| FR-045a-32 | Single token benchmark MUST exist | P0 |
| FR-045a-33 | Single MUST measure one token | P0 |
| FR-045a-34 | Batch benchmark MUST exist | P1 |
| FR-045a-35 | Batch MUST measure parallel | P1 |
| FR-045a-36 | Cold start benchmark MUST exist | P0 |
| FR-045a-37 | Cold MUST measure from zero | P0 |
| FR-045a-38 | Warm start benchmark MUST exist | P0 |
| FR-045a-39 | Warm MUST measure cached | P0 |
| FR-045a-40 | Custom benchmark MUST be supported | P1 |

### FR-041 to FR-055: Measurement Quality

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045a-41 | High-resolution timer MUST be used | P0 |
| FR-045a-42 | Timer resolution MUST be <1μs | P0 |
| FR-045a-43 | Monotonic timer MUST be used | P0 |
| FR-045a-44 | Timer overhead MUST be measured | P0 |
| FR-045a-45 | Timer overhead MUST be subtracted | P0 |
| FR-045a-46 | Memory MUST be measured accurately | P0 |
| FR-045a-47 | GC pressure MUST be measured | P1 |
| FR-045a-48 | Gen0/Gen1/Gen2 MUST be tracked | P1 |
| FR-045a-49 | Allocation rate MUST be calculated | P1 |
| FR-045a-50 | CPU affinity MUST be optional | P2 |
| FR-045a-51 | Process priority MUST be settable | P2 |
| FR-045a-52 | Background work MUST be minimized | P0 |
| FR-045a-53 | Logging MUST be minimized | P0 |
| FR-045a-54 | Measurement mode MUST reduce noise | P0 |
| FR-045a-55 | Noise detection MUST exist | P0 |

### FR-056 to FR-065: Results

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045a-56 | Results MUST be detailed | P0 |
| FR-045a-57 | Raw measurements MUST be available | P0 |
| FR-045a-58 | Summary MUST be generated | P0 |
| FR-045a-59 | Histogram MUST be available | P1 |
| FR-045a-60 | Distribution MUST be shown | P1 |
| FR-045a-61 | Confidence interval MUST exist | P0 |
| FR-045a-62 | Default CI MUST be 95% | P0 |
| FR-045a-63 | Comparison MUST show delta | P0 |
| FR-045a-64 | Delta MUST show significance | P0 |
| FR-045a-65 | Export MUST include raw data | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-045a-01 | Timer resolution | <1μs | P0 |
| NFR-045a-02 | Measurement overhead | <100ns | P0 |
| NFR-045a-03 | Stats calculation | <10ms | P0 |
| NFR-045a-04 | Memory accuracy | ±1KB | P0 |
| NFR-045a-05 | Run-to-run variance | <5% | P0 |
| NFR-045a-06 | Warmup effectiveness | JIT done | P0 |
| NFR-045a-07 | GC impact | Negligible | P0 |
| NFR-045a-08 | Results storage | <100ms | P0 |
| NFR-045a-09 | Histogram generation | <50ms | P1 |
| NFR-045a-10 | Comparison time | <100ms | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-045a-11 | Reproducibility | >95% same | P0 |
| NFR-045a-12 | Outlier detection | 100% | P0 |
| NFR-045a-13 | Cross-platform | All OS | P0 |
| NFR-045a-14 | Timer availability | 100% | P0 |
| NFR-045a-15 | Memory measurement | 100% | P0 |
| NFR-045a-16 | Error recovery | Per benchmark | P0 |
| NFR-045a-17 | Data integrity | 100% | P0 |
| NFR-045a-18 | Stats accuracy | ±0.1% | P0 |
| NFR-045a-19 | Confidence calc | Correct | P0 |
| NFR-045a-20 | Isolation | From other | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-045a-21 | Benchmark logged | Debug | P0 |
| NFR-045a-22 | Warmup logged | Debug | P0 |
| NFR-045a-23 | Outliers logged | Info | P0 |
| NFR-045a-24 | Results logged | Info | P0 |
| NFR-045a-25 | Variance logged | Warning if high | P0 |
| NFR-045a-26 | Environment logged | Debug | P0 |
| NFR-045a-27 | Structured logging | JSON | P0 |
| NFR-045a-28 | Trace correlation | Optional | P1 |
| NFR-045a-29 | Raw data available | Debug | P0 |
| NFR-045a-30 | Histogram data | Exportable | P1 |

---

## Acceptance Criteria / Definition of Done

### Core
- [ ] AC-001: Isolated execution
- [ ] AC-002: GC forced
- [ ] AC-003: Warmup runs
- [ ] AC-004: Iterations configurable
- [ ] AC-005: Outliers detected
- [ ] AC-006: Statistics calculated
- [ ] AC-007: Percentiles
- [ ] AC-008: Regression detection

### Benchmarks
- [ ] AC-009: TTFT works
- [ ] AC-010: TPS works
- [ ] AC-011: Memory works
- [ ] AC-012: Encode works
- [ ] AC-013: Cold start works
- [ ] AC-014: Warm start works
- [ ] AC-015: Single token works
- [ ] AC-016: Custom works

### Quality
- [ ] AC-017: High-res timer
- [ ] AC-018: Timer overhead subtracted
- [ ] AC-019: Memory accurate
- [ ] AC-020: Noise minimized
- [ ] AC-021: Variance detected
- [ ] AC-022: Reproducible
- [ ] AC-023: Confidence interval
- [ ] AC-024: Comparison works

### Results
- [ ] AC-025: Detailed results
- [ ] AC-026: Raw available
- [ ] AC-027: Summary generated
- [ ] AC-028: Histogram available
- [ ] AC-029: Cross-platform
- [ ] AC-030: Tests pass
- [ ] AC-031: Documented
- [ ] AC-032: Reviewed

---

## User Verification Scenarios

### Scenario 1: TTFT Benchmark
**Persona:** Developer measuring latency  
**Preconditions:** LLM running  
**Steps:**
1. Run TTFT benchmark
2. 100 iterations
3. Stats calculated
4. P95 shown

**Verification Checklist:**
- [ ] Benchmark runs
- [ ] Iterations complete
- [ ] Stats accurate
- [ ] P95 meaningful

### Scenario 2: TPS Benchmark
**Persona:** Developer measuring throughput  
**Preconditions:** LLM running  
**Steps:**
1. Run TPS benchmark
2. Measure tokens/second
3. Compare models
4. Choose faster

**Verification Checklist:**
- [ ] TPS measured
- [ ] Accurate count
- [ ] Comparable
- [ ] Decision clear

### Scenario 3: Memory Benchmark
**Persona:** Developer checking resources  
**Preconditions:** LLM running  
**Steps:**
1. Run memory benchmark
2. Peak measured
3. Allocation tracked
4. Report generated

**Verification Checklist:**
- [ ] Memory measured
- [ ] Peak accurate
- [ ] Allocation shown
- [ ] Useful data

### Scenario 4: Outlier Detection
**Persona:** Developer with noisy data  
**Preconditions:** Some outliers  
**Steps:**
1. Run benchmark
2. Outliers detected
3. Outliers removed
4. Stats cleaner

**Verification Checklist:**
- [ ] Outliers found
- [ ] Correctly identified
- [ ] Stats improved
- [ ] Warning shown

### Scenario 5: Regression Detection
**Persona:** Developer after change  
**Preconditions:** Baseline exists  
**Steps:**
1. Run benchmark
2. Compare to baseline
3. Regression found
4. Delta shown

**Verification Checklist:**
- [ ] Comparison works
- [ ] Regression detected
- [ ] Delta clear
- [ ] Significance shown

### Scenario 6: Reproducibility
**Persona:** Developer validating  
**Preconditions:** Same conditions  
**Steps:**
1. Run benchmark
2. Record results
3. Run again
4. Results similar

**Verification Checklist:**
- [ ] Reproducible
- [ ] Low variance
- [ ] Consistent
- [ ] Trustworthy

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-045a-01 | Isolation setup | FR-045a-01 |
| UT-045a-02 | GC forcing | FR-045a-02 |
| UT-045a-03 | Warmup execution | FR-045a-03 |
| UT-045a-04 | Outlier detection | FR-045a-07 |
| UT-045a-05 | Statistics calculation | FR-045a-10 |
| UT-045a-06 | Percentile calculation | FR-045a-15 |
| UT-045a-07 | Timer resolution | FR-045a-41 |
| UT-045a-08 | Timer overhead | FR-045a-44 |
| UT-045a-09 | Memory measurement | FR-045a-46 |
| UT-045a-10 | Confidence interval | FR-045a-61 |
| UT-045a-11 | Comparison delta | FR-045a-63 |
| UT-045a-12 | TTFT benchmark | FR-045a-21 |
| UT-045a-13 | TPS benchmark | FR-045a-23 |
| UT-045a-14 | Memory benchmark | FR-045a-25 |
| UT-045a-15 | Raw data export | FR-045a-57 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-045a-01 | Harness integration | Task 045 |
| IT-045a-02 | LLM integration | Task 024 |
| IT-045a-03 | Full TTFT run | E2E |
| IT-045a-04 | Full TPS run | E2E |
| IT-045a-05 | Memory run | E2E |
| IT-045a-06 | Reproducibility | NFR-045a-11 |
| IT-045a-07 | Cross-platform | NFR-045a-13 |
| IT-045a-08 | High iteration count | FR-045a-06 |
| IT-045a-09 | Outlier handling | FR-045a-08 |
| IT-045a-10 | Baseline comparison | FR-045a-19 |
| IT-045a-11 | Variance detection | FR-045a-17 |
| IT-045a-12 | Results storage | FR-045a-56 |
| IT-045a-13 | Histogram | FR-045a-59 |
| IT-045a-14 | Logging | NFR-045a-21 |
| IT-045a-15 | Custom benchmark | FR-045a-40 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Performance/
│       ├── MicrobenchmarkResult.cs
│       ├── MeasurementSample.cs
│       └── StatisticalSummary.cs
├── Acode.Application/
│   └── Performance/
│       ├── IMicrobenchmark.cs
│       └── MicrobenchmarkOptions.cs
├── Acode.Infrastructure/
│   └── Performance/
│       ├── MicrobenchmarkRunner.cs
│       ├── HighResolutionTimer.cs
│       ├── MemoryMeasurement.cs
│       ├── OutlierDetector.cs
│       ├── StatisticsCalculator.cs
│       └── Microbenchmarks/
│           ├── TTFTBenchmark.cs
│           ├── TPSBenchmark.cs
│           ├── MemoryBenchmark.cs
│           ├── PromptEncodeBenchmark.cs
│           ├── ColdStartBenchmark.cs
│           └── WarmStartBenchmark.cs
```

### Timer Implementation

```csharp
public class HighResolutionTimer
{
    private static readonly double TicksPerNanosecond = 
        Stopwatch.Frequency / 1_000_000_000.0;
    
    public static long GetTimestamp() => Stopwatch.GetTimestamp();
    
    public static long ToNanoseconds(long ticks) => 
        (long)(ticks / TicksPerNanosecond);
    
    public static TimeSpan Elapsed(long start, long end) =>
        TimeSpan.FromTicks((long)((end - start) / TicksPerNanosecond * 
            TimeSpan.TicksPerSecond / 1_000_000_000));
}
```

### Statistics Output

```
TTFT Microbenchmark Results
===========================
Iterations: 100 (5 warmup, 2 outliers removed)

Time to First Token:
  Mean:     45.23 ms
  Median:   44.89 ms
  StdDev:   2.34 ms
  Min:      41.12 ms
  Max:      52.67 ms
  
Percentiles:
  P50:      44.89 ms
  P90:      48.12 ms
  P95:      49.87 ms
  P99:      51.45 ms

Confidence Interval (95%): 44.77 - 45.69 ms

vs Baseline:
  Delta:    +2.3 ms (+5.4%)
  Status:   REGRESSION (significant, p < 0.05)
```

**End of Task 045.a Specification**
