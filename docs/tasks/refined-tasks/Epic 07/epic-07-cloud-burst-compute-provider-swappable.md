# EPIC 7 — Cloud Burst Compute (Provider-Swappable)

**Priority:** P1 – High  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Epic 06 (Workers), Epic 01 (Modes), Epic 02 (Interfaces)  

---

## Epic Overview

Epic 7 implements cloud burst compute with swappable providers. When local resources are insufficient, execution MUST burst to cloud compute. Providers MUST be pluggable.

This epic defines the ComputeTarget abstraction. Local, SSH, and AWS EC2 targets MUST be supported. Each target MUST implement the same interface. Switching providers MUST require only configuration.

Burst heuristics MUST determine when to use cloud. Metrics MUST trigger automatic scaling. Placement strategies MUST be configurable. Inference and execution MAY be on different targets.

### Purpose

Acode MUST handle workloads that exceed local capacity. Cloud burst enables:
- Scalable execution
- Faster completion
- Resource optimization
- Provider flexibility

### Boundaries

This epic covers:
- ComputeTarget interface and lifecycle
- SSH-based remote execution
- AWS EC2 provisioning
- Inference/execution placement
- Burst heuristics and scaling

This epic does NOT cover:
- Kubernetes orchestration
- Multi-cloud routing
- Cost optimization AI
- Reserved instance management

### Dependencies

| Dependency | Purpose |
|------------|---------|
| Epic 06 | Worker pool integration |
| Epic 01 | Mode compliance (local-only blocks cloud) |
| Epic 02 | Interface definitions |
| Epic 04 | Audit logging for cloud ops |

---

## Outcomes

1. ComputeTarget interface MUST be defined
2. Local target MUST work (baseline)
3. SSH target MUST execute remotely
4. AWS EC2 target MUST provision instances
5. Workspace preparation MUST sync code
6. Command execution MUST work remotely
7. Artifacts MUST upload/download
8. Teardown MUST cleanup resources
9. Placement strategies MUST be configurable
10. Local-inference/cloud-exec MUST work
11. Cloud-inference/cloud-exec MUST work
12. Burst heuristics MUST trigger scaling
13. Thresholds MUST be configurable
14. Auto/always/never modes MUST work
15. Worker scaling MUST be controlled
16. All operations MUST be logged
17. Credentials MUST be secured
18. Costs MUST be trackable
19. Timeouts MUST be enforced
20. Fallback MUST work

---

## Non-Goals

1. Kubernetes deployment
2. GCP/Azure in initial release
3. Spot instance bidding
4. Multi-region failover
5. Cost prediction ML
6. Reserved instance management
7. Hybrid cloud networking
8. Service mesh integration
9. Cloud marketplace listings
10. Serverless functions (Lambda)
11. Container registries
12. Cloud-native storage
13. Cloud databases
14. IAM role automation
15. VPC peering

---

## Architecture & Integration Points

### Core Interfaces

```
IComputeTarget
├── Id: string
├── Type: ComputeTargetType
├── PrepareWorkspaceAsync(config) → void
├── ExecuteAsync(command) → ExecResult
├── UploadAsync(local, remote) → void
├── DownloadAsync(remote, local) → void
└── TeardownAsync() → void

IComputeTargetFactory
├── CreateAsync(config) → IComputeTarget
├── GetAvailableTargetsAsync() → IEnumerable<TargetInfo>
└── ValidateConfigAsync(config) → ValidationResult

IBurstHeuristics
├── ShouldBurstAsync(metrics) → BurstDecision
├── GetScalingRecommendationAsync() → ScaleRecommendation
└── RecordMetricsAsync(metrics) → void

IPlacementStrategy
├── GetInferenceTargetAsync(task) → IComputeTarget
├── GetExecutionTargetAsync(task) → IComputeTarget
└── GetPlacementPlanAsync(batch) → PlacementPlan
```

### Events

| Event | Trigger |
|-------|---------|
| TargetCreated | Compute target provisioned |
| TargetReady | Workspace prepared |
| ExecutionStarted | Command began |
| ExecutionCompleted | Command finished |
| ArtifactUploaded | File sent to target |
| ArtifactDownloaded | File retrieved |
| TargetTornDown | Resources released |
| BurstTriggered | Cloud scaling started |
| BurstCompleted | Cloud scaling done |

### Data Contracts

```
ComputeTargetConfig
├── type: string (local, ssh, ec2)
├── credentials: CredentialRef
├── region: string?
├── instanceType: string?
├── sshHost: string?
├── sshUser: string?
└── options: Dictionary

BurstDecision
├── shouldBurst: bool
├── targetCount: int
├── reason: string
├── metrics: Dictionary
└── confidence: float

PlacementPlan
├── inferenceTarget: IComputeTarget
├── executionTarget: IComputeTarget
├── strategy: string
└── rationale: string
```

---

## Operational Considerations

### Mode Compliance

| Mode | Cloud |
|------|-------|
| local-only | BLOCKED |
| burst | ALLOWED |
| airgapped | BLOCKED |

### Safety

- Credentials MUST NOT be logged
- Cloud resources MUST have auto-teardown
- Timeouts MUST prevent runaway costs
- Failed teardown MUST alert
- Orphaned resources MUST be detectable

### Audit

- All cloud operations MUST be logged
- Instance lifecycle MUST be tracked
- Costs MUST be attributable to tasks
- Credentials access MUST be audited

---

## Acceptance Criteria / Definition of Done

### ComputeTarget Interface (Task 029)
- [ ] AC-001: Interface defined
- [ ] AC-002: Local target works
- [ ] AC-003: Workspace prepares
- [ ] AC-004: Commands execute
- [ ] AC-005: Artifacts upload
- [ ] AC-006: Artifacts download
- [ ] AC-007: Teardown works
- [ ] AC-008: Errors handled

### SSH Target (Task 030)
- [ ] AC-009: SSH connection works
- [ ] AC-010: Repo syncs
- [ ] AC-011: Remote exec works
- [ ] AC-012: Caching works
- [ ] AC-013: Artifacts pull back
- [ ] AC-014: Cleanup works

### AWS EC2 Target (Task 031)
- [ ] AC-015: Instance launches
- [ ] AC-016: IaC templates work
- [ ] AC-017: Bootstrap runs
- [ ] AC-018: Spawn-per-task works
- [ ] AC-019: Pooled mode works
- [ ] AC-020: Termination works
- [ ] AC-021: Cost tracking works

### Placement (Task 032)
- [ ] AC-022: Local-local works
- [ ] AC-023: Cloud-cloud works
- [ ] AC-024: Local-cloud works
- [ ] AC-025: Strategy configurable
- [ ] AC-026: Placement logged

### Burst Heuristics (Task 033)
- [ ] AC-027: Thresholds work
- [ ] AC-028: Metrics tracked
- [ ] AC-029: Auto mode works
- [ ] AC-030: Always mode works
- [ ] AC-031: Never mode works
- [ ] AC-032: Scaling controls work

### Cross-Cutting
- [ ] AC-033: Mode compliance
- [ ] AC-034: Credentials secured
- [ ] AC-035: Audit complete
- [ ] AC-036: Performance OK
- [ ] AC-037: Docs complete
- [ ] AC-038: Tests pass

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Cloud costs runaway | High | Hard limits, alerts |
| Credential leak | Critical | Vault, no logging |
| Orphaned instances | High | Auto-teardown, sweeper |
| Network issues | Medium | Retries, timeouts |
| Provider outage | Medium | Fallback to local |
| Region unavailable | Medium | Multi-region config |
| Instance quota | Medium | Pre-check, alert |
| SSH key management | High | Rotate, vault |
| Bootstrap failure | Medium | Retry, fallback |
| Artifact corruption | Medium | Checksums |
| Clock skew | Low | NTP sync |
| Rate limiting | Medium | Backoff, queuing |

---

## Milestone Plan

### Milestone 1: Interface + Local (Week 1)
- Task 029: ComputeTarget interface
- Task 029.a-d: Workspace, exec, artifacts, teardown

### Milestone 2: SSH Target (Week 2)
- Task 030: SSH implementation
- Task 030.a-c: Sync, exec, artifacts

### Milestone 3: AWS EC2 (Week 3)
- Task 031: EC2 implementation
- Task 031.a-c: IaC, bootstrap, modes

### Milestone 4: Placement + Heuristics (Week 4)
- Task 032: Placement strategies
- Task 033: Burst heuristics

---

## Definition of Epic Complete

- [ ] ComputeTarget interface stable
- [ ] Local target functional
- [ ] SSH target functional
- [ ] EC2 target functional
- [ ] Placement strategies work
- [ ] Burst heuristics work
- [ ] Mode compliance verified
- [ ] Credentials secured
- [ ] Auto-teardown tested
- [ ] Cost tracking works
- [ ] All CLI commands work
- [ ] Documentation complete
- [ ] Unit tests pass (>80%)
- [ ] Integration tests pass
- [ ] E2E cloud tests pass
- [ ] Security review passed
- [ ] Performance acceptable
- [ ] Audit logging complete
- [ ] Fallback works
- [ ] Orphan detection works

---

**END OF EPIC 7**