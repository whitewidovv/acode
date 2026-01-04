# Task 035.c: CI-Specific Task Runner Support

**Priority:** P1 – High  
**Tier:** L – Feature Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 8 – CI/CD Integration  
**Dependencies:** Task 035, Task 034  

---

## Description

Task 035.c implements CI-specific task runner integration for workflow maintenance. CI environments often use different tools (Make, Just, Nx, Turborepo) that require specialized handling. The maintenance engine MUST understand and work with these tools correctly.

Task runners abstract build/test/lint commands behind consistent interfaces. Workflows often call these task runners instead of raw commands. Maintenance operations MUST preserve task runner invocations while updating surrounding workflow configuration.

This task ensures compatibility with common task runners including Make, Just, Nx, Turborepo, and custom script runners. Detection, validation, and preservation of task runner patterns are essential.

### Business Value

Task runner support provides:
- Accurate maintenance for real-world workflows
- Preservation of established patterns
- Compatibility with monorepo tools
- Reduced false positives in analysis

### Scope Boundaries

This task covers task runner detection and compatibility. Workflow analysis is Task 035. Approval gates are 035.b. Task execution is NOT in scope.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Workflow Parser | Task 034 | Parsed YAML | Input workflows |
| Maintenance Engine | Task 035 | Context | For analysis |
| Pattern Detectors | `IPatternDetector` | Task runner info | Specialized |
| Change Proposer | Task 035.a | Preserve patterns | In proposals |
| Repository Config | `agent-config.yml` | Task runner config | Optional |
| File System | `IFileSystem` | Detect runner files | Makefile, etc. |
| Registry | `ITaskRunnerRegistry` | Known runners | Extensible |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Unknown task runner | No match in registry | Warn, continue | May miss patterns |
| Runner file missing | File not found | Fallback to raw | Degraded analysis |
| Version detection fails | Parse error | Skip version check | No version info |
| Custom runner undetected | No config | Manual config needed | Must configure |
| Conflicting runners | Multiple detected | Use priority | May need override |
| Pattern false positive | Wrong detection | User override | Manual correction |
| Cache detection fails | IO error | Skip cache | No cache info |
| Config parse error | Invalid YAML | Error message | Must fix config |

### Assumptions

1. **Common runners used**: Make, Just, Nx, Turborepo, etc.
2. **Standard patterns**: Runners follow common invocation patterns
3. **File-based detection**: Runner config files present
4. **Version extractable**: Version can be determined
5. **Registry extensible**: Can add custom runners
6. **Override available**: User can force detection
7. **Single primary runner**: One main runner per project
8. **Compatible proposals**: Changes preserve runner calls

### Security Considerations

1. **No execution**: Never execute task runners
2. **Read-only detection**: Only read config files
3. **Safe parsing**: Handle malformed configs
4. **No injection**: Config values not executed
5. **Path validation**: Runner paths validated
6. **Trusted registry**: Only known runners
7. **User control**: Overrides require explicit config
8. **Audit logging**: Detection logged

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Task Runner | Tool that manages build tasks |
| Make | Classic Unix build tool |
| Just | Command runner (justfile) |
| Nx | Smart monorepo build system |
| Turborepo | High-performance build system |
| Registry | Known task runner database |
| Detection | Finding task runner in repo |
| Preservation | Keeping runner invocations |

---

## Out of Scope

- Task runner execution
- Task runner installation
- Build orchestration
- Custom DSL for runners
- Runner migration tools
- Performance profiling of runners

---

## Functional Requirements

### FR-001 to FR-020: Task Runner Detection

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035C-01 | `ITaskRunnerDetector` interface MUST exist | P0 |
| FR-035C-02 | `DetectAsync` MUST find runners in repo | P0 |
| FR-035C-03 | Make detection MUST look for Makefile | P0 |
| FR-035C-04 | Just detection MUST look for justfile | P0 |
| FR-035C-05 | Nx detection MUST look for nx.json | P0 |
| FR-035C-06 | Turborepo detection MUST look for turbo.json | P0 |
| FR-035C-07 | npm scripts MUST be detected | P1 |
| FR-035C-08 | Detection MUST return runner metadata | P0 |
| FR-035C-09 | Metadata MUST include runner type | P0 |
| FR-035C-10 | Metadata MUST include config file path | P0 |
| FR-035C-11 | Metadata MUST include version if available | P1 |
| FR-035C-12 | Multiple runners MUST be detected | P1 |
| FR-035C-13 | Primary runner MUST be identified | P1 |
| FR-035C-14 | Detection priority MUST be configurable | P2 |
| FR-035C-15 | agent-config.yml override MUST work | P1 |
| FR-035C-16 | Detection MUST cache results | P2 |
| FR-035C-17 | Cache MUST invalidate on file change | P2 |
| FR-035C-18 | Unknown runner MUST warn | P1 |
| FR-035C-19 | Detection MUST be fast (<500ms) | P1 |
| FR-035C-20 | Recursive detection for monorepos | P2 |

### FR-021 to FR-040: Task Runner Registry

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035C-21 | `ITaskRunnerRegistry` interface MUST exist | P0 |
| FR-035C-22 | Registry MUST include Make | P0 |
| FR-035C-23 | Registry MUST include Just | P0 |
| FR-035C-24 | Registry MUST include Nx | P0 |
| FR-035C-25 | Registry MUST include Turborepo | P0 |
| FR-035C-26 | Registry MUST include npm/yarn/pnpm scripts | P0 |
| FR-035C-27 | Registry MUST include Gradle | P1 |
| FR-035C-28 | Registry MUST include Maven | P1 |
| FR-035C-29 | Registry MUST include MSBuild | P1 |
| FR-035C-30 | Registry entries MUST have invocation pattern | P0 |
| FR-035C-31 | Registry entries MUST have config file names | P0 |
| FR-035C-32 | Registry entries MUST have version extraction | P1 |
| FR-035C-33 | Registry MUST support custom entries | P1 |
| FR-035C-34 | Custom entries via agent-config.yml | P1 |
| FR-035C-35 | Registry MUST be extensible | P1 |
| FR-035C-36 | Registry lookup MUST be O(1) | P2 |
| FR-035C-37 | Registry MUST validate entries | P1 |
| FR-035C-38 | Invalid entries MUST error | P1 |
| FR-035C-39 | Registry MUST be singleton | P2 |
| FR-035C-40 | Registry MUST log loaded entries | P2 |

### FR-041 to FR-060: Workflow Pattern Preservation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035C-41 | `ITaskRunnerPreserver` interface MUST exist | P0 |
| FR-035C-42 | Preserver MUST identify runner calls in workflow | P0 |
| FR-035C-43 | Make calls (`make target`) MUST be preserved | P0 |
| FR-035C-44 | Just calls (`just target`) MUST be preserved | P0 |
| FR-035C-45 | Nx calls (`npx nx ...`) MUST be preserved | P0 |
| FR-035C-46 | Turbo calls (`npx turbo ...`) MUST be preserved | P0 |
| FR-035C-47 | npm script calls MUST be preserved | P0 |
| FR-035C-48 | Preservation MUST apply to proposals | P0 |
| FR-035C-49 | Proposals MUST NOT change runner invocations | P0 |
| FR-035C-50 | Proposals MUST update runner version if needed | P1 |
| FR-035C-51 | Version update MUST be separate proposal | P1 |
| FR-035C-52 | Preserver MUST validate runner in workflow | P1 |
| FR-035C-53 | Missing runner MUST warn | P1 |
| FR-035C-54 | Runner mismatch MUST be detected | P1 |
| FR-035C-55 | Preserver MUST log actions | P2 |
| FR-035C-56 | Pattern matching MUST be configurable | P2 |
| FR-035C-57 | False positives MUST be overridable | P2 |
| FR-035C-58 | Preserver MUST emit events | P2 |
| FR-035C-59 | Metrics on preserved patterns | P2 |
| FR-035C-60 | Preserver MUST be testable | P1 |

### FR-061 to FR-075: Monorepo Support

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035C-61 | Monorepo detection MUST work | P1 |
| FR-035C-62 | Nx workspaces MUST be detected | P1 |
| FR-035C-63 | Turborepo workspaces MUST be detected | P1 |
| FR-035C-64 | Lerna detection MUST work | P2 |
| FR-035C-65 | Yarn workspaces MUST be detected | P1 |
| FR-035C-66 | pnpm workspaces MUST be detected | P1 |
| FR-035C-67 | Per-package runners MUST be found | P1 |
| FR-035C-68 | Root runner MUST be identified | P1 |
| FR-035C-69 | Package paths MUST be recorded | P1 |
| FR-035C-70 | Affected package analysis MUST work | P2 |
| FR-035C-71 | Cross-package dependencies MUST be known | P2 |
| FR-035C-72 | Filtered builds MUST be detected | P2 |
| FR-035C-73 | Cache strategies MUST be identified | P2 |
| FR-035C-74 | Parallel execution MUST be detected | P2 |
| FR-035C-75 | Monorepo config MUST be parsed | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-035C-01 | Detection latency | <500ms | P1 |
| NFR-035C-02 | Registry lookup | <1ms | P1 |
| NFR-035C-03 | Pattern matching | <50ms | P1 |
| NFR-035C-04 | Cache hit lookup | <10ms | P2 |
| NFR-035C-05 | Config file parsing | <100ms | P1 |
| NFR-035C-06 | Monorepo scan | <2s | P2 |
| NFR-035C-07 | Memory for registry | <5MB | P2 |
| NFR-035C-08 | Startup time impact | <200ms | P1 |
| NFR-035C-09 | Concurrent detection | Supported | P2 |
| NFR-035C-10 | File system reads | Minimized | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-035C-11 | Detection accuracy | >95% | P0 |
| NFR-035C-12 | Pattern preservation | 100% | P0 |
| NFR-035C-13 | No false modifications | 0% | P0 |
| NFR-035C-14 | Graceful unknown handling | Always | P1 |
| NFR-035C-15 | Config error recovery | Always | P1 |
| NFR-035C-16 | Registry stability | No crashes | P0 |
| NFR-035C-17 | Cache consistency | Always valid | P1 |
| NFR-035C-18 | Version extraction accuracy | >90% | P1 |
| NFR-035C-19 | Monorepo detection accuracy | >90% | P1 |
| NFR-035C-20 | Override reliability | 100% | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-035C-21 | Detection logged | Structured | P1 |
| NFR-035C-22 | Runners found logged | Info level | P1 |
| NFR-035C-23 | Unknown runners warned | Warning level | P0 |
| NFR-035C-24 | Preservation logged | Debug level | P2 |
| NFR-035C-25 | Metrics: runners detected | Counter | P2 |
| NFR-035C-26 | Metrics: patterns preserved | Counter | P2 |
| NFR-035C-27 | Performance metrics | Histogram | P2 |
| NFR-035C-28 | Config parse errors logged | Error level | P0 |
| NFR-035C-29 | Override usage logged | Info level | P1 |
| NFR-035C-30 | Registry state logged | Debug level | P2 |

---

## Mode Compliance

| Mode | Task Runner Behavior |
|------|---------------------|
| Local-Only | Detection from local files only |
| Burst | Same as Local-Only |
| Air-Gapped | Full support, no network |

---

## Acceptance Criteria / Definition of Done

### Detection
- [ ] AC-001: `ITaskRunnerDetector` interface exists
- [ ] AC-002: Make detected by Makefile
- [ ] AC-003: Just detected by justfile
- [ ] AC-004: Nx detected by nx.json
- [ ] AC-005: Turborepo detected by turbo.json
- [ ] AC-006: npm scripts detected
- [ ] AC-007: Multiple runners detected
- [ ] AC-008: Primary runner identified

### Registry
- [ ] AC-009: `ITaskRunnerRegistry` interface exists
- [ ] AC-010: Make entry exists
- [ ] AC-011: Just entry exists
- [ ] AC-012: Nx entry exists
- [ ] AC-013: Turborepo entry exists
- [ ] AC-014: npm/yarn/pnpm entries exist
- [ ] AC-015: Custom entries supported
- [ ] AC-016: agent-config.yml integration

### Preservation
- [ ] AC-017: `ITaskRunnerPreserver` interface exists
- [ ] AC-018: Make calls preserved
- [ ] AC-019: Just calls preserved
- [ ] AC-020: Nx calls preserved
- [ ] AC-021: Turbo calls preserved
- [ ] AC-022: npm script calls preserved
- [ ] AC-023: Proposals don't change runner calls
- [ ] AC-024: Version updates separate

### Monorepo
- [ ] AC-025: Nx workspace detected
- [ ] AC-026: Turborepo workspace detected
- [ ] AC-027: Yarn/pnpm workspaces detected
- [ ] AC-028: Per-package runners found
- [ ] AC-029: Root runner identified
- [ ] AC-030: Package paths recorded

### Validation
- [ ] AC-031: Unknown runner warns
- [ ] AC-032: Detection caches results
- [ ] AC-033: Cache invalidates correctly
- [ ] AC-034: Override works
- [ ] AC-035: Logging complete
- [ ] AC-036: Performance < 500ms

---

## User Verification Scenarios

### Scenario 1: Makefile Detected
**Persona:** Developer with Make-based project  
**Preconditions:** Makefile exists  
**Steps:**
1. Run `acode ci maintain analyze`
2. Detection runs
3. Make runner found
4. Make calls preserved in proposals

**Verification Checklist:**
- [ ] Make detected
- [ ] Version extracted if possible
- [ ] Proposals preserve `make` calls
- [ ] No false modifications

### Scenario 2: Nx Monorepo Support
**Persona:** Developer with Nx workspace  
**Preconditions:** nx.json and packages exist  
**Steps:**
1. Run analysis
2. Nx detected
3. Packages found
4. `npx nx` calls preserved

**Verification Checklist:**
- [ ] Nx detected
- [ ] Workspace understood
- [ ] Package runners found
- [ ] Nx calls preserved

### Scenario 3: Custom Runner Config
**Persona:** Developer with custom task runner  
**Preconditions:** Custom runner not in registry  
**Steps:**
1. Warning: unknown runner
2. Add to agent-config.yml
3. Re-run analysis
4. Custom runner detected

**Verification Checklist:**
- [ ] Warning shown
- [ ] Config accepted
- [ ] Custom runner found
- [ ] Calls preserved

### Scenario 4: Multiple Runners
**Persona:** Developer with Make + npm scripts  
**Preconditions:** Makefile and package.json  
**Steps:**
1. Run analysis
2. Both runners detected
3. Primary identified
4. Both patterns preserved

**Verification Checklist:**
- [ ] Both detected
- [ ] Primary correct
- [ ] Make calls preserved
- [ ] npm scripts preserved

### Scenario 5: Just Command Runner
**Persona:** Developer using Just  
**Preconditions:** justfile exists  
**Steps:**
1. Run analysis
2. Just detected
3. Proposals preserve `just` calls
4. Recipes not modified

**Verification Checklist:**
- [ ] Just detected
- [ ] Version extracted
- [ ] `just` calls preserved
- [ ] Correct analysis

### Scenario 6: Turborepo Pipeline
**Persona:** Developer with Turborepo  
**Preconditions:** turbo.json exists  
**Steps:**
1. Run analysis
2. Turborepo detected
3. Pipeline understood
4. `npx turbo` calls preserved

**Verification Checklist:**
- [ ] Turborepo detected
- [ ] Packages found
- [ ] Turbo calls preserved
- [ ] Cache config recognized

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-035C-01 | Make detection by Makefile | FR-035C-03 |
| UT-035C-02 | Just detection by justfile | FR-035C-04 |
| UT-035C-03 | Nx detection by nx.json | FR-035C-05 |
| UT-035C-04 | Turborepo detection | FR-035C-06 |
| UT-035C-05 | npm scripts detection | FR-035C-07 |
| UT-035C-06 | Registry lookup O(1) | FR-035C-36 |
| UT-035C-07 | Custom registry entry | FR-035C-33 |
| UT-035C-08 | Make call preservation | FR-035C-43 |
| UT-035C-09 | Just call preservation | FR-035C-44 |
| UT-035C-10 | Nx call preservation | FR-035C-45 |
| UT-035C-11 | Turbo call preservation | FR-035C-46 |
| UT-035C-12 | Multiple runner detection | FR-035C-12 |
| UT-035C-13 | Config override | FR-035C-15 |
| UT-035C-14 | Detection caching | FR-035C-16 |
| UT-035C-15 | Detection < 500ms | NFR-035C-01 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-035C-01 | Full detection flow | E2E |
| IT-035C-02 | Preservation in proposals | FR-035C-48 |
| IT-035C-03 | Monorepo Nx detection | FR-035C-62 |
| IT-035C-04 | Monorepo Turborepo detection | FR-035C-63 |
| IT-035C-05 | Custom runner via config | FR-035C-34 |
| IT-035C-06 | Multiple runners priority | FR-035C-13 |
| IT-035C-07 | Cache invalidation | FR-035C-17 |
| IT-035C-08 | Unknown runner warning | FR-035C-18 |
| IT-035C-09 | Version extraction | FR-035C-11 |
| IT-035C-10 | Gradle detection | FR-035C-27 |
| IT-035C-11 | Maven detection | FR-035C-28 |
| IT-035C-12 | MSBuild detection | FR-035C-29 |
| IT-035C-13 | Yarn workspaces | FR-035C-65 |
| IT-035C-14 | pnpm workspaces | FR-035C-66 |
| IT-035C-15 | Performance benchmark | NFR-035C-01 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── CiCd/
│       └── Maintenance/
│           └── TaskRunners/
│               ├── TaskRunnerInfo.cs
│               ├── TaskRunnerType.cs
│               └── MonorepoInfo.cs
├── Acode.Application/
│   └── CiCd/
│       └── Maintenance/
│           └── TaskRunners/
│               ├── ITaskRunnerDetector.cs
│               ├── ITaskRunnerRegistry.cs
│               └── ITaskRunnerPreserver.cs
└── Acode.Infrastructure/
    └── CiCd/
        └── Maintenance/
            └── TaskRunners/
                ├── TaskRunnerDetector.cs
                ├── InMemoryTaskRunnerRegistry.cs
                ├── TaskRunnerPreserver.cs
                └── Runners/
                    ├── MakeRunner.cs
                    ├── JustRunner.cs
                    ├── NxRunner.cs
                    ├── TurborepoRunner.cs
                    └── NpmScriptsRunner.cs
```

**End of Task 035.c Specification**
