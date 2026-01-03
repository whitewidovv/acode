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

### Assumptions

1. At least one of .NET SDK or Node.js is installed on the system
2. The repository contains valid project files (.sln, .csproj, package.json)
3. Project files are in standard locations (not deeply nested in unusual paths)
4. Build and test commands complete in reasonable time
5. Package registries (NuGet, npm) are accessible (or offline caches exist)
6. Environment variables for SDKs are properly configured
7. Project files are well-formed (valid XML/JSON)
8. The agent has permission to execute build tools
9. Working directory is set correctly before runner invocation
10. Dependencies listed in project files are resolvable

### Security Considerations

Runners execute external build tools which can have significant system access:

1. **Trusted Build Tools:** Only invoke well-known tools (dotnet, npm, yarn). Never execute arbitrary scripts without user confirmation.

2. **Package Verification:** When restoring packages, rely on registry security (npm audit, NuGet signatures). Report security warnings.

3. **Script Execution:** For npm scripts, warn if scripts seem suspicious (curl | bash patterns, obfuscated code).

4. **Environment Isolation:** Build tools should run with minimum necessary permissions. Consider Docker sandbox for untrusted code.

5. **Credential Protection:** Build tools may require credentials (private registries). Never log credentials, use secure storage.

6. **Network Restriction:** In air-gapped mode, builds must use only cached packages. Runners must respect mode restrictions.

7. **Output Sanitization:** Build output may contain sensitive paths or values. Sanitize before logging.

8. **Transitive Dependencies:** Runners cannot verify all transitive dependencies. Recommend `npm audit` and similar tools.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Runner | Language-specific executor |
| .NET | Microsoft's development platform |
| Node.js | JavaScript runtime |
| npm | Node package manager |
| yarn | Alternative package manager |
| Solution | .NET .sln file |
| Project | .NET .csproj/.fsproj file |
| package.json | Node.js manifest |
| Build | Compile source code |
| Test | Execute test suite |
| Run | Start application |
| Restore | Download dependencies |
| Script | package.json command |

---

## Out of Scope

The following items are explicitly excluded from Task 019:

- **Project detection details** - See Task 019.a
- **Test wrapper details** - See Task 019.b
- **Repo contract integration** - See Task 019.c
- **Python/Go/Rust runners** - Future versions
- **IDE integration** - CLI only
- **Hot reload** - Standard execution
- **Package publishing** - Local only
- **Debugging** - No debugger support

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

## Testing Requirements

### Unit Tests

#### Runner Registry Tests (`Tests/Unit/Runners/RunnerRegistryTests.cs`)

- `RunnerRegistry_Register_AddsRunner`
- `RunnerRegistry_Register_DuplicateLanguage_Throws`
- `RunnerRegistry_GetRunner_ReturnsCorrectRunner`
- `RunnerRegistry_GetRunner_UnknownLanguage_ReturnsNull`
- `RunnerRegistry_GetRunnerForPath_MatchesPatterns`
- `RunnerRegistry_GetRunnerForPath_NoMatch_ReturnsNull`
- `RunnerRegistry_GetRunnerForPath_MultipleMatch_ReturnsHighestPriority`
- `RunnerRegistry_GetAllRunners_ReturnsAll`
- `RunnerRegistry_DetectAll_ReturnsAllMatches`
- `RunnerRegistry_AutoRegistersBuiltInRunners`
- `RunnerRegistry_CachesDetectionResults`
- `RunnerRegistry_InvalidatesCacheOnFileChange`

#### .NET Runner Tests (`Tests/Unit/Runners/DotNetRunnerTests.cs`)

- `DotNetRunner_Language_ReturnsDotNet`
- `DotNetRunner_FilePatterns_IncludesSln`
- `DotNetRunner_FilePatterns_IncludesCsproj`
- `DotNetRunner_FilePatterns_IncludesFsproj`
- `DotNetRunner_IsAvailable_TrueWhenDotNetExists`
- `DotNetRunner_IsAvailable_FalseWhenDotNetMissing`
- `DotNetRunner_GetVersion_ReturnsCorrectVersion`
- `DotNetRunner_DetectAsync_FindsSolution`
- `DotNetRunner_DetectAsync_FindsProject`
- `DotNetRunner_DetectAsync_PrefersSolutionOverProject`
- `DotNetRunner_DetectAsync_HandlesNested`
- `DotNetRunner_BuildAsync_ConstructsCorrectCommand`
- `DotNetRunner_BuildAsync_IncludesConfiguration`
- `DotNetRunner_BuildAsync_IncludesVerbosity`
- `DotNetRunner_BuildAsync_IncludesNoRestore`
- `DotNetRunner_BuildAsync_ParsesErrors`
- `DotNetRunner_BuildAsync_ParsesWarnings`
- `DotNetRunner_TestAsync_ConstructsCorrectCommand`
- `DotNetRunner_TestAsync_IncludesFilter`
- `DotNetRunner_TestAsync_ParsesResults`
- `DotNetRunner_TestAsync_CapturesFailures`
- `DotNetRunner_RunAsync_ConstructsCorrectCommand`
- `DotNetRunner_RunAsync_PassesArguments`
- `DotNetRunner_RestoreAsync_ConstructsCorrectCommand`

#### .NET Output Parser Tests (`Tests/Unit/Runners/DotNetOutputParserTests.cs`)

- `DotNetOutputParser_ParseBuild_ExtractsErrors`
- `DotNetOutputParser_ParseBuild_ExtractsWarnings`
- `DotNetOutputParser_ParseBuild_ExtractsFile`
- `DotNetOutputParser_ParseBuild_ExtractsLine`
- `DotNetOutputParser_ParseBuild_ExtractsColumn`
- `DotNetOutputParser_ParseBuild_ExtractsCode`
- `DotNetOutputParser_ParseBuild_ExtractsMessage`
- `DotNetOutputParser_ParseTest_ExtractsPassCount`
- `DotNetOutputParser_ParseTest_ExtractsFailCount`
- `DotNetOutputParser_ParseTest_ExtractsSkipCount`
- `DotNetOutputParser_ParseTest_ExtractsTestNames`
- `DotNetOutputParser_ParseTest_ExtractsFailureMessages`
- `DotNetOutputParser_HandlesMalformedOutput`

#### Node.js Runner Tests (`Tests/Unit/Runners/NodeRunnerTests.cs`)

- `NodeRunner_Language_ReturnsNode`
- `NodeRunner_FilePatterns_IncludesPackageJson`
- `NodeRunner_IsAvailable_TrueWhenNodeExists`
- `NodeRunner_IsAvailable_FalseWhenNodeMissing`
- `NodeRunner_GetVersion_ReturnsCorrectVersion`
- `NodeRunner_DetectAsync_FindsPackageJson`
- `NodeRunner_DetectAsync_DetectsNpm`
- `NodeRunner_DetectAsync_DetectsYarn`
- `NodeRunner_DetectAsync_DetectsPnpm`
- `NodeRunner_DetectAsync_HandlesWorkspaces`
- `NodeRunner_BuildAsync_ConstructsCorrectCommand`
- `NodeRunner_BuildAsync_ChecksScriptExists`
- `NodeRunner_BuildAsync_UsesCorrectPackageManager`
- `NodeRunner_BuildAsync_ParsesErrors`
- `NodeRunner_TestAsync_ConstructsCorrectCommand`
- `NodeRunner_TestAsync_ChecksScriptExists`
- `NodeRunner_TestAsync_ParsesJestOutput`
- `NodeRunner_TestAsync_ParsesMochaOutput`
- `NodeRunner_RunAsync_ConstructsCorrectCommand`
- `NodeRunner_RunAsync_SupportsDevScript`
- `NodeRunner_RestoreAsync_ConstructsCorrectCommand`
- `NodeRunner_RestoreAsync_SupportsProduction`

#### Node.js Output Parser Tests (`Tests/Unit/Runners/NodeOutputParserTests.cs`)

- `NodeOutputParser_ParseBuild_ExtractsErrors`
- `NodeOutputParser_ParseBuild_HandlesEresolve`
- `NodeOutputParser_ParseBuild_HandlesEnoent`
- `NodeOutputParser_ParseTest_ExtractsJestResults`
- `NodeOutputParser_ParseTest_ExtractsMochaResults`
- `NodeOutputParser_ParseTest_ExtractsPassCount`
- `NodeOutputParser_ParseTest_ExtractsFailCount`
- `NodeOutputParser_ParseTest_ExtractsTestNames`
- `NodeOutputParser_HandlesMalformedOutput`

#### RunnerResult Tests (`Tests/Unit/Runners/RunnerResultTests.cs`)

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