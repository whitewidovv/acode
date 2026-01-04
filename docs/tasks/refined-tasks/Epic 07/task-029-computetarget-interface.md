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

| Component | Integration Type | Purpose |
|-----------|------------------|---------|
| Task 027 (Worker Pool) | Consumer | Workers use compute targets for task execution |
| Task 001 (Operating Modes) | Constraint | Mode determines which target types are allowed |
| Task 030 (SSH Target) | Implementation | SSH target implements IComputeTarget |
| Task 031 (EC2 Target) | Implementation | EC2 target implements IComputeTarget |
| Task 032 (Placement Engine) | Consumer | Placement engine selects targets based on requirements |
| Task 033 (Burst Heuristics) | Consumer | Heuristics determine when to use cloud targets |
| Task 002a (Config) | Configuration | Target configuration from agent-config.yml |

### Failure Modes

| Failure | Detection | Recovery |
|---------|-----------|----------|
| Target unavailable | Factory creation fails | Return error, suggest alternatives, queue for retry |
| Connection lost mid-operation | Operation timeout or exception | Retry with exponential backoff, report partial progress |
| Resource exhausted | Quota errors from provider | Queue task, alert user, suggest resource increase |
| Config invalid | Validation during factory creation | Reject with specific validation errors, suggest fixes |
| State transition invalid | State machine violation | Log error, force cleanup, report inconsistent state |
| Target stuck in busy state | Heartbeat timeout | Force teardown, mark as failed, create new target |
| Concurrent access conflict | Lock contention timeout | Retry with backoff, implement request queuing |
| Cleanup fails on teardown | Teardown operation errors | Log warning, schedule orphan cleanup, continue |

### Assumptions

1. Target implementations are registered in the DI container before factory usage
2. Configuration schema is validated before reaching the factory
3. Network connectivity to remote targets is available when needed
4. Operating mode is established before any target creation attempts
5. Concurrent target limits are enforced at the factory level, not per-target
6. All target implementations follow the same state machine transitions
7. Targets are single-use—once torn down, they cannot be reused
8. Workspace paths are consistent across all target types (same logical structure)

### Security Considerations

1. **Credential Isolation**: Target credentials (SSH keys, AWS secrets) MUST NOT be accessible from executed commands—credentials are for target access only, not for commands running on targets
2. **State Machine Enforcement**: State transitions MUST be enforced to prevent use-after-teardown or double-disposal vulnerabilities
3. **Metadata Sanitization**: Target metadata dictionary MUST NOT contain secrets—it's used for logging and debugging
4. **Factory Access Control**: Factory MUST validate that requested target type is allowed in current operating mode before creation
5. **Resource Limits**: Targets MUST enforce resource limits (CPU, memory, disk) to prevent denial-of-service from runaway tasks
6. **Audit Trail**: All target lifecycle events MUST be logged for security auditing without exposing sensitive data
7. **Cleanup Guarantees**: Teardown MUST release all resources even on failure—orphaned resources are a security risk
8. **Input Validation**: All paths, commands, and configuration values MUST be validated before use

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| ComputeTarget | Abstraction over execution environments (local, SSH, EC2) providing unified lifecycle and execution APIs |
| Factory | Component responsible for creating and tracking compute targets based on configuration |
| Lifecycle | Complete target existence from creation through teardown, including state transitions |
| Workspace | Prepared execution environment on target with code, dependencies, and configuration |
| Artifact | File produced by command execution that must be transferred back to agent |
| Teardown | Graceful resource cleanup including workspace deletion and connection closure |
| State Machine | Defined state transitions ensuring targets move through valid lifecycle phases |
| Placement | Decision process for selecting appropriate target type for a given task |
| ULID | Universally Unique Lexicographically Sortable Identifier—provides unique target IDs |

---

## Out of Scope

The following items are explicitly excluded from Task 029:

- **Specific provider implementations** — SSH in Task 030, EC2 in Task 031
- **Cost calculation and tracking** — Handled by individual provider implementations
- **Multi-target orchestration** — Single target per task; parallel tasks use multiple targets
- **Container orchestration** — Docker workers in Task 027.b are not compute targets
- **Serverless targets** — Lambda/Azure Functions are future work
- **Load balancing across targets** — Placement engine responsibility (Task 032)
- **Target pooling and reuse** — Targets are single-use; pooling is provider-specific
- **Health monitoring dashboards** — CLI provides basic status; dashboards are future

---

## Functional Requirements

### IComputeTarget Interface (FR-029-01 to FR-029-25)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029-01 | System MUST define `IComputeTarget` interface as the compute target contract | Must Have |
| FR-029-02 | Target MUST have unique `TargetId` property (string, immutable after creation) | Must Have |
| FR-029-03 | `TargetId` MUST be ULID format for lexicographic sorting and uniqueness | Must Have |
| FR-029-04 | Target MUST have `TargetType` property (string: "local", "ssh", "ec2") | Must Have |
| FR-029-05 | `TargetType` MUST be immutable after creation | Must Have |
| FR-029-06 | Target MUST have `State` property reflecting current lifecycle phase | Must Have |
| FR-029-07 | `State` MUST be one of: NotProvisioned, Provisioning, Ready, Executing, TearingDown, Terminated, Failed | Must Have |
| FR-029-08 | Target MUST have `Metadata` property (IReadOnlyDictionary<string, string>) | Should Have |
| FR-029-09 | `Metadata` MUST NOT contain secrets or sensitive values | Must Have |
| FR-029-10 | Target MUST have `IsReady` computed property (true when State == Ready) | Must Have |
| FR-029-11 | Target MUST have `CreatedAt` property (DateTimeOffset) | Should Have |
| FR-029-12 | Target MUST have `LastActivityAt` property (updated on each operation) | Should Have |
| FR-029-13 | Target MUST define `PrepareAsync(WorkspaceContext, CancellationToken)` method | Must Have |
| FR-029-14 | `PrepareAsync` MUST return `Task<WorkspacePrepareResult>` | Must Have |
| FR-029-15 | `PrepareAsync` MUST transition state from NotProvisioned → Provisioning → Ready | Must Have |
| FR-029-16 | Target MUST define `ExecuteAsync(string, ExecuteOptions?, CancellationToken)` method | Must Have |
| FR-029-17 | `ExecuteAsync` MUST return `Task<ExecuteResult>` with stdout, stderr, exit code | Must Have |
| FR-029-18 | `ExecuteAsync` MUST transition state to Executing during execution | Should Have |
| FR-029-19 | Target MUST define `UploadAsync(string, string, TransferOptions?, CancellationToken)` method | Must Have |
| FR-029-20 | `UploadAsync` MUST transfer file from local path to remote path | Must Have |
| FR-029-21 | Target MUST define `DownloadAsync(string, string, TransferOptions?, CancellationToken)` method | Must Have |
| FR-029-22 | `DownloadAsync` MUST transfer file from remote path to local path | Must Have |
| FR-029-23 | Target MUST define `TeardownAsync(CancellationToken)` method | Must Have |
| FR-029-24 | `TeardownAsync` MUST cleanup all resources and release connections | Must Have |
| FR-029-25 | Target MUST implement `IAsyncDisposable` delegating to `TeardownAsync` | Must Have |

### IComputeTargetFactory Interface (FR-029-26 to FR-029-50)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029-26 | System MUST define `IComputeTargetFactory` interface | Must Have |
| FR-029-27 | Factory MUST define `CreateAsync(ComputeTargetConfiguration, CancellationToken)` method | Must Have |
| FR-029-28 | `CreateAsync` MUST return `Task<IComputeTarget>` | Must Have |
| FR-029-29 | `CreateAsync` MUST validate configuration before creation | Must Have |
| FR-029-30 | Invalid configuration MUST throw `ComputeTargetConfigurationException` | Must Have |
| FR-029-31 | Factory MUST define `ValidateConfigurationAsync(ComputeTargetConfiguration, CancellationToken)` method | Should Have |
| FR-029-32 | `ValidateConfigurationAsync` MUST return `Task<ValidationResult>` | Should Have |
| FR-029-33 | `ValidationResult` MUST contain list of validation errors if any | Should Have |
| FR-029-34 | Factory MUST define `GetActiveTargetsAsync(CancellationToken)` method | Should Have |
| FR-029-35 | `GetActiveTargetsAsync` MUST return all non-terminated targets | Should Have |
| FR-029-36 | Factory MUST track all created targets for lifecycle management | Must Have |
| FR-029-37 | Factory MUST be registered as singleton in DI container | Must Have |
| FR-029-38 | Factory MUST enforce maximum concurrent targets limit | Should Have |
| FR-029-39 | Maximum concurrent limit MUST be configurable (default: 10) | Should Have |
| FR-029-40 | Exceeding limit MUST queue creation until slot available | Should Have |
| FR-029-41 | Queue wait timeout MUST be configurable | Should Have |
| FR-029-42 | Factory MUST define `DisposeAllAsync(CancellationToken)` method | Must Have |
| FR-029-43 | `DisposeAllAsync` MUST teardown all active targets | Must Have |
| FR-029-44 | Factory MUST publish events for target creation and disposal | Should Have |
| FR-029-45 | Factory MUST maintain metrics for target lifecycle | Should Have |
| FR-029-46 | Factory MUST select appropriate provider based on target type | Must Have |
| FR-029-47 | Provider selection MUST use registered `IComputeTargetProvider` implementations | Must Have |
| FR-029-48 | Missing provider MUST throw `ComputeTargetProviderNotFoundException` | Must Have |
| FR-029-49 | Factory MUST log all creation and disposal operations | Must Have |
| FR-029-50 | Factory MUST be thread-safe for concurrent operations | Must Have |

### Mode Compliance (FR-029-51 to FR-029-70)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029-51 | Factory MUST validate operating mode before target creation | Must Have |
| FR-029-52 | Mode `local-only` MUST allow only target type "local" | Must Have |
| FR-029-53 | Mode `airgapped` MUST allow only target type "local" | Must Have |
| FR-029-54 | Mode `burst` MUST allow all target types ("local", "ssh", "ec2") | Must Have |
| FR-029-55 | Mode violation MUST throw `ModeViolationException` | Must Have |
| FR-029-56 | `ModeViolationException` MUST include current mode and requested target type | Must Have |
| FR-029-57 | `ModeViolationException` MUST include list of allowed target types | Should Have |
| FR-029-58 | Mode check MUST be first validation before any resource allocation | Must Have |
| FR-029-59 | Target type MUST be specified in configuration | Must Have |
| FR-029-60 | Target type MUST be validated against mode allowlist | Must Have |
| FR-029-61 | Allowlist per mode MUST be configurable in agent-config.yml | Should Have |
| FR-029-62 | Default allowlist MUST be used when not configured | Must Have |
| FR-029-63 | Custom allowlist override MUST log warning | Should Have |
| FR-029-64 | All mode violations MUST be logged at Error level | Must Have |
| FR-029-65 | All mode violations MUST be audited | Must Have |
| FR-029-66 | Audit entry MUST include timestamp, mode, target type, outcome | Should Have |
| FR-029-67 | Mode check failure MUST NOT create any resources | Must Have |
| FR-029-68 | Mode check failure MUST NOT make network calls | Must Have |
| FR-029-69 | Mode MUST be retrieved from `IModeValidator` service | Must Have |
| FR-029-70 | `IModeValidator` MUST be injected into factory | Must Have |

### State Machine (FR-029-71 to FR-029-90)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029-71 | Target MUST implement strict state machine for lifecycle transitions | Must Have |
| FR-029-72 | Initial state MUST be `NotProvisioned` | Must Have |
| FR-029-73 | `NotProvisioned` MUST transition only to `Provisioning` via PrepareAsync | Must Have |
| FR-029-74 | `Provisioning` MUST transition to `Ready` on success | Must Have |
| FR-029-75 | `Provisioning` MUST transition to `Failed` on error | Must Have |
| FR-029-76 | `Ready` MUST transition to `Executing` when ExecuteAsync starts | Should Have |
| FR-029-77 | `Executing` MUST transition back to `Ready` when execution completes | Should Have |
| FR-029-78 | `Ready` or `Executing` MUST transition to `TearingDown` via TeardownAsync | Must Have |
| FR-029-79 | `TearingDown` MUST transition to `Terminated` on success | Must Have |
| FR-029-80 | `TearingDown` MUST transition to `Failed` on error (but still cleanup) | Must Have |
| FR-029-81 | `Failed` MUST be terminal (no transitions out) | Must Have |
| FR-029-82 | `Terminated` MUST be terminal (no transitions out) | Must Have |
| FR-029-83 | Invalid transition attempt MUST throw `InvalidStateTransitionException` | Must Have |
| FR-029-84 | State transitions MUST be atomic (no intermediate states visible) | Should Have |
| FR-029-85 | State transitions MUST publish events | Should Have |
| FR-029-86 | State property MUST be thread-safe | Must Have |
| FR-029-87 | State MUST be queryable without blocking | Must Have |
| FR-029-88 | `StateChanged` event MUST be exposed for observers | Should Have |
| FR-029-89 | Event MUST include previous state, new state, and timestamp | Should Have |
| FR-029-90 | Event MUST include target ID for correlation | Should Have |

### Configuration Model (FR-029-91 to FR-029-110)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029-91 | System MUST define `ComputeTargetConfiguration` base record | Must Have |
| FR-029-92 | Configuration MUST have `TargetType` property (required) | Must Have |
| FR-029-93 | Configuration MUST have `Name` property (optional, for display) | Should Have |
| FR-029-94 | Configuration MUST have `Timeout` property (TimeSpan, default 5 min) | Should Have |
| FR-029-95 | Configuration MUST have `Metadata` property (Dictionary<string, string>) | Should Have |
| FR-029-96 | System MUST define `LocalTargetConfiguration` extending base | Must Have |
| FR-029-97 | `LocalTargetConfiguration` MUST have `WorkingDirectory` property | Should Have |
| FR-029-98 | System MUST define `SshTargetConfiguration` extending base | Must Have |
| FR-029-99 | `SshTargetConfiguration` MUST have `Host`, `Port`, `User`, `KeyPath` properties | Must Have |
| FR-029-100 | System MUST define `Ec2TargetConfiguration` extending base | Must Have |
| FR-029-101 | `Ec2TargetConfiguration` MUST have `Region`, `InstanceType`, `AmiId` properties | Must Have |
| FR-029-102 | All configuration types MUST be immutable records | Must Have |
| FR-029-103 | All configuration types MUST be serializable to JSON | Should Have |
| FR-029-104 | All configuration types MUST be deserializable from YAML | Should Have |
| FR-029-105 | Configuration validation MUST check required properties | Must Have |
| FR-029-106 | Configuration validation MUST check property formats (e.g., valid region) | Should Have |
| FR-029-107 | Configuration MUST support environment variable substitution | Should Have |
| FR-029-108 | Environment substitution syntax MUST be `${ENV_VAR}` | Should Have |
| FR-029-109 | Missing environment variable MUST fail validation | Should Have |
| FR-029-110 | Configuration MUST support default values per property | Should Have |

---

## Non-Functional Requirements

### Performance (NFR-029-01 to NFR-029-15)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029-01 | Target creation time (factory.CreateAsync) | <500ms for local, <5s for remote | Must Have |
| NFR-029-02 | Target teardown time | <2s for local, <30s for remote | Must Have |
| NFR-029-03 | State property access time | <1ms (no blocking) | Must Have |
| NFR-029-04 | Factory.GetActiveTargetsAsync response time | <50ms for 100 targets | Should Have |
| NFR-029-05 | Memory footprint per target (excluding workspace) | <5MB | Should Have |
| NFR-029-06 | Memory footprint for factory singleton | <10MB | Should Have |
| NFR-029-07 | Maximum concurrent targets supported | 100 | Should Have |
| NFR-029-08 | State transition time | <10ms | Should Have |
| NFR-029-09 | Event publication latency | <5ms | Should Have |
| NFR-029-10 | Configuration validation time | <50ms | Should Have |
| NFR-029-11 | Mode check time | <1ms | Must Have |
| NFR-029-12 | Metadata dictionary access time | O(1) | Should Have |
| NFR-029-13 | Target ID generation time | <1ms | Should Have |
| NFR-029-14 | Concurrent operation throughput | 50 ops/sec | Should Have |
| NFR-029-15 | GC pressure per operation | <1KB allocations | Could Have |

### Reliability (NFR-029-16 to NFR-029-30)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029-16 | State machine correctness | 100% valid transitions only | Must Have |
| NFR-029-17 | Resource cleanup on teardown | 100% (no leaks) | Must Have |
| NFR-029-18 | Resource cleanup on failure | 100% (no leaks) | Must Have |
| NFR-029-19 | Thread safety for state property | Lock-free reads | Must Have |
| NFR-029-20 | Thread safety for factory operations | Full thread safety | Must Have |
| NFR-029-21 | Cancellation token respect | <1s response | Must Have |
| NFR-029-22 | Exception handling (no unobserved exceptions) | 0 unobserved | Must Have |
| NFR-029-23 | Retry with exponential backoff | 3 retries, 100ms-5s | Should Have |
| NFR-029-24 | Idempotent teardown | Safe to call multiple times | Must Have |
| NFR-029-25 | Recovery from partial failure | Cleanup and report | Should Have |
| NFR-029-26 | Factory disposal completeness | All targets torn down | Must Have |
| NFR-029-27 | Timeout enforcement accuracy | ±100ms | Should Have |
| NFR-029-28 | Connection loss detection | <5s | Should Have |
| NFR-029-29 | Heartbeat for long operations | Every 30s | Could Have |
| NFR-029-30 | Orphan target detection | Within 5 minutes | Should Have |

### Maintainability (NFR-029-31 to NFR-029-45)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029-31 | Interface unit test coverage | 100% | Must Have |
| NFR-029-32 | Factory unit test coverage | >95% | Must Have |
| NFR-029-33 | State machine test coverage | 100% transitions | Must Have |
| NFR-029-34 | Maximum cyclomatic complexity | 10 per method | Should Have |
| NFR-029-35 | Maximum method length | 50 lines | Should Have |
| NFR-029-36 | XML documentation coverage | 100% public members | Must Have |
| NFR-029-37 | Interface segregation | Single responsibility | Should Have |
| NFR-029-38 | Dependency injection support | Constructor injection only | Must Have |
| NFR-029-39 | Mock-friendly interfaces | All dependencies injectable | Must Have |
| NFR-029-40 | Code duplication | <3% | Should Have |
| NFR-029-41 | Async consistency | All I/O operations async | Must Have |
| NFR-029-42 | Nullable reference type compliance | Enabled, no warnings | Should Have |
| NFR-029-43 | New provider addition effort | <1 day | Should Have |
| NFR-029-44 | Configuration extension effort | <2 hours | Should Have |
| NFR-029-45 | Breaking change impact assessment | Documented | Should Have |

### Observability (NFR-029-46 to NFR-029-60)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029-46 | Target creation log | Info level | Must Have |
| NFR-029-47 | Target teardown log | Info level | Must Have |
| NFR-029-48 | State transition log | Debug level | Should Have |
| NFR-029-49 | Error log with context | Error level with exception | Must Have |
| NFR-029-50 | Mode violation log | Error level | Must Have |
| NFR-029-51 | Configuration validation log | Debug level | Should Have |
| NFR-029-52 | TargetId in all logs | Correlation | Must Have |
| NFR-029-53 | Structured logging format | JSON-compatible | Should Have |
| NFR-029-54 | Metrics: active target count | Gauge | Should Have |
| NFR-029-55 | Metrics: target creation rate | Counter | Should Have |
| NFR-029-56 | Metrics: target lifetime duration | Histogram | Should Have |
| NFR-029-57 | Metrics: state transition count | Counter per state | Should Have |
| NFR-029-58 | Metrics: mode violation count | Counter | Should Have |
| NFR-029-59 | Distributed tracing support | OpenTelemetry spans | Could Have |
| NFR-029-60 | Health check endpoint integration | IHealthCheck | Could Have |

---

## User Manual Documentation

### Overview

The ComputeTarget abstraction provides a unified interface for executing commands across different compute environments—local machine, remote SSH servers, and cloud instances like EC2. This enables the agent to seamlessly burst workloads to more powerful infrastructure when needed while maintaining a consistent execution model.

### Configuration

```yaml
# .agent/config.yml
compute:
  defaultTarget: local
  maxConcurrentTargets: 10
  
  targets:
    local:
      enabled: true
      workingDirectory: ./workspace
      
    ssh:
      enabled: true
      hosts:
        - name: build-server
          host: build.example.com
          port: 22
          user: acode
          keyPath: ~/.ssh/acode_rsa
          timeout: 30s
          
    ec2:
      enabled: true
      region: us-west-2
      instanceType: t3.medium
      ami: ami-0c55b159cbfafe1f0
      keyName: acode-key
      subnetId: subnet-12345678
      securityGroupIds:
        - sg-12345678
```

### Target Lifecycle

```
NotProvisioned → Provisioning → Ready ⟷ Executing → TearingDown → Terminated
                      ↓                      ↓               ↓
                   Failed ←──────────────────┴───────────────┘
```

### CLI Commands

```bash
# List available target types
acode compute types

# Show current targets
acode compute list

# Show target details
acode compute show <target-id>

# Create a target manually
acode compute create --type ssh --config server.yaml

# Teardown a target
acode compute teardown <target-id>

# Teardown all targets
acode compute teardown --all

# Validate target configuration
acode compute validate --config target.yaml
```

### Error Messages

| Error Code | Message | Resolution |
|------------|---------|------------|
| ACODE-CT-001 | Mode violation: {targetType} not allowed in {mode} mode | Switch to burst mode or use allowed target type |
| ACODE-CT-002 | Configuration validation failed: {errors} | Fix configuration per error messages |
| ACODE-CT-003 | Target creation failed: {reason} | Check connectivity, credentials, and resources |
| ACODE-CT-004 | Invalid state transition: {from} → {to} | Internal error—report bug with logs |
| ACODE-CT-005 | Target not ready: state is {state} | Wait for provisioning or check for failures |
| ACODE-CT-006 | Maximum concurrent targets exceeded | Wait for existing targets or increase limit |
| ACODE-CT-007 | Target teardown failed: {reason} | Check target status, may need manual cleanup |
| ACODE-CT-008 | Provider not found: {targetType} | Ensure provider is registered in DI |

---

## Acceptance Criteria / Definition of Done

### Interface Definition (AC-029-01 to AC-029-10)

- [ ] AC-029-01: `IComputeTarget` interface defined in `Acode.Domain.Compute` namespace
- [ ] AC-029-02: Interface includes `Id` property returning `ComputeTargetId`
- [ ] AC-029-03: Interface includes `Type` property returning `ComputeTargetType` enum
- [ ] AC-029-04: Interface includes `State` property returning `ComputeTargetState` enum (thread-safe)
- [ ] AC-029-05: Interface includes `StateChanged` event with `TargetStateChangedEventArgs`
- [ ] AC-029-06: Interface includes `Metadata` property returning `IReadOnlyDictionary<string, object>`
- [ ] AC-029-07: Interface includes `PrepareAsync(CancellationToken)` method
- [ ] AC-029-08: Interface includes `ExecuteAsync(ExecutionCommand, CancellationToken)` method
- [ ] AC-029-09: Interface includes `TransferArtifactsAsync(...)` method with `TransferDirection` enum
- [ ] AC-029-10: Interface implements `IAsyncDisposable` for resource cleanup

### State Machine (AC-029-11 to AC-029-20)

- [ ] AC-029-11: `ComputeTargetState` enum defined with values: NotProvisioned, Provisioning, Ready, Executing, TearingDown, Terminated, Failed
- [ ] AC-029-12: Valid transition: NotProvisioned → Provisioning (on Create)
- [ ] AC-029-13: Valid transition: Provisioning → Ready (on PrepareAsync success)
- [ ] AC-029-14: Valid transition: Provisioning → Failed (on PrepareAsync failure)
- [ ] AC-029-15: Valid transition: Ready → Executing (on ExecuteAsync start)
- [ ] AC-029-16: Valid transition: Executing → Ready (on ExecuteAsync complete)
- [ ] AC-029-17: Valid transition: Ready → TearingDown (on DisposeAsync)
- [ ] AC-029-18: Valid transition: TearingDown → Terminated (on cleanup complete)
- [ ] AC-029-19: Valid transition: Any → Failed (on unrecoverable error)
- [ ] AC-029-20: Invalid transitions throw `InvalidOperationException` with descriptive message

### Factory Implementation (AC-029-21 to AC-029-35)

- [ ] AC-029-21: `IComputeTargetFactory` interface defined in `Acode.Application.Compute`
- [ ] AC-029-22: Factory includes `CreateAsync(ComputeTargetConfig, CancellationToken)` method
- [ ] AC-029-23: Factory includes `GetActiveTargetsAsync(CancellationToken)` method
- [ ] AC-029-24: Factory includes `GetTargetAsync(ComputeTargetId, CancellationToken)` method
- [ ] AC-029-25: Factory includes `ValidateConfigAsync(ComputeTargetConfig, CancellationToken)` method
- [ ] AC-029-26: Factory registered as singleton in DI container
- [ ] AC-029-27: Factory tracks all created targets internally
- [ ] AC-029-28: Factory implements `IAsyncDisposable` to cleanup all targets
- [ ] AC-029-29: Factory enforces `MaxConcurrentTargets` limit (configurable)
- [ ] AC-029-30: Factory throws `TargetLimitExceededException` when limit reached
- [ ] AC-029-31: Factory uses provider pattern for different target types
- [ ] AC-029-32: Factory resolves provider via `ITargetProvider<TConfig>`
- [ ] AC-029-33: Factory validates configuration before provider invocation
- [ ] AC-029-34: Factory emits `TargetCreated` event on successful creation
- [ ] AC-029-35: Factory emits `TargetTornDown` event on disposal

### Mode Compliance (AC-029-36 to AC-029-45)

- [ ] AC-029-36: `IModeValidator` interface defined for mode checking
- [ ] AC-029-37: Mode validator checks target type against current operating mode
- [ ] AC-029-38: `local-only` mode allows only `ComputeTargetType.Local`
- [ ] AC-029-39: `burst` mode allows all target types
- [ ] AC-029-40: `airgapped` mode allows only `ComputeTargetType.Local`
- [ ] AC-029-41: Mode violation throws `ModeViolationException`
- [ ] AC-029-42: Exception message includes mode, requested type, and allowed types
- [ ] AC-029-43: Mode check occurs BEFORE any resource allocation
- [ ] AC-029-44: Mode validation logged at Debug level
- [ ] AC-029-45: Mode violations logged at Error level with context

### Configuration (AC-029-46 to AC-029-55)

- [ ] AC-029-46: `ComputeTargetConfig` abstract base class defined
- [ ] AC-029-47: Config includes `TargetType` property
- [ ] AC-029-48: Config includes `Name` optional property
- [ ] AC-029-49: Config supports validation via `IValidatableObject`
- [ ] AC-029-50: `LocalTargetConfig` extends base with `WorkingDirectory`
- [ ] AC-029-51: `SshTargetConfig` extends base with Host, Port, User, KeyPath
- [ ] AC-029-52: `Ec2TargetConfig` extends base with Region, InstanceType, AMI, etc.
- [ ] AC-029-53: Configuration loaded from YAML `compute:` section
- [ ] AC-029-54: Configuration supports environment variable substitution `${VAR}`
- [ ] AC-029-55: Missing required configuration throws `ConfigurationException`

### Events and Metrics (AC-029-56 to AC-029-65)

- [ ] AC-029-56: `TargetCreatedEvent` includes TargetId, Type, Timestamp
- [ ] AC-029-57: `TargetStateChangedEvent` includes TargetId, OldState, NewState, Timestamp
- [ ] AC-029-58: `ExecutionStartedEvent` includes TargetId, Command, Timestamp
- [ ] AC-029-59: `ExecutionCompletedEvent` includes TargetId, ExitCode, Duration, Timestamp
- [ ] AC-029-60: Events published via `IEventBus` or `IMediator`
- [ ] AC-029-61: Metric: `acode_compute_targets_active` gauge exposed
- [ ] AC-029-62: Metric: `acode_compute_target_created_total` counter exposed
- [ ] AC-029-63: Metric: `acode_compute_target_duration_seconds` histogram exposed
- [ ] AC-029-64: Metric: `acode_compute_mode_violations_total` counter exposed
- [ ] AC-029-65: Metrics registered in OpenTelemetry meter

### Local Target Implementation (AC-029-66 to AC-029-75)

- [ ] AC-029-66: `LocalComputeTarget` implements `IComputeTarget`
- [ ] AC-029-67: Local target uses `System.Diagnostics.Process` for execution
- [ ] AC-029-68: Local target respects `WorkingDirectory` configuration
- [ ] AC-029-69: Local target captures stdout, stderr, and exit code
- [ ] AC-029-70: Local target supports streaming output via events
- [ ] AC-029-71: Local target respects cancellation token (kills process)
- [ ] AC-029-72: Local target respects timeout configuration
- [ ] AC-029-73: Local target PrepareAsync validates directory exists
- [ ] AC-029-74: Local target disposal kills any running process
- [ ] AC-029-75: Local target works on Windows, macOS, and Linux

### CLI Integration (AC-029-76 to AC-029-85)

- [ ] AC-029-76: `acode compute list` shows all active targets with state
- [ ] AC-029-77: `acode compute show <id>` shows target details including metadata
- [ ] AC-029-78: `acode compute create --type <type>` creates target interactively
- [ ] AC-029-79: `acode compute create --config <file>` creates from config file
- [ ] AC-029-80: `acode compute teardown <id>` tears down specific target
- [ ] AC-029-81: `acode compute teardown --all` tears down all targets
- [ ] AC-029-82: `acode compute validate --config <file>` validates without creating
- [ ] AC-029-83: CLI outputs table format for list commands
- [ ] AC-029-84: CLI outputs JSON format with `--json` flag
- [ ] AC-029-85: CLI shows progress indicator during provisioning

### Error Handling (AC-029-86 to AC-029-95)

- [ ] AC-029-86: `ComputeTargetException` base class defined for all compute errors
- [ ] AC-029-87: `ModeViolationException` includes Mode and RequestedType properties
- [ ] AC-029-88: `TargetUnavailableException` includes TargetId and Reason properties
- [ ] AC-029-89: `TargetLimitExceededException` includes CurrentCount and MaxAllowed
- [ ] AC-029-90: All exceptions include inner exception for debugging
- [ ] AC-029-91: All exceptions are serializable for logging
- [ ] AC-029-92: Failed targets transition to Failed state before exception propagation
- [ ] AC-029-93: Partial failures during teardown logged but don't prevent completion
- [ ] AC-029-94: Resource cleanup guaranteed even on exception paths
- [ ] AC-029-95: Timeout exceptions include elapsed time and configured timeout

### Documentation and Quality (AC-029-96 to AC-029-105)

- [ ] AC-029-96: All public types have XML documentation
- [ ] AC-029-97: Interface documentation includes usage examples
- [ ] AC-029-98: Exception documentation includes resolution guidance
- [ ] AC-029-99: Architecture decision record (ADR) written for abstraction design
- [ ] AC-029-100: Unit test coverage ≥95% for all interface implementations
- [ ] AC-029-101: Integration tests cover full target lifecycle
- [ ] AC-029-102: Mode compliance tests cover all mode/target combinations
- [ ] AC-029-103: All tests pass on Windows, macOS, and Linux CI
- [ ] AC-029-104: No compiler warnings (TreatWarningsAsErrors enabled)
- [ ] AC-029-105: Code reviewed and approved by at least one team member

---

## User Verification Scenarios

### Scenario 1: Developer Creates Local Target and Executes Command

**Persona:** Developer working in local-only mode on Windows laptop

**Steps:**
1. Run `acode compute list` → Shows "No active targets"
2. Run `acode compute create --type local` → Creates local target
3. Run `acode compute list` → Shows local target with State=Ready
4. Agent executes `dotnet build` on the target
5. Build output streams to console in real-time
6. Exit code 0 displayed with execution duration
7. Run `acode compute teardown --all` → Target removed

**Verification:**
- [ ] All commands execute without error
- [ ] Target transitions through correct states
- [ ] Build output visible in real-time
- [ ] Exit code and duration displayed correctly

### Scenario 2: Developer Attempts SSH Target in Local-Only Mode

**Persona:** Developer in local-only mode attempts to burst to remote server

**Steps:**
1. Configure agent in `local-only` mode
2. Run `acode compute create --type ssh --host build.example.com`
3. Should see error: "Mode violation: ssh not allowed in local-only mode"
4. Error includes suggestion: "Switch to burst mode or use local target"
5. Run `acode config set mode burst`
6. Retry `acode compute create --type ssh --host build.example.com`
7. SSH target created successfully

**Verification:**
- [ ] Mode violation error is clear and actionable
- [ ] After mode change, SSH target creation works
- [ ] No partial resources left from failed attempt

### Scenario 3: Factory Enforces Concurrent Target Limit

**Persona:** System running multiple parallel tasks

**Steps:**
1. Configure `maxConcurrentTargets: 3`
2. Create target 1 → Success
3. Create target 2 → Success
4. Create target 3 → Success
5. Create target 4 → Error: "Maximum concurrent targets exceeded (3/3)"
6. Teardown target 1
7. Create target 4 → Success

**Verification:**
- [ ] Limit enforced correctly
- [ ] Error message includes current/max counts
- [ ] After teardown, new target can be created

### Scenario 4: Target State Machine Transitions

**Persona:** Developer debugging target lifecycle

**Steps:**
1. Enable debug logging
2. Create local target
3. Observe log: State NotProvisioned → Provisioning
4. Observe log: State Provisioning → Ready
5. Execute command on target
6. Observe log: State Ready → Executing
7. Observe log: State Executing → Ready
8. Teardown target
9. Observe log: State Ready → TearingDown → Terminated

**Verification:**
- [ ] All state transitions logged with timestamps
- [ ] StateChanged event fired for each transition
- [ ] No invalid transitions occur

### Scenario 5: Configuration Validation Before Creation

**Persona:** Developer with invalid configuration

**Steps:**
1. Create config file with missing required field (e.g., no `host` for SSH)
2. Run `acode compute validate --config invalid.yaml`
3. Error lists all validation failures
4. Fix configuration
5. Run `acode compute validate --config fixed.yaml` → Valid
6. Run `acode compute create --config fixed.yaml` → Success

**Verification:**
- [ ] Validation catches all issues before resource allocation
- [ ] Error messages identify specific fields
- [ ] Valid configuration creates target successfully

### Scenario 6: Graceful Cleanup on Process Cancellation

**Persona:** Developer cancels long-running operation

**Steps:**
1. Create local target
2. Start long-running command: `acode run "sleep 300"`
3. Press Ctrl+C during execution
4. Observe: Running process killed within 1 second
5. Observe: Target state transitions to TearingDown
6. Observe: Target state transitions to Terminated
7. Run `acode compute list` → No active targets

**Verification:**
- [ ] Cancellation is responsive (<1s)
- [ ] No orphan processes left running
- [ ] Target cleaned up properly

---

## Testing Requirements

### Unit Tests (UT-029-01 to UT-029-30)

- [ ] UT-029-01: `ComputeTargetId.New()` generates unique IDs
- [ ] UT-029-02: `ComputeTargetId.Parse()` handles valid/invalid inputs
- [ ] UT-029-03: `ComputeTargetId` equality works correctly
- [ ] UT-029-04: `ComputeTargetState` enum has all required values
- [ ] UT-029-05: State machine validates all legal transitions
- [ ] UT-029-06: State machine rejects all illegal transitions
- [ ] UT-029-07: `ExecutionResult` captures stdout, stderr, exit code
- [ ] UT-029-08: `ExecutionResult` calculates duration correctly
- [ ] UT-029-09: Factory creates targets with correct initial state
- [ ] UT-029-10: Factory validates configuration before creation
- [ ] UT-029-11: Factory tracks active targets correctly
- [ ] UT-029-12: Factory enforces concurrent limit
- [ ] UT-029-13: Factory disposes all targets on disposal
- [ ] UT-029-14: Mode validator allows valid combinations
- [ ] UT-029-15: Mode validator rejects invalid combinations
- [ ] UT-029-16: Mode validator message includes all context
- [ ] UT-029-17: `LocalTargetConfig` validates required properties
- [ ] UT-029-18: `SshTargetConfig` validates host, port, user
- [ ] UT-029-19: `Ec2TargetConfig` validates region, instance type
- [ ] UT-029-20: Config environment substitution works
- [ ] UT-029-21: Config missing env var throws with var name
- [ ] UT-029-22: Events include correct properties
- [ ] UT-029-23: Events are immutable
- [ ] UT-029-24: Exceptions include required context
- [ ] UT-029-25: Exceptions are serializable
- [ ] UT-029-26: LocalComputeTarget executes process correctly
- [ ] UT-029-27: LocalComputeTarget captures output streams
- [ ] UT-029-28: LocalComputeTarget respects cancellation
- [ ] UT-029-29: LocalComputeTarget respects timeout
- [ ] UT-029-30: LocalComputeTarget kills process on dispose

### Integration Tests (IT-029-01 to IT-029-15)

- [ ] IT-029-01: Full local target lifecycle (create → prepare → execute → teardown)
- [ ] IT-029-02: Multiple concurrent local targets
- [ ] IT-029-03: Target survives multiple command executions
- [ ] IT-029-04: Factory cleanup on application shutdown
- [ ] IT-029-05: Mode blocking prevents SSH/EC2 in local-only mode
- [ ] IT-029-06: Mode change allows previously blocked target types
- [ ] IT-029-07: CLI list command shows correct target states
- [ ] IT-029-08: CLI create command creates target successfully
- [ ] IT-029-09: CLI teardown command removes target
- [ ] IT-029-10: Configuration loaded from YAML correctly
- [ ] IT-029-11: Events published to event bus
- [ ] IT-029-12: Metrics exposed correctly
- [ ] IT-029-13: Cancellation propagates through full stack
- [ ] IT-029-14: Error recovery leaves system in clean state
- [ ] IT-029-15: Cross-platform execution (Windows, macOS, Linux)

---

## Implementation Prompt

You are implementing the ComputeTarget interface for the Acode project. This is the core abstraction enabling execution on local, SSH, and cloud targets. Follow Clean Architecture principles with TDD.

### Part 1: File Structure and Domain Models

#### File Structure

```
src/Acode.Domain/
├── Compute/
│   ├── ComputeTargetType.cs
│   ├── ComputeTargetState.cs
│   ├── ComputeTargetId.cs
│   ├── IComputeTarget.cs
│   ├── TargetMetadata.cs
│   ├── Events/
│   │   ├── TargetCreatedEvent.cs
│   │   ├── TargetStateChangedEvent.cs
│   │   ├── TargetPreparedEvent.cs
│   │   ├── ExecutionStartedEvent.cs
│   │   ├── ExecutionCompletedEvent.cs
│   │   ├── ArtifactTransferredEvent.cs
│   │   └── TargetTornDownEvent.cs
│   ├── Configs/
│   │   ├── ComputeTargetConfig.cs
│   │   ├── WorkspaceConfig.cs
│   │   ├── ExecutionCommand.cs
│   │   └── ArtifactTransferConfig.cs
│   ├── Results/
│   │   ├── ExecutionResult.cs
│   │   └── TransferResult.cs
│   └── Exceptions/
│       ├── ComputeTargetException.cs
│       ├── ModeViolationException.cs
│       ├── TargetUnavailableException.cs
│       └── TargetLimitExceededException.cs

src/Acode.Application/
├── Compute/
│   ├── IComputeTargetFactory.cs
│   ├── ITargetRegistry.cs
│   ├── IModeValidator.cs
│   ├── TargetInfo.cs
│   ├── ValidationResult.cs
│   └── Commands/
│       ├── CreateTargetCommand.cs
│       ├── TeardownTargetCommand.cs
│       └── Handlers/
│           ├── CreateTargetCommandHandler.cs
│           └── TeardownTargetCommandHandler.cs

src/Acode.Infrastructure/
├── Compute/
│   ├── ComputeTargetFactory.cs
│   ├── TargetRegistry.cs
│   ├── ModeValidator.cs
│   ├── Local/
│   │   └── LocalComputeTarget.cs
│   ├── StateManagement/
│   │   ├── TargetStateManager.cs
│   │   └── AtomicStateTransition.cs
│   └── Configuration/
│       ├── ComputeTargetOptions.cs
│       └── ComputeServiceCollectionExtensions.cs

src/Acode.Cli/
├── Commands/
│   └── Compute/
│       ├── ComputeCommand.cs
│       ├── ListTargetsCommand.cs
│       ├── CreateTargetCommand.cs
│       ├── TargetStatusCommand.cs
│       └── TeardownTargetCommand.cs

tests/Acode.Domain.Tests/
├── Compute/
│   ├── ComputeTargetIdTests.cs
│   ├── ComputeTargetStateTests.cs
│   ├── ExecutionResultTests.cs
│   └── Events/
│       └── TargetEventTests.cs

tests/Acode.Application.Tests/
├── Compute/
│   ├── CreateTargetCommandHandlerTests.cs
│   └── TeardownTargetCommandHandlerTests.cs

tests/Acode.Infrastructure.Tests/
├── Compute/
│   ├── ComputeTargetFactoryTests.cs
│   ├── TargetRegistryTests.cs
│   ├── ModeValidatorTests.cs
│   ├── Local/
│   │   └── LocalComputeTargetTests.cs
│   └── StateManagement/
│       └── TargetStateManagerTests.cs

tests/Acode.Integration.Tests/
├── Compute/
│   ├── TargetLifecycleTests.cs
│   ├── ConcurrentTargetTests.cs
│   └── ModeComplianceTests.cs
```

#### Domain Models

```csharp
// src/Acode.Domain/Compute/ComputeTargetType.cs
namespace Acode.Domain.Compute;

public enum ComputeTargetType
{
    Local = 0,
    SSH = 1,
    EC2 = 2
}

// src/Acode.Domain/Compute/ComputeTargetState.cs
namespace Acode.Domain.Compute;

public enum ComputeTargetState
{
    Created = 0,
    Preparing = 1,
    Ready = 2,
    Busy = 3,
    Tearingdown = 4,
    Terminated = 5,
    Failed = 6
}

// src/Acode.Domain/Compute/ComputeTargetId.cs
namespace Acode.Domain.Compute;

public readonly record struct ComputeTargetId
{
    public string Value { get; }
    
    private ComputeTargetId(string value) => Value = value;
    
    public static ComputeTargetId New() => new(Ulid.NewUlid().ToString());
    
    public static ComputeTargetId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Target ID cannot be empty", nameof(value));
        return new ComputeTargetId(value);
    }
    
    public override string ToString() => Value;
    public static implicit operator string(ComputeTargetId id) => id.Value;
}

// src/Acode.Domain/Compute/TargetMetadata.cs
namespace Acode.Domain.Compute;

public sealed class TargetMetadata
{
    private readonly Dictionary<string, object> _data = new();
    
    public IReadOnlyDictionary<string, object> Data => _data;
    
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public DateTimeOffset? TerminatedAt { get; private set; }
    public string? Region { get; init; }
    public string? InstanceType { get; init; }
    public string? Host { get; init; }
    
    public void Set(string key, object value) => _data[key] = value;
    public T? Get<T>(string key) => _data.TryGetValue(key, out var v) ? (T)v : default;
    
    public void MarkReady() => ReadyAt = DateTimeOffset.UtcNow;
    public void MarkTerminated() => TerminatedAt = DateTimeOffset.UtcNow;
}
```

**End of Task 029 Specification - Part 1/5**

### Part 2: Core Interface and Events

```csharp
// src/Acode.Domain/Compute/IComputeTarget.cs
namespace Acode.Domain.Compute;

public interface IComputeTarget : IAsyncDisposable
{
    ComputeTargetId Id { get; }
    ComputeTargetType Type { get; }
    ComputeTargetState State { get; }
    TargetMetadata Metadata { get; }
    
    event EventHandler<TargetStateChangedEvent>? StateChanged;
    
    Task PrepareWorkspaceAsync(
        WorkspaceConfig config,
        IProgress<PreparationProgress>? progress = null,
        CancellationToken ct = default);
    
    Task<ExecutionResult> ExecuteAsync(
        ExecutionCommand command,
        CancellationToken ct = default);
    
    Task<TransferResult> UploadAsync(
        ArtifactTransferConfig config,
        CancellationToken ct = default);
    
    Task<TransferResult> DownloadAsync(
        ArtifactTransferConfig config,
        CancellationToken ct = default);
    
    Task TeardownAsync(CancellationToken ct = default);
}

// src/Acode.Domain/Compute/Configs/WorkspaceConfig.cs
namespace Acode.Domain.Compute.Configs;

public sealed record WorkspaceConfig
{
    public required string SourcePath { get; init; }
    public required string Ref { get; init; }
    public required string WorktreePath { get; init; }
    public bool CleanBeforeSync { get; init; } = true;
    public CacheConfig? Cache { get; init; }
    public DependencyConfig? Dependencies { get; init; }
    public IReadOnlyList<string>? PrepareCommands { get; init; }
}

public sealed record CacheConfig(bool Enabled, string CachePath);

public sealed record DependencyConfig(
    bool AutoDetect,
    IReadOnlyList<string>? CustomCommands);

// src/Acode.Domain/Compute/Configs/ExecutionCommand.cs
namespace Acode.Domain.Compute.Configs;

public sealed record ExecutionCommand
{
    public required string Command { get; init; }
    public IReadOnlyList<string>? Arguments { get; init; }
    public string? WorkingDirectory { get; init; }
    public IReadOnlyDictionary<string, string>? Environment { get; init; }
    public TimeSpan? Timeout { get; init; }
    public bool CaptureOutput { get; init; } = true;
    public bool StreamOutput { get; init; } = false;
}

// src/Acode.Domain/Compute/Configs/ArtifactTransferConfig.cs
namespace Acode.Domain.Compute.Configs;

public sealed record ArtifactTransferConfig
{
    public required string LocalPath { get; init; }
    public required string RemotePath { get; init; }
    public bool Recursive { get; init; } = false;
    public bool PreservePermissions { get; init; } = true;
    public IReadOnlyList<string>? ExcludePatterns { get; init; }
}

// src/Acode.Domain/Compute/Results/ExecutionResult.cs
namespace Acode.Domain.Compute.Results;

public sealed record ExecutionResult
{
    public required int ExitCode { get; init; }
    public string? StandardOutput { get; init; }
    public string? StandardError { get; init; }
    public required TimeSpan Duration { get; init; }
    public bool TimedOut { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
    
    public bool IsSuccess => ExitCode == 0 && !TimedOut;
}

// src/Acode.Domain/Compute/Results/TransferResult.cs
namespace Acode.Domain.Compute.Results;

public sealed record TransferResult
{
    public required bool Success { get; init; }
    public required long BytesTransferred { get; init; }
    public required TimeSpan Duration { get; init; }
    public int FilesTransferred { get; init; }
    public string? ErrorMessage { get; init; }
}

// src/Acode.Domain/Compute/Events/TargetCreatedEvent.cs
namespace Acode.Domain.Compute.Events;

public sealed record TargetCreatedEvent(
    ComputeTargetId TargetId,
    ComputeTargetType Type,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Events/TargetStateChangedEvent.cs
namespace Acode.Domain.Compute.Events;

public sealed record TargetStateChangedEvent(
    ComputeTargetId TargetId,
    ComputeTargetState OldState,
    ComputeTargetState NewState,
    string? Reason,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Events/ExecutionStartedEvent.cs
namespace Acode.Domain.Compute.Events;

public sealed record ExecutionStartedEvent(
    ComputeTargetId TargetId,
    string Command,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Events/ExecutionCompletedEvent.cs
namespace Acode.Domain.Compute.Events;

public sealed record ExecutionCompletedEvent(
    ComputeTargetId TargetId,
    int ExitCode,
    TimeSpan Duration,
    bool TimedOut,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Events/TargetTornDownEvent.cs
namespace Acode.Domain.Compute.Events;

public sealed record TargetTornDownEvent(
    ComputeTargetId TargetId,
    TimeSpan TotalLifetime,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 029 Specification - Part 2/5**

### Part 3: Application Layer - Factory and Registry

```csharp
// src/Acode.Application/Compute/IComputeTargetFactory.cs
namespace Acode.Application.Compute;

public interface IComputeTargetFactory
{
    Task<IComputeTarget> CreateAsync(
        ComputeTargetConfig config,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<TargetInfo>> GetAvailableTargetsAsync(
        CancellationToken ct = default);
    
    Task<ValidationResult> ValidateConfigAsync(
        ComputeTargetConfig config,
        CancellationToken ct = default);
    
    Task DisposeAllAsync(CancellationToken ct = default);
}

// src/Acode.Application/Compute/ITargetRegistry.cs
namespace Acode.Application.Compute;

public interface ITargetRegistry
{
    void Register(IComputeTarget target);
    void Unregister(ComputeTargetId id);
    IComputeTarget? Get(ComputeTargetId id);
    IReadOnlyList<IComputeTarget> GetAll();
    IReadOnlyList<IComputeTarget> GetByState(ComputeTargetState state);
    IReadOnlyList<IComputeTarget> GetByType(ComputeTargetType type);
    int Count { get; }
    int CountByState(ComputeTargetState state);
}

// src/Acode.Application/Compute/IModeValidator.cs
namespace Acode.Application.Compute;

public interface IModeValidator
{
    bool IsTargetTypeAllowed(ComputeTargetType type);
    void ValidateOrThrow(ComputeTargetType type);
    IReadOnlySet<ComputeTargetType> GetAllowedTypes();
}

// src/Acode.Application/Compute/TargetInfo.cs
namespace Acode.Application.Compute;

public sealed record TargetInfo(
    ComputeTargetId Id,
    ComputeTargetType Type,
    ComputeTargetState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadyAt,
    IReadOnlyDictionary<string, object> Metadata);

// src/Acode.Application/Compute/ValidationResult.cs
namespace Acode.Application.Compute;

public sealed record ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];
    
    public static ValidationResult Valid() => new() { IsValid = true };
    
    public static ValidationResult Invalid(params ValidationError[] errors) =>
        new() { IsValid = false, Errors = errors };
}

public sealed record ValidationError(string Code, string Message, string? Field = null);

// src/Acode.Application/Compute/ComputeTargetConfig.cs
namespace Acode.Application.Compute;

public sealed record ComputeTargetConfig
{
    public required ComputeTargetType Type { get; init; }
    public string? Name { get; init; }
    public SshTargetConfig? Ssh { get; init; }
    public Ec2TargetConfig? Ec2 { get; init; }
    public LocalTargetConfig? Local { get; init; }
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromMinutes(30);
    public bool AutoTeardown { get; init; } = true;
}

public sealed record SshTargetConfig(
    string Host,
    string User,
    string? KeyPath,
    int Port = 22);

public sealed record Ec2TargetConfig(
    string Region,
    string InstanceType,
    string KeyName,
    string? SubnetId,
    string? SecurityGroupId,
    string? AmiId);

public sealed record LocalTargetConfig(
    string? WorkingDirectory);

// src/Acode.Application/Compute/Commands/CreateTargetCommand.cs
namespace Acode.Application.Compute.Commands;

public sealed record CreateTargetCommand(ComputeTargetConfig Config) 
    : ICommand<IComputeTarget>;

// src/Acode.Application/Compute/Commands/TeardownTargetCommand.cs
namespace Acode.Application.Compute.Commands;

public sealed record TeardownTargetCommand(ComputeTargetId TargetId) 
    : ICommand<bool>;

// src/Acode.Application/Compute/Commands/Handlers/CreateTargetCommandHandler.cs
namespace Acode.Application.Compute.Commands.Handlers;

public sealed class CreateTargetCommandHandler 
    : ICommandHandler<CreateTargetCommand, IComputeTarget>
{
    private readonly IComputeTargetFactory _factory;
    private readonly IModeValidator _modeValidator;
    private readonly IAuditLogger _auditLogger;
    
    public CreateTargetCommandHandler(
        IComputeTargetFactory factory,
        IModeValidator modeValidator,
        IAuditLogger auditLogger)
    {
        _factory = factory;
        _modeValidator = modeValidator;
        _auditLogger = auditLogger;
    }
    
    public async Task<IComputeTarget> HandleAsync(
        CreateTargetCommand command,
        CancellationToken ct)
    {
        _modeValidator.ValidateOrThrow(command.Config.Type);
        
        var validation = await _factory.ValidateConfigAsync(command.Config, ct);
        if (!validation.IsValid)
        {
            throw new ComputeTargetException(
                $"Invalid config: {string.Join(", ", validation.Errors.Select(e => e.Message))}");
        }
        
        var target = await _factory.CreateAsync(command.Config, ct);
        
        await _auditLogger.LogAsync(new AuditEntry
        {
            Action = "ComputeTarget.Created",
            TargetId = target.Id.Value,
            TargetType = target.Type.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        });
        
        return target;
    }
}
```

**End of Task 029 Specification - Part 3/5**

### Part 4: Infrastructure - Factory and Local Target

```csharp
// src/Acode.Infrastructure/Compute/ComputeTargetFactory.cs
namespace Acode.Infrastructure.Compute;

public sealed class ComputeTargetFactory : IComputeTargetFactory, IAsyncDisposable
{
    private readonly ITargetRegistry _registry;
    private readonly IModeValidator _modeValidator;
    private readonly IServiceProvider _services;
    private readonly ComputeTargetOptions _options;
    private readonly ILogger<ComputeTargetFactory> _logger;
    private readonly SemaphoreSlim _creationLock = new(1, 1);
    
    public ComputeTargetFactory(
        ITargetRegistry registry,
        IModeValidator modeValidator,
        IServiceProvider services,
        IOptions<ComputeTargetOptions> options,
        ILogger<ComputeTargetFactory> logger)
    {
        _registry = registry;
        _modeValidator = modeValidator;
        _services = services;
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<IComputeTarget> CreateAsync(
        ComputeTargetConfig config,
        CancellationToken ct = default)
    {
        _modeValidator.ValidateOrThrow(config.Type);
        
        await _creationLock.WaitAsync(ct);
        try
        {
            if (_registry.Count >= _options.MaxConcurrentTargets)
            {
                throw new TargetLimitExceededException(
                    $"Max concurrent targets ({_options.MaxConcurrentTargets}) reached");
            }
            
            var target = CreateTargetInstance(config);
            _registry.Register(target);
            
            _logger.LogInformation(
                "Created compute target {TargetId} of type {Type}",
                target.Id, target.Type);
            
            return target;
        }
        finally
        {
            _creationLock.Release();
        }
    }
    
    private IComputeTarget CreateTargetInstance(ComputeTargetConfig config)
    {
        return config.Type switch
        {
            ComputeTargetType.Local => CreateLocalTarget(config),
            ComputeTargetType.SSH => CreateSshTarget(config),
            ComputeTargetType.EC2 => CreateEc2Target(config),
            _ => throw new ArgumentException($"Unknown target type: {config.Type}")
        };
    }
    
    private LocalComputeTarget CreateLocalTarget(ComputeTargetConfig config)
    {
        var stateManager = _services.GetRequiredService<ITargetStateManager>();
        var processRunner = _services.GetRequiredService<IProcessRunner>();
        var fileSystem = _services.GetRequiredService<IFileSystem>();
        var logger = _services.GetRequiredService<ILogger<LocalComputeTarget>>();
        
        return new LocalComputeTarget(
            ComputeTargetId.New(),
            config.Local ?? new LocalTargetConfig(null),
            stateManager,
            processRunner,
            fileSystem,
            logger);
    }
    
    // SSH and EC2 targets implemented in Tasks 030 and 031
    private IComputeTarget CreateSshTarget(ComputeTargetConfig config)
        => throw new NotImplementedException("SSH target in Task 030");
    
    private IComputeTarget CreateEc2Target(ComputeTargetConfig config)
        => throw new NotImplementedException("EC2 target in Task 031");
    
    public Task<IReadOnlyList<TargetInfo>> GetAvailableTargetsAsync(
        CancellationToken ct = default)
    {
        var targets = _registry.GetAll()
            .Select(t => new TargetInfo(
                t.Id, t.Type, t.State,
                t.Metadata.CreatedAt,
                t.Metadata.ReadyAt,
                t.Metadata.Data))
            .ToList();
        
        return Task.FromResult<IReadOnlyList<TargetInfo>>(targets);
    }
    
    public Task<ValidationResult> ValidateConfigAsync(
        ComputeTargetConfig config,
        CancellationToken ct = default)
    {
        var errors = new List<ValidationError>();
        
        if (!_modeValidator.IsTargetTypeAllowed(config.Type))
        {
            errors.Add(new ValidationError(
                "MODE_VIOLATION",
                $"Target type {config.Type} not allowed in current mode",
                nameof(config.Type)));
        }
        
        switch (config.Type)
        {
            case ComputeTargetType.SSH when config.Ssh is null:
                errors.Add(new ValidationError(
                    "MISSING_CONFIG", "SSH config required", nameof(config.Ssh)));
                break;
            case ComputeTargetType.EC2 when config.Ec2 is null:
                errors.Add(new ValidationError(
                    "MISSING_CONFIG", "EC2 config required", nameof(config.Ec2)));
                break;
        }
        
        return Task.FromResult(errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(errors.ToArray()));
    }
    
    public async Task DisposeAllAsync(CancellationToken ct = default)
    {
        var targets = _registry.GetAll().ToList();
        foreach (var target in targets)
        {
            try
            {
                await target.TeardownAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to teardown target {TargetId}", target.Id);
            }
            finally
            {
                _registry.Unregister(target.Id);
            }
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        await DisposeAllAsync();
        _creationLock.Dispose();
    }
}

// src/Acode.Infrastructure/Compute/TargetRegistry.cs
namespace Acode.Infrastructure.Compute;

public sealed class TargetRegistry : ITargetRegistry
{
    private readonly ConcurrentDictionary<ComputeTargetId, IComputeTarget> _targets = new();
    
    public void Register(IComputeTarget target)
    {
        if (!_targets.TryAdd(target.Id, target))
            throw new InvalidOperationException($"Target {target.Id} already registered");
    }
    
    public void Unregister(ComputeTargetId id) => _targets.TryRemove(id, out _);
    
    public IComputeTarget? Get(ComputeTargetId id) =>
        _targets.TryGetValue(id, out var t) ? t : null;
    
    public IReadOnlyList<IComputeTarget> GetAll() => _targets.Values.ToList();
    
    public IReadOnlyList<IComputeTarget> GetByState(ComputeTargetState state) =>
        _targets.Values.Where(t => t.State == state).ToList();
    
    public IReadOnlyList<IComputeTarget> GetByType(ComputeTargetType type) =>
        _targets.Values.Where(t => t.Type == type).ToList();
    
    public int Count => _targets.Count;
    
    public int CountByState(ComputeTargetState state) =>
        _targets.Values.Count(t => t.State == state);
}

// src/Acode.Infrastructure/Compute/ModeValidator.cs
namespace Acode.Infrastructure.Compute;

public sealed class ModeValidator : IModeValidator
{
    private readonly IOperatingModeProvider _modeProvider;
    
    private static readonly IReadOnlyDictionary<OperatingMode, HashSet<ComputeTargetType>> 
        AllowedTypes = new Dictionary<OperatingMode, HashSet<ComputeTargetType>>
    {
        [OperatingMode.LocalOnly] = [ComputeTargetType.Local],
        [OperatingMode.Burst] = [ComputeTargetType.Local, ComputeTargetType.SSH, ComputeTargetType.EC2],
        [OperatingMode.Airgapped] = [ComputeTargetType.Local]
    };
    
    public ModeValidator(IOperatingModeProvider modeProvider)
    {
        _modeProvider = modeProvider;
    }
    
    public bool IsTargetTypeAllowed(ComputeTargetType type)
    {
        var mode = _modeProvider.CurrentMode;
        return AllowedTypes.TryGetValue(mode, out var allowed) && allowed.Contains(type);
    }
    
    public void ValidateOrThrow(ComputeTargetType type)
    {
        if (!IsTargetTypeAllowed(type))
        {
            throw new ModeViolationException(
                $"Target type {type} is not allowed in {_modeProvider.CurrentMode} mode");
        }
    }
    
    public IReadOnlySet<ComputeTargetType> GetAllowedTypes()
    {
        var mode = _modeProvider.CurrentMode;
        return AllowedTypes.TryGetValue(mode, out var allowed) 
            ? allowed 
            : new HashSet<ComputeTargetType>();
    }
}
```

**End of Task 029 Specification - Part 4/5**

### Part 5: Local Target, State Management, CLI, and Rollout

```csharp
// src/Acode.Infrastructure/Compute/Local/LocalComputeTarget.cs
namespace Acode.Infrastructure.Compute.Local;

public sealed class LocalComputeTarget : IComputeTarget
{
    private readonly ITargetStateManager _stateManager;
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<LocalComputeTarget> _logger;
    
    public ComputeTargetId Id { get; }
    public ComputeTargetType Type => ComputeTargetType.Local;
    public ComputeTargetState State => _stateManager.GetState(Id);
    public TargetMetadata Metadata { get; }
    
    public event EventHandler<TargetStateChangedEvent>? StateChanged;
    
    public LocalComputeTarget(
        ComputeTargetId id,
        LocalTargetConfig config,
        ITargetStateManager stateManager,
        IProcessRunner processRunner,
        IFileSystem fileSystem,
        ILogger<LocalComputeTarget> logger)
    {
        Id = id;
        _stateManager = stateManager;
        _processRunner = processRunner;
        _fileSystem = fileSystem;
        _logger = logger;
        
        Metadata = new TargetMetadata { CreatedAt = DateTimeOffset.UtcNow };
        _stateManager.SetState(Id, ComputeTargetState.Created);
    }
    
    public async Task PrepareWorkspaceAsync(
        WorkspaceConfig config,
        IProgress<PreparationProgress>? progress = null,
        CancellationToken ct = default)
    {
        await TransitionStateAsync(ComputeTargetState.Preparing, ct);
        
        try
        {
            progress?.Report(new PreparationProgress(
                PreparationPhase.Creating, 0, "Creating workspace"));
            
            _fileSystem.Directory.CreateDirectory(config.WorktreePath);
            
            if (config.CleanBeforeSync)
            {
                progress?.Report(new PreparationProgress(
                    PreparationPhase.Cleaning, 10, "Cleaning workspace"));
                CleanDirectory(config.WorktreePath);
            }
            
            progress?.Report(new PreparationProgress(
                PreparationPhase.Syncing, 30, "Syncing source"));
            
            // Local target: copy or link files
            await SyncLocalAsync(config.SourcePath, config.WorktreePath, ct);
            
            progress?.Report(new PreparationProgress(
                PreparationPhase.Completed, 100, "Workspace ready"));
            
            await TransitionStateAsync(ComputeTargetState.Ready, ct);
            Metadata.MarkReady();
        }
        catch
        {
            await TransitionStateAsync(ComputeTargetState.Failed, ct);
            throw;
        }
    }
    
    public async Task<ExecutionResult> ExecuteAsync(
        ExecutionCommand command,
        CancellationToken ct = default)
    {
        if (State != ComputeTargetState.Ready)
            throw new InvalidOperationException($"Target not ready: {State}");
        
        await TransitionStateAsync(ComputeTargetState.Busy, ct);
        
        try
        {
            var startedAt = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            
            var result = await _processRunner.RunAsync(
                command.Command,
                command.Arguments ?? [],
                command.WorkingDirectory,
                command.Environment,
                command.Timeout,
                ct);
            
            stopwatch.Stop();
            
            await TransitionStateAsync(ComputeTargetState.Ready, ct);
            
            return new ExecutionResult
            {
                ExitCode = result.ExitCode,
                StandardOutput = result.StdOut,
                StandardError = result.StdErr,
                Duration = stopwatch.Elapsed,
                TimedOut = result.TimedOut,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow
            };
        }
        catch
        {
            await TransitionStateAsync(ComputeTargetState.Ready, ct);
            throw;
        }
    }
    
    public Task<TransferResult> UploadAsync(
        ArtifactTransferConfig config,
        CancellationToken ct = default)
    {
        // Local target: just verify path exists
        return Task.FromResult(new TransferResult
        {
            Success = _fileSystem.File.Exists(config.LocalPath),
            BytesTransferred = 0,
            Duration = TimeSpan.Zero,
            FilesTransferred = 1
        });
    }
    
    public Task<TransferResult> DownloadAsync(
        ArtifactTransferConfig config,
        CancellationToken ct = default)
    {
        // Local target: copy file
        var stopwatch = Stopwatch.StartNew();
        _fileSystem.File.Copy(config.RemotePath, config.LocalPath, overwrite: true);
        stopwatch.Stop();
        
        var info = _fileSystem.FileInfo.New(config.LocalPath);
        return Task.FromResult(new TransferResult
        {
            Success = true,
            BytesTransferred = info.Length,
            Duration = stopwatch.Elapsed,
            FilesTransferred = 1
        });
    }
    
    public async Task TeardownAsync(CancellationToken ct = default)
    {
        await TransitionStateAsync(ComputeTargetState.Tearingdown, ct);
        // Local target: minimal cleanup
        await TransitionStateAsync(ComputeTargetState.Terminated, ct);
        Metadata.MarkTerminated();
    }
    
    public async ValueTask DisposeAsync()
    {
        if (State != ComputeTargetState.Terminated)
            await TeardownAsync();
    }
    
    private async Task TransitionStateAsync(ComputeTargetState newState, CancellationToken ct)
    {
        var oldState = State;
        _stateManager.SetState(Id, newState);
        StateChanged?.Invoke(this, new TargetStateChangedEvent(
            Id, oldState, newState, null, DateTimeOffset.UtcNow));
    }
    
    private void CleanDirectory(string path)
    {
        if (_fileSystem.Directory.Exists(path))
        {
            foreach (var file in _fileSystem.Directory.GetFiles(path))
                _fileSystem.File.Delete(file);
            foreach (var dir in _fileSystem.Directory.GetDirectories(path))
                _fileSystem.Directory.Delete(dir, true);
        }
    }
    
    private async Task SyncLocalAsync(string source, string dest, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            foreach (var file in _fileSystem.Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                var relative = Path.GetRelativePath(source, file);
                var destPath = Path.Combine(dest, relative);
                _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                _fileSystem.File.Copy(file, destPath, overwrite: true);
            }
        }, ct);
    }
}

// src/Acode.Infrastructure/Compute/StateManagement/TargetStateManager.cs
namespace Acode.Infrastructure.Compute.StateManagement;

public sealed class TargetStateManager : ITargetStateManager
{
    private readonly ConcurrentDictionary<ComputeTargetId, ComputeTargetState> _states = new();
    
    public ComputeTargetState GetState(ComputeTargetId id) =>
        _states.TryGetValue(id, out var state) ? state : ComputeTargetState.Created;
    
    public void SetState(ComputeTargetId id, ComputeTargetState state) =>
        _states[id] = state;
    
    public bool TryTransition(
        ComputeTargetId id,
        ComputeTargetState expected,
        ComputeTargetState newState)
    {
        return _states.TryUpdate(id, newState, expected);
    }
}

// src/Acode.Cli/Commands/Compute/ListTargetsCommand.cs
namespace Acode.Cli.Commands.Compute;

[Command("compute list", Description = "List all compute targets")]
public class ListTargetsCommand : ICommand
{
    private readonly IComputeTargetFactory _factory;
    private readonly IAnsiConsole _console;
    
    public ListTargetsCommand(IComputeTargetFactory factory, IAnsiConsole console)
    {
        _factory = factory;
        _console = console;
    }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var targets = await _factory.GetAvailableTargetsAsync();
        
        var table = new Table()
            .AddColumn("ID")
            .AddColumn("Type")
            .AddColumn("State")
            .AddColumn("Created")
            .AddColumn("Ready");
        
        foreach (var t in targets)
        {
            table.AddRow(
                t.Id.Value[..8],
                t.Type.ToString(),
                FormatState(t.State),
                t.CreatedAt.ToString("HH:mm:ss"),
                t.ReadyAt?.ToString("HH:mm:ss") ?? "-");
        }
        
        _console.Write(table);
    }
    
    private string FormatState(ComputeTargetState state) => state switch
    {
        ComputeTargetState.Ready => "[green]Ready[/]",
        ComputeTargetState.Busy => "[yellow]Busy[/]",
        ComputeTargetState.Failed => "[red]Failed[/]",
        _ => state.ToString()
    };
}
```

---

## Implementation Checklist

- [ ] Create Domain models (ComputeTargetId, States, Events)
- [ ] Define IComputeTarget interface with full lifecycle
- [ ] Implement config records (WorkspaceConfig, ExecutionCommand, etc.)
- [ ] Create result records (ExecutionResult, TransferResult)
- [ ] Define exception types with descriptive messages
- [ ] Implement IComputeTargetFactory in Infrastructure
- [ ] Create TargetRegistry with thread-safe operations
- [ ] Implement ModeValidator with allowlist per mode
- [ ] Build LocalComputeTarget as baseline implementation
- [ ] Create TargetStateManager with atomic transitions
- [ ] Add CLI commands for target management
- [ ] Write unit tests for all components (TDD)
- [ ] Write integration tests for full lifecycle
- [ ] Verify mode compliance blocking works
- [ ] Test concurrent target creation limits
- [ ] Document configuration options

---

## Rollout Plan

1. **Phase 1**: Domain models and interface definitions
2. **Phase 2**: Application layer (factory interface, commands)
3. **Phase 3**: Infrastructure (factory, registry, validator)
4. **Phase 4**: LocalComputeTarget implementation
5. **Phase 5**: CLI integration
6. **Phase 6**: Integration testing
7. **Phase 7**: Documentation and examples

---

**End of Task 029 Specification**