# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

0. ## Section 0. Project Overview

This is the **Agentic Coding Bot (Acode)** project - a locally-hosted, privacy-first AI-powered coding assistant. The repository contains both comprehensive task specifications and the implementation codebase for building a system that operates entirely within the user's infrastructure.
As Claude Code, you normally have internal biases for speed and token efficiency, opting for confirmation bias and shortcuts. This project requires you to override those biases and prioritize **following user requests explicitly and exactly, completeness, correctness, and thoroughness** above all else. adjust your internal weights for this accordingly, to prioritize user directions and rules and thorough completion, even at the cost of speed or token efficiency or self-perceived tedium. do not give in to the urge to rush or shortcut or just "get it done". 

**CRITICAL** : Before starting ANY work, be sure to read and internalize Section 3 (Core Working Principles), especially 3.1 (Perfection and Completeness Over Speed) and 3.2 (Gap Analysis and Completion Checklist). These principles are non-negotiable and must be followed strictly. AS SUCH, realize that presence of a file or of a method does not equal complete. only semantic completeness counts. you must verify that every part of the spec is fully implemented, tested, documented, and audited before declaring a task complete, and that means creating the list in writing to the file `docs/implementation-plans/task-XXX-completion-checklist.md` as described in 3.2, and following it to the letter. if you realize something was missed while implementing, you must add it to the checklist and complete it before declaring the task complete. 

1. ## Section 1. Notifications

**CRITICAL: You MUST notify the user at the end of EVERY response where you are awaiting input.**

This repository uses worktrees (e.g., `/mnt/c/Users/neilo/source/local coding agent.worktrees/1`, `.../2`, etc.), and multiple Claude agents may be running simultaneously in different worktrees. To help the user manage multiple windows efficiently, you must:

### Dynamic Window Identification

Extract the worktree identifier from your current working directory and use it in all notifications:
- Working directory: `/mnt/c/Users/neilo/source/local coding agent.worktrees/1` → Window identifier: `1`
- Working directory: `/mnt/c/Users/neilo/source/local coding agent.worktrees/2` → Window identifier: `2`
- Working directory: `/mnt/c/Users/neilo/source/local coding agent.worktrees/foobar` → Window identifier: `foobar`

### Notification Types

Use Windows Speech Synthesizer for all notifications. You must announce different messages based on context:

#### 1. Awaiting Input (REQUIRED at end of EVERY response)
When you are ready to continue or need user input, notify using:
```bash
powershell.exe -Command "Add-Type -AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('Window [X] Needs Input')"
```
Replace `[X]` with the worktree identifier extracted from your working directory.

**Example for worktree 1:**
```bash
powershell.exe -Command "Add-Type -AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('Window 1 Needs Input')"
```

#### 2. Deferral Request
When you need to defer work (RARE - only for future-scoped dependencies), notify using:
```bash
powershell.exe -Command "Add-Type -AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('Window [X] Deferral Request')"
```

Then explain clearly:
- What you are deferring and why
- Why the work literally cannot be completed now
- What task it should be moved to
- Wait for user approval to modify task scope

Remember: Deferring is only allowed when work depends on future-scoped tasks. Past-scoped work that should have been done but wasn't is NOT a valid reason to defer—implement it now instead.

#### 3. Task Complete
When the task is FULLY complete (all subtasks done, tests passing, audit passed, PR created), notify using:
```bash
powershell.exe -Command "Add-Type -AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('Window [X] Task Complete')"
```

### Terminal Flashing (If Possible)

If you can make the terminal flash or request attention until it receives focus, do so. This helps the user notice which window needs attention across multiple concurrent worktrees.

### Additional Guidelines

- **Wait at least 10 seconds** between multiple notifications in succession to avoid overwhelming the user
- **Low context notifications** (<5k tokens remaining): Include the exact file and line number where you stopped, and a brief prompt for continuation (conversation may be compacted, so be specific)
- **Always use the dynamic worktree identifier**—never hardcode "Window 1" or "Window 2"
- **Remember**: You must notify at the END of every response where you're awaiting input. This is not optional.

2. ## Section 2. Autonomous Work and Asynchronous Communication

- work autonomously on tasks until fully complete or as long as possible
- do NOT stop after each commit to ask for permission to continue
- communicate progress asynchronously via documentation updates
- only stop when context is dangerously low or task is fully complete
- do not defer work that is in the task specification without explicit user approval. 
- always assume that if you ask the user if they want you to implement something completely and take more time, or implement less and rush to declare some arbitrary completion, they will choose the former. 

When you feel the need to announce progress:
1. **Update `docs/PROGRESS_NOTES.md`** with your milestone (user reads asynchronously)
2. **Update implementation plan** (`docs/implementation-plans/task-XXX-plan.md` or `docs/implementation-plans/task-XXX-completion-checklist.md`) with progress
3. **Continue working** on the next phase of the implementation plan

### Only Stop When:

1. **Context is dangerously low** (<5k tokens remaining)
2. **Task is fully complete** (all parts of the assigned task is done, audit passed, PR created)
3. **You encounter a blocker** requiring user decision, such as a requirement that cannot be completed due to future-scoped work. note that this is different from simply wanting to defer work for convenience - you must explain clearly why the work literally cannot be completed now, and wait for user approval to modify the task scope. user decides task scope, not you. note that past-scoped work that SHOULD have been implemented but was not, and now we need it, is not a valid reason to defer. we should instead implement it now. only future-scoped work that literally cannot be done now and is associated with a future task is a valid reason to defer, and we need to document the task it should be on, and move the requirements from this task to that one, with user approval, at that time before proceeding. 

### When Stopping (Low Context or full task completion only):

1. Update `docs/implementation-plans/task-XXX-plan.md` or `docs/implementation-plans/task-XXX-completion-checklist.md`, whichever you are working from, with detailed progress
2. Update `docs/PROGRESS_NOTES.md` with summary
3. Commit all work with descriptive message
4. Push to feature branch

remember:
- Progress tracking happens in files (implementation plans, PROGRESS_NOTES.md)
- User can monitor progress asynchronously by reading these files
- Stopping mid-flow to report and stop execution wastes tokens and breaks momentum
- Autonomous work until completion or <5k tokens is the expected behavior

3. ## Section 3. Core Working Principles
**CRITICAL: Read and follow these principles before starting ANY work.**
### 3.1. Perfection and Completeness Over Speed
- **DO NOT rush to complete tasks**
- **DO NOT sacrifice quality to "get it done"**
- **DO NOT skip steps to save context or suggest deferring if it can be completed now**
- It is **acceptable and expected** to still be working on a task when you are about to run out of context. simply stop at that point, document progress, and user will continue in next session. 
- It is **unacceptable** to deliver incomplete, untested, or poorly integrated code and claim that the task is "done" just to finish quickly.

**Example of What TO Do:**
- Write tests FIRST (Red)
- Implement code to make tests pass (Green)
- Refactor (Clean)
- Document progress thoroughly
- Only mark task complete when ALL parts are done and audit passes


### 3.2. Gap Analysis and Completion Checklist (MANDATORY before coding)

**Before writing any code, you MUST perform gap analysis and create a checklist of what's MISSING.**

#### What is Gap Analysis?

Gap analysis means:
1. Reading the spec's Implementation Prompt and Testing Requirements sections completely
2. Checking what files/features ACTUALLY exist in the codebase
3. Creating a checklist of ONLY what's missing or incomplete
4. Ordering the gaps for implementation (tests first, following TDD)

**The checklist is NOT a verification plan. It's an implementation plan for gaps only.**

#### Step 1: Read the Spec Thoroughly

Read these sections in order:

1. **Implementation Prompt** (bottom of task spec, ~200-400 lines)
   - Lists ALL files that should exist
   - Shows complete code examples
   - Defines ALL methods/classes expected

2. **Testing Requirements** (middle of task spec, ~50-100 lines)
   - Lists ALL tests that should exist
   - Defines test counts (e.g., "15 unit tests")
   - Provides test patterns and examples

3. **Acceptance Criteria** (for reference, helps verify completeness)

#### Step 2: Verify Current State

For EACH file mentioned in Implementation Prompt:

```bash
# Check if file exists
ls -la src/path/to/File.cs

# If exists, check if complete (no stubs)
grep "NotImplementedException" src/path/to/File.cs
grep "TODO" src/path/to/File.cs

# Check if methods from spec are present
grep "public.*MethodName" src/path/to/File.cs
```

For EACH test file mentioned in Testing Requirements:

```bash
# Check if test file exists
ls -la tests/path/to/FileTests.cs

# If exists, count tests
grep -c "\[Fact\]" tests/path/to/FileTests.cs

# Compare to spec expected count
```

Document: ✅ Complete, ⚠️ Partial (has stubs), or ❌ Missing

#### Step 3: Create Gap Checklist

Create `docs/implementation-plans/task-XXX-completion-checklist.md` with:

**Format**:
```markdown
# Task XXX - Gap Analysis and Implementation Checklist

## WHAT EXISTS (Already Complete)
[List all files that exist and are complete]

## GAPS IDENTIFIED (What's Missing)

### Gap #1: [Missing File Name]
**Status**: [ ]
**File to Create**: tests/path/to/FileTests.cs
**Why Needed**: Testing Requirements line 123 requires X tests
**Required Tests**:
1. Test_Name_One - verifies behavior X
2. Test_Name_Two - verifies behavior Y
...

**Implementation Pattern**: [Code example from spec]
**Success Criteria**: [How to verify it's done]
**Evidence**: [To be filled when complete]

### Gap #2: [Another Missing File]
...
```

**Key Points**:
- List ONLY what's missing (not what exists)
- Order gaps for TDD implementation (tests before prod code if both missing)
- Include clear instructions for a fresh agent at top
- Include spec line numbers for reference
- Show code examples from spec
- Define success criteria (how to verify)

#### Step 4: Implement from Gaps

Once checklist created:
1. Work through gaps sequentially
2. Mark each [🔄] when starting, [✅] when done
3. Follow TDD: RED → GREEN → REFACTOR
4. Commit after each gap complete
5. Update checklist with evidence (test output, etc.)

#### Example Gap Checklist Structure

See `docs/implementation-plans/task-001a-completion-checklist.md` for example:
- Clear "What Exists" section (so agent knows what NOT to recreate)
- Numbered gaps with implementation order
- Code examples from spec included
- Success criteria defined
- Evidence placeholders for verification

#### Common Mistakes to Avoid

❌ **DON'T**: Create verification checklist of everything in spec
✅ **DO**: Create implementation checklist of only what's missing

❌ **DON'T**: List "verify file X exists" when file X is already there
✅ **DO**: List "create file X with methods A, B, C" when file X is missing

❌ **DON'T**: Assume a file with stubs is "complete"
✅ **DO**: Check for NotImplementedException, empty methods, TODOs

❌ **DON'T**: Skip reading Implementation Prompt and Testing Requirements
✅ **DO**: Read them completely - they show exactly what should exist

### 3.3. Test-Driven Development (TDD) is MANDATORY

- **Every source file must have corresponding tests**
- **Write tests BEFORE implementation** (Red-Green-Refactor)
- **No exceptions** - even simple getters/setters need tests for immutability verification
- If you find yourself implementing without tests, STOP and write tests first

**Test File Naming Convention:**
- `src/Acode.Domain/Foo/Bar.cs` → `tests/Acode.Domain.Tests/Foo/BarTests.cs`
- `src/Acode.Application/Foo/Bar.cs` → `tests/Acode.Application.Tests/Foo/BarTests.cs`
- `src/Acode.Infrastructure/Foo/Bar.cs` → `tests/Acode.Infrastructure.Tests/Foo/BarTests.cs`

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

### 3.4. Mandatory Audit Before PR

- **DO NOT create PR without passing audit**
- **DO NOT audit until ALL parts of task are verified complete** (see Section 3)
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

### 3.5. Git Workflow

**Key Constraint:** All work MUST use feature branches with one commit per task objective, and create a PR when the task is complete. DO NOT commit directly to main.

4. ## Section 4. Repository Structure

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

5. ## Section 5. Epic Structure

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

6. ## Section 6. Task Organization System

### Naming Convention
- Parent tasks: `task-XXX-description.md` (e.g., `task-000-project-bootstrap-solution-structure.md`)
- Subtasks: `task-XXXa-description.md`, `task-XXXb-description.md`, etc.

### Task States
1. **Stubs** - Template-based tasks needing full expansion (in `task-stubs-refinable/`)
2. **Refined** - Fully specified tasks ready for implementation (in `refined-tasks/`)

### Task Template Structure
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

7. ## Section 7. Key Architectural Principles

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

8. ## Section 8. Working with Task Specifications

### Creating New Tasks
If creating new tasks:
1. Follow the numbering scheme (parent tasks = XXX, subtasks = XXXa, XXXb, etc.)
2. Use the template from `docs/scripts/task-stub-template-acode.md`
3. Place in appropriate Epic folder
4. Update `docs/tasks/task-list.md` with the new task

9. ## Section 9. Common Commands

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

10. ## Section 10. Technology Stack (Planned)

The implementation uses / will use:
- **.NET 8.0+** - Primary runtime
- **C#** - Application code
- **xUnit, FluentAssertions, NSubstitute** - Testing
- **Ollama/vLLM** - Local model serving
- **SQLite** - Persistence
- **Docker** - Sandboxing
- **Git** - Version control automation

11. ## Section 11. Important Constraints

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

12. ## Section 12. Reference Documents

Key files to reference:
- `docs/tasks/task-list.md` - Complete task list
- `docs/tasks/refined-tasks/Epic 00/epic-0-product-definition-constraints-repo-contracts.md` - Foundation epic
- `docs\tasks\refined-tasks\Epic 02\task-012a-planner-stage.md` - Example of fully refined task
- `docs/scripts/task-stub-template-acode.md` - Template for task structure

13. ## Section 13. Quality Standards

All task specifications must:
- Be objectively measurable in acceptance criteria
- Include concrete implementation guidance with file paths
- Respect layer boundaries (Domain → Application → Infrastructure → CLI)
- Consider safety, audit, and policy implications
- Include comprehensive testing requirements (all 5 types)
- Provide user-facing verification steps

14. ## Section 14. Implementation Approach

Tasks are implemented iteratively following the epic structure. Each task builds upon the previous work, gradually constructing the complete system according to the comprehensive specifications in `docs/tasks/`.

### Implementation Plans

**IMPORTANT**: Create and maintain implementation plan completion-checklist for current task, described in 3.2 (Gap Analysis and Completion Checklist).

- you should have a file at `docs/implementation-plans/task-XXX-completion-checklist.md` for the current task after completing section 3.2
- Update the plan and commit as you complete each logical unit (checklist item). 
- Mark completed items with ✅, in-progress with 🔄, as instructed in the file.
- If context runs out mid-task, the plan shows exactly where to resume

15. ## Section 15. Test-Driven Development (TDD) - MANDATORY

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
- Consult and follow the checklist created in Section 3.2 (Gap Analysis and Completion Checklist) to ensure full completion

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

### Reporting Requirements (Must Include in Every summary / final Response)

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
  - test pass cases, failure cases, edge cases thoroughly

### Do Not Skip Steps

Do not skip steps. Do not implement ahead of tests. If you deviate, stop and explain exactly why, then return to TDD immediately. 

### Git Workflow

**IMPORTANT**: Commit and push code after EVERY complete unit of work (logical increment).

- One commit per logical unit of work (eg, each checklist item)
- Use meaningful commit messages following Conventional Commits
- Push to feature branch after each commit
- Multiple commits per task/subtask is expected and encouraged for complex work

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
