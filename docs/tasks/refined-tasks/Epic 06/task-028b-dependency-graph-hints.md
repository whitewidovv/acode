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

## Assumptions

### Technical Assumptions

1. **Graph Library**: In-memory graph data structure is available (custom or library)
2. **DAG Requirement**: Dependency graph must be directed acyclic graph (DAG)
3. **Cycle Detection**: Cycles in dependencies are detected and rejected
4. **Topological Sort**: Tasks can be ordered via topological sort for execution
5. **Efficient Traversal**: Graph traversal is O(V+E) for scheduling decisions
6. **Persistence**: Graph state persists across restarts

### Dependency Types

7. **Explicit Dependencies**: Tasks can declare depends_on relationships
8. **Implicit Hints**: File overlap creates soft dependency hints (not hard blocks)
9. **Hint vs Dependency**: Hints affect scheduling priority, not execution blocking
10. **Bidirectional Hints**: File overlap creates mutual hints between both tasks
11. **Priority Boosting**: Tasks with more dependents get higher effective priority

### Scheduling Assumptions

12. **Scheduler Integration**: Worker pool uses graph for task selection
13. **Critical Path**: Longest dependency chain determines minimum completion time
14. **Parallel Safety**: Independent tasks (no edges) can run in parallel
15. **Visualization**: Graph can be exported for debugging (DOT format)

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

## Best Practices

### Graph Design

1. **DAG Enforcement**: Always validate no cycles before adding edges
2. **Immutable Nodes**: Once added, task nodes should not be modified
3. **Edge Semantics**: Clearly distinguish hard dependencies from soft hints
4. **Incremental Updates**: Support adding tasks without rebuilding entire graph

### Hint Generation

5. **File Overlap Only**: Generate hints based on actual file overlap, not guesses
6. **Bidirectional Hints**: If A overlaps B, both A→B and B→A hints exist
7. **Hint Decay**: Old hints may become stale; consider TTL for hints
8. **Don't Over-Hint**: Too many hints defeats the purpose; prioritize high-overlap

### Scheduling Integration

9. **Respect Hard Dependencies**: Never schedule task before its dependencies
10. **Hints Affect Priority**: Tasks with hints should schedule cautiously
11. **Critical Path First**: Prioritize tasks on critical path for faster completion
12. **Visualize for Debugging**: Provide DOT export for graph visualization

---

## Troubleshooting

### Issue: Cycle Detected in Dependencies

**Symptoms:** "Circular dependency detected" error when adding task

**Possible Causes:**
- Explicit circular depends_on declarations
- File overlap hints creating apparent cycle
- Task A depends on B which depends on A

**Solutions:**
1. Review depends_on declarations in task specs
2. Hints don't create cycles; check if hints wrongly treated as dependencies
3. Refactor tasks to break circular relationship

### Issue: Tasks Running Out of Order

**Symptoms:** Dependent task starts before its dependency completes

**Possible Causes:**
- Dependency not registered in graph
- Scheduler not consulting graph
- Race condition in status check

**Solutions:**
1. Verify dependency appears in graph: `acode graph show <task-id>`
2. Check scheduler logs for task selection reasoning
3. Ensure atomic check-and-dequeue operation

### Issue: Graph Export Empty or Malformed

**Symptoms:** DOT export produces empty or unparseable output

**Possible Causes:**
- No tasks in graph
- Special characters in task IDs/titles not escaped
- Newlines in labels breaking DOT syntax

**Solutions:**
1. Verify tasks exist: `acode task list`
2. Escape special characters in DOT output
3. Truncate or sanitize task titles for labels

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

### File Structure

```
src/
├── Acode.Domain/
│   └── Dependencies/
│       ├── DependencyHint.cs
│       ├── HintStrength.cs
│       └── GraphNode.cs
├── Acode.Application/
│   └── Dependencies/
│       ├── IDependencyGraph.cs
│       ├── IFileHintGenerator.cs
│       └── IGraphPersistence.cs
├── Acode.Infrastructure/
│   └── Dependencies/
│       ├── DependencyGraph.cs
│       ├── FileHintGenerator.cs
│       ├── GraphPersistence.cs
│       ├── TopologicalSorter.cs
│       ├── CycleDetector.cs
│       └── DotExporter.cs
└── Acode.Cli/
    └── Commands/
        └── Deps/
            ├── DepsShowCommand.cs
            ├── DepsExportCommand.cs
            └── DepsAddCommand.cs
tests/
└── Acode.Infrastructure.Tests/
    └── Dependencies/
        ├── DependencyGraphTests.cs
        ├── CycleDetectorTests.cs
        ├── TopologicalSorterTests.cs
        └── FileHintGeneratorTests.cs
```

### Part 1: Domain Models

```csharp
// File: src/Acode.Domain/Dependencies/HintStrength.cs
namespace Acode.Domain.Dependencies;

/// <summary>
/// Strength of dependency hint.
/// </summary>
public enum HintStrength
{
    /// <summary>Advisory only, scheduler may ignore.</summary>
    Weak = 0,
    
    /// <summary>Should be respected unless necessary.</summary>
    Medium = 1,
    
    /// <summary>Acts like hard dependency.</summary>
    Strong = 2
}

// File: src/Acode.Domain/Dependencies/DependencyHint.cs
namespace Acode.Domain.Dependencies;

/// <summary>
/// Suggested ordering between tasks.
/// </summary>
public sealed record DependencyHint
{
    /// <summary>Task that should run first.</summary>
    public required string FromTaskId { get; init; }
    
    /// <summary>Task that should run after.</summary>
    public required string ToTaskId { get; init; }
    
    /// <summary>How strongly the hint should be enforced.</summary>
    public required HintStrength Strength { get; init; }
    
    /// <summary>Reason for the hint (for logging).</summary>
    public required string Reason { get; init; }
    
    /// <summary>Whether this was manually specified.</summary>
    public bool IsManual { get; init; } = false;
    
    /// <summary>When the hint was created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

// File: src/Acode.Domain/Dependencies/GraphNode.cs
namespace Acode.Domain.Dependencies;

/// <summary>
/// Node in dependency graph representing a task.
/// </summary>
public sealed class GraphNode
{
    public required string TaskId { get; init; }
    public required TaskSpec Spec { get; init; }
    public TaskGraphStatus Status { get; set; } = TaskGraphStatus.Pending;
    
    /// <summary>Hard dependencies (must complete first).</summary>
    public HashSet<string> Dependencies { get; } = new();
    
    /// <summary>Soft hints (should complete first).</summary>
    public List<DependencyHint> Hints { get; } = new();
    
    /// <summary>Tasks that depend on this one.</summary>
    public HashSet<string> Dependents { get; } = new();
    
    /// <summary>All effective dependencies (hard + strong hints).</summary>
    public IEnumerable<string> EffectiveDependencies =>
        Dependencies.Concat(
            Hints.Where(h => h.Strength == HintStrength.Strong)
                 .Select(h => h.FromTaskId))
        .Distinct();
    
    /// <summary>Check if all dependencies are complete.</summary>
    public bool IsReady(IReadOnlySet<string> completedTasks) =>
        EffectiveDependencies.All(completedTasks.Contains);
}

public enum TaskGraphStatus
{
    Pending,
    Ready,
    Running,
    Completed,
    Failed,
    Skipped
}
```

### Part 2: Application Interfaces

```csharp
// File: src/Acode.Application/Dependencies/IDependencyGraph.cs
namespace Acode.Application.Dependencies;

/// <summary>
/// Manages task dependencies and ordering.
/// </summary>
public interface IDependencyGraph
{
    /// <summary>Add a task to the graph.</summary>
    void AddTask(TaskSpec spec);
    
    /// <summary>Remove a task from the graph.</summary>
    void RemoveTask(string taskId);
    
    /// <summary>Mark a task as completed.</summary>
    void MarkComplete(string taskId);
    
    /// <summary>Mark a task as failed.</summary>
    void MarkFailed(string taskId);
    
    /// <summary>Get direct dependencies of a task.</summary>
    IReadOnlyList<string> GetDependencies(string taskId);
    
    /// <summary>Get tasks that depend on this task.</summary>
    IReadOnlyList<string> GetDependents(string taskId);
    
    /// <summary>Check if task is ready to execute.</summary>
    bool IsReady(string taskId);
    
    /// <summary>Get all tasks ready to execute.</summary>
    IReadOnlyList<string> GetReadyTasks();
    
    /// <summary>Get topologically sorted task list.</summary>
    IReadOnlyList<string> TopologicalSort();
    
    /// <summary>Get groups of tasks that can run in parallel.</summary>
    IReadOnlyList<IReadOnlyList<string>> GetParallelGroups();
    
    /// <summary>Get the critical path (longest dependency chain).</summary>
    IReadOnlyList<string> GetCriticalPath();
    
    /// <summary>Check for circular dependencies.</summary>
    bool HasCycle(out IReadOnlyList<string>? cyclePath);
    
    /// <summary>Add a manual dependency hint.</summary>
    void AddHint(DependencyHint hint);
    
    /// <summary>Remove a hint.</summary>
    void RemoveHint(string fromTaskId, string toTaskId);
    
    /// <summary>Export graph in DOT format.</summary>
    string ExportDot();
    
    /// <summary>Get graph statistics.</summary>
    GraphStats GetStats();
    
    /// <summary>Event when task becomes ready.</summary>
    event EventHandler<string>? TaskBecameReady;
}

public sealed record GraphStats
{
    public required int TotalTasks { get; init; }
    public required int PendingTasks { get; init; }
    public required int CompletedTasks { get; init; }
    public required int ReadyTasks { get; init; }
    public required int TotalEdges { get; init; }
    public required int CriticalPathLength { get; init; }
    public double Progress => TotalTasks > 0 
        ? (double)CompletedTasks / TotalTasks 
        : 0;
}

// File: src/Acode.Application/Dependencies/IFileHintGenerator.cs
namespace Acode.Application.Dependencies;

/// <summary>
/// Generates dependency hints based on file overlaps.
/// </summary>
public interface IFileHintGenerator
{
    /// <summary>Generate hints for a set of tasks.</summary>
    IReadOnlyList<DependencyHint> GenerateHints(
        IReadOnlyList<TaskSpec> tasks);
    
    /// <summary>Generate hints for a new task given existing tasks.</summary>
    IReadOnlyList<DependencyHint> GenerateHintsForTask(
        TaskSpec newTask,
        IReadOnlyList<TaskSpec> existingTasks);
}

// File: src/Acode.Application/Dependencies/IGraphPersistence.cs
namespace Acode.Application.Dependencies;

/// <summary>
/// Persists graph state for crash recovery.
/// </summary>
public interface IGraphPersistence
{
    /// <summary>Save graph state.</summary>
    Task SaveAsync(DependencyGraphState state, CancellationToken ct = default);
    
    /// <summary>Load graph state.</summary>
    Task<DependencyGraphState?> LoadAsync(CancellationToken ct = default);
    
    /// <summary>Clear persisted state.</summary>
    Task ClearAsync(CancellationToken ct = default);
}

public sealed record DependencyGraphState
{
    public required IReadOnlyList<GraphNodeState> Nodes { get; init; }
    public required IReadOnlyList<DependencyHint> Hints { get; init; }
    public required DateTimeOffset SavedAt { get; init; }
}

public sealed record GraphNodeState
{
    public required string TaskId { get; init; }
    public required TaskGraphStatus Status { get; init; }
    public required IReadOnlyList<string> Dependencies { get; init; }
}
```

### Part 3: Dependency Graph Implementation

```csharp
// File: src/Acode.Infrastructure/Dependencies/DependencyGraph.cs
namespace Acode.Infrastructure.Dependencies;

public sealed class DependencyGraph : IDependencyGraph
{
    private readonly Dictionary<string, GraphNode> _nodes = new();
    private readonly HashSet<string> _completed = new();
    private readonly List<DependencyHint> _hints = new();
    private readonly IFileHintGenerator _hintGenerator;
    private readonly ILogger<DependencyGraph> _logger;
    private readonly object _lock = new();
    
    public event EventHandler<string>? TaskBecameReady;
    
    public DependencyGraph(
        IFileHintGenerator hintGenerator,
        ILogger<DependencyGraph> logger)
    {
        _hintGenerator = hintGenerator;
        _logger = logger;
    }
    
    public void AddTask(TaskSpec spec)
    {
        lock (_lock)
        {
            if (_nodes.ContainsKey(spec.Id))
                throw new InvalidOperationException(
                    $"Task {spec.Id} already in graph");
            
            var node = new GraphNode
            {
                TaskId = spec.Id,
                Spec = spec
            };
            
            // Add explicit dependencies
            if (spec.Dependencies != null)
            {
                foreach (var dep in spec.Dependencies)
                {
                    if (!_nodes.ContainsKey(dep))
                    {
                        _logger.LogWarning(
                            "Task {TaskId} depends on unknown task {Dep}",
                            spec.Id, dep);
                        continue;
                    }
                    
                    node.Dependencies.Add(dep);
                    _nodes[dep].Dependents.Add(spec.Id);
                }
            }
            
            _nodes[spec.Id] = node;
            
            // Generate file-based hints
            var existingSpecs = _nodes.Values
                .Where(n => n.TaskId != spec.Id)
                .Select(n => n.Spec)
                .ToList();
            
            var hints = _hintGenerator.GenerateHintsForTask(spec, existingSpecs);
            foreach (var hint in hints)
            {
                AddHintInternal(hint);
            }
            
            // Check for cycles
            if (HasCycle(out var cycle))
            {
                // Remove the task that caused the cycle
                _nodes.Remove(spec.Id);
                throw new InvalidOperationException(
                    $"Adding task {spec.Id} creates cycle: {string.Join(" -> ", cycle!)}");
            }
            
            // Check if immediately ready
            if (node.IsReady(_completed))
            {
                node.Status = TaskGraphStatus.Ready;
                TaskBecameReady?.Invoke(this, spec.Id);
            }
            
            _logger.LogDebug(
                "Added task {TaskId} with {DepCount} dependencies",
                spec.Id, node.Dependencies.Count);
        }
    }
    
    public void RemoveTask(string taskId)
    {
        lock (_lock)
        {
            if (!_nodes.TryGetValue(taskId, out var node))
                return;
            
            // Remove from dependents of dependencies
            foreach (var dep in node.Dependencies)
            {
                if (_nodes.TryGetValue(dep, out var depNode))
                    depNode.Dependents.Remove(taskId);
            }
            
            // Remove from dependencies of dependents
            foreach (var dependent in node.Dependents)
            {
                if (_nodes.TryGetValue(dependent, out var depNode))
                    depNode.Dependencies.Remove(taskId);
            }
            
            // Remove hints involving this task
            _hints.RemoveAll(h => 
                h.FromTaskId == taskId || h.ToTaskId == taskId);
            
            _nodes.Remove(taskId);
            _completed.Remove(taskId);
            
            _logger.LogDebug("Removed task {TaskId}", taskId);
        }
    }
    
    public void MarkComplete(string taskId)
    {
        lock (_lock)
        {
            if (!_nodes.TryGetValue(taskId, out var node))
                return;
            
            node.Status = TaskGraphStatus.Completed;
            _completed.Add(taskId);
            
            // Check if any dependents are now ready
            foreach (var dependentId in node.Dependents)
            {
                if (_nodes.TryGetValue(dependentId, out var dependent))
                {
                    if (dependent.Status == TaskGraphStatus.Pending &&
                        dependent.IsReady(_completed))
                    {
                        dependent.Status = TaskGraphStatus.Ready;
                        TaskBecameReady?.Invoke(this, dependentId);
                    }
                }
            }
            
            _logger.LogDebug("Marked task {TaskId} complete", taskId);
        }
    }
    
    public void MarkFailed(string taskId)
    {
        lock (_lock)
        {
            if (!_nodes.TryGetValue(taskId, out var node))
                return;
            
            node.Status = TaskGraphStatus.Failed;
            
            // Optionally skip dependents
            // (depending on failure policy)
        }
    }
    
    public IReadOnlyList<string> GetDependencies(string taskId)
    {
        lock (_lock)
        {
            return _nodes.TryGetValue(taskId, out var node)
                ? node.Dependencies.ToList()
                : [];
        }
    }
    
    public IReadOnlyList<string> GetDependents(string taskId)
    {
        lock (_lock)
        {
            return _nodes.TryGetValue(taskId, out var node)
                ? node.Dependents.ToList()
                : [];
        }
    }
    
    public bool IsReady(string taskId)
    {
        lock (_lock)
        {
            return _nodes.TryGetValue(taskId, out var node) &&
                   node.IsReady(_completed);
        }
    }
    
    public IReadOnlyList<string> GetReadyTasks()
    {
        lock (_lock)
        {
            return _nodes.Values
                .Where(n => n.Status == TaskGraphStatus.Pending &&
                           n.IsReady(_completed))
                .Select(n => n.TaskId)
                .ToList();
        }
    }
    
    public IReadOnlyList<string> TopologicalSort()
    {
        lock (_lock)
        {
            return TopologicalSorter.Sort(_nodes);
        }
    }
    
    public IReadOnlyList<IReadOnlyList<string>> GetParallelGroups()
    {
        lock (_lock)
        {
            return TopologicalSorter.GetParallelGroups(_nodes, _completed);
        }
    }
    
    public IReadOnlyList<string> GetCriticalPath()
    {
        lock (_lock)
        {
            return TopologicalSorter.FindCriticalPath(_nodes);
        }
    }
    
    public bool HasCycle(out IReadOnlyList<string>? cyclePath)
    {
        lock (_lock)
        {
            return CycleDetector.HasCycle(_nodes, out cyclePath);
        }
    }
    
    public void AddHint(DependencyHint hint)
    {
        lock (_lock)
        {
            AddHintInternal(hint with { IsManual = true });
        }
    }
    
    private void AddHintInternal(DependencyHint hint)
    {
        // Check for existing hint
        var existing = _hints.FirstOrDefault(h =>
            h.FromTaskId == hint.FromTaskId &&
            h.ToTaskId == hint.ToTaskId);
        
        if (existing != null)
        {
            // Manual hints override auto
            if (hint.IsManual && !existing.IsManual)
            {
                _hints.Remove(existing);
            }
            else
            {
                return; // Keep existing
            }
        }
        
        _hints.Add(hint);
        
        // Add to node
        if (_nodes.TryGetValue(hint.ToTaskId, out var node))
        {
            node.Hints.Add(hint);
        }
        
        _logger.LogDebug(
            "Added {Strength} hint: {From} -> {To} ({Reason})",
            hint.Strength, hint.FromTaskId, hint.ToTaskId, hint.Reason);
    }
    
    public void RemoveHint(string fromTaskId, string toTaskId)
    {
        lock (_lock)
        {
            _hints.RemoveAll(h =>
                h.FromTaskId == fromTaskId &&
                h.ToTaskId == toTaskId);
            
            if (_nodes.TryGetValue(toTaskId, out var node))
            {
                node.Hints.RemoveAll(h => h.FromTaskId == fromTaskId);
            }
        }
    }
    
    public string ExportDot()
    {
        lock (_lock)
        {
            return DotExporter.Export(_nodes, _hints);
        }
    }
    
    public GraphStats GetStats()
    {
        lock (_lock)
        {
            var pending = _nodes.Values.Count(n => 
                n.Status == TaskGraphStatus.Pending);
            var completed = _nodes.Values.Count(n => 
                n.Status == TaskGraphStatus.Completed);
            var ready = GetReadyTasks().Count;
            var edges = _nodes.Values.Sum(n => n.Dependencies.Count);
            var criticalPath = GetCriticalPath();
            
            return new GraphStats
            {
                TotalTasks = _nodes.Count,
                PendingTasks = pending,
                CompletedTasks = completed,
                ReadyTasks = ready,
                TotalEdges = edges,
                CriticalPathLength = criticalPath.Count
            };
        }
    }
}
```

### Part 4: Graph Algorithms

```csharp
// File: src/Acode.Infrastructure/Dependencies/CycleDetector.cs
namespace Acode.Infrastructure.Dependencies;

public static class CycleDetector
{
    public static bool HasCycle(
        Dictionary<string, GraphNode> nodes,
        out IReadOnlyList<string>? cyclePath)
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var path = new List<string>();
        
        foreach (var nodeId in nodes.Keys)
        {
            if (HasCycleDfs(nodeId, nodes, visited, recursionStack, path))
            {
                cyclePath = BuildCyclePath(path);
                return true;
            }
        }
        
        cyclePath = null;
        return false;
    }
    
    private static bool HasCycleDfs(
        string nodeId,
        Dictionary<string, GraphNode> nodes,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> path)
    {
        if (recursionStack.Contains(nodeId))
        {
            path.Add(nodeId);
            return true;
        }
        
        if (visited.Contains(nodeId))
            return false;
        
        visited.Add(nodeId);
        recursionStack.Add(nodeId);
        path.Add(nodeId);
        
        if (nodes.TryGetValue(nodeId, out var node))
        {
            foreach (var dep in node.EffectiveDependencies)
            {
                if (HasCycleDfs(dep, nodes, visited, recursionStack, path))
                    return true;
            }
        }
        
        path.RemoveAt(path.Count - 1);
        recursionStack.Remove(nodeId);
        return false;
    }
    
    private static IReadOnlyList<string> BuildCyclePath(List<string> path)
    {
        // Find where the cycle starts
        var cycleStart = path[^1];
        var startIndex = path.IndexOf(cycleStart);
        return path.Skip(startIndex).ToList();
    }
}

// File: src/Acode.Infrastructure/Dependencies/TopologicalSorter.cs
namespace Acode.Infrastructure.Dependencies;

public static class TopologicalSorter
{
    public static IReadOnlyList<string> Sort(
        Dictionary<string, GraphNode> nodes)
    {
        var result = new List<string>();
        var visited = new HashSet<string>();
        var temp = new HashSet<string>();
        
        foreach (var nodeId in nodes.Keys)
        {
            if (!visited.Contains(nodeId))
            {
                Visit(nodeId, nodes, visited, temp, result);
            }
        }
        
        result.Reverse();
        return result;
    }
    
    private static void Visit(
        string nodeId,
        Dictionary<string, GraphNode> nodes,
        HashSet<string> visited,
        HashSet<string> temp,
        List<string> result)
    {
        if (temp.Contains(nodeId))
            return; // Cycle, already handled
        
        if (visited.Contains(nodeId))
            return;
        
        temp.Add(nodeId);
        
        if (nodes.TryGetValue(nodeId, out var node))
        {
            foreach (var dep in node.EffectiveDependencies)
            {
                Visit(dep, nodes, visited, temp, result);
            }
        }
        
        temp.Remove(nodeId);
        visited.Add(nodeId);
        result.Add(nodeId);
    }
    
    public static IReadOnlyList<IReadOnlyList<string>> GetParallelGroups(
        Dictionary<string, GraphNode> nodes,
        HashSet<string> completed)
    {
        var groups = new List<List<string>>();
        var remaining = new HashSet<string>(
            nodes.Keys.Where(k => !completed.Contains(k)));
        var done = new HashSet<string>(completed);
        
        while (remaining.Count > 0)
        {
            // Find all nodes with no remaining dependencies
            var ready = remaining
                .Where(id => nodes[id].EffectiveDependencies.All(done.Contains))
                .ToList();
            
            if (ready.Count == 0)
                break; // Cycle or stuck
            
            groups.Add(ready);
            
            foreach (var id in ready)
            {
                remaining.Remove(id);
                done.Add(id);
            }
        }
        
        return groups;
    }
    
    public static IReadOnlyList<string> FindCriticalPath(
        Dictionary<string, GraphNode> nodes)
    {
        // Find longest path using dynamic programming
        var distances = new Dictionary<string, int>();
        var predecessors = new Dictionary<string, string?>();
        
        foreach (var nodeId in nodes.Keys)
        {
            distances[nodeId] = 0;
            predecessors[nodeId] = null;
        }
        
        var sorted = Sort(nodes);
        
        foreach (var nodeId in sorted)
        {
            if (!nodes.TryGetValue(nodeId, out var node))
                continue;
            
            foreach (var dependentId in node.Dependents)
            {
                var newDist = distances[nodeId] + 1;
                if (newDist > distances[dependentId])
                {
                    distances[dependentId] = newDist;
                    predecessors[dependentId] = nodeId;
                }
            }
        }
        
        // Find node with longest path
        var endNode = distances.OrderByDescending(kv => kv.Value).First().Key;
        
        // Reconstruct path
        var path = new List<string>();
        string? current = endNode;
        while (current != null)
        {
            path.Add(current);
            current = predecessors[current];
        }
        
        path.Reverse();
        return path;
    }
}

// File: src/Acode.Infrastructure/Dependencies/DotExporter.cs
namespace Acode.Infrastructure.Dependencies;

public static class DotExporter
{
    public static string Export(
        Dictionary<string, GraphNode> nodes,
        List<DependencyHint> hints)
    {
        var sb = new StringBuilder();
        sb.AppendLine("digraph TaskDependencies {");
        sb.AppendLine("  rankdir=LR;");
        sb.AppendLine("  node [shape=box];");
        sb.AppendLine();
        
        // Nodes with status colors
        foreach (var (id, node) in nodes)
        {
            var color = node.Status switch
            {
                TaskGraphStatus.Completed => "green",
                TaskGraphStatus.Running => "yellow",
                TaskGraphStatus.Failed => "red",
                TaskGraphStatus.Ready => "lightblue",
                _ => "white"
            };
            
            var shortId = id.Length > 12 ? id[..12] : id;
            sb.AppendLine($"  \"{id}\" [label=\"{shortId}\" fillcolor=\"{color}\" style=filled];");
        }
        
        sb.AppendLine();
        
        // Hard dependency edges
        foreach (var (id, node) in nodes)
        {
            foreach (var dep in node.Dependencies)
            {
                sb.AppendLine($"  \"{dep}\" -> \"{id}\";");
            }
        }
        
        // Hint edges (dashed)
        foreach (var hint in hints)
        {
            var style = hint.Strength switch
            {
                HintStrength.Strong => "dashed",
                HintStrength.Medium => "dotted",
                _ => "dotted"
            };
            var color = hint.Strength switch
            {
                HintStrength.Strong => "blue",
                HintStrength.Medium => "gray",
                _ => "lightgray"
            };
            
            sb.AppendLine($"  \"{hint.FromTaskId}\" -> \"{hint.ToTaskId}\" " +
                         $"[style={style} color=\"{color}\"];");
        }
        
        sb.AppendLine("}");
        return sb.ToString();
    }
}
```

### Part 5: File Hint Generator

```csharp
// File: src/Acode.Infrastructure/Dependencies/FileHintGenerator.cs
namespace Acode.Infrastructure.Dependencies;

public sealed class FileHintGenerator : IFileHintGenerator
{
    private readonly FileHintOptions _options;
    private readonly ILogger<FileHintGenerator> _logger;
    
    public FileHintGenerator(
        FileHintOptions options,
        ILogger<FileHintGenerator> logger)
    {
        _options = options;
        _logger = logger;
    }
    
    public IReadOnlyList<DependencyHint> GenerateHints(
        IReadOnlyList<TaskSpec> tasks)
    {
        var hints = new List<DependencyHint>();
        
        // Group tasks by file
        var fileToTasks = new Dictionary<string, List<TaskSpec>>();
        
        foreach (var task in tasks)
        {
            if (task.Files == null) continue;
            
            foreach (var file in task.Files)
            {
                if (!fileToTasks.TryGetValue(file, out var list))
                {
                    list = new List<TaskSpec>();
                    fileToTasks[file] = list;
                }
                list.Add(task);
            }
        }
        
        // Generate hints for overlapping files
        foreach (var (file, fileTasks) in fileToTasks)
        {
            if (fileTasks.Count < 2) continue;
            
            var strength = GetStrengthForFile(file);
            
            // Earlier task should complete before later
            for (int i = 0; i < fileTasks.Count - 1; i++)
            {
                for (int j = i + 1; j < fileTasks.Count; j++)
                {
                    hints.Add(new DependencyHint
                    {
                        FromTaskId = fileTasks[i].Id,
                        ToTaskId = fileTasks[j].Id,
                        Strength = strength,
                        Reason = $"Both modify {file}"
                    });
                }
            }
        }
        
        return hints;
    }
    
    public IReadOnlyList<DependencyHint> GenerateHintsForTask(
        TaskSpec newTask,
        IReadOnlyList<TaskSpec> existingTasks)
    {
        var hints = new List<DependencyHint>();
        
        if (newTask.Files == null || newTask.Files.Count == 0)
            return hints;
        
        foreach (var existingTask in existingTasks)
        {
            if (existingTask.Files == null) continue;
            
            var commonFiles = newTask.Files
                .Intersect(existingTask.Files)
                .ToList();
            
            if (commonFiles.Count == 0) continue;
            
            var strength = commonFiles
                .Select(GetStrengthForFile)
                .Max();
            
            hints.Add(new DependencyHint
            {
                FromTaskId = existingTask.Id,
                ToTaskId = newTask.Id,
                Strength = strength,
                Reason = $"Both modify: {string.Join(", ", commonFiles.Take(3))}" +
                        (commonFiles.Count > 3 ? $" (+{commonFiles.Count - 3} more)" : "")
            });
        }
        
        return hints;
    }
    
    private HintStrength GetStrengthForFile(string file)
    {
        foreach (var rule in _options.Patterns)
        {
            if (MatchesPattern(file, rule.Pattern))
                return rule.Strength;
        }
        return _options.DefaultStrength;
    }
    
    private static bool MatchesPattern(string path, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", "[^/]*")
            .Replace(@"\?", ".") + "$";
        
        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }
}

public sealed record FileHintOptions
{
    public HintStrength DefaultStrength { get; init; } = HintStrength.Weak;
    public IReadOnlyList<FileHintRule> Patterns { get; init; } = DefaultPatterns;
    
    public static IReadOnlyList<FileHintRule> DefaultPatterns => new[]
    {
        new FileHintRule { Pattern = "**/*.cs", Strength = HintStrength.Medium },
        new FileHintRule { Pattern = "**/appsettings*.json", Strength = HintStrength.Strong },
        new FileHintRule { Pattern = "**/*.test.cs", Strength = HintStrength.Weak },
        new FileHintRule { Pattern = "**/migrations/*.cs", Strength = HintStrength.Strong }
    };
}

public sealed record FileHintRule
{
    public required string Pattern { get; init; }
    public required HintStrength Strength { get; init; }
}
```

### Implementation Checklist

- [ ] Create `HintStrength`, `DependencyHint`, `GraphNode` domain models
- [ ] Create `IDependencyGraph` interface with full API
- [ ] Create `IFileHintGenerator` interface
- [ ] Create `IGraphPersistence` for crash recovery
- [ ] Implement `DependencyGraph` with node management
- [ ] Implement `CycleDetector` with DFS algorithm
- [ ] Implement `TopologicalSorter` with Kahn's algorithm
- [ ] Implement `TopologicalSorter.GetParallelGroups`
- [ ] Implement `TopologicalSorter.FindCriticalPath`
- [ ] Implement `DotExporter` for visualization
- [ ] Implement `FileHintGenerator` with pattern matching
- [ ] Implement `GraphPersistence` with JSON storage
- [ ] Add CLI commands for graph inspection
- [ ] Add `TaskBecameReady` event handling
- [ ] Write unit tests for cycle detection
- [ ] Write unit tests for topological sort
- [ ] Write unit tests for hint generation
- [ ] Write integration tests for full graph lifecycle

### Rollout Plan

1. **Day 1**: Domain models and interfaces
2. **Day 2**: CycleDetector and TopologicalSorter
3. **Day 3**: DependencyGraph core implementation
4. **Day 4**: FileHintGenerator
5. **Day 5**: DotExporter and visualization
6. **Day 6**: GraphPersistence
7. **Day 7**: CLI commands
8. **Day 8**: Testing

---

**End of Task 028.b Specification**