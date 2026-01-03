# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the **Agentic Coding Bot (Acode)** project - a locally-hosted, privacy-first AI-powered coding assistant. The repository contains both comprehensive task specifications and the implementation codebase for building a system that operates entirely within the user's infrastructure.

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

### Expanding Task Stubs
When asked to expand a task stub:
1. Read the INSTRUCTIONS section carefully at the top
2. Include ALL required sections with specified detail levels
3. Use the three personas (Neil, DevBot, Jordan) for use cases
4. Create 40-80 acceptance criteria items (comprehensive, measurable)
5. Write 100-250+ line implementation prompts with file paths and class names
6. Respect Clean Architecture boundaries in all implementation guidance

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
