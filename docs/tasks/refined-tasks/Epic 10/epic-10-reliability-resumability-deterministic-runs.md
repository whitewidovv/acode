# EPIC 10 — Reliability, Resumability, Deterministic Runs

**Priority:** P0 – Critical  
**Phase:** Phase 10 – Core Reliability  
**Dependencies:** Task 039 (Audit), Task 050 (Workspace DB), Task 038 (Secrets)  

---

## Epic Overview

Epic 10 establishes the reliability foundation for the agentic coding bot. This includes crash-safe event logging, intelligent retry policies, and reproducibility tooling that enables debugging and deterministic replay.

The core insight is that agent operations must survive failures gracefully. When a crash occurs, the agent must resume exactly where it left off, not from the beginning. This requires append-only event logs with ordering guarantees.

Retry policies determine how the agent responds to failures. Not all failures are equal—some are transient (network), some are permanent (permission denied), and some require human intervention. The policy framework categorizes failures and applies appropriate responses.

Reproducibility enables debugging by allowing exact replay of agent sessions. All prompts, settings, and tool calls are persisted (with secrets redacted) so that issues can be reproduced and fixed.

### Purpose

Ensure agent operations are:
- **Crash-safe**: Survive failures and resume
- **Reliable**: Handle transient failures gracefully
- **Reproducible**: Enable debugging via replay
- **Deterministic**: Same inputs produce same outputs

### Boundaries

This epic covers runtime reliability infrastructure. It does not cover:
- Application-level testing (Epic 14)
- Performance optimization (separate concern)
- Distributed coordination (single-agent focus)

### Key Principles

1. **Append-only**: Events never modified after write
2. **Ordered**: Strict sequence guarantees
3. **Idempotent**: Safe to replay
4. **Categorized**: Failures have proper types
5. **Bounded**: Retries are capped
6. **Human-aware**: Know when to escalate

---

## Outcomes

1. Agent survives crashes and resumes correctly
2. Event log captures all state changes
3. Events are strictly ordered
4. Events are append-only (immutable)
5. Resume rules clearly defined
6. Resume point accurately identified
7. Transient failures retried automatically
8. Permanent failures reported immediately
9. Retry count is bounded
10. Human escalation triggers correctly
11. Session state persisted for replay
12. Prompts/settings captured (redacted)
13. Deterministic mode available
14. Replay tooling functional
15. Event sourcing enables debugging
16. No data loss on crash
17. Graceful degradation always
18. Clear failure categorization
19. Audit trail complete
20. Performance acceptable

---

## Non-Goals

1. Distributed agent coordination
2. Multi-machine state sync
3. Real-time event streaming
4. Complex event processing (CEP)
5. Event analytics
6. Historical trend analysis
7. Automatic recovery from all failures
8. Zero-downtime upgrades
9. Hot code reload during run
10. Predictive failure detection
11. Machine learning for retry tuning
12. Cross-session state sharing
13. Global retry configuration UI
14. Failure dashboards
15. Alerting system integration

---

## Architecture & Integration Points

### Core Components

| Component | Responsibility | Task |
|-----------|----------------|------|
| Event Log | Append-only persistence | 040 |
| Resume Engine | Identify resume point | 040.b |
| Sequence Generator | Ordering guarantees | 040.c |
| Retry Policy Engine | Apply retry rules | 041 |
| Failure Classifier | Categorize errors | 041.a |
| Retry Counter | Track attempt counts | 041.b |
| Escalation Engine | Human transition | 041.c |
| Replay Recorder | Capture session data | 042 |
| Deterministic Toggle | Mode switches | 042.b |
| Replay Player | Re-execute session | 042.c |

### Interfaces

```csharp
public interface IEventLog
{
    Task<EventId> AppendAsync(Event @event);
    Task<IEnumerable<Event>> ReadFromAsync(SequenceNumber from);
    Task<SequenceNumber> GetLastSequenceAsync();
}

public interface IResumeEngine
{
    Task<ResumePoint?> FindResumePointAsync();
    Task MarkCompletedAsync(SequenceNumber seq);
}

public interface IRetryPolicy
{
    RetryDecision Evaluate(Exception ex, int attemptCount);
}

public interface IFailureClassifier
{
    FailureCategory Classify(Exception ex);
}

public interface IReplayRecorder
{
    Task RecordAsync(SessionData data);
    Task<SessionData> LoadAsync(SessionId id);
}
```

### Data Contracts

```csharp
public record Event(
    EventId Id,
    SequenceNumber Sequence,
    DateTimeOffset Timestamp,
    EventType Type,
    byte[] Payload,
    string? Hash
);

public enum FailureCategory
{
    Transient,      // Network, timeout
    Permanent,      // Permission, not found
    NeedsHuman,     // Approval required
    Unknown         // Requires investigation
}

public record RetryDecision(
    bool ShouldRetry,
    TimeSpan? DelayBefore,
    string? Reason
);
```

### Events Published

- `EventAppended` - New event in log
- `ResumePointIdentified` - Resume location found
- `RetryAttempt` - Retry starting
- `RetryExhausted` - Max retries reached
- `HumanEscalation` - Needs human
- `SessionRecorded` - Session captured
- `ReplayStarted` - Replay beginning
- `ReplayCompleted` - Replay finished

---

## Operational Considerations

### Operating Modes

| Mode | Event Log | Retry | Replay |
|------|-----------|-------|--------|
| Local-Only | SQLite | Full | Full |
| Burst | SQLite + sync | Full | Full |
| Airgapped | SQLite | Full | Full |

### Safety Integration

- All events redacted before persist (Task 038)
- Replay data redacted (no raw secrets)
- Audit trail maintained (Task 039)
- Policy engine respected (Task 037)

### Performance Targets

- Event append: <10ms
- Resume point find: <100ms
- Retry decision: <5ms
- Replay load: <1s
- Memory per session: <50MB

---

## Acceptance Criteria / Definition of Done

### Event Log (Task 040)
- [ ] AC-001: Append-only event log implemented
- [ ] AC-002: Events have unique IDs
- [ ] AC-003: Events have sequence numbers
- [ ] AC-004: Events are immutable
- [ ] AC-005: Events persisted to SQLite
- [ ] AC-006: Events survive crash
- [ ] AC-007: Read from sequence works
- [ ] AC-008: Last sequence queryable
- [ ] AC-009: Concurrent append safe
- [ ] AC-010: Ordering guaranteed

### Resume (Task 040.a-c)
- [ ] AC-011: Resume point identified
- [ ] AC-012: Incomplete tasks found
- [ ] AC-013: Completed tasks skipped
- [ ] AC-014: Resume after crash works
- [ ] AC-015: Resume rules documented
- [ ] AC-016: Ordering preserved on resume
- [ ] AC-017: Gaps detected
- [ ] AC-018: Recovery from gaps
- [ ] AC-019: Resume audit logged
- [ ] AC-020: Resume test coverage

### Retry Policy (Task 041)
- [ ] AC-021: Retry policy engine implemented
- [ ] AC-022: Transient failures retried
- [ ] AC-023: Permanent failures not retried
- [ ] AC-024: Retry delay configurable
- [ ] AC-025: Exponential backoff supported
- [ ] AC-026: Jitter supported
- [ ] AC-027: Policy per operation type
- [ ] AC-028: Policy configurable
- [ ] AC-029: Retry events published
- [ ] AC-030: Retry audit logged

### Failure Classification (Task 041.a)
- [ ] AC-031: Failure classifier implemented
- [ ] AC-032: Network errors = transient
- [ ] AC-033: Timeout errors = transient
- [ ] AC-034: Permission errors = permanent
- [ ] AC-035: Not found = permanent
- [ ] AC-036: Approval needed = needs-human
- [ ] AC-037: Unknown = categorized
- [ ] AC-038: Custom classifiers supported
- [ ] AC-039: Classification logged
- [ ] AC-040: Classification tested

### Capped Retries (Task 041.b)
- [ ] AC-041: Retry count tracked
- [ ] AC-042: Max retries configurable
- [ ] AC-043: Default max = 3
- [ ] AC-044: Exceeded = escalate
- [ ] AC-045: Per-operation caps
- [ ] AC-046: Global caps
- [ ] AC-047: Cap enforcement strict
- [ ] AC-048: Exhaustion logged
- [ ] AC-049: Exhaustion event published
- [ ] AC-050: Exhaustion tested

### Human Escalation (Task 041.c)
- [ ] AC-051: Needs-human detection works
- [ ] AC-052: Escalation triggers correctly
- [ ] AC-053: User notified
- [ ] AC-054: Agent pauses
- [ ] AC-055: Human response handled
- [ ] AC-056: Resume after human works
- [ ] AC-057: Escalation rules configurable
- [ ] AC-058: Escalation logged
- [ ] AC-059: Escalation audited
- [ ] AC-060: Escalation tested

### Reproducibility (Task 042)
- [ ] AC-061: Session data captured
- [ ] AC-062: Prompts recorded
- [ ] AC-063: Settings recorded
- [ ] AC-064: Tool calls recorded
- [ ] AC-065: All data redacted
- [ ] AC-066: Storage efficient
- [ ] AC-067: Load fast
- [ ] AC-068: Format documented
- [ ] AC-069: Compression supported
- [ ] AC-070: Version tracked

### Deterministic Mode (Task 042.b)
- [ ] AC-071: Deterministic mode toggle
- [ ] AC-072: Same inputs = same outputs
- [ ] AC-073: Random seeded
- [ ] AC-074: Time mocked
- [ ] AC-075: Order preserved
- [ ] AC-076: Mode logged
- [ ] AC-077: Mode tested
- [ ] AC-078: Non-determinism warned
- [ ] AC-079: Determinism verified
- [ ] AC-080: Documentation complete

### Replay (Task 042.c)
- [ ] AC-081: Replay tooling implemented
- [ ] AC-082: Session loadable
- [ ] AC-083: Step-by-step replay
- [ ] AC-084: Full replay supported
- [ ] AC-085: Breakpoints supported
- [ ] AC-086: Comparison mode
- [ ] AC-087: Diff on divergence
- [ ] AC-088: Replay CLI works
- [ ] AC-089: Replay logged
- [ ] AC-090: Replay tested

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Event log corruption | Low | High | Checksums, WAL mode |
| Resume point wrong | Medium | High | Comprehensive testing |
| Retry storm | Medium | Medium | Rate limiting |
| Infinite retry | Low | High | Strict caps |
| Human not available | Medium | Medium | Timeout + default |
| Replay divergence | Medium | Medium | Deterministic mode |
| Memory on large replay | Medium | Medium | Streaming |
| Performance regression | Low | Medium | Benchmarks |
| Secret in replay | Low | Critical | Redaction verification |
| Ordering violation | Low | High | Sequence generator |
| Concurrent write race | Medium | Medium | Locks |
| Schema evolution | Medium | Medium | Versioning |

---

## Milestone Plan

### Milestone 1: Event Log Foundation
**Tasks:** 040, 040.a, 040.c
**Deliverables:**
- Append-only event log
- Sequence generator
- Ordering guarantees
- Basic persistence

### Milestone 2: Resume Capability
**Tasks:** 040.b
**Deliverables:**
- Resume point identification
- Resume rules engine
- Crash recovery
- Gap detection

### Milestone 3: Retry Framework
**Tasks:** 041, 041.a, 041.b
**Deliverables:**
- Retry policy engine
- Failure classifier
- Capped retries
- Backoff strategies

### Milestone 4: Human Escalation
**Tasks:** 041.c
**Deliverables:**
- Escalation detection
- User notification
- Pause/resume flow
- Escalation rules

### Milestone 5: Reproducibility
**Tasks:** 042, 042.a, 042.b, 042.c
**Deliverables:**
- Session recording
- Deterministic mode
- Replay tooling
- Comparison mode

---

## Definition of Epic Complete

- [ ] All 12 tasks implemented and tested
- [ ] Event log survives crash (verified)
- [ ] Resume after crash works (verified)
- [ ] Retry policies working correctly
- [ ] Human escalation functional
- [ ] Replay tooling complete
- [ ] Deterministic mode available
- [ ] All secrets redacted in replay
- [ ] Performance targets met
- [ ] Unit test coverage >80%
- [ ] Integration tests passing
- [ ] E2E tests for crash recovery
- [ ] Documentation complete
- [ ] API reference complete
- [ ] CLI commands documented
- [ ] Configuration documented
- [ ] Audit integration verified
- [ ] Cross-platform tested
- [ ] Memory bounded (verified)
- [ ] No data loss on crash (verified)

---

**END OF EPIC 10**
