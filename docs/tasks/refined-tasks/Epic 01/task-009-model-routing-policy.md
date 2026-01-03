# Task 009: Model Routing Policy

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 004 (Model Provider Interface), Task 005 (Ollama Provider), Task 006 (vLLM Provider), Task 002 (.agent/config.yml)  

---

## Description

Task 009 implements the Model Routing Policy system, which determines which model handles which type of task within the agentic workflow. Different agent activities—planning, coding, reviewing—have different requirements for model capabilities. Routing policy allows optimal model assignment based on task characteristics, available resources, and user preferences.

Model routing addresses a core challenge in local AI: balancing quality with resource constraints. Large models produce better results but require significant GPU memory and compute time. Small models are faster and lighter but may struggle with complex tasks. The routing policy enables intelligent assignment—using powerful models where needed and efficient models where sufficient.

The agent operates in different roles during a workflow. The planner role breaks down high-level tasks into actionable steps. The coder role implements those steps with actual code changes. The reviewer role verifies correctness and provides feedback. Each role has distinct requirements—planning benefits from strong reasoning, coding requires precise instruction following, review needs critical analysis.

The routing policy maps roles to models. Users can configure different models for different roles in `.agent/config.yml`. For example, a 70B model for planning and review, but a 7B model for simple coding tasks. The policy engine resolves these mappings at runtime, considering availability and fallback rules.

Routing decisions consider task complexity. Simple tasks like variable renaming can use smaller models. Complex tasks like architectural refactoring benefit from larger models. The policy can include heuristics that estimate complexity and adjust routing accordingly. These heuristics can be overridden by explicit user configuration.

The policy integrates with the Model Provider Interface (Task 004). When the agent needs model inference, it requests a model for a specific role. The routing policy resolves this request to a concrete model configuration. The provider interface then handles the actual inference. This separation keeps routing logic independent of provider implementation.

Fallback escalation handles model unavailability. If the configured model for a role is unavailable (server down, insufficient resources), the policy can escalate to alternative models. Escalation follows a defined hierarchy—try the next model in the fallback chain, or fail gracefully if no alternatives are available. This ensures robustness.

Configuration supports multiple routing strategies. The simplest strategy uses a single model for all roles. The role-based strategy assigns different models to different roles. The adaptive strategy considers task complexity. Users choose the strategy that fits their setup and preferences.

The policy respects Task 001 operating mode constraints. In "local-only" mode, only local models are eligible for routing. In "air-gapped" mode, no network-based models are used even if configured. The routing policy enforces these constraints before model selection.

Observability includes logging of routing decisions. Each request logs which role requested inference, which model was selected, and why. Fallback events are logged prominently so users understand when and why escalation occurred. This visibility helps users tune their configuration.

The routing policy is extensible for future enhancements. New routing strategies can be added without changing existing code. Custom heuristics can be plugged in. The design follows the Open-Closed Principle—open for extension, closed for modification.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Model Routing | Mapping tasks to models |
| Routing Policy | Rules for model selection |
| Role | Agent mode (planner, coder, reviewer) |
| Planner Role | Task decomposition mode |
| Coder Role | Implementation mode |
| Reviewer Role | Verification mode |
| Task Complexity | Estimated difficulty of task |
| Routing Strategy | Approach to model selection |
| Single Model Strategy | One model for all roles |
| Role-based Strategy | Different models per role |
| Adaptive Strategy | Complexity-aware routing |
| Fallback | Alternative when primary unavailable |
| Escalation | Moving to fallback model |
| Fallback Chain | Ordered list of alternatives |
| Model Eligibility | Whether model can be used |
| Routing Decision | Result of policy evaluation |
| Policy Engine | Component that executes policy |
| Override | User-specified routing |
| Heuristic | Rule for estimating complexity |

---

## Out of Scope

The following items are explicitly excluded from Task 009:

- **Model loading/unloading** - Covered in provider tasks
- **Model inference execution** - Covered in Task 004
- **Provider implementation** - Covered in Tasks 005, 006
- **Prompt composition** - Covered in Task 008
- **Tool calling** - Covered in Task 007
- **Load balancing across instances** - Not in MVP
- **Cost optimization** - Not applicable (local models)
- **A/B testing of models** - Post-MVP
- **Model performance benchmarking** - Separate concern
- **Automatic model selection** - Post-MVP

---

## Functional Requirements

### IRoutingPolicy Interface

- FR-001: Interface MUST be defined in Application layer
- FR-002: Interface MUST have GetModel(role, context) method
- FR-003: GetModel MUST return ModelConfiguration
- FR-004: Interface MUST have GetFallbackModel(role, context) method
- FR-005: Interface MUST have IsModelAvailable(modelId) method
- FR-006: Interface MUST have ListAvailableModels() method

### RoutingPolicy Implementation

- FR-007: Implementation MUST be in Infrastructure layer
- FR-008: Policy MUST read configuration from config file
- FR-009: Policy MUST support single model strategy
- FR-010: Policy MUST support role-based strategy
- FR-011: Policy MUST respect operating mode constraints
- FR-012: Policy MUST log routing decisions

### Role Definitions

- FR-013: MUST define "planner" role
- FR-014: MUST define "coder" role
- FR-015: MUST define "reviewer" role
- FR-016: MUST define "default" role as fallback
- FR-017: Roles MUST be extensible

### Configuration Schema

- FR-018: Config section: models.routing
- FR-019: MUST have strategy field (single, role-based, adaptive)
- FR-020: Default strategy MUST be "single"
- FR-021: MUST have default_model field
- FR-022: MAY have role_models map
- FR-023: role_models keys: planner, coder, reviewer
- FR-024: MAY have fallback_chain array

### Single Model Strategy

- FR-025: MUST use default_model for all roles
- FR-026: No role-specific configuration required
- FR-027: Fallback MUST still apply

### Role-Based Strategy

- FR-028: MUST read role_models from config
- FR-029: Each role MAY have dedicated model
- FR-030: Missing role MUST use default_model
- FR-031: Role model MUST override default

### Model Resolution

- FR-032: Resolution MUST check model availability
- FR-033: Unavailable model MUST trigger fallback
- FR-034: Resolution MUST validate model ID
- FR-035: Invalid model ID MUST fail with clear error
- FR-036: Resolution MUST cache results per session

### Fallback Chain

- FR-037: Fallback chain MUST be ordered array
- FR-038: Chain traversal MUST be sequential
- FR-039: First available model MUST be selected
- FR-040: Empty chain or all unavailable MUST fail
- FR-041: Fallback MUST be logged as WARNING

### Operating Mode Constraints

- FR-042: MUST check Task 001 operating mode
- FR-043: local-only mode MUST use local models
- FR-044: air-gapped mode MUST reject remote
- FR-045: Mode violation MUST fail with error
- FR-046: Constraint check MUST precede selection

### Model Availability

- FR-047: Availability MUST query provider registry
- FR-048: Availability MUST check model is loaded
- FR-049: Availability MUST timeout after 5s
- FR-050: Unavailable MUST trigger fallback

### CLI Integration

- FR-051: `acode models routing` MUST show config
- FR-052: MUST show current strategy
- FR-053: MUST show role assignments
- FR-054: MUST show fallback chain
- FR-055: `acode models test <role>` MUST test routing

### Logging

- FR-056: Routing request MUST be logged
- FR-057: Selected model MUST be logged
- FR-058: Selection reason MUST be logged
- FR-059: Fallback events MUST be logged WARNING
- FR-060: Mode constraint checks MUST be logged

### Error Handling

- FR-061: No available model MUST fail gracefully
- FR-062: Error MUST include attempted models
- FR-063: Error MUST suggest configuration fix
- FR-064: Error code MUST be specific

---

## Non-Functional Requirements

### Performance

- NFR-001: Routing decision MUST complete in < 10ms
- NFR-002: Availability check MUST timeout at 5s
- NFR-003: Cache MUST be used for repeated lookups
- NFR-004: Policy initialization MUST be < 100ms

### Reliability

- NFR-005: Invalid config MUST fail with clear message
- NFR-006: Unavailable model MUST trigger fallback
- NFR-007: All fallbacks exhausted MUST fail gracefully
- NFR-008: Policy MUST handle concurrent requests

### Security

- NFR-009: Model IDs MUST be validated
- NFR-010: No secrets in routing config
- NFR-011: Config parsing MUST be safe

### Observability

- NFR-012: All routing decisions MUST be logged
- NFR-013: Fallback events MUST be prominent
- NFR-014: Metrics SHOULD track routing distribution
- NFR-015: Health check SHOULD include routing status

### Maintainability

- NFR-016: Strategies MUST be pluggable
- NFR-017: New roles MUST be addable
- NFR-018: All public APIs MUST have XML docs
- NFR-019: Tests MUST cover all strategies

---

## User Manual Documentation

### Overview

Model routing determines which model handles each agent task. Different roles (planner, coder, reviewer) can use different models based on your configuration and available resources.

### Quick Start

```yaml
# .agent/config.yml - Simple single-model setup
models:
  routing:
    strategy: single
    default_model: llama3.2:70b
```

### Routing Strategies

#### Single Model Strategy

Uses one model for all roles. Simplest configuration.

```yaml
models:
  routing:
    strategy: single
    default_model: llama3.2:70b
```

#### Role-Based Strategy

Different models for different roles.

```yaml
models:
  routing:
    strategy: role-based
    default_model: llama3.2:7b
    role_models:
      planner: llama3.2:70b    # Reasoning needs large model
      coder: llama3.2:7b        # Implementation is simpler
      reviewer: llama3.2:70b    # Review needs analysis
```

### Configuration Reference

```yaml
models:
  routing:
    # Strategy: single, role-based, adaptive
    strategy: role-based
    
    # Default model for unassigned roles
    default_model: llama3.2:7b
    
    # Role-specific models (for role-based strategy)
    role_models:
      planner: llama3.2:70b
      coder: llama3.2:7b
      reviewer: llama3.2:70b
    
    # Fallback chain when primary unavailable
    fallback_chain:
      - llama3.2:70b
      - llama3.2:7b
      - mistral:7b
```

### Roles

| Role | Purpose | Typical Requirements |
|------|---------|---------------------|
| planner | Task decomposition | Strong reasoning |
| coder | Implementation | Instruction following |
| reviewer | Verification | Critical analysis |

### Fallback Behavior

When a configured model is unavailable, routing tries alternatives:

```
[INFO] Routing request for role 'planner'
[INFO] Checking primary model: llama3.2:70b
[WARN] Model llama3.2:70b unavailable, trying fallback
[INFO] Checking fallback: llama3.2:7b
[INFO] Selected model: llama3.2:7b (via fallback)
```

Configure fallback chain:

```yaml
models:
  routing:
    fallback_chain:
      - llama3.2:70b    # Try first
      - llama3.2:7b     # Then this
      - mistral:7b      # Last resort
```

### CLI Commands

```bash
# Show routing configuration
$ acode models routing
Routing Strategy: role-based
Default Model: llama3.2:7b

Role Assignments:
  planner  → llama3.2:70b (available)
  coder    → llama3.2:7b (available)
  reviewer → llama3.2:70b (available)

Fallback Chain:
  1. llama3.2:70b (available)
  2. llama3.2:7b (available)
  3. mistral:7b (not loaded)

# Test routing for specific role
$ acode models test planner
Testing routing for role 'planner'...
Primary model: llama3.2:70b
Status: Available
Selection: llama3.2:70b
```

### Operating Mode Integration

Routing respects Task 001 operating modes:

**local-only mode:**
- Only local models eligible
- Remote/cloud models rejected

**air-gapped mode:**
- No network access for models
- Must use pre-loaded local models

```yaml
# Mode set in config
operating_mode: local-only

models:
  routing:
    strategy: single
    default_model: llama3.2:7b  # Must be local
```

### Best Practices

1. **Start simple** - Use single model strategy first
2. **Large for planning** - Use big models for reasoning
3. **Fast for coding** - Smaller models often sufficient
4. **Configure fallbacks** - Ensure alternatives exist
5. **Test routing** - Verify before starting work

### Troubleshooting

#### No Available Model

```
Error: No available model for role 'coder'
  Tried: llama3.2:7b (unavailable), mistral:7b (unavailable)
  Suggestion: Start a model with 'ollama run <model>'
```

**Solution:** Start at least one model, or check fallback chain.

#### Invalid Model ID

```
Error: Invalid model ID 'llama-70b'
  Valid format: name:tag or name:tag@provider
```

**Solution:** Check model ID format.

#### Mode Constraint Violation

```
Error: Model 'gpt-4' not allowed in local-only mode
```

**Solution:** Use a local model or change operating mode.

---

## Acceptance Criteria

### Interface

- [ ] AC-001: IRoutingPolicy in Application layer
- [ ] AC-002: GetModel method exists
- [ ] AC-003: Returns ModelConfiguration
- [ ] AC-004: GetFallbackModel method exists
- [ ] AC-005: IsModelAvailable method exists
- [ ] AC-006: ListAvailableModels method exists

### Implementation

- [ ] AC-007: Infrastructure layer implementation
- [ ] AC-008: Reads from config file
- [ ] AC-009: Single model strategy works
- [ ] AC-010: Role-based strategy works
- [ ] AC-011: Respects operating mode
- [ ] AC-012: Logs decisions

### Roles

- [ ] AC-013: planner role defined
- [ ] AC-014: coder role defined
- [ ] AC-015: reviewer role defined
- [ ] AC-016: default role works

### Configuration

- [ ] AC-017: models.routing section
- [ ] AC-018: strategy field works
- [ ] AC-019: Default is "single"
- [ ] AC-020: default_model field works
- [ ] AC-021: role_models map works
- [ ] AC-022: fallback_chain works

### Single Strategy

- [ ] AC-023: Uses default_model
- [ ] AC-024: All roles same model
- [ ] AC-025: Fallback applies

### Role-Based Strategy

- [ ] AC-026: Reads role_models
- [ ] AC-027: Each role configurable
- [ ] AC-028: Missing uses default
- [ ] AC-029: Override works

### Fallback

- [ ] AC-030: Chain is ordered
- [ ] AC-031: Sequential traversal
- [ ] AC-032: First available selected
- [ ] AC-033: All unavailable fails
- [ ] AC-034: Fallback logged

### Mode Constraints

- [ ] AC-035: Checks operating mode
- [ ] AC-036: local-only enforced
- [ ] AC-037: air-gapped enforced
- [ ] AC-038: Violation fails

### Availability

- [ ] AC-039: Queries provider
- [ ] AC-040: Checks loaded state
- [ ] AC-041: 5s timeout
- [ ] AC-042: Triggers fallback

### CLI

- [ ] AC-043: routing command works
- [ ] AC-044: Shows strategy
- [ ] AC-045: Shows assignments
- [ ] AC-046: Shows chain
- [ ] AC-047: test command works

### Logging

- [ ] AC-048: Request logged
- [ ] AC-049: Selection logged
- [ ] AC-050: Reason logged
- [ ] AC-051: Fallback WARNING

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Application/Routing/
├── RoutingPolicyTests.cs
│   ├── Should_Return_Model_For_Role()
│   ├── Should_Use_Single_Strategy()
│   ├── Should_Use_RoleBased_Strategy()
│   ├── Should_Fallback_On_Unavailable()
│   └── Should_Respect_Mode_Constraints()
│
├── FallbackChainTests.cs
│   ├── Should_Traverse_Sequentially()
│   ├── Should_Select_First_Available()
│   └── Should_Fail_When_All_Unavailable()
│
└── RoleDefinitionTests.cs
    ├── Should_Define_Planner()
    ├── Should_Define_Coder()
    └── Should_Define_Reviewer()
```

### Integration Tests

```
Tests/Integration/Routing/
├── RoutingIntegrationTests.cs
│   ├── Should_Route_To_Available_Model()
│   ├── Should_Fallback_To_Alternative()
│   └── Should_Read_Config_Correctly()
```

### E2E Tests

```
Tests/E2E/Routing/
├── RoutingE2ETests.cs
│   ├── Should_Use_Configured_Models()
│   └── Should_Handle_Model_Restart()
```

### Performance Tests

- PERF-001: Routing decision < 10ms
- PERF-002: Availability check < 5s timeout
- PERF-003: Policy init < 100ms

---

## User Verification Steps

### Scenario 1: Single Model

1. Configure single strategy
2. Request routing for any role
3. Verify: Same model returned

### Scenario 2: Role-Based

1. Configure role-based strategy
2. Configure different models per role
3. Request routing for each role
4. Verify: Correct model per role

### Scenario 3: Fallback

1. Configure primary model
2. Stop primary model
3. Request routing
4. Verify: Fallback used

### Scenario 4: All Unavailable

1. Configure fallback chain
2. Stop all models
3. Request routing
4. Verify: Error with suggestions

### Scenario 5: Mode Constraint

1. Set local-only mode
2. Configure remote model
3. Request routing
4. Verify: Mode violation error

### Scenario 6: CLI Routing

1. Run `acode models routing`
2. Verify: Shows configuration
3. Verify: Shows availability

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/Routing/
├── IRoutingPolicy.cs
├── RoutingRequest.cs
├── RoutingDecision.cs
├── AgentRole.cs
└── RoutingConfiguration.cs

src/AgenticCoder.Infrastructure/Routing/
├── RoutingPolicy.cs
├── SingleModelStrategy.cs
├── RoleBasedStrategy.cs
├── FallbackHandler.cs
└── AvailabilityChecker.cs
```

### IRoutingPolicy Interface

```csharp
namespace AgenticCoder.Application.Routing;

public interface IRoutingPolicy
{
    RoutingDecision GetModel(AgentRole role, RoutingContext context);
    RoutingDecision GetFallbackModel(AgentRole role, RoutingContext context);
    bool IsModelAvailable(string modelId);
    IReadOnlyList<ModelInfo> ListAvailableModels();
}

public enum AgentRole
{
    Default,
    Planner,
    Coder,
    Reviewer
}

public sealed class RoutingDecision
{
    public required string ModelId { get; init; }
    public required bool IsFallback { get; init; }
    public string? FallbackReason { get; init; }
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-RTE-001 | No available model for role |
| ACODE-RTE-002 | Invalid model ID |
| ACODE-RTE-003 | Mode constraint violation |
| ACODE-RTE-004 | Fallback chain exhausted |
| ACODE-RTE-005 | Invalid routing configuration |

### Logging Fields

```json
{
  "event": "routing_decision",
  "role": "planner",
  "requested_model": "llama3.2:70b",
  "selected_model": "llama3.2:7b",
  "is_fallback": true,
  "fallback_reason": "primary_unavailable",
  "decision_time_ms": 5
}
```

### Implementation Checklist

1. [ ] Create IRoutingPolicy interface
2. [ ] Create AgentRole enum
3. [ ] Create RoutingDecision class
4. [ ] Create RoutingConfiguration class
5. [ ] Implement SingleModelStrategy
6. [ ] Implement RoleBasedStrategy
7. [ ] Implement FallbackHandler
8. [ ] Implement AvailabilityChecker
9. [ ] Implement RoutingPolicy
10. [ ] Add CLI commands
11. [ ] Write unit tests
12. [ ] Write integration tests
13. [ ] Add XML documentation

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Routing"
```

---

**End of Task 009 Specification**