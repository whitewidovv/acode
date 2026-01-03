# INSTRUCTIONS FOR CLAUDE TO COMPLETE THIS EPIC SUMMARY (REFINED SPEC TARGET)

You are expanding an **epic stub** into a complete EPIC specification for **Agentic Coding Bot (Acode)**.

Quality bar:
- This EPIC doc must make it easy to implement every task in the epic.
- It must define boundaries, shared interfaces, and cross-cutting constraints.

## Required Sections
1) Epic Overview (purpose, boundaries, dependencies)
2) Outcomes (10–25)
3) Non-Goals (10–25)
4) Architecture & Integration Points (interfaces, events, data contracts)
5) Operational Considerations (modes/safety/audit)
6) Acceptance Criteria / Definition of Done (50–120 checkboxes)
7) Risks & Mitigations (12+)
8) Milestone Plan (3–7 milestones mapping to tasks)
9) “Definition of Epic Complete” checklist (20–40)

---

## Canonical Context (from task-list.md)

- **Epic:** EPIC 1 — Model Runtime, Inference, Tool-Calling Contract
- **Tasks in this epic:**
- Task 004: Model Provider Interface
  - Task 004.a: Define message/tool-call types
  - Task 004.b: Define response format + usage reporting
  - Task 004.c: Provider registry + config selection
- Task 005: Ollama Provider Adapter
  - Task 005.a: Implement request/response + streaming handling
  - Task 005.b: Tool-call parsing + retry-on-invalid-json
  - Task 005.c: Setup docs + smoke test script
- Task 006: vLLM Provider Adapter
  - Task 006.a: Implement serving assumptions + client adapter
  - Task 006.b: Structured outputs enforcement integration
  - Task 006.c: Load/health-check endpoints + error handling
- Task 007: Tool Schema Registry + Strict Validation
  - Task 007.a: JSON Schema definitions for all core tools
  - Task 007.b: Validator errors → model retry contract
  - Task 007.c: Truncation + artifact attachment rules
- Task 008: Prompt Pack System
  - Task 008.a: Prompt pack file layout + hashing/versioning
  - Task 008.b: Loader/validator + selection via config
  - Task 008.c: Starter packs (dotnet/react, strict minimal diff)
- Task 009: Model Routing Policy
  - Task 009.a: Planner/coder/reviewer roles
  - Task 009.b: Routing heuristics + overrides
  - Task 009.c: Fallback escalation rules

---

# EPIC 1 — Model Runtime, Inference, Tool-Calling Contract

**Priority:** TBD  
**Phase:** TBD  
**Dependencies:** TBD  

---

## Epic Overview (EXPAND THIS)

---

## Outcomes

---

## Non-Goals

---

## Architecture & Integration Points

---

## Operational Considerations

---

## Acceptance Criteria / Definition of Done

- [ ] TODO

---

## Risks & Mitigations

- [ ] TODO

---

## Milestone Plan

---

## Definition of Epic Complete

- [ ] TODO

---

**END OF EPIC 1**
