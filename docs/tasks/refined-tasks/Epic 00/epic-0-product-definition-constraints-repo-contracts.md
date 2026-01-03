# EPIC 0 — Product Definition, Constraints, Repo Contracts

**Priority:** 1 / 13 (Highest - Foundation Epic)  
**Phase:** Foundation  
**Dependencies:** None (This is the foundational epic)  

---

## Epic Overview

### Purpose

Epic 0 establishes the foundational architecture, governance model, and operational framework for the Agentic Coding Bot (Acode). This epic is the bedrock upon which all subsequent functionality is built. Without the artifacts produced by this epic, no other work can proceed safely or consistently.

The Agentic Coding Bot is designed to be a locally-hosted, privacy-first, AI-powered coding assistant that operates entirely within the user's infrastructure. Unlike cloud-based coding assistants, Acode enforces strict boundaries around data sovereignty, network access, and external API usage. This epic defines those boundaries and creates the mechanisms to enforce them.

### Business Value

The primary business value of this epic is **risk mitigation** and **foundation stability**. By investing heavily in constraints, contracts, and threat modeling upfront, we:

1. **Prevent catastrophic failures** — Clear operating modes prevent accidental data exfiltration or unauthorized API calls
2. **Enable auditability** — Every action the agent takes can be traced, logged, and reviewed
3. **Reduce rework** — A well-defined project structure prevents architectural drift and refactoring churn
4. **Accelerate future development** — Clean Architecture boundaries and repo contracts make adding features predictable
5. **Build trust** — Users can verify that Acode respects their privacy and security requirements

### Boundaries

**This epic includes:**
- Repository initialization and .NET solution structure
- Documentation scaffolding (README, REPO_STRUCTURE, CONFIG, OPERATING_MODES)
- Operating mode definitions (local-only, burst, airgapped)
- Hard constraint enforcement mechanisms
- Repo contract file specification (`.agent/config.yml`)
- Threat model and default safety posture
- Audit baseline requirements

**This epic does NOT include:**
- Model provider implementations (Epic 1)
- CLI command implementations (Epic 2)
- Any runtime code beyond structural scaffolding
- Integration with external systems

### Dependencies

This is the foundational epic. All other epics depend on the artifacts produced here:
- **Epic 1** depends on operating mode constraints defined in Task 001
- **Epic 2** depends on CLI structure established in Task 000
- **Epics 3-12** depend on the repo contract format defined in Task 002
- **All Epics** must respect the threat model and safety posture from Task 003

---

## Tasks in This Epic

| Task | Title | Complexity | Dependencies |
|------|-------|------------|--------------|
| 000 | Project Bootstrap & Solution Structure | 8 | None |
| 000.a | Create repo + .NET solution + baseline project layout | 5 | None |
| 000.b | Add baseline docs (README, REPO_STRUCTURE, CONFIG, OPERATING_MODES) | 3 | 000.a |
| 000.c | Add baseline tooling + formatting + test scaffolding | 5 | 000.a |
| 001 | Define Operating Modes & Hard Constraints | 8 | 000 |
| 001.a | Define mode matrix (local-only / burst / airgapped) | 5 | 000.b |
| 001.b | Define "no external LLM API" validation rules | 5 | 001.a |
| 001.c | Write constraints doc + enforcement checklist | 3 | 001.a, 001.b |
| 002 | Define Repo Contract File (.agent/config.yml) | 8 | 001 |
| 002.a | Define schema + examples | 5 | 001.c |
| 002.b | Implement parser + validator requirements | 5 | 002.a |
| 002.c | Define command groups (setup/build/test/lint/format/start) | 3 | 002.a |
| 003 | Threat Model & Default Safety Posture | 8 | 001, 002 |
| 003.a | Enumerate risk categories + mitigations | 5 | 002.c |
| 003.b | Define default denylist + protected paths | 5 | 003.a |
| 003.c | Define audit baseline requirements | 5 | 003.a, 003.b |

---

## Outcomes

1. A fully initialized Git repository with proper `.gitignore`, `.gitattributes`, and branch protection recommendations
2. A .NET 8.0+ solution following Clean Architecture with Domain, Application, Infrastructure, CLI, and Tests projects
3. A comprehensive README.md explaining the project purpose, setup, and usage
4. A REPO_STRUCTURE.md documenting the canonical folder layout and naming conventions
5. A CONFIG.md explaining all configuration options and their defaults
6. An OPERATING_MODES.md defining local-only, burst, and airgapped modes with their constraints
7. EditorConfig, `.editorconfig`, and formatting rules enforced via tooling
8. A test scaffolding with xUnit, FluentAssertions, and NSubstitute configured
9. A complete mode matrix document showing what operations are permitted in each mode
10. Validation rules that prevent any external LLM API calls in local-only mode
11. An enforcement checklist that can be used during code review and CI
12. A complete `.agent/config.yml` schema with JSON Schema validation
13. Example config files for common scenarios (.NET, React, Python, minimal)
14. Parser and validator requirements documented for implementation in Epic 2
15. Command group definitions for setup, build, test, lint, format, and start
16. A threat model document enumerating all risk categories
17. Mitigation strategies for each identified risk
18. A default denylist of file patterns that MUST NOT be modified by the agent
19. Protected path definitions for sensitive directories (`.git`, credentials, etc.)
20. Audit baseline requirements specifying what events MUST be logged
21. Log schema definitions for audit events
22. Verification procedures for audit log integrity
23. CI pipeline templates for running constraint checks
24. A CONTRIBUTING.md guide for future contributors
25. Architecture Decision Records (ADRs) for key decisions made in this epic

---

## Non-Goals

1. Implementing any model provider adapters (deferred to Epic 1)
2. Implementing the CLI command framework (deferred to Epic 2)
3. Implementing file indexing or search (deferred to Epic 3)
4. Implementing command execution or sandboxing (deferred to Epic 4)
5. Implementing Git automation (deferred to Epic 5)
6. Creating any UI or interactive elements beyond basic CLI scaffolding
7. Integrating with any external services or APIs
8. Implementing any AI/ML inference capabilities
9. Creating Docker containers or container orchestration
10. Implementing any data persistence beyond configuration files
11. Creating user authentication or authorization systems
12. Implementing any network communication beyond localhost
13. Supporting languages other than C# in the initial scaffolding
14. Creating production deployment artifacts
15. Implementing telemetry or analytics collection
16. Creating plugin or extension systems
17. Implementing any caching mechanisms
18. Creating backup or disaster recovery systems
19. Implementing rate limiting or throttling
20. Creating any billing or licensing enforcement

---

## Architecture & Integration Points

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                         CLI Layer                            │
│  (Acode.Cli)                                                │
│  - Entry point, command parsing, output formatting           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│  (Acode.Application)                                        │
│  - Use cases, orchestration, DTOs                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Domain Layer                            │
│  (Acode.Domain)                                             │
│  - Entities, value objects, domain services, interfaces     │
└─────────────────────────────────────────────────────────────┘
                              ▲
                              │
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                       │
│  (Acode.Infrastructure)                                     │
│  - External integrations, file system, persistence          │
└─────────────────────────────────────────────────────────────┘
```

### Key Interfaces Defined in This Epic

| Interface | Layer | Purpose |
|-----------|-------|---------|
| `IOperatingModeProvider` | Domain | Returns current operating mode and validates operations |
| `IConstraintValidator` | Domain | Validates operations against mode constraints |
| `IRepoContractReader` | Application | Reads and parses `.agent/config.yml` |
| `IRepoContractValidator` | Application | Validates repo contract against schema |
| `IAuditLogger` | Domain | Logs audit events (interface only; implementation in Epic 9) |
| `IPathAccessPolicy` | Domain | Determines if a path is accessible/modifiable |

### Events Defined in This Epic

| Event | Description |
|-------|-------------|
| `ModeViolationAttempted` | Raised when an operation violates the current mode's constraints |
| `ProtectedPathAccessAttempted` | Raised when access to a protected path is attempted |
| `ConfigValidationFailed` | Raised when `.agent/config.yml` fails validation |
| `AuditEventLogged` | Raised when any auditable action occurs |

### Data Contracts

**OperatingMode (Enum)**
```csharp
public enum OperatingMode
{
    LocalOnly,    // No network access, local models only
    Burst,        // Temporary cloud compute allowed, no external LLM APIs
    Airgapped     // Complete network isolation, strictest mode
}
```

**RepoContract (Configuration)**
```yaml
version: "1.0"
project:
  name: string
  type: dotnet | node | python | other
commands:
  setup: string[]
  build: string[]
  test: string[]
  lint: string[]
  format: string[]
  start: string[]
safety:
  protected_paths: string[]
  denylist_patterns: string[]
  max_file_size_bytes: number
```

---

## Operational Considerations

### Mode Enforcement

| Mode | Network | External LLM APIs | Cloud Compute | Local Models |
|------|---------|-------------------|---------------|--------------|
| LocalOnly | ❌ Blocked | ❌ Blocked | ❌ Blocked | ✅ Required |
| Burst | ✅ Allowed | ❌ Blocked | ✅ Allowed | ✅ Optional |
| Airgapped | ❌ Blocked | ❌ Blocked | ❌ Blocked | ✅ Required |

### Safety Defaults

All safety features are **opt-out, not opt-in**. The default configuration:
- Blocks modification of `.git/`, `.github/`, `.vscode/`, `node_modules/`, `bin/`, `obj/`
- Blocks access to files matching `*.pem`, `*.key`, `*.env`, `*secret*`, `*credential*`
- Limits file modifications to 100KB per file by default
- Requires explicit approval for any file deletion
- Logs all file modifications, command executions, and model interactions

### Audit Requirements

Every operation MUST produce an audit log entry containing:
- Timestamp (UTC, ISO 8601)
- Operation type
- Actor (user or automation)
- Target (file path, command, etc.)
- Outcome (success, failure, blocked)
- Mode at time of operation
- Session ID

---

## Acceptance Criteria / Definition of Done

### Repository Structure (15 items)
- [ ] Git repository initialized with `.gitignore` for .NET, Node, Python
- [ ] `.gitattributes` configured for consistent line endings
- [ ] `README.md` exists with project overview, setup, and usage sections
- [ ] `REPO_STRUCTURE.md` documents all directories and their purposes
- [ ] `CONFIG.md` documents all configuration options
- [ ] `OPERATING_MODES.md` defines all three modes with constraints
- [ ] `CONTRIBUTING.md` provides contribution guidelines
- [ ] `LICENSE` file present (MIT or as specified)
- [ ] `.editorconfig` enforces coding style
- [ ] `Directory.Build.props` centralizes project settings
- [ ] `Directory.Packages.props` enables central package management
- [ ] `.github/` directory contains issue and PR templates
- [ ] `docs/` directory structure matches specification
- [ ] All documentation files use consistent Markdown formatting
- [ ] Repository passes `markdownlint` with zero errors

### Solution Structure (20 items)
- [ ] `Acode.sln` solution file exists at repository root
- [ ] `src/Acode.Domain/` project exists with correct references
- [ ] `src/Acode.Application/` project exists with correct references
- [ ] `src/Acode.Infrastructure/` project exists with correct references
- [ ] `src/Acode.Cli/` project exists as executable
- [ ] `tests/Acode.Domain.Tests/` project exists
- [ ] `tests/Acode.Application.Tests/` project exists
- [ ] `tests/Acode.Infrastructure.Tests/` project exists
- [ ] `tests/Acode.Cli.Tests/` project exists
- [ ] `tests/Acode.Integration.Tests/` project exists
- [ ] All projects target .NET 8.0 or later
- [ ] Domain project has no external dependencies except abstractions
- [ ] Application project references only Domain
- [ ] Infrastructure project references Domain and Application
- [ ] CLI project references all layers appropriately
- [ ] Test projects reference appropriate production projects
- [ ] `dotnet build` succeeds with zero warnings (TreatWarningsAsErrors=true)
- [ ] `dotnet test` runs with zero test failures (placeholder tests)
- [ ] Solution compiles in under 30 seconds on reference hardware
- [ ] NuGet packages restored from local cache or nuget.org only

### Operating Modes (25 items)
- [ ] `OperatingMode` enum defined with LocalOnly, Burst, Airgapped values
- [ ] Mode matrix document specifies permitted operations per mode
- [ ] LocalOnly mode blocks all outbound network connections
- [ ] LocalOnly mode requires local model provider
- [ ] Burst mode allows cloud compute targets only
- [ ] Burst mode blocks external LLM API endpoints (OpenAI, Anthropic, etc.)
- [ ] Airgapped mode blocks all network including localhost:8080+ model endpoints
- [ ] Mode can be set via environment variable `ACODE_MODE`
- [ ] Mode can be set via `.agent/config.yml`
- [ ] Mode can be overridden via CLI flag `--mode`
- [ ] Mode precedence: CLI > Environment > Config > Default(LocalOnly)
- [ ] Mode violations raise `ModeViolationAttempted` event
- [ ] Mode violations are logged with full context
- [ ] Mode validation occurs before any operation execution
- [ ] External LLM API blocklist includes: api.openai.com, api.anthropic.com, api.cohere.ai, api.ai21.com, generativelanguage.googleapis.com
- [ ] Blocklist is configurable but defaults are not removable
- [ ] Mode switching requires explicit user confirmation
- [ ] Mode is displayed in CLI output header
- [ ] Mode is included in all audit log entries
- [ ] Unit tests cover all mode transition scenarios
- [ ] Integration tests verify network blocking in LocalOnly mode
- [ ] E2E tests verify mode enforcement end-to-end
- [ ] Documentation includes mode selection guide
- [ ] Error messages for mode violations are actionable
- [ ] Mode validation performance is under 1ms

### Repo Contract (25 items)
- [ ] `.agent/config.yml` JSON Schema defined and documented
- [ ] Schema version field is required (`version: "1.0"`)
- [ ] Project section defines name and type
- [ ] Commands section supports setup, build, test, lint, format, start
- [ ] Each command group is an array of shell commands
- [ ] Safety section defines protected_paths and denylist_patterns
- [ ] Parser handles missing optional fields with defaults
- [ ] Parser rejects unknown fields in strict mode
- [ ] Validator produces actionable error messages
- [ ] Validator error messages include line numbers
- [ ] Schema supports YAML anchors and aliases
- [ ] Schema examples provided for .NET, Node, Python projects
- [ ] Minimal valid config example documented
- [ ] Full-featured config example documented
- [ ] Config inheritance from parent directories supported
- [ ] Config validation runs in under 10ms
- [ ] Config changes trigger re-validation
- [ ] Config file watching is supported (for future use)
- [ ] Config errors prevent agent startup
- [ ] Config warnings are logged but don't prevent startup
- [ ] JSON Schema published to schema store (or local)
- [ ] VS Code settings recommend YAML extension for config editing
- [ ] IntelliSense support documented for config editing
- [ ] Config migration path documented for version upgrades
- [ ] Unit tests cover all schema validation scenarios

### Threat Model & Safety (30 items)
- [ ] Threat model document enumerates all risk categories
- [ ] Risk categories include: data exfiltration, unauthorized modification, credential exposure, resource exhaustion, denial of service
- [ ] Each risk has severity rating (Critical, High, Medium, Low)
- [ ] Each risk has likelihood rating
- [ ] Each risk has one or more mitigations
- [ ] Mitigations map to specific code implementations
- [ ] Default denylist blocks `.git/` directory modifications
- [ ] Default denylist blocks `.env` files
- [ ] Default denylist blocks `**/secrets/**` paths
- [ ] Default denylist blocks `**/*.pem`, `**/*.key`, `**/*.pfx`
- [ ] Default denylist blocks `**/credentials*` files
- [ ] Default denylist blocks `**/.ssh/**` paths
- [ ] Protected paths include all version control directories
- [ ] Protected paths include CI/CD configuration files
- [ ] Path patterns support glob syntax
- [ ] Path matching is case-insensitive on Windows
- [ ] Denylist additions are cumulative (can't remove defaults)
- [ ] Allowlist can selectively permit denied paths with justification
- [ ] Justification for allowlist entries is logged
- [ ] File size limits enforced (default 100KB)
- [ ] Binary file detection prevents text operations on binaries
- [ ] Symlink following is disabled by default
- [ ] Path traversal attacks prevented (no `../` escaping workspace)
- [ ] Audit log captures all safety-relevant events
- [ ] Audit log format follows structured logging standards
- [ ] Audit log includes correlation IDs
- [ ] Audit log integrity can be verified
- [ ] Audit log rotation policy defined
- [ ] Audit log retention policy defined
- [ ] Security review checklist provided for contributions

### Testing & Quality (15 items)
- [ ] xUnit test framework configured
- [ ] FluentAssertions configured for readable assertions
- [ ] NSubstitute configured for mocking
- [ ] Code coverage collection configured (target: 80%+)
- [ ] Coverage reports generated in CI
- [ ] Mutation testing configured (Stryker.NET)
- [ ] Linting via `dotnet format` enforced
- [ ] Static analysis via Roslyn analyzers enabled
- [ ] Security scanning via security-code-scan enabled
- [ ] All tests pass in under 60 seconds
- [ ] Test naming follows convention: `MethodName_Scenario_ExpectedResult`
- [ ] Tests are organized by layer matching production code
- [ ] Integration tests use TestContainers or similar isolation
- [ ] E2E tests run against compiled CLI executable
- [ ] Performance benchmarks established for critical paths

### Documentation (10 items)
- [ ] All public APIs have XML documentation
- [ ] Architecture diagrams rendered as SVG/PNG
- [ ] Decision log (ADR) format established
- [ ] At least 3 ADRs written for key decisions
- [ ] Glossary of terms maintained
- [ ] FAQ section addresses common questions
- [ ] Troubleshooting guide covers known issues
- [ ] Version history / changelog format established
- [ ] Documentation spell-checked
- [ ] All links in documentation validated

---

## Risks & Mitigations

| # | Risk | Likelihood | Impact | Mitigation |
|---|------|------------|--------|------------|
| 1 | Over-engineering the initial structure | High | Medium | Define "just enough" structure; defer extensibility to later epics |
| 2 | Mode enforcement can be bypassed | Medium | Critical | Multiple enforcement layers; audit logging; code review checklist |
| 3 | Config schema too rigid | Medium | Medium | Design for extensibility with `x-` prefixed custom fields |
| 4 | Config schema too permissive | Medium | High | Strict validation by default; explicit opt-in for lax modes |
| 5 | Threat model incomplete | High | High | Regular threat model reviews; security-focused code review |
| 6 | Denylist too restrictive | Medium | Medium | Clear documentation; easy allowlist process with audit trail |
| 7 | Denylist too permissive | Low | Critical | Conservative defaults; no way to remove built-in denies |
| 8 | Audit logging impacts performance | Medium | Medium | Async logging; configurable log levels; buffered writes |
| 9 | Clean Architecture adds complexity | Low | Medium | Clear layer responsibilities; comprehensive examples |
| 10 | .NET 8 dependency limits portability | Low | Low | .NET 8 is LTS; container deployment mitigates |
| 11 | Documentation becomes stale | High | Medium | Documentation tests; CI checks for doc freshness |
| 12 | Test scaffolding insufficient | Medium | Medium | Start with comprehensive test patterns; expand iteratively |
| 13 | Schema versioning conflicts | Low | High | Semver for schema; clear migration paths |
| 14 | Contributor onboarding too complex | Medium | Medium | Detailed CONTRIBUTING.md; video walkthrough |

---

## Milestone Plan

### Milestone 0.1: Repository Bootstrap (Tasks 000, 000.a, 000.b, 000.c)
**Target:** Week 1  
**Deliverables:**
- Initialized Git repository
- .NET solution with all projects
- Baseline documentation
- Tooling and formatting configured
- Test scaffolding in place

**Exit Criteria:**
- `dotnet build` succeeds
- `dotnet test` runs with placeholder tests passing
- All documentation files present and formatted

### Milestone 0.2: Operating Modes (Tasks 001, 001.a, 001.b, 001.c)
**Target:** Week 2  
**Deliverables:**
- Operating mode enum and interfaces
- Mode matrix documentation
- External LLM API blocklist
- Enforcement checklist

**Exit Criteria:**
- Mode validation logic implemented (unit tested)
- Documentation reviewed and approved
- Enforcement checklist integrated into PR template

### Milestone 0.3: Repo Contract (Tasks 002, 002.a, 002.b, 002.c)
**Target:** Week 3  
**Deliverables:**
- JSON Schema for `.agent/config.yml`
- Example config files
- Parser and validator requirements spec
- Command group definitions

**Exit Criteria:**
- Schema validates all example files
- Validator requirements documented for Epic 2
- Command groups cover common project types

### Milestone 0.4: Threat Model & Safety (Tasks 003, 003.a, 003.b, 003.c)
**Target:** Week 4  
**Deliverables:**
- Threat model document
- Risk registry
- Default denylist and protected paths
- Audit baseline requirements

**Exit Criteria:**
- All identified risks have mitigations
- Denylist blocks all sensitive patterns
- Audit requirements implementable in Epic 9

### Milestone 0.5: Epic Complete
**Target:** Week 5  
**Deliverables:**
- All documentation finalized
- All ADRs written
- Epic retrospective conducted
- Handoff package for Epic 1

**Exit Criteria:**
- All acceptance criteria checked
- No open blockers for Epic 1
- Stakeholder sign-off obtained

---

## Definition of Epic Complete

### Code & Structure
- [ ] All projects compile with zero warnings
- [ ] All projects have TreatWarningsAsErrors enabled
- [ ] All tests pass
- [ ] Code coverage exceeds 80% for new code
- [ ] Static analysis shows zero high/critical issues
- [ ] All TODO comments resolved or tracked as issues

### Documentation
- [ ] README.md complete and reviewed
- [ ] REPO_STRUCTURE.md matches actual structure
- [ ] CONFIG.md documents all options
- [ ] OPERATING_MODES.md reviewed by security
- [ ] All ADRs written and approved
- [ ] API documentation generated and published
- [ ] Changelog updated

### Process
- [ ] All tasks marked complete in tracking system
- [ ] All PRs merged to main branch
- [ ] All CI checks passing on main
- [ ] Release tag created (v0.1.0-alpha)
- [ ] Retrospective conducted and documented
- [ ] Lessons learned captured

### Handoff
- [ ] Epic 1 team briefed on constraints and contracts
- [ ] No blocking questions from Epic 1 team
- [ ] Dependency graph updated in project management tool
- [ ] Risk register reviewed and updated
- [ ] Stakeholder demo completed

### Quality Gates
- [ ] Security review completed
- [ ] Architecture review completed
- [ ] Documentation review completed
- [ ] Performance baseline established
- [ ] No known critical bugs

---

**END OF EPIC 0**
