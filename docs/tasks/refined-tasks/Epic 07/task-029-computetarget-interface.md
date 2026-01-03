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