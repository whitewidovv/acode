# Task 001.a: Define Mode Matrix (LocalOnly / Burst / Airgapped)

**Priority:** 5 / 49  
**Tier:** Foundation  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** Task 001 (parent task defines mode concepts)  

---

## Description

### Overview

Task 001.a creates the comprehensive mode matrix that defines the precise capabilities, restrictions, and behaviors for each of Acode's three operating modes: LocalOnly, Burst, and Airgapped. This matrix serves as the authoritative reference for all mode-related decisions in the codebase.

The mode matrix is not just documentation—it is a specification that will be implemented as code. Every row in the matrix corresponds to a runtime check. Every capability listed will be enforced programmatically.

### Business Value

A clear mode matrix delivers:

1. **Unambiguous Implementation** — Developers know exactly what each mode permits
2. **Testable Specifications** — Each matrix cell becomes a test case
3. **User Communication** — Users can understand exactly what each mode does
4. **Security Auditing** — Auditors can verify implementation matches specification
5. **Compliance Mapping** — Map modes to regulatory requirements

### Scope Boundaries

**In Scope:**
- Complete capability matrix for all three modes
- Network access permissions per mode
- LLM provider permissions per mode
- File system access permissions per mode
- Tool execution permissions per mode
- Data transmission rules per mode
- Mode persistence rules
- Mode transition prerequisites

**Out of Scope:**
- Implementation of mode enforcement (parent Task 001)
- Validation rule implementation (Task 001.b)
- Constraint documentation (Task 001.c)
- Network blocking implementation (Task 007)
- Provider implementation (Tasks 004-006)

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 001 | Parent | Defines mode architecture |
| Task 001.b | Sibling | Uses matrix for validation rules |
| Task 001.c | Sibling | References matrix in docs |
| Task 002 | Consumer | Config schema supports modes |
| Tasks 004-006 | Consumer | Providers check matrix |
| Task 007 | Consumer | Network blocking uses matrix |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Matrix incomplete | Implementation gaps | Comprehensive enumeration |
| Matrix ambiguous | Inconsistent behavior | Clear yes/no/conditional |
| Matrix conflicts | Implementation confusion | Review for consistency |
| Matrix outdated | Drift from implementation | Version with code |

### Assumptions

1. Three modes are sufficient for all use cases
2. Modes are mutually exclusive
3. Mode capabilities are static (don't change at runtime)
4. All features can be categorized into matrix rows
5. Conditional capabilities can be expressed clearly

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Mode Matrix** | Table defining capabilities per operating mode |
| **Capability** | Specific action or resource access |
| **Permission** | Allowed, Denied, or Conditional |
| **Conditional** | Allowed with specific prerequisites |
| **Network Scope** | Classification of network access (none, local, limited, full) |
| **Localhost** | Network access limited to 127.0.0.1 |
| **Local Network** | Same machine or LAN |
| **External Network** | Internet / WAN access |
| **LLM Provider** | Service providing AI inference |
| **Local Provider** | LLM running on same machine (Ollama) |
| **Remote Provider** | LLM accessed via internet API |
| **Tool Execution** | Running external commands |
| **Sandboxed Execution** | Execution with restricted permissions |
| **Data Transmission** | Sending data off the local machine |
| **Prompt Transmission** | Sending prompts to external service |
| **Code Transmission** | Sending source code externally |

---

## Out of Scope

- Implementation of mode checks in code
- Validation service implementation
- Network blocking implementation
- Provider adapter implementation
- CLI mode selection implementation
- Configuration parsing implementation
- Audit logging implementation
- Consent flow implementation
- Security audit procedures
- Compliance certification

---

## Functional Requirements

### Mode Matrix Definition (FR-001a-01 to FR-001a-30)

| ID | Requirement |
|----|-------------|
| FR-001a-01 | Matrix MUST define all three modes |
| FR-001a-02 | Matrix MUST cover network access |
| FR-001a-03 | Matrix MUST cover LLM provider access |
| FR-001a-04 | Matrix MUST cover file system access |
| FR-001a-05 | Matrix MUST cover tool execution |
| FR-001a-06 | Matrix MUST cover data transmission |
| FR-001a-07 | Matrix MUST cover mode persistence |
| FR-001a-08 | Matrix MUST cover mode transitions |
| FR-001a-09 | Each cell MUST be Allowed/Denied/Conditional |
| FR-001a-10 | Conditional cells MUST specify prerequisites |
| FR-001a-11 | Matrix MUST be stored in code as data |
| FR-001a-12 | Matrix MUST be queryable at runtime |
| FR-001a-13 | Matrix MUST be versioned with code |
| FR-001a-14 | Matrix MUST be testable |
| FR-001a-15 | Matrix MUST be documented |
| FR-001a-16 | Matrix MUST handle edge cases |
| FR-001a-17 | Matrix MUST be consistent |
| FR-001a-18 | Matrix MUST be complete (no gaps) |
| FR-001a-19 | Matrix MUST be unambiguous |
| FR-001a-20 | Matrix MUST be the single source of truth |
| FR-001a-21 | Matrix changes MUST require code review |
| FR-001a-22 | Matrix MUST support mode comparison |
| FR-001a-23 | Matrix MUST identify mode by enum |
| FR-001a-24 | Matrix MUST support capability enumeration |
| FR-001a-25 | Matrix MUST support permission checking |
| FR-001a-26 | Matrix MUST be immutable at runtime |
| FR-001a-27 | Matrix MUST be efficiently accessible |
| FR-001a-28 | Matrix MUST support serialization |
| FR-001a-29 | Matrix MUST support display/formatting |
| FR-001a-30 | Matrix MUST include rationale comments |

### LocalOnly Mode Capabilities (FR-001a-31 to FR-001a-50)

| ID | Requirement |
|----|-------------|
| FR-001a-31 | LocalOnly MUST allow localhost network (127.0.0.1) |
| FR-001a-32 | LocalOnly MUST allow Ollama API calls |
| FR-001a-33 | LocalOnly MUST allow local file reads |
| FR-001a-34 | LocalOnly MUST allow local file writes |
| FR-001a-35 | LocalOnly MUST allow tool execution |
| FR-001a-36 | LocalOnly MUST allow NuGet/npm downloads |
| FR-001a-37 | LocalOnly MUST allow git operations |
| FR-001a-38 | LocalOnly MUST NOT allow external LLM APIs |
| FR-001a-39 | LocalOnly MUST NOT allow code transmission to cloud |
| FR-001a-40 | LocalOnly MUST NOT allow prompt transmission to cloud |
| FR-001a-41 | LocalOnly MUST NOT allow telemetry |
| FR-001a-42 | LocalOnly MUST be the default mode |
| FR-001a-43 | LocalOnly MUST allow transition to Burst (with consent) |
| FR-001a-44 | LocalOnly MUST allow transition to Airgapped (config) |
| FR-001a-45 | LocalOnly MUST persist across sessions |
| FR-001a-46 | LocalOnly MUST be overridable by CLI |
| FR-001a-47 | LocalOnly MUST be settable via config |
| FR-001a-48 | LocalOnly MUST be settable via environment |
| FR-001a-49 | LocalOnly MUST log mode on startup |
| FR-001a-50 | LocalOnly MUST display mode on status check |

### Burst Mode Capabilities (FR-001a-51 to FR-001a-70)

| ID | Requirement |
|----|-------------|
| FR-001a-51 | Burst MUST require explicit consent |
| FR-001a-52 | Burst MUST allow external LLM APIs |
| FR-001a-53 | Burst MUST allow localhost network |
| FR-001a-54 | Burst MUST allow local file reads |
| FR-001a-55 | Burst MUST allow local file writes |
| FR-001a-56 | Burst MUST allow tool execution |
| FR-001a-57 | Burst MUST allow prompt transmission |
| FR-001a-58 | Burst MUST redact secrets before transmission |
| FR-001a-59 | Burst MUST log all external API calls |
| FR-001a-60 | Burst MUST be session-scoped only |
| FR-001a-61 | Burst MUST NOT persist across sessions |
| FR-001a-62 | Burst MUST NOT be settable in config |
| FR-001a-63 | Burst MUST specify target provider |
| FR-001a-64 | Burst MUST allow transition to LocalOnly |
| FR-001a-65 | Burst MUST NOT allow transition to Airgapped |
| FR-001a-66 | Burst consent MUST specify data shared |
| FR-001a-67 | Burst consent MUST be revocable |
| FR-001a-68 | Burst MUST track API calls for audit |
| FR-001a-69 | Burst MUST NOT allow bulk code upload |
| FR-001a-70 | Burst MUST minimize data in prompts |

### Airgapped Mode Capabilities (FR-001a-71 to FR-001a-90)

| ID | Requirement |
|----|-------------|
| FR-001a-71 | Airgapped MUST prohibit ALL network access |
| FR-001a-72 | Airgapped MUST NOT allow localhost connections |
| FR-001a-73 | Airgapped MUST NOT allow any DNS lookups |
| FR-001a-74 | Airgapped MUST work with pre-loaded models |
| FR-001a-75 | Airgapped MUST allow local file reads |
| FR-001a-76 | Airgapped MUST allow local file writes |
| FR-001a-77 | Airgapped MUST allow sandboxed tool execution |
| FR-001a-78 | Airgapped MUST NOT allow tool network access |
| FR-001a-79 | Airgapped MUST be config-only (not CLI) |
| FR-001a-80 | Airgapped MUST persist permanently |
| FR-001a-81 | Airgapped MUST NOT allow mode transitions |
| FR-001a-82 | Airgapped MUST NOT allow transition to LocalOnly |
| FR-001a-83 | Airgapped MUST NOT allow transition to Burst |
| FR-001a-84 | Airgapped MUST log all blocked network attempts |
| FR-001a-85 | Airgapped MUST provide clear setup docs |
| FR-001a-86 | Airgapped MUST validate model availability |
| FR-001a-87 | Airgapped MUST fail gracefully if model missing |
| FR-001a-88 | Airgapped MUST support enterprise deployment |
| FR-001a-89 | Airgapped MUST be auditable |
| FR-001a-90 | Airgapped MUST have no escape hatches |

---

## Non-Functional Requirements

### Accuracy (NFR-001a-01 to NFR-001a-08)

| ID | Requirement |
|----|-------------|
| NFR-001a-01 | Matrix MUST be 100% accurate to implementation |
| NFR-001a-02 | Matrix MUST be updated with code changes |
| NFR-001a-03 | Matrix MUST be reviewed for accuracy |
| NFR-001a-04 | Matrix MUST pass automated consistency checks |
| NFR-001a-05 | Matrix MUST match user documentation |
| NFR-001a-06 | Matrix MUST match security documentation |
| NFR-001a-07 | Matrix MUST be validated by tests |
| NFR-001a-08 | Matrix drift MUST be detected by CI |

### Completeness (NFR-001a-09 to NFR-001a-15)

| ID | Requirement |
|----|-------------|
| NFR-001a-09 | Matrix MUST cover all capabilities |
| NFR-001a-10 | Matrix MUST have no undefined cells |
| NFR-001a-11 | Matrix MUST cover all modes |
| NFR-001a-12 | Matrix MUST cover all transitions |
| NFR-001a-13 | Matrix MUST cover all persistence rules |
| NFR-001a-14 | Matrix MUST cover edge cases |
| NFR-001a-15 | Matrix gaps MUST be flagged by static analysis |

### Usability (NFR-001a-16 to NFR-001a-22)

| ID | Requirement |
|----|-------------|
| NFR-001a-16 | Matrix MUST be human-readable |
| NFR-001a-17 | Matrix MUST be machine-parseable |
| NFR-001a-18 | Matrix MUST be printable |
| NFR-001a-19 | Matrix MUST be searchable |
| NFR-001a-20 | Matrix MUST include examples |
| NFR-001a-21 | Matrix MUST include rationales |
| NFR-001a-22 | Matrix MUST be accessible to non-developers |

### Performance (NFR-001a-23 to NFR-001a-28)

| ID | Requirement |
|----|-------------|
| NFR-001a-23 | Matrix lookup MUST be O(1) |
| NFR-001a-24 | Matrix loading MUST be under 10ms |
| NFR-001a-25 | Matrix MUST be cacheable |
| NFR-001a-26 | Matrix MUST NOT require file I/O per check |
| NFR-001a-27 | Matrix size MUST be under 10KB |
| NFR-001a-28 | Matrix iteration MUST be efficient |

---

## User Manual Documentation

### Mode Matrix Overview

The mode matrix is the authoritative reference for what each operating mode allows and prohibits. Every runtime check in Acode references this matrix.

### Complete Mode Matrix

#### Network Access

| Capability | LocalOnly | Burst | Airgapped |
|------------|-----------|-------|-----------|
| Localhost (127.0.0.1) | ✅ Allowed | ✅ Allowed | ❌ Denied |
| Local network (LAN) | ❌ Denied | ✅ Allowed | ❌ Denied |
| External network | ❌ Denied | ✅ Allowed | ❌ Denied |
| DNS lookups | ✅ Allowed* | ✅ Allowed | ❌ Denied |

*Only for package downloads, not LLM

#### LLM Provider Access

| Capability | LocalOnly | Burst | Airgapped |
|------------|-----------|-------|-----------|
| Ollama (local) | ✅ Allowed | ✅ Allowed | ✅ Allowed* |
| OpenAI API | ❌ Denied | ⚠️ Conditional† | ❌ Denied |
| Anthropic API | ❌ Denied | ⚠️ Conditional† | ❌ Denied |
| Azure OpenAI | ❌ Denied | ⚠️ Conditional† | ❌ Denied |
| Custom API | ❌ Denied | ⚠️ Conditional† | ❌ Denied |

*Pre-loaded models only, no network  
†Requires explicit consent

#### File System Access

| Capability | LocalOnly | Burst | Airgapped |
|------------|-----------|-------|-----------|
| Read project files | ✅ Allowed | ✅ Allowed | ✅ Allowed |
| Write project files | ✅ Allowed | ✅ Allowed | ✅ Allowed |
| Read system files | ⚠️ Limited | ⚠️ Limited | ⚠️ Limited |
| Write system files | ❌ Denied | ❌ Denied | ❌ Denied |
| Read home directory | ✅ Allowed | ✅ Allowed | ✅ Allowed |
| Write to ~/.acode | ✅ Allowed | ✅ Allowed | ✅ Allowed |

#### Tool Execution

| Capability | LocalOnly | Burst | Airgapped |
|------------|-----------|-------|-----------|
| dotnet CLI | ✅ Allowed | ✅ Allowed | ✅ Allowed |
| git operations | ✅ Allowed | ✅ Allowed | ⚠️ Local only |
| npm/yarn | ✅ Allowed | ✅ Allowed | ❌ Denied* |
| Custom tools | ✅ Allowed | ✅ Allowed | ⚠️ Sandboxed |
| Shell commands | ✅ Allowed | ✅ Allowed | ⚠️ Sandboxed |

*Network would be required for installs

#### Data Transmission

| Capability | LocalOnly | Burst | Airgapped |
|------------|-----------|-------|-----------|
| Send prompts externally | ❌ Denied | ⚠️ Conditional† | ❌ Denied |
| Send code snippets | ❌ Denied | ⚠️ Conditional† | ❌ Denied |
| Send full files | ❌ Denied | ❌ Denied | ❌ Denied |
| Send repository data | ❌ Denied | ❌ Denied | ❌ Denied |
| Send telemetry | ❌ Denied | ❌ Denied | ❌ Denied |
| Send crash reports | ❌ Denied | ⚠️ Optional | ❌ Denied |

†With consent, limited context only

#### Mode Persistence

| Property | LocalOnly | Burst | Airgapped |
|----------|-----------|-------|-----------|
| Persists across sessions | ✅ Yes | ❌ No | ✅ Yes |
| Configurable in file | ✅ Yes | ❌ No | ✅ Yes |
| CLI override allowed | ✅ Yes | ✅ Yes | ❌ No |
| Env var override allowed | ✅ Yes | ✅ Yes | ❌ No |
| Is default mode | ✅ Yes | ❌ No | ❌ No |

#### Mode Transitions

| From → To | LocalOnly | Burst | Airgapped |
|-----------|-----------|-------|-----------|
| **LocalOnly** | — | ⚠️ Conditional* | ⚠️ Config only |
| **Burst** | ✅ Allowed | — | ❌ Denied |
| **Airgapped** | ❌ Denied | ❌ Denied | — |

*Requires explicit consent

### Reading the Matrix

**Legend:**
- ✅ **Allowed** — Action is permitted unconditionally
- ❌ **Denied** — Action is prohibited, will fail with error
- ⚠️ **Conditional** — Action is permitted only if prerequisite is met

**Prerequisites for Conditional:**
- Burst LLM access: User must give explicit consent
- Config-only transitions: Requires configuration file change + restart

### Code Representation

The matrix is represented in code as:

```csharp
public static class ModeMatrix
{
    public static Permission GetPermission(
        OperatingMode mode, 
        Capability capability)
    {
        return (mode, capability) switch
        {
            (LocalOnly, Capability.LocalhostNetwork) => Permission.Allowed,
            (LocalOnly, Capability.ExternalLlmApi) => Permission.Denied,
            (Burst, Capability.ExternalLlmApi) => Permission.ConditionalOnConsent,
            (Airgapped, Capability.AnyNetwork) => Permission.Denied,
            // ... all cells defined
        };
    }
}
```

### Querying the Matrix

```bash
# Show full matrix
acode config matrix

# Check specific capability
acode config matrix --capability external-llm

# Show matrix for specific mode
acode config matrix --mode airgapped

# Export matrix as JSON
acode config matrix --format json > matrix.json
```

### Troubleshooting

**Q: Why is my action denied?**
A: Check the matrix for your current mode. Run `acode config mode` to see current mode.

**Q: How do I find what's allowed?**
A: Run `acode config matrix --mode <mode>` for complete list.

**Q: Why is Burst not in my config options?**
A: Burst mode is session-only and cannot be persisted in configuration.

**Q: Why can't I override Airgapped mode?**
A: Airgapped is a security feature that cannot be bypassed at runtime.

---

## Acceptance Criteria / Definition of Done

### Matrix Completeness (30 items)

- [ ] Matrix covers LocalOnly mode
- [ ] Matrix covers Burst mode
- [ ] Matrix covers Airgapped mode
- [ ] Matrix covers network access (5+ rows)
- [ ] Matrix covers LLM providers (5+ rows)
- [ ] Matrix covers file system (5+ rows)
- [ ] Matrix covers tool execution (5+ rows)
- [ ] Matrix covers data transmission (5+ rows)
- [ ] Matrix covers mode persistence (5+ rows)
- [ ] Matrix covers mode transitions (all combinations)
- [ ] Every cell has a value (no blanks)
- [ ] Every Conditional has prerequisites listed
- [ ] Matrix includes legend
- [ ] Matrix includes examples
- [ ] Matrix includes rationales
- [ ] Matrix is version controlled
- [ ] Matrix has change history
- [ ] Matrix reviewed by security
- [ ] Matrix reviewed by product
- [ ] Matrix approved by stakeholders
- [ ] Matrix in code as data structure
- [ ] Matrix queryable at runtime
- [ ] Matrix serializable to JSON
- [ ] Matrix printable as table
- [ ] Matrix searchable
- [ ] Matrix documented in user docs
- [ ] Matrix documented in developer docs
- [ ] Matrix used by validation code
- [ ] Matrix covered by tests
- [ ] Matrix drift detection in place

### LocalOnly Specifications (25 items)

- [ ] Localhost network allowed
- [ ] Ollama access allowed
- [ ] Local file read allowed
- [ ] Local file write allowed
- [ ] Tool execution allowed
- [ ] Package downloads allowed
- [ ] Git operations allowed
- [ ] External LLM API denied
- [ ] Code transmission denied
- [ ] Prompt transmission denied
- [ ] Telemetry denied
- [ ] Is default mode
- [ ] Transition to Burst allowed (with consent)
- [ ] Transition to Airgapped allowed (config)
- [ ] Persists across sessions
- [ ] Configurable in file
- [ ] CLI override allowed
- [ ] Env var override allowed
- [ ] Logs mode on startup
- [ ] Displays in status
- [ ] All behaviors tested
- [ ] All behaviors documented
- [ ] No gaps in specification
- [ ] No contradictions
- [ ] Implemented correctly

### Burst Specifications (25 items)

- [ ] Requires explicit consent
- [ ] External LLM API allowed (with consent)
- [ ] Localhost network allowed
- [ ] Local file read allowed
- [ ] Local file write allowed
- [ ] Tool execution allowed
- [ ] Prompt transmission allowed (with consent)
- [ ] Secrets redacted before transmission
- [ ] All external calls logged
- [ ] Session-scoped only
- [ ] Does not persist across sessions
- [ ] Cannot be set in config
- [ ] Must specify target provider
- [ ] Transition to LocalOnly allowed
- [ ] Transition to Airgapped denied
- [ ] Consent specifies data shared
- [ ] Consent is revocable
- [ ] API calls tracked for audit
- [ ] Bulk code upload denied
- [ ] Prompts minimized
- [ ] All behaviors tested
- [ ] All behaviors documented
- [ ] No gaps in specification
- [ ] No contradictions
- [ ] Implemented correctly

### Airgapped Specifications (25 items)

- [ ] All network access denied
- [ ] Localhost connections denied
- [ ] DNS lookups denied
- [ ] Pre-loaded models work
- [ ] Local file read allowed
- [ ] Local file write allowed
- [ ] Sandboxed tool execution allowed
- [ ] Tool network access denied
- [ ] Config-only setting
- [ ] CLI override denied
- [ ] Env var override denied
- [ ] Persists permanently
- [ ] No mode transitions allowed
- [ ] Transition to LocalOnly denied
- [ ] Transition to Burst denied
- [ ] Blocked attempts logged
- [ ] Clear setup documentation
- [ ] Model availability validated
- [ ] Graceful failure if model missing
- [ ] Enterprise deployment supported
- [ ] Fully auditable
- [ ] No escape hatches
- [ ] All behaviors tested
- [ ] All behaviors documented
- [ ] Implemented correctly

### Matrix Integration (20 items)

- [ ] Matrix used by ModeValidator
- [ ] Matrix used by ProviderSelector
- [ ] Matrix used by NetworkGuard
- [ ] Matrix used by CLI
- [ ] Matrix used by documentation generator
- [ ] Matrix lookup is O(1)
- [ ] Matrix loaded in under 10ms
- [ ] Matrix cached appropriately
- [ ] Matrix no file I/O per check
- [ ] Matrix size under 10KB
- [ ] Matrix tests comprehensive
- [ ] Matrix tests cover all cells
- [ ] Matrix integration tests pass
- [ ] Matrix E2E tests pass
- [ ] Matrix performance acceptable
- [ ] Matrix consistency verified
- [ ] Matrix no contradictions
- [ ] Matrix matches documentation
- [ ] Matrix matches implementation
- [ ] Matrix change process defined

---

## Testing Requirements

### Unit Tests

| ID | Test | Expected |
|----|------|----------|
| UT-001a-01 | Matrix has all 3 modes | 3 modes present |
| UT-001a-02 | Matrix has all capabilities | All caps present |
| UT-001a-03 | Matrix has no null cells | No nulls |
| UT-001a-04 | LocalOnly localhost = Allowed | Returns Allowed |
| UT-001a-05 | LocalOnly external LLM = Denied | Returns Denied |
| UT-001a-06 | Burst external LLM = Conditional | Returns Conditional |
| UT-001a-07 | Airgapped any network = Denied | Returns Denied |
| UT-001a-08 | Matrix lookup is O(1) | Constant time |
| UT-001a-09 | Matrix serializes to JSON | Valid JSON |
| UT-001a-10 | Matrix deserializes from JSON | Matches original |
| UT-001a-11 | Matrix prints as table | Formatted table |
| UT-001a-12 | Matrix is immutable | Modifications throw |
| UT-001a-13 | All transitions defined | No missing combos |
| UT-001a-14 | LocalOnly → Burst = Conditional | Returns Conditional |
| UT-001a-15 | Airgapped → any = Denied | Returns Denied |

### Integration Tests

| ID | Test | Expected |
|----|------|----------|
| IT-001a-01 | Matrix matches ModeValidator | Consistent behavior |
| IT-001a-02 | Matrix matches documentation | No drift |
| IT-001a-03 | Matrix loaded from assembly | Fast load |
| IT-001a-04 | Matrix query via CLI | Correct output |
| IT-001a-05 | Matrix export to JSON | Valid file |
| IT-001a-06 | All modes represented | 3 modes in output |
| IT-001a-07 | All capabilities listed | Complete list |
| IT-001a-08 | Conditional prerequisites shown | Clear display |
| IT-001a-09 | Legend displayed | User understands |
| IT-001a-10 | Matrix used in validation | Blocks correctly |

### End-to-End Tests

| ID | Test | Expected |
|----|------|----------|
| E2E-001a-01 | Run in LocalOnly, check matrix | Behavior matches |
| E2E-001a-02 | Run in Burst, check matrix | Behavior matches |
| E2E-001a-03 | Run in Airgapped, check matrix | Behavior matches |
| E2E-001a-04 | User queries matrix via CLI | Clear output |
| E2E-001a-05 | Denied action matches matrix | Correct denial |
| E2E-001a-06 | Allowed action matches matrix | Correct allow |
| E2E-001a-07 | Conditional action matches matrix | Correct behavior |
| E2E-001a-08 | Transition follows matrix | Correct result |

### Performance Benchmarks

| ID | Metric | Target |
|----|--------|--------|
| PB-001a-01 | Matrix lookup time | < 1μs |
| PB-001a-02 | Matrix load time | < 10ms |
| PB-001a-03 | Matrix size in memory | < 10KB |
| PB-001a-04 | Matrix serialization time | < 50ms |
| PB-001a-05 | Matrix iteration (all cells) | < 1ms |

---

## User Verification Steps

### Verification 1: View Full Matrix
1. Run `acode config matrix`
2. **Verify:** Complete matrix displayed with all modes and capabilities

### Verification 2: Query Specific Capability
1. Run `acode config matrix --capability external-llm`
2. **Verify:** Shows permission for each mode

### Verification 3: Query Specific Mode
1. Run `acode config matrix --mode airgapped`
2. **Verify:** Shows all capabilities for Airgapped mode

### Verification 4: Export Matrix
1. Run `acode config matrix --format json > matrix.json`
2. Open matrix.json
3. **Verify:** Valid JSON with complete matrix

### Verification 5: Matrix Matches Behavior
1. Note LocalOnly denies external LLM in matrix
2. Try external LLM call in LocalOnly mode
3. **Verify:** Action denied as matrix specifies

### Verification 6: Conditional Prerequisites
1. Run `acode config matrix` and find conditional cells
2. **Verify:** Prerequisites clearly listed

### Verification 7: Transition Matrix
1. Run `acode config matrix --transitions`
2. **Verify:** All mode transition rules displayed

### Verification 8: Matrix Legend
1. View matrix output
2. **Verify:** Legend explains ✅, ❌, ⚠️ symbols

### Verification 9: Matrix in Documentation
1. Open user documentation
2. Find mode matrix section
3. **Verify:** Matches runtime matrix exactly

### Verification 10: Matrix Completeness
1. Count all cells in matrix
2. **Verify:** No blank or undefined cells

---

## Implementation Prompt for Claude

### Files to Create

```
src/Acode.Domain/
├── Modes/
│   ├── ModeMatrix.cs           # Matrix data structure
│   ├── Capability.cs           # Capability enum
│   ├── Permission.cs           # Permission enum
│   └── MatrixExporter.cs       # Export functionality
│
src/Acode.CLI/
├── Commands/
│   └── ConfigMatrixCommand.cs  # CLI for matrix display
│
docs/
└── mode-matrix.md              # User documentation
```

### Core Types

```csharp
namespace Acode.Domain.Modes;

/// <summary>
/// All capabilities that can be checked against operating mode.
/// </summary>
public enum Capability
{
    // Network
    LocalhostNetwork,
    LocalAreaNetwork,
    ExternalNetwork,
    DnsLookup,
    
    // LLM Providers
    OllamaLocal,
    OpenAiApi,
    AnthropicApi,
    AzureOpenAiApi,
    CustomLlmApi,
    
    // File System
    ReadProjectFiles,
    WriteProjectFiles,
    ReadSystemFiles,
    WriteSystemFiles,
    ReadHomeDirectory,
    WriteAcodeDirectory,
    
    // Tool Execution
    DotnetCli,
    GitOperations,
    NpmYarn,
    CustomTools,
    ShellCommands,
    
    // Data Transmission
    SendPrompts,
    SendCodeSnippets,
    SendFullFiles,
    SendRepositoryData,
    SendTelemetry,
    SendCrashReports
}

/// <summary>
/// Permission levels for mode-capability combinations.
/// </summary>
public enum Permission
{
    Allowed,
    Denied,
    ConditionalOnConsent,
    ConditionalOnConfig,
    LimitedScope
}

/// <summary>
/// Mode matrix entry with optional prerequisite.
/// </summary>
public sealed record MatrixEntry
{
    public required OperatingMode Mode { get; init; }
    public required Capability Capability { get; init; }
    public required Permission Permission { get; init; }
    public string? Prerequisite { get; init; }
    public string? Rationale { get; init; }
}

/// <summary>
/// The authoritative mode capability matrix.
/// </summary>
public static class ModeMatrix
{
    private static readonly FrozenDictionary<(OperatingMode, Capability), MatrixEntry> _matrix;
    
    static ModeMatrix()
    {
        var entries = BuildMatrix();
        _matrix = entries.ToFrozenDictionary(e => (e.Mode, e.Capability));
    }
    
    public static Permission GetPermission(OperatingMode mode, Capability capability)
    {
        return _matrix.TryGetValue((mode, capability), out var entry)
            ? entry.Permission
            : Permission.Denied; // Fail-safe
    }
    
    public static MatrixEntry? GetEntry(OperatingMode mode, Capability capability)
    {
        return _matrix.GetValueOrDefault((mode, capability));
    }
    
    public static IEnumerable<MatrixEntry> GetAllEntries() => _matrix.Values;
    
    public static IEnumerable<MatrixEntry> GetEntriesForMode(OperatingMode mode)
        => _matrix.Values.Where(e => e.Mode == mode);
    
    public static IEnumerable<MatrixEntry> GetEntriesForCapability(Capability capability)
        => _matrix.Values.Where(e => e.Capability == capability);
    
    private static IEnumerable<MatrixEntry> BuildMatrix()
    {
        // LocalOnly mode entries
        yield return new MatrixEntry
        {
            Mode = OperatingMode.LocalOnly,
            Capability = Capability.LocalhostNetwork,
            Permission = Permission.Allowed,
            Rationale = "Required for Ollama communication"
        };
        
        yield return new MatrixEntry
        {
            Mode = OperatingMode.LocalOnly,
            Capability = Capability.ExternalNetwork,
            Permission = Permission.Denied,
            Rationale = "Core privacy constraint"
        };
        
        // ... all entries
    }
}
```

### CLI Command

```csharp
[Command("config matrix", Description = "Display the mode capability matrix")]
public class ConfigMatrixCommand
{
    [Option("--mode", Description = "Filter by mode")]
    public OperatingMode? Mode { get; set; }
    
    [Option("--capability", Description = "Filter by capability")]
    public string? Capability { get; set; }
    
    [Option("--format", Description = "Output format (table, json)")]
    public string Format { get; set; } = "table";
    
    [Option("--transitions", Description = "Show transition matrix")]
    public bool Transitions { get; set; }
}
```

### Validation Checklist Before Merge

- [ ] All capabilities enumerated
- [ ] All mode combinations defined
- [ ] No null or missing cells
- [ ] Matrix is immutable
- [ ] Matrix is testable
- [ ] Matrix matches documentation
- [ ] Performance targets met
- [ ] Security review passed

---

**END OF TASK 001.a**
