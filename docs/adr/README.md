# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records (ADRs) for the Acode project.

## What is an ADR?

An Architecture Decision Record (ADR) captures an important architectural decision made along with its context and consequences. ADRs are lightweight documents that help teams understand why certain technical choices were made.

## Format

Each ADR follows this structure:

- **Status**: Proposed, Accepted, Deprecated, Superseded
- **Context**: The issue, problem, or opportunity
- **Decision**: The change being proposed or implemented
- **Consequences**: The resulting context after applying the decision (both positive and negative)
- **Alternatives Considered**: Other options explored and why they were rejected

## ADR Index

### Core Privacy & Safety Decisions

| ADR | Title | Status | Related Constraints |
|-----|-------|--------|-------------------|
| [ADR-001](adr-001-no-external-llm-default.md) | No External LLM API by Default | Accepted | HC-01, HC-03 |
| [ADR-002](adr-002-three-operating-modes.md) | Three Operating Modes | Accepted | HC-01, HC-02, HC-03 |
| [ADR-003](adr-003-airgapped-permanence.md) | Airgapped Mode Permanence | Accepted | HC-02 |
| [ADR-004](adr-004-burst-mode-consent.md) | Burst Mode Requires Consent | Accepted | HC-03 |
| [ADR-005](adr-005-secrets-redaction.md) | Secrets Redaction Before Transmission | Accepted | HC-04 |

### Architectural Decisions

| ADR | Title | Status | Related |
|-----|-------|--------|---------|
| [ADR-001](001-clean-architecture.md) | Clean Architecture Adoption | Accepted | All |

## Adding a New ADR

1. Copy the ADR template
2. Assign the next sequential number (e.g., adr-006-title.md)
3. Fill in all sections completely
4. Submit via pull request
5. Update this index
6. Mark status as "Proposed" until approved
7. Change status to "Accepted" after team approval

## ADR Lifecycle

- **Proposed**: ADR is drafted and under review
- **Accepted**: ADR is approved and represents current architecture
- **Deprecated**: ADR is no longer recommended (but not replaced)
- **Superseded**: ADR has been replaced by a newer ADR (link to replacement)

## Guidelines

- ADRs are **immutable** once accepted - do not edit accepted ADRs
- To change a decision, create a new ADR that supersedes the old one
- Reference ADRs in code comments when implementing decisions
- Keep ADRs concise but complete
- Focus on the "why" not the "how" (implementation details go elsewhere)
- Link to related constraints in CONSTRAINTS.md

## Resources

- [ADR GitHub Organization](https://adr.github.io/)
- [Documenting Architecture Decisions](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
- [Acode Constraints Reference](../../CONSTRAINTS.md)

---

**Questions?** See [CONTRIBUTING.md](../../CONTRIBUTING.md) or open an issue.
