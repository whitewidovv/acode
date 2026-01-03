# Task 029: ComputeTarget Interface

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 003 (Interfaces), Task 027 (Workers), Task 001 (Modes)  

---

## Description

Task 029 defines the ComputeTarget interface. A ComputeTarget is an abstraction over execution environments. Local, SSH, and cloud targets MUST implement this interface.

The interface MUST define lifecycle operations. Workspace preparation, command execution, artifact transfer, and teardown MUST be abstracted. Implementations MUST be swappable via configuration.

ComputeTargets MUST be managed by a factory. The factory MUST create targets from configuration. Target type MUST be determined from config. Validation MUST prevent invalid configurations.

### Business Value

ComputeTarget abstraction enables:
- Provider flexibility
- Configuration-driven switching
- Consistent API for all targets
- Testing with mocks
- Future provider additions

### Scope Boundaries

This task covers the interface. Subtasks cover prepare (029.a), execute (029.b), artifacts (029.c), and teardown (029.d).

### Integration Points

- Task 027: Workers use targets
- Task 001: Mode affects target availability
- Task 030-031: Implement this interface

### Failure Modes

- Target unavailable → Fallback or fail
- Connection lost → Retry with backoff
- Resource exhausted → Queue for later
- Config invalid → Reject with error

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| ComputeTarget | Execution environment abstraction |
| Factory | Target creation component |
| Lifecycle | Target creation to teardown |
| Workspace | Prepared execution environment |
| Artifact | File produced by execution |
| Teardown | Resource cleanup |

---

## Out of Scope

- Specific provider implementation
- Cost calculation
- Multi-target orchestration
- Container orchestration
- Serverless targets

---

## Functional Requirements

### FR-001 to FR-025: Interface Definition

- FR-001: `IComputeTarget` interface MUST be defined
- FR-002: Target MUST have unique ID
- FR-003: ID MUST be ULID
- FR-004: Target MUST have type property
- FR-005: Types: Local, SSH, EC2
- FR-006: Target MUST have state property
- FR-007: States: Created, Preparing, Ready, Busy, Tearingdown, Terminated
- FR-008: Target MUST have metadata dictionary
- FR-009: `PrepareWorkspaceAsync` MUST be defined
- FR-010: Prepare MUST accept config
- FR-011: Prepare MUST sync code
- FR-012: Prepare MUST install dependencies
- FR-013: `ExecuteAsync` MUST be defined
- FR-014: Execute MUST accept command
- FR-015: Execute MUST return result
- FR-016: Execute MUST support timeout
- FR-017: `UploadAsync` MUST be defined
- FR-018: Upload MUST accept local path
- FR-019: Upload MUST accept remote path
- FR-020: `DownloadAsync` MUST be defined
- FR-021: Download MUST accept remote path
- FR-022: Download MUST accept local path
- FR-023: `TeardownAsync` MUST be defined
- FR-024: Teardown MUST cleanup all resources
- FR-025: Target MUST implement IAsyncDisposable

### FR-026 to FR-045: Factory

- FR-026: `IComputeTargetFactory` MUST be defined
- FR-027: `CreateAsync` MUST create target
- FR-028: Create MUST accept config
- FR-029: Create MUST return target
- FR-030: Create MUST validate config
- FR-031: Invalid config MUST throw
- FR-032: `GetAvailableTargetsAsync` MUST list
- FR-033: List MUST show active targets
- FR-034: List MUST show target state
- FR-035: `ValidateConfigAsync` MUST check
- FR-036: Validation MUST be pre-create
- FR-037: Factory MUST be registered as singleton
- FR-038: Factory MUST track all targets
- FR-039: Factory MUST support disposal
- FR-040: Disposal MUST teardown all targets
- FR-041: Factory MUST enforce limits
- FR-042: Max concurrent MUST be configurable
- FR-043: Over-limit MUST queue
- FR-044: Factory events MUST emit
- FR-045: Factory metrics MUST track

### FR-046 to FR-060: Mode Compliance

- FR-046: Target creation MUST check mode
- FR-047: local-only MUST allow Local only
- FR-048: burst MUST allow all types
- FR-049: airgapped MUST allow Local only
- FR-050: Mode violation MUST throw
- FR-051: Exception MUST be descriptive
- FR-052: Mode check MUST be first operation
- FR-053: Config MUST specify target type
- FR-054: Type MUST match mode allowlist
- FR-055: Allowlist MUST be configurable
- FR-056: Default allowlist per mode
- FR-057: Override MUST be explicit
- FR-058: Override MUST log warning
- FR-059: Audit MUST capture mode
- FR-060: Audit MUST capture target type

---

## Non-Functional Requirements

- NFR-001: Target creation MUST be <5s
- NFR-002: Interface MUST be thread-safe
- NFR-003: State transitions MUST be atomic
- NFR-004: Memory per target MUST be bounded
- NFR-005: 100 concurrent targets MUST work
- NFR-006: Factory MUST be singleton-safe
- NFR-007: All operations MUST be cancellable
- NFR-008: Timeouts MUST be configurable
- NFR-009: Retries MUST use backoff
- NFR-010: Logging MUST be structured

---

## User Manual Documentation

### Configuration

```yaml
compute:
  defaultTarget: local
  maxConcurrentTargets: 10
  
  targets:
    local:
      enabled: true
      
    ssh:
      enabled: true
      hosts:
        - name: build-server
          host: build.example.com
          user: acode
          keyPath: ~/.ssh/acode_rsa
          
    ec2:
      enabled: true
      region: us-west-2
      instanceType: t3.medium
      keyName: acode-key
```

### Target Lifecycle

```
Created → Preparing → Ready → Busy → Ready → ... → Tearingdown → Terminated
```

### CLI Commands

```bash
# List available targets
acode compute list

# Create target
acode compute create --type ssh --config server.yaml

# Check target status
acode compute status target-abc123

# Teardown target
acode compute teardown target-abc123
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Interface defined
- [ ] AC-002: Factory defined
- [ ] AC-003: Local target works
- [ ] AC-004: Mode compliance works
- [ ] AC-005: State transitions work
- [ ] AC-006: Create/teardown works
- [ ] AC-007: Events emitted
- [ ] AC-008: Metrics tracked
- [ ] AC-009: Disposal works
- [ ] AC-010: Tests pass

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Interface contract
- [ ] UT-002: Factory creation
- [ ] UT-003: Mode validation
- [ ] UT-004: State transitions
- [ ] UT-005: Disposal

### Integration Tests

- [ ] IT-001: Full lifecycle
- [ ] IT-002: Concurrent targets
- [ ] IT-003: Mode blocking

---

## Implementation Prompt

### Interface

```csharp
public interface IComputeTarget : IAsyncDisposable
{
    string Id { get; }
    ComputeTargetType Type { get; }
    ComputeTargetState State { get; }
    IReadOnlyDictionary<string, object> Metadata { get; }
    
    Task PrepareWorkspaceAsync(WorkspaceConfig config,
        CancellationToken ct = default);
        
    Task<ExecutionResult> ExecuteAsync(ExecutionCommand command,
        CancellationToken ct = default);
        
    Task UploadAsync(string localPath, string remotePath,
        CancellationToken ct = default);
        
    Task DownloadAsync(string remotePath, string localPath,
        CancellationToken ct = default);
        
    Task TeardownAsync(CancellationToken ct = default);
}

public enum ComputeTargetType { Local, SSH, EC2 }

public enum ComputeTargetState 
{ 
    Created, Preparing, Ready, Busy, 
    Tearingdown, Terminated, Failed 
}

public interface IComputeTargetFactory
{
    Task<IComputeTarget> CreateAsync(ComputeTargetConfig config,
        CancellationToken ct = default);
        
    Task<IReadOnlyList<TargetInfo>> GetAvailableTargetsAsync(
        CancellationToken ct = default);
        
    Task<ValidationResult> ValidateConfigAsync(ComputeTargetConfig config,
        CancellationToken ct = default);
}
```

---

**End of Task 029 Specification**