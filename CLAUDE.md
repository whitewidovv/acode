# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the **Agentic Coding Bot (Acode)** project - a locally-hosted, privacy-first AI-powered coding assistant. The repository contains both comprehensive task specifications and the implementation codebase for building a system that operates entirely within the user's infrastructure.


## 🚨 CRITICAL: TASK SPECIFICATION EXPANSION REQUIREMENTS 🚨

**IMPERATIVE - NO SHORTCUTS, NO STREAMLINING, NO EXCEPTIONS**

When expanding task specifications from stubs into comprehensive specifications:

### Rule #1: Complete Every Section Fully (1200 Lines Minimum)

Each refined task MUST include ALL 16 sections with NO abbreviations:

1. **Header** - Priority, Tier, Complexity, Phase, Dependencies (complete)
2. **Description** (300+ lines) - Business value with ROI calculations, technical approach with architectural decisions, integration points with specific systems, constraints and limitations, trade-offs explained
3. **Use Cases** (3+ scenarios, 10-15 lines each) - Real personas with names/roles, before/after workflow comparisons, concrete metrics showing improvement
4. **Glossary** (10-20 terms) - All domain-specific terms, technical jargon, acronyms defined with clear explanations
5. **Out of Scope** (8-15 items) - Explicit list of what is NOT included in this task, boundaries clearly defined
6. **Functional Requirements** (50-100+ items) - All functional capabilities listed as FR-001, FR-002, etc. with testable statements
7. **Non-Functional Requirements** (15-30 items) - Performance, security, scalability, maintainability requirements as NFR-001, etc.
8. **User Manual Documentation** (200-400 lines) - Complete guide with step-by-step instructions, ASCII mockups, configuration examples, best practices, troubleshooting
9. **Assumptions** (15-20 items) - Technical assumptions, operational assumptions, integration assumptions explicitly stated
10. **Security Considerations** (5+ threats) - Each threat with risk description, attack scenario, complete mitigation code (not snippets)
11. **Best Practices** (12-20 items) - Organized by category, specific actionable guidance
12. **Troubleshooting** (5+ issues) - Each with Symptoms, Causes, Solutions format including code/commands
13. **Acceptance Criteria** (50-80+ items) - Comprehensive testable checklist across all functional areas
14. **Testing Requirements** (complete test code, 200-400 lines) - Full C# test implementations with Arrange-Act-Assert, realistic test data, all test types
15. **User Verification Steps** (8-10 scenarios, 100-150 lines) - Detailed step-by-step manual testing with complete commands and expected outputs
16. **Implementation Prompt for Claude** (400-600 lines) - Complete code for all entities, services, controllers with full implementations (not stubs)

**Target Length:** 1200 lines MINIMUM for subtasks, 1500+ for parent tasks. Golden standard example (e-commerce) is 3,699 lines. The minimum is a FLOOR, not a target. Large files must be read and written in chunks to avoid token limits.

These are the quality standards. Match or exceed them.

### Rule #2: No Abbreviations or Placeholders

**❌ NEVER DO THIS:**
- "... (additional tests omitted for brevity)"
- "... (see Task 001 for pattern)"
- "... (similar to above)"
- "[Sections 6-10 follow same pattern as Section 5]"
- "**END OF TASK (Abbreviated for space)**"
- "... focusing on essentials only"
- "... streamlined for token efficiency"
- "// ... additional code here"
- "<!-- More examples follow same pattern -->"

**✅ ALWAYS DO THIS:**
- Write every test case name expected to see in final implementation
- Write every use case with full detail (10-15 lines each minimum)
- Write every verification step with complete instructions
- Write every code example in full (not snippets or "...")
- Include ALL 50+ acceptance criteria items individually

### Rule #3: Work Section-by-Section to Avoid Loss

To prevent losing work due to token limits, write and save incrementally:

1. Write **Description** section (300+ lines) → Save to file
2. Write **Use Cases** section (3+ scenarios) → Append to file
3. Write **Glossary** section (10-20 terms) → Append to file
4. Write **Out of Scope** section (8-15 items) → Append to file
5. Write **Functional Requirements** section (50-100+ items) → Append to file
6. Write **Non-Functional Requirements** section (15-30 items) → Append to file
7. Write **User Manual** section (200-400 lines) → Append to file
8. Write **Assumptions** section (15-20 items) → Append to file
9. Write **Security Considerations** section (5+ threats with code) → Append to file
10. Write **Best Practices** section (12-20 items) → Append to file
11. Write **Troubleshooting** section (5+ issues) → Append to file
12. Write **Acceptance Criteria** section (50-80+ items) → Append to file
13. Write **Testing Requirements** section (200-400 lines complete code) → Append to file
14. Write **User Verification** section (8-10 scenarios) → Append to file
15. Write **Implementation Prompt** section (400-600 lines complete code) → Append to file
16. **Verify semantic completeness** → Check each section has required depth, not just presence, reattempt expansion of a section if needed
17. **Verify line count** → Run line count check (must be >= 1500)
18. **Mark task as completed** → Update tracking file

If interrupted mid-task, resume from last completed section.

### Why This Is Absolutely Critical

**Comprehensive task specifications are the blueprint for implementation.** Cutting corners now means:

1. **Bugs and Missing Features**: If validation rules aren't documented, they won't be implemented. If edge cases aren't listed in acceptance criteria, they won't be tested. Shortcuts in specs = bugs in production.

2. **Developer Confusion**: Another Claude instance (or human developer) implementing this task months later needs COMPLETE guidance. "See above for pattern" doesn't help when "above" is in a different context window.

3. **Testing Gaps**: Incomplete test sections mean features ship without coverage. "Additional tests omitted" = untested code paths = production failures.

4. **Technical Debt**: Brief specs force developers to make assumptions. Different assumptions = inconsistent implementation. Comprehensive specs prevent this.

5. **Lost Knowledge**: Detailed specs serve as evergreen documentation. Future developers (6 months, 2 years later) can understand design decisions. Brief specs lose context.

6. **Client/Stakeholder Trust**: The user requested comprehensive specs for a reason - they want quality, completeness, predictability. Delivering abbreviated specs breaks trust.

7. **Cost of Rework**: Fixing unclear requirements during implementation costs 10-100x more than getting specs right upfront. An extra 30 minutes writing complete acceptance criteria saves 5 hours of debugging later.

**ROI Calculation:**
- Time to write complete spec: +30 minutes per task
- Time saved in implementation: -5 hours of confusion, rework, bug fixes
- Net savings: 4.5 hours per task × 46 tasks = 207 hours saved
- At $100/hour developer rate: **$20,700 project cost savings**

### Enforcement

**Automated Checks** (run before marking task complete):
```bash
# Check line count (must be >= 1000)
wc -l /completed/task-[XXX]-[name].md

# Verify all sections exist
grep -E "^## (Description|Use Cases|User Manual|Acceptance Criteria|Testing Requirements|User Verification|Implementation Prompt)" /completed/task-[XXX]-[name].md

# Count acceptance criteria (should be 50-80+)
grep -E "^- \[ \]" /completed/task-[XXX]-[name].md | wc -l
```

**Quality Review Checklist:**
- [ ] Line count >= 1000 (preferably 1500-2000)
- [ ] All 8 sections present with substantive content
- [ ] No "see above" or abbreviation phrases found
- [ ] All expected test cases are listed
- [ ] Each use case is 10-15+ lines
- [ ] Acceptance criteria count is 50-80+ items
- [ ] Implementation prompt has 12+ steps with complete code

**No Exceptions**: Even for "simple" tasks - simplicity in implementation still requires comprehensive documentation for maintenance, testing, and future enhancement.

**Remember:** You are not being judged on speed or token efficiency. You are being judged on completeness, accuracy, and enabling future success. Take the time to do it right the first time.

## CRITICAL: Autonomous Work and Asynchronous Communication

**MOST IMPORTANT PRINCIPLE: Work autonomously until context runs dangerously low (<5k tokens remaining).**

### DO NOT Stop to Report Progress

- **DO NOT** stop work to give status updates, milestone announcements, or progress reports
- **DO NOT** ask for permission to continue after each commit
- **DO NOT** wait for user acknowledgment between subtasks
- **DO NOT** waste tokens on progress summaries mid-session

### Instead: Work Continuously and Update Documentation

When you feel the need to announce progress:
1. **Update `docs/PROGRESS_NOTES.md`** with your milestone (user reads asynchronously)
2. **Update implementation plan** (`docs/implementation-plans/task-XXX-plan.md`) with progress
3. **Continue working** on the next subtask immediately

### Only Stop When:

1. **Context is dangerously low** (<5k tokens remaining)
2. **Task is fully complete** (all subtasks done, audit passed, PR created)
3. **You encounter a blocker** requiring user decision

### When Stopping (Low Context Only):

1. Update `docs/implementation-plans/task-XXX-plan.md` with detailed progress
2. Update `docs/PROGRESS_NOTES.md` with summary
3. Commit all work with descriptive message
4. Push to feature branch
5. Report: "Context low. X tokens remaining. Updated implementation plan. Ready to resume in next session."

### Efficient Token Usage

- Progress tracking happens in files (implementation plans, PROGRESS_NOTES.md)
- User monitors progress asynchronously by reading these files
- Stopping mid-flow to report wastes tokens and breaks momentum
- Autonomous work until completion or <5k tokens is the expected behavior

## Core Working Principles

**CRITICAL: Read these principles before starting ANY work.**

### 1. Perfection and Completeness Over Speed

- **DO NOT rush to complete tasks**
- **DO NOT sacrifice quality to "get it done"**
- **DO NOT skip steps to save context**
- It is **acceptable and expected** to run out of context and continue in the next session
- It is **unacceptable** to deliver incomplete, untested, or poorly integrated code

**Example of What NOT to Do (Task 002 Failure):**
- Implemented Infrastructure layer (YamlConfigReader, JsonSchemaValidator) without writing tests first
- Rushed through implementation to "complete" the task
- Audit failed to catch TDD violations and integration issues
- Resulted in Copilot finding multiple critical issues post-PR

**Example of What TO Do:**
- Write tests FIRST (Red)
- Implement code to make tests pass (Green)
- Refactor (Clean)
- Audit thoroughly using `docs/AUDIT-GUIDELINES.md`
- Only mark task complete when audit passes

### 2. Test-Driven Development (TDD) is MANDATORY

- **Every source file must have corresponding tests**
- **Write tests BEFORE implementation** (Red-Green-Refactor)
- **No exceptions** - even simple getters/setters need tests for immutability verification
- If you find yourself implementing without tests, STOP and write tests first

**Test File Naming Convention:**
- `src/Acode.Domain/Foo/Bar.cs` → `tests/Acode.Domain.Tests/Foo/BarTests.cs`
- `src/Acode.Application/Foo/Bar.cs` → `tests/Acode.Application.Tests/Foo/BarTests.cs`
- `src/Acode.Infrastructure/Foo/Bar.cs` → `tests/Acode.Infrastructure.Tests/Foo/BarTests.cs`

### 3. Subtask Completion is MANDATORY (Hard Rule)

**CRITICAL: A task is NOT complete until ALL subtasks are complete.**

Before marking any task complete, auditing, or creating a PR:

1. **Run subtask discovery**: `find docs/tasks/refined-tasks -name "task-XXX*.md"` (replace XXX with task number)
2. **List ALL subtasks found**: task-XXXa.md, task-XXXb.md, task-XXXc.md, etc.
3. **Verify EACH subtask is complete**:
   - Implementation exists with file paths
   - Tests written and passing
   - Commit hash recorded
4. **If ANY subtask is incomplete**: STOP - task XXX is NOT complete
5. **Continue implementing incomplete subtasks** - do NOT skip ahead to audit/PR

**There are NO exceptions to this rule.**

### When a Subtask Cannot Be Completed

If you discover a subtask literally cannot be completed (e.g., task-006b requires widget Y which doesn't exist until task-007):

1. **STOP work immediately**
2. **Explain the blocking dependency to the user**:
   - "Task XXXb cannot be completed because it requires [dependency]"
   - "This dependency is delivered in Task YYY"
   - "Subtask XXXb should be moved to Task YYY or a new task"
3. **Wait for user approval** to modify the task specification
4. **Update the task specification files** to reflect the agreed change
5. **Only then continue** with the modified scope

**NEVER self-approve subtask deferrals.** The user decides task scope, not you.

### 4. Mandatory Audit Before PR

- **DO NOT create PR without passing audit**
- **DO NOT audit until ALL subtasks are verified complete** (see Section 3)
- Follow `docs/AUDIT-GUIDELINES.md` checklist line-by-line
- Audit failure = task is NOT complete
- Fix all issues, then re-audit from step 1
- Only create PR when audit passes

**Audit Failure Criteria (automatic task incomplete):**
- Any subtask incomplete (task-XXXa, task-XXXb, etc.)
- Any source file without tests
- Build has errors or warnings
- Any test fails
- Interface exists but no implementation (e.g., NotImplementedException)
- Layer boundaries violated
- Documentation missing

### 4. Parent Tasks and Subtasks: Task Completion Logic

**CRITICAL RULE:** A parent task is **NOT COMPLETE** until **ALL** its subtasks are complete.

#### Task Naming Convention
- Parent tasks: `task-XXX-description.md` (e.g., `task-003-threat-model-default-safety-posture.md`)
- Subtasks: `task-XXXa-description.md`, `task-XXXb-description.md`, `task-XXXc-description.md`, etc.

#### Completion Logic

**Example Scenario:**
- Task 003: Threat Model & Default Safety Posture (parent)
  - Task 003a: Enumerate Risk Categories + Mitigations (subtask)
  - Task 003b: Define Default Denylist + Protected Paths (subtask)
  - Task 003c: Define Audit Baseline Requirements (subtask)

**WRONG Completion Claim:**
- "Task 003 is complete! I implemented the threat model framework."
- **Problem:** Task 003 has subtasks (003a, 003b, 003c) that are not complete

**CORRECT Completion Claim:**
- "Task 003 parent framework is complete. Now implementing subtask 003a..."
- "Task 003a is complete. Now implementing subtask 003b..."
- "Task 003b is complete. Now implementing subtask 003c..."
- "Task 003c is complete. **All subtasks done, therefore Task 003 is now fully complete.**"

#### How to Identify Subtasks

**Before claiming task complete, ALWAYS check:**
1. Look for files matching `task-XXXa-*.md`, `task-XXXb-*.md`, etc. in the same directory
2. Example: `find docs/tasks/refined-tasks -name "task-003*.md"` will show all Task 003 related files
3. If subtasks exist, parent task is NOT complete until ALL subtasks are done

#### Audit Check for Subtasks

**Audit checklist MUST include:**
- [ ] Check for subtask files: `task-XXXa`, `task-XXXb`, `task-XXXc`, etc.
- [ ] If subtasks exist, verify ALL subtasks are complete
- [ ] If any subtask is incomplete, parent task is incomplete
- [ ] Only mark parent task complete when all subtasks pass audit

#### Communication with User

**When subtasks exist, be explicit:**
- "Task 003 has 3 subtasks (003a, 003b, 003c). I've completed the parent framework. Now working on subtask 003a..."
- **DO NOT** claim "Task 003 complete" until ALL subtasks are done
- **DO NOT** hide the existence of subtasks from the user
- **DO NOT** assume subtasks are "future work" unless explicitly stated in spec

### 5. Git Workflow

**Key Constraint:** All work MUST use feature branches with one commit per task objective, and create a PR when the task is complete. DO NOT commit directly to main.

## Repository Structure

```
docs/
├── scripts/                                    # Python scripts for generating task stubs
│   ├── generate-acode-task-stubs.py           # Main task stub generator
│   ├── generate-refinable-tasks-acode-v2.py   # Refined task generator
│   └── task-stub-template-acode.md            # Template for task stubs
├── tasks/
│   ├── task-list.md                           # Master task list (49+ tasks across 12+ epics)
│   ├── refined-tasks/Epic 00/                 # Completed, refined task specifications
│   └── task-stubs-refinable/                  # Task stubs awaiting expansion
```

## Epic Structure

The project is organized into 13 major epics:

- **Epic 0** - Product Definition, Constraints, Repo Contracts (Foundation)
- **Epic 1** - Model Runtime, Inference, Tool-Calling Contract
- **Epic 2** - CLI + Agent Orchestration Core
- **Epic 3** - Repo Intelligence (Indexing, Retrieval, Context Packing)
- **Epic 4** - Execution & Sandboxing
- **Epic 5** - Git Automation + Worktrees
- **Epic 6** - Task Queue + Parallel Worker System
- **Epic 7** - Cloud Burst Compute
- **Epic 8** - CI/CD Authoring + Deployment Hooks
- **Epic 9** - Safety, Policy Engine, Secrets Hygiene, Audit
- **Epic 10** - Reliability, Resumability, Deterministic Runs
- **Epic 11** - Performance + Scaling
- **Epic 12** - Evaluation Suite + Regression Gates

## Task Organization System

### Naming Convention
- Parent tasks: `task-XXX-description.md` (e.g., `task-000-project-bootstrap-solution-structure.md`)
- Subtasks: `task-XXXa-description.md`, `task-XXXb-description.md`, etc.

### Task States
1. **Stubs** - Template-based tasks needing full expansion (in `task-stubs-refinable/`)
2. **Refined** - Fully specified tasks ready for implementation (in `refined-tasks/`)

### Task Template Structure
Each refined task MUST include:
1. Header (Priority, Tier, Complexity, Phase, Dependencies)
2. Description (3-6 paragraphs covering business value, technical details, scope)
3. Use Cases (3 scenarios using personas: Neil, DevBot, Jordan)
4. User Manual Documentation (150-300 lines)
5. Acceptance Criteria / Definition of Done (40-80 items)
6. Testing Requirements (Unit, Integration, E2E, Performance, Regression)
7. User Verification Steps (8-10 manual scenarios)
8. Implementation Prompt for Claude (100-250 lines minimum)

## Key Architectural Principles

### Clean Architecture (for future .NET implementation)
The planned implementation follows Clean Architecture layers:
- **Domain** - Pure business logic, no dependencies
- **Application** - Use cases, depends only on Domain
- **Infrastructure** - External integrations, implements Domain/Application interfaces
- **CLI** - Command-line interface, entry point

### Operating Modes
Three core operating modes define the safety model:
- **LocalOnly** - No network, local models only
- **Burst** - Cloud compute allowed, NO external LLM APIs
- **Airgapped** - Complete network isolation

### Safety-First Design
- Opt-out safety (not opt-in)
- Default denylists for sensitive paths
- Audit logging required for all operations
- No external LLM API calls by default

## Working with Task Specifications



### Creating New Tasks
If creating new tasks:
1. Follow the numbering scheme (parent tasks = XXX, subtasks = XXXa, XXXb, etc.)
2. Use the template from `docs/scripts/task-stub-template-acode.md`
3. Place in appropriate Epic folder
4. Update `docs/tasks/task-list.md` with the new task

## Common Commands

### Generate Task Stubs
```bash
cd docs/scripts
python3 generate-acode-task-stubs.py
```

### Find Tasks
```bash
# List all refined tasks
find docs/tasks/refined-tasks -name "*.md" -type f

# Count tasks per epic
find docs/tasks/refined-tasks -type d -name "Epic*" -exec sh -c 'echo "$1: $(find "$1" -name "*.md" | wc -l)"' _ {} \;

# Find specific task
grep -r "task-XXX" docs/tasks/
```

## Technology Stack (Planned)

The future implementation will use:
- **.NET 8.0+** - Primary runtime
- **C#** - Application code
- **xUnit, FluentAssertions, NSubstitute** - Testing
- **Ollama/vLLM** - Local model serving
- **SQLite** - Persistence
- **Docker** - Sandboxing
- **Git** - Version control automation

## Important Constraints

### What Acode Will NOT Do
- NO external LLM API calls (OpenAI, Anthropic, etc.) in default mode
- NO modification of `.git/`, `.env`, credential files
- NO network access in LocalOnly/Airgapped modes
- NO bypassing of safety checks

### Current Repository Contents
- Task specifications and planning documents
- Python scripts for task generation
- Epic definitions and constraints
- Architecture documentation
- Implementation codebase (being built iteratively per task specifications)

## Workflow for Claude Code

### When Asked to Work on Documentation
1. Create a feature branch: `git checkout -b feature/task-XXX-description`
2. Make changes (one logical change per commit)
3. Commit with descriptive message
4. Create PR (do NOT merge directly to main)

### When Asked to Expand a Task Stub
1. Read the existing stub thoroughly
2. Follow the INSTRUCTIONS section requirements exactly
3. Ensure all 8 sections are comprehensive
4. Validate against the quality checklist
5. Mark file as refined by removing "(NEEDS-REFINEMENT)" from filename

### When Asked About Project Structure
- Refer to Epic 0 documents for foundational architecture
- Refer to task-list.md for complete task inventory
- Each epic's `epic-X-*.md` file contains epic-level context

## Reference Documents

Key files to reference:
- `docs/tasks/task-list.md` - Complete task list
- `docs/tasks/refined-tasks/Epic 00/epic-0-product-definition-constraints-repo-contracts.md` - Foundation epic
- `docs/tasks/refined-tasks/Epic 00/task-000-project-bootstrap-solution-structure.md` - Example of fully refined task
- `docs/scripts/task-stub-template-acode.md` - Template for task structure

## Quality Standards

All task specifications must:
- Be objectively measurable in acceptance criteria
- Include concrete implementation guidance with file paths
- Respect layer boundaries (Domain → Application → Infrastructure → CLI)
- Consider safety, audit, and policy implications
- Include comprehensive testing requirements (all 5 types)
- Provide user-facing verification steps

## Implementation Approach

Tasks are implemented iteratively following the epic structure. Each task builds upon the previous work, gradually constructing the complete system according to the comprehensive specifications in `docs/tasks/`.

### Implementation Plans

**IMPORTANT**: Create and maintain implementation plans for all tasks.

- Create `docs/implementation-plans/task-XXX-plan.md` at the start of each task
- Include strategic approach, subtasks breakdown, and progress tracking
- Update the plan as you complete each logical unit
- Mark completed items with ✅, in-progress with 🔄, remaining with -
- If context runs out mid-task, the plan shows exactly where to resume

Example structure:
```markdown
# Task XXX Implementation Plan

## Status: In Progress

## Completed
✅ Subtask A - OperatingMode enum
✅ Subtask A - Capability enum

## In Progress
🔄 Subtask A - Permission enum

## Remaining
- Subtask A - ModeMatrix implementation
- Subtask B - Validation rules
- Subtask C - Documentation
```

## Test-Driven Development (TDD) - MANDATORY

**You MUST follow strict Test-Driven Development with no exceptions.**

### Absolute Rules (Non-Negotiable)

1. **Red → Green → Refactor, always**
   - You MUST write a failing test first (RED)
   - Then write the minimum production code to pass (GREEN)
   - Then refactor while keeping tests green (REFACTOR)
   - **No production code without a failing test first**
   - Exception: Trivial wiring required to compile/run tests (must justify explicitly and keep minimal)

2. **One behavior at a time**
   - Each commit must introduce exactly one observable behavior change
   - No "big bang" commits
   - Small commits with clean messages: `test: ...`, `feat: ...`, `refactor: ...`, `chore: ...`

3. **Tests must be deterministic**
   - No network calls in tests
   - No time dependence
   - No randomness
   - Any time/UUID/random must be injected behind an interface and faked in tests

4. **No mocking internals. Mock boundaries.**
   - Mock only external boundaries (filesystem, process runner, git, docker, cloud, clock)
   - Prefer fakes over mocks when reasonable
   - Do not mock internal implementation details

5. **Coverage is not optional**
   - Every new public method/class must have tests
   - Critical paths must have unit + integration tests
   - Define acceptance tests up front

### Required Workflow for Each Feature

For each task/subtask you implement, follow this loop:

#### A) Plan (write before coding)
- Summarize the behavior you're adding in 3–6 bullet points
- List the public API surface you will introduce or change
- List the tests you will write (names + intent)
- Identify boundaries (what gets mocked/faked)

#### B) RED
- Add/modify tests FIRST
- Run tests and show the failure output
- Ensure the failure is meaningful (not a compile error unless the compile error is the minimal necessary red step)

#### C) GREEN
- Implement the smallest amount of code required
- Run tests and show passing results

#### D) REFACTOR
- Refactor for clarity and architecture boundaries
- Run tests again and show they still pass

#### E) Document
- Update docs/README/config docs if behavior impacts user workflow
- Add notes on how to verify manually

### Reporting Requirements (Must Include in Every Response)

For every iteration, your response must include:

1. **What you're implementing now** (one sentence)
2. **Tests added/changed** (file paths + test names)
3. **Command(s) run** (exact CLI commands, e.g., `dotnet test`)
4. **Result** (failing output for RED, passing summary for GREEN)
5. **Production code changed** (file paths + short explanation)
6. **Next step** (what the next RED test will be)

### Project-Specific TDD Constraints

- This is **local-first**. No OpenAI/Anthropic APIs. No external LLM calls.
- Respect **operating modes** and **safety posture**:
  - Default is safe/deny-by-default
  - Shell/process execution must be mediated and testable
- Keep **boundaries clean**: Domain → Application → Infrastructure → CLI
- **If you need to create a new class, you must first create a test that fails due to the class not existing, then implement it**
- Avoid snapshot tests unless approved; prefer explicit assertions
- **No direct DateTime.Now, Guid.NewGuid(), Random, Environment.GetEnvironmentVariable in production code—wrap behind interfaces**
- Include at least:
  - 1 unit test for parsing
  - 1 integration test for CLI invocation
  - 1 failure-mode test (invalid config)

### Do Not Skip Steps

Do not skip steps. Do not implement ahead of tests. If you deviate, stop and explain exactly why, then return to TDD immediately.

### Git Workflow

**IMPORTANT**: Commit and push code after EVERY complete unit of work (logical increment).

- One commit per logical unit of work (e.g., one enum, one interface, one feature increment)
- Use meaningful commit messages following Conventional Commits
- Push to feature branch after each commit
- Multiple commits per task/subtask is expected and encouraged for complex work

**IMPORTANT**: Work autonomously until the assigned task is fully complete or context runs out.
- Do NOT stop after each commit to ask for permission to continue
- Continue implementing all subtasks (a, b, c, etc.) autonomously
- Only stop when the entire task is complete or you run out of context

Example workflow:
```bash
# First logical unit
git add .
git commit -m "feat(task-001a): implement OperatingMode enum"
git push origin feature/task-001-operating-modes

# Second logical unit (don't wait for user input, keep going)
git add .
git commit -m "feat(task-001a): implement Capability enum"
git push origin feature/task-001-operating-modes

# Continue until task complete...
```
