# Task 019: Language Runners (.NET + JS)

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 018 (Command Runner), Task 002 (Config Contract)  

---

## Description

Task 019 implements language-specific runners for .NET and JavaScript. Runners understand project conventions. They execute build, test, and run commands correctly.

Language runners encapsulate ecosystem knowledge. .NET projects use `dotnet` CLI. Node.js projects use `npm` or `yarn`. Each has conventions for building and testing.

The agent needs unified abstractions. `Build()` works regardless of language. `Test()` works regardless of framework. Runners translate to specific commands.

.NET runner handles .NET projects. Solutions (.sln), projects (.csproj, .fsproj), executables. Build with `dotnet build`. Test with `dotnet test`.

JavaScript runner handles Node.js projects. Package.json defines scripts. Dependencies via npm or yarn. Test with `npm test` or configured command.

Project detection determines the appropriate runner. Find .sln or .csproj for .NET. Find package.json for Node.js. Multiple projects can coexist.

Runners integrate with Task 018 command execution. They construct commands. Task 018 executes them. Output is captured consistently.

Runners read repo contracts from Task 002. The config can override default commands. Custom build scripts are supported.

Error handling interprets language-specific output. .NET errors have specific formats. npm errors have different formats. Runners parse and normalize.

Task 019.a handles project layout detection. Task 019.b implements test wrappers. Task 019.c integrates repo contract commands.

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

### Runner Interface

- FR-001: Define ILanguageRunner interface
- FR-002: Language property
- FR-003: FilePatterns property
- FR-004: DetectAsync method
- FR-005: BuildAsync method
- FR-006: TestAsync method
- FR-007: RunAsync method
- FR-008: RestoreAsync method

### Runner Registry

- FR-009: Define IRunnerRegistry interface
- FR-010: Register runner method
- FR-011: Get runner for path method
- FR-012: Get all runners method
- FR-013: Auto-register built-in runners
- FR-014: Priority ordering

### .NET Runner

- FR-015: Detect .sln files
- FR-016: Detect .csproj files
- FR-017: Detect .fsproj files
- FR-018: Build with dotnet build
- FR-019: Test with dotnet test
- FR-020: Run with dotnet run
- FR-021: Restore with dotnet restore
- FR-022: Support configuration (Debug/Release)
- FR-023: Support verbosity
- FR-024: Parse build errors
- FR-025: Parse test results

### JavaScript Runner

- FR-026: Detect package.json
- FR-027: Detect npm vs yarn
- FR-028: Build with npm run build
- FR-029: Test with npm test
- FR-030: Run with npm start
- FR-031: Restore with npm install
- FR-032: Support custom scripts
- FR-033: Parse npm errors
- FR-034: Parse test output

### Command Construction

- FR-035: Build command strings
- FR-036: Add arguments
- FR-037: Set working directory
- FR-038: Set environment
- FR-039: Configure timeout

### Output Parsing

- FR-040: Define IOutputParser interface
- FR-041: Parse build output
- FR-042: Parse test output
- FR-043: Extract errors
- FR-044: Extract warnings
- FR-045: Normalize format

### Result Model

- FR-046: Define RunnerResult record
- FR-047: Include success flag
- FR-048: Include error list
- FR-049: Include warning list
- FR-050: Include test results

---

## Non-Functional Requirements

### Performance

- NFR-001: Detection < 100ms
- NFR-002: Command construction < 10ms
- NFR-003: Parsing < 50ms

### Reliability

- NFR-004: Handle missing SDKs
- NFR-005: Handle invalid projects
- NFR-006: Graceful fallback

### Compatibility

- NFR-007: Multiple .NET versions
- NFR-008: Multiple Node versions
- NFR-009: Cross-platform paths

---

## User Manual Documentation

### Overview

Language runners execute build, test, and run commands for .NET and JavaScript projects. They understand project conventions and provide unified abstractions.

### Configuration

```yaml
# .agent/config.yml
runners:
  dotnet:
    # Enable .NET runner
    enabled: true
    
    # Default configuration
    configuration: Debug
    
    # Default verbosity
    verbosity: minimal
    
    # Custom dotnet path
    path: null
    
  node:
    # Enable Node runner
    enabled: true
    
    # Package manager: npm, yarn, pnpm
    package_manager: npm
    
    # Custom node path
    path: null
```

### Supported Operations

| Operation | .NET | Node.js |
|-----------|------|---------|
| Detect | ✅ | ✅ |
| Build | dotnet build | npm run build |
| Test | dotnet test | npm test |
| Run | dotnet run | npm start |
| Restore | dotnet restore | npm install |

### CLI Commands

```bash
# Detect project type
acode project detect

# Build project
acode build

# Build with configuration
acode build --configuration Release

# Test project
acode test

# Test with verbosity
acode test --verbosity detailed

# Run project
acode run

# Restore dependencies
acode restore
```

### Detection Output

```bash
$ acode project detect

Detected Projects:
  1. MyApp.sln (dotnet)
     ├── src/MyApp/MyApp.csproj
     ├── src/MyApp.Core/MyApp.Core.csproj
     └── tests/MyApp.Tests/MyApp.Tests.csproj

  2. frontend (node)
     └── frontend/package.json
```

### Build Output

```bash
$ acode build

Building MyApp.sln...
  MyApp.Core -> bin/Debug/net8.0/MyApp.Core.dll
  MyApp -> bin/Debug/net8.0/MyApp.dll

Build succeeded.
  0 Warning(s)
  0 Error(s)
```

### Test Output

```bash
$ acode test

Running tests for MyApp.Tests...

Passed!  - Failed: 0, Passed: 45, Skipped: 2, Total: 47
Duration: 3.2 s
```

### Troubleshooting

#### SDK Not Found

**Problem:** dotnet SDK not installed

**Solutions:**
1. Install .NET SDK
2. Add to PATH
3. Configure custom path

#### npm Not Found

**Problem:** Node.js not installed

**Solutions:**
1. Install Node.js
2. Add to PATH
3. Configure custom path

#### Build Failed

**Problem:** Build errors

**Solutions:**
1. Check error messages
2. Restore dependencies first
3. Check SDK version compatibility

---

## Acceptance Criteria

### Interface

- [ ] AC-001: ILanguageRunner defined
- [ ] AC-002: IRunnerRegistry defined
- [ ] AC-003: All methods implemented

### .NET Runner

- [ ] AC-004: Detection works
- [ ] AC-005: Build works
- [ ] AC-006: Test works
- [ ] AC-007: Run works

### Node Runner

- [ ] AC-008: Detection works
- [ ] AC-009: Build works
- [ ] AC-010: Test works
- [ ] AC-011: Run works

### Output

- [ ] AC-012: Errors parsed
- [ ] AC-013: Warnings parsed
- [ ] AC-014: Results normalized

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Runners/
├── RunnerRegistryTests.cs
│   ├── Should_Register_Runner()
│   └── Should_Get_By_Path()
│
├── DotNetRunnerTests.cs
│   ├── Should_Detect_Solution()
│   ├── Should_Detect_Project()
│   ├── Should_Build()
│   └── Should_Parse_Errors()
│
└── NodeRunnerTests.cs
    ├── Should_Detect_Package_Json()
    ├── Should_Build()
    └── Should_Parse_Errors()
```

### Integration Tests

```
Tests/Integration/Runners/
├── DotNetRunnerIntegrationTests.cs
│   └── Should_Build_Real_Project()
│
└── NodeRunnerIntegrationTests.cs
    └── Should_Build_Real_Project()
```

### E2E Tests

```
Tests/E2E/Runners/
├── RunnerE2ETests.cs
│   ├── Should_Build_Via_CLI()
│   └── Should_Test_Via_CLI()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Detection | 50ms | 100ms |
| Command construction | 5ms | 10ms |
| Output parsing | 25ms | 50ms |

---

## User Verification Steps

### Scenario 1: Detect .NET

1. Create .NET project
2. Run `acode project detect`
3. Verify: .NET detected

### Scenario 2: Build .NET

1. Create .NET project
2. Run `acode build`
3. Verify: Build succeeds

### Scenario 3: Test .NET

1. Create .NET test project
2. Run `acode test`
3. Verify: Tests run

### Scenario 4: Build Node

1. Create Node.js project
2. Run `acode build`
3. Verify: Build succeeds

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Runners/
│   ├── ILanguageRunner.cs
│   ├── RunnerResult.cs
│   └── ProjectInfo.cs
│
src/AgenticCoder.Application/
├── Runners/
│   └── IRunnerRegistry.cs
│
src/AgenticCoder.Infrastructure/
├── Runners/
│   ├── RunnerRegistry.cs
│   ├── DotNetRunner.cs
│   ├── NodeRunner.cs
│   └── OutputParsers/
│       ├── DotNetOutputParser.cs
│       └── NodeOutputParser.cs
```

### ILanguageRunner Interface

```csharp
namespace AgenticCoder.Domain.Runners;

public interface ILanguageRunner
{
    string Language { get; }
    string[] FilePatterns { get; }
    
    Task<bool> DetectAsync(string path, CancellationToken ct = default);
    Task<RunnerResult> BuildAsync(string path, BuildOptions? options = null, CancellationToken ct = default);
    Task<RunnerResult> TestAsync(string path, TestOptions? options = null, CancellationToken ct = default);
    Task<RunnerResult> RunAsync(string path, RunOptions? options = null, CancellationToken ct = default);
    Task<RunnerResult> RestoreAsync(string path, CancellationToken ct = default);
}
```

### DotNetRunner Class

```csharp
public class DotNetRunner : ILanguageRunner
{
    public string Language => "dotnet";
    public string[] FilePatterns => new[] { "*.sln", "*.csproj", "*.fsproj" };
    
    public async Task<bool> DetectAsync(string path, CancellationToken ct)
    {
        return Directory.GetFiles(path, "*.sln").Any()
            || Directory.GetFiles(path, "*.csproj").Any();
    }
    
    public async Task<RunnerResult> BuildAsync(string path, BuildOptions? options, CancellationToken ct)
    {
        var command = new Command
        {
            Executable = "dotnet",
            Arguments = new[] { "build", path, "-c", options?.Configuration ?? "Debug" },
            WorkingDirectory = path
        };
        
        var result = await _executor.ExecuteAsync(command, ct: ct);
        return _parser.ParseBuildOutput(result);
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-RUN-001 | Runner not found |
| ACODE-RUN-002 | SDK not found |
| ACODE-RUN-003 | Build failed |
| ACODE-RUN-004 | Test failed |

### Implementation Checklist

1. [ ] Create runner interface
2. [ ] Create runner registry
3. [ ] Implement .NET runner
4. [ ] Implement Node runner
5. [ ] Add output parsers
6. [ ] Add CLI commands
7. [ ] Add configuration
8. [ ] Add unit tests

### Rollout Plan

1. **Phase 1:** Runner interface
2. **Phase 2:** Registry
3. **Phase 3:** .NET runner
4. **Phase 4:** Node runner
5. **Phase 5:** CLI integration

---

**End of Task 019 Specification**