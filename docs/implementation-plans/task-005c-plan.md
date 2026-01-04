# Task 005c Implementation Plan

## Status: In Progress

## Overview

Implement setup documentation and smoke test scripts for Ollama provider adapter.

## Deliverables

### 1. Setup Documentation (docs/ollama-setup.md)
- [ ] Prerequisites section (FR-013 to FR-018)
- [ ] Quick Start section (FR-026 to FR-030)
- [ ] Configuration section (FR-019 to FR-025)
- [ ] Troubleshooting section (FR-031 to FR-038)
- [ ] Version compatibility section (FR-078 to FR-081)

### 2. Smoke Test Script - Bash (scripts/smoke-test-ollama.sh)
- [ ] Health check test (FR-060 to FR-061)
- [ ] Model list test (FR-062 to FR-063)
- [ ] Completion test (FR-064 to FR-066)
- [ ] Streaming test (FR-067 to FR-068)
- [ ] Tool calling test STUB with TODO: Task 007d (FR-069 to FR-070)
- [ ] Exit codes (0/1/2) (FR-049 to FR-051)
- [ ] Test output formatting (FR-071 to FR-077)

### 3. Smoke Test Script - PowerShell (scripts/smoke-test-ollama.ps1)
- [ ] Same tests as Bash version
- [ ] PowerShell-specific syntax

### 4. CLI Integration (later - optional for minimal 005c)
- Defer to future task as CLI smoke-test command requires more infrastructure

## Implementation Order

1. **Setup Documentation** (docs/ollama-setup.md) - highest value, no dependencies
2. **Bash Smoke Test** (scripts/smoke-test-ollama.sh) - core functionality
3. **PowerShell Smoke Test** (scripts/smoke-test-ollama.ps1) - Windows support

## Notes

- Tool calling test implemented as stub: "SKIPPED - Requires Task 007d"
- Add TODO comments in stub pointing to Task 007d FR-082 through FR-087
- CLI integration (FR-052 to FR-059) deferred - would require CLI command infrastructure

## Progress

- [x] Documentation complete (docs/ollama-setup.md)
- [x] Bash script complete (scripts/smoke-test-ollama.sh)
- [x] PowerShell script complete (scripts/smoke-test-ollama.ps1)
- [x] Tool calling test stub with TODO comments
- [ ] Committed
