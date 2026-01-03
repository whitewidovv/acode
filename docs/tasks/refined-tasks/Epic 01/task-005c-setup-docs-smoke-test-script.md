# Task 005.c: Setup Docs and Smoke Test Script

**Priority:** P1 – High Priority  
**Tier:** Developer Experience  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 005, Task 005.a, Task 005.b, Task 002  

---

## Description

Task 005.c provides comprehensive setup documentation and automated smoke test scripts for the Ollama provider adapter. This task ensures that developers and operators can quickly configure, verify, and troubleshoot the Ollama integration. While the core adapter functionality is implemented in Tasks 005, 005.a, and 005.b, this task delivers the surrounding documentation and verification tooling that makes the adapter production-ready.

Setup documentation guides users through the complete process of preparing their environment for Ollama-based inference. This includes prerequisites (Ollama installation, model downloading), configuration (endpoint settings, timeout tuning), verification (connectivity checks, model availability), and integration with the broader Acode system. The documentation MUST be comprehensive enough that a user with no prior Ollama experience can achieve a working setup.

The smoke test script provides automated verification that the Ollama integration is functioning correctly. Rather than manual testing, the script exercises the critical paths: connectivity check, model listing, non-streaming completion, streaming completion, and tool calling. Each test produces clear pass/fail output with diagnostic information on failure. The smoke test MUST be runnable from the command line and integrate with CI/CD pipelines.

Documentation addresses multiple audiences with different needs. The quick start guide serves users who want minimal configuration to begin experimenting. The detailed configuration reference serves operators who need to tune the adapter for production workloads. The troubleshooting guide serves users encountering issues. Each section MUST be self-contained while linking to related topics.

The smoke test script exercises specific functionality in isolation to pinpoint failures. If connectivity fails, subsequent tests are skipped with a clear message indicating the root cause. If model listing succeeds but completion fails, the output indicates the specific failure. This structured approach enables rapid diagnosis rather than opaque "something is broken" errors.

Integration with the Acode CLI provides a convenient entry point for smoke tests. The `acode providers smoke-test ollama` command runs the smoke test suite with formatted output. Exit codes follow conventions: 0 for all tests passed, 1 for test failures, 2 for configuration errors. This enables shell scripting and pipeline integration.

The smoke test configuration allows customization for different environments. Users may specify a different model for testing, skip certain tests (e.g., tool calling if not using function-capable models), or adjust timeouts for slower systems. Configuration comes from the same `.agent/config.yml` used by the adapter, ensuring tests reflect actual operational configuration.

Documentation includes version compatibility information. Ollama's API evolves, and the adapter MUST document which Ollama versions are tested and supported. The smoke test optionally verifies Ollama version and warns if the version is outside the tested range. This prevents cryptic failures from API incompatibilities.

Error messages in both documentation and smoke tests are actionable. Rather than generic "connection failed," messages include specific guidance: "Connection to http://localhost:11434 failed. Verify Ollama is running with 'ollama serve' and listening on the configured port." Every documented error includes at least one concrete remediation step.

This task delivers the developer experience layer that transforms a functional adapter into a usable product. Without clear documentation and verification tooling, users struggle with setup, configuration errors persist undetected, and support burden increases. The investment in this task pays dividends across all users of the Ollama integration.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Setup Documentation | Comprehensive guide for Ollama adapter configuration |
| Smoke Test | Quick verification that critical functionality works |
| Quick Start | Minimal steps to achieve working configuration |
| Configuration Reference | Detailed documentation of all settings |
| Troubleshooting Guide | Diagnosis and resolution of common issues |
| Test Script | Automated script executing smoke tests |
| Exit Code | Numeric result indicating test outcome |
| Health Check Test | Verifies basic connectivity to Ollama |
| Model List Test | Verifies model enumeration works |
| Completion Test | Verifies non-streaming inference works |
| Streaming Test | Verifies streaming inference works |
| Tool Call Test | Verifies function calling works |
| Test Report | Structured output summarizing test results |
| Version Check | Verification of Ollama version compatibility |
| Prerequisites | Required software and configuration before setup |
| Model Pull | Downloading a model to local Ollama instance |
| Test Configuration | Settings controlling smoke test behavior |
| CI Integration | Running smoke tests in continuous integration |

---

## Out of Scope

The following items are explicitly excluded from Task 005.c:

- **Integration test suite** - Full integration tests are separate
- **Performance benchmarking** - Performance testing is separate task
- **Automated Ollama installation** - Manual install is prerequisite
- **Model recommendation engine** - Model selection is user choice
- **GUI setup wizard** - CLI-only documentation
- **Video tutorials** - Text documentation only
- **Localization** - English documentation only
- **Ollama troubleshooting** - Focus on Acode integration only
- **Hardware recommendations** - System requirements are Ollama's domain
- **Model fine-tuning docs** - Training documentation not included
- **Multi-node setup** - Single Ollama instance assumed

---

## Functional Requirements

### Setup Documentation

- FR-001: Setup docs MUST be in Markdown format
- FR-002: Setup docs MUST be located in docs/ollama-setup.md
- FR-003: Setup docs MUST include prerequisites section
- FR-004: Setup docs MUST include installation verification section
- FR-005: Setup docs MUST include configuration section
- FR-006: Setup docs MUST include quick start section
- FR-007: Setup docs MUST include troubleshooting section
- FR-008: Setup docs MUST include version compatibility section
- FR-009: Setup docs MUST include links to Ollama documentation
- FR-010: Setup docs MUST include CLI command examples
- FR-011: Setup docs MUST include configuration file examples
- FR-012: Setup docs MUST include error message explanations

### Prerequisites Section

- FR-013: Prerequisites MUST list Ollama installation requirement
- FR-014: Prerequisites MUST specify minimum Ollama version (0.1.23+)
- FR-015: Prerequisites MUST describe model download requirement
- FR-016: Prerequisites MUST list recommended models
- FR-017: Prerequisites MUST explain ollama serve command
- FR-018: Prerequisites MUST include verification command (ollama list)

### Configuration Section

- FR-019: Config section MUST document all provider settings
- FR-020: Config section MUST include default values for each setting
- FR-021: Config section MUST explain environment variable overrides
- FR-022: Config section MUST include complete YAML example
- FR-023: Config section MUST explain timeout tuning
- FR-024: Config section MUST explain retry configuration
- FR-025: Config section MUST explain model mappings

### Quick Start Section

- FR-026: Quick start MUST be under 50 lines
- FR-027: Quick start MUST assume Ollama is running
- FR-028: Quick start MUST show minimal config
- FR-029: Quick start MUST include first command example
- FR-030: Quick start MUST verify success criteria

### Troubleshooting Section

- FR-031: Troubleshooting MUST address connection refused
- FR-032: Troubleshooting MUST address model not found
- FR-033: Troubleshooting MUST address timeout errors
- FR-034: Troubleshooting MUST address memory errors
- FR-035: Troubleshooting MUST address slow generation
- FR-036: Troubleshooting MUST address tool call failures
- FR-037: Each issue MUST include symptoms and resolution
- FR-038: Troubleshooting MUST include diagnostic commands

### Smoke Test Script

- FR-039: Script MUST be PowerShell and Bash compatible
- FR-040: Script MUST be located in scripts/smoke-test-ollama.ps1
- FR-041: Script MUST have Bash equivalent at scripts/smoke-test-ollama.sh
- FR-042: Script MUST check Ollama connectivity
- FR-043: Script MUST verify at least one model available
- FR-044: Script MUST test non-streaming completion
- FR-045: Script MUST test streaming completion
- FR-046: Script MUST test tool calling (if model supports)
- FR-047: Script MUST report pass/fail for each test
- FR-048: Script MUST provide diagnostic output on failure
- FR-049: Script MUST exit with code 0 on success
- FR-050: Script MUST exit with code 1 on test failure
- FR-051: Script MUST exit with code 2 on configuration error

### CLI Integration

- FR-052: CLI MUST expose smoke-test subcommand
- FR-053: `acode providers smoke-test ollama` MUST run tests
- FR-054: CLI MUST display formatted test results
- FR-055: CLI MUST support --verbose flag for details
- FR-056: CLI MUST support --skip-tool-test flag
- FR-057: CLI MUST support --model flag to specify test model
- FR-058: CLI MUST support --timeout flag for slow systems
- FR-059: CLI MUST load config from standard location

### Test Cases

- FR-060: HealthCheck test MUST call /api/tags
- FR-061: HealthCheck test MUST timeout after 5 seconds
- FR-062: ModelList test MUST parse model response
- FR-063: ModelList test MUST verify at least one model
- FR-064: Completion test MUST send simple prompt
- FR-065: Completion test MUST verify non-empty response
- FR-066: Completion test MUST verify finish reason
- FR-067: Streaming test MUST receive multiple chunks
- FR-068: Streaming test MUST verify final chunk
- FR-069: ToolCall test MUST use simple tool definition
- FR-070: ToolCall test MUST verify tool call parsed

### Test Output

- FR-071: Output MUST show test name and result
- FR-072: Output MUST show elapsed time per test
- FR-073: Output MUST show summary at end
- FR-074: Failure output MUST include error message
- FR-075: Failure output MUST include diagnostic hints
- FR-076: Output MUST be parseable (JSON option)
- FR-077: Output MUST support --quiet for CI

### Version Checking

- FR-078: Script MUST check Ollama version if available
- FR-079: Script MUST warn if version below minimum
- FR-080: Script MUST warn if version above tested maximum
- FR-081: Version check failure MUST NOT block tests

### Configuration

- FR-082: Test config MUST support custom endpoint
- FR-083: Test config MUST support custom model
- FR-084: Test config MUST support custom timeout
- FR-085: Test config MUST support skipping tests
- FR-086: Config MUST load from .agent/config.yml
- FR-087: Config MUST support CLI flag overrides

---

## Non-Functional Requirements

### Performance

- NFR-001: Documentation MUST load in < 1 second (web)
- NFR-002: Full smoke test suite MUST complete in < 60 seconds
- NFR-003: Individual tests MUST timeout appropriately
- NFR-004: Script startup MUST be < 1 second

### Reliability

- NFR-005: Scripts MUST work on Windows, macOS, Linux
- NFR-006: Scripts MUST handle missing Ollama gracefully
- NFR-007: Scripts MUST not leave orphan processes
- NFR-008: Documentation MUST be accurate and tested

### Usability

- NFR-009: Documentation MUST be understandable by beginners
- NFR-010: Error messages MUST be actionable
- NFR-011: Quick start MUST work first time for most users
- NFR-012: Test output MUST be human-readable

### Maintainability

- NFR-013: Documentation MUST be kept in sync with code
- NFR-014: Scripts MUST be easy to extend with new tests
- NFR-015: Version requirements MUST be updated with releases
- NFR-016: Links MUST be validated periodically

### Accessibility

- NFR-017: Documentation MUST use proper heading hierarchy
- NFR-018: Code examples MUST be copyable
- NFR-019: Error messages MUST not use color-only indicators

---

## User Manual Documentation

### Overview

This task provides the documentation and verification tools for the Ollama provider adapter. Users should refer to this documentation for setup, configuration, and troubleshooting.

### Quick Start

Get Acode working with Ollama in 5 minutes:

```bash
# 1. Install Ollama (if not already installed)
# Visit https://ollama.ai and follow instructions

# 2. Start Ollama
ollama serve

# 3. Pull a model
ollama pull llama3.2:8b

# 4. Create minimal config
cat > .agent/config.yml << 'EOF'
model:
  default_provider: ollama
  providers:
    ollama:
      enabled: true
      endpoint: http://localhost:11434
EOF

# 5. Verify setup
acode providers smoke-test ollama

# 6. Run first command
acode ask "What is 2+2?"
```

### Smoke Test Usage

Run the smoke test to verify Ollama integration:

```bash
# Basic smoke test
acode providers smoke-test ollama

# With verbose output
acode providers smoke-test ollama --verbose

# Using specific model
acode providers smoke-test ollama --model codellama:13b

# Skip tool calling test
acode providers smoke-test ollama --skip-tool-test

# Increase timeout for slow systems
acode providers smoke-test ollama --timeout 120
```

### Expected Smoke Test Output

```
$ acode providers smoke-test ollama

Ollama Provider Smoke Test
===========================

[1/5] Health Check............ PASS (45ms)
[2/5] Model List.............. PASS (120ms)
      Found: llama3.2:8b, codellama:13b
[3/5] Non-Streaming Completion PASS (2.3s)
[4/5] Streaming Completion.... PASS (1.8s)
[5/5] Tool Calling............ PASS (3.1s)

===========================
All tests passed (5/5)
Total time: 7.4s
```

### Failed Test Output

```
$ acode providers smoke-test ollama

Ollama Provider Smoke Test
===========================

[1/5] Health Check............ FAIL
      Error: Connection refused
      
      Possible causes:
      - Ollama is not running
      - Wrong endpoint configured
      
      Try:
      1. Start Ollama: ollama serve
      2. Check endpoint in config: http://localhost:11434
      3. Test manually: curl http://localhost:11434/api/tags

Stopping: Cannot proceed without connectivity
===========================
Tests failed (0/5, 1 error)
```

### Configuration for Testing

The smoke test uses configuration from `.agent/config.yml`:

```yaml
model:
  providers:
    ollama:
      endpoint: http://localhost:11434
      default_model: llama3.2:8b
      request_timeout_seconds: 60
      
      # Smoke test specific (optional)
      smoke_test:
        model: llama3.2:8b
        skip_tool_test: false
        timeout_seconds: 120
```

### Standalone Script Usage

For environments without Acode CLI:

```powershell
# PowerShell
.\scripts\smoke-test-ollama.ps1 -Endpoint "http://localhost:11434" -Model "llama3.2:8b"

# Bash
./scripts/smoke-test-ollama.sh --endpoint "http://localhost:11434" --model "llama3.2:8b"
```

### CI/CD Integration

```yaml
# GitHub Actions example
- name: Smoke Test Ollama
  run: |
    acode providers smoke-test ollama --quiet
  continue-on-error: false
```

Exit codes:
- `0`: All tests passed
- `1`: One or more tests failed
- `2`: Configuration or setup error

### Version Compatibility

| Ollama Version | Status | Notes |
|---------------|--------|-------|
| 0.1.23+ | Tested | Recommended minimum |
| 0.1.30+ | Tested | Tool calling improved |
| 0.2.0+ | Tested | Latest stable |

### Troubleshooting Reference

| Symptom | Cause | Resolution |
|---------|-------|------------|
| Connection refused | Ollama not running | Run `ollama serve` |
| Model not found | Model not pulled | Run `ollama pull <model>` |
| Timeout | Model loading slow | Wait or increase timeout |
| Out of memory | Model too large | Use smaller/quantized model |
| Tool call fail | Model lacks support | Use function-capable model |

---

## Acceptance Criteria

### Setup Documentation

- [ ] AC-001: Markdown format
- [ ] AC-002: Located at docs/ollama-setup.md
- [ ] AC-003: Prerequisites section exists
- [ ] AC-004: Installation verification section exists
- [ ] AC-005: Configuration section exists
- [ ] AC-006: Quick start section exists
- [ ] AC-007: Troubleshooting section exists
- [ ] AC-008: Version compatibility section exists
- [ ] AC-009: Links to Ollama docs
- [ ] AC-010: CLI examples included
- [ ] AC-011: Config file examples included
- [ ] AC-012: Error explanations included

### Prerequisites Content

- [ ] AC-013: Ollama installation listed
- [ ] AC-014: Minimum version specified
- [ ] AC-015: Model download explained
- [ ] AC-016: Recommended models listed
- [ ] AC-017: ollama serve explained
- [ ] AC-018: Verification command included

### Configuration Content

- [ ] AC-019: All settings documented
- [ ] AC-020: Default values listed
- [ ] AC-021: Env var overrides explained
- [ ] AC-022: Complete YAML example
- [ ] AC-023: Timeout tuning explained
- [ ] AC-024: Retry config explained
- [ ] AC-025: Model mappings explained

### Quick Start Content

- [ ] AC-026: Under 50 lines
- [ ] AC-027: Assumes Ollama running
- [ ] AC-028: Minimal config shown
- [ ] AC-029: First command shown
- [ ] AC-030: Success criteria defined

### Troubleshooting Content

- [ ] AC-031: Connection refused addressed
- [ ] AC-032: Model not found addressed
- [ ] AC-033: Timeout errors addressed
- [ ] AC-034: Memory errors addressed
- [ ] AC-035: Slow generation addressed
- [ ] AC-036: Tool call failures addressed
- [ ] AC-037: Symptoms and resolutions
- [ ] AC-038: Diagnostic commands

### Smoke Test Script

- [ ] AC-039: PowerShell compatible
- [ ] AC-040: Located at scripts/smoke-test-ollama.ps1
- [ ] AC-041: Bash equivalent exists
- [ ] AC-042: Checks connectivity
- [ ] AC-043: Verifies model available
- [ ] AC-044: Tests non-streaming
- [ ] AC-045: Tests streaming
- [ ] AC-046: Tests tool calling
- [ ] AC-047: Reports pass/fail
- [ ] AC-048: Diagnostic output on failure
- [ ] AC-049: Exit code 0 on success
- [ ] AC-050: Exit code 1 on failure
- [ ] AC-051: Exit code 2 on config error

### CLI Integration

- [ ] AC-052: smoke-test subcommand exists
- [ ] AC-053: ollama argument works
- [ ] AC-054: Formatted output
- [ ] AC-055: --verbose flag works
- [ ] AC-056: --skip-tool-test works
- [ ] AC-057: --model flag works
- [ ] AC-058: --timeout flag works
- [ ] AC-059: Loads config correctly

### Test Cases

- [ ] AC-060: HealthCheck calls /api/tags
- [ ] AC-061: HealthCheck times out
- [ ] AC-062: ModelList parses response
- [ ] AC-063: ModelList verifies models
- [ ] AC-064: Completion sends prompt
- [ ] AC-065: Completion verifies response
- [ ] AC-066: Completion verifies finish
- [ ] AC-067: Streaming receives chunks
- [ ] AC-068: Streaming verifies final
- [ ] AC-069: ToolCall uses definition
- [ ] AC-070: ToolCall verifies parse

### Test Output

- [ ] AC-071: Shows test name/result
- [ ] AC-072: Shows elapsed time
- [ ] AC-073: Shows summary
- [ ] AC-074: Failure shows error
- [ ] AC-075: Failure shows hints
- [ ] AC-076: JSON output option
- [ ] AC-077: --quiet works

### Version Check

- [ ] AC-078: Checks Ollama version
- [ ] AC-079: Warns below minimum
- [ ] AC-080: Warns above maximum
- [ ] AC-081: Doesn't block tests

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/CLI/SmokeTest/
├── SmokeTestRunnerTests.cs
│   ├── Should_Run_All_Tests()
│   ├── Should_Stop_On_Connectivity_Failure()
│   ├── Should_Skip_Tool_Test_When_Flagged()
│   └── Should_Report_Summary()
│
├── HealthCheckTestTests.cs
│   ├── Should_Pass_On_Success()
│   ├── Should_Fail_On_Timeout()
│   └── Should_Fail_On_ConnectionRefused()
│
└── TestOutputFormatterTests.cs
    ├── Should_Format_Pass()
    ├── Should_Format_Fail()
    └── Should_Format_Summary()
```

### Integration Tests

```
Tests/Integration/SmokeTest/
├── SmokeTestIntegrationTests.cs
│   ├── Should_Complete_All_Tests()
│   └── Should_Handle_No_Ollama()
```

### Documentation Tests

```
Tests/Docs/
├── DocumentationLinkTests.cs
│   └── Should_Have_Valid_Links()
```

---

## User Verification Steps

### Scenario 1: Run Smoke Test

1. Start Ollama with model loaded
2. Run `acode providers smoke-test ollama`
3. Verify all 5 tests pass
4. Verify summary shows 5/5

### Scenario 2: Handle Missing Ollama

1. Stop Ollama
2. Run smoke test
3. Verify health check fails
4. Verify helpful error message
5. Verify exit code 1

### Scenario 3: Skip Tool Test

1. Run `acode providers smoke-test ollama --skip-tool-test`
2. Verify only 4 tests run
3. Verify tool test skipped message

### Scenario 4: Custom Model

1. Run `acode providers smoke-test ollama --model codellama:13b`
2. Verify tests use specified model

### Scenario 5: Verbose Output

1. Run with --verbose
2. Verify additional diagnostic info shown
3. Verify request/response details shown

### Scenario 6: JSON Output

1. Run `acode providers smoke-test ollama --output json`
2. Verify valid JSON output
3. Verify parseable in jq

### Scenario 7: CI Mode

1. Run with --quiet
2. Verify minimal output
3. Verify exit code reflects result

### Scenario 8: Documentation Accuracy

1. Follow quick start guide
2. Verify each step works
3. Verify result matches expected

---

## Implementation Prompt

### File Structure

```
docs/
├── ollama-setup.md
└── providers/
    └── ollama-troubleshooting.md

scripts/
├── smoke-test-ollama.ps1
└── smoke-test-ollama.sh

src/AgenticCoder.CLI/Commands/Providers/
└── SmokeTestCommand.cs

src/AgenticCoder.Infrastructure/Ollama/SmokeTest/
├── OllamaSmokeTestRunner.cs
├── Tests/
│   ├── HealthCheckTest.cs
│   ├── ModelListTest.cs
│   ├── CompletionTest.cs
│   ├── StreamingTest.cs
│   └── ToolCallTest.cs
└── Output/
    ├── TestResult.cs
    └── TestReporter.cs
```

### SmokeTestCommand Implementation

```csharp
namespace AgenticCoder.CLI.Commands.Providers;

[Command("providers smoke-test")]
public class SmokeTestCommand : ICommand
{
    [Argument(0)]
    public string Provider { get; set; } = "ollama";
    
    [Option("--verbose")]
    public bool Verbose { get; set; }
    
    [Option("--skip-tool-test")]
    public bool SkipToolTest { get; set; }
    
    [Option("--model")]
    public string? Model { get; set; }
    
    [Option("--timeout")]
    public int TimeoutSeconds { get; set; } = 60;
    
    [Option("--output")]
    public string OutputFormat { get; set; } = "text";
    
    public async Task<int> ExecuteAsync()
    {
        var runner = new OllamaSmokeTestRunner(_config);
        var options = new SmokeTestOptions
        {
            Model = Model,
            SkipToolTest = SkipToolTest,
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
        };
        
        var results = await runner.RunAsync(options);
        
        var reporter = OutputFormat == "json"
            ? new JsonTestReporter()
            : new TextTestReporter(Verbose);
        
        reporter.Report(results);
        
        return results.AllPassed ? 0 : 1;
    }
}
```

### PowerShell Script Template

```powershell
# smoke-test-ollama.ps1
param(
    [string]$Endpoint = "http://localhost:11434",
    [string]$Model = "llama3.2:8b",
    [switch]$SkipToolTest,
    [int]$TimeoutSeconds = 60
)

function Test-Health {
    Write-Host "[1/5] Health Check..." -NoNewline
    try {
        $response = Invoke-RestMethod -Uri "$Endpoint/api/tags" -TimeoutSec 5
        Write-Host " PASS" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host " FAIL" -ForegroundColor Red
        Write-Host "      Error: $($_.Exception.Message)"
        return $false
    }
}

# Additional tests...

$results = @()
if (-not (Test-Health)) {
    Write-Host "Stopping: Cannot proceed without connectivity"
    exit 1
}

# Run remaining tests...
```

### Implementation Checklist

1. [ ] Create docs/ollama-setup.md
2. [ ] Write prerequisites section
3. [ ] Write configuration section
4. [ ] Write quick start section
5. [ ] Write troubleshooting section
6. [ ] Create smoke-test-ollama.ps1
7. [ ] Create smoke-test-ollama.sh
8. [ ] Implement OllamaSmokeTestRunner
9. [ ] Implement individual test classes
10. [ ] Implement test reporter
11. [ ] Add CLI command
12. [ ] Write unit tests
13. [ ] Validate documentation accuracy

### Verification Command

```bash
# Run smoke test
acode providers smoke-test ollama

# Verify documentation
mdl docs/ollama-setup.md  # Markdown linting
```

---

**End of Task 005.c Specification**