# Task 009 Suite Gap Analysis Plan

## Executive Summary

This document outlines the systematic gap analysis and implementation plan for Task 009 (Model Routing Policy) and all its subtasks (009a, 009b, 009c). Following the gap analysis methodology, we will verify actual implementation state against specifications, implement missing components, and ensure all tests pass.

---

## Task Suite Overview

| Task | Name | Lines | Focus Area |
|------|------|-------|------------|
| 009 | Model Routing Policy | 3,213 | Core routing interface, strategies, fallback handling |
| 009a | Planner/Coder/Reviewer Roles | 3,110 | AgentRole enum, RoleDefinition, IRoleRegistry, role transitions |
| 009b | Routing Heuristics & Overrides | 3,092 | Complexity estimation, token routing, load balancing, overrides |
| 009c | Fallback & Escalation Rules | 3,125 | Fallback chains, escalation policies, recovery strategies |

**Total Specification Lines:** ~12,540

---

## Execution Order

We will implement in this order to respect dependencies:

1. **Task 009 (Parent)** - Foundation types and interfaces
2. **Task 009a** - Role definitions and registry (builds on AgentRole from 009)
3. **Task 009b** - Routing heuristics (builds on RoutingContext from 009)
4. **Task 009c** - Fallback/escalation (builds on FallbackHandler from 009)

---

## Task 009: Model Routing Policy

### Current Implementation State

**Status: NOT IMPLEMENTED**

Based on search results:
- No `IRoutingPolicy` interface found
- No `AgentRole` enum found (related to model routing)
- No `RoutingDecision` class found
- No `ModelRegistry` (routing-specific) found
- Existing `Routing/` folder in CLI contains only FuzzyMatcher (command routing, not model routing)

### Required Files (from Implementation Prompt)

#### Application Layer (`src/Acode.Application/Routing/`)
| File | Status | Description |
|------|--------|-------------|
| IRoutingPolicy.cs | ❌ MISSING | Core routing interface |
| AgentRole.cs | ❌ MISSING | Enum for agent roles |
| RoutingContext.cs | ❌ MISSING | Context for routing decisions |
| RoutingDecision.cs | ❌ MISSING | Result of routing decision |
| RoutingConfiguration.cs | ❌ MISSING | Configuration data class |
| RoutingStrategy.cs | ❌ MISSING | Enum for routing strategies |
| TaskComplexity.cs | ❌ MISSING | Enum for complexity levels |
| RoutingException.cs | ❌ MISSING | Custom exception type |

#### Infrastructure Layer (`src/Acode.Infrastructure/Routing/`)
| File | Status | Description |
|------|--------|-------------|
| RoutingPolicy.cs | ❌ MISSING | Main routing implementation |
| SingleModelStrategy.cs | ❌ MISSING | Single model strategy |
| RoleBasedStrategy.cs | ❌ MISSING | Role-based strategy |
| AdaptiveStrategy.cs | ❌ MISSING | Adaptive strategy (future) |
| FallbackHandler.cs | ❌ MISSING | Fallback chain logic |
| AvailabilityChecker.cs | ❌ MISSING | Model availability checks |
| ModelRegistry.cs | ❌ MISSING | Model registry and cache |
| ConfigurationValidator.cs | ❌ MISSING | Configuration validation |

#### Test Files (`tests/Acode.Infrastructure.Tests/Routing/`)
| File | Status | Expected Tests |
|------|--------|----------------|
| RoutingPolicyTests.cs | ❌ MISSING | ~10 tests |
| FallbackHandlerTests.cs | ❌ MISSING | ~6 tests |
| RoutingIntegrationTests.cs | ❌ MISSING | ~3 tests |
| RoutingPerformanceTests.cs | ❌ MISSING | ~2 tests |

### Acceptance Criteria (51 items)
- AC-001 to AC-006: Interface definition
- AC-007 to AC-012: Implementation requirements
- AC-013 to AC-016: Role definitions
- AC-017 to AC-022: Configuration
- AC-023 to AC-025: Single strategy
- AC-026 to AC-029: Role-based strategy
- AC-030 to AC-034: Fallback handling
- AC-035 to AC-038: Mode constraints
- AC-039 to AC-042: Availability checking
- AC-043 to AC-047: CLI commands
- AC-048 to AC-051: Logging

### Tests Required (from Testing Requirements)
1. Should_Route_Planner_Role_To_Configured_Large_Model
2. Should_Route_Coder_Role_To_Configured_Small_Model
3. Should_Use_Single_Model_Strategy_When_Configured
4. Should_Fallback_To_Secondary_Model_When_Primary_Unavailable
5. Should_Respect_Operating_Mode_Constraints
6. Should_Throw_When_All_Fallback_Models_Unavailable
7. Should_Use_Default_Model_When_Role_Not_Configured
8. Should_Honor_User_Override_In_Routing_Context
9. Should_Cache_Availability_Checks_Within_TTL_Window
10. Should_Validate_Model_ID_Format_Before_Selection

---

## Task 009a: Planner/Coder/Reviewer Roles

**Spec Location:** `docs/tasks/refined-tasks/Epic 01/task-009a-planner-coder-reviewer-roles.md`

### Key Deliverables (to be extracted from spec)
- RoleDefinition value object
- IRoleRegistry interface
- Role transition validation
- Context strategy differentiation
- Tool filtering by role

### Critical Sections to Read
- Acceptance Criteria: Line 1562+
- Testing Requirements: Line 1635+
- Implementation Prompt: Line 2873+

---

## Task 009b: Routing Heuristics & Overrides

**Spec Location:** `docs/tasks/refined-tasks/Epic 01/task-009b-routing-heuristics-overrides.md`

### Key Deliverables (to be extracted from spec)
- Complexity estimation heuristics
- Token-based routing
- Load balancing strategies
- Cost optimization rules
- Override mechanisms

### Critical Sections to Read
- Acceptance Criteria: Line 1489+
- Testing Requirements: Line 1610+
- Implementation Prompt: Line 2919+

---

## Task 009c: Fallback & Escalation Rules

**Spec Location:** `docs/tasks/refined-tasks/Epic 01/task-009c-fallback-escalation-rules.md`

### Key Deliverables (to be extracted from spec)
- Fallback chain configuration
- Escalation policies
- Graceful degradation
- Recovery strategies
- Circuit breaker pattern

### Critical Sections to Read
- Acceptance Criteria: Line 1831+
- Testing Requirements: Line 1946+
- Implementation Prompt: Line 3011+

---

## Implementation Checklist

### Phase 1: Task 009 Parent
- [ ] Create Application/Routing directory
- [ ] Implement AgentRole enum
- [ ] Implement RoutingStrategy enum
- [ ] Implement TaskComplexity enum
- [ ] Implement RoutingContext class
- [ ] Implement RoutingDecision class
- [ ] Implement RoutingConfiguration class
- [ ] Implement RoutingException class
- [ ] Implement IRoutingPolicy interface
- [ ] Create Infrastructure/Routing directory
- [ ] Implement SingleModelStrategy
- [ ] Implement RoleBasedStrategy
- [ ] Implement AdaptiveStrategy (placeholder)
- [ ] Implement FallbackHandler
- [ ] Implement ModelRegistry
- [ ] Implement AvailabilityChecker
- [ ] Implement RoutingPolicy
- [ ] Create all tests
- [ ] Verify all tests pass

### Phase 2: Task 009a
- [ ] Read spec completely
- [ ] Implement RoleDefinition
- [ ] Implement IRoleRegistry
- [ ] Implement role transition validation
- [ ] Create all tests
- [ ] Verify all tests pass

### Phase 3: Task 009b
- [ ] Read spec completely
- [ ] Implement complexity heuristics
- [ ] Implement override mechanisms
- [ ] Create all tests
- [ ] Verify all tests pass

### Phase 4: Task 009c
- [ ] Read spec completely
- [ ] Implement fallback chain logic
- [ ] Implement escalation policies
- [ ] Create all tests
- [ ] Verify all tests pass

---

## Verification Commands

```powershell
# Run all Task 009 tests
dotnet test --filter "FullyQualifiedName~Routing"

# Build with warnings as errors
dotnet build --warnaserror

# Check for StyleCop violations
dotnet build /p:TreatWarningsAsErrors=true
```

---

**Created:** 2026-01-10
**Methodology:** Gap Analysis Methodology v1.0
