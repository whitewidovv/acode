# Task 021.a: Artifact Directory Standards

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 021 (Artifact Collection)  

---

## Description

Task 021.a defines artifact directory standards. Artifacts MUST be stored in a consistent structure. Predictable paths enable tooling and inspection.

The base directory MUST be `.acode/artifacts`. All artifacts MUST reside under this path. The directory MUST be created automatically.

Artifacts MUST be organized by run ID. Each run MUST have its own subdirectory. Collisions MUST NOT occur.

File naming MUST be consistent. Stdout MUST be `stdout.txt`. Stderr MUST be `stderr.txt`. Custom artifacts MUST have descriptive names.

Retention MUST respect disk limits. Old artifacts MUST be pruned. Pruning MUST remove entire run directories.

---

## Functional Requirements

- FR-001: Base directory MUST be `.acode/artifacts`
- FR-002: Each run MUST have directory `.acode/artifacts/{run-id}/`
- FR-003: Stdout MUST be stored as `stdout.txt`
- FR-004: Stderr MUST be stored as `stderr.txt`
- FR-005: Test results MUST be stored as `test-results.json`
- FR-006: Build logs MUST be stored as `build.log`
- FR-007: Metadata MUST be stored as `run.json`
- FR-008: Directory creation MUST be automatic

### Directory Structure

```
.acode/
└── artifacts/
    ├── run-001/
    │   ├── run.json
    │   ├── stdout.txt
    │   ├── stderr.txt
    │   └── test-results.json
    └── run-002/
        ├── run.json
        ├── stdout.txt
        └── stderr.txt
```

---

## Acceptance Criteria

- [ ] AC-001: Artifacts MUST be stored under `.acode/artifacts`
- [ ] AC-002: Each run MUST have its own directory
- [ ] AC-003: Standard file names MUST be used
- [ ] AC-004: Directory MUST be created if missing

---

## Implementation Prompt

### Path Resolution

```csharp
public class ArtifactPaths
{
    public const string BaseDir = ".acode/artifacts";
    
    public static string GetRunDir(string runId) => 
        Path.Combine(BaseDir, runId);
    
    public static string GetStdout(string runId) => 
        Path.Combine(GetRunDir(runId), "stdout.txt");
    
    public static string GetStderr(string runId) => 
        Path.Combine(GetRunDir(runId), "stderr.txt");
    
    public static string GetMetadata(string runId) => 
        Path.Combine(GetRunDir(runId), "run.json");
}
```

---

**End of Task 021.a Specification**