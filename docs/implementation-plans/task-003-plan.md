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
✅ Domain Layer - Security Types:
  - ✅ SecuritySeverity enum (5 levels: Debug → Critical)
  - ✅ ThreatActor enum (10 actors: User, Agent, ExternalLlm, etc.)
  - ✅ DataClassification enum (4 levels: Public → Secret)
  - ✅ TrustBoundary enum (8 boundaries)
  - ✅ AttackVector record (VectorId, Description, ThreatActor, Boundary)
✅ Domain Layer - Risk Types (Task 003.a):
  - ✅ RiskCategory enum (6 STRIDE categories)
✅ Domain Layer - PathProtection Types (Task 003.b):
  - ✅ PathCategory enum (9 categories)
  - ✅ Platform enum (4 platforms: Windows, Linux, MacOS, All)
✅ Domain Layer - Audit Types (Task 003.c):
  - ✅ AuditEventType enum (25 mandatory event types)

**Commits so far:** 11 (all with strict TDD - RED→GREEN→REFACTOR)
**Tests passing:** 189 total (57 new Task-003 tests + 132 from Task-002)
**Test coverage:** 100% of all implemented domain types
**Lines of Code:** ~900 lines production code, ~1100 lines test code

## In Progress

🔄 Domain Layer: Continue with value objects and complex records

## Remaining

- Domain Layer: SecurityInvariant, FailSafeBehavior records
- Risk Types: RiskId, RiskCategory (STRIDE), DreadScore, Risk, Mitigation
- Protected Paths: PathCategory, DenylistEntry, DefaultDenylist data
- Audit Types: AuditEventType, AuditEvent, SessionId, CorrelationId
- Application Layer: Interfaces (ISecretRedactor, IPathValidator, etc.)
- Infrastructure Layer: Implementations
- CLI Layer: Security commands
- Documentation: SECURITY.md, threat-model.md
- Audit per AUDIT-GUIDELINES.md

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
