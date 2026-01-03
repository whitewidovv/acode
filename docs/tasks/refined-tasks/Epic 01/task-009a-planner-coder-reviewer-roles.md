# Task 009.a: Planner/Coder/Reviewer Roles

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 009, Task 008 (Prompt Pack System), Task 004 (Model Provider Interface)  

---

## Description

Task 009.a defines the three core agent roles—Planner, Coder, and Reviewer—that structure the agentic workflow. Each role has distinct responsibilities, prompts, and behaviors that enable specialized handling of different phases in the coding process. Role separation enables better results by focusing model attention on specific tasks.

The Planner role handles task decomposition and strategy. When a user provides a high-level request ("add authentication to this API"), the Planner breaks it into actionable steps, identifies dependencies, and creates a work plan. The Planner focuses on the "what" and "why," not the implementation details. It produces a structured plan that the Coder executes.

The Coder role handles implementation. It takes tasks from the plan and produces actual code changes. The Coder focuses on correctness, following the strict minimal diff philosophy—making only necessary changes. It writes code, runs tests, and iterates until the task is complete. The Coder is the most active role, consuming most model inference time.

The Reviewer role handles verification and quality assurance. After the Coder makes changes, the Reviewer examines them for correctness, style compliance, potential bugs, and adherence to the original request. The Reviewer provides feedback—approving changes or requesting revisions. This creates a review loop that improves output quality.

Role transitions follow a defined workflow. A typical flow: User request → Planner creates plan → Coder implements step 1 → Reviewer verifies → Coder implements step 2 → ... → All steps complete → Final review. The orchestrator (covered in later tasks) manages these transitions. This task defines the roles themselves.

Each role has associated prompts from the Prompt Pack System (Task 008). The active pack provides role-specific prompts that shape model behavior. When the agent enters the Planner role, it uses the planner.md prompt. This prompt emphasizes reasoning, decomposition, and clear step definition. Role prompts are layered on top of the system prompt.

Role definitions include capability constraints. The Planner can analyze files but cannot modify them. The Coder can modify files but must work within the plan. The Reviewer can flag issues but cannot make changes directly. These constraints enforce separation of concerns and prevent role confusion.

Context management differs by role. The Planner needs broad context—project structure, existing patterns, architectural decisions. The Coder needs focused context—specific files, function signatures, test cases. The Reviewer needs both—the changes made and the relevant surrounding code. Role definitions include guidance on context assembly.

Role state is tracked throughout the session. The current role is logged with each action. Role transitions are explicit events. This visibility helps users understand what the agent is doing and why. It also enables debugging when behavior is unexpected.

The role system integrates with Model Routing (Task 009). Different roles can use different models based on configuration. A powerful model for planning (complex reasoning) and a lighter model for coding (straightforward tasks) optimizes resource usage while maintaining quality where it matters.

Role definitions are extensible. While three core roles ship by default, the system supports custom roles for specialized workflows. Users can define additional roles with custom prompts and capabilities. This extensibility enables adaptation to different development practices.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Agent Role | Mode of operation with specific focus |
| Planner Role | Task decomposition and planning |
| Coder Role | Implementation and code changes |
| Reviewer Role | Verification and quality assurance |
| Role Transition | Changing from one role to another |
| Role Prompt | Prompt specific to a role |
| Role Capability | What a role can do |
| Role Constraint | What a role cannot do |
| Role Context | Information available to a role |
| Workflow | Sequence of role transitions |
| Plan | Structured output from Planner |
| Task Step | Individual item in a plan |
| Review | Verification output from Reviewer |
| Approval | Positive review result |
| Revision Request | Negative review result |
| Role State | Current active role |
| Default Role | Fallback when unspecified |

---

## Out of Scope

The following items are explicitly excluded from Task 009.a:

- **Workflow orchestration** - Covered in later tasks
- **Role transition logic** - Covered in orchestrator
- **Model routing decisions** - Covered in Task 009
- **Prompt content** - Covered in Task 008.c
- **Model inference** - Covered in Task 004
- **Custom role creation** - Post-MVP
- **Role-based permissions** - Not in MVP
- **Role metrics/analytics** - Post-MVP
- **Multi-agent roles** - Not in MVP
- **Role inheritance** - Post-MVP

---

## Functional Requirements

### AgentRole Enum

- FR-001: AgentRole enum MUST be in Domain layer
- FR-002: MUST include Planner value
- FR-003: MUST include Coder value
- FR-004: MUST include Reviewer value
- FR-005: MUST include Default value
- FR-006: Enum values MUST have string representations
- FR-007: Unknown values MUST resolve to Default

### RoleDefinition Class

- FR-008: RoleDefinition MUST be in Domain layer
- FR-009: MUST have Role property (AgentRole)
- FR-010: MUST have Name property (display name)
- FR-011: MUST have Description property
- FR-012: MUST have Capabilities property (list)
- FR-013: MUST have Constraints property (list)
- FR-014: MUST have PromptKey property (role prompt ID)

### IRoleRegistry Interface

- FR-015: Interface MUST be in Application layer
- FR-016: MUST have GetRole(AgentRole) method
- FR-017: MUST have ListRoles() method
- FR-018: MUST have GetCurrentRole() method
- FR-019: MUST have SetCurrentRole(AgentRole) method

### Planner Role Definition

- FR-020: Role name MUST be "Planner"
- FR-021: Description MUST explain decomposition focus
- FR-022: Capabilities MUST include: analyze files, read context
- FR-023: Capabilities MUST include: create plan, identify dependencies
- FR-024: Constraints MUST include: cannot modify files
- FR-025: Constraints MUST include: cannot execute code
- FR-026: PromptKey MUST be "planner"

### Coder Role Definition

- FR-027: Role name MUST be "Coder"
- FR-028: Description MUST explain implementation focus
- FR-029: Capabilities MUST include: read files, write files
- FR-030: Capabilities MUST include: execute commands, run tests
- FR-031: Capabilities MUST include: create files, delete files
- FR-032: Constraints MUST include: must follow plan
- FR-033: Constraints MUST include: minimal diff only
- FR-034: PromptKey MUST be "coder"

### Reviewer Role Definition

- FR-035: Role name MUST be "Reviewer"
- FR-036: Description MUST explain verification focus
- FR-037: Capabilities MUST include: analyze changes, read context
- FR-038: Capabilities MUST include: provide feedback, approve/reject
- FR-039: Constraints MUST include: cannot modify files
- FR-040: Constraints MUST include: cannot execute code
- FR-041: PromptKey MUST be "reviewer"

### Default Role Definition

- FR-042: Role name MUST be "Default"
- FR-043: Description MUST explain general purpose
- FR-044: Capabilities MUST include all capabilities
- FR-045: Constraints MUST be minimal
- FR-046: PromptKey MUST be "system"

### Role State Management

- FR-047: Current role MUST be tracked
- FR-048: Initial role MUST be Default
- FR-049: Role changes MUST be explicit
- FR-050: Role MUST persist within session
- FR-051: Role MUST be logged on change
- FR-052: Role MUST be queryable at any time

### Role Context Assembly

- FR-053: Each role MUST have context strategy
- FR-054: Planner context MUST be broad
- FR-055: Coder context MUST be focused
- FR-056: Reviewer context MUST include changes
- FR-057: Context MUST include role constraints

### Role Prompt Integration

- FR-058: Role MUST reference prompt pack component
- FR-059: Role prompt MUST be loaded from active pack
- FR-060: Missing prompt MUST fall back to system
- FR-061: Prompt MUST include role constraints

### CLI Integration

- FR-062: `acode roles list` MUST show all roles
- FR-063: MUST show name, description, capabilities
- FR-064: `acode roles show <role>` MUST show details
- FR-065: MUST show full constraints list
- FR-066: `acode status` MUST show current role

### Logging

- FR-067: Role transitions MUST be logged
- FR-068: Log MUST include from-role and to-role
- FR-069: Log MUST include transition reason
- FR-070: Current role MUST be in all log entries

---

## Non-Functional Requirements

### Performance

- NFR-001: Role lookup MUST complete in < 1ms
- NFR-002: Role transition MUST complete in < 10ms
- NFR-003: Role state MUST be cached

### Reliability

- NFR-004: Invalid role MUST resolve to Default
- NFR-005: Role state MUST survive within session
- NFR-006: Concurrent access MUST be thread-safe

### Security

- NFR-007: Role constraints MUST be enforced
- NFR-008: Role escalation MUST be prevented
- NFR-009: Role state MUST not be user-modifiable

### Observability

- NFR-010: Role transitions MUST be logged
- NFR-011: Role metrics SHOULD be tracked
- NFR-012: Current role MUST be visible

### Maintainability

- NFR-013: Role definitions MUST be data-driven
- NFR-014: New roles MUST be addable without code changes
- NFR-015: All public APIs MUST have XML docs

---

## User Manual Documentation

### Overview

Agent roles structure the workflow by specializing behavior for different phases. The three core roles—Planner, Coder, and Reviewer—enable focused, high-quality output.

### Core Roles

#### Planner

**Purpose:** Decompose tasks into actionable steps.

**Capabilities:**
- Analyze project structure and files
- Read context and understand requirements
- Create structured plans with dependencies
- Identify risks and considerations

**Constraints:**
- Cannot modify files
- Cannot execute commands
- Must focus on planning, not implementation

**When active:** At the start of a task, breaking down the request.

#### Coder

**Purpose:** Implement code changes according to plan.

**Capabilities:**
- Read and write files
- Execute terminal commands
- Run tests and verify results
- Create and delete files

**Constraints:**
- Must follow the established plan
- Must apply strict minimal diff
- Must not make unrelated changes

**When active:** During implementation of plan steps.

#### Reviewer

**Purpose:** Verify changes and provide feedback.

**Capabilities:**
- Analyze code changes
- Compare against requirements
- Identify bugs and issues
- Provide constructive feedback

**Constraints:**
- Cannot modify files
- Cannot execute commands
- Must be constructive and specific

**When active:** After implementation, before finalizing.

### Workflow Example

```
User: "Add input validation to the login form"

[Planner Role]
Breaking down request:
1. Identify login form component
2. Determine validation requirements
3. Create validation functions
4. Integrate with form

[Coder Role - Step 1]
Reading login form at src/components/Login.tsx...

[Coder Role - Step 2]
Creating validation schema...

[Coder Role - Step 3]
Integrating validation...

[Reviewer Role]
Reviewing changes:
✓ Validation added to email field
✓ Validation added to password field
✓ Error messages displayed correctly
✓ Form submission blocked when invalid
Approved.
```

### CLI Commands

```bash
# List all roles
$ acode roles list
┌──────────┬──────────────────────────────────────────┐
│ Role     │ Description                               │
├──────────┼──────────────────────────────────────────┤
│ Planner  │ Task decomposition and planning          │
│ Coder    │ Implementation and code changes          │
│ Reviewer │ Verification and quality assurance       │
│ Default  │ General purpose operation                │
└──────────┴──────────────────────────────────────────┘

# Show role details
$ acode roles show planner
Role: Planner
Description: Task decomposition and planning

Capabilities:
  - Analyze files and project structure
  - Read context and requirements
  - Create structured plans
  - Identify dependencies

Constraints:
  - Cannot modify files
  - Cannot execute commands

# Check current role
$ acode status
Current Role: Coder
Active Plan: 3/5 steps complete
```

### Configuration

Role behavior is configured via prompt packs:

```yaml
# .agent/config.yml
prompts:
  pack_id: acode-standard  # Includes role prompts
```

Role-specific models (optional):

```yaml
models:
  routing:
    strategy: role-based
    role_models:
      planner: llama3.2:70b   # Complex reasoning
      coder: llama3.2:7b      # Faster execution
      reviewer: llama3.2:70b  # Thorough analysis
```

### Role Transitions

Roles transition automatically based on workflow:

1. **User request** → Planner (decompose task)
2. **Plan created** → Coder (implement step)
3. **Step complete** → Reviewer (verify)
4. **Approved** → Coder (next step) or Done
5. **Rejected** → Coder (revise)

### Troubleshooting

#### Role Not Changing

The agent stays in one role unexpectedly.

**Cause:** Workflow state not advancing.  
**Solution:** Check for errors, verify plan is complete.

#### Wrong Role Behavior

The agent acts outside its role constraints.

**Cause:** Prompt not enforcing constraints.  
**Solution:** Verify prompt pack has proper role prompts.

---

## Acceptance Criteria

### Enum

- [ ] AC-001: AgentRole in Domain
- [ ] AC-002: Planner value exists
- [ ] AC-003: Coder value exists
- [ ] AC-004: Reviewer value exists
- [ ] AC-005: Default value exists
- [ ] AC-006: String representations work
- [ ] AC-007: Unknown = Default

### RoleDefinition

- [ ] AC-008: RoleDefinition in Domain
- [ ] AC-009: Role property exists
- [ ] AC-010: Name property exists
- [ ] AC-011: Description property exists
- [ ] AC-012: Capabilities property exists
- [ ] AC-013: Constraints property exists
- [ ] AC-014: PromptKey property exists

### Registry

- [ ] AC-015: IRoleRegistry in Application
- [ ] AC-016: GetRole method exists
- [ ] AC-017: ListRoles method exists
- [ ] AC-018: GetCurrentRole method exists
- [ ] AC-019: SetCurrentRole method exists

### Planner Definition

- [ ] AC-020: Name is "Planner"
- [ ] AC-021: Capabilities include analyze
- [ ] AC-022: Capabilities include create plan
- [ ] AC-023: Constraints include no modify
- [ ] AC-024: PromptKey is "planner"

### Coder Definition

- [ ] AC-025: Name is "Coder"
- [ ] AC-026: Capabilities include write files
- [ ] AC-027: Capabilities include execute
- [ ] AC-028: Constraints include follow plan
- [ ] AC-029: PromptKey is "coder"

### Reviewer Definition

- [ ] AC-030: Name is "Reviewer"
- [ ] AC-031: Capabilities include analyze changes
- [ ] AC-032: Capabilities include provide feedback
- [ ] AC-033: Constraints include no modify
- [ ] AC-034: PromptKey is "reviewer"

### State Management

- [ ] AC-035: Current role tracked
- [ ] AC-036: Initial is Default
- [ ] AC-037: Changes explicit
- [ ] AC-038: Persists in session
- [ ] AC-039: Logged on change
- [ ] AC-040: Queryable

### CLI

- [ ] AC-041: list command works
- [ ] AC-042: Shows all roles
- [ ] AC-043: show command works
- [ ] AC-044: Shows details
- [ ] AC-045: status shows role

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Domain/Roles/
├── AgentRoleTests.cs
│   ├── Should_Define_All_Roles()
│   ├── Should_Convert_To_String()
│   └── Should_Parse_From_String()
│
├── RoleDefinitionTests.cs
│   ├── Should_Define_Planner()
│   ├── Should_Define_Coder()
│   ├── Should_Define_Reviewer()
│   └── Should_Define_Default()
│
└── RoleRegistryTests.cs
    ├── Should_Get_Role_By_Enum()
    ├── Should_List_All_Roles()
    ├── Should_Track_Current_Role()
    └── Should_Transition_Role()
```

### Integration Tests

```
Tests/Integration/Roles/
├── RoleIntegrationTests.cs
│   ├── Should_Load_Role_Prompts()
│   └── Should_Apply_Role_Context()
```

### E2E Tests

```
Tests/E2E/Roles/
├── RoleE2ETests.cs
│   ├── Should_Transition_Through_Workflow()
│   └── Should_Enforce_Constraints()
```

---

## User Verification Steps

### Scenario 1: List Roles

1. Run `acode roles list`
2. Verify: All four roles shown
3. Verify: Descriptions accurate

### Scenario 2: Show Role Details

1. Run `acode roles show planner`
2. Verify: Full capabilities listed
3. Verify: Constraints shown

### Scenario 3: Check Current Role

1. Start agent session
2. Run `acode status`
3. Verify: Current role displayed

### Scenario 4: Role Transition

1. Submit task
2. Observe role changes
3. Verify: Planner → Coder → Reviewer

### Scenario 5: Constraint Enforcement

1. In Planner role
2. Attempt file modification
3. Verify: Blocked with explanation

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/Roles/
├── AgentRole.cs
└── RoleDefinition.cs

src/AgenticCoder.Application/Roles/
├── IRoleRegistry.cs
└── RoleContext.cs

src/AgenticCoder.Infrastructure/Roles/
├── RoleRegistry.cs
├── RoleDefinitionProvider.cs
└── RoleStateTracker.cs
```

### AgentRole Enum

```csharp
namespace AgenticCoder.Domain.Roles;

public enum AgentRole
{
    Default = 0,
    Planner = 1,
    Coder = 2,
    Reviewer = 3
}

public static class AgentRoleExtensions
{
    public static string ToDisplayString(this AgentRole role) => role switch
    {
        AgentRole.Default => "Default",
        AgentRole.Planner => "Planner",
        AgentRole.Coder => "Coder",
        AgentRole.Reviewer => "Reviewer",
        _ => "Default"
    };
}
```

### RoleDefinition Class

```csharp
namespace AgenticCoder.Domain.Roles;

public sealed class RoleDefinition
{
    public required AgentRole Role { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> Capabilities { get; init; }
    public required IReadOnlyList<string> Constraints { get; init; }
    public required string PromptKey { get; init; }
}
```

### IRoleRegistry Interface

```csharp
namespace AgenticCoder.Application.Roles;

public interface IRoleRegistry
{
    RoleDefinition GetRole(AgentRole role);
    IReadOnlyList<RoleDefinition> ListRoles();
    AgentRole GetCurrentRole();
    void SetCurrentRole(AgentRole role);
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-ROL-001 | Invalid role specified |
| ACODE-ROL-002 | Role transition not allowed |
| ACODE-ROL-003 | Role constraint violation |

### Logging Fields

```json
{
  "event": "role_transition",
  "from_role": "Planner",
  "to_role": "Coder",
  "reason": "plan_complete",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Implementation Checklist

1. [ ] Create AgentRole enum
2. [ ] Create RoleDefinition class
3. [ ] Create IRoleRegistry interface
4. [ ] Define Planner role
5. [ ] Define Coder role
6. [ ] Define Reviewer role
7. [ ] Define Default role
8. [ ] Implement RoleRegistry
9. [ ] Implement RoleStateTracker
10. [ ] Add CLI commands
11. [ ] Write unit tests
12. [ ] Add XML documentation

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Roles"
```

---

**End of Task 009.a Specification**