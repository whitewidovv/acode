# INSTRUCTIONS FOR CLAUDE TO COMPLETE THIS TASK (REFINED SPEC TARGET)

You are expanding a **task stub** into a *complete, enterprise-grade, implementation-ready* specification for **Agentic Coding Bot (Acode)**.

These specs must be on par with our e-commerce task samples:
- Typical length: **8,457–22,968 words** (target **~10k–18k** unless task is genuinely smaller/larger)
- Acceptance Criteria / Definition of Done: typically **103–341 checkboxes** (target **~180–260**)

## Non-negotiable quality bar
- Write as if a mediocre automation engineer will implement it verbatim.
- No “hand-wavy” language (avoid: *should*, *ideally*, *nice to have*). Use *MUST* and *MUST NOT*.
- Every section must be objectively testable or auditable.
- Respect Clean Architecture boundaries (Domain → Application → Infrastructure → CLI).
- Respect Task 001 constraints (no external LLM APIs; mode rules).

## Required Sections (all required; do not delete)
1) Description
   - 6–12 paragraphs
   - Include: business value, scope boundaries, integration points (with task numbers), failure modes, assumptions
2) Glossary / Terms (10–25 entries where relevant)
3) Out-of-Scope (explicit bullets)
4) Functional Requirements (grouped; 40–120 items)
5) Non-Functional Requirements (security, performance, reliability; 20–60 items)
6) User Manual Documentation
   - 250–600 lines typical
   - Include: quick start, config knobs, CLI examples, best practices, troubleshooting, FAQs
7) Acceptance Criteria / Definition of Done
   - Target: 180–260 checkbox items
   - Must include categories: Functionality, Safety/Policy, CLI/UX, Logging/Audit, Performance, Docs, Tests, Compatibility
8) Testing Requirements (all 5 types)
   - Unit (15–30)
   - Integration (10–20)
   - E2E (8–15)
   - Performance/Benchmarks (5–10, with targets)
   - Regression (explicit impacted areas)
9) User Verification Steps
   - 12–20 scenarios with “Verify:” expectations
10) Implementation Prompt
   - 200–600 lines
   - Must include: file paths, class/interface names, contracts, error codes, logging fields
   - Must include “Validation checklist before merge”
   - Must include “Rollout plan” (even if local-only)

## Anti-footgun requirements
- Specify exit codes for CLI errors
- Specify logging schema fields
- Specify default config values and precedence
- Specify how secrets are redacted in logs/artifacts

---

## Canonical Context (from task-list.md)

- **Epic:** EPIC 8 — CI/CD Authoring + Deployment Hooks
- **Canonical Task Title:** Task 036.c: non-bypassable approvals (default)
- **Sibling Subtasks (if applicable):**
  - Task 036.a: deploy tool schema
  - Task 036.b: disabled by default
  - Task 036.c: non-bypassable approvals (default)

- **Hard constraints reminder:** MUST comply with Task 001 operating modes and the “no external LLM API” constraint set.
- **Repo contract reminder:** MUST align with Task 002 `.agent/config.yml` contract where relevant.

---

# Task 036.c: non-bypassable approvals (default)

**Priority:** TBD  
**Tier:** TBD  
**Complexity:** TBD (Fibonacci points)  
**Phase:** TBD  
**Dependencies:** Task 038, Task 021.c, Task 039, Task 050

---

## Description (EXPAND THIS)

**Update:** Approvals/disabled-by-default controls MUST be auditable and persist their decisions as DB events (who/when/why) with redaction.

**Update (export/provenance):** Any deploy artifacts (manifests, logs, diffs) included in export bundles MUST include provenance metadata and be redacted prior to packaging.

**Update (auditable deployments):** Deployment hook executions MUST be recorded as structured audit events in the Workspace DB (Task 050) with:
- inputs (redacted), approvals, targets, artifacts produced, and final status,
- correlation fields (run_id, worktree_id, repo_sha),
- sync state (Pending/Acked/Failed) if mirrored to remote Postgres.

**Update (Workspace DB Foundation / provenance):** This task MUST integrate with the Workspace DB (Task 050) to:
- persist and surface provenance metadata (repo SHA, worktree id, run_id/session_id, timestamps),
- record CI/deploy template generation events as audit events (Task 039), and
- expose “sync status” for any generated artifacts/config bundles that will be exported (Task 021.c / Task 039.b).

Provide a complete description including business value, scope, integration points, assumptions, and failure modes.

---

## Glossary / Terms (ADD AS NEEDED)

---

## Out of Scope (EXPLICIT)

---

## Functional Requirements (LIST)

---

## Non-Functional Requirements (LIST)

---

## User Manual Documentation (WRITE COMPLETE)

---

## Acceptance Criteria / Definition of Done (CHECKLIST)

- [ ] TODO

---

## Testing Requirements

### Unit Tests
- [ ] TODO

### Integration Tests
- [ ] TODO

### End-to-End Tests
- [ ] TODO

### Performance / Benchmarks
- [ ] TODO

### Regression / Impacted Areas
- [ ] TODO

---

## User Verification Steps

- [ ] TODO

---

## Implementation Prompt for Claude

Provide a step-by-step implementation guide (file paths, contracts, interfaces, error handling, logging, validation checklist, rollout plan).

---

**END OF TASK 036.c**
