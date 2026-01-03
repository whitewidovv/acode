# Task 003 Implementation Plan: Threat Model & Default Safety Posture

## Status: In Progress

## Overview

Implement comprehensive threat model and default safety posture for Acode, establishing security foundation through:
- Task 003: Core threat model framework and default security posture
- Task 003.a: Risk enumeration with STRIDE/DREAD methodology
- Task 003.b: Default denylist and protected paths
- Task 003.c: Audit baseline requirements

## Strategic Approach

Following TDD with Clean Architecture boundaries:
1. **Domain Layer**: Security entities, enums, invariants (pure business logic)
2. **Application Layer**: Security services interfaces and implementations
3. **Infrastructure Layer**: File system implementations, concrete redactors
4. **CLI Layer**: Security commands for user interaction
5. **Documentation**: SECURITY.md, threat model, risk register

Each subtask will be completed with:
- RED → GREEN → REFACTOR cycle
- Commit after each logical unit
- Push to feature branch after each commit
- Autonomous progression through all subtasks

## Subtask Breakdown

### Task 003 - Core Threat Model Framework

#### 003.a - Domain Layer: Security Types
- [ ] ThreatActor enum
- [ ] AttackVector record
- [ ] TrustBoundary enum
- [ ] DataClassification enum
- [ ] SecurityInvariant records
- [ ] FailSafeBehavior record
- [ ] SecurityEvent record
- [ ] SecuritySeverity enum
- [ ] SecurityEventCodes constants

#### 003.b - Application Layer: Security Interfaces
- [ ] ISecurityChecker interface + implementation
- [ ] ISecretRedactor interface + implementation
- [ ] IPathValidator interface + implementation
- [ ] IInvariantEnforcer interface + implementation
- [ ] IDataClassifier interface + implementation
- [ ] ITrustBoundaryMonitor interface + implementation
- [ ] ISecurityAuditLogger interface + implementation
- [ ] IConsentManager interface + implementation

#### 003.c - Infrastructure Layer: Security Implementations
- [ ] FileSystemPathValidator
- [ ] RegexSecretRedactor with default patterns
- [ ] JsonSecurityAuditLogger with tamper-evident hashing
- [ ] ConsoleConsentManager
- [ ] WebhookSecurityNotifier (basic)

#### 003.d - CLI Layer: Security Commands
- [ ] SecurityCommand (parent)
- [ ] SecurityStatusCommand
- [ ] SecurityCheckCommand
- [ ] SecurityAuditCommand
- [ ] SecurityThreatsCommand
- [ ] SecurityScanSecretsCommand

#### 003.e - Documentation
- [ ] SECURITY.md (public threat model overview)
- [ ] docs/security/threat-model.md (detailed)
- [ ] docs/security/trust-boundaries.md
- [ ] docs/security/data-classification.md
- [ ] Trust boundary diagrams (ASCII/text-based)

### Task 003.a - Risk Enumeration & Mitigations

#### 003a.a - Domain Layer: Risk Types
- [ ] RiskId value object with validation (RISK-X-NNN format)
- [ ] RiskCategory enum (STRIDE)
- [ ] DreadScore value object with calculation
- [ ] Risk record
- [ ] Mitigation record
- [ ] MitigationStatus enum
- [ ] Severity enum

#### 003a.b - Risk Register Data
- [ ] docs/security/risk-register.yaml (40+ risks)
  - Spoofing risks (6+)
  - Tampering risks (7+)
  - Repudiation risks (5+)
  - Information Disclosure risks (10+)
  - Denial of Service risks (7+)
  - Elevation of Privilege risks (7+)
- [ ] All risks have DREAD scores
- [ ] All risks mapped to mitigations
- [ ] High-severity risks have multiple mitigations

#### 003a.c - Application Layer: Risk Services
- [ ] IRiskRegister interface + implementation
- [ ] IRiskFilter interface + implementation
- [ ] IMitigationVerifier interface + implementation
- [ ] RiskRegisterLoader (YAML parser)
- [ ] RiskRegisterExporter (JSON/YAML/Markdown)

#### 003a.d - Infrastructure Layer: Risk Persistence
- [ ] YamlRiskRegisterRepository
- [ ] File-based risk register storage
- [ ] Export formatters

#### 003a.e - CLI Layer: Risk Commands
- [ ] RisksCommand (list all, filter by category/severity)
- [ ] RiskDetailCommand (show individual risk)
- [ ] MitigationsCommand (list mitigations)
- [ ] VerifyMitigationsCommand (run verification tests)
- [ ] RiskCoverageCommand (coverage metrics)

#### 003a.f - Documentation
- [ ] docs/security/risk-register.md (generated from YAML)
- [ ] docs/security/scoring/dread-methodology.md
- [ ] Individual mitigation docs (MIT-001.md, MIT-002.md, etc.)

### Task 003.b - Default Denylist & Protected Paths

#### 003b.a - Domain Layer: Path Protection Types
- [ ] ProtectedPathPattern value object
- [ ] DenylistRule record
- [ ] PathAccessDecision enum
- [ ] PathAccessReason record

#### 003b.b - Application Layer: Path Services
- [ ] IPathProtectionService interface
- [ ] PathProtectionService implementation
- [ ] Default protected paths (`.git/`, `.ssh/`, `.env`, etc.)
- [ ] Default denylist patterns (secrets, credentials, keys)
- [ ] Path traversal detection
- [ ] Symlink safety checks

#### 003b.c - Configuration
- [ ] Protected paths in config schema
- [ ] Denylist patterns in config schema
- [ ] Allowlist override capability (with audit)

#### 003b.d - Documentation
- [ ] docs/security/protected-paths.md
- [ ] docs/security/denylist.md
- [ ] User guidance on customizing protection

### Task 003.c - Audit Baseline Requirements

#### 003c.a - Domain Layer: Audit Types
- [ ] AuditEvent record
- [ ] AuditEventType enum
- [ ] AuditLogEntry record
- [ ] AuditIntegrityResult record

#### 003c.b - Application Layer: Audit Services
- [ ] IAuditLogger interface (extended from 003)
- [ ] Audit event schema
- [ ] Tamper-evident hash chain
- [ ] Structured logging (JSON)
- [ ] Correlation ID tracking

#### 003c.c - Infrastructure Layer: Audit Persistence
- [ ] File-based audit logger
- [ ] JSON structured logs
- [ ] Log rotation policy
- [ ] Integrity verification

#### 003c.d - Documentation
- [ ] docs/security/audit-requirements.md
- [ ] Audit log schema documentation
- [ ] Retention and rotation policy

## Completed

✅ Strategic implementation plan created
✅ Feature branch created: `feature/task-003-threat-model`

### Domain Layer - Complete (22 types, 23 commits)

✅ **Security Types (Core):**
  - ✅ SecuritySeverity enum (5 levels: Debug → Critical)
  - ✅ ThreatActor enum (10 actors: User, Agent, ExternalLlm, LocalModel, etc.)
  - ✅ DataClassification enum (4 levels: Public → Secret)
  - ✅ TrustBoundary enum (8 boundaries)
  - ✅ AttackVector record (VectorId, Description, ThreatActor, Boundary)

✅ **Risk Types (Task 003.a - Complete):**
  - ✅ RiskCategory enum (6 STRIDE categories)
  - ✅ RiskId value object (format: RISK-[STRIDE]-NNN, with validation)
  - ✅ Severity enum (4 levels: Low, Medium, High, Critical)
  - ✅ DreadScore value object (5 components, auto-calculated average & severity)
  - ✅ MitigationStatus enum (4 statuses)
  - ✅ Mitigation record (full mitigation metadata)
  - ✅ Risk record (complete risk with DREAD, mitigations, attack vectors)

✅ **PathProtection Types (Task 003.b - Complete):**
  - ✅ PathCategory enum (9 categories: SshKeys, GpgKeys, CloudCredentials, etc.)
  - ✅ Platform enum (4 platforms: Windows, Linux, MacOS, All)
  - ✅ DenylistEntry record (Pattern, Reason, RiskId, Category, Platforms)
  - ✅ DefaultDenylist static class (45+ protected paths immutable)

✅ **Audit Types (Task 003.c - Complete):**
  - ✅ AuditEventType enum (25 mandatory event types)
  - ✅ AuditSeverity enum (5 levels)
  - ✅ EventId value object (GUID wrapper with validation)
  - ✅ SessionId value object (GUID wrapper with validation)
  - ✅ CorrelationId value object (GUID wrapper with validation)
  - ✅ AuditEvent record (complete audit event structure)

✅ **Integration Test Fixes (from Task 002):**
  - ✅ ISchemaValidator interface created
  - ✅ JsonSchemaValidator implements ISchemaValidator
  - ✅ ConfigValidator wired to use JsonSchemaValidator via DI
  - ✅ ConfigCommand fixed to use repository root instead of config path
  - ✅ Program.cs fixed to pass current directory
  - ✅ Schema constrained to only allow schema_version "1.0.0"
  - ✅ ConfigE2ETests path resolution fixed
  - ✅ All 6 integration tests now passing (was 2/6)

**Commits so far:** 29 (22 domain + 1 integration fix + 1 plan update + 3 app interfaces + 2 app types)
**Tests passing:** 366 total (255 Domain + 59 Application + 35 Infrastructure + 11 CLI + 6 Integration)
**Test coverage:** 100% of all implemented types
**New tests this session:** 66 domain tests + 11 application tests + integration test fixes
**Lines of Code:** ~2400 lines production code, ~3000 lines test code

### Application Layer - Interfaces Complete

✅ **Path Protection:**
  - ✅ FileOperation enum (Read, Write, Delete, List)
  - ✅ PathValidationResult record (Allowed/Blocked factory methods)
  - ✅ IProtectedPathValidator interface

✅ **Audit:**
  - ✅ IAuditLogger interface (LogAsync, FlushAsync)

✅ **Secret Redaction:**
  - ✅ RedactedContent record
  - ✅ ISecretRedactor interface (Redact with optional file path context)

## In Progress

🔄 Infrastructure Layer: Implementing security services (ProtectedPathValidator, JsonAuditLogger, etc.)

## Remaining

- Infrastructure Layer: Security implementations (ProtectedPathValidator, RegexSecretRedactor, JsonAuditLogger)
- CLI Layer: Security commands (show-denylist, check-path, security-status, audit commands)
- Documentation: SECURITY.md, threat-model.md, risk-register.md
- Audit per AUDIT-GUIDELINES.md
- Create PR when audit passes

## Key Decisions

1. **TDD Mandatory**: Every class gets tests first (RED-GREEN-REFACTOR)
2. **Clean Architecture**: Strict layer boundaries, no dependencies upward
3. **Security First**: Fail-safe defaults, deny-by-default posture
4. **Documentation**: Public SECURITY.md + detailed internal docs
5. **Git Workflow**: Feature branch `feature/task-003-threat-model`, commit per logical unit

## Implementation Order

Following dependency chain:
1. Domain types (no dependencies) - Task 003.a
2. Application interfaces (depend on Domain) - Task 003.b
3. Infrastructure implementations (depend on Application) - Task 003.c
4. Risk register data (YAML) - Task 003.a data
5. Risk services (depend on risk data) - Task 003.a services
6. CLI commands (depend on services) - Task 003.d + 003.a CLI
7. Documentation (generated + hand-written) - Final phase
8. Audit (final integration) - Task 003.c

## Testing Strategy

- **Unit Tests**: Every domain entity, value object, service method
- **Integration Tests**: Service interactions, file I/O, YAML parsing
- **E2E Tests**: CLI commands, full security workflows
- **Performance**: Path validation <10ms, secret redaction <5ms/KB

## Success Criteria

- [ ] All 115+ functional requirements implemented
- [ ] All 50+ non-functional requirements met
- [ ] 40+ risks documented with DREAD scores
- [ ] All high-severity risks have multiple mitigations
- [ ] All security controls tested and verified
- [ ] SECURITY.md complete and reviewed
- [ ] Audit per docs/AUDIT-GUIDELINES.md passes
- [ ] All tests pass (unit + integration + E2E)
- [ ] Performance benchmarks met

## Notes

- Work autonomously through all subtasks
- Commit frequently (per logical unit)
- Update this plan as progress is made
- If context runs out, plan shows exactly where to resume
