# EPIC 9 — Safety, Policy Engine, Secrets Hygiene, Audit

**Priority:** P0 – Critical  
**Phase:** Phase 9 – Safety & Compliance  
**Dependencies:** Epic 05 (Core Infrastructure), Task 050 (Workspace DB)  

---

## Epic Overview

Epic 9 implements the safety, policy, and compliance foundation for Acode. This epic ensures that all agent operations are governed by configurable policies, secrets are properly handled, and complete audit trails are maintained.

The Policy-as-Config Engine (Task 037) provides a hierarchical configuration system where global policies can be overridden at repository and task levels. This enables organizations to set baseline security requirements while allowing project-specific customization.

Secrets Redaction and Diff Scanning (Task 038) ensures that sensitive information is never exposed to models, logged inappropriately, or committed to version control. This is a critical security boundary.

The Audit Trail and Export system (Task 039) records all tool calls, commands, diffs, and model interactions. Export bundles enable compliance reviews and incident analysis while ensuring no raw secrets are ever included.

### Business Value

- **Policy Governance**: Configurable policies at multiple levels
- **Secrets Protection**: Prevent secret exposure in all contexts
- **Compliance**: Complete audit trail for regulatory requirements
- **Safety Boundaries**: Enforceable limits on agent behavior

### Epic Boundaries

This epic covers policy configuration, secrets handling, and audit systems. It does NOT cover:
- Model inference (Epic 06)
- Tool execution (Epic 05)
- CI/CD generation (Epic 08)

### Cross-Cutting Concerns

All components in this epic integrate with:
- Workspace DB (Task 050) for persistence
- Event Bus for async notifications
- Configuration system for policy loading
- Logging infrastructure for structured output

---

## Outcomes

1. Policy engine loads and evaluates policies from config hierarchy
2. Global policies enforced across all operations
3. Repository-level overrides customizable via `.agent/config.yml`
4. Per-task overrides available for specific operations
5. Policy violations block operations with clear messages
6. Secrets detected in tool output before model exposure
7. Secrets redacted from logs and audit trails
8. Commit/push blocked when secrets detected
9. Configurable secret patterns with regex support
10. Corpus tests validate redaction completeness
11. All tool calls recorded in audit trail
12. Commands and their outputs captured
13. Diffs recorded with redaction applied
14. Model prompts and responses logged (redacted)
15. Export bundles contain complete audit data
16. Export verification confirms no raw secrets
17. Audit data queryable and searchable
18. Retention policies enforced
19. Performance impact minimal (<50ms overhead)
20. All systems work in all operating modes

---

## Non-Goals

1. Real-time policy editing UI
2. Remote policy distribution
3. Multi-tenant policy management
4. Policy marketplace
5. External secrets vault integration
6. Hardware security module support
7. Real-time model output filtering (post-hoc only)
8. Custom DSL for policy rules
9. Policy simulation/testing environment
10. Automated policy suggestions
11. Export to external audit systems
12. SIEM integration
13. Compliance certification automation
14. Role-based access control for policies
15. Policy versioning with rollback

---

## Architecture & Integration Points

### Core Interfaces

| Interface | Location | Purpose |
|-----------|----------|---------|
| `IPolicyEngine` | Application | Evaluate policies |
| `IPolicyLoader` | Infrastructure | Load policy hierarchy |
| `IPolicyEvaluator` | Application | Check specific policies |
| `ISecretScanner` | Application | Detect secrets |
| `ISecretRedactor` | Application | Remove secrets |
| `IAuditRecorder` | Application | Record events |
| `IAuditExporter` | Application | Export bundles |
| `IExportVerifier` | Application | Verify no secrets |

### Data Contracts

```csharp
// Policy evaluation result
record PolicyResult(bool Allowed, string? DenialReason, PolicyLevel Source);

// Secret detection result
record SecretMatch(string Pattern, int StartIndex, int Length, string Category);

// Audit event
record AuditEvent(
    Guid Id,
    string Type,
    DateTime Timestamp,
    string Payload, // Redacted
    Dictionary<string, string> Metadata
);
```

### Events

| Event | Publisher | Consumers |
|-------|-----------|-----------|
| `PolicyViolationEvent` | PolicyEngine | CLI, Audit |
| `SecretDetectedEvent` | SecretScanner | Audit, Block |
| `AuditEventRecorded` | AuditRecorder | Export, DB |
| `ExportCreatedEvent` | AuditExporter | Notification |

---

## Operational Considerations

### Mode Compliance

| Mode | Policy | Secrets | Audit |
|------|--------|---------|-------|
| Local-Only | Full evaluation | Full scanning | Local DB |
| Burst | Full evaluation | Full scanning | Local DB |
| Air-Gapped | Full evaluation | Full scanning | Local DB only |

### Security Boundaries

1. Secrets MUST be redacted before model exposure
2. Secrets MUST NOT appear in any logs
3. Export bundles MUST be verified before creation
4. Policy violations MUST block operations
5. Audit trail MUST be tamper-resistant
6. Redaction patterns MUST be configurable
7. No bypass mechanisms for secrets protection

### Performance Targets

- Policy evaluation: <10ms
- Secret scanning: <100ms per 1MB
- Audit write: <50ms
- Export generation: <5s for typical session
- Memory overhead: <100MB

---

## Acceptance Criteria / Definition of Done

### Policy Engine (Task 037)
- [ ] AC-001: `IPolicyEngine` interface exists
- [ ] AC-002: Policy hierarchy loads correctly
- [ ] AC-003: Global policies enforced
- [ ] AC-004: Repo overrides work
- [ ] AC-005: Task overrides work
- [ ] AC-006: Precedence order correct
- [ ] AC-007: Violations block operations
- [ ] AC-008: Clear error messages
- [ ] AC-009: Policy caching works
- [ ] AC-010: Hot reload supported
- [ ] AC-011: Evaluation < 10ms
- [ ] AC-012: All policies documented
- [ ] AC-013: Schema validation works
- [ ] AC-014: Invalid config rejected
- [ ] AC-015: Events emitted

### Secrets Handling (Task 038)
- [ ] AC-016: Secret patterns configurable
- [ ] AC-017: Built-in patterns for common secrets
- [ ] AC-018: Regex patterns supported
- [ ] AC-019: Tool output scanned before model
- [ ] AC-020: Matches redacted with [REDACTED]
- [ ] AC-021: Commit blocked on detection
- [ ] AC-022: Push blocked on detection
- [ ] AC-023: Clear user notification
- [ ] AC-024: Bypass not possible
- [ ] AC-025: Corpus tests pass
- [ ] AC-026: False positive rate tracked
- [ ] AC-027: Performance < 100ms/MB
- [ ] AC-028: Categories configurable
- [ ] AC-029: Severity levels work
- [ ] AC-030: Logging never includes secrets

### Audit Trail (Task 039)
- [ ] AC-031: Tool calls recorded
- [ ] AC-032: Commands recorded
- [ ] AC-033: Diffs recorded (redacted)
- [ ] AC-034: Model prompts recorded
- [ ] AC-035: Model responses recorded
- [ ] AC-036: Timestamps accurate
- [ ] AC-037: Correlation IDs present
- [ ] AC-038: Queryable by time
- [ ] AC-039: Queryable by type
- [ ] AC-040: Searchable
- [ ] AC-041: Export bundle created
- [ ] AC-042: Export includes all data
- [ ] AC-043: Export verified for secrets
- [ ] AC-044: Export format documented
- [ ] AC-045: Retention policy enforced
- [ ] AC-046: Purge command works
- [ ] AC-047: Export < 5s typical
- [ ] AC-048: No secrets in export
- [ ] AC-049: Checksum for integrity
- [ ] AC-050: CLI commands work

---

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Secret pattern false negatives | High | Medium | Comprehensive corpus tests |
| Performance degradation | Medium | Medium | Efficient algorithms, caching |
| Policy conflicts | Medium | Low | Clear precedence rules |
| Audit storage exhaustion | Low | Medium | Retention policies, compression |
| Export with secrets | Critical | Low | Multiple verification passes |
| Hot reload race conditions | Medium | Low | Locking, atomic updates |
| Pattern regex DoS | Medium | Low | Timeout, complexity limits |
| Incomplete audit | High | Low | Guaranteed recording |
| Redaction over-aggressive | Low | Medium | Tunable patterns |
| Config parsing errors | Medium | Medium | Schema validation |
| Memory pressure from scanning | Low | Medium | Streaming approach |
| Policy evaluation loops | Medium | Low | Cycle detection |

---

## Milestone Plan

### Milestone 1: Policy Foundation
**Tasks:** 037, 037.a, 037.b, 037.c  
**Deliverables:**
- Policy engine core
- Global policy configuration
- Repository overrides
- Per-task overrides

### Milestone 2: Secrets Protection
**Tasks:** 038, 038.a, 038.b, 038.c  
**Deliverables:**
- Secret scanner
- Tool output redaction
- Commit/push blocking
- Configurable patterns

### Milestone 3: Audit & Export
**Tasks:** 039, 039.a, 039.b, 039.c  
**Deliverables:**
- Audit recording
- Export bundle generation
- Secret verification
- CLI commands

---

## Definition of Epic Complete

- [ ] All 12 tasks implemented and tested
- [ ] Policy engine functional with hierarchy
- [ ] Secret scanning covers all tool output
- [ ] Commit/push blocking prevents secret exposure
- [ ] Audit trail records all operations
- [ ] Export bundles verifiable
- [ ] No secrets in any export
- [ ] CLI commands for all operations
- [ ] Documentation complete
- [ ] Unit test coverage > 80%
- [ ] Integration tests pass
- [ ] Performance benchmarks met
- [ ] Security review passed
- [ ] All operating modes tested
- [ ] Corpus tests validate patterns
- [ ] Schema validation works
- [ ] Error messages clear
- [ ] Events emitted correctly
- [ ] Metrics exposed
- [ ] Logging structured

---

## Task Summary

| Task | Title | Priority | Dependencies |
|------|-------|----------|--------------|
| 037 | Policy-as-Config Engine | P0 | Task 002 |
| 037.a | Global Policy Config | P0 | Task 037 |
| 037.b | Repo Overrides | P0 | Task 037 |
| 037.c | Per-Task Overrides | P1 | Task 037 |
| 038 | Secrets Redaction + Diff Scanning | P0 | - |
| 038.a | Redact Tool Output Before Model | P0 | Task 038 |
| 038.b | Block Commit/Push on Detection | P0 | Task 038 |
| 038.c | Configurable Patterns + Corpus | P0 | Task 038 |
| 039 | Audit Trail + Export | P0 | Task 050 |
| 039.a | Record Tool Calls/Commands | P0 | Task 039 |
| 039.b | Export Bundle | P0 | Task 039 |
| 039.c | Verify Export No Raw Secrets | P0 | Task 039, 038 |

---

**END OF EPIC 9**
