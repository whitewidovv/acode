# Task 001: Define Operating Modes & Hard Constraints

**Priority:** 4 / 49  
**Tier:** Foundation  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** Task 000.c (tooling infrastructure must exist)  

---

## Description

### Overview

Task 001 defines the operating modes and hard constraints that govern all behavior of the Agentic Coding Bot (Acode). These constraints are non-negotiable architectural decisions that shape every feature, every code path, and every configuration option in the system.

Operating modes determine what resources Acode can access at runtime. Hard constraints are invariants that must never be violated regardless of mode. Together, they form the security and privacy posture of the product.

### Business Value

Acode's core value proposition is **privacy-first, local-first AI coding assistance**. This task defines the rules that make that value proposition trustworthy:

1. **Data Sovereignty** — User code never leaves the local machine without explicit, informed consent
2. **Offline Capability** — Acode works fully without any network connectivity
3. **Enterprise Acceptability** — Security teams can approve Acode because constraints are provable
4. **Regulatory Compliance** — Hard constraints support GDPR, SOC2, and airgapped deployment requirements
5. **User Trust** — Clear modes make privacy guarantees understandable

### Scope Boundaries

**In Scope:**
- Definition of all operating modes (LocalOnly, Burst, Airgapped)
- Definition of all hard constraints
- Mode transition rules
- Constraint enforcement architecture
- Configuration schema for mode selection
- Runtime validation requirements
- Audit logging requirements for mode changes
- Documentation of constraints

**Out of Scope:**
- Implementation of network blocking (Task 007)
- Implementation of model providers (Tasks 004-006)
- CI/CD enforcement (Epic 8)
- Threat modeling (Task 003)
- Configuration file parsing (Task 002)

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 001.a | Subtask | Defines mode matrix in detail |
| Task 001.b | Subtask | Defines LLM API validation rules |
| Task 001.c | Subtask | Writes constraints document |
| Task 002 | Consumer | Config file must support mode selection |
| Task 003 | Consumer | Threat model references constraints |
| Task 004-006 | Consumer | Providers must respect mode rules |
| Task 007 | Consumer | Network blocking implements constraints |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Mode not enforced | Privacy breach | Defense-in-depth validation |
| Constraint bypassed | Trust violation | Compile-time + runtime checks |
| Mode ambiguous | User confusion | Clear mode indicator in CLI |
| Transition blocked | Workflow disruption | Clear error messages |
| Config invalid | Startup failure | Validation with helpful errors |

### Assumptions

1. Users understand the difference between local and cloud AI
2. Network connectivity can be detected reliably
3. Mode transitions during a session are valid use cases
4. Some enterprises require Airgapped mode permanently
5. Burst mode consent is per-session, not persistent

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Operating Mode** | Runtime configuration determining resource access |
| **LocalOnly Mode** | Default mode; all inference local, no network for LLM |
| **Burst Mode** | Temporary mode allowing external LLM with consent |
| **Airgapped Mode** | Permanent mode; no network access whatsoever |
| **Hard Constraint** | Invariant that must never be violated |
| **Soft Constraint** | Preference that can be overridden |
| **Mode Transition** | Change from one operating mode to another |
| **Consent** | Explicit user acknowledgment before Burst mode |
| **Inference** | LLM processing of prompts to generate responses |
| **Local Inference** | Inference using locally-running model (Ollama) |
| **Remote Inference** | Inference using cloud API (forbidden by default) |
| **Privacy Boundary** | Logical boundary data must not cross |
| **Data Exfiltration** | Unauthorized transmission of data externally |
| **Constraint Violation** | Attempted action that breaks a hard constraint |
| **Audit Log** | Immutable record of mode changes and violations |
| **Defense in Depth** | Multiple layers of constraint enforcement |
| **Fail-Safe** | Default to most restrictive mode on errors |
| **Opt-In** | User must explicitly enable less restrictive modes |
| **Session** | Single invocation of Acode CLI |
| **Persistent Mode** | Mode that survives across sessions (Airgapped) |

---

## Out of Scope

- Implementation of network interception or blocking
- Implementation of specific model providers
- User interface for consent (covered in CLI tasks)
- Threat modeling and risk analysis (Task 003)
- Configuration file schema and parsing (Task 002)
- CI/CD enforcement of constraints (Epic 8)
- Telemetry or analytics constraints
- Third-party plugin constraints
- License enforcement
- Feature flags unrelated to modes

---

## Functional Requirements

### Operating Mode Definitions (FR-001-01 to FR-001-20)

| ID | Requirement |
|----|-------------|
| FR-001-01 | System MUST support exactly three operating modes |
| FR-001-02 | Mode names MUST be: LocalOnly, Burst, Airgapped |
| FR-001-03 | LocalOnly MUST be the default mode |
| FR-001-04 | LocalOnly MUST prohibit external LLM API calls |
| FR-001-05 | LocalOnly MUST allow local network for Ollama |
| FR-001-06 | LocalOnly MUST allow package downloads (non-LLM) |
| FR-001-07 | Burst MUST require explicit user consent |
| FR-001-08 | Burst MUST allow external LLM API calls |
| FR-001-09 | Burst MUST be temporary (session-scoped) |
| FR-001-10 | Burst consent MUST specify which provider |
| FR-001-11 | Burst MUST log all external API calls |
| FR-001-12 | Airgapped MUST prohibit ALL network access |
| FR-001-13 | Airgapped MUST be permanently set via config |
| FR-001-14 | Airgapped MUST NOT allow transition to other modes |
| FR-001-15 | Airgapped MUST work with pre-downloaded models |
| FR-001-16 | Mode MUST be determinable at startup |
| FR-001-17 | Mode MUST be queryable at runtime |
| FR-001-18 | Mode MUST be displayed to user on request |
| FR-001-19 | Mode changes MUST be logged with timestamp |
| FR-001-20 | Mode state MUST be immutable within enforcement |

### Hard Constraints (FR-001-21 to FR-001-40)

| ID | Requirement |
|----|-------------|
| FR-001-21 | User source code MUST NOT leave machine in LocalOnly |
| FR-001-22 | User source code MUST NOT leave machine in Airgapped |
| FR-001-23 | In Burst mode, only prompt context sent (not full repo) |
| FR-001-24 | Prompts in Burst MUST be logged locally |
| FR-001-25 | No external LLM call without mode=Burst |
| FR-001-26 | No network call in Airgapped mode |
| FR-001-27 | Secrets MUST be redacted before any external call |
| FR-001-28 | Consent MUST be given before first Burst API call |
| FR-001-29 | Constraint violations MUST be logged as errors |
| FR-001-30 | Constraint violations MUST abort the operation |
| FR-001-31 | Default on unknown mode MUST be LocalOnly |
| FR-001-32 | Default on config error MUST be LocalOnly |
| FR-001-33 | Default on network uncertainty MUST be LocalOnly |
| FR-001-34 | All constraints MUST be enforced at runtime |
| FR-001-35 | Constraints MUST be documented in code |
| FR-001-36 | Constraint checks MUST be centralized |
| FR-001-37 | Constraint interface MUST be mockable for tests |
| FR-001-38 | Constraints MUST apply regardless of entry point |
| FR-001-39 | Constraints MUST apply to all model providers |
| FR-001-40 | Constraint enforcement MUST be synchronous |

### Mode Transitions (FR-001-41 to FR-001-55)

| ID | Requirement |
|----|-------------|
| FR-001-41 | LocalOnly → Burst MUST require consent |
| FR-001-42 | LocalOnly → Airgapped MUST require config change |
| FR-001-43 | Burst → LocalOnly MUST be allowed without consent |
| FR-001-44 | Burst → Airgapped MUST NOT be allowed at runtime |
| FR-001-45 | Airgapped → LocalOnly MUST NOT be allowed |
| FR-001-46 | Airgapped → Burst MUST NOT be allowed |
| FR-001-47 | Mode transition MUST log before and after states |
| FR-001-48 | Mode transition MUST validate prerequisites |
| FR-001-49 | Failed transition MUST remain in current mode |
| FR-001-50 | Transition API MUST return success/failure |
| FR-001-51 | Transition events MUST be observable |
| FR-001-52 | Transition duration MUST be under 100ms |
| FR-001-53 | Parallel transition requests MUST be serialized |
| FR-001-54 | Transition state MUST be atomic |
| FR-001-55 | Transition MUST NOT interrupt active operations |

### Configuration (FR-001-56 to FR-001-70)

| ID | Requirement |
|----|-------------|
| FR-001-56 | Mode MUST be configurable via .agent/config.yml |
| FR-001-57 | Mode MUST be overridable via CLI flag |
| FR-001-58 | Mode MUST be overridable via environment variable |
| FR-001-59 | Precedence: CLI > env > config > default |
| FR-001-60 | Invalid mode value MUST fail with clear error |
| FR-001-61 | Airgapped in config MUST prevent CLI override |
| FR-001-62 | Burst via config MUST NOT be allowed (session only) |
| FR-001-63 | Mode config MUST be validated at startup |
| FR-001-64 | Config errors MUST list valid options |
| FR-001-65 | Mode setting MUST be case-insensitive |
| FR-001-66 | Mode aliases MUST NOT exist (prevent confusion) |
| FR-001-67 | Deprecated mode names MUST produce warnings |
| FR-001-68 | Config schema MUST document mode field |
| FR-001-69 | Config MUST support mode per-repository |
| FR-001-70 | Global mode MUST be settable in user config |

### Validation (FR-001-71 to FR-001-85)

| ID | Requirement |
|----|-------------|
| FR-001-71 | Mode validator MUST run before any LLM call |
| FR-001-72 | Mode validator MUST check current mode |
| FR-001-73 | Mode validator MUST check target action |
| FR-001-74 | Mode validator MUST return allow/deny |
| FR-001-75 | Denied actions MUST include reason |
| FR-001-76 | Denied actions MUST include remediation |
| FR-001-77 | Validator MUST be called from service layer |
| FR-001-78 | Validator MUST NOT be bypassable |
| FR-001-79 | Validator interface MUST be mockable |
| FR-001-80 | Validator MUST log all denials |
| FR-001-81 | Validator MUST be testable in isolation |
| FR-001-82 | Validator MUST have negligible performance |
| FR-001-83 | Validator MUST not throw exceptions (return result) |
| FR-001-84 | Validator MUST handle null/empty mode gracefully |
| FR-001-85 | Validator MUST be stateless |

---

## Non-Functional Requirements

### Security (NFR-001-01 to NFR-001-15)

| ID | Requirement |
|----|-------------|
| NFR-001-01 | Constraint code MUST NOT be modifiable at runtime |
| NFR-001-02 | Mode state MUST NOT be modifiable via reflection |
| NFR-001-03 | Audit logs MUST be append-only |
| NFR-001-04 | Audit logs MUST include timestamps |
| NFR-001-05 | Audit logs MUST include user identity |
| NFR-001-06 | Constraint checks MUST NOT be removable by plugins |
| NFR-001-07 | Defense in depth: multiple validation points |
| NFR-001-08 | Fail-safe: default to most restrictive on error |
| NFR-001-09 | No constraint check in user-controllable code |
| NFR-001-10 | Mode setting MUST be validated server-side (CLI) |
| NFR-001-11 | Secrets detection MUST occur before transmission |
| NFR-001-12 | Mode violations MUST be audit-logged |
| NFR-001-13 | Constraint code MUST have no dependencies on plugins |
| NFR-001-14 | Mode validation MUST NOT trust client input |
| NFR-001-15 | Airgapped mode MUST be verifiable by security audit |

### Performance (NFR-001-16 to NFR-001-22)

| ID | Requirement |
|----|-------------|
| NFR-001-16 | Mode check MUST complete in under 1ms |
| NFR-001-17 | Mode transition MUST complete in under 100ms |
| NFR-001-18 | Startup mode determination MUST be under 50ms |
| NFR-001-19 | Constraint validation MUST NOT block async operations |
| NFR-001-20 | Mode state access MUST be lock-free for reads |
| NFR-001-21 | Constraint checks MUST NOT allocate heap memory |
| NFR-001-22 | Audit logging MUST be asynchronous |

### Reliability (NFR-001-23 to NFR-001-32)

| ID | Requirement |
|----|-------------|
| NFR-001-23 | Mode state MUST survive process crashes gracefully |
| NFR-001-24 | Constraint enforcement MUST NOT crash on bad input |
| NFR-001-25 | Mode determination MUST NOT depend on network |
| NFR-001-26 | Airgapped detection MUST be deterministic |
| NFR-001-27 | Audit log writes MUST be durable |
| NFR-001-28 | Mode transition MUST be atomic |
| NFR-001-29 | Concurrent mode checks MUST be thread-safe |
| NFR-001-30 | Constraint code MUST have 100% test coverage |
| NFR-001-31 | Mode state MUST be consistent across threads |
| NFR-001-32 | Recovery from constraint failure MUST be graceful |

### Maintainability (NFR-001-33 to NFR-001-40)

| ID | Requirement |
|----|-------------|
| NFR-001-33 | Constraint additions MUST require code review |
| NFR-001-34 | Mode definitions MUST be in single source file |
| NFR-001-35 | Constraint logic MUST be unit-testable |
| NFR-001-36 | Mode enum MUST be exhaustive in switch statements |
| NFR-001-37 | New modes MUST be addable without breaking changes |
| NFR-001-38 | Constraint documentation MUST be auto-generated |
| NFR-001-39 | Mode comparison MUST use type-safe enums |
| NFR-001-40 | Constraint interfaces MUST follow ISP |

---

## User Manual Documentation

### Operating Mode Overview

Acode operates in one of three modes that control its network and API access:

| Mode | Local LLM | External LLM | Network | Persistence |
|------|-----------|--------------|---------|-------------|
| LocalOnly | ✅ | ❌ | Limited* | Default |
| Burst | ✅ | ✅ (with consent) | Full | Session |
| Airgapped | ✅ (pre-loaded) | ❌ | None | Permanent |

*Limited = Ollama on localhost, package downloads

### LocalOnly Mode (Default)

LocalOnly is the default mode. All AI inference runs on your machine using Ollama.

**Allowed:**
- Running local models via Ollama (localhost only)
- Downloading packages via NuGet, npm, etc.
- Git operations

**Prohibited:**
- Calling external LLM APIs (OpenAI, Anthropic, etc.)
- Sending code or prompts to any external AI service

```bash
# Check current mode
acode config mode

# Explicitly set LocalOnly (usually unnecessary)
acode --mode local-only analyze

# LocalOnly is default; this is equivalent
acode analyze
```

### Burst Mode

Burst mode temporarily allows external LLM API access. You must explicitly consent.

**Use Cases:**
- Complex tasks requiring larger models
- When local hardware is insufficient
- One-time use of premium API features

**Consent Flow:**
```bash
# Attempt to use external API
acode --mode burst analyze

# System prompts:
#   ⚠️  BURST MODE REQUESTED
#   This will send prompts to: OpenAI gpt-4
#   Your code context may be included in prompts.
#
#   Type 'CONFIRM' to proceed, or press Enter to cancel:

CONFIRM

# Proceeds with external API call
```

**Important:**
- Consent is required each session
- Cannot persist Burst mode in config
- All external calls are logged locally

### Airgapped Mode

Airgapped mode disables ALL network access. For high-security environments.

**Setup Requirements:**
1. Pre-download Ollama and models
2. Pre-download all dependencies
3. Set mode in config:

```yaml
# .agent/config.yml
mode: airgapped
model:
  provider: ollama
  name: codellama:13b  # Must be pre-pulled
```

**Characteristics:**
- No network calls whatsoever
- Cannot be changed at runtime
- Cannot be overridden by CLI
- Must have all resources local

```bash
# In airgapped mode, this will fail:
acode --mode burst analyze
# Error: Cannot enter Burst mode. Airgapped mode is permanent.

# This works (if model is pre-loaded):
acode analyze
```

### Configuration Precedence

Mode is determined by (highest to lowest priority):

1. **CLI Flag**: `--mode local-only|burst|airgapped`
2. **Environment Variable**: `ACODE_MODE=local-only|burst|airgapped`
3. **Repository Config**: `.agent/config.yml` mode field
4. **User Config**: `~/.acode/config.yml` mode field
5. **Default**: LocalOnly

**Exception:** Airgapped mode in config cannot be overridden by CLI or env.

### Hard Constraints

These constraints can NEVER be violated:

| Constraint | Description |
|------------|-------------|
| HC-01 | No external LLM API in LocalOnly mode |
| HC-02 | No network in Airgapped mode |
| HC-03 | No source code transmission without Burst consent |
| HC-04 | Secrets must be redacted before any external call |
| HC-05 | Consent required before first Burst API call |
| HC-06 | All mode changes logged |
| HC-07 | Violations logged and operation aborted |

### Checking Mode Status

```bash
# Show current mode and constraints
acode config mode --verbose

# Output:
#   Current Mode: LocalOnly
#   Effective Config: .agent/config.yml
#   
#   Allowed Actions:
#     ✅ Local model inference (Ollama)
#     ✅ Package downloads
#     ✅ Git operations
#   
#   Prohibited Actions:
#     ❌ External LLM API calls
#     ❌ Cloud AI services

# JSON output for scripting
acode config mode --json
```

### Transitioning Modes

```bash
# LocalOnly → Burst (requires consent)
acode --mode burst analyze

# Burst → LocalOnly (immediate, no consent)
acode --mode local-only analyze

# Any → Airgapped (requires config change + restart)
# Edit .agent/config.yml, then:
acode analyze  # Now in Airgapped mode
```

### Logging and Audit

All mode transitions and constraint checks are logged:

```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "event": "mode_transition",
  "from_mode": "LocalOnly",
  "to_mode": "Burst",
  "consent_given": true,
  "user": "developer@example.com",
  "session_id": "abc123"
}
```

Log location: `~/.acode/logs/audit.jsonl`

### Troubleshooting

**Q: Why can't I use external APIs?**
A: You're in LocalOnly mode (default). Use `--mode burst` with consent.

**Q: Why does `--mode burst` fail?**
A: Check if Airgapped mode is set in config. Airgapped cannot be overridden.

**Q: How do I permanently allow external APIs?**
A: You cannot. Burst mode is session-only by design for safety.

**Q: Model not found in Airgapped mode?**
A: Pre-pull models while online: `ollama pull codellama:13b`

**Q: Why is my config mode ignored?**
A: CLI flag or environment variable may be overriding it.

---

## Acceptance Criteria / Definition of Done

### Mode Definitions (25 items)

- [ ] LocalOnly mode defined as enum value
- [ ] Burst mode defined as enum value
- [ ] Airgapped mode defined as enum value
- [ ] No other modes exist
- [ ] LocalOnly is default
- [ ] LocalOnly prohibits external LLM
- [ ] LocalOnly allows Ollama localhost
- [ ] Burst requires consent
- [ ] Burst allows external LLM
- [ ] Burst is session-scoped
- [ ] Airgapped prohibits all network
- [ ] Airgapped is config-only
- [ ] Airgapped cannot transition
- [ ] Mode names are consistent throughout
- [ ] Mode enum is exhaustive
- [ ] Mode is queryable at runtime
- [ ] Mode is displayed to user
- [ ] Mode is logged on startup
- [ ] Mode is logged on transition
- [ ] Mode is validated at startup
- [ ] Invalid mode fails with error
- [ ] Mode comparison is type-safe
- [ ] Mode enum documented
- [ ] Mode tests comprehensive
- [ ] Mode behavior documented

### Hard Constraints (25 items)

- [ ] Constraint interface defined
- [ ] All constraints enumerated
- [ ] Constraints are immutable
- [ ] Constraints apply universally
- [ ] Constraints checked before actions
- [ ] Constraint violations logged
- [ ] Constraint violations abort operations
- [ ] Constraints documented in code
- [ ] Constraints documented for users
- [ ] No external LLM in LocalOnly enforced
- [ ] No network in Airgapped enforced
- [ ] Consent required for Burst enforced
- [ ] Secrets redaction enforced
- [ ] Fail-safe to LocalOnly
- [ ] Defense in depth (multiple checks)
- [ ] Constraint checks centralized
- [ ] Constraint checks mockable
- [ ] Constraint code has 100% coverage
- [ ] Constraint code reviewed
- [ ] No constraint bypasses possible
- [ ] Constraints in Domain layer
- [ ] Constraints thread-safe
- [ ] Constraints performant (< 1ms)
- [ ] Constraints logged
- [ ] Constraints auditable

### Mode Transitions (20 items)

- [ ] LocalOnly → Burst with consent
- [ ] LocalOnly → Burst denied without consent
- [ ] Burst → LocalOnly allowed
- [ ] Burst → Airgapped blocked
- [ ] Airgapped → LocalOnly blocked
- [ ] Airgapped → Burst blocked
- [ ] Transitions logged
- [ ] Transitions atomic
- [ ] Transitions return result
- [ ] Failed transitions stay in mode
- [ ] Transitions observable (events)
- [ ] Transitions serialized
- [ ] Transitions under 100ms
- [ ] Concurrent transitions handled
- [ ] Transition state consistent
- [ ] Transition API documented
- [ ] Transition tests comprehensive
- [ ] Transition errors have messages
- [ ] Transition errors have remediation
- [ ] Transition audit logged

### Configuration (20 items)

- [ ] Mode in .agent/config.yml
- [ ] Mode via CLI flag
- [ ] Mode via environment variable
- [ ] Precedence: CLI > env > config > default
- [ ] Invalid mode rejected
- [ ] Airgapped not overridable
- [ ] Burst not in config
- [ ] Mode validated at startup
- [ ] Mode case-insensitive
- [ ] No mode aliases
- [ ] Deprecated modes warn
- [ ] Config schema documents mode
- [ ] Per-repository mode supported
- [ ] Global mode supported
- [ ] Config errors list valid options
- [ ] Config mode documented
- [ ] Environment variable documented
- [ ] CLI flag documented
- [ ] Precedence documented
- [ ] Config tests comprehensive

### Validation (20 items)

- [ ] Validator interface defined
- [ ] Validator runs before LLM calls
- [ ] Validator checks mode
- [ ] Validator checks action
- [ ] Validator returns result
- [ ] Denied actions include reason
- [ ] Denied actions include remediation
- [ ] Validator in Application layer
- [ ] Validator not bypassable
- [ ] Validator mockable
- [ ] Validator logs denials
- [ ] Validator testable in isolation
- [ ] Validator under 1ms
- [ ] Validator no exceptions
- [ ] Validator handles null
- [ ] Validator stateless
- [ ] Validator thread-safe
- [ ] Validator documented
- [ ] Validator tests comprehensive
- [ ] Validator integrated with providers

### Logging/Audit (20 items)

- [ ] Mode changes logged
- [ ] Constraint checks logged
- [ ] Violations logged as errors
- [ ] Timestamps included
- [ ] User identity included
- [ ] Session ID included
- [ ] Log format documented
- [ ] Logs are JSONL format
- [ ] Logs are append-only
- [ ] Logs location configurable
- [ ] Logs rotated appropriately
- [ ] Secrets redacted from logs
- [ ] Logs parseable
- [ ] Logs include mode before/after
- [ ] Logs include action attempted
- [ ] Logs include result
- [ ] Log writing async
- [ ] Log writing durable
- [ ] Log schema documented
- [ ] Log integration tested

### Documentation (20 items)

- [ ] Mode overview documented
- [ ] Each mode described
- [ ] Constraints listed
- [ ] Transitions explained
- [ ] Configuration documented
- [ ] CLI examples provided
- [ ] Troubleshooting section
- [ ] FAQ section
- [ ] Quick start guide
- [ ] Enterprise deployment guide
- [ ] Airgapped setup guide
- [ ] Audit log format documented
- [ ] API documentation
- [ ] Code documentation (XML docs)
- [ ] Architecture decision record
- [ ] Threat considerations noted
- [ ] Compliance mapping
- [ ] User manual complete
- [ ] Developer guide complete
- [ ] Examples repository updated

### Testing (20 items)

- [ ] Unit tests for mode enum
- [ ] Unit tests for constraints
- [ ] Unit tests for transitions
- [ ] Unit tests for validation
- [ ] Integration tests for mode flow
- [ ] Integration tests for config
- [ ] E2E tests for LocalOnly
- [ ] E2E tests for Burst
- [ ] E2E tests for Airgapped
- [ ] Performance tests for checks
- [ ] Security tests for bypasses
- [ ] Negative tests for violations
- [ ] Edge case tests
- [ ] Concurrent access tests
- [ ] Config precedence tests
- [ ] Error handling tests
- [ ] Logging tests
- [ ] Mock provider tests
- [ ] 100% constraint coverage
- [ ] All tests pass

---

## Testing Requirements

### Unit Tests

| ID | Test | Expected |
|----|------|----------|
| UT-001-01 | Mode enum has exactly 3 values | LocalOnly, Burst, Airgapped |
| UT-001-02 | Default mode is LocalOnly | Mode.Default == LocalOnly |
| UT-001-03 | Mode.LocalOnly allows local inference | Returns true |
| UT-001-04 | Mode.LocalOnly denies external API | Returns false |
| UT-001-05 | Mode.Burst allows external API | Returns true (with consent) |
| UT-001-06 | Mode.Airgapped denies all network | Returns false |
| UT-001-07 | Constraint check for LocalOnly external call | Denied |
| UT-001-08 | Constraint check for Burst with consent | Allowed |
| UT-001-09 | Constraint check for Burst without consent | Denied |
| UT-001-10 | Constraint check for Airgapped any network | Denied |
| UT-001-11 | Transition LocalOnly to Burst with consent | Success |
| UT-001-12 | Transition LocalOnly to Burst without consent | Failure |
| UT-001-13 | Transition Burst to LocalOnly | Success |
| UT-001-14 | Transition Airgapped to any | Failure |
| UT-001-15 | Mode from config "local-only" | LocalOnly |
| UT-001-16 | Mode from config "LOCAL-ONLY" | LocalOnly |
| UT-001-17 | Mode from config "invalid" | Error |
| UT-001-18 | Validator with null mode | LocalOnly assumed |
| UT-001-19 | Validator with empty action | Error |
| UT-001-20 | Audit log entry created on transition | Entry exists |

### Integration Tests

| ID | Test | Expected |
|----|------|----------|
| IT-001-01 | Startup with no config | Mode is LocalOnly |
| IT-001-02 | Startup with config local-only | Mode is LocalOnly |
| IT-001-03 | Startup with config airgapped | Mode is Airgapped |
| IT-001-04 | CLI --mode burst overrides config | Mode is Burst (with consent) |
| IT-001-05 | CLI --mode burst in Airgapped config | Error, stays Airgapped |
| IT-001-06 | Environment ACODE_MODE=burst | Mode is Burst (with consent) |
| IT-001-07 | Ollama call in LocalOnly | Success |
| IT-001-08 | External API call in LocalOnly | Blocked |
| IT-001-09 | External API call in Burst | Success (with consent) |
| IT-001-10 | Any network call in Airgapped | Blocked |
| IT-001-11 | Mode transition logged | Audit entry created |
| IT-001-12 | Constraint violation logged | Error entry created |
| IT-001-13 | Config precedence respected | CLI > env > config |
| IT-001-14 | Invalid mode in config | Startup error |
| IT-001-15 | Mode persists in session | Same mode throughout |

### End-to-End Tests

| ID | Test | Expected |
|----|------|----------|
| E2E-001-01 | Full LocalOnly workflow | Completes with local model |
| E2E-001-02 | Full Burst workflow with consent | Completes with external API |
| E2E-001-03 | Burst consent declined | Falls back to LocalOnly |
| E2E-001-04 | Full Airgapped workflow | Completes with pre-loaded model |
| E2E-001-05 | Airgapped with missing model | Clear error message |
| E2E-001-06 | Mode shown in status | Correct mode displayed |
| E2E-001-07 | Audit log complete after session | All events logged |
| E2E-001-08 | Multiple transitions in session | All handled correctly |
| E2E-001-09 | Concurrent mode checks | Thread-safe, consistent |
| E2E-001-10 | Recovery after constraint violation | Continues in safe mode |

### Performance Benchmarks

| ID | Metric | Target |
|----|--------|--------|
| PB-001-01 | Mode check latency | < 1ms |
| PB-001-02 | Mode transition latency | < 100ms |
| PB-001-03 | Startup mode determination | < 50ms |
| PB-001-04 | Constraint validation latency | < 1ms |
| PB-001-05 | Audit log write latency | < 10ms |
| PB-001-06 | Memory for mode state | < 1KB |
| PB-001-07 | Concurrent checks throughput | > 10,000/sec |

### Regression Tests

| Area | Test |
|------|------|
| Config parsing | Mode field parsed correctly |
| CLI parsing | --mode flag handled |
| Provider integration | All providers respect mode |
| Logging | All mode events captured |
| Error handling | Graceful degradation |

---

## User Verification Steps

### Verification 1: Default Mode is LocalOnly
1. Start Acode with no configuration
2. Run `acode config mode`
3. **Verify:** Output shows "LocalOnly"

### Verification 2: LocalOnly Blocks External API
1. Start in LocalOnly mode
2. Attempt action requiring external API
3. **Verify:** Error message, operation blocked

### Verification 3: Burst Requires Consent
1. Run `acode --mode burst analyze`
2. Observe consent prompt
3. Decline consent
4. **Verify:** Falls back to LocalOnly

### Verification 4: Burst Consent Flow
1. Run `acode --mode burst analyze`
2. Type "CONFIRM" when prompted
3. **Verify:** External API call succeeds

### Verification 5: Airgapped Blocks All Network
1. Set `mode: airgapped` in config
2. Attempt any network operation
3. **Verify:** Operation blocked

### Verification 6: Airgapped Cannot Be Overridden
1. Set `mode: airgapped` in config
2. Run `acode --mode burst analyze`
3. **Verify:** Error, stays in Airgapped

### Verification 7: Mode Shown in Status
1. Run `acode config mode --verbose`
2. **Verify:** Current mode and constraints displayed

### Verification 8: Mode Transition Logged
1. Transition from LocalOnly to Burst (with consent)
2. Check `~/.acode/logs/audit.jsonl`
3. **Verify:** Transition event logged

### Verification 9: Constraint Violation Logged
1. Attempt blocked action
2. Check logs
3. **Verify:** Violation logged as error

### Verification 10: CLI Override Works
1. Set `mode: local-only` in config
2. Run `acode --mode burst analyze` with consent
3. **Verify:** Burst mode active for session

### Verification 11: Environment Override Works
1. Set `ACODE_MODE=burst`
2. Run `acode analyze` with consent
3. **Verify:** Burst mode active

### Verification 12: Precedence Correct
1. Set config mode, env var, and CLI flag all different
2. **Verify:** CLI flag wins (except Airgapped)

---

## Implementation Prompt for Claude

### Files to Create/Modify

```
src/Acode.Domain/
├── Modes/
│   ├── OperatingMode.cs         # Enum definition
│   ├── ModeConstraints.cs       # Hard constraint definitions
│   ├── IModeValidator.cs        # Validation interface
│   └── ModeTransitionRules.cs   # Transition logic
│
src/Acode.Application/
├── Modes/
│   ├── ModeService.cs           # Mode management service
│   ├── ModeValidationService.cs # Validation implementation
│   ├── ConsentService.cs        # Burst consent handling
│   └── ModeAuditLogger.cs       # Audit logging
│
src/Acode.Infrastructure/
├── Configuration/
│   └── ModeConfiguration.cs     # Config binding
│
src/Acode.CLI/
├── Commands/
│   └── ConfigModeCommand.cs     # CLI for mode status
│
docs/
├── operating-modes.md           # User documentation
└── constraints.md               # Constraint reference
```

### Core Types

```csharp
namespace Acode.Domain.Modes;

/// <summary>
/// Operating modes controlling Acode's network and API access.
/// </summary>
public enum OperatingMode
{
    /// <summary>
    /// Default mode. Local inference only, no external LLM APIs.
    /// </summary>
    LocalOnly = 0,
    
    /// <summary>
    /// Temporary mode allowing external LLM APIs with explicit consent.
    /// Session-scoped only; cannot be persisted.
    /// </summary>
    Burst = 1,
    
    /// <summary>
    /// Permanent mode with no network access whatsoever.
    /// Cannot be changed at runtime.
    /// </summary>
    Airgapped = 2
}

/// <summary>
/// Hard constraints that must never be violated.
/// </summary>
public static class HardConstraints
{
    public const string NoExternalLlmInLocalOnly = "HC-01";
    public const string NoNetworkInAirgapped = "HC-02";
    public const string ConsentRequiredForBurst = "HC-03";
    public const string SecretsRedactedBeforeTransmission = "HC-04";
}

/// <summary>
/// Result of a mode constraint check.
/// </summary>
public sealed record ConstraintCheckResult
{
    public bool IsAllowed { get; init; }
    public string? DenialReason { get; init; }
    public string? Remediation { get; init; }
    public string? ConstraintId { get; init; }
    
    public static ConstraintCheckResult Allowed() => new() { IsAllowed = true };
    
    public static ConstraintCheckResult Denied(
        string constraintId, 
        string reason, 
        string remediation) => new()
    {
        IsAllowed = false,
        ConstraintId = constraintId,
        DenialReason = reason,
        Remediation = remediation
    };
}

/// <summary>
/// Validates actions against current operating mode.
/// </summary>
public interface IModeValidator
{
    ConstraintCheckResult ValidateAction(
        OperatingMode currentMode,
        ActionType action,
        bool hasConsent = false);
        
    bool CanTransition(
        OperatingMode from,
        OperatingMode to,
        bool hasConsent = false);
}
```

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 10 | Mode validation failed |
| 11 | Constraint violation |
| 12 | Invalid mode specified |
| 13 | Airgapped override attempted |
| 14 | Consent required but not given |
| 15 | Mode transition failed |

### Logging Schema

```json
{
  "timestamp": "ISO8601",
  "level": "info|warn|error",
  "event": "mode_check|mode_transition|constraint_violation",
  "session_id": "guid",
  "mode_current": "LocalOnly|Burst|Airgapped",
  "mode_target": "LocalOnly|Burst|Airgapped|null",
  "action": "string",
  "result": "allowed|denied",
  "constraint_id": "HC-01|HC-02|...|null",
  "reason": "string|null",
  "consent_given": "bool|null"
}
```

### Validation Checklist Before Merge

- [ ] All mode enum values defined
- [ ] All constraints documented
- [ ] All transitions implemented
- [ ] All tests passing
- [ ] 100% constraint code coverage
- [ ] Documentation complete
- [ ] Audit logging verified
- [ ] Performance targets met
- [ ] Security review passed
- [ ] No constraint bypasses possible

### Rollout Plan

1. **Phase 1:** Implement mode enum and constraints (this task)
2. **Phase 2:** Integrate with providers (Tasks 004-006)
3. **Phase 3:** Integrate with CLI (CLI tasks)
4. **Phase 4:** Add network blocking (Task 007)
5. **Phase 5:** Security audit

---

**END OF TASK 001**
