# Task 005c - Missing Deliverables Implementation Plan

## CRITICAL ISSUE DISCOVERED

During fresh gap analysis in audit (as required by CLAUDE.md Section 3.2), discovered that I implemented only ~60% of task requirements. I focused on the C# smoke test infrastructure and CLI integration but **completely missed** the documentation and standalone scripts.

## What Was Implemented (✅ Complete)

- ✅ C# smoke test infrastructure (TestResult, ITestReporter, ISmokeTest, OllamaSmokeTestRunner)
- ✅ CLI Integration: `acode providers smoke-test ollama`
- ✅ All flags: --endpoint, --model, --timeout, --skip-tool-test, --verbose
- ✅ Test Cases: Health, Model, Completion, Streaming, ToolCall (FR-060 to FR-075)
- ✅ Test Output: Formatted text and JSON (FR-076 to FR-077)
- ✅ Test Configuration: Custom endpoint, model, timeout (FR-082 to FR-087)

## What Is Missing (❌ Not Implemented)

### Gap #1: Setup Documentation (FR-001 to FR-038)
**Status**: [ ]
**File to Create**: `docs/ollama-setup.md`
**Why Needed**: FR-001 to FR-038 require comprehensive setup documentation
**Required Sections**:
1. Prerequisites (FR-013 to FR-018)
   - Ollama installation requirement
   - Minimum version (0.1.23+)
   - Model download requirement
   - Recommended models
   - `ollama serve` command explanation
   - Verification command (`ollama list`)

2. Installation Verification (FR-004)
   - Commands to verify Ollama is working
   - Expected output examples

3. Configuration (FR-019 to FR-025)
   - All provider settings documented
   - Default values for each setting
   - Environment variable overrides
   - Complete YAML example
   - Timeout tuning guidance
   - Retry configuration
   - Model mappings

4. Quick Start (FR-026 to FR-030)
   - Under 50 lines
   - Assumes Ollama running
   - Minimal config
   - First command example
   - Success criteria

5. Troubleshooting (FR-031 to FR-038)
   - Connection refused
   - Model not found
   - Timeout errors
   - Memory errors
   - Slow generation
   - Tool call failures
   - Symptoms and resolutions for each
   - Diagnostic commands

6. Version Compatibility (FR-008)
   - Tested Ollama versions
   - Known incompatibilities

7. CLI Examples (FR-010)
   - All smoke test commands
   - Flag combinations

8. Configuration File Examples (FR-011)
   - Complete `.agent/config.yml` examples

9. Error Message Explanations (FR-012)
   - Common errors and meanings

10. Links to Ollama Documentation (FR-009)
    - Official Ollama docs
    - Model library
    - API reference

**Success Criteria**: Documentation file exists and covers all 10 sections
**Evidence**: [To be filled when complete]

---

### Gap #2: PowerShell Smoke Test Script (FR-039 to FR-051)
**Status**: [ ]
**File to Create**: `scripts/smoke-test-ollama.ps1`
**Why Needed**: FR-040 requires standalone PowerShell script
**Required Functionality**:
1. Check Ollama connectivity (FR-042)
2. Verify at least one model available (FR-043)
3. Test non-streaming completion (FR-044)
4. Test streaming completion (FR-045)
5. Test tool calling if model supports (FR-046)
6. Report pass/fail for each test (FR-047)
7. Provide diagnostic output on failure (FR-048)
8. Exit with code 0 on success (FR-049)
9. Exit with code 1 on test failure (FR-050)
10. Exit with code 2 on configuration error (FR-051)

**Implementation Pattern**:
```powershell
# scripts/smoke-test-ollama.ps1
param(
    [string]$Endpoint = "http://localhost:11434",
    [string]$Model = "llama3.2:latest",
    [int]$Timeout = 60,
    [switch]$SkipToolTest,
    [switch]$Verbose
)

# Test 1: Health Check
# Test 2: Model List
# Test 3: Completion
# Test 4: Streaming
# Test 5: Tool Call (if not skipped)

# Exit with appropriate code
```

**Success Criteria**: Script runs all tests and exits with correct code
**Evidence**: [To be filled when complete]

---

### Gap #3: Bash Smoke Test Script (FR-039, FR-041)
**Status**: [ ]
**File to Create**: `scripts/smoke-test-ollama.sh`
**Why Needed**: FR-041 requires Bash equivalent for cross-platform support
**Required Functionality**: Same as Gap #2 but in Bash

**Implementation Pattern**:
```bash
#!/usr/bin/env bash
# scripts/smoke-test-ollama.sh

ENDPOINT="${ENDPOINT:-http://localhost:11434}"
MODEL="${MODEL:-llama3.2:latest}"
TIMEOUT="${TIMEOUT:-60}"
SKIP_TOOL_TEST=false
VERBOSE=false

# Parse args
while [[ $# -gt 0 ]]; do
    case $1 in
        --endpoint) ENDPOINT="$2"; shift 2 ;;
        --model) MODEL="$2"; shift 2 ;;
        --timeout) TIMEOUT="$2"; shift 2 ;;
        --skip-tool-test) SKIP_TOOL_TEST=true; shift ;;
        --verbose) VERBOSE=true; shift ;;
        *) echo "Unknown option: $1"; exit 2 ;;
    esac
done

# Test 1: Health Check
# Test 2: Model List
# Test 3: Completion
# Test 4: Streaming
# Test 5: Tool Call (if not skipped)

# Exit with appropriate code
```

**Success Criteria**: Script runs all tests and exits with correct code
**Evidence**: [To be filled when complete]

---

### Gap #4: Version Checking (FR-078 to FR-081)
**Status**: [ ]
**Where to Add**: Both scripts and potentially CLI command
**Why Needed**: FR-078 to FR-081 require Ollama version validation
**Required Functionality**:
1. Check Ollama version if available (FR-078)
2. Warn if version below minimum (0.1.23+) (FR-079)
3. Warn if version above tested maximum (FR-080)
4. Version check failure MUST NOT block tests (FR-081)

**Implementation**: Call `ollama --version` and parse output
**Success Criteria**: Version warnings appear but tests continue
**Evidence**: [To be filled when complete]

---

### Gap #5: Script Tests
**Status**: [ ]
**Files to Create**:
- `tests/Scripts/SmokeTestScriptTests.ps1` (Pester tests)
- Manual verification steps
**Why Needed**: Scripts need testing just like C# code
**Required Tests**:
1. Scripts can be executed
2. Scripts exit with correct codes
3. Scripts handle missing Ollama gracefully
4. Scripts produce expected output format

**Success Criteria**: All script tests pass
**Evidence**: [To be filled when complete]

---

## Implementation Order

### Phase 1: Documentation (Gap #1)
1. Create `docs/ollama-setup.md` with complete structure
2. Write Prerequisites section
3. Write Installation Verification section
4. Write Configuration section
5. Write Quick Start section
6. Write Troubleshooting section
7. Write Version Compatibility section
8. Add CLI examples
9. Add configuration file examples
10. Add error message explanations
11. Add links to Ollama docs

**Target**: ~400-600 lines of comprehensive documentation

### Phase 2: PowerShell Script (Gap #2)
1. Create `scripts/smoke-test-ollama.ps1`
2. Implement parameter parsing
3. Implement health check test
4. Implement model list test
5. Implement completion test
6. Implement streaming test
7. Implement tool call test (with skip logic)
8. Implement output formatting
9. Implement exit code logic
10. Add verbose flag support

**Target**: ~300-400 lines

### Phase 3: Bash Script (Gap #3)
1. Create `scripts/smoke-test-ollama.sh`
2. Port all PowerShell functionality to Bash
3. Ensure cross-platform compatibility
4. Test on Linux/macOS

**Target**: ~300-400 lines

### Phase 4: Version Checking (Gap #4)
1. Add version checking to PowerShell script
2. Add version checking to Bash script
3. Optionally add to CLI command

**Target**: ~50 lines per script

### Phase 5: Testing and Verification
1. Manual test PowerShell script on Windows
2. Manual test Bash script on Linux/WSL
3. Verify all exit codes
4. Verify all documentation links work
5. Update audit report with evidence

---

## Completion Criteria

Task 005c is complete when:
- ✅ C# infrastructure complete (already done)
- ✅ CLI integration complete (already done)
- ✅ `docs/ollama-setup.md` exists with all 10 required sections
- ✅ `scripts/smoke-test-ollama.ps1` exists and works
- ✅ `scripts/smoke-test-ollama.sh` exists and works
- ✅ Version checking implemented in scripts
- ✅ All scripts tested manually
- ✅ Audit report updated with missing deliverables evidence
- ✅ PR updated to reflect all deliverables

---

## Estimated Scope

- **Documentation**: ~500 lines, ~2-3 hours
- **PowerShell Script**: ~350 lines, ~2 hours
- **Bash Script**: ~350 lines, ~1.5 hours (port from PS)
- **Version Checking**: ~100 lines total, ~30 minutes
- **Testing**: ~1 hour
- **Total**: ~7-8 hours of work

This is **substantial additional scope** that was missed in initial implementation.

---

**Last Updated**: 2026-01-13 (Discovered during audit fresh gap analysis)
