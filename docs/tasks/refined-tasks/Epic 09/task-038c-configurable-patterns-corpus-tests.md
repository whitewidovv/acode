# Task 038.c: Configurable Patterns + Corpus Tests

**Priority:** P0 – Critical  
**Tier:** L – Feature Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 9 – Safety & Compliance  
**Dependencies:** Task 038, Task 002  

---

## Description

Task 038.c implements the configurable pattern system and corpus test framework for secret detection. This enables customization of detection patterns while maintaining validation through comprehensive test suites.

The pattern system supports multiple detection methods: regex patterns, known format signatures, entropy thresholds, and keyword heuristics. Patterns are organized by category (cloud providers, credentials, keys, tokens) and can be enabled/disabled per environment.

Corpus tests are the quality assurance mechanism. A curated test corpus contains known secrets (sanitized) that MUST be detected and known non-secrets that MUST NOT be flagged. Every pattern change requires corpus test validation.

### Business Value

Configurable patterns provide:
- Organization-specific detection
- Reduced false positives
- Custom format support
- Validated detection quality
- Pattern evolution over time

### Scope Boundaries

This task covers pattern configuration and corpus testing. Core engine is 038. Tool output is 038.a. Git blocking is 038.b.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Config Parser | Task 002 | Pattern config | From config.yml |
| Redaction Engine | Task 038 | Pattern list | Active patterns |
| CI Pipeline | Task 035 | Corpus tests | Validation |
| Policy Engine | Task 037 | Enable/disable | Per policy |
| Audit | Task 039 | Pattern changes | Track updates |
| Hot Reload | `IPatternLoader` | Refresh | Live update |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Invalid regex | Compile test | Skip pattern | Warning |
| Corpus test fail | Test runner | Block release | CI fail |
| Performance regression | Benchmark | Reject pattern | CI fail |
| False positive spike | Metrics | Tune pattern | User reports |
| False negative | Corpus miss | Add test case | Security risk |
| Pattern conflict | Overlap check | Warn | Inconsistent |
| Hot reload fail | Exception | Keep old | Warn user |
| Memory issue | Monitor | Optimize | Slow scan |

### Assumptions

1. **Patterns are YAML/JSON**: Structured config
2. **Corpus is versioned**: Git-tracked
3. **CI validates**: Before merge
4. **Hot reload supported**: For development
5. **Performance tested**: Each pattern
6. **Categories defined**: Organized groupings
7. **Defaults shipped**: Built-in patterns
8. **User patterns merge**: Additive to defaults

### Security Considerations

1. **Corpus sanitized**: No real secrets in tests
2. **Pattern review**: Changes require review
3. **Coverage tracking**: Know what's detected
4. **False negative critical**: Must add test
5. **Audit changes**: Who changed what
6. **No bypass via config**: Critical patterns locked
7. **Entropy baseline**: Configurable threshold
8. **Known formats required**: Major providers

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Secret Pattern | Detection rule |
| Regex Pattern | Regular expression rule |
| Known Format | Provider-specific signature |
| Entropy Analysis | Randomness detection |
| Keyword Heuristic | Context-based detection |
| Corpus Test | Known secret/non-secret set |
| False Positive | Non-secret flagged |
| False Negative | Secret missed |
| Pattern Category | Group (AWS, GitHub, etc.) |
| Coverage | Percentage of types detected |

---

## Out of Scope

- Machine learning patterns
- External pattern feeds
- Real-time pattern updates
- Pattern marketplace
- Community patterns
- Pattern encryption

---

## Functional Requirements

### FR-001 to FR-015: Pattern Configuration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038C-01 | Patterns MUST be configurable in YAML | P0 |
| FR-038C-02 | Pattern schema MUST be documented | P0 |
| FR-038C-03 | Pattern name MUST be unique | P0 |
| FR-038C-04 | Pattern category MUST be specified | P0 |
| FR-038C-05 | Pattern type MUST be specified | P0 |
| FR-038C-06 | Regex pattern MUST compile | P0 |
| FR-038C-07 | Pattern enabled MUST be configurable | P0 |
| FR-038C-08 | Pattern priority MUST be configurable | P1 |
| FR-038C-09 | Pattern description MUST be provided | P1 |
| FR-038C-10 | Pattern examples MUST be provided | P1 |
| FR-038C-11 | Pattern false positive notes | P2 |
| FR-038C-12 | Pattern performance hints | P2 |
| FR-038C-13 | Pattern version MUST be tracked | P1 |
| FR-038C-14 | Pattern author MUST be tracked | P2 |
| FR-038C-15 | Pattern changelog MUST exist | P2 |

### FR-016 to FR-030: Built-in Patterns

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038C-16 | AWS patterns MUST be included | P0 |
| FR-038C-17 | GitHub patterns MUST be included | P0 |
| FR-038C-18 | Azure patterns MUST be included | P0 |
| FR-038C-19 | GCP patterns MUST be included | P0 |
| FR-038C-20 | Generic password MUST be included | P0 |
| FR-038C-21 | Private key MUST be included | P0 |
| FR-038C-22 | JWT token MUST be included | P0 |
| FR-038C-23 | Connection string MUST be included | P0 |
| FR-038C-24 | API key generic MUST be included | P0 |
| FR-038C-25 | Bearer token MUST be included | P0 |
| FR-038C-26 | OAuth secrets MUST be included | P0 |
| FR-038C-27 | Database credentials MUST be included | P0 |
| FR-038C-28 | Slack tokens MUST be included | P1 |
| FR-038C-29 | Stripe keys MUST be included | P1 |
| FR-038C-30 | npm tokens MUST be included | P1 |

### FR-031 to FR-045: Custom Patterns

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038C-31 | Custom patterns MUST be additive | P0 |
| FR-038C-32 | Custom patterns MUST merge with built-in | P0 |
| FR-038C-33 | Custom patterns MUST validate | P0 |
| FR-038C-34 | Custom patterns MUST have corpus tests | P0 |
| FR-038C-35 | Custom pattern override MUST work | P1 |
| FR-038C-36 | Custom pattern disable MUST work | P0 |
| FR-038C-37 | Repo-level custom MUST be supported | P0 |
| FR-038C-38 | Global custom MUST be supported | P0 |
| FR-038C-39 | Custom priority MUST be configurable | P1 |
| FR-038C-40 | Custom category MUST be allowed | P1 |
| FR-038C-41 | Custom entropy threshold MUST work | P1 |
| FR-038C-42 | Custom keyword list MUST work | P1 |
| FR-038C-43 | Custom context rules MUST work | P2 |
| FR-038C-44 | Custom false positive rules MUST work | P2 |
| FR-038C-45 | Hot reload MUST work for custom | P1 |

### FR-046 to FR-060: Corpus Tests

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038C-46 | Corpus MUST include true positives | P0 |
| FR-038C-47 | Corpus MUST include true negatives | P0 |
| FR-038C-48 | Corpus MUST be versioned | P0 |
| FR-038C-49 | Corpus MUST be sanitized | P0 |
| FR-038C-50 | Corpus MUST run on every build | P0 |
| FR-038C-51 | Corpus failure MUST block merge | P0 |
| FR-038C-52 | Corpus coverage MUST be reported | P1 |
| FR-038C-53 | Pattern coverage MUST be tracked | P1 |
| FR-038C-54 | Missing coverage MUST warn | P0 |
| FR-038C-55 | Corpus MUST test each pattern | P0 |
| FR-038C-56 | False positive test MUST exist | P0 |
| FR-038C-57 | Performance test MUST exist | P1 |
| FR-038C-58 | Edge case tests MUST exist | P1 |
| FR-038C-59 | Encoding tests MUST exist | P1 |
| FR-038C-60 | Corpus MUST be extensible | P0 |

### FR-061 to FR-070: CLI Support

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038C-61 | `acode patterns list` MUST work | P0 |
| FR-038C-62 | List MUST show all patterns | P0 |
| FR-038C-63 | List MUST show enabled status | P0 |
| FR-038C-64 | `acode patterns test` MUST work | P0 |
| FR-038C-65 | Test MUST run corpus | P0 |
| FR-038C-66 | Test MUST report results | P0 |
| FR-038C-67 | `acode patterns validate` MUST work | P0 |
| FR-038C-68 | Validate MUST check syntax | P0 |
| FR-038C-69 | `acode patterns reload` MUST work | P1 |
| FR-038C-70 | Reload MUST hot reload | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-038C-01 | Pattern compile | <10ms each | P1 |
| NFR-038C-02 | Pattern match | <1ms each | P1 |
| NFR-038C-03 | All patterns match | <50ms | P0 |
| NFR-038C-04 | Hot reload | <100ms | P1 |
| NFR-038C-05 | Corpus full run | <30s | P1 |
| NFR-038C-06 | Memory per pattern | <1MB | P2 |
| NFR-038C-07 | Pattern count | 100+ | P1 |
| NFR-038C-08 | Regex backtrack limit | Enforced | P0 |
| NFR-038C-09 | Catastrophic regex prevention | Required | P0 |
| NFR-038C-10 | Benchmark regression | <10% | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-038C-11 | Corpus pass rate | 100% | P0 |
| NFR-038C-12 | False negative rate | 0% for corpus | P0 |
| NFR-038C-13 | False positive rate | <5% | P1 |
| NFR-038C-14 | Pattern validation | 100% | P0 |
| NFR-038C-15 | Hot reload stability | No crash | P0 |
| NFR-038C-16 | Thread safety | No races | P0 |
| NFR-038C-17 | Cross-platform | All OS | P0 |
| NFR-038C-18 | Unicode support | Full | P0 |
| NFR-038C-19 | Encoding robustness | UTF-8 | P0 |
| NFR-038C-20 | Graceful degradation | On error | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-038C-21 | Pattern load logged | Info | P0 |
| NFR-038C-22 | Pattern match logged | Debug | P1 |
| NFR-038C-23 | Corpus results logged | Info | P0 |
| NFR-038C-24 | Hot reload logged | Info | P0 |
| NFR-038C-25 | Metrics: pattern matches | Counter | P2 |
| NFR-038C-26 | Metrics: false positives | Counter | P2 |
| NFR-038C-27 | Events: pattern updated | Published | P1 |
| NFR-038C-28 | Structured logging | JSON | P0 |
| NFR-038C-29 | Coverage report | Machine-readable | P1 |
| NFR-038C-30 | Performance report | Benchmark | P1 |

---

## Acceptance Criteria / Definition of Done

### Configuration
- [ ] AC-001: YAML config works
- [ ] AC-002: Schema documented
- [ ] AC-003: Name unique
- [ ] AC-004: Category specified
- [ ] AC-005: Type specified
- [ ] AC-006: Regex compiles
- [ ] AC-007: Enable/disable works
- [ ] AC-008: Priority works

### Built-in
- [ ] AC-009: AWS patterns
- [ ] AC-010: GitHub patterns
- [ ] AC-011: Azure patterns
- [ ] AC-012: GCP patterns
- [ ] AC-013: Password patterns
- [ ] AC-014: Private key patterns
- [ ] AC-015: JWT patterns
- [ ] AC-016: Connection strings

### Custom
- [ ] AC-017: Custom additive
- [ ] AC-018: Merge with built-in
- [ ] AC-019: Validation works
- [ ] AC-020: Override works
- [ ] AC-021: Disable works
- [ ] AC-022: Repo level works
- [ ] AC-023: Global level works
- [ ] AC-024: Hot reload works

### Corpus
- [ ] AC-025: True positives exist
- [ ] AC-026: True negatives exist
- [ ] AC-027: Versioned
- [ ] AC-028: Sanitized
- [ ] AC-029: Runs on build
- [ ] AC-030: Blocks on failure
- [ ] AC-031: Coverage reported
- [ ] AC-032: Extensible

---

## User Verification Scenarios

### Scenario 1: View Patterns
**Persona:** Developer checking config  
**Preconditions:** System running  
**Steps:**
1. Run `acode patterns list`
2. See all patterns
3. Check enabled status
4. Review categories

**Verification Checklist:**
- [ ] List shows all
- [ ] Status correct
- [ ] Categories shown
- [ ] Format readable

### Scenario 2: Add Custom Pattern
**Persona:** Security admin  
**Preconditions:** Internal format exists  
**Steps:**
1. Add pattern to config
2. Add corpus test
3. Run validation
4. Hot reload

**Verification Checklist:**
- [ ] Pattern added
- [ ] Test added
- [ ] Validation passes
- [ ] Reload works

### Scenario 3: Disable Pattern
**Persona:** Developer with false positives  
**Preconditions:** Pattern causing issues  
**Steps:**
1. Set enabled: false
2. Reload patterns
3. Verify disabled
4. Check scan works

**Verification Checklist:**
- [ ] Config updated
- [ ] Reload works
- [ ] Pattern disabled
- [ ] Others work

### Scenario 4: Run Corpus Tests
**Persona:** CI pipeline  
**Preconditions:** Pattern changes  
**Steps:**
1. Run `acode patterns test`
2. All tests execute
3. Results reported
4. Coverage shown

**Verification Checklist:**
- [ ] Tests run
- [ ] Results clear
- [ ] Coverage shown
- [ ] Exit code correct

### Scenario 5: Pattern Performance
**Persona:** Developer adding pattern  
**Preconditions:** New regex  
**Steps:**
1. Add complex pattern
2. Run benchmark
3. Check performance
4. Optimize if needed

**Verification Checklist:**
- [ ] Benchmark runs
- [ ] Time measured
- [ ] Threshold checked
- [ ] Report clear

### Scenario 6: Catastrophic Regex Prevention
**Persona:** Developer with bad regex  
**Preconditions:** Backtracking regex  
**Steps:**
1. Add problematic pattern
2. Validation catches
3. Error shown
4. Rejected

**Verification Checklist:**
- [ ] Regex analyzed
- [ ] Problem detected
- [ ] Error clear
- [ ] Not loaded

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-038C-01 | YAML config parse | FR-038C-01 |
| UT-038C-02 | Pattern compile | FR-038C-06 |
| UT-038C-03 | Built-in AWS | FR-038C-16 |
| UT-038C-04 | Built-in GitHub | FR-038C-17 |
| UT-038C-05 | Custom additive | FR-038C-31 |
| UT-038C-06 | Custom merge | FR-038C-32 |
| UT-038C-07 | Enable/disable | FR-038C-07 |
| UT-038C-08 | Hot reload | FR-038C-45 |
| UT-038C-09 | Corpus true pos | FR-038C-46 |
| UT-038C-10 | Corpus true neg | FR-038C-47 |
| UT-038C-11 | CLI list | FR-038C-61 |
| UT-038C-12 | CLI test | FR-038C-64 |
| UT-038C-13 | Regex backtrack limit | NFR-038C-08 |
| UT-038C-14 | Performance check | NFR-038C-03 |
| UT-038C-15 | Thread safety | NFR-038C-16 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-038C-01 | Full pattern flow | E2E |
| IT-038C-02 | Corpus full run | FR-038C-50 |
| IT-038C-03 | All built-ins | FR-038C-16-30 |
| IT-038C-04 | Custom + built-in | FR-038C-32 |
| IT-038C-05 | Hot reload live | FR-038C-45 |
| IT-038C-06 | CI integration | FR-038C-51 |
| IT-038C-07 | Coverage report | FR-038C-52 |
| IT-038C-08 | Performance bench | NFR-038C-10 |
| IT-038C-09 | Cross-platform | NFR-038C-17 |
| IT-038C-10 | Unicode handling | NFR-038C-18 |
| IT-038C-11 | Config validation | FR-038C-67 |
| IT-038C-12 | Error handling | NFR-038C-20 |
| IT-038C-13 | Metrics export | NFR-038C-25 |
| IT-038C-14 | Logging | NFR-038C-21 |
| IT-038C-15 | Catastrophic prevention | NFR-038C-09 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Secrets/
│       └── Patterns/
│           ├── SecretPattern.cs
│           ├── PatternCategory.cs
│           ├── PatternType.cs
│           └── CorpusTestResult.cs
├── Acode.Application/
│   └── Secrets/
│       └── Patterns/
│           ├── IPatternProvider.cs
│           ├── IPatternValidator.cs
│           └── ICorpusTestRunner.cs
├── Acode.Infrastructure/
│   └── Secrets/
│       └── Patterns/
│           ├── PatternProvider.cs
│           ├── PatternValidator.cs
│           ├── CorpusTestRunner.cs
│           └── BuiltIn/
│               ├── AwsPatterns.cs
│               ├── GitHubPatterns.cs
│               ├── AzurePatterns.cs
│               └── GenericPatterns.cs
└── tests/
    └── corpus/
        ├── true-positives/
        │   ├── aws-keys.txt
        │   ├── github-tokens.txt
        │   └── ...
        └── true-negatives/
            ├── similar-but-not-secret.txt
            └── ...
```

### Pattern Schema

```yaml
# pattern definition
patterns:
  - name: "aws-access-key"
    category: cloud
    type: regex
    enabled: true
    priority: 100
    pattern: 'AKIA[0-9A-Z]{16}'
    description: "AWS Access Key ID"
    examples:
      - "AKIAIOSFODNN7EXAMPLE"
    false_positive_notes: |
      May match similar patterns in test data.
    performance: "O(n) linear scan"
    version: "1.0"
```

### Corpus Structure

```
corpus/
├── README.md
├── true-positives/
│   ├── aws/
│   │   ├── access-key.txt  # AKIAIOSFODNN7EXAMPLE
│   │   └── secret-key.txt
│   ├── github/
│   │   ├── pat.txt
│   │   └── oauth.txt
│   └── generic/
│       ├── password.txt
│       └── api-key.txt
└── true-negatives/
    ├── similar-strings.txt
    ├── base64-but-not-secret.txt
    └── high-entropy-but-safe.txt
```

**End of Task 038.c Specification**
