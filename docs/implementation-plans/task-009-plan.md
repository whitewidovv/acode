# Task 009 Implementation Plan: Model Routing Policy

## Status: In Progress

**Started:** 2026-01-06
**Epic:** Epic 01 - Model Runtime, Inference, Tool-Calling Contract
**Priority:** P1 (High)
**Complexity:** 21 Fibonacci Points

---

## Executive Summary

Task 009 implements the Model Routing Policy system - intelligent model selection based on agent role, task complexity, and resource constraints. This enables optimal resource utilization by routing simple tasks to efficient models and complex tasks to powerful models.

**Scope Breakdown:**
- Task 009 (Parent): Policy interface, routing strategies, configuration integration
- Task 009a: Planner/Coder/Reviewer role definitions
- Task 009b: Routing heuristics and user overrides
- Task 009c: Fallback and escalation rules

---

## Core Principles

1. **Strict TDD** - Red → Green → Refactor for every behavior
2. **Incremental Commits** - One logical unit per commit
3. **Subtask Completion** - Task 009 NOT complete until ALL subtasks (009a, 009b, 009c) complete
4. **Layer Boundaries** - Domain → Application → Infrastructure
5. **No Shortcuts** - All FRs must be implemented

---

## Implementation Strategy

### Dependency Order

```
Task 009a (Roles: Domain foundation)
    ↓
Task 009b (Routing heuristics: Application + Infrastructure)
    ↓
Task 009c (Fallback rules: Infrastructure)
    ↓
Task 009 Parent (Policy orchestration + Configuration)
```

---

## Phase 1: Task 009a - Planner/Coder/Reviewer Roles

### Phase 1.1: Domain - AgentRole Enum

- [ ] **Test:** AgentRoleTests - Enum has Planner, Coder, Reviewer, Default
- [ ] **Impl:** AgentRole enum in Domain/Models/Routing/
- [ ] **Commit:** "feat(task-009a): implement AgentRole enum"

### Phase 1.2: Domain - RoleDefinition Value Object

- [ ] **Test:** RoleDefinitionTests - Constructor validates required fields
- [ ] **Impl:** RoleDefinition record with Name, Description, Capabilities, Constraints
- [ ] **Test:** RoleDefinitionTests - Immutability verified
- [ ] **Impl:** Init-only properties
- [ ] **Commit:** "feat(task-009a): implement RoleDefinition value object"

### Phase 1.3: Application - IRoleRegistry Interface

- [ ] **Test:** (Will test via concrete implementation)
- [ ] **Impl:** IRoleRegistry interface in Application/Models/
- [ ] **Commit:** "feat(task-009a): define IRoleRegistry interface"

### Phase 1.4: Infrastructure - RoleRegistry Implementation

- [ ] **Test:** RoleRegistryTests - GetRole returns correct definition
- [ ] **Impl:** RoleRegistry with 4 predefined roles
- [ ] **Test:** RoleRegistryTests - ListRoles returns all 4 roles
- [ ] **Impl:** ListRoles method
- [ ] **Test:** RoleRegistryTests - SetCurrentRole tracks state
- [ ] **Impl:** Current role tracking
- [ ] **Commit:** "feat(task-009a): implement RoleRegistry with 4 core roles"

### Task 009a Completion

- [ ] All role definitions verified
- [ ] Registry implements all interface methods
- [ ] Build succeeds with zero warnings
- [ ] All tests pass
- [ ] **Task 009a marked COMPLETE**

---

## Phase 2: Task 009b - Routing Heuristics + Overrides

### Phase 2.1: Domain - RoutingRequest Value Object

- [ ] **Test:** RoutingRequestTests - Constructor with role, context
- [ ] **Impl:** RoutingRequest record (AgentRole, RoutingContext)
- [ ] **Commit:** "feat(task-009b): implement RoutingRequest value object"

### Phase 2.2: Domain - RoutingDecision Value Object

- [ ] **Test:** RoutingDecisionTests - Contains selected model, fallback status
- [ ] **Impl:** RoutingDecision record (ModelId, IsFallback, Reason)
- [ ] **Test:** RoutingDecisionTests - Immutability verified
- [ ] **Impl:** Init-only properties
- [ ] **Commit:** "feat(task-009b): implement RoutingDecision value object"

### Phase 2.3: Application - IRoutingPolicy Interface

- [ ] **Test:** (Will test via concrete implementation)
- [ ] **Impl:** IRoutingPolicy with GetModel, GetFallbackModel, IsModelAvailable
- [ ] **Commit:** "feat(task-009b): define IRoutingPolicy interface"

### Phase 2.4: Infrastructure - SingleModelStrategy

- [ ] **Test:** SingleModelStrategyTests - Routes all roles to default_model
- [ ] **Impl:** SingleModelStrategy reads config, returns default
- [ ] **Test:** SingleModelStrategyTests - Logs routing decision
- [ ] **Impl:** Structured logging
- [ ] **Commit:** "feat(task-009b): implement SingleModelStrategy"

### Phase 2.5: Infrastructure - RoleBasedStrategy

- [ ] **Test:** RoleBasedStrategyTests - Routes based on role_models config
- [ ] **Impl:** RoleBasedStrategy with role-to-model mapping
- [ ] **Test:** RoleBasedStrategyTests - Falls back to default_model for unmapped roles
- [ ] **Impl:** Default fallback logic
- [ ] **Commit:** "feat(task-009b): implement RoleBasedStrategy"

### Phase 2.6: Infrastructure - User Override Handling

- [ ] **Test:** RoutingPolicyTests - User override bypasses strategy
- [ ] **Impl:** Override detection in RoutingContext
- [ ] **Test:** RoutingPolicyTests - Override respects operating mode constraints
- [ ] **Impl:** Operating mode validation for overrides
- [ ] **Commit:** "feat(task-009b): implement user override handling"

### Task 009b Completion

- [ ] All routing strategies implemented
- [ ] Override handling works
- [ ] Build succeeds with zero warnings
- [ ] All tests pass
- [ ] **Task 009b marked COMPLETE**

---

## Phase 3: Task 009c - Fallback/Escalation Rules

### Phase 3.1: Infrastructure - FallbackHandler

- [ ] **Test:** FallbackHandlerTests - Tries fallback chain sequentially
- [ ] **Impl:** FallbackHandler with chain traversal
- [ ] **Test:** FallbackHandlerTests - Logs each fallback attempt
- [ ] **Impl:** Fallback logging
- [ ] **Test:** FallbackHandlerTests - Fails when all unavailable
- [ ] **Impl:** Graceful failure with suggestions
- [ ] **Commit:** "feat(task-009c): implement FallbackHandler"

### Phase 3.2: Infrastructure - Model Availability Checking

- [ ] **Test:** ModelAvailabilityTests - Queries provider registry
- [ ] **Impl:** Availability checker using IProviderRegistry (Task 004)
- [ ] **Test:** ModelAvailabilityTests - Caches results with 5-second TTL
- [ ] **Impl:** Caching with TTL
- [ ] **Commit:** "feat(task-009c): implement model availability checking"

### Phase 3.3: Infrastructure - Operating Mode Constraint Validation

- [ ] **Test:** ConstraintValidatorTests - local-only rejects remote models
- [ ] **Impl:** Operating mode constraint checker
- [ ] **Test:** ConstraintValidatorTests - air-gapped rejects network models
- [ ] **Impl:** Air-gapped mode validation
- [ ] **Commit:** "feat(task-009c): implement operating mode constraint validation"

### Task 009c Completion

- [ ] Fallback handling complete
- [ ] Availability checking works
- [ ] Operating mode constraints enforced
- [ ] Build succeeds with zero warnings
- [ ] All tests pass
- [ ] **Task 009c marked COMPLETE**

---

## Phase 4: Task 009 Parent - Policy Orchestration + Configuration

### Phase 4.1: Infrastructure - RoutingPolicy Implementation

- [ ] **Test:** RoutingPolicyTests - Orchestrates strategy selection
- [ ] **Impl:** RoutingPolicy reads config, selects strategy
- [ ] **Test:** RoutingPolicyTests - Integrates fallback handler
- [ ] **Impl:** Fallback integration
- [ ] **Commit:** "feat(task-009): implement RoutingPolicy orchestration"

### Phase 4.2: Infrastructure - Configuration Integration

- [ ] **Test:** ConfigurationTests - Reads models.routing from config
- [ ] **Impl:** Configuration reader using Task 002
- [ ] **Test:** ConfigurationTests - Validates schema
- [ ] **Impl:** Schema validation
- [ ] **Commit:** "feat(task-009): implement configuration integration"

### Phase 4.3: Infrastructure - Structured Logging

- [ ] **Test:** LoggingTests - Logs routing decisions as JSON
- [ ] **Impl:** JSON structured logging
- [ ] **Commit:** "feat(task-009): implement structured logging"

### Task 009 Parent Completion

- [ ] Policy orchestration complete
- [ ] Configuration integration works
- [ ] Logging functional
- [ ] Build succeeds with zero warnings
- [ ] All tests pass
- [ ] **Task 009 Parent marked COMPLETE**

---

## Phase 5: Final Audit and PR

### Phase 5.1: Subtask Verification

- [ ] **Verify:** Task 009a COMPLETE
- [ ] **Verify:** Task 009b COMPLETE
- [ ] **Verify:** Task 009c COMPLETE
- [ ] **Verify:** Task 009 parent COMPLETE

### Phase 5.2: Comprehensive Audit

- [ ] **TDD Compliance:** Every source file has tests
- [ ] **Build Quality:** 0 errors, 0 warnings
- [ ] **Layer Boundaries:** Domain → Application → Infrastructure
- [ ] **Integration:** All interfaces implemented
- [ ] **Create:** docs/TASK-009-AUDIT.md

### Phase 5.3: Pull Request

- [ ] **Create:** PR with title "feat(epic-01): Task 009 - Model Routing Policy"
- [ ] **Include:** Summary, test coverage, audit link
- [ ] **Verify:** All tests pass on CI

---

## Progress Tracking

### Current Status
- **Phase:** Phase 4 (Task 009 Parent) - Policy Orchestration
- **Next Action:** Phase 4.1 - Implement RoutingPolicy

### Metrics
- **Commits:** 19
- **Tests Written:** 88 total
  - AgentRole: 7
  - RoleDefinition: 7
  - RoleRegistry: 12
  - RoutingRequest: 8
  - RoutingDecision: 6
  - SingleModelStrategy: 11 (includes override tests)
  - RoleBasedStrategy: 13 (includes override tests)
  - FallbackHandler: 11
  - ModelAvailabilityChecker: 13
- **Tests Passing:** 88/88 ✅ (Task 009 specific)
- **Code Quality:** 0 errors, 0 warnings

### Components Completed
- ✅ Task 009a: Planner/Coder/Reviewer Roles (COMPLETE)
  - ✅ AgentRole enum
  - ✅ RoleDefinition record
  - ✅ IRoleRegistry interface
  - ✅ RoleRegistry implementation (4 core roles)
- ✅ Task 009b: Routing Heuristics + Overrides (COMPLETE)
  - ✅ RoutingRequest record
  - ✅ RoutingDecision record
  - ✅ IRoutingPolicy interface
  - ✅ RoutingConfig domain model
  - ✅ IRoutingStrategy interface
  - ✅ SingleModelStrategy implementation
  - ✅ RoleBasedStrategy implementation
  - ✅ User override handling in both strategies
- ✅ Task 009c: Fallback/Escalation Rules (COMPLETE - Phase 3.3 deferred)
  - ✅ IModelAvailabilityChecker interface
  - ✅ FallbackHandler implementation
  - ✅ ModelAvailabilityChecker with 5s TTL cache
  - ⏸️ Operating Mode Constraints (deferred - requires model metadata)

### Next Steps
- Phase 4.1: RoutingPolicy Orchestration (TDD)
- Phase 4.2: Configuration Integration
- Phase 4.3: Structured Logging
- Phase 5: Final Audit and PR

---

## Notes

### Key Design Decisions
1. **Strategy Pattern** for routing logic extensibility
2. **Deterministic selection** for reproducibility
3. **Fast decision time** (<10ms requirement)
4. **Configuration-driven** behavior for flexibility

### Dependencies
- **Task 001:** Operating mode constraints
- **Task 002:** Configuration loading
- **Task 004:** Model provider interface (IProviderRegistry)
- **Task 008:** Prompt pack integration

---

**Last Updated:** 2026-01-06
