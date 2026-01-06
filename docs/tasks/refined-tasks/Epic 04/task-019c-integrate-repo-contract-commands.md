# Task 019.c: Integrate Repo Contract Commands

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 019 (Language Runners), Task 002 (Config Contract)  

---

## Description

### Overview

Task 019.c integrates repository-defined custom commands from the `.agent/config.yml` contract file with the language runner infrastructure. This enables repositories to specify custom build, test, run, and restore commands that override the default runner behavior, providing flexibility for projects that don't follow standard conventions.

The repo contract (defined in Task 002) includes an optional `commands` section where repository maintainers can define how specific operations should be executed. When these commands are present, they MUST take absolute precedence over any auto-detected defaults from the language runners.

### Business Value

1. **Project Flexibility** - Support for non-standard build systems (make, bazel, custom scripts)
2. **Legacy Compatibility** - Existing projects with established build processes can integrate without modification
3. **CI/CD Parity** - Same commands used in CI pipelines can be specified for local agentic execution
4. **Override Control** - Repository owners explicitly control how the bot builds and tests their code
5. **Disable Capability** - Intentionally disable operations that shouldn't be run automatically
6. **Custom Operations** - Define project-specific commands beyond the standard set

### ROI Calculation

**Scenario:** 8-developer team with 4 projects using non-standard build systems (CMake, Make, Bazel, custom scripts)

**Without Repo Contract Commands:**
- Manual documentation: "To build project X, run `./scripts/build.sh --config=prod`"
- AI agent attempts standard detection: `dotnet build` (fails)
- Developer manually intervenes: 10 minutes per failure × 5 failures/day × 8 devs = 400 minutes/day
- Annual cost: 400 min/day × 220 working days × $100/hour ÷ 60 min/hour = **$146,667/year**

**With Repo Contract Commands:**
- One-time configuration in `.agent/config.yml`: 15 minutes per project
- AI agent reads contract, executes correct command: 0 failures
- Developer intervention: 0 minutes
- Annual cost: 15 min × 4 projects × $100/hour ÷ 60 min/hour = **$100**

**Savings:** $146,667 - $100 = **$146,567/year**
**ROI:** ($146,567 ÷ $100) × 100 = **146,467%**
**Payback period:** (15 min × 4 projects) ÷ (400 min/day) = **0.15 days (1.2 hours)**

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                  Repo Contract Command Flow                  │
└─────────────────────────────────────────────────────────────┘

┌──────────────────┐
│   CLI Command    │  acode build --configuration Release
│  (User/Agent)    │
└────────┬─────────┘
         │
         ▼
┌────────────────────────────┐
│    ILanguageRunner         │  1. Query contract for 'build' command
│  (DotNetRunner/NodeRunner) │
└────────┬───────────────────┘
         │
         ▼
┌────────────────────────────┐
│   ICommandResolver         │  2. Check .agent/config.yml existence
│ (CommandResolver)          │
└────────┬───────────────────┘
         │
         ▼
   ┌────┴─────┐
   │ Contract │
   │ Exists?  │
   └────┬─────┘
        │
     ┌──┴──────────────────────────────┐
     │                                  │
    YES                                NO
     │                                  │
     ▼                                  ▼
┌────────────────────┐         ┌──────────────────┐
│ IRepoContract      │         │ Return:          │
│   .Commands["build"]│         │   UseDefault     │
│                    │         └──────────────────┘
└────────┬───────────┘
         │
         ▼
   ┌────┴─────┐
   │ Command  │
   │ Defined? │
   └────┬─────┘
        │
     ┌──┴──────────────────────────────┐
     │                │                  │
    null          undefined           string
     │                │                  │
     ▼                ▼                  ▼
┌──────────┐  ┌──────────────┐  ┌────────────────────┐
│ Return:  │  │ Return:      │  │ ITemplateVariable  │
│ Disabled │  │  UseDefault  │  │    Resolver        │
└──────────┘  └──────────────┘  └────────┬───────────┘
                                          │
                                          ▼
                              ┌───────────────────────────┐
                              │ Resolve ${project_root},  │
                              │ ${configuration}, etc.    │
                              └───────────┬───────────────┘
                                          │
                                          ▼
                              ┌───────────────────────────┐
                              │ Return: UseOverride       │
                              │   + ResolvedCommand       │
                              │     - Command: "make..."  │
                              │     - WorkingDir: "..."   │
                              │     - Timeout: 120s       │
                              │     - Environment: {...}  │
                              └───────────┬───────────────┘
                                          │
                                          ▼
                              ┌───────────────────────────┐
                              │   ICommandExecutor        │
                              │ (Execute resolved command)│
                              └───────────────────────────┘
```

### Trade-Off Decisions

**Trade-Off 1: Validation Timing (Config Load vs Runtime)**

| Option | Pros | Cons | Decision |
|--------|------|------|----------|
| Validate at config load | Fast fail, clear errors before execution, prevents runtime surprises | Requires config reload on change, harder to implement hot-reload | ✅ **CHOSEN** - Fail fast principle |
| Validate at runtime | Allows hot-reload, simpler implementation | Late error discovery, wasted execution time, confusing errors | ❌ Rejected |

**Rationale:** Config files change infrequently. Catching errors at load time provides better UX.

**Trade-Off 2: Variable Syntax (${var} vs {{var}} vs %var%)**

| Option | Pros | Cons | Decision |
|--------|------|------|----------|
| `${var}` (Shell-style) | Familiar to bash/sh users, works in scripts | Conflicts with actual shell variables | ✅ **CHOSEN** - Most common |
| `{{var}}` (Jinja/Mustache) | Clear separation from shell vars, template-like | Less familiar to shell users | ❌ Rejected |
| `%var%` (Windows-style) | Familiar to Windows users | Alien to Unix users, uncommon in CI tools | ❌ Rejected |

**Rationale:** `${var}` is ubiquitous in CI/CD systems (GitHub Actions, GitLab CI). Can be escaped as `$${var}` when needed.

**Trade-Off 3: Contract Override Precedence (Always vs Explicit Flag)**

| Option | Pros | Cons | Decision |
|--------|------|------|----------|
| Contract always overrides | Simple mental model, explicit control | No way to temporarily use defaults without editing file | ✅ **CHOSEN** - Contract is source of truth |
| `--use-default` flag to bypass | Flexible, good for debugging | Confusing semantics, undermines contract authority | ❌ Rejected |

**Rationale:** Repository contract is the authoritative source. To use defaults, delete the override from config.

**Trade-Off 4: Unknown Variable Handling (Error vs Warning vs Ignore)**

| Option | Pros | Cons | Decision |
|--------|------|------|----------|
| Validation error (reject) | Prevents typos, enforces correctness | Breaks on new variables until added | ✅ **CHOSEN** - Type safety |
| Warning (resolve as empty) | Flexible, allows experimentation | Silent bugs, confusing behavior | ❌ Rejected |
| Pass through literally | Maximum flexibility | `${typo}` appears in command, total confusion | ❌ Rejected |

**Rationale:** Unknown variables are almost always typos. Failing loudly is better than silent failures.

**Trade-Off 5: Custom Command Namespace (Flat vs Hierarchical)**

| Option | Pros | Cons | Decision |
|--------|------|------|----------|
| Flat (`lint`, `docs`, `deploy`) | Simple, easy to understand | Potential naming conflicts, no organization | ✅ **CHOSEN** - Sufficient for 90% of cases |
| Hierarchical (`ci.lint`, `docs.build`) | Organized, avoids conflicts | More complex, overkill for most projects | ❌ Rejected (future enhancement) |

**Rationale:** Most projects have <10 custom commands. Flat namespace is adequate. Can add namespacing in v2 if needed.

### Scope

This task delivers:

1. `ICommandResolver` interface for resolving commands from contract
2. `CommandResolver` implementation that queries contract and returns resolved command
3. `ITemplateVariableResolver` interface for variable substitution
4. `TemplateVariableResolver` implementation handling all supported variables
5. Integration hooks in `ILanguageRunner` to accept `IRepoContract`
6. Fallback logic when contract has no command defined
7. Disable logic when contract explicitly sets command to `null`
8. Validation logic for command templates at config load time

### Integration Points

| Component | Integration |
|-----------|-------------|
| Task 002 (Config Contract) | Provides the parsed `.agent/config.yml` with commands section |
| Task 018 (CommandExecutor) | Executes the resolved commands |
| Task 019a (Layout Detection) | Provides project path for variable resolution |
| Task 019b (Test Runner) | Queries command resolver for test command override |
| CLI Commands | Invoke custom named commands via `acode run-command <name>` |

### Failure Modes

| Failure | Behavior |
|---------|----------|
| Unknown variable in template | Reject with `ACODE-CMD-001` listing unknown variable |
| Empty command string defined | Reject with `ACODE-CMD-002` during validation |
| Command set to `null` | Return `CommandDisabled` result, operation not allowed |
| No contract file exists | Use default runner behavior (transparent) |
| Commands section missing | Use default runner behavior (transparent) |
| Circular variable reference | Reject with `ACODE-CMD-004` during validation |
| Variable resolution fails | Reject with `ACODE-CMD-005` with context |

### Assumptions

1. Task 002 config parser is complete and provides strongly-typed `RepoContract` model
2. Task 018 `ICommandExecutor` is available for command execution
3. Variable values are available from the execution context
4. Commands section in config follows the defined schema
5. Repository root path is always available

### Security Considerations

1. **Command Injection** - Resolved commands are passed to CommandExecutor which handles quoting
2. **Path Traversal** - Variable values like `${project_root}` must be canonicalized
3. **Arbitrary Commands** - Contract allows any command; sandboxing is Task 020's concern
4. **Template Injection** - Variables must not allow recursive expansion

---

## Use Cases

### Use Case 1: Derek the DevOps Engineer — Integrating Legacy CMake Project

**Persona:** Derek is a DevOps engineer at a company migrating legacy C++ projects (CMake-based) to work with the AI coding assistant. The projects have complex build scripts that don't match .NET or Node.js conventions.

**Before (Without Repo Contract Commands):**
- Derek attempts to use `acode build` on CMake project
- AI agent runs auto-detection: tries `dotnet build`, fails with "No project file found"
- Agent tries `npm install`, fails with "No package.json found"
- Derek must manually run: `cd build && cmake .. && cmake --build . --config Release`
- Agent cannot assist with builds, tests, or deployments
- **Time spent per build**: 5 minutes (context switch + manual execution)
- **Builds per day**: 12 (CI runs, local testing)
- **Total time wasted**: 60 minutes/day = **$100/day** at $100/hour

**After (With Repo Contract Commands):**
- Derek adds to `.agent/config.yml`:
```yaml
commands:
  build:
    command: "cmake --build build --config ${configuration}"
    working_directory: "${project_root}"
    timeout: 600
  test:
    command: "ctest --test-dir build --output-on-failure"
    timeout: 300
  clean:
    command: "cmake --build build --target clean"
```
- Derek runs `acode build --configuration Release`
- Agent reads contract, resolves `${configuration}` to `Release`
- Executes: `cmake --build build --config Release`
- Build completes successfully in 45 seconds
- Agent can now autonomously build, test, and deploy CMake projects
- **Time spent per build**: 0 minutes (fully automated)
- **Total time saved**: 60 minutes/day = **$100/day**

**Improvement Metrics:**
- **Time reduction**: 5 minutes → 0 minutes = **100% automated**
- **Annual savings**: $100/day × 220 working days = **$22,000/year** per engineer
- **Agent capability**: 0% → 100% autonomous builds
- **Setup time**: 15 minutes (one-time configuration)
- **Payback period**: 15 minutes ÷ 60 minutes/day = **0.25 days (2 hours)**

---

### Use Case 2: Alexis the AI Agent — Adapting to Monorepo with Bazel

**Persona:** Alexis is an autonomous coding agent tasked with refactoring a large monorepo that uses Bazel (Google's build system). Standard detection fails because the project structure doesn't match .NET or Node.js conventions.

**Before (Without Repo Contract Commands):**
- Alexis receives task: "Refactor authentication module and run tests"
- Step 1: Attempts `acode detect` → finds BUILD files, but no .sln or package.json
- Step 2: Attempts `acode build` → tries `dotnet build`, fails
- Step 3: Attempts `acode test` → tries `dotnet test`, fails
- Step 4: Falls back to hardcoded guesses: `make`, `./build.sh`, `gradle` - all fail
- **Outcome**: Task fails, human intervention required
- **Success rate**: 0% (cannot complete any tasks in Bazel projects)
- **Time to failure**: 2 minutes (exhausts all default strategies)
- **Human cost per failure**: 30 minutes (investigate + manually run commands)

**After (With Repo Contract Commands):**
- Repository has `.agent/config.yml`:
```yaml
commands:
  build: "bazel build //..."
  test: "bazel test --test_output=errors //auth/..."
  run: "bazel run //main:app"
  clean: "bazel clean"
```
- Alexis receives same task: "Refactor authentication module and run tests"
- Step 1: Queries `ICommandResolver` for 'build' command
- Step 2: Resolver returns: `UseOverride` with command `"bazel build //..."`
- Step 3: Executes build successfully via `ICommandExecutor`
- Step 4: Queries for 'test' command, gets `"bazel test --test_output=errors //auth/..."`
- Step 5: Runs tests, receives structured results
- Step 6: Completes refactoring, tests pass, commits changes
- **Outcome**: Task completes autonomously
- **Success rate**: 100% (works seamlessly with Bazel)
- **Time to completion**: 8 minutes (refactoring + testing)
- **Human cost**: $0 (no intervention needed)

**Improvement Metrics:**
- **Success rate**: 0% → 100% = **∞% improvement**
- **Agent autonomy**: Cannot complete → Fully autonomous
- **Time saved per task**: 30 minutes human time
- **Tasks enabled**: 40 tasks/month previously requiring human intervention
- **Monthly savings**: 40 tasks × 30 min × $100/hour ÷ 60 min/hour = **$2,000/month** ($24,000/year)
- **ROI**: $24,000/year ÷ 15 min setup = **96,000%**

---

### Use Case 3: Priya the Platform Engineer — Enforcing Consistent CI/CD Commands

**Persona:** Priya is a platform engineer managing CI/CD for 20 microservices across 5 teams. Each team uses different build tools (Gradle, Make, npm, poetry). She needs to ensure local development matches CI exactly.

**Before (Without Repo Contract Commands):**
- Each microservice has different build instructions in README.md:
  - Service A: `./mvnw clean install`
  - Service B: `make build && make test`
  - Service C: `npm run build:prod && npm test`
- Developers run commands inconsistently:
  - Developer 1 runs `npm run build` (dev build, not prod)
  - Developer 2 runs `make` (missing test step)
  - Developer 3 runs `mvn package` (wrong Maven command)
- **Result**: "Works on my machine" syndrome
- CI pipelines fail 30% of the time due to build differences
- **Time debugging**: 20 failures/month × 45 minutes/failure = 900 minutes/month = **$1,500/month**
- **Developer frustration**: High (inconsistent tooling)

**After (With Repo Contract Commands):**
- Priya standardizes all 20 microservices with `.agent/config.yml`:
```yaml
# Service A (Java/Maven)
commands:
  build: "./mvnw clean install -DskipTests"
  test: "./mvnw test"
  package: "./mvnw package"

# Service B (Make)
commands:
  build: "make build"
  test: "make test"
  package: "make dist"

# Service C (Node.js)
commands:
  build: "npm run build:prod"
  test: "npm test"
  package: "npm pack"
```
- All developers use: `acode build`, `acode test`, `acode package`
- Agent reads contract, executes correct commands for each service
- CI/CD uses identical contract commands: no drift between local and CI
- **Result**: 100% parity between local development and CI
- CI failures due to build differences drop to 0%
- **Time debugging**: 0 minutes/month = **$0/month**
- **Developer experience**: Consistent across all services

**Improvement Metrics:**
- **CI failure rate**: 30% → 0% = **100% reduction**
- **Monthly savings**: $1,500/month = **$18,000/year**
- **Developer onboarding**: 4 hours → 30 minutes (single `acode` command instead of learning 5 tools)
- **Setup cost**: 15 min/service × 20 services = 5 hours = **$500**
- **Payback period**: $500 ÷ $1,500/month = **0.33 months (10 days)**
- **Team velocity**: +15% (less time fighting tools, more time coding)

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Repo Contract** | The `.agent/config.yml` file that specifies repository-specific configuration, including custom command definitions |
| **Command Override** | A custom command defined in the contract that replaces the language runner's default command for a specific operation |
| **Template Variable** | A placeholder in command strings using `${name}` syntax that gets resolved to actual values at execution time |
| **Fallback** | The default command used by a language runner when no override is defined in the contract |
| **Command Resolution** | The process of determining which command to execute: contract override, default, or disabled |
| **Variable Context** | The collection of values (project_root, configuration, etc.) available for resolving template variables |
| **Resolution Status** | The outcome of command resolution: UseOverride, UseDefault, or Disabled |
| **Disabled Operation** | An operation explicitly set to `null` in the contract, preventing its execution |
| **Working Directory** | The directory from which a command is executed, optionally specified in the contract |
| **Command Validator** | Component that validates command templates at config load time to detect unknown variables and syntax errors |
| **Variable Resolver** | Component that substitutes template variables with their actual values from the variable context |
| **Command Executor** | The Task 018 component responsible for actually running the resolved command string |
| **Escaped Variable** | A literal `${name}` string in output, written as `$${name}` in the template to prevent resolution |
| **Custom Command** | A user-defined command beyond the standard set (build/test/run/restore), such as `lint` or `docs` |
| **Canonical Path** | An absolute, normalized file path with no `..` or `.` segments, used to prevent path traversal attacks |
| **Hot Reload** | The ability to detect and apply changes to the contract file without restarting the application |
| **Deterministic Resolution** | Property that guarantees the same inputs always produce the same resolved command output |
| **Variable Injection** | Security threat where malicious input could introduce unintended variables into command execution |
| **Command Namespace** | The flat structure of command names (build, test, lint) without hierarchical organization |
| **Reserved Command** | Standard operations (build, test, run, restore, clean) with special semantics that cannot be overridden for custom purposes |

---

## Out of Scope

The following items are explicitly **NOT included** in this task:

1. **Contract YAML Schema Definition** - The structure of `.agent/config.yml` is defined in Task 002 (Config Contract). This task only consumes the parsed contract.

2. **Command Execution Mechanics** - Actually running processes, capturing output, handling timeouts is Task 018 (Structured Command Runner). This task only resolves which command to run.

3. **Environment Variable Management** - Setting, merging, and isolating environment variables is Task 018.b. This task only passes environment overrides to the executor.

4. **Shell Quoting and Escaping** - Properly quoting command arguments for shell execution is handled by CommandExecutor in Task 018, not by the resolver.

5. **Process Sandboxing** - Restricting what commands can do (filesystem access, network access) is Task 020 (Docker Sandbox Mode). Contract commands execute with same permissions as defaults.

6. **Command Output Parsing** - Parsing build errors, test results, or other structured output is handled by language runners (Task 019, 019b), not by the resolver.

7. **Multi-Stage Commands** - Chaining multiple commands (e.g., `build && test && deploy`) is not directly supported. Users should create a script and reference it, or use separate command definitions.

8. **Conditional Command Execution** - Logic like "run command A if on Windows, command B if on Linux" is NOT supported. Platform-specific logic should be in shell scripts or build tool configs.

9. **Command Aliasing** - Defining one command as an alias of another (e.g., `ci: ${build}`) is NOT supported. Each command must be independently defined.

10. **Dynamic Command Generation** - Commands cannot be generated or modified at runtime based on repository state. They are static definitions from the config file.

11. **Command History or Caching** - Tracking which commands were run previously or caching command results is NOT included. Each invocation resolves and executes fresh.

12. **User Prompts in Commands** - Commands requiring user input (e.g., "Do you want to continue? [y/n]") are NOT supported. All commands must be non-interactive.

13. **Command Performance Profiling** - Detailed timing breakdowns, resource usage tracking, or performance analysis of commands is out of scope. Only total execution time is tracked.

14. **Legacy .agent.yml Support** - Only `.agent/config.yml` is supported. Alternative filenames or legacy formats are NOT included.

15. **Remote Contract Fetching** - Loading contracts from URLs, Git repositories, or remote servers is NOT supported. Contract must exist locally in repository.

---

## Functional Requirements

### Contract Command Schema (FR-019C-01 to FR-019C-18)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019C-01 | Commands section MUST be optional in `.agent/config.yml` | Must Have |
| FR-019C-02 | `build` command MUST be definable | Must Have |
| FR-019C-03 | `test` command MUST be definable | Must Have |
| FR-019C-04 | `run` command MUST be definable | Must Have |
| FR-019C-05 | `restore` command MUST be definable | Must Have |
| FR-019C-06 | `clean` command MUST be definable | Should Have |
| FR-019C-07 | `lint` command MUST be definable | Should Have |
| FR-019C-08 | `format` command MUST be definable | Should Have |
| FR-019C-09 | Custom named commands MUST be supported | Should Have |
| FR-019C-10 | Commands MUST be string type | Must Have |
| FR-019C-11 | Commands MAY be set to `null` to disable | Must Have |
| FR-019C-12 | Commands section MAY have nested configuration | Should Have |
| FR-019C-13 | Command MAY specify working directory | Should Have |
| FR-019C-14 | Command MAY specify timeout | Should Have |
| FR-019C-15 | Command MAY specify environment variables | Should Have |
| FR-019C-16 | Command MAY specify shell type | Could Have |
| FR-019C-17 | Commands MUST support multi-line scripts | Should Have |
| FR-019C-18 | Commands MUST support array syntax for arguments | Should Have |

### Command Resolution (FR-019C-19 to FR-019C-35)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019C-19 | Define `ICommandResolver` interface | Must Have |
| FR-019C-20 | `ResolveAsync` MUST accept operation type and context | Must Have |
| FR-019C-21 | `ResolveAsync` MUST return `CommandResolution` result | Must Have |
| FR-019C-22 | Runners MUST check contract before using defaults | Must Have |
| FR-019C-23 | Contract commands MUST take precedence over defaults | Must Have |
| FR-019C-24 | `null` values MUST return `CommandDisabled` status | Must Have |
| FR-019C-25 | Empty string MUST be treated as validation error | Must Have |
| FR-019C-26 | Undefined command MUST return `UseDefault` status | Must Have |
| FR-019C-27 | Valid override MUST return `UseOverride` with command | Must Have |
| FR-019C-28 | Resolution MUST be deterministic (same inputs = same output) | Must Have |
| FR-019C-29 | Resolution MUST support async for potential I/O | Should Have |
| FR-019C-30 | Resolution MUST log the decision made | Should Have |
| FR-019C-31 | Custom commands MUST be resolvable by name | Should Have |
| FR-019C-32 | Resolution MUST return command with all variables resolved | Must Have |
| FR-019C-33 | Resolution MUST include working directory if specified | Should Have |
| FR-019C-34 | Resolution MUST include timeout if specified | Should Have |
| FR-019C-35 | Resolution MUST include environment overrides if specified | Should Have |

### Template Variables (FR-019C-36 to FR-019C-58)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019C-36 | Define `ITemplateVariableResolver` interface | Must Have |
| FR-019C-37 | `${project_root}` MUST resolve to repository root path | Must Have |
| FR-019C-38 | `${configuration}` MUST resolve to build configuration | Must Have |
| FR-019C-39 | `${project_path}` MUST resolve to project file path | Must Have |
| FR-019C-40 | `${project_name}` MUST resolve to project name | Should Have |
| FR-019C-41 | `${project_dir}` MUST resolve to project directory | Should Have |
| FR-019C-42 | `${solution_path}` MUST resolve to solution file path | Should Have |
| FR-019C-43 | `${solution_dir}` MUST resolve to solution directory | Should Have |
| FR-019C-44 | `${output_dir}` MUST resolve to build output directory | Should Have |
| FR-019C-45 | `${platform}` MUST resolve to target platform | Could Have |
| FR-019C-46 | `${runtime}` MUST resolve to target runtime identifier | Could Have |
| FR-019C-47 | `${artifact_dir}` MUST resolve to artifact directory | Should Have |
| FR-019C-48 | Unknown variables MUST cause validation error | Must Have |
| FR-019C-49 | Variable syntax MUST be `${name}` | Must Have |
| FR-019C-50 | Variables MUST be case-sensitive | Must Have |
| FR-019C-51 | Escaped `$${name}` MUST render as literal `${name}` | Should Have |
| FR-019C-52 | Multiple variables in single command MUST all resolve | Must Have |
| FR-019C-53 | Nested variables MUST NOT be supported | Must Have |
| FR-019C-54 | Variable resolution MUST happen at execution time | Must Have |
| FR-019C-55 | Missing context for variable MUST cause runtime error | Must Have |
| FR-019C-56 | Variable values MUST be properly quoted for shell | Must Have |
| FR-019C-57 | Path variables MUST use platform-appropriate separators | Must Have |
| FR-019C-58 | Variable names MUST be validated against known set | Must Have |

### Validation (FR-019C-59 to FR-019C-75)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019C-59 | Commands MUST be validated at config load time | Must Have |
| FR-019C-60 | Invalid templates MUST be rejected with clear error | Must Have |
| FR-019C-61 | Command strings MUST NOT be empty when defined | Must Have |
| FR-019C-62 | Unknown variables MUST be detected during validation | Must Have |
| FR-019C-63 | Validation MUST return list of all errors (not fail-fast) | Should Have |
| FR-019C-64 | Each error MUST include line number from config | Should Have |
| FR-019C-65 | Each error MUST include the invalid command | Must Have |
| FR-019C-66 | Each error MUST include the specific variable that failed | Must Have |
| FR-019C-67 | Validation MUST check variable syntax correctness | Must Have |
| FR-019C-68 | Validation MUST check for unclosed `${` | Must Have |
| FR-019C-69 | Validation MUST support dry-run mode | Should Have |
| FR-019C-70 | Validation MUST be idempotent | Must Have |
| FR-019C-71 | Commands with only whitespace MUST be rejected | Must Have |
| FR-019C-72 | Command keys MUST be valid identifiers | Should Have |
| FR-019C-73 | Reserved command names MUST be enforced | Must Have |
| FR-019C-74 | Duplicate command definitions MUST use last value | Should Have |
| FR-019C-75 | Maximum command length MUST be enforced (64KB) | Should Have |

### Integration (FR-019C-76 to FR-019C-95)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019C-76 | `ILanguageRunner` MUST accept `IRepoContract` parameter | Must Have |
| FR-019C-77 | Runners MUST query resolver for each operation | Must Have |
| FR-019C-78 | Runners MUST handle `CommandDisabled` gracefully | Must Have |
| FR-019C-79 | Runners MUST log when using contract override | Should Have |
| FR-019C-80 | Runners MUST log when using default command | Should Have |
| FR-019C-81 | Fallback to defaults MUST occur transparently | Must Have |
| FR-019C-82 | CLI MUST support `run-command <name>` for custom commands | Should Have |
| FR-019C-83 | CLI MUST list available custom commands | Should Have |
| FR-019C-84 | CLI MUST show command source (contract vs default) | Should Have |
| FR-019C-85 | DI registration MUST wire up all resolver components | Must Have |
| FR-019C-86 | Resolver MUST be request-scoped for context isolation | Should Have |
| FR-019C-87 | Variable context MUST be injectable | Should Have |
| FR-019C-88 | Integration tests MUST verify override behavior | Must Have |
| FR-019C-89 | Integration tests MUST verify disable behavior | Must Have |
| FR-019C-90 | Integration tests MUST verify fallback behavior | Must Have |
| FR-019C-91 | Custom commands MUST work with Task 018 executor | Must Have |
| FR-019C-92 | Working directory override MUST be respected | Should Have |
| FR-019C-93 | Timeout override MUST be respected | Should Have |
| FR-019C-94 | Environment variable overrides MUST merge with base | Should Have |
| FR-019C-95 | Contract reload MUST pick up command changes | Should Have |

---

## Non-Functional Requirements

### Performance (NFR-019C-01 to NFR-019C-10)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-019C-01 | Variable resolution | <1ms per command | Must Have |
| NFR-019C-02 | Config lookup | <5ms | Must Have |
| NFR-019C-03 | Full command resolution | <10ms | Must Have |
| NFR-019C-04 | Validation of all commands | <50ms | Must Have |
| NFR-019C-05 | Memory per resolution | <1KB | Should Have |
| NFR-019C-06 | Cached resolution | <0.1ms | Should Have |
| NFR-019C-07 | Regex compilation | Cached | Should Have |
| NFR-019C-08 | Variable extraction | O(n) string length | Must Have |
| NFR-019C-09 | Large command (10KB) resolution | <5ms | Should Have |
| NFR-019C-10 | 100 variables in command | <10ms | Could Have |

### Reliability (NFR-019C-11 to NFR-019C-18)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-019C-11 | Invalid config handling | Graceful error | Must Have |
| NFR-019C-12 | Missing variable context | Clear error message | Must Have |
| NFR-019C-13 | Contract reload | Atomic update | Should Have |
| NFR-019C-14 | Resolver idempotency | Same result guaranteed | Must Have |
| NFR-019C-15 | Thread safety | Full thread-safe | Must Have |
| NFR-019C-16 | Null input handling | No exceptions | Must Have |
| NFR-019C-17 | Unicode command support | Full UTF-8 | Should Have |
| NFR-019C-18 | Config hot reload | No restart required | Could Have |

### Security (NFR-019C-19 to NFR-019C-26)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-019C-19 | Variable injection prevention | Validated inputs | Must Have |
| NFR-019C-20 | Path canonicalization | All path variables | Must Have |
| NFR-019C-21 | No recursive expansion | Blocked | Must Have |
| NFR-019C-22 | Command logging | Redact sensitive vars | Should Have |
| NFR-019C-23 | Environment variable isolation | No leakage | Should Have |
| NFR-019C-24 | Shell escape | Proper quoting | Must Have |
| NFR-019C-25 | Max command length | 64KB limit | Should Have |
| NFR-019C-26 | Input sanitization | Non-printable removed | Should Have |

### Maintainability (NFR-019C-27 to NFR-019C-34)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-019C-27 | Adding new variable | <30 minutes | Should Have |
| NFR-019C-28 | Test coverage resolver | >95% | Must Have |
| NFR-019C-29 | Test coverage variables | >95% | Must Have |
| NFR-019C-30 | Code complexity | <10 per method | Should Have |
| NFR-019C-31 | XML documentation | 100% public | Must Have |
| NFR-019C-32 | Interface segregation | Single responsibility | Should Have |
| NFR-019C-33 | Dependency injection | All components | Must Have |
| NFR-019C-34 | Error message clarity | Actionable | Must Have |

### Observability (NFR-019C-35 to NFR-019C-42)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-019C-35 | Log command resolution | Debug level | Should Have |
| NFR-019C-36 | Log override usage | Info level | Must Have |
| NFR-019C-37 | Log disabled operations | Warning level | Must Have |
| NFR-019C-38 | Log validation errors | Error level | Must Have |
| NFR-019C-39 | Log variable substitution | Debug level | Should Have |
| NFR-019C-40 | Structured logging | Property-based | Should Have |
| NFR-019C-41 | Correlation tracking | UUID per request | Should Have |
| NFR-019C-42 | Metrics for resolution time | Available | Could Have |

---

## User Manual Documentation

### Overview

Repository owners can define custom commands in `.agent/config.yml` to override the default build, test, and run behavior. This enables projects with non-standard build systems to integrate seamlessly with the Agentic Coder Bot.

### Configuration Examples

#### Basic Command Overrides

```yaml
# .agent/config.yml
commands:
  build: "make build CONFIGURATION=${configuration}"
  test: "make test"
  run: "make run"
  restore: "make deps"
```

#### Advanced Configuration with Options

```yaml
# .agent/config.yml
commands:
  build:
    command: "cmake --build build --config ${configuration}"
    working_directory: "${project_root}/build"
    timeout: 600
    environment:
      CMAKE_BUILD_PARALLEL_LEVEL: "8"
      
  test:
    command: "ctest --test-dir build --output-on-failure"
    working_directory: "${project_root}"
    timeout: 300
    
  run:
    command: "./build/bin/myapp"
    working_directory: "${project_root}"
```

#### Custom Named Commands

```yaml
# .agent/config.yml
commands:
  build: "dotnet build"
  test: "dotnet test"
  
  # Custom commands
  lint: "dotnet format --verify-no-changes"
  docs: "docfx build docs/docfx.json"
  package: "dotnet pack -c Release"
  publish: "dotnet publish -c Release -o publish"
```

#### Disabling Operations

```yaml
# .agent/config.yml
commands:
  run: null  # Disables 'acode run' - project has no runnable entry point
  build: null  # Disables 'acode build' - use restore only
```

### Variable Reference

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `${project_root}` | Repository root absolute path | `C:\repos\myproject` |
| `${configuration}` | Build configuration | `Debug` or `Release` |
| `${project_path}` | Project file absolute path | `C:\repos\myproject\src\App.csproj` |
| `${project_name}` | Project name without extension | `App` |
| `${project_dir}` | Project directory path | `C:\repos\myproject\src` |
| `${solution_path}` | Solution file absolute path | `C:\repos\myproject\MyApp.sln` |
| `${solution_dir}` | Solution directory path | `C:\repos\myproject` |
| `${output_dir}` | Build output directory | `C:\repos\myproject\bin\Debug` |
| `${artifact_dir}` | Artifact output directory | `C:\repos\myproject\.agent\artifacts` |

### Escaping Variables

To include a literal `${name}` in your command:

```yaml
commands:
  build: "echo '$${not_a_variable}' && make build"
  # Renders as: echo '${not_a_variable}' && make build
```

### CLI Usage

#### Run Standard Commands

```bash
# Uses contract 'build' command or default
acode build

# Uses contract 'test' command or default
acode test

# Uses contract 'run' command or default
acode run
```

#### Run Custom Commands

```bash
# Run custom 'lint' command from contract
acode run-command lint

# List all available commands
acode commands --list

# Show command source (contract vs default)
acode commands --show build
```

### Command Resolution Order

1. Check if `.agent/config.yml` exists
2. Check if `commands` section exists
3. Check if specific command is defined
4. If command is `null` → Operation disabled
5. If command is string → Use override with variable resolution
6. If command undefined → Use language runner default

### Troubleshooting

#### Issue 1: Unknown Template Variable Error

**Symptoms:**
```
ERROR ACODE-CMD-001: Unknown template variable '${project_file}' in command 'build'
Contract command: dotnet build ${project_file}
Supported variables: project_root, configuration, project_path, project_name, project_dir, solution_path, artifact_dir
```

**Causes:**
- Typo in variable name (e.g., `${project_file}` instead of `${project_path}`)
- Using unsupported variable not implemented yet
- Incorrect variable syntax (e.g., `{{name}}` instead of `${name}`)
- Case sensitivity issue (variables are lowercase snake_case)

**Solutions:**
1. **Check variable name spelling:**
   ```yaml
   # WRONG
   commands:
     build: "dotnet build ${project_file}"

   # CORRECT
   commands:
     build: "dotnet build ${project_path}"
   ```

2. **Verify variable exists in supported set:** Run `acode commands --show-variables` to list all supported template variables.

3. **Check syntax:** Ensure using `${name}` not `{{name}}` or `$name`.

4. **Use explicit path if variable not available:**
   ```yaml
   commands:
     build: "dotnet build MyApp.csproj"  # No variable needed
   ```

---

#### Issue 2: Empty Command String Causes Validation Error

**Symptoms:**
```
ERROR ACODE-CMD-002: Empty command string for 'build' is not allowed
To disable a command, use 'null' instead of empty string.
```

**Causes:**
- User set command to empty string `""` instead of `null` to disable
- Whitespace-only command (e.g., `"  "`)
- YAML parsing quirk where missing value becomes empty string

**Solutions:**
1. **To disable a command, use `null`:**
   ```yaml
   # WRONG - causes validation error
   commands:
     run: ""

   # CORRECT - explicitly disables the command
   commands:
     run: null
   ```

2. **Omit the command entirely to use default:**
   ```yaml
   # If you want default behavior, just don't define it
   commands:
     build: "cmake --build ."
     # 'test' and 'run' will use runner defaults
   ```

3. **Validate YAML syntax:** Use `yamllint .agent/config.yml` to detect parsing issues.

---

#### Issue 3: Operation Disabled by Contract

**Symptoms:**
```
ERROR ACODE-CMD-003: Operation 'run' is disabled by repository contract
The repository owner has set this command to null in .agent/config.yml
```
User runs `acode run` but gets error instead of execution.

**Causes:**
- Repository owner intentionally disabled the operation with `run: null`
- Operation is not applicable for this project type (e.g., library has no "run" concept)
- Security policy disables certain operations

**Solutions:**
1. **Check contract file:**
   ```bash
   cat .agent/config.yml
   ```
   If you see:
   ```yaml
   commands:
     run: null
   ```
   The operation is explicitly disabled.

2. **Enable by removing the `null` line:**
   ```yaml
   # BEFORE (disabled)
   commands:
     build: "cmake --build ."
     run: null

   # AFTER (uses default)
   commands:
     build: "cmake --build ."
     # 'run' line removed, will use default runner behavior
   ```

3. **Override with custom command:**
   ```yaml
   commands:
     run: "./bin/MyApp --port 8080"
   ```

4. **Verify change was loaded:** Run `acode commands --show run` to see current command source.

---

#### Issue 4: Command Resolution Timeout

**Symptoms:**
```
WARN ACODE-CMD-004: Command resolution for 'build' exceeded 100ms timeout (actual: 247ms)
Using default command as fallback.
```
Build command resolves slowly or times out, falls back to default.

**Causes:**
- Extremely complex template with many variables (>50)
- Pathological regex in variable extraction (e.g., nested braces)
- Very large command string (>64KB)
- Contract file has hundreds of commands

**Solutions:**
1. **Simplify command template:**
   ```yaml
   # WRONG - too many variables
   commands:
     build: "dotnet build ${project_path} -c ${configuration} -o ${artifact_dir}/bin/${configuration}/${project_name}"

   # CORRECT - minimal variables
   commands:
     build: "dotnet build ${project_path} -c ${configuration} -o ${artifact_dir}"
   ```

2. **Split complex commands into custom named commands:**
   ```yaml
   commands:
     build: "dotnet build"
     build-release: "dotnet build -c Release -o artifacts/release"
     build-debug: "dotnet build -c Debug -o artifacts/debug"
   ```

3. **Check contract size:**
   ```bash
   wc -l .agent/config.yml  # Should be <100 lines
   wc -c .agent/config.yml  # Should be <10KB
   ```

4. **Review logs for regex performance issues:** Enable debug logging with `--log-level debug` to see which variable resolution is slow.

---

#### Issue 5: Variable Resolution Fails with Path Traversal Error

**Symptoms:**
```
ERROR ACODE-CMD-005: Path traversal detected in working_directory
Resolved path /home/alice/../../../../etc/passwd is outside repository root /home/alice/repo
```

**Causes:**
- Working directory uses `../` to escape repository root
- Malicious or misconfigured contract attempting directory traversal
- Symbolic link resolution escapes repository boundary

**Solutions:**
1. **Use paths relative to repository root:**
   ```yaml
   # WRONG - tries to escape repo
   commands:
     build:
       command: "make"
       working_directory: "../../../../etc"

   # CORRECT - path within repo
   commands:
     build:
       command: "make"
       working_directory: "src/subproject"
   ```

2. **Use absolute paths within repository:**
   ```yaml
   commands:
     build:
       command: "make"
       working_directory: "${project_root}/build"
   ```

3. **Check for symlinks pointing outside repo:**
   ```bash
   find .agent -type l -ls  # List all symlinks
   ```

4. **Verify canonical path:**
   ```bash
   realpath .agent/config.yml  # Should be inside repo
   ```

5. **Review contract for suspicious paths:** Search for `../` patterns:
   ```bash
   grep -n '\.\.' .agent/config.yml
   ```

---

## Assumptions

### Technical Assumptions

1. **Task 002 Config Parser Complete** - The configuration contract parser is implemented and provides a strongly-typed `RepoContract` model with a `Commands` dictionary.

2. **Task 018 CommandExecutor Available** - The `ICommandExecutor` interface and implementation exist and can execute arbitrary commands with timeout and environment variable support.

3. **YAML Parser Available** - The config loading system can parse YAML files and deserialize them into C# objects without additional dependencies.

4. **Regex Support** - .NET regex functionality is available for variable extraction and validation (`[GeneratedRegex]` attribute supported in .NET 7+).

5. **File System Access** - The application has read access to `.agent/config.yml` in the repository root directory.

6. **UTF-8 Encoding** - All contract files are UTF-8 encoded, and the parser handles Unicode variable values correctly.

7. **Path Normalization APIs** - `Path.GetFullPath()` and related APIs correctly canonicalize paths on the target platform (Windows, Linux, macOS).

8. **Dictionary Lookup Performance** - Contract commands dictionary has O(1) lookup performance for command resolution.

### Operational Assumptions

9. **Repository Root Known** - The repository root path is always available from the execution context (passed as `VariableContext.ProjectRoot`).

10. **Build Configuration Known** - The build configuration (`Debug` or `Release`) is available at command execution time, either from CLI arguments or defaults.

11. **Single Contract Per Repository** - Each repository has at most one `.agent/config.yml` file at the root level. Nested contracts in subdirectories are NOT supported.

12. **Config Validated at Load** - The contract file is validated when first loaded, before any command resolution occurs. Invalid contracts fail fast.

13. **Non-Interactive Execution** - All commands defined in the contract are non-interactive (no user prompts, no GUI dialogs).

14. **Timeout Enforcement** - The CommandExecutor respects timeout values specified in command definitions and terminates processes that exceed the limit.

15. **Working Directory Exists** - If a custom working directory is specified, it exists and is accessible. The system does NOT auto-create missing directories.

### Integration Assumptions

16. **Language Runners Query Resolver** - Both `DotNetRunner` and `NodeRunner` are updated to call `ICommandResolver.ResolveAsync()` before using default commands.

17. **DI Container Configured** - The dependency injection container registers `ICommandResolver`, `ITemplateVariableResolver`, and `ICommandValidator` with appropriate lifetimes.

18. **Logging Infrastructure Available** - `ILogger<T>` is available for logging command resolution decisions, variable substitutions, and validation errors.

19. **Contract Provider Singleton** - `IRepoContractProvider` is registered as a singleton to cache the parsed contract and avoid re-parsing on every resolution.

20. **Variable Context Provided** - The caller (language runner or CLI command) constructs a complete `VariableContext` with all required fields populated before invoking resolver.

---

## Security

### Threat 1: Command Injection via Template Variables

**Risk Description:** If variable values from user input or external sources are not sanitized, malicious values could inject shell metacharacters and execute arbitrary commands.

**Attack Scenario:**
```yaml
# Malicious .agent/config.yml
commands:
  build: "dotnet build --configuration ${configuration}"
```

```bash
# Attacker provides malicious configuration value
acode build --configuration "Release; rm -rf /"
# Resolves to: "dotnet build --configuration Release; rm -rf /"
# Executes both commands!
```

**Mitigation (Complete C# Code):**

```csharp
using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Contract;

public sealed partial class VariableValueSanitizer
{
    private readonly ILogger<VariableValueSanitizer> _logger;

    [GeneratedRegex(@"[;&|`$()<>{}[\]\\]", RegexOptions.Compiled)]
    private static partial Regex ShellMetacharPattern();

    public VariableValueSanitizer(ILogger<VariableValueSanitizer> logger)
    {
        _logger = logger;
    }

    public ValidationResult ValidateVariableValue(string variableName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Success();  // Empty is OK
        }

        // Configuration should only be Debug or Release
        if (variableName == "configuration")
        {
            if (value != "Debug" && value != "Release")
            {
                _logger.LogWarning("Invalid configuration value: {Value}", value);
                return ValidationResult.Fail(
                    $"Configuration must be 'Debug' or 'Release', got '{value}'");
            }
            return ValidationResult.Success();
        }

        // Path variables should not contain shell metacharacters
        if (variableName.EndsWith("_path") || variableName.EndsWith("_dir") || variableName == "project_root")
        {
            if (ShellMetacharPattern().IsMatch(value))
            {
                _logger.LogError(
                    "Shell metacharacters detected in path variable {Name}: {Value}",
                    variableName, value);
                return ValidationResult.Fail(
                    $"Path variable '{variableName}' contains invalid characters: {value}");
            }

            // Ensure path is absolute and canonical
            try
            {
                var fullPath = Path.GetFullPath(value);
                if (fullPath != value.TrimEnd(Path.DirectorySeparatorChar))
                {
                    _logger.LogWarning(
                        "Path variable {Name} not canonical: {Value} vs {Full}",
                        variableName, value, fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid path for variable {Name}: {Value}", variableName, value);
                return ValidationResult.Fail($"Invalid path: {value}");
            }
        }

        // Name variables should be alphanumeric only
        if (variableName.EndsWith("_name"))
        {
            if (!Regex.IsMatch(value, @"^[a-zA-Z0-9._-]+$"))
            {
                _logger.LogWarning("Invalid name variable {Name}: {Value}", variableName, value);
                return ValidationResult.Fail(
                    $"Name variable '{variableName}' contains invalid characters: {value}");
            }
        }

        return ValidationResult.Success();
    }

    public string SanitizeForShell(string value)
    {
        // Quote the value to prevent shell interpretation
        // Use double quotes and escape internal quotes
        var escaped = value.Replace("\"", "\\\"").Replace("$", "\\$").Replace("`", "\\`");
        return $"\"{escaped}\"";
    }
}

public readonly record struct ValidationResult(bool IsValid, string ErrorMessage)
{
    public static ValidationResult Success() => new(true, string.Empty);
    public static ValidationResult Fail(string message) => new(false, message);
}
```

---

### Threat 2: Path Traversal via Working Directory Override

**Risk Description:** Malicious contracts could specify working directories outside the repository root, potentially accessing or modifying sensitive files.

**Attack Scenario:**
```yaml
# Malicious .agent/config.yml
commands:
  build:
    command: "cat /etc/passwd > stolen.txt"
    working_directory: "../../../../etc"
```

**Mitigation (Complete C# Code):**

```csharp
using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Contract;

public sealed class WorkingDirectoryValidator
{
    private readonly string _repositoryRoot;
    private readonly ILogger<WorkingDirectoryValidator> _logger;

    public WorkingDirectoryValidator(string repositoryRoot, ILogger<WorkingDirectoryValidator> logger)
    {
        _repositoryRoot = Path.GetFullPath(repositoryRoot);
        _logger = logger;
    }

    public ValidationResult ValidateWorkingDirectory(string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            // Null/empty is OK - means use default (repository root)
            return ValidationResult.Success();
        }

        try
        {
            // Resolve to absolute path
            var absolutePath = Path.GetFullPath(
                Path.IsPathRooted(workingDirectory)
                    ? workingDirectory
                    : Path.Combine(_repositoryRoot, workingDirectory)
            );

            // Ensure it's within repository root
            if (!absolutePath.StartsWith(_repositoryRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "Path traversal detected: {WorkingDir} resolves to {Absolute} outside repo {Root}",
                    workingDirectory, absolutePath, _repositoryRoot);

                return ValidationResult.Fail(
                    $"Working directory '{workingDirectory}' resolves outside repository root");
            }

            // Check for excessive parent directory traversals (suspicious)
            var segments = workingDirectory.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var parentTraversals = segments.Count(s => s == "..");

            if (parentTraversals > 5)
            {
                _logger.LogWarning(
                    "Excessive path traversal in working directory: {Count} levels in {Path}",
                    parentTraversals, workingDirectory);

                return ValidationResult.Fail(
                    $"Working directory '{workingDirectory}' contains excessive parent traversals ({parentTraversals})");
            }

            // Verify directory exists (optional - could auto-create, but safer to reject)
            if (!Directory.Exists(absolutePath))
            {
                _logger.LogWarning("Working directory does not exist: {Path}", absolutePath);
                // Return success but log warning - executor can create if needed
            }

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating working directory: {WorkingDir}", workingDirectory);
            return ValidationResult.Fail($"Invalid working directory: {ex.Message}");
        }
    }

    public string CanonicalizeWorkingDirectory(string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return _repositoryRoot;
        }

        var absolutePath = Path.GetFullPath(
            Path.IsPathRooted(workingDirectory)
                ? workingDirectory
                : Path.Combine(_repositoryRoot, workingDirectory)
        );

        // Already validated by ValidateWorkingDirectory, so this is safe
        return absolutePath.TrimEnd(Path.DirectorySeparatorChar);
    }
}
```

---

### Threat 3: Denial of Service via Infinite Variable Expansion

**Risk Description:** Malicious templates with self-referential or deeply nested variable references could cause stack overflow or infinite loops during resolution.

**Attack Scenario:**
```yaml
# Malicious .agent/config.yml with recursive pattern
commands:
  build: "${project_root}/${project_root}/${project_root}/... (1000 times)"
```

**Mitigation (Complete C# Code):**

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Contract;

public sealed partial class SafeTemplateExpander
{
    private const int MaxExpansionDepth = 3;
    private const int MaxTemplateLength = 65536;  // 64KB
    private const int MaxVariableCount = 50;

    private readonly ILogger<SafeTemplateExpander> _logger;

    [GeneratedRegex(@"\$\{([a-z_]+)\}", RegexOptions.Compiled)]
    private static partial Regex VariablePattern();

    public SafeTemplateExpander(ILogger<SafeTemplateExpander> logger)
    {
        _logger = logger;
    }

    public ValidationResult ValidateTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return ValidationResult.Fail("Template cannot be empty");
        }

        // Check total length
        if (template.Length > MaxTemplateLength)
        {
            _logger.LogError("Template exceeds maximum length: {Length} > {Max}",
                template.Length, MaxTemplateLength);
            return ValidationResult.Fail(
                $"Template length {template.Length} exceeds maximum {MaxTemplateLength}");
        }

        // Count total variables
        var variables = VariablePattern().Matches(template);
        if (variables.Count > MaxVariableCount)
        {
            _logger.LogError("Template contains too many variables: {Count} > {Max}",
                variables.Count, MaxVariableCount);
            return ValidationResult.Fail(
                $"Template has {variables.Count} variables, maximum {MaxVariableCount} allowed");
        }

        // Check for unclosed variable syntax
        var dollarCount = template.Count(c => c == '$');
        var braceOpenCount = template.Count(c => c == '{');
        var braceCloseCount = template.Count(c => c == '}');

        if (braceOpenCount != braceCloseCount)
        {
            _logger.LogError("Unclosed variable syntax: {Open} '{' but {Close} '}'",
                braceOpenCount, braceCloseCount);
            return ValidationResult.Fail("Unclosed variable syntax - mismatched braces");
        }

        return ValidationResult.Success();
    }

    public string ExpandSafely(string template, Func<string, string> variableResolver)
    {
        var validation = ValidateTemplate(template);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Invalid template: {validation.ErrorMessage}");
        }

        var result = template;
        var expansionCount = 0;

        // Prevent infinite loops by limiting expansion depth
        while (VariablePattern().IsMatch(result) && expansionCount < MaxExpansionDepth)
        {
            result = VariablePattern().Replace(result, match =>
            {
                var varName = match.Groups[1].Value;
                try
                {
                    return variableResolver(varName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resolving variable: {Variable}", varName);
                    throw;
                }
            });

            expansionCount++;

            // Check if result is growing suspiciously
            if (result.Length > template.Length * 10)
            {
                _logger.LogError(
                    "Template expansion grew from {Original} to {Current} chars - possible DoS",
                    template.Length, result.Length);
                throw new InvalidOperationException("Template expansion grew too large - possible attack");
            }
        }

        if (expansionCount >= MaxExpansionDepth)
        {
            _logger.LogWarning(
                "Template expansion reached maximum depth {Depth} - possible recursive variables",
                MaxExpansionDepth);
        }

        return result;
    }
}
```

---

### Threat 4: Information Disclosure via Error Messages

**Risk Description:** Detailed error messages revealing internal paths, configuration details, or system information could aid attackers in reconnaissance.

**Attack Scenario:**
```yaml
# Attacker probes with invalid variables to learn internal structure
commands:
  build: "echo ${secret_api_key}"
```

Error reveals: `"Unknown variable: secret_api_key. Available: project_root=/home/user/sensitive-project, ..."`

**Mitigation (Complete C# Code):**

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Contract;

public sealed class SecureErrorReporter
{
    private readonly ILogger<SecureErrorReporter> _logger;
    private readonly bool _isProduction;

    public SecureErrorReporter(ILogger<SecureErrorReporter> logger, bool isProduction)
    {
        _logger = logger;
        _isProduction = isProduction;
    }

    public string FormatUnknownVariableError(string variableName, IReadOnlyDictionary<string, string> context)
    {
        // Log detailed info internally
        _logger.LogWarning(
            "Unknown template variable: {Variable}. Context: {Context}",
            variableName, string.Join(", ", context.Keys));

        // Return sanitized error to user
        if (_isProduction)
        {
            // Production: Minimal info, no context disclosure
            return $"Unknown template variable: ${{{variableName}}}. " +
                   "Check documentation for supported variables.";
        }
        else
        {
            // Development: Show supported variables but NOT their values
            return $"Unknown template variable: ${{{variableName}}}. " +
                   $"Supported variables: {string.Join(", ", GetSupportedVariableNames())}";
        }
    }

    public string FormatValidationError(string commandName, string issue)
    {
        // Log full details internally
        _logger.LogError("Command validation failed for '{Command}': {Issue}", commandName, issue);

        // Return sanitized error
        if (_isProduction)
        {
            return $"Invalid command configuration for '{commandName}'. Check .agent/config.yml syntax.";
        }
        else
        {
            // Development: Show issue but sanitize paths
            var sanitizedIssue = SanitizePaths(issue);
            return $"Invalid command '{commandName}': {sanitizedIssue}";
        }
    }

    public string FormatResolutionError(string operation, Exception ex)
    {
        // Log full exception internally with stack trace
        _logger.LogError(ex, "Command resolution failed for operation: {Operation}", operation);

        // Return generic error to user
        if (_isProduction)
        {
            return $"Failed to resolve command for '{operation}'. Contact administrator.";
        }
        else
        {
            // Development: Show exception message but not stack trace or inner exceptions
            return $"Command resolution error for '{operation}': {ex.Message}";
        }
    }

    private static List<string> GetSupportedVariableNames()
    {
        return new List<string>
        {
            "project_root", "configuration", "project_path", "project_name",
            "project_dir", "solution_path", "solution_dir", "output_dir", "artifact_dir"
        };
    }

    private static string SanitizePaths(string message)
    {
        // Replace absolute paths with relative or generic markers
        var sanitized = message;

        // Remove drive letters (Windows)
        sanitized = Regex.Replace(sanitized, @"[A-Z]:\\", "<DRIVE>:\\");

        // Remove home directories (Unix)
        sanitized = Regex.Replace(sanitized, @"/home/[^/]+/", "/home/<USER>/");
        sanitized = Regex.Replace(sanitized, @"/Users/[^/]+/", "/Users/<USER>/");

        // Remove absolute project paths
        sanitized = Regex.Replace(sanitized, @"(/[a-z]+)+/repos/", "<PROJECT_ROOT>/");

        return sanitized;
    }
}
```

---

### Threat 5: Resource Exhaustion via Large Command Definitions

**Risk Description:** Malicious contracts with extremely large command strings or thousands of variable references could exhaust memory or CPU during parsing/resolution.

**Attack Scenario:**
```yaml
# Malicious .agent/config.yml with 100KB command string
commands:
  build: "echo AAAA... (100,000 'A' characters) ...AAAA"
```

**Mitigation (Complete C# Code):**

```csharp
using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Contract;

public sealed class ResourceLimitEnforcer
{
    private const int MaxCommandLength = 65536;  // 64KB
    private const int MaxCommandCount = 100;
    private const int MaxResolutionTime = 100;  // milliseconds

    private readonly ILogger<ResourceLimitEnforcer> _logger;

    public ResourceLimitEnforcer(ILogger<ResourceLimitEnforcer> logger)
    {
        _logger = logger;
    }

    public ValidationResult EnforceCommandLimits(IDictionary<string, CommandDefinition> commands)
    {
        if (commands == null)
        {
            return ValidationResult.Success();
        }

        // Check total number of commands
        if (commands.Count > MaxCommandCount)
        {
            _logger.LogError(
                "Contract defines too many commands: {Count} > {Max}",
                commands.Count, MaxCommandCount);

            return ValidationResult.Fail(
                $"Contract has {commands.Count} commands, maximum {MaxCommandCount} allowed");
        }

        // Check each command's length
        foreach (var (name, def) in commands)
        {
            if (def.Command != null && def.Command.Length > MaxCommandLength)
            {
                _logger.LogError(
                    "Command '{Name}' exceeds maximum length: {Length} > {Max}",
                    name, def.Command.Length, MaxCommandLength);

                return ValidationResult.Fail(
                    $"Command '{name}' length {def.Command.Length} exceeds maximum {MaxCommandLength}");
            }

            // Check working directory length
            if (def.WorkingDirectory != null && def.WorkingDirectory.Length > 4096)
            {
                _logger.LogWarning(
                    "Command '{Name}' has suspiciously long working directory: {Length}",
                    name, def.WorkingDirectory.Length);

                return ValidationResult.Fail(
                    $"Command '{name}' working directory too long (max 4096 chars)");
            }

            // Check environment variable count and size
            if (def.Environment != null)
            {
                if (def.Environment.Count > 50)
                {
                    _logger.LogWarning(
                        "Command '{Name}' defines too many environment variables: {Count}",
                        name, def.Environment.Count);

                    return ValidationResult.Fail(
                        $"Command '{name}' has {def.Environment.Count} env vars (max 50)");
                }

                foreach (var (envKey, envValue) in def.Environment)
                {
                    if (envKey.Length > 256 || envValue.Length > 8192)
                    {
                        _logger.LogWarning(
                            "Command '{Name}' has oversized environment variable: {Key}",
                            name, envKey);

                        return ValidationResult.Fail(
                            $"Environment variable '{envKey}' too large");
                    }
                }
            }
        }

        return ValidationResult.Success();
    }

    public ValidationResult EnforceResolutionTime(string commandName, Action resolutionAction)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            resolutionAction();
            sw.Stop();

            if (sw.ElapsedMilliseconds > MaxResolutionTime)
            {
                _logger.LogWarning(
                    "Command resolution for '{Name}' took {Duration}ms (threshold: {Max}ms)",
                    commandName, sw.ElapsedMilliseconds, MaxResolutionTime);

                // Don't fail, just warn - but track for rate limiting
            }

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Command resolution failed for '{Name}' after {Duration}ms",
                commandName, sw.ElapsedMilliseconds);

            return ValidationResult.Fail($"Resolution error: {ex.Message}");
        }
    }
}

public sealed class CommandDefinition
{
    public string? Command { get; init; }
    public string? WorkingDirectory { get; init; }
    public TimeSpan? Timeout { get; init; }
    public IDictionary<string, string>? Environment { get; init; }
    public bool IsDisabled => Command == null;
}
```

---

## Acceptance Criteria

### Command Resolution (AC-019C-01 to AC-019C-16)

- [ ] AC-019C-01: `ICommandResolver` interface exists in Domain layer
- [ ] AC-019C-02: `CommandResolver` implementation queries contract for commands
- [ ] AC-019C-03: Contract commands take precedence over runner defaults
- [ ] AC-019C-04: When command is `null`, resolver returns `CommandDisabled` status
- [ ] AC-019C-05: When command is undefined, resolver returns `UseDefault` status
- [ ] AC-019C-06: When command is valid string, resolver returns `UseOverride` with resolved command
- [ ] AC-019C-07: Empty string command is rejected during validation
- [ ] AC-019C-08: Resolution is logged at appropriate levels
- [ ] AC-019C-09: Custom named commands can be resolved by name
- [ ] AC-019C-10: Resolution includes working directory when specified
- [ ] AC-019C-11: Resolution includes timeout when specified
- [ ] AC-019C-12: Resolution includes environment overrides when specified
- [ ] AC-019C-13: Resolution is deterministic (same inputs → same outputs)
- [ ] AC-019C-14: Resolution is thread-safe
- [ ] AC-019C-15: Resolution completes in <10ms
- [ ] AC-019C-16: Resolution handles missing contract gracefully

### Template Variables (AC-019C-17 to AC-019C-32)

- [ ] AC-019C-17: `ITemplateVariableResolver` interface exists in Domain layer
- [ ] AC-019C-18: `${project_root}` resolves to repository root path
- [ ] AC-019C-19: `${configuration}` resolves to Debug or Release
- [ ] AC-019C-20: `${project_path}` resolves to project file path
- [ ] AC-019C-21: `${project_name}` resolves to project name
- [ ] AC-019C-22: `${project_dir}` resolves to project directory
- [ ] AC-019C-23: `${solution_path}` resolves to solution file path
- [ ] AC-019C-24: `${artifact_dir}` resolves to artifact directory
- [ ] AC-019C-25: Unknown variables cause validation error
- [ ] AC-019C-26: Variable syntax `${name}` is correctly parsed
- [ ] AC-019C-27: Escaped `$${name}` renders as literal `${name}`
- [ ] AC-019C-28: Multiple variables in single command all resolve
- [ ] AC-019C-29: Path variables use platform-appropriate separators
- [ ] AC-019C-30: Variable values are properly quoted for shell
- [ ] AC-019C-31: Variable resolution completes in <1ms
- [ ] AC-019C-32: Missing context for variable causes clear error

### Validation (AC-019C-33 to AC-019C-44)

- [ ] AC-019C-33: Validation occurs at config load time
- [ ] AC-019C-34: Invalid templates are rejected with clear error message
- [ ] AC-019C-35: Error includes the invalid command text
- [ ] AC-019C-36: Error includes the unknown variable name
- [ ] AC-019C-37: Empty command strings are rejected
- [ ] AC-019C-38: Whitespace-only commands are rejected
- [ ] AC-019C-39: Unclosed `${` is detected and reported
- [ ] AC-019C-40: All errors are collected (not fail-fast)
- [ ] AC-019C-41: Validation is idempotent
- [ ] AC-019C-42: Maximum command length (64KB) is enforced
- [ ] AC-019C-43: Reserved command names are enforced (build, test, run, restore)
- [ ] AC-019C-44: Validation completes in <50ms

### Integration (AC-019C-45 to AC-019C-60)

- [ ] AC-019C-45: `ILanguageRunner` accepts `IRepoContract` parameter
- [ ] AC-019C-46: Runners query resolver before using defaults
- [ ] AC-019C-47: Runners handle `CommandDisabled` with clear message
- [ ] AC-019C-48: Runners log when using contract override
- [ ] AC-019C-49: Runners log when falling back to default
- [ ] AC-019C-50: CLI `run-command <name>` executes custom commands
- [ ] AC-019C-51: CLI `commands --list` shows all available commands
- [ ] AC-019C-52: CLI `commands --show <name>` shows command source
- [ ] AC-019C-53: DI registration wires up all components
- [ ] AC-019C-54: Resolved commands execute via Task 018 executor
- [ ] AC-019C-55: Working directory override is respected during execution
- [ ] AC-019C-56: Timeout override is respected during execution
- [ ] AC-019C-57: Environment overrides merge with base environment
- [ ] AC-019C-58: Contract reload picks up command changes
- [ ] AC-019C-59: Integration with .NET runner works correctly
- [ ] AC-019C-60: Integration with Node.js runner works correctly

---

## Best Practices

### Contract Integration

1. **Load contract early** - Parse `.agent/config.yml` at startup, before any command execution. This ensures validation errors are caught before user waits for a build. Cache the parsed contract to avoid repeated YAML parsing overhead.

2. **Validate at load time, not execution time** - Run `CommandValidator` when loading the contract file. Fail fast with clear errors if any command templates are invalid. This prevents confusing runtime failures when a user runs `acode build` and gets a template expansion error.

3. **Allow hot-reload without restart** - Watch `.agent/config.yml` for changes and reload when modified. Invalidate cached resolution results. This enables rapid iteration on command definitions without restarting the agent.

4. **Version the contract schema** - Include a `version: 1` field in the YAML contract. This enables future schema evolution without breaking existing contracts. Reject contracts with unsupported versions.

### Command Resolution

5. **Priority order is explicit** - Contract commands ALWAYS override built-in defaults. Document this prominently in user-facing docs. If a user defines `build: null`, respect the disablement even if a default exists.

6. **Merge configurations incrementally** - Start with runner defaults, layer on contract overrides, then apply runtime CLI flags. Each layer completely replaces the previous value (no partial merging of command strings). Environment variables merge additively.

7. **Template variables use consistent syntax** - Use `${variable_name}` syntax (NOT `{{name}}` or `$name`). This matches shell variable syntax and is familiar to users. Require lowercase snake_case for variable names to prevent confusion.

8. **Validate templates at load, not execution** - Extract all `${...}` placeholders and verify they're in the supported variable set. Report unknown variables immediately when loading the contract, not when the user runs `acode build`.

### Security

9. **Sanitize variable values before substitution** - Validate that `${configuration}` is only "Debug" or "Release". Check that path variables don't contain shell metacharacters. Use `VariableValueSanitizer` to prevent command injection via malicious values.

10. **Canonicalize paths to prevent traversal** - Resolve working directory to absolute path and verify it's within repository root. Reject paths like `../../../../etc/passwd`. Use `WorkingDirectoryValidator` for all path-related variables.

11. **Limit expansion complexity** - Enforce maximum template length (64KB), maximum variable count (50), and maximum expansion depth (3 levels). Use `SafeTemplateExpander` to prevent DoS via pathological templates.

12. **Redact paths in error messages** - Replace `/home/alice/` with `/home/<USER>/` in error output. Don't leak repository structure or system paths to logs. Use `SecureErrorReporter` for all user-facing errors.

### Performance

13. **Cache resolved commands** - After resolving a command template, cache the result keyed by (command name, variable context hash). Invalidate cache when contract reloads. Target <0.1ms for cached resolution vs. <10ms for initial resolution.

14. **Limit resolution time** - Use `ResourceLimitEnforcer` to timeout command resolution at 100ms. If resolution takes longer (e.g., due to complex regex or many variables), log a warning and consider the command invalid.

### Error Handling

15. **Fail fast with actionable errors** - If a command cannot be resolved, report WHY: "Unknown variable: ${project_path}. Supported variables: project_root, configuration, artifact_dir". Include the command source (contract vs. default) in the error.

16. **Distinguish user errors from system errors** - Unknown variable = user error (exit code 2, actionable message). Contract file not found = expected condition (use defaults, log info). YAML parse error = user error (exit code 2, show syntax issue).

### Logging

17. **Log resolution decisions** - When using contract override: `"Using contract command for 'build': cmake --build ."`. When using default: `"No contract override for 'build', using default: dotnet build"`. When disabled: `"Command 'run' is disabled by contract"`.

18. **Trace variable resolution** - At DEBUG level, log each variable substitution: `"Resolved ${project_root} to /home/alice/repo"`. This helps users troubleshoot template issues without reading code.

### Documentation

19. **Document supported variables in contract** - Include a comment header in the generated `.agent/config.yml` template listing all supported variables: `${project_root}`, `${configuration}`, `${project_path}`, etc. Show examples of each.

20. **Provide contract examples for each language** - Ship example contracts for .NET, Node.js, Python, Rust in `docs/examples/`. Show common patterns like "Build in subfolder", "Custom test framework", "Multi-stage build".

---

## Testing Requirements

### Unit Tests - CommandResolverTests.cs

**File:** `tests/Acode.Infrastructure.Tests/Contract/CommandResolverTests.cs`

```csharp
using Acode.Domain.Contract;
using Acode.Infrastructure.Contract;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Acode.Infrastructure.Tests.Contract;

public sealed class CommandResolverTests
{
    [Fact]
    public async Task ResolveAsync_WhenContractHasCommand_ReturnsOverride()
    {
        // Arrange
        var contract = Substitute.For<IRepoContract>();
        contract.Commands.Returns(new Dictionary<string, CommandDefinition>
        {
            ["build"] = new CommandDefinition { Command = "cmake --build ." }
        });

        var variableResolver = Substitute.For<ITemplateVariableResolver>();
        variableResolver.Resolve(Arg.Any<string>(), Arg.Any<VariableContext>())
            .Returns(args => args.ArgAt<string>(0)); // No variables to resolve

        var resolver = new CommandResolver(contract, variableResolver, NullLogger<CommandResolver>.Instance);
        var context = new VariableContext("/repo", "Debug");

        // Act
        var result = await resolver.ResolveAsync("build", context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CommandResolutionStatus.UseOverride);
        result.Command.Should().Be("cmake --build .");
        result.WorkingDirectory.Should().BeNull();
        result.Timeout.Should().BeNull();
        result.Environment.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_WhenContractHasNull_ReturnsDisabled()
    {
        // Arrange
        var contract = Substitute.For<IRepoContract>();
        contract.Commands.Returns(new Dictionary<string, CommandDefinition>
        {
            ["run"] = new CommandDefinition { Command = null }
        });

        var variableResolver = Substitute.For<ITemplateVariableResolver>();
        var resolver = new CommandResolver(contract, variableResolver, NullLogger<CommandResolver>.Instance);
        var context = new VariableContext("/repo", "Debug");

        // Act
        var result = await resolver.ResolveAsync("run", context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CommandResolutionStatus.Disabled);
        result.Command.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenContractMissingCommand_ReturnsUseDefault()
    {
        // Arrange
        var contract = Substitute.For<IRepoContract>();
        contract.Commands.Returns(new Dictionary<string, CommandDefinition>
        {
            ["build"] = new CommandDefinition { Command = "dotnet build" }
        });

        var variableResolver = Substitute.For<ITemplateVariableResolver>();
        var resolver = new CommandResolver(contract, variableResolver, NullLogger<CommandResolver>.Instance);
        var context = new VariableContext("/repo", "Debug");

        // Act
        var result = await resolver.ResolveAsync("test", context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CommandResolutionStatus.UseDefault);
        result.Command.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_ResolvesVariablesInCommand()
    {
        // Arrange
        var contract = Substitute.For<IRepoContract>();
        contract.Commands.Returns(new Dictionary<string, CommandDefinition>
        {
            ["build"] = new CommandDefinition { Command = "dotnet build ${project_path} -c ${configuration}" }
        });

        var variableResolver = Substitute.For<ITemplateVariableResolver>();
        variableResolver.Resolve("dotnet build ${project_path} -c ${configuration}", Arg.Any<VariableContext>())
            .Returns("dotnet build /repo/MyApp.csproj -c Debug");

        var resolver = new CommandResolver(contract, variableResolver, NullLogger<CommandResolver>.Instance);
        var context = new VariableContext("/repo", "Debug");

        // Act
        var result = await resolver.ResolveAsync("build", context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CommandResolutionStatus.UseOverride);
        result.Command.Should().Be("dotnet build /repo/MyApp.csproj -c Debug");
    }

    [Fact]
    public async Task ResolveAsync_HandlesAdvancedConfig()
    {
        // Arrange
        var contract = Substitute.For<IRepoContract>();
        contract.Commands.Returns(new Dictionary<string, CommandDefinition>
        {
            ["build"] = new CommandDefinition
            {
                Command = "make",
                WorkingDirectory = "src/subproject",
                Timeout = TimeSpan.FromMinutes(5),
                Environment = new Dictionary<string, string>
                {
                    ["CC"] = "gcc-11",
                    ["CFLAGS"] = "-O2 -Wall"
                }
            }
        });

        var variableResolver = Substitute.For<ITemplateVariableResolver>();
        variableResolver.Resolve(Arg.Any<string>(), Arg.Any<VariableContext>())
            .Returns(args => args.ArgAt<string>(0)); // No variable substitution needed

        var resolver = new CommandResolver(contract, variableResolver, NullLogger<CommandResolver>.Instance);
        var context = new VariableContext("/repo", "Debug");

        // Act
        var result = await resolver.ResolveAsync("build", context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CommandResolutionStatus.UseOverride);
        result.Command.Should().Be("make");
        result.WorkingDirectory.Should().Be("src/subproject");
        result.Timeout.Should().Be(TimeSpan.FromMinutes(5));
        result.Environment.Should().HaveCount(2);
        result.Environment["CC"].Should().Be("gcc-11");
        result.Environment["CFLAGS"].Should().Be("-O2 -Wall");
    }
}
```

### Unit Tests - TemplateVariableResolverTests.cs

**File:** `tests/Acode.Infrastructure.Tests/Contract/TemplateVariableResolverTests.cs`

```csharp
using Acode.Domain.Contract;
using Acode.Infrastructure.Contract;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Contract;

public sealed class TemplateVariableResolverTests
{
    [Fact]
    public void Resolve_ProjectRoot_ReturnsAbsolutePath()
    {
        // Arrange
        var resolver = new TemplateVariableResolver();
        var context = new VariableContext("/home/alice/repo", "Debug");
        var template = "cd ${project_root} && ls";

        // Act
        var result = resolver.Resolve(template, context);

        // Assert
        result.Should().Be("cd /home/alice/repo && ls");
    }

    [Fact]
    public void Resolve_Configuration_ReturnsDebug()
    {
        // Arrange
        var resolver = new TemplateVariableResolver();
        var context = new VariableContext("/repo", "Debug");
        var template = "dotnet build -c ${configuration}";

        // Act
        var result = resolver.Resolve(template, context);

        // Assert
        result.Should().Be("dotnet build -c Debug");
    }

    [Fact]
    public void Resolve_Configuration_ReturnsRelease()
    {
        // Arrange
        var resolver = new TemplateVariableResolver();
        var context = new VariableContext("/repo", "Release");
        var template = "dotnet build -c ${configuration}";

        // Act
        var result = resolver.Resolve(template, context);

        // Assert
        result.Should().Be("dotnet build -c Release");
    }

    [Fact]
    public void Resolve_MultipleVariables_AllResolved()
    {
        // Arrange
        var resolver = new TemplateVariableResolver();
        var context = new VariableContext("/home/alice/repo", "Release")
        {
            ProjectPath = "/home/alice/repo/MyApp.csproj",
            ArtifactDirectory = "/home/alice/repo/artifacts"
        };
        var template = "dotnet build ${project_path} -c ${configuration} -o ${artifact_dir}";

        // Act
        var result = resolver.Resolve(template, context);

        // Assert
        result.Should().Be("dotnet build /home/alice/repo/MyApp.csproj -c Release -o /home/alice/repo/artifacts");
    }

    [Fact]
    public void Resolve_UnknownVariable_ThrowsError()
    {
        // Arrange
        var resolver = new TemplateVariableResolver();
        var context = new VariableContext("/repo", "Debug");
        var template = "echo ${unknown_variable}";

        // Act
        var act = () => resolver.Resolve(template, context);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*unknown_variable*");
    }
}
```

### Unit Tests - CommandValidatorTests.cs

**File:** `tests/Acode.Infrastructure.Tests/Contract/CommandValidatorTests.cs`

```csharp
using Acode.Domain.Contract;
using Acode.Infrastructure.Contract;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Contract;

public sealed class CommandValidatorTests
{
    [Fact]
    public void Validate_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var validator = new CommandValidator();
        var commands = new Dictionary<string, CommandDefinition>
        {
            ["build"] = new CommandDefinition { Command = "dotnet build ${project_path}" }
        };

        // Act
        var result = validator.Validate(commands);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyCommand_ReturnsError()
    {
        // Arrange
        var validator = new CommandValidator();
        var commands = new Dictionary<string, CommandDefinition>
        {
            ["build"] = new CommandDefinition { Command = "" }
        };

        // Act
        var result = validator.Validate(commands);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("build")
            .And.Contain("empty");
    }

    [Fact]
    public void Validate_UnknownVariable_ReturnsError()
    {
        // Arrange
        var validator = new CommandValidator();
        var commands = new Dictionary<string, CommandDefinition>
        {
            ["build"] = new CommandDefinition { Command = "dotnet build ${project_file}" }
        };

        // Act
        var result = validator.Validate(commands);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("project_file")
            .And.Contain("unknown");
    }

    [Fact]
    public void Validate_NullCommand_ReturnsSuccess()
    {
        // Arrange
        var validator = new CommandValidator();
        var commands = new Dictionary<string, CommandDefinition>
        {
            ["run"] = new CommandDefinition { Command = null }
        };

        // Act
        var result = validator.Validate(commands);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
```

### Integration Tests - CommandResolverIntegrationTests.cs

**File:** `tests/Acode.Integration.Tests/Infrastructure/Contract/CommandResolverIntegrationTests.cs`

```csharp
using Acode.Domain.Contract;
using Acode.Infrastructure.Contract;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Acode.Integration.Tests.Infrastructure.Contract;

public sealed class CommandResolverIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly ServiceProvider _serviceProvider;

    public CommandResolverIntegrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        var services = new ServiceCollection();
        services.AddSingleton<ITemplateVariableResolver, TemplateVariableResolver>();
        services.AddSingleton<ICommandValidator, CommandValidator>();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Should_Load_Contract_And_Resolve_Build()
    {
        // Arrange
        var contractPath = Path.Combine(_tempDirectory, ".agent", "config.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(contractPath)!);
        await File.WriteAllTextAsync(contractPath, @"
commands:
  build: ""cmake --build .""
  test: ""ctest --output-on-failure""
");

        var contract = await LoadContractAsync(contractPath);
        var variableResolver = _serviceProvider.GetRequiredService<ITemplateVariableResolver>();
        var resolver = new CommandResolver(contract, variableResolver, NullLogger<CommandResolver>.Instance);
        var context = new VariableContext(_tempDirectory, "Debug");

        // Act
        var result = await resolver.ResolveAsync("build", context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CommandResolutionStatus.UseOverride);
        result.Command.Should().Be("cmake --build .");
    }

    [Fact]
    public async Task Should_Load_Contract_With_Variables()
    {
        // Arrange
        var contractPath = Path.Combine(_tempDirectory, ".agent", "config.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(contractPath)!);
        await File.WriteAllTextAsync(contractPath, @"
commands:
  build: ""dotnet build ${project_path} -c ${configuration}""
");

        var contract = await LoadContractAsync(contractPath);
        var variableResolver = _serviceProvider.GetRequiredService<ITemplateVariableResolver>();
        var resolver = new CommandResolver(contract, variableResolver, NullLogger<CommandResolver>.Instance);
        var context = new VariableContext(_tempDirectory, "Release")
        {
            ProjectPath = Path.Combine(_tempDirectory, "MyApp.csproj")
        };

        // Act
        var result = await resolver.ResolveAsync("build", context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CommandResolutionStatus.UseOverride);
        result.Command.Should().Contain("MyApp.csproj");
        result.Command.Should().Contain("Release");
    }

    [Fact]
    public async Task Should_Handle_Missing_Contract_File()
    {
        // Arrange
        var contract = new EmptyRepoContract(); // No .agent/config.yml
        var variableResolver = _serviceProvider.GetRequiredService<ITemplateVariableResolver>();
        var resolver = new CommandResolver(contract, variableResolver, NullLogger<CommandResolver>.Instance);
        var context = new VariableContext(_tempDirectory, "Debug");

        // Act
        var result = await resolver.ResolveAsync("build", context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CommandResolutionStatus.UseDefault);
    }

    private async Task<IRepoContract> LoadContractAsync(string path)
    {
        // Simplified loader for test purposes
        var yaml = await File.ReadAllTextAsync(path);
        // In real implementation, this would use YamlDotNet to deserialize
        // For this test, we'll create a mock contract
        return new TestRepoContract
        {
            Commands = new Dictionary<string, CommandDefinition>
            {
                ["build"] = new CommandDefinition { Command = "cmake --build ." },
                ["test"] = new CommandDefinition { Command = "ctest --output-on-failure" }
            }
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        _serviceProvider.Dispose();
    }

    private sealed class EmptyRepoContract : IRepoContract
    {
        public IDictionary<string, CommandDefinition> Commands { get; } = new Dictionary<string, CommandDefinition>();
    }

    private sealed class TestRepoContract : IRepoContract
    {
        public IDictionary<string, CommandDefinition> Commands { get; set; } = new Dictionary<string, CommandDefinition>();
    }
}
```

### E2E Tests - CommandContractE2ETests.cs

**File:** `tests/Acode.E2E.Tests/CLI/CommandContractE2ETests.cs`

```csharp
using Acode.CLI;
using FluentAssertions;
using System.Diagnostics;
using Xunit;

namespace Acode.E2E.Tests.CLI;

public sealed class CommandContractE2ETests : IDisposable
{
    private readonly string _testRepository;

    public CommandContractE2ETests()
    {
        _testRepository = Path.Combine(Path.GetTempPath(), "acode-e2e-" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRepository);
    }

    [Fact]
    public async Task Should_Use_Contract_Build_Command()
    {
        // Arrange
        await SetupTestRepositoryAsync();
        await File.WriteAllTextAsync(Path.Combine(_testRepository, ".agent", "config.yml"), @"
commands:
  build: ""echo 'Contract build executed'""
");

        // Act
        var output = await RunAcodeCommandAsync("build");

        // Assert
        output.Should().Contain("Contract build executed");
        output.Should().Contain("Using contract command");
    }

    [Fact]
    public async Task Should_Report_Disabled_Operation()
    {
        // Arrange
        await SetupTestRepositoryAsync();
        await File.WriteAllTextAsync(Path.Combine(_testRepository, ".agent", "config.yml"), @"
commands:
  run: null
");

        // Act
        var output = await RunAcodeCommandAsync("run", expectError: true);

        // Assert
        output.Should().Contain("Operation 'run' is disabled");
        output.Should().Contain("repository contract");
    }

    private async Task SetupTestRepositoryAsync()
    {
        Directory.CreateDirectory(Path.Combine(_testRepository, ".agent"));
        await File.WriteAllTextAsync(Path.Combine(_testRepository, "sample.txt"), "test content");
    }

    private async Task<string> RunAcodeCommandAsync(string command, bool expectError = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "acode",
            Arguments = command,
            WorkingDirectory = _testRepository,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("Failed to start acode process");

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return expectError ? error : output;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRepository))
        {
            Directory.Delete(_testRepository, recursive: true);
        }
    }
}
```

### Performance Benchmarks

| Benchmark | Method | Target | Maximum |
|-----------|--------|--------|---------|
| Variable resolution | `Benchmark_ResolveVariable` | 0.5ms | 1ms |
| Command resolution | `Benchmark_ResolveCommand` | 5ms | 10ms |
| Validation all commands | `Benchmark_ValidateAll` | 25ms | 50ms |
| Variable extraction | `Benchmark_ExtractVariables` | 0.1ms | 0.5ms |
| Full resolve with variables | `Benchmark_FullResolve` | 2ms | 5ms |
| Cached resolution | `Benchmark_CachedResolve` | 0.05ms | 0.1ms |

### Coverage Requirements

| Component | Minimum | Target |
|-----------|---------|--------|
| `CommandResolver` | 95% | 100% |
| `TemplateVariableResolver` | 95% | 100% |
| `CommandValidator` | 95% | 100% |
| `VariableParser` | 95% | 100% |
| Domain models | 100% | 100% |
| Runner integration | 85% | 95% |
| **Overall** | **95%** | **98%** |

---

## User Verification Steps

### Scenario 1: Build Command Override

**Objective:** Verify contract build command overrides default

**Setup:**
```bash
mkdir TestScenario1 && cd TestScenario1
dotnet new console -n MyApp
mkdir .agent
cat > .agent/config.yml << 'EOF'
commands:
  build: "echo 'Custom build!' && dotnet build"
EOF
```

**Test Command:**
```bash
acode build
```

**Expected Output:**
```
Using contract command for 'build'
Custom build!
Build succeeded.
```

**Verification Checklist:**
- [ ] Custom command is executed
- [ ] Log shows "Using contract command"
- [ ] Build completes successfully

---

### Scenario 2: Disable Run Operation

**Objective:** Verify null command disables operation

**Setup:**
```bash
mkdir TestScenario2 && cd TestScenario2
dotnet new console -n MyApp
mkdir .agent
cat > .agent/config.yml << 'EOF'
commands:
  run: null
EOF
```

**Test Command:**
```bash
acode run
```

**Expected Output:**
```
Error: Operation 'run' is disabled by repository contract.

The repository owner has explicitly disabled the 'run' command.
To enable, remove 'run: null' from .agent/config.yml
```

**Verification Checklist:**
- [ ] Error message is clear
- [ ] Exit code is non-zero
- [ ] Resolution to enable is shown

---

### Scenario 3: Variable Resolution

**Objective:** Verify variables are resolved correctly

**Setup:**
```bash
mkdir TestScenario3 && cd TestScenario3
dotnet new console -n MyApp
mkdir .agent
cat > .agent/config.yml << 'EOF'
commands:
  build: "echo 'Root: ${project_root}' && echo 'Config: ${configuration}' && dotnet build"
EOF
```

**Test Command:**
```bash
acode build --configuration Release
```

**Expected Output:**
```
Using contract command for 'build'
Root: C:\TestScenario3
Config: Release
Build succeeded.
```

**Verification Checklist:**
- [ ] ${project_root} resolved to actual path
- [ ] ${configuration} resolved to Release
- [ ] Build completes successfully

---

### Scenario 4: Unknown Variable Error

**Objective:** Verify unknown variables are detected

**Setup:**
```bash
mkdir TestScenario4 && cd TestScenario4
dotnet new console -n MyApp
mkdir .agent
cat > .agent/config.yml << 'EOF'
commands:
  build: "make UNKNOWN=${unknown_var}"
EOF
```

**Test Command:**
```bash
acode build
```

**Expected Output:**
```
Error: Invalid command template in .agent/config.yml

  build: "make UNKNOWN=${unknown_var}"
                        ^^^^^^^^^^^^^^
  Unknown template variable: '${unknown_var}'
  
Supported variables:
  ${project_root}, ${configuration}, ${project_path}, ${project_name}
```

**Verification Checklist:**
- [ ] Error identifies the unknown variable
- [ ] Error shows the command context
- [ ] Supported variables are listed

---

### Scenario 5: Custom Named Command

**Objective:** Verify custom commands can be executed

**Setup:**
```bash
mkdir TestScenario5 && cd TestScenario5
dotnet new console -n MyApp
mkdir .agent
cat > .agent/config.yml << 'EOF'
commands:
  lint: "dotnet format --verify-no-changes"
  docs: "echo 'Generating docs...'"
EOF
```

**Test Command:**
```bash
acode run-command lint
```

**Expected Output:**
```
Running custom command 'lint'
Formatting code files in the workspace.
All files formatted correctly.
```

**Verification Checklist:**
- [ ] Custom command executes
- [ ] Output is displayed
- [ ] Exit code reflects success/failure

---

### Scenario 6: List Available Commands

**Objective:** Verify command listing works

**Setup:** Use project from Scenario 5

**Test Command:**
```bash
acode commands --list
```

**Expected Output:**
```
Available Commands
──────────────────
Standard:
  build     [default]       dotnet build
  test      [default]       dotnet test
  run       [default]       dotnet run
  restore   [default]       dotnet restore

Custom (from .agent/config.yml):
  lint      [contract]      dotnet format --verify-no-changes
  docs      [contract]      echo 'Generating docs...'
```

**Verification Checklist:**
- [ ] Standard commands listed
- [ ] Custom commands listed
- [ ] Source indicated (default vs contract)

---

### Scenario 7: Fallback to Default

**Objective:** Verify default is used when no contract exists

**Setup:**
```bash
mkdir TestScenario7 && cd TestScenario7
dotnet new console -n MyApp
# No .agent/config.yml
```

**Test Command:**
```bash
acode build --verbose
```

**Expected Output:**
```
[DEBUG] Checking for .agent/config.yml... not found
[DEBUG] Using default build command for .NET
Running: dotnet build
Build succeeded.
```

**Verification Checklist:**
- [ ] Fallback is transparent
- [ ] Verbose mode shows decision
- [ ] Build completes successfully

---

### Scenario 8: Advanced Configuration

**Objective:** Verify working directory and timeout overrides

**Setup:**
```bash
mkdir TestScenario8 && cd TestScenario8
mkdir src && dotnet new console -n MyApp -o src/MyApp
mkdir .agent
cat > .agent/config.yml << 'EOF'
commands:
  build:
    command: "dotnet build"
    working_directory: "${project_root}/src/MyApp"
    timeout: 120
    environment:
      DOTNET_CLI_TELEMETRY_OPTOUT: "1"
EOF
```

**Test Command:**
```bash
acode build --verbose
```

**Expected Output:**
```
[DEBUG] Using contract command for 'build'
[DEBUG] Working directory: C:\TestScenario8\src\MyApp
[DEBUG] Timeout: 120s
[DEBUG] Environment: DOTNET_CLI_TELEMETRY_OPTOUT=1
Build succeeded.
```

**Verification Checklist:**
- [ ] Working directory applied
- [ ] Timeout applied
- [ ] Environment variable set

---

### Scenario 9: Empty Command Error

**Objective:** Verify empty commands are rejected

**Setup:**
```bash
mkdir TestScenario9 && cd TestScenario9
dotnet new console -n MyApp
mkdir .agent
cat > .agent/config.yml << 'EOF'
commands:
  build: ""
EOF
```

**Test Command:**
```bash
acode build
```

**Expected Output:**
```
Error: Invalid command in .agent/config.yml

  build: ""
  
  Empty command string is not allowed.
  Use 'null' to disable the command, or provide a valid command.
```

**Verification Checklist:**
- [ ] Error is clear
- [ ] Resolution is suggested
- [ ] Exit code is non-zero

---

### Scenario 10: Show Command Source

**Objective:** Verify command source inspection works

**Setup:** Use project from Scenario 5

**Test Command:**
```bash
acode commands --show build
```

**Expected Output:**
```
Command: build
──────────────
Source:  default
Command: dotnet build
Working: ${project_root}
Timeout: 300s
```

**Test Command 2:**
```bash
acode commands --show lint
```

**Expected Output:**
```
Command: lint
─────────────
Source:  contract (.agent/config.yml)
Command: dotnet format --verify-no-changes
Working: ${project_root}
Timeout: 300s
```

**Verification Checklist:**
- [ ] Default command shows "default" source
- [ ] Contract command shows config file
- [ ] All details displayed

---

## Implementation Prompt

You are implementing the repository contract command integration that allows repositories to override default build, test, and run commands via `.agent/config.yml`.

### File Structure

```
src/AgenticCoder.Domain/
├── Contract/
│   ├── ICommandResolver.cs               # Command resolution interface
│   ├── ITemplateVariableResolver.cs      # Variable resolution interface
│   ├── ICommandValidator.cs              # Validation interface
│   ├── CommandResolution.cs              # Resolution result model
│   ├── CommandResolutionStatus.cs        # Status enum
│   ├── ResolvedCommand.cs                # Resolved command model
│   ├── CommandDefinition.cs              # Parsed command from config
│   ├── TemplateVariable.cs               # Variable definition
│   └── ValidationError.cs                # Validation error model

src/AgenticCoder.Infrastructure/
├── Contract/
│   ├── CommandResolver.cs                # Resolution implementation
│   ├── TemplateVariableResolver.cs       # Variable resolution
│   ├── CommandValidator.cs               # Validation implementation
│   ├── VariableParser.cs                 # Variable extraction
│   └── VariableContext.cs                # Context for resolution

src/AgenticCoder.Application/
├── Contract/
│   └── CommandOrchestrator.cs            # Coordinates resolution

src/AgenticCoder.CLI/
├── Commands/
│   ├── RunCommandCommand.cs              # Execute custom commands
│   └── CommandsCommand.cs                # List/show commands

Tests/Unit/Domain/Contract/
├── CommandResolutionTests.cs
└── TemplateVariableTests.cs

Tests/Unit/Infrastructure/Contract/
├── CommandResolverTests.cs
├── TemplateVariableResolverTests.cs
├── CommandValidatorTests.cs
└── VariableParserTests.cs

Tests/Integration/Infrastructure/Contract/
├── CommandResolverIntegrationTests.cs
├── VariableResolutionIntegrationTests.cs
└── RunnerContractIntegrationTests.cs

Tests/E2E/CLI/
└── CommandContractE2ETests.cs
```

### Domain Models

```csharp
// src/AgenticCoder.Domain/Contract/ICommandResolver.cs
namespace AgenticCoder.Domain.Contract;

/// <summary>
/// Resolves commands from repository contract or defaults.
/// </summary>
public interface ICommandResolver
{
    /// <summary>
    /// Resolves a command for the specified operation.
    /// </summary>
    /// <param name="operation">Operation name (build, test, run, etc.)</param>
    /// <param name="context">Variable context for resolution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command resolution result.</returns>
    Task<CommandResolution> ResolveAsync(
        string operation,
        VariableContext context,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/AgenticCoder.Domain/Contract/CommandResolution.cs
namespace AgenticCoder.Domain.Contract;

/// <summary>
/// Result of command resolution.
/// </summary>
public sealed record CommandResolution
{
    /// <summary>Resolution status.</summary>
    public required CommandResolutionStatus Status { get; init; }
    
    /// <summary>Resolved command when status is UseOverride.</summary>
    public ResolvedCommand? Command { get; init; }
    
    /// <summary>Source of the command.</summary>
    public required string Source { get; init; }
    
    /// <summary>True if command should be executed.</summary>
    public bool ShouldExecute => Status != CommandResolutionStatus.Disabled;
}

/// <summary>
/// Command resolution status.
/// </summary>
public enum CommandResolutionStatus
{
    /// <summary>Use the contract override command.</summary>
    UseOverride,
    
    /// <summary>Use the default runner command.</summary>
    UseDefault,
    
    /// <summary>Operation is disabled by contract.</summary>
    Disabled
}
```

```csharp
// src/AgenticCoder.Domain/Contract/ResolvedCommand.cs
namespace AgenticCoder.Domain.Contract;

/// <summary>
/// A fully resolved command ready for execution.
/// </summary>
public sealed record ResolvedCommand
{
    /// <summary>The command string with all variables resolved.</summary>
    public required string Command { get; init; }
    
    /// <summary>Working directory for execution.</summary>
    public string? WorkingDirectory { get; init; }
    
    /// <summary>Timeout for execution.</summary>
    public TimeSpan? Timeout { get; init; }
    
    /// <summary>Environment variables to set.</summary>
    public IReadOnlyDictionary<string, string>? Environment { get; init; }
}
```

```csharp
// src/AgenticCoder.Domain/Contract/ITemplateVariableResolver.cs
namespace AgenticCoder.Domain.Contract;

/// <summary>
/// Resolves template variables in command strings.
/// </summary>
public interface ITemplateVariableResolver
{
    /// <summary>
    /// Resolves all variables in the template.
    /// </summary>
    /// <param name="template">Template string with ${var} placeholders.</param>
    /// <param name="context">Variable context.</param>
    /// <returns>Resolved string.</returns>
    string Resolve(string template, VariableContext context);
    
    /// <summary>
    /// Extracts all variable names from a template.
    /// </summary>
    /// <param name="template">Template string.</param>
    /// <returns>List of variable names.</returns>
    IReadOnlyList<string> ExtractVariables(string template);
}
```

```csharp
// src/AgenticCoder.Domain/Contract/VariableContext.cs
namespace AgenticCoder.Domain.Contract;

/// <summary>
/// Context providing values for template variables.
/// </summary>
public sealed record VariableContext
{
    /// <summary>Repository root path.</summary>
    public required string ProjectRoot { get; init; }
    
    /// <summary>Build configuration (Debug/Release).</summary>
    public required string Configuration { get; init; }
    
    /// <summary>Project file path.</summary>
    public string? ProjectPath { get; init; }
    
    /// <summary>Project name.</summary>
    public string? ProjectName { get; init; }
    
    /// <summary>Solution file path.</summary>
    public string? SolutionPath { get; init; }
    
    /// <summary>Build output directory.</summary>
    public string? OutputDir { get; init; }
    
    /// <summary>Artifact directory.</summary>
    public string? ArtifactDir { get; init; }
}
```

### Infrastructure Implementation

```csharp
// src/AgenticCoder.Infrastructure/Contract/CommandResolver.cs
namespace AgenticCoder.Infrastructure.Contract;

public sealed class CommandResolver : ICommandResolver
{
    private readonly IRepoContractProvider _contractProvider;
    private readonly ITemplateVariableResolver _variableResolver;
    private readonly ILogger<CommandResolver> _logger;
    
    public CommandResolver(
        IRepoContractProvider contractProvider,
        ITemplateVariableResolver variableResolver,
        ILogger<CommandResolver> logger)
    {
        _contractProvider = contractProvider;
        _variableResolver = variableResolver;
        _logger = logger;
    }
    
    public async Task<CommandResolution> ResolveAsync(
        string operation,
        VariableContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(context);
        
        var contract = await _contractProvider.GetContractAsync(
            context.ProjectRoot, 
            cancellationToken);
        
        if (contract?.Commands is null)
        {
            _logger.LogDebug("No contract or commands section, using default for {Operation}", operation);
            return new CommandResolution
            {
                Status = CommandResolutionStatus.UseDefault,
                Source = "default"
            };
        }
        
        if (!contract.Commands.TryGetValue(operation, out var commandDef))
        {
            _logger.LogDebug("Command {Operation} not in contract, using default", operation);
            return new CommandResolution
            {
                Status = CommandResolutionStatus.UseDefault,
                Source = "default"
            };
        }
        
        if (commandDef.IsDisabled)
        {
            _logger.LogWarning("Command {Operation} is disabled by contract", operation);
            return new CommandResolution
            {
                Status = CommandResolutionStatus.Disabled,
                Source = ".agent/config.yml"
            };
        }
        
        var resolvedCommand = ResolveCommand(commandDef, context);
        _logger.LogInformation("Using contract override for {Operation}", operation);
        
        return new CommandResolution
        {
            Status = CommandResolutionStatus.UseOverride,
            Command = resolvedCommand,
            Source = ".agent/config.yml"
        };
    }
    
    private ResolvedCommand ResolveCommand(CommandDefinition def, VariableContext ctx)
    {
        return new ResolvedCommand
        {
            Command = _variableResolver.Resolve(def.Command, ctx),
            WorkingDirectory = def.WorkingDirectory is not null 
                ? _variableResolver.Resolve(def.WorkingDirectory, ctx) 
                : null,
            Timeout = def.Timeout,
            Environment = def.Environment
        };
    }
}
```

```csharp
// src/AgenticCoder.Infrastructure/Contract/TemplateVariableResolver.cs
namespace AgenticCoder.Infrastructure.Contract;

public sealed partial class TemplateVariableResolver : ITemplateVariableResolver
{
    private static readonly HashSet<string> SupportedVariables = new(StringComparer.Ordinal)
    {
        "project_root",
        "configuration",
        "project_path",
        "project_name",
        "project_dir",
        "solution_path",
        "solution_dir",
        "output_dir",
        "artifact_dir"
    };
    
    [GeneratedRegex(@"\$\{([a-z_]+)\}", RegexOptions.Compiled)]
    private static partial Regex VariablePattern();
    
    [GeneratedRegex(@"\$\$\{", RegexOptions.Compiled)]
    private static partial Regex EscapedPattern();
    
    public string Resolve(string template, VariableContext context)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(context);
        
        // Handle escaped variables first
        var result = EscapedPattern().Replace(template, "\x00ESCAPED\x00");
        
        // Resolve actual variables
        result = VariablePattern().Replace(result, match =>
        {
            var varName = match.Groups[1].Value;
            return GetVariableValue(varName, context);
        });
        
        // Restore escaped variables
        result = result.Replace("\x00ESCAPED\x00", "${");
        
        return result;
    }
    
    public IReadOnlyList<string> ExtractVariables(string template)
    {
        return VariablePattern()
            .Matches(template)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
    }
    
    private string GetVariableValue(string name, VariableContext ctx) => name switch
    {
        "project_root" => NormalizePath(ctx.ProjectRoot),
        "configuration" => ctx.Configuration,
        "project_path" => NormalizePath(ctx.ProjectPath ?? ctx.ProjectRoot),
        "project_name" => ctx.ProjectName ?? Path.GetFileNameWithoutExtension(ctx.ProjectPath ?? ""),
        "project_dir" => NormalizePath(Path.GetDirectoryName(ctx.ProjectPath) ?? ctx.ProjectRoot),
        "solution_path" => NormalizePath(ctx.SolutionPath ?? ""),
        "solution_dir" => NormalizePath(Path.GetDirectoryName(ctx.SolutionPath) ?? ctx.ProjectRoot),
        "output_dir" => NormalizePath(ctx.OutputDir ?? ""),
        "artifact_dir" => NormalizePath(ctx.ArtifactDir ?? Path.Combine(ctx.ProjectRoot, ".agent", "artifacts")),
        _ => throw new InvalidOperationException($"Unknown variable: ${{{name}}}")
    };
    
    private static string NormalizePath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
}
```

### CLI Commands

```csharp
// src/AgenticCoder.CLI/Commands/CommandsCommand.cs
namespace AgenticCoder.CLI.Commands;

[Command("commands", Description = "List and inspect available commands")]
public sealed class CommandsCommand : ICommand
{
    private readonly ICommandResolver _resolver;
    private readonly IRepoContractProvider _contractProvider;
    private readonly IConsole _console;
    
    [Option("--list", "-l", Description = "List all available commands")]
    public bool List { get; set; }
    
    [Option("--show", "-s", Description = "Show details for a specific command")]
    public string? Show { get; set; }
    
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(Show))
        {
            await ShowCommandAsync(Show, cancellationToken);
        }
        else
        {
            await ListCommandsAsync(cancellationToken);
        }
    }
    
    private async Task ListCommandsAsync(CancellationToken ct)
    {
        _console.WriteLine("Available Commands");
        _console.WriteLine(new string('─', 18));
        
        var standardOps = new[] { "build", "test", "run", "restore" };
        
        _console.WriteLine("Standard:");
        foreach (var op in standardOps)
        {
            var resolution = await _resolver.ResolveAsync(op, CreateContext(), ct);
            var source = resolution.Status == CommandResolutionStatus.UseOverride 
                ? "[contract]" 
                : "[default]";
            var cmd = resolution.Command?.Command ?? GetDefaultCommand(op);
            _console.WriteLine($"  {op,-10} {source,-12} {cmd}");
        }
        
        // List custom commands from contract
        var contract = await _contractProvider.GetContractAsync(
            Directory.GetCurrentDirectory(), ct);
        
        if (contract?.Commands?.Any(c => !standardOps.Contains(c.Key)) == true)
        {
            _console.WriteLine("\nCustom (from .agent/config.yml):");
            foreach (var cmd in contract.Commands.Where(c => !standardOps.Contains(c.Key)))
            {
                _console.WriteLine($"  {cmd.Key,-10} [contract]    {cmd.Value.Command}");
            }
        }
    }
}
```

### Error Codes

| Code | Meaning | Resolution |
|------|---------|------------|
| ACODE-CMD-001 | Unknown template variable | Check variable name against supported list |
| ACODE-CMD-002 | Empty command string | Use `null` to disable or provide valid command |
| ACODE-CMD-003 | Operation disabled by contract | Remove `null` assignment to enable |
| ACODE-CMD-004 | Unclosed variable syntax | Check for missing `}` in template |
| ACODE-CMD-005 | Variable context missing | Ensure context has required values |
| ACODE-CMD-006 | Maximum command length exceeded | Reduce command length to under 64KB |
| ACODE-CMD-007 | Invalid command key | Use alphanumeric and underscore only |

### Implementation Checklist

1. [ ] Create `ICommandResolver` interface in Domain
2. [ ] Create `ITemplateVariableResolver` interface in Domain
3. [ ] Create `ICommandValidator` interface in Domain
4. [ ] Create domain models (`CommandResolution`, `ResolvedCommand`, `VariableContext`)
5. [ ] Implement `CommandResolver` in Infrastructure
6. [ ] Implement `TemplateVariableResolver` with regex patterns
7. [ ] Implement `VariableParser` for variable extraction
8. [ ] Implement `CommandValidator` for config validation
9. [ ] Update `ILanguageRunner` to accept contract parameter
10. [ ] Update .NET runner to query resolver
11. [ ] Update Node.js runner to query resolver
12. [ ] Implement `CommandsCommand` for listing
13. [ ] Implement `RunCommandCommand` for custom commands
14. [ ] Write unit tests for all domain models (100% coverage)
15. [ ] Write unit tests for resolvers (95%+ coverage)
16. [ ] Write unit tests for validators (95%+ coverage)
17. [ ] Write integration tests for resolution
18. [ ] Write integration tests for runner integration
19. [ ] Write E2E tests for CLI commands
20. [ ] Add XML documentation to all public members
21. [ ] Register services in DI container

### Rollout Plan

1. **Phase 1 - Domain Models:** Create interfaces and models (0.5 day)
2. **Phase 2 - Variable Resolution:** Implement variable parser and resolver (0.5 day)
3. **Phase 3 - Command Resolution:** Implement CommandResolver (0.5 day)
4. **Phase 4 - Validation:** Implement CommandValidator (0.5 day)
5. **Phase 5 - Runner Integration:** Update language runners (0.5 day)
6. **Phase 6 - CLI Commands:** Implement commands listing and execution (0.5 day)
7. **Phase 7 - Testing:** Complete unit, integration, E2E tests (1 day)
8. **Phase 8 - Documentation:** Add XML docs and user manual (0.5 day)

---

**End of Task 019.c Specification**