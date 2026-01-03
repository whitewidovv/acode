# Task 018.b: Working Dir/Env Enforcement

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 018 (Structured Command Runner), Task 050 (Workspace Database)  

---

## Description

Task 018.b implements working directory and environment variable enforcement. Commands must execute in the correct directory with the correct environment.

Working directory determines file resolution. Relative paths resolve from the working directory. Commands expect specific working directories. Incorrect directories cause failures.

The agent operates on repository code. Commands typically run from the repo root. Some commands require subdirectories. Working directory must be configurable per command.

Environment variables configure command behavior. PATH determines executable lookup. Language-specific variables control runtimes. Project variables customize behavior.

Environment modes determine variable handling. Inherit mode passes parent environment. Replace mode uses only specified variables. Merge mode combines both.

Sensitive variables require protection. API keys, passwords, tokens are sensitive. They must be passed but not logged. Redaction patterns hide sensitive values.

Path normalization ensures cross-platform compatibility. Windows uses backslashes. Unix uses forward slashes. Normalization handles both.

Environment validation prevents injection attacks. Variable names must be safe. Values must not contain injection patterns. Validation rejects dangerous input.

Audit events record execution context. Working directory and environment are logged. Sensitive values are redacted. Correlation IDs enable tracing.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Working Directory | Command execution path |
| CWD | Current working directory |
| Environment Variable | Named configuration value |
| PATH | Executable search path |
| Inherit | Pass parent environment |
| Merge | Combine environments |
| Replace | Use only specified vars |
| Redaction | Hide sensitive values |
| Normalization | Path standardization |
| Injection | Malicious input attack |
| Sensitive | Security-critical value |
| Variable Name | Environment key |
| Variable Value | Environment data |

---

## Out of Scope

The following items are explicitly excluded from Task 018.b:

- **Output capture** - See Task 018.a
- **Artifact logging** - See Task 018.c
- **Shell profile loading** - No shell profiles
- **User switching** - Same user only
- **Container environment** - See Task 020
- **Dynamic environment** - Static at command start
- **File-based env loading** - Explicit variables only

---

## Functional Requirements

### Working Directory

- FR-001: Set working directory
- FR-002: Validate directory exists
- FR-003: Validate is directory (not file)
- FR-004: Normalize path separators
- FR-005: Resolve relative paths
- FR-006: Handle UNC paths (Windows)
- FR-007: Handle symlinks
- FR-008: Use repo root as default

### Path Validation

- FR-009: Reject path traversal
- FR-010: Reject null bytes
- FR-011: Validate path length
- FR-012: Validate characters
- FR-013: Handle whitespace

### Environment Variables

- FR-014: Define environment mode
- FR-015: Implement inherit mode
- FR-016: Implement replace mode
- FR-017: Implement merge mode
- FR-018: Handle empty values
- FR-019: Handle missing values

### Variable Handling

- FR-020: Validate variable names
- FR-021: Reject invalid names
- FR-022: Handle case sensitivity
- FR-023: Handle special characters
- FR-024: Limit value size

### Merge Behavior

- FR-025: Parent vars as base
- FR-026: Override with specified
- FR-027: Remove specified nulls
- FR-028: Preserve unspecified

### Sensitive Variables

- FR-029: Define sensitive patterns
- FR-030: Detect sensitive names
- FR-031: Redact in logs
- FR-032: Redact in errors
- FR-033: Pass actual values to process
- FR-034: Configurable patterns

### PATH Handling

- FR-035: Inherit PATH default
- FR-036: Prepend additional paths
- FR-037: Append additional paths
- FR-038: Platform-correct separator
- FR-039: Remove duplicates

### Validation

- FR-040: Reject command injection
- FR-041: Sanitize shell characters
- FR-042: Validate before execution
- FR-043: Log validation failures

### Audit Recording

- FR-044: Record working directory
- FR-045: Record environment (redacted)
- FR-046: Store correlation IDs
- FR-047: Persist to workspace DB

---

## Non-Functional Requirements

### Performance

- NFR-001: Path validation < 1ms
- NFR-002: Environment merge < 5ms
- NFR-003: No allocation bloat

### Security

- NFR-004: No injection possible
- NFR-005: Sensitive values protected
- NFR-006: Paths restricted
- NFR-007: Values validated

### Reliability

- NFR-008: Consistent behavior
- NFR-009: Clear error messages
- NFR-010: Fallback to defaults

---

## User Manual Documentation

### Overview

Working directory and environment enforcement ensures commands execute in the correct context. Security protections prevent injection attacks.

### Configuration

```yaml
# .agent/config.yml
execution:
  working_directory:
    # Default to repo root
    default: "${repo_root}"
    
    # Allow paths outside repo
    allow_external: false
    
  environment:
    # inherit, replace, merge
    mode: inherit
    
    # Variables to always include
    defaults:
      DOTNET_CLI_TELEMETRY_OPTOUT: "1"
      
    # Sensitive variable patterns
    sensitive_patterns:
      - "*_KEY"
      - "*_SECRET"
      - "*_TOKEN"
      - "*_PASSWORD"
      - "*_CREDENTIAL*"
      
    # PATH modifications
    path:
      prepend: []
      append: []
```

### Environment Modes

| Mode | Description |
|------|-------------|
| inherit | Start with parent environment |
| replace | Only specified variables |
| merge | Parent + specified (specified wins) |

### Sensitive Variable Redaction

```
# Original log entry (NOT stored)
Environment: API_KEY=sk-abc123def456

# Redacted log entry (stored)
Environment: API_KEY=<REDACTED>
```

### Variable Name Validation

| Character | Allowed |
|-----------|---------|
| A-Z | ✅ |
| a-z | ✅ |
| 0-9 | ✅ (not first) |
| _ | ✅ |
| Others | ❌ |

### CLI Examples

```bash
# Execute in specific directory
acode exec "npm install" --cwd ./frontend

# Execute with environment variable
acode exec "node app.js" --env "NODE_ENV=production"

# Execute with multiple variables
acode exec "python script.py" \
  --env "DEBUG=true" \
  --env "LOG_LEVEL=info"

# Execute with environment merge
acode exec "dotnet build" --env-mode merge
```

### Troubleshooting

#### Directory Not Found

**Problem:** Working directory doesn't exist

**Solutions:**
1. Verify path is correct
2. Create directory first
3. Check relative path resolution

#### Path Traversal Rejected

**Problem:** Path contains ../

**Solutions:**
1. Use absolute paths
2. Use paths within repo root
3. Enable allow_external if needed

#### Environment Not Set

**Problem:** Variable not visible to command

**Solutions:**
1. Check environment mode
2. Verify variable name is valid
3. Check for typos

---

## Acceptance Criteria

### Working Directory

- [ ] AC-001: Directory set correctly
- [ ] AC-002: Validation works
- [ ] AC-003: Normalization works
- [ ] AC-004: Default works

### Environment

- [ ] AC-005: Inherit mode works
- [ ] AC-006: Replace mode works
- [ ] AC-007: Merge mode works
- [ ] AC-008: Variables passed

### Security

- [ ] AC-009: Sensitive redacted
- [ ] AC-010: Injection prevented
- [ ] AC-011: Paths restricted

### Audit

- [ ] AC-012: Context logged
- [ ] AC-013: Redaction applied
- [ ] AC-014: IDs present

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Execution/Environment/
├── WorkingDirectoryTests.cs
│   ├── Should_Set_Directory()
│   ├── Should_Validate_Exists()
│   ├── Should_Normalize_Path()
│   └── Should_Reject_Traversal()
│
├── EnvironmentModeTests.cs
│   ├── Should_Inherit()
│   ├── Should_Replace()
│   └── Should_Merge()
│
├── SensitiveRedactionTests.cs
│   ├── Should_Detect_Sensitive()
│   ├── Should_Redact_In_Logs()
│   └── Should_Pass_Actual_Value()
│
└── ValidationTests.cs
    ├── Should_Validate_Names()
    └── Should_Reject_Injection()
```

### Integration Tests

```
Tests/Integration/Execution/Environment/
├── EnvironmentIntegrationTests.cs
│   ├── Should_Execute_With_Env()
│   └── Should_Execute_In_Directory()
```

### E2E Tests

```
Tests/E2E/Execution/Environment/
├── EnvironmentE2ETests.cs
│   └── Should_Set_Via_CLI()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Path validation | 0.5ms | 1ms |
| Environment merge | 2ms | 5ms |
| Sensitive detection | 0.5ms | 1ms |

---

## User Verification Steps

### Scenario 1: Set Working Directory

1. Run command with --cwd
2. Verify: Runs in specified directory

### Scenario 2: Set Environment Variable

1. Run command with --env
2. Verify: Variable visible to command

### Scenario 3: Merge Environment

1. Set parent variable
2. Run with merge mode
3. Verify: Both variables present

### Scenario 4: Sensitive Redaction

1. Set sensitive variable
2. Check audit log
3. Verify: Value redacted

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/
├── Execution/
│   └── Environment/
│       ├── WorkingDirectoryResolver.cs
│       ├── EnvironmentBuilder.cs
│       ├── SensitiveRedactor.cs
│       └── PathValidator.cs
```

### WorkingDirectoryResolver Class

```csharp
namespace AgenticCoder.Infrastructure.Execution.Environment;

public class WorkingDirectoryResolver
{
    private readonly WorkingDirectoryOptions _options;
    
    public string Resolve(string? requested, string repoRoot)
    {
        if (string.IsNullOrEmpty(requested))
            return repoRoot;
            
        var normalized = NormalizePath(requested);
        var absolute = Path.GetFullPath(normalized, repoRoot);
        
        Validate(absolute, repoRoot);
        
        return absolute;
    }
    
    private void Validate(string path, string repoRoot)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(path);
            
        if (!_options.AllowExternal && !path.StartsWith(repoRoot))
            throw new InvalidOperationException("Path outside repo");
    }
}
```

### EnvironmentBuilder Class

```csharp
public class EnvironmentBuilder
{
    public IDictionary<string, string> Build(
        EnvironmentMode mode,
        IDictionary<string, string>? specified)
    {
        return mode switch
        {
            EnvironmentMode.Inherit => MergeWithParent(specified),
            EnvironmentMode.Replace => specified ?? new Dictionary<string, string>(),
            EnvironmentMode.Merge => MergeWithParent(specified),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-ENV-001 | Directory not found |
| ACODE-ENV-002 | Path outside repo |
| ACODE-ENV-003 | Invalid variable name |
| ACODE-ENV-004 | Path traversal attempt |

### Implementation Checklist

1. [ ] Create working directory resolver
2. [ ] Create environment builder
3. [ ] Add path validation
4. [ ] Add sensitive redaction
5. [ ] Add environment modes
6. [ ] Add audit recording
7. [ ] Add CLI options
8. [ ] Add unit tests

### Rollout Plan

1. **Phase 1:** Working directory
2. **Phase 2:** Environment modes
3. **Phase 3:** Sensitive redaction
4. **Phase 4:** Validation
5. **Phase 5:** Integration

---

**End of Task 018.b Specification**