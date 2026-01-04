# Task 041.a: Categorize Failures – Transient vs Permanent

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 10 – Core Reliability  
**Dependencies:** Task 041 (Retry Framework)  

---

## Description

Task 041.a implements failure categorization—the logic that determines whether an error is transient (likely to succeed on retry) or permanent (will never succeed). This classification is foundational to the retry framework; retrying permanent failures wastes time, while failing on transient errors leaves value on the table.

The failure classifier examines exception types, error codes, HTTP status codes, and error messages to categorize failures. It uses a rule-based approach with configurable matchers. Default rules cover common scenarios: network timeouts are transient, authentication failures are permanent, 5xx HTTP errors are transient, 4xx are usually permanent.

The classifier supports customization. Users can define additional rules in configuration, override default classifications, and mark specific error codes as retryable or non-retryable. This flexibility handles edge cases where defaults are wrong.

Unknown exceptions default to non-retryable to prevent infinite loops on novel failures. However, a configurable "unknown-as-transient" mode exists for aggressive retry scenarios.

### Business Value

Failure categorization provides:
- Appropriate retry decisions
- No wasted retry on permanent failures
- Recovery from transient issues
- Predictable behavior
- Customization for edge cases

### Scope Boundaries

This task covers failure classification. Retry execution is Task 041. Retry caps are Task 041.b. Human escalation is Task 041.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Retry Framework | Task 041 | Classification query | Core use |
| Config | Task 002 | Classification rules | YAML |
| Event Log | Task 040 | Log classification | Audit |
| Tool Executor | `IToolExecutor` | Exception source | Input |
| HTTP Client | Network | HTTP errors | Status codes |
| File System | I/O | IO exceptions | Error codes |
| Secrets | Task 038 | Redact details | Logging |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Unknown exception | Type check | Default non-retry | May miss retry |
| Misclassification | Audit review | Config fix | Wasted/missed retry |
| Rule conflict | Validation | First match wins | Predictable |
| Config error | Parse failure | Use defaults | Warning |
| Null exception | Guard | Error | Bug |
| Nested exception | Unwrap | Check all | Correct |
| Dynamic error | Runtime check | Evaluate | Works |
| Exception hierarchy | Base check | Match closest | Correct |

### Assumptions

1. **Exception types informative**: Usually true
2. **Error codes standard**: Platform-specific
3. **HTTP status reliable**: Industry standard
4. **Defaults cover most cases**: 80/20 rule
5. **Config overrides needed**: Edge cases
6. **Immutable at runtime**: No hot reload
7. **Classification fast**: <1ms
8. **Logging safe**: Redacted

### Security Considerations

1. **Error messages redacted**: No secrets
2. **Classification logged**: Audit
3. **Auth failures non-retryable**: Prevent brute force
4. **No sensitive data in rules**: Config public
5. **Exception stack safe**: Redacted
6. **Rules validated**: No injection
7. **Audit of overrides**: Track changes
8. **Default conservative**: Non-retry unknown

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Transient Failure | Temporary, may succeed on retry |
| Permanent Failure | Will never succeed |
| Classification | Determining failure type |
| Classifier | Logic for classification |
| Rule | Pattern for matching errors |
| Matcher | Pattern matching logic |
| Exception Hierarchy | Base/derived relationships |
| Unwrap | Extract inner exceptions |
| Override | Custom rule replacing default |
| Default Rule | Built-in classification |

---

## Out of Scope

- ML-based classification
- Distributed classification
- Real-time classification learning
- Classification analytics
- Cross-agent classification sharing
- Probabilistic classification

---

## Functional Requirements

### FR-001 to FR-020: Core Classification

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041a-01 | Classifier MUST accept exception | P0 |
| FR-041a-02 | Classifier MUST return transient/permanent | P0 |
| FR-041a-03 | Classifier MUST check exception type | P0 |
| FR-041a-04 | Classifier MUST check error code | P0 |
| FR-041a-05 | Classifier MUST check HTTP status | P0 |
| FR-041a-06 | Classifier MUST check message patterns | P1 |
| FR-041a-07 | Classifier MUST unwrap inner exceptions | P0 |
| FR-041a-08 | Classifier MUST check all inner | P0 |
| FR-041a-09 | First transient found MUST win | P0 |
| FR-041a-10 | Unknown MUST default to permanent | P0 |
| FR-041a-11 | Unknown-as-transient MUST be configurable | P1 |
| FR-041a-12 | Classification MUST be logged | P0 |
| FR-041a-13 | Log MUST include reason | P0 |
| FR-041a-14 | Classification MUST be fast | P0 |
| FR-041a-15 | Classification MUST be deterministic | P0 |
| FR-041a-16 | Same exception MUST give same result | P0 |
| FR-041a-17 | Null exception MUST error | P0 |
| FR-041a-18 | Exception details MUST be redacted in log | P0 |
| FR-041a-19 | Classification MUST support context | P1 |
| FR-041a-20 | Context MUST include operation type | P1 |

### FR-021 to FR-035: Default Rules

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041a-21 | TimeoutException MUST be transient | P0 |
| FR-041a-22 | SocketException MUST be transient | P0 |
| FR-041a-23 | HttpRequestException MUST check code | P0 |
| FR-041a-24 | HTTP 5xx MUST be transient | P0 |
| FR-041a-25 | HTTP 429 MUST be transient | P0 |
| FR-041a-26 | HTTP 408 MUST be transient | P0 |
| FR-041a-27 | HTTP 4xx (other) MUST be permanent | P0 |
| FR-041a-28 | HTTP 401/403 MUST be permanent | P0 |
| FR-041a-29 | IOException (file locked) MUST be transient | P0 |
| FR-041a-30 | IOException (not found) MUST be permanent | P0 |
| FR-041a-31 | UnauthorizedAccessException MUST be permanent | P0 |
| FR-041a-32 | ArgumentException MUST be permanent | P0 |
| FR-041a-33 | NullReferenceException MUST be permanent | P0 |
| FR-041a-34 | OutOfMemoryException MUST be permanent | P0 |
| FR-041a-35 | OperationCanceledException MUST be permanent | P0 |

### FR-036 to FR-050: Custom Rules

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041a-36 | Custom rules MUST be configurable | P0 |
| FR-041a-37 | Rules MUST be in config.yml | P0 |
| FR-041a-38 | Rule MUST specify exception type | P0 |
| FR-041a-39 | Rule MUST specify classification | P0 |
| FR-041a-40 | Rule MAY specify error code | P1 |
| FR-041a-41 | Rule MAY specify message pattern | P1 |
| FR-041a-42 | Message pattern MUST be regex | P1 |
| FR-041a-43 | Custom rules MUST override defaults | P0 |
| FR-041a-44 | Rule priority MUST be configurable | P1 |
| FR-041a-45 | First matching rule MUST win | P0 |
| FR-041a-46 | Rule validation MUST occur on load | P0 |
| FR-041a-47 | Invalid rule MUST error | P0 |
| FR-041a-48 | Rule count MUST be logged | P1 |
| FR-041a-49 | Rule matching MUST be logged | P1 |
| FR-041a-50 | No rules MUST use defaults | P0 |

### FR-051 to FR-060: Edge Cases

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041a-51 | AggregateException MUST check all | P0 |
| FR-041a-52 | Any transient in aggregate MUST be transient | P0 |
| FR-041a-53 | Deeply nested MUST be checked | P0 |
| FR-041a-54 | Max nesting depth MUST exist | P0 |
| FR-041a-55 | Default depth MUST be 10 | P0 |
| FR-041a-56 | Depth exceeded MUST warn | P0 |
| FR-041a-57 | Circular reference MUST be detected | P0 |
| FR-041a-58 | Circular MUST error | P0 |
| FR-041a-59 | Custom exception MUST be supported | P0 |
| FR-041a-60 | Base class matching MUST work | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-041a-01 | Single classification | <1ms | P0 |
| NFR-041a-02 | Nested (depth 10) | <5ms | P0 |
| NFR-041a-03 | Aggregate (10 inner) | <5ms | P0 |
| NFR-041a-04 | Rule lookup | <0.1ms | P0 |
| NFR-041a-05 | Regex match | <1ms | P1 |
| NFR-041a-06 | Config load | <50ms | P0 |
| NFR-041a-07 | Memory per rule | <1KB | P2 |
| NFR-041a-08 | Rule cache | 1000 rules | P2 |
| NFR-041a-09 | Classification throughput | 10k/s | P2 |
| NFR-041a-10 | No allocation in hot path | P1 | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-041a-11 | Correct classification | 99%+ | P0 |
| NFR-041a-12 | No false transient | 99.9% | P0 |
| NFR-041a-13 | Deterministic | 100% | P0 |
| NFR-041a-14 | Thread safety | No races | P0 |
| NFR-041a-15 | Exception safety | No throw | P0 |
| NFR-041a-16 | Null handling | No crash | P0 |
| NFR-041a-17 | Cross-platform | All OS | P0 |
| NFR-041a-18 | Default always works | 100% | P0 |
| NFR-041a-19 | Config error resilient | Falls back | P0 |
| NFR-041a-20 | Graceful degradation | On error | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-041a-21 | Classification logged | Debug | P0 |
| NFR-041a-22 | Reason logged | Debug | P0 |
| NFR-041a-23 | Unknown logged | Warning | P0 |
| NFR-041a-24 | Config logged | Info | P1 |
| NFR-041a-25 | Metrics: transient count | Counter | P2 |
| NFR-041a-26 | Metrics: permanent count | Counter | P2 |
| NFR-041a-27 | Metrics: unknown count | Counter | P2 |
| NFR-041a-28 | Structured logging | JSON | P0 |
| NFR-041a-29 | Exception redaction | Task 038 | P0 |
| NFR-041a-30 | Rule match logged | Debug | P1 |

---

## Acceptance Criteria / Definition of Done

### Core Classification
- [ ] AC-001: Exception accepted
- [ ] AC-002: Returns transient/permanent
- [ ] AC-003: Type checked
- [ ] AC-004: Code checked
- [ ] AC-005: HTTP status checked
- [ ] AC-006: Inner unwrapped
- [ ] AC-007: First transient wins
- [ ] AC-008: Unknown defaults permanent

### Default Rules
- [ ] AC-009: Timeout is transient
- [ ] AC-010: Socket is transient
- [ ] AC-011: HTTP 5xx transient
- [ ] AC-012: HTTP 429 transient
- [ ] AC-013: HTTP 4xx permanent
- [ ] AC-014: Auth failures permanent
- [ ] AC-015: File locked transient
- [ ] AC-016: Not found permanent

### Custom Rules
- [ ] AC-017: Rules configurable
- [ ] AC-018: In config.yml
- [ ] AC-019: Type specified
- [ ] AC-020: Classification specified
- [ ] AC-021: Override defaults
- [ ] AC-022: First match wins
- [ ] AC-023: Validation on load
- [ ] AC-024: Invalid errors

### Edge Cases
- [ ] AC-025: Aggregate checked
- [ ] AC-026: Deeply nested checked
- [ ] AC-027: Max depth enforced
- [ ] AC-028: Circular detected
- [ ] AC-029: Custom types work
- [ ] AC-030: Base class matching
- [ ] AC-031: Redacted logging
- [ ] AC-032: Fast performance

---

## User Verification Scenarios

### Scenario 1: Timeout Classification
**Persona:** Retry framework  
**Preconditions:** TimeoutException thrown  
**Steps:**
1. Exception caught
2. Classifier invoked
3. Type matched
4. Returns transient

**Verification Checklist:**
- [ ] Type recognized
- [ ] Transient returned
- [ ] Logged
- [ ] Fast

### Scenario 2: Auth Failure
**Persona:** Retry framework  
**Preconditions:** 401 HTTP error  
**Steps:**
1. Exception caught
2. HTTP status checked
3. 401 matched
4. Returns permanent

**Verification Checklist:**
- [ ] Status extracted
- [ ] Permanent returned
- [ ] No retry
- [ ] Logged

### Scenario 3: Custom Rule
**Persona:** User with edge case  
**Preconditions:** Custom rule configured  
**Steps:**
1. Exception thrown
2. Custom rule matches
3. Override applied
4. Correct classification

**Verification Checklist:**
- [ ] Rule matched
- [ ] Override works
- [ ] Logged
- [ ] As expected

### Scenario 4: Nested Exception
**Persona:** Retry framework  
**Preconditions:** Wrapped exception  
**Steps:**
1. Outer exception caught
2. Inner unwrapped
3. Inner classified
4. Transient if any

**Verification Checklist:**
- [ ] Unwrapped
- [ ] Inner checked
- [ ] Correct result
- [ ] Depth limited

### Scenario 5: Unknown Exception
**Persona:** Retry framework  
**Preconditions:** Novel exception  
**Steps:**
1. Unknown type caught
2. No rule matches
3. Default applied
4. Permanent returned

**Verification Checklist:**
- [ ] Unknown detected
- [ ] Default used
- [ ] Permanent returned
- [ ] Warning logged

### Scenario 6: Aggregate Exception
**Persona:** Retry framework  
**Preconditions:** Multiple inner  
**Steps:**
1. AggregateException caught
2. All inner checked
3. Any transient found
4. Transient returned

**Verification Checklist:**
- [ ] All checked
- [ ] First transient wins
- [ ] Logged
- [ ] Fast

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-041a-01 | Timeout transient | FR-041a-21 |
| UT-041a-02 | Socket transient | FR-041a-22 |
| UT-041a-03 | HTTP 5xx transient | FR-041a-24 |
| UT-041a-04 | HTTP 429 transient | FR-041a-25 |
| UT-041a-05 | HTTP 401 permanent | FR-041a-28 |
| UT-041a-06 | HTTP 404 permanent | FR-041a-27 |
| UT-041a-07 | File locked transient | FR-041a-29 |
| UT-041a-08 | Not found permanent | FR-041a-30 |
| UT-041a-09 | Unknown permanent | FR-041a-10 |
| UT-041a-10 | Inner exception | FR-041a-07 |
| UT-041a-11 | Aggregate exception | FR-041a-51 |
| UT-041a-12 | Custom rule | FR-041a-36 |
| UT-041a-13 | Rule override | FR-041a-43 |
| UT-041a-14 | Base class match | FR-041a-60 |
| UT-041a-15 | Max depth | FR-041a-54 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-041a-01 | Full classification flow | E2E |
| IT-041a-02 | Config loading | FR-041a-37 |
| IT-041a-03 | Retry integration | Task 041 |
| IT-041a-04 | Multiple rules | FR-041a-45 |
| IT-041a-05 | Real network errors | FR-041a-22 |
| IT-041a-06 | Real HTTP errors | FR-041a-24 |
| IT-041a-07 | Real file errors | FR-041a-29 |
| IT-041a-08 | Logging | NFR-041a-21 |
| IT-041a-09 | Redaction | Task 038 |
| IT-041a-10 | Performance | NFR-041a-01 |
| IT-041a-11 | Cross-platform | NFR-041a-17 |
| IT-041a-12 | Thread safety | NFR-041a-14 |
| IT-041a-13 | Config error | NFR-041a-19 |
| IT-041a-14 | Unknown handling | FR-041a-10 |
| IT-041a-15 | Event log | Task 040 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Retry/
│       ├── FailureType.cs
│       ├── ClassificationRule.cs
│       └── ClassificationResult.cs
├── Acode.Application/
│   └── Retry/
│       ├── IFailureClassifier.cs
│       └── ClassifierOptions.cs
├── Acode.Infrastructure/
│   └── Retry/
│       ├── FailureClassifier.cs
│       ├── DefaultRules.cs
│       └── RuleLoader.cs
```

### Configuration Schema

```yaml
retry:
  classification:
    unknownAsTransient: false
    maxNestingDepth: 10
    rules:
      - type: MyApp.CustomException
        classification: transient
      - type: System.Net.Http.HttpRequestException
        errorCodes: [503, 504]
        classification: transient
      - type: System.IO.IOException
        messagePattern: "being used by another process"
        classification: transient
```

### Key Implementation

```csharp
public class FailureClassifier : IFailureClassifier
{
    public ClassificationResult Classify(Exception exception, string? context = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        
        var visited = new HashSet<Exception>();
        var result = ClassifyRecursive(exception, visited, 0);
        
        _logger.LogDebug("Classified {Type} as {Result}: {Reason}",
            exception.GetType().Name, result.Type, result.Reason);
        
        return result;
    }
    
    private ClassificationResult ClassifyRecursive(
        Exception exception, 
        HashSet<Exception> visited,
        int depth)
    {
        if (depth > _options.MaxNestingDepth)
        {
            _logger.LogWarning("Max nesting depth exceeded");
            return ClassificationResult.Permanent("Max depth exceeded");
        }
        
        if (!visited.Add(exception))
        {
            return ClassificationResult.Permanent("Circular reference");
        }
        
        // Check custom rules first
        var customResult = CheckCustomRules(exception);
        if (customResult != null)
            return customResult;
        
        // Check default rules
        var defaultResult = CheckDefaultRules(exception);
        if (defaultResult != null)
            return defaultResult;
        
        // Check inner exceptions
        if (exception.InnerException != null)
        {
            var innerResult = ClassifyRecursive(
                exception.InnerException, visited, depth + 1);
            if (innerResult.Type == FailureType.Transient)
                return innerResult;
        }
        
        // Check aggregate
        if (exception is AggregateException agg)
        {
            foreach (var inner in agg.InnerExceptions)
            {
                var aggResult = ClassifyRecursive(inner, visited, depth + 1);
                if (aggResult.Type == FailureType.Transient)
                    return aggResult;
            }
        }
        
        // Unknown default
        return _options.UnknownAsTransient
            ? ClassificationResult.Transient("Unknown (configured transient)")
            : ClassificationResult.Permanent("Unknown exception type");
    }
}
```

**End of Task 041.a Specification**
