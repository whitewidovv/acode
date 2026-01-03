# Task 028.b: Dependency Graph Hints

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 028 (Merge Coordinator), Task 025 (Task Spec)  

---

## Description

Task 028.b implements dependency graph hints for task ordering. Tasks MAY declare dependencies. The graph MUST be computed and respected. Cycles MUST be detected and rejected.

Dependency hints enable intelligent scheduling. Tasks touching the same files SHOULD run sequentially. Independent tasks MAY run in parallel. The scheduler MUST use hints to optimize.

File-based hints MUST be auto-generated. If Task A and B both list the same file, a dependency hint MUST be created. Manual hints MUST override auto-generated ones.

### Business Value

Dependency hints enable:
- Reduced merge conflicts
- Better scheduling
- Predictable execution order
- Explicit dependencies
- Conflict prevention

### Scope Boundaries

This task covers dependency hints. Task scheduling is in Task 026. Merge coordination is in Task 028.

### Integration Points

- Task 025: Task spec includes dependencies
- Task 026: Queue uses hints for ordering
- Task 028: Merge uses hints for planning

### Failure Modes

- Cycle detected → Reject with error
- Missing dependency → Proceed with warning
- Stale hint → Ignore if target complete

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Dependency | Task that must complete first |
| Hint | Suggested ordering |
| Graph | Task relationship structure |
| Cycle | Circular dependency |
| Topological Sort | Order respecting dependencies |
| DAG | Directed Acyclic Graph |
| Critical Path | Longest dependency chain |

---

## Out of Scope

- Resource-based scheduling
- Priority inheritance
- Preemption
- Real-time scheduling
- Dynamic dependency discovery
- Cross-repository dependencies

---

## Functional Requirements

### FR-001 to FR-025: Graph Construction

- FR-001: `IDependencyGraph` interface MUST be defined
- FR-002: Graph MUST accept task specs
- FR-003: Explicit dependencies MUST be edges
- FR-004: File overlap MUST create hints
- FR-005: Hints MUST be softer than explicit
- FR-006: Graph MUST be directed
- FR-007: Graph MUST be acyclic (DAG)
- FR-008: Cycle MUST be detected
- FR-009: Cycle MUST throw with path
- FR-010: AddTask MUST validate
- FR-011: RemoveTask MUST update graph
- FR-012: Completed tasks MUST be prunable
- FR-013: Graph MUST be queryable
- FR-014: GetDependencies MUST return deps
- FR-015: GetDependents MUST return waiting
- FR-016: IsReady MUST check all deps done
- FR-017: GetReady MUST return executable
- FR-018: Graph MUST support partial view
- FR-019: Subgraph by tag MUST work
- FR-020: Subgraph by priority MUST work
- FR-021: Graph MUST be serializable
- FR-022: Graph state MUST persist
- FR-023: Graph MUST reload on restart
- FR-024: Graph events MUST emit
- FR-025: Graph metrics MUST track

### FR-026 to FR-045: File-Based Hints

- FR-026: Task files MUST be analyzed
- FR-027: Same file MUST create hint
- FR-028: Hint direction MUST use order
- FR-029: Earlier enqueued → dependency
- FR-030: Hint strength MUST be configurable
- FR-031: Strong hint MUST act like dependency
- FR-032: Weak hint MUST be advisory
- FR-033: Default strength MUST be weak
- FR-034: File patterns MUST be matchable
- FR-035: Pattern hints MUST be configurable
- FR-036: Critical files MUST be strong
- FR-037: Test files MUST be weak
- FR-038: Config files MUST be medium
- FR-039: Hint generation MUST be fast
- FR-040: Hints MUST be cached
- FR-041: Cache MUST invalidate on file list change
- FR-042: Hint override MUST be supported
- FR-043: Manual hints MUST take precedence
- FR-044: Hint removal MUST be supported
- FR-045: Hint log MUST be available

### FR-046 to FR-065: Scheduling Support

- FR-046: TopologicalSort MUST be provided
- FR-047: Sort MUST respect dependencies
- FR-048: Sort MUST be stable for ties
- FR-049: Tie-breaker MUST use priority
- FR-050: Same priority MUST use FIFO
- FR-051: Critical path MUST be computed
- FR-052: Critical path MUST be queryable
- FR-053: Parallel groups MUST be identified
- FR-054: Groups are independent sets
- FR-055: Group execution MAY be parallel
- FR-056: Scheduler MUST use groups
- FR-057: Blocking task MUST be identified
- FR-058: Blocked tasks MUST be countable
- FR-059: Unblocking MUST trigger events
- FR-060: Progress MUST be computable
- FR-061: Progress = completed / total
- FR-062: ETA MUST be estimable
- FR-063: ETA uses average task time
- FR-064: Visualization MUST be available
- FR-065: DOT format MUST be exportable

---

## Non-Functional Requirements

- NFR-001: Graph update MUST be <10ms
- NFR-002: Topological sort MUST be <100ms
- NFR-003: 10k tasks MUST be supported
- NFR-004: Memory MUST be bounded
- NFR-005: Cycle detection MUST be fast
- NFR-006: Persistence MUST be atomic
- NFR-007: Reload MUST be fast
- NFR-008: Concurrent access MUST be safe
- NFR-009: Deterministic ordering
- NFR-010: Clear error messages

---

## User Manual Documentation

### Configuration

```yaml
dependencies:
  fileHints:
    enabled: true
    defaultStrength: weak  # weak, medium, strong
    
  patterns:
    - pattern: "**/*.cs"
      strength: medium
    - pattern: "**/appsettings*.json"
      strength: strong
    - pattern: "**/*.test.cs"
      strength: weak
      
  cyclePolicy: reject  # reject, warn, ignore
```

### CLI Commands

```bash
# Show dependency graph
acode deps show

# Show dependencies for task
acode deps show task-abc123

# Show what's blocking a task
acode deps blockers task-abc123

# Show what task is blocking
acode deps blocked-by task-abc123

# Export graph as DOT
acode deps export --format dot > graph.dot

# Visualize (requires graphviz)
acode deps export --format dot | dot -Tpng > graph.png

# Show critical path
acode deps critical-path

# Add manual dependency
acode deps add task-abc123 --depends-on task-def456

# Remove hint
acode deps remove-hint task-abc123 task-def456
```

### Graph Visualization

```
task-001 ─────┐
              ├──► task-003 ───► task-005
task-002 ─────┘                     │
                                    ▼
task-004 ─────────────────────► task-006
```

### Task Spec Dependencies

```yaml
# task-abc123.yaml
title: "Implement login"
dependencies:
  - task-def456  # Explicit: must run after auth setup
files:
  - src/Auth/LoginHandler.cs
  # Auto-hint: if another task lists this file
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Graph constructed
- [ ] AC-002: Dependencies tracked
- [ ] AC-003: Cycles detected
- [ ] AC-004: File hints generated
- [ ] AC-005: Hint strength works
- [ ] AC-006: TopologicalSort works
- [ ] AC-007: Parallel groups found
- [ ] AC-008: Critical path computed
- [ ] AC-009: Graph persists
- [ ] AC-010: Events emitted
- [ ] AC-011: CLI commands work
- [ ] AC-012: DOT export works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Graph construction
- [ ] UT-002: Cycle detection
- [ ] UT-003: Topological sort
- [ ] UT-004: File hint generation
- [ ] UT-005: Parallel groups

### Integration Tests

- [ ] IT-001: Full graph lifecycle
- [ ] IT-002: Persistence/reload
- [ ] IT-003: Large graph performance
- [ ] IT-004: DOT export

---

## Implementation Prompt

### Interface

```csharp
public interface IDependencyGraph
{
    void AddTask(TaskSpec spec);
    void RemoveTask(string taskId);
    void MarkComplete(string taskId);
    
    IReadOnlyList<string> GetDependencies(string taskId);
    IReadOnlyList<string> GetDependents(string taskId);
    
    bool IsReady(string taskId);
    IReadOnlyList<string> GetReadyTasks();
    
    IReadOnlyList<string> TopologicalSort();
    IReadOnlyList<IReadOnlyList<string>> GetParallelGroups();
    
    IReadOnlyList<string> GetCriticalPath();
    
    bool HasCycle(out IReadOnlyList<string>? cyclePath);
    
    string ExportDot();
}

public interface IFileHintGenerator
{
    IReadOnlyList<DependencyHint> GenerateHints(
        IReadOnlyList<TaskSpec> tasks);
}

public record DependencyHint(
    string FromTaskId,
    string ToTaskId,
    HintStrength Strength,
    string Reason);

public enum HintStrength { Weak, Medium, Strong }
```

### Cycle Detection

```csharp
public bool HasCycle(out IReadOnlyList<string>? cyclePath)
{
    var visited = new HashSet<string>();
    var recursionStack = new HashSet<string>();
    var path = new List<string>();
    
    foreach (var node in _nodes.Keys)
    {
        if (HasCycleDfs(node, visited, recursionStack, path))
        {
            cyclePath = path.ToList();
            return true;
        }
    }
    
    cyclePath = null;
    return false;
}
```

---

**End of Task 028.b Specification**