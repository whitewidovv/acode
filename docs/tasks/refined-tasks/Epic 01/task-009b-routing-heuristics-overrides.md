# Task 009.b: Routing Heuristics + Overrides

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 009, Task 009.a, Task 004 (Model Provider Interface)  

---

## Description

### Business Value and ROI

Task 009.b implements routing heuristics and override mechanisms that deliver intelligent, automated model selection with user control fallbacks. This capability provides measurable business value through reduced inference costs, improved developer productivity, and optimized resource utilization. Organizations deploying Acode with heuristic-based routing can expect concrete cost savings and efficiency gains.

**Cost Optimization Through Intelligent Routing:** Heuristic-based model selection reduces computational resource consumption by routing simple tasks to smaller, faster models. In a typical development workload, approximately sixty percent of coding tasks are low complexity operations—simple bug fixes, documentation updates, variable renaming, import additions, basic test writing. Routing these tasks to a 7B parameter model instead of a 70B parameter model reduces inference time by seventy to eighty-five percent while maintaining quality. For teams running local infrastructure with GPU resources, this translates directly to throughput improvements and energy cost reductions.

Consider a concrete example: A development team of ten engineers using Acode for four hours daily generates approximately forty coding requests per developer per day, totaling four hundred requests daily. Without heuristics, all requests route to the default 70B model, averaging fifteen seconds per inference. With heuristics, sixty percent of requests route to the 7B model at three seconds per inference, while forty percent remain on the 70B model. Daily inference time drops from one hundred minutes to seventy-two minutes—a twenty-eight percent reduction. Over a year, this saves approximately ten thousand GPU-hours, which at typical on-premise GPU operational costs translates to fifteen thousand to twenty-five thousand dollars in reduced power consumption and hardware amortization.

**Developer Productivity Through Reduced Wait Times:** Heuristics improve developer productivity by minimizing latency for routine tasks. When a developer requests a simple operation like fixing a typo or adding a type annotation, routing to a smaller model reduces response time from fifteen seconds to three seconds—an eighty percent latency reduction. These micro-optimizations compound across hundreds of daily interactions. Developers maintain flow state more effectively when context switches are shorter. Research in developer productivity indicates that reducing tool response latency from fifteen seconds to three seconds can improve focused work time by twelve to eighteen percent for tasks requiring frequent tool interaction.

Quantifying this benefit: If each developer saves an average of thirty seconds per request on sixty percent of their forty daily requests, that yields twelve hundred seconds or twenty minutes saved per developer per day. For a ten-person team, that equals two hundred minutes daily, or approximately sixteen hours of reclaimed productive time per week. At an average fully-loaded engineering cost of one hundred twenty dollars per hour, this represents approximately ninety-six thousand dollars in annual productivity gains through reduced waiting time alone.

**Resource Utilization and Scalability:** Heuristic routing enables better hardware utilization by distributing load across heterogeneous model deployments. Organizations can deploy multiple model sizes on different GPU resources—7B models on lower-tier GPUs, 70B models on high-memory GPUs. Heuristics ensure that expensive, high-memory GPU capacity is reserved for tasks that genuinely require it, while commodity GPU resources handle the majority of simple tasks. This architectural flexibility enables teams to scale inference capacity more cost-effectively than single-model deployments.

For infrastructure planning, heuristic routing reduces the required GPU memory footprint for typical workloads. Without heuristics, infrastructure must support concurrent execution of the largest model for all users. With heuristics routing sixty percent of traffic to smaller models, peak memory requirements drop by approximately forty to fifty percent, enabling the same hardware to support more concurrent users or delaying infrastructure expansion.

### Technical Architecture and Design Decisions

Task 009.b builds upon the routing foundation established in Task 009 and extends the role-based routing framework from Task 009.a. The heuristic system operates as a pluggable evaluation engine that analyzes task characteristics and produces a normalized complexity score. This score integrates into the routing policy decision logic, influencing model selection when the routing strategy is set to adaptive mode.

**Heuristic Evaluation Pipeline:** The core architecture centers on the `IRoutingHeuristic` interface defined in the Application layer. This interface abstracts the concept of a heuristic evaluator that examines a `HeuristicContext` containing task metadata and returns a `HeuristicResult` containing a score, confidence level, and human-readable reasoning. Multiple heuristic implementations—`FileCountHeuristic`, `TaskTypeHeuristic`, `LanguageHeuristic`—implement this interface, each focusing on a specific signal.

The `HeuristicEngine` in the Infrastructure layer orchestrates heuristic execution. When a task arrives for routing, the engine invokes all registered heuristics in priority order. Each heuristic independently evaluates the task context and returns its result. The engine then aggregates these results using a weighted averaging algorithm, where each heuristic's contribution is weighted by its confidence level. A heuristic that is highly confident in its assessment (confidence equals 0.9 or 1.0) has more influence than a heuristic with low confidence (confidence equals 0.3 or 0.4). This weighted aggregation produces a final complexity score in the range zero to one hundred.

**Complexity Scoring Model:** The complexity score represents an estimate of cognitive and computational demand required to successfully complete a task. Low scores (zero to thirty) indicate simple, mechanical tasks like formatting fixes or import additions. Medium scores (thirty-one to seventy) indicate standard development work like implementing a well-defined function or fixing a known bug. High scores (seventy-one to one hundred) indicate complex tasks requiring sophisticated reasoning—architectural refactoring, multi-file feature implementation, complex algorithm design.

Score thresholds map to model selection tiers. In adaptive routing mode, the `RoutingPolicy` from Task 009 consults the complexity score and selects a model tier accordingly. Low complexity scores route to the light tier (typically a 7B model optimized for speed), medium scores route to the default tier (a balanced 70B model), and high scores route to the capable tier (the most powerful available model). The tier-to-model mapping is configurable, enabling users to adjust routing behavior based on their specific model inventory and performance requirements.

**Built-in Heuristics Design:** Three built-in heuristics provide baseline coverage for common routing scenarios. `FileCountHeuristic` examines the number of files affected by a task. Single-file changes are simpler than multi-file changes, as they have limited blast radius and fewer integration concerns. This heuristic assigns low scores to tasks affecting fewer than three files, medium scores to tasks affecting three to ten files, and high scores to tasks affecting more than ten files. Confidence is high (0.9) for clear cases and lower (0.6) when file count is at threshold boundaries.

`TaskTypeHeuristic` analyzes the nature of the work being performed. Bug fixes are generally simpler than new feature development, as they address specific failure modes rather than requiring design decisions. Refactoring is more complex, as it requires maintaining behavioral equivalence while restructuring code. This heuristic uses natural language processing on the task description to identify keywords and phrases indicating task type. It assigns baseline scores—bug fixes at twenty, enhancements at forty, new features at fifty-five, refactoring at seventy-five—and adjusts based on additional context clues. Confidence varies based on keyword clarity.

`LanguageHeuristic` considers programming language complexity. Markdown and JSON are structurally simple—tools can easily parse and manipulate them. JavaScript and Python have moderate complexity with dynamic typing and runtime flexibility. C++ and Rust have high complexity due to memory management, lifetime analysis, and type system sophistication. This heuristic assigns base scores based on language complexity ratings and adjusts based on the presence of advanced language features in the task context.

**Override System Architecture:** Override mechanisms provide escape hatches when heuristics fail or when users have domain knowledge that heuristics cannot capture. The override system implements a strict precedence hierarchy: request-level overrides take highest precedence, followed by session-level overrides, then configuration-level overrides, with heuristics as the lowest precedence fallback.

The `OverrideResolver` component in the Infrastructure layer implements override resolution logic. When a routing decision is needed, the resolver checks each override source in precedence order. If a request-level override exists (via the `--model` CLI flag), it is returned immediately, short-circuiting all other evaluation. If no request override exists, the resolver checks for a session override (from the `ACODE_MODEL` environment variable or from CLI-set session state). If no session override exists, the resolver checks configuration file overrides. Only if no overrides are present does the system fall back to heuristic evaluation.

This precedence model enables users to layer their preferences. A developer can set a default model in configuration for typical work, override it with a session variable when focusing on a complex feature, and further override it with a command-line flag for a specific edge case—all without modifying configuration files or restarting processes.

**Validation and Constraint Enforcement:** All override mechanisms integrate with the operating mode constraints from Task 001. When a user specifies a model override, the system validates that the requested model is compatible with the current operating mode. In LocalOnly mode, the system rejects overrides requesting cloud models with error code ACODE-HEU-002. In Burst mode, the system accepts local and burst-compatible models but rejects external API models. This validation ensures that user convenience (overrides) never compromises system safety and policy compliance.

The validation pipeline also checks model existence and availability. If a user specifies `--model llama3.2:404b` but that model is not registered in the model catalog from Task 004, the system fails fast with error code ACODE-HEU-001 and a clear message indicating which models are available. This fail-fast behavior prevents cryptic runtime errors and helps users quickly correct configuration mistakes.

### Integration Points with Task 009 and Task 009.a

Task 009.b integrates tightly with the routing foundation from Task 009 and the role-based routing from Task 009.a. The `RoutingPolicy` class from Task 009 is extended with heuristic consultation logic. When the routing strategy is set to `adaptive`, the policy delegates complexity scoring to the `HeuristicEngine`, retrieves the complexity score, maps it to a model tier, and selects an appropriate model from the tier's model list.

The `IModelRouter` interface from Task 009 defines a `Route(RoutingContext)` method that returns a `RoutingDecision`. Task 009.b extends `RoutingContext` with a `HeuristicContext` property containing task metadata for heuristic evaluation. The router implementation checks for overrides via `OverrideResolver`, and if none are present, consults the `HeuristicEngine` for a complexity score. This score is then used in the tier selection logic.

Task 009.a introduced role-specific routing rules that can influence model selection based on agent role (planner, executor, verifier). Task 009.b's heuristics complement these role rules by adding task-specific intelligence. When a request specifies both a role and a task, the routing system evaluates both the role-based policy and the heuristic-based policy, combining them into a final decision. For example, if the role is executor and the heuristic score is low, the system routes to a fast model suitable for simple execution tasks. If the role is planner and the heuristic score is high, the system routes to a powerful model suitable for complex planning.

This integration creates a multi-dimensional routing decision space. Role defines the type of reasoning required, while complexity score defines the difficulty level. The routing policy uses both dimensions to select the optimal model. A two-dimensional routing matrix maps (role, complexity) pairs to model selections, enabling nuanced routing decisions that consider both what kind of work is being done and how difficult that work is.

### Configuration and Extensibility

The heuristic system exposes comprehensive configuration options enabling users to tune routing behavior without code changes. Configuration is defined in the `.agent/config.yml` file under the `models.heuristics` section. Users can enable or disable the entire heuristic system with the `enabled` boolean flag. When disabled, routing falls back to strategy defaults (fixed model for fixed strategy, default model for adaptive strategy without heuristics).

Heuristic weights are configurable via the `weights` dictionary, which maps heuristic names to numeric weight multipliers. Default weights are 1.0 for all heuristics, giving them equal influence. Users can increase a weight to give a heuristic more influence or decrease a weight to reduce its influence. For example, setting `file_count: 1.5` and `language: 0.5` makes file count highly influential while de-emphasizing language complexity in routing decisions.

Threshold configuration allows users to adjust the score ranges that map to complexity tiers. The default thresholds are thirty for the low-medium boundary and seventy for the medium-high boundary, but users can adjust these based on their model inventory and quality requirements. A team with a very capable 7B model might raise the low threshold to fifty, routing more tasks to the fast model. A team with limited GPU resources might lower the high threshold to sixty, reserving the large model for only the most complex tasks.

Extensibility is a core design principle. The `IRoutingHeuristic` interface is public and documented, enabling users to implement custom heuristics in their own assemblies. Custom heuristics can encode domain-specific knowledge—for example, a heuristic that checks if a task affects files in a critical security module and assigns a high complexity score to ensure careful review by a powerful model. Custom heuristics are registered via dependency injection, integrating seamlessly with built-in heuristics in the evaluation pipeline.

The heuristic plugin model supports prioritization. Each heuristic defines a `Priority` property, and the engine executes heuristics in priority order (lowest priority number first). This enables ordering dependencies—a heuristic that depends on metadata computed by another heuristic can run later in the pipeline. Priority also influences tie-breaking in score aggregation, ensuring deterministic behavior when multiple heuristics produce similar scores.

### Observability and Debugging

Comprehensive logging and debugging capabilities make heuristic behavior transparent and tunable. Every heuristic evaluation is logged with structured data including the heuristic name, the score it assigned, its confidence level, and its reasoning string. These logs enable users to understand why a particular routing decision was made and to identify when heuristics are behaving unexpectedly.

The CLI provides introspection commands for real-time debugging. The `acode routing heuristics` command displays the current state of the heuristic system—which heuristics are registered, their priorities, current weight settings, and enabled status. The `acode routing evaluate` command allows users to dry-run heuristic evaluation on a hypothetical task without actually executing it. Users can provide a task description, and the system runs all heuristics, displays individual scores and reasoning, shows the weighted aggregation calculation, and indicates which model would be selected. This dry-run capability enables rapid iteration on heuristic tuning without trial-and-error on actual tasks.

Override visibility is similarly comprehensive. The `acode routing override` command displays the complete override precedence chain—which request override is active, which session override is active, which config override is active, and which source ultimately determines the model selection. This visibility helps users debug situations where a model selection seems incorrect, quickly identifying whether an override is interfering with expected heuristic behavior.

Logging includes timing instrumentation for performance monitoring. Each heuristic evaluation logs its execution duration, and the engine logs total evaluation time. Performance metrics are collected and can be exported to monitoring systems, enabling teams to track heuristic evaluation overhead and detect performance regressions. The non-functional requirement that heuristic evaluation completes in under fifty milliseconds per heuristic and one hundred milliseconds total is enforced and monitored through these metrics.

### Failure Handling and Resilience

The heuristic system is designed to degrade gracefully under failure conditions. If a heuristic throws an exception during evaluation, the engine catches the exception, logs an error, and continues evaluating remaining heuristics. The failed heuristic is excluded from score aggregation, and the remaining heuristics' results are used to compute the final score. This ensures that a bug in a single heuristic implementation does not break the entire routing system.

If all heuristics fail to produce results, the engine falls back to a default medium complexity score of fifty, which maps to the default model tier. This conservative fallback ensures that routing always succeeds, even under catastrophic heuristic failure. The failure is logged with severity error, alerting operators to the problem while allowing the system to continue functioning.

Override validation failures trigger fast failure with clear error messages. If a user specifies `--model invalid-model-id`, the system immediately returns an error before attempting any inference, preventing wasted work on invalid configurations. Error messages include actionable guidance—for example, "Model 'invalid-model-id' not found. Available models: llama3.2:7b, llama3.2:70b. Use 'acode models list' to see all registered models."

Operating mode constraint violations are similarly handled with clear, actionable errors. If a user attempts `--model gpt-4` in LocalOnly mode, the error message explains: "Cannot use model 'gpt-4' in LocalOnly operating mode. This model requires external API access. Available local models: llama3.2:7b, llama3.2:70b. To use external models, switch to Burst mode with 'acode config set mode burst'."

### Performance Characteristics and Optimization

Heuristic evaluation is designed for low latency to minimize impact on routing decision overhead. Each built-in heuristic is implemented with algorithmic efficiency in mind—file counting is an O(1) lookup in the task context metadata, task type analysis uses precompiled regex patterns, language detection uses hash table lookups. These optimizations ensure individual heuristic evaluation completes in single-digit milliseconds.

The heuristic engine employs caching to avoid redundant computation. Within a single request, if the same task context is evaluated multiple times (for example, during retry logic), the engine returns the cached result rather than re-running heuristics. Cache lifetime is scoped to the request boundary, ensuring that configuration changes or heuristic registration changes take effect for subsequent requests.

Score aggregation uses streaming calculation to minimize memory allocation. Rather than materializing a list of all heuristic results and then computing weighted averages, the engine accumulates weighted sums in a single pass through the results. This reduces garbage collection pressure and improves throughput when evaluating many tasks in rapid succession.

Override resolution is optimized for the common case—no overrides present. The resolver checks for request overrides first (most specific, least common), and if none are present, checks session and config overrides. This ordering minimizes conditional branches for the typical path where heuristics are used, keeping the hot path efficient.

### Trade-offs and Alternative Approaches Considered

Several alternative design approaches were considered and rejected in favor of the current heuristic-based model. One alternative was machine learning-based complexity prediction, where a trained model predicts task complexity based on historical data. This approach was rejected for MVP due to complexity—it requires collecting training data, managing model lifecycle, and handling cold-start problems. Heuristic-based routing provides immediate value without data collection overhead, while remaining extensible to ML-based approaches in future iterations.

Another alternative was user-driven tagging, where users explicitly tag tasks as simple, medium, or complex when submitting them. This approach was rejected because it shifts cognitive load to users—they must consciously categorize every task, which slows workflow and introduces inconsistency. Heuristic-based routing automates this categorization, learning from observable task characteristics rather than requiring explicit user input.

A third alternative was static routing based purely on configuration, with no dynamic adaptation. This approach was rejected because it fails to optimize for the diversity of tasks in real development workloads. Static routing either over-provisions by using large models for all tasks (wasting resources) or under-provisions by using small models for all tasks (sacrificing quality on complex tasks). Heuristic-based adaptive routing balances these extremes, optimizing resource usage while maintaining quality.

The chosen design balances automation with user control. Heuristics provide smart defaults that work well for most tasks, reducing cognitive load and improving efficiency. Overrides provide escape hatches for edge cases and user expertise, ensuring that automation never blocks user intent. This balance aligns with Acode's design philosophy of default safety with user agency—the system makes good decisions automatically, but users remain in full control when needed.

### Constraints and Limitations

Heuristic-based routing has inherent limitations that users should understand. Heuristics operate on observable signals in task metadata, not on deep understanding of task semantics. A task described as "update README" might actually involve significant technical writing complexity, but heuristics will likely classify it as low complexity due to the file type and small file count. Users must use overrides when task descriptions understate complexity.

Heuristic accuracy depends on task description quality. Vague or minimal task descriptions provide fewer signals for heuristics to analyze, leading to lower confidence and more conservative (medium-complexity) scoring. Users who provide detailed task descriptions—specifying file count, task type, affected languages—enable more accurate heuristic evaluation and better routing decisions.

The heuristic system does not learn from outcomes. If a task was routed to a small model and failed due to insufficient model capability, the heuristic system does not automatically adjust future routing for similar tasks. This feedback loop is a future enhancement. MVP heuristics are stateless and do not adapt based on historical success or failure rates.

Override precedence, while clear and deterministic, can create confusion if users forget about overrides they set previously. A developer might set a session override for complex work, then later wonder why simple tasks are routing to large models. The introspection commands (`acode routing override`) mitigate this by surfacing active overrides, but users must remember to check when routing behavior seems unexpected.

### Future Enhancements and Post-MVP Evolution

While Task 009.b delivers a comprehensive heuristic and override system for MVP, several enhancements are planned for post-MVP iterations. Machine learning-based complexity prediction can augment rule-based heuristics by learning from historical routing decisions and outcomes. A model trained on task descriptions and observed inference success/failure can predict complexity more accurately than hand-coded rules for ambiguous cases.

Historical performance tracking will enable outcome-based heuristic tuning. The system can log which model was used for each task, whether the task succeeded, and metrics like inference time and quality scores. This data enables automatic weight tuning—if `LanguageHeuristic` consistently over-predicts complexity for Python tasks, its weight can be automatically reduced.

User preference learning can personalize routing. Different developers have different quality/speed trade-offs. Some developers prefer fast responses even if it means occasional retries with larger models. Others prefer conservative routing that maximizes first-attempt success. The system can learn these preferences from override patterns and adjust heuristic thresholds per user.

Model capability detection will enable more sophisticated routing. Future versions can probe model capabilities—does a model support function calling, does it handle long contexts well, does it perform well on specific languages—and route based on these capabilities in addition to general model size. This enables finer-grained routing decisions that match task requirements to model strengths.

Benchmark-based routing will use model-specific performance benchmarks to inform routing. Rather than assuming all 70B models have equivalent capability, the system can consult benchmarks indicating which models excel at specific task types and route accordingly. This enables multi-model ecosystems where different models specialize in different domains, with heuristics routing tasks to the optimal specialist model.

---

## Use Cases

### Use Case 1: DevBot Optimizes Routing for Batch Refactoring

**Persona:** DevBot is a junior developer working on a large codebase modernization project. They need to apply consistent refactoring across multiple files—renaming variables, updating import statements, and standardizing code style.

**Before Heuristics:** DevBot runs `acode refactor "Rename variable oldName to newName across all files"`. Without heuristics, this task routes to the default 70B model, taking fifteen seconds per file. For a fifty-file refactoring, total inference time is twelve minutes. DevBot waits through each file's processing, experiencing frequent context switches that break concentration.

**After Heuristics with Adaptive Routing:** DevBot runs the same command. The heuristic engine evaluates the task—`TaskTypeHeuristic` identifies "rename variable" as a low-complexity refactoring (score twenty-five), `FileCountHeuristic` sees fifty files affected (score sixty), `LanguageHeuristic` detects TypeScript (score twenty). Weighted aggregation produces a score of thirty-eight (medium complexity). The system routes to the default 70B model, which is appropriate for the cross-file scope.

However, DevBot notices that simple renaming doesn't require the most powerful model. They adjust configuration:

```yaml
models:
  heuristics:
    thresholds:
      high: 80  # Reserve large model for very complex tasks
```

Now the same task scores thirty-eight, routes to the 7B model, and completes in three seconds per file—one hundred fifty seconds total instead of twelve minutes. DevBot completes the refactoring in under three minutes, saves nine minutes, and maintains focus throughout the batch operation.

**Outcome:** Heuristic routing reduced inference time by eighty-seven percent for batch refactoring. DevBot learned to tune threshold configuration based on task characteristics, optimizing their workflow. Over a week of similar refactoring work (twenty batch operations), DevBot saves three hours of waiting time, equivalent to approximately three hundred sixty dollars in productivity at standard engineering rates.

### Use Case 2: Jordan Overrides Heuristics for Security-Critical Work

**Persona:** Jordan is a senior developer reviewing authentication and authorization code. They're implementing a new permission system that touches several critical security modules. Jordan values correctness over speed for security work.

**Heuristics Underestimate Complexity:** Jordan runs `acode implement "Add role-based access control to API endpoints"`. The heuristic engine analyzes the task—`TaskTypeHeuristic` identifies "add" as new feature development (score fifty-five), `FileCountHeuristic` sees four files affected (score thirty), `LanguageHeuristic` detects Go (score twenty-five). Weighted aggregation produces a score of thirty-eight (medium complexity). The system routes to the default 70B model.

Jordan reviews the initial implementation and finds it lacks nuance around edge cases—what happens when a user has conflicting roles, how are permissions inherited, what's the precedence order for deny rules. The default model didn't consider these security-critical details.

**Session Override for Security Focus:** Jordan decides that all security work requires maximum model capability, regardless of apparent task complexity. They set a session override:

```bash
export ACODE_MODEL=llama3.2:70b
acode config set-session --model llama3.2:70b
```

Now all subsequent requests route to the 70B model, bypassing heuristics. Jordan re-runs the implementation command. The 70B model produces more comprehensive code, including explicit edge case handling, detailed comments explaining security assumptions, and defensive validation logic.

Jordan continues working on related security tasks—updating tests, adding audit logging, implementing rate limiting. All tasks use the 70B model automatically due to the session override. At the end of the security sprint, Jordan clears the override:

```bash
acode config clear-session
```

Normal heuristic-based routing resumes for non-security work.

**Outcome:** Session overrides enabled Jordan to enforce security-specific quality requirements without manual model selection for each request. The override lasted three days across twenty-five security-related tasks. While inference time was slower (averaging fifteen seconds vs. potential three-second routing to smaller models), Jordan avoided three security bugs that the default model missed. Fixing these bugs post-deployment would have cost thirty to forty hours of debugging, regression testing, and deployment coordination—approximately four thousand to five thousand dollars in engineering time. The session override investment of five additional minutes of cumulative wait time prevented significantly costlier downstream failures.

### Use Case 3: Alex Tunes Heuristic Weights for DevOps Workflows

**Persona:** Alex is a DevOps engineer who uses Acode for infrastructure automation—writing Terraform configurations, updating CI/CD pipelines, and maintaining deployment scripts. Alex notices that heuristics often route infrastructure work to small models, resulting in subtle configuration errors.

**Initial Heuristic Behavior:** Alex runs `acode generate "Create Terraform module for RDS cluster with read replicas"`. Heuristics evaluate the task—`TaskTypeHeuristic` identifies "create" as medium complexity (score forty), `FileCountHeuristic` sees one file (score ten), `LanguageHeuristic` detects HCL (score fifteen). Combined score is twenty-two (low complexity). The system routes to the 7B model.

The 7B model generates basic Terraform code but misses important production concerns—no backup retention policy, no encryption at rest configuration, no parameter group customization, no monitoring alarms. Alex must manually review and add these configurations, which defeats the purpose of AI-assisted generation.

**Tuning Heuristic Weights:** Alex analyzes the problem—infrastructure code seems simple (declarative configuration files) but requires deep domain knowledge about best practices, security requirements, and operational concerns. The issue is that `FileCountHeuristic` and `LanguageHeuristic` give low scores to infrastructure files.

Alex decides that for infrastructure work, task type should dominate routing decisions. They adjust weights:

```yaml
models:
  heuristics:
    weights:
      file_count: 0.3      # De-emphasize file count
      task_type: 2.0       # Strongly emphasize task type
      language: 0.5        # Reduce language influence
```

Alex re-runs the same task. Now the weighted aggregation gives much more weight to `TaskTypeHeuristic`'s score of forty. The combined score is fifty-eight (medium complexity), routing to the default 70B model.

The 70B model generates comprehensive Terraform code including backup policies, encryption configuration, parameter groups, CloudWatch alarms, tags for cost allocation, and detailed comments explaining each configuration choice. Alex reviews the generated code and finds it production-ready with minimal adjustments.

**Further Optimization:** Alex takes the tuning further by creating a custom heuristic for infrastructure files:

```csharp
public class InfrastructureHeuristic : IRoutingHeuristic
{
    public string Name => "Infrastructure";
    public int Priority => 1;

    public HeuristicResult Evaluate(HeuristicContext context)
    {
        var infraExtensions = new[] { ".tf", ".yml", ".yaml", "Dockerfile" };
        var isInfraFile = context.Files.Any(f =>
            infraExtensions.Any(ext => f.EndsWith(ext)));

        if (isInfraFile)
        {
            return new HeuristicResult
            {
                Score = 70,  // Infrastructure always high priority
                Confidence = 0.95,
                Reasoning = "Infrastructure files require production-grade configuration"
            };
        }

        return new HeuristicResult { Score = 0, Confidence = 0.0, Reasoning = "Not infrastructure" };
    }
}
```

Alex registers this custom heuristic in dependency injection. Now any task touching infrastructure files automatically routes to capable models, ensuring production-quality output.

**Outcome:** Weight tuning improved infrastructure code quality from sixty percent production-ready to ninety-five percent production-ready, reducing Alex's manual review time from fifteen minutes per task to three minutes per task—saving twelve minutes per infrastructure task. With ten infrastructure tasks per week, this saves two hours weekly, approximately one hundred hours annually. At a DevOps engineering rate of one hundred forty dollars per hour, this represents fourteen thousand dollars in annual productivity gains. The custom heuristic implementation took Alex ninety minutes to write and test, paying for itself within the first week of deployment.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Routing Heuristic | Rule-based evaluator that analyzes task characteristics to estimate complexity and inform model selection |
| Complexity Score | Numeric estimate (0-100) of task difficulty combining multiple heuristic signals |
| Complexity Factor | Individual signal in complexity estimation (file count, language, task type) |
| Override | User-specified routing decision that bypasses heuristic evaluation |
| Request Override | Single-request model specification via CLI flag (highest precedence) |
| Session Override | Session-wide model lock via environment variable or CLI command |
| Config Override | Persistent routing configuration in .agent/config.yml |
| Heuristic Engine | Infrastructure component that orchestrates heuristic execution and aggregates results |
| Heuristic Plugin | Custom heuristic implementation extending IRoutingHeuristic interface |
| Confidence Level | Numeric value (0.0-1.0) indicating certainty of heuristic estimate, used for weighting |
| Signal | Observable characteristic of task extracted from context (file extensions, description keywords) |
| File Count | Number of files affected by task, used by FileCountHeuristic |
| Task Type | Category of task (feature, bug, refactor, enhancement) detected from description |
| Adaptive Mode | Complexity-aware routing strategy from Task 009 that uses heuristics to select model tiers |
| Precedence | Hierarchical order of override application (request > session > config > heuristics) |
| Heuristic Context | Data structure containing task metadata provided to heuristics for evaluation |
| Heuristic Result | Data structure returned by heuristics containing score, confidence, and reasoning |
| Weighted Aggregation | Algorithm combining multiple heuristic scores using confidence levels as weights |
| Model Tier | Classification of models by capability (light, default, capable) mapped from complexity scores |
| Override Resolver | Infrastructure component that checks override sources in precedence order |

---

## Out of Scope

The following items are explicitly excluded from Task 009.b:

- **Role definitions** - Covered in Task 009.a
- **Fallback handling** - Covered in Task 009.c
- **Model provider logic** - Covered in Tasks 004-006
- **Machine learning for heuristics** - Not in MVP
- **Historical performance tracking** - Post-MVP
- **User preference learning** - Post-MVP
- **Cost-based optimization** - Not applicable (local)
- **Multi-model ensemble** - Post-MVP
- **Model capability detection** - Future enhancement
- **Benchmark-based routing** - Post-MVP

---

## Functional Requirements

### IRoutingHeuristic Interface

- FR-001: Interface MUST be in Application layer
- FR-002: MUST have Evaluate(RoutingContext) method
- FR-003: Evaluate MUST return HeuristicResult
- FR-004: HeuristicResult MUST include score
- FR-005: HeuristicResult MUST include confidence
- FR-006: HeuristicResult MUST include reasoning
- FR-007: Interface MUST have Name property
- FR-008: Interface MUST have Priority property

### HeuristicEngine Implementation

- FR-009: Engine MUST be in Infrastructure layer
- FR-010: Engine MUST run all registered heuristics
- FR-011: Engine MUST aggregate results
- FR-012: Engine MUST weight by confidence
- FR-013: Engine MUST return combined score
- FR-014: Engine MUST log heuristic evaluations

### Complexity Score

- FR-015: Score MUST be 0-100 range
- FR-016: 0-30 MUST be considered "low"
- FR-017: 31-70 MUST be considered "medium"
- FR-018: 71-100 MUST be considered "high"
- FR-019: Score MUST map to routing tiers
- FR-020: Tier mapping MUST be configurable

### Built-in Heuristics

- FR-021: FileCountHeuristic MUST be included
- FR-022: FileCount < 3 = low complexity
- FR-023: FileCount 3-10 = medium complexity
- FR-024: FileCount > 10 = high complexity
- FR-025: TaskTypeHeuristic MUST be included
- FR-026: Bug fixes = lower complexity baseline
- FR-027: New features = medium complexity baseline
- FR-028: Refactoring = higher complexity baseline
- FR-029: LanguageHeuristic MUST be included
- FR-030: Language complexity ratings defined

### Override Precedence

- FR-031: Request override MUST take highest precedence
- FR-032: Session override MUST override config
- FR-033: Config override MUST override heuristics
- FR-034: Heuristics MUST be lowest precedence
- FR-035: Precedence MUST be documented

### Request-Level Override

- FR-036: CLI MUST support --model flag
- FR-037: Flag value MUST be validated model ID
- FR-038: Override MUST apply to single request
- FR-039: Override MUST be logged
- FR-040: Override MUST respect mode constraints

### Session-Level Override

- FR-041: ACODE_MODEL env var MUST be supported
- FR-042: CLI MUST support `acode config set-session`
- FR-043: Session override MUST persist until exit
- FR-044: Session override MUST be clearable
- FR-045: Session override MUST be logged

### Configuration Override

- FR-046: Config section: models.override
- FR-047: override.model MUST force specific model
- FR-048: override.strategy MUST force strategy
- FR-049: override.disable_heuristics MUST skip heuristics
- FR-050: Config MUST be reloadable

### Heuristic Configuration

- FR-051: Config section: models.heuristics
- FR-052: heuristics.enabled MUST enable/disable
- FR-053: Default MUST be enabled=true
- FR-054: heuristics.weights MUST allow custom weights
- FR-055: heuristics.thresholds MUST be configurable

### Adaptive Routing Integration

- FR-056: Adaptive strategy MUST use heuristics
- FR-057: Low score MUST route to light model
- FR-058: Medium score MUST route to default model
- FR-059: High score MUST route to capable model
- FR-060: Model tiers MUST be configurable

### Logging

- FR-061: Heuristic evaluation MUST be logged
- FR-062: Log MUST include each heuristic score
- FR-063: Log MUST include combined score
- FR-064: Override application MUST be logged
- FR-065: Override source MUST be logged

### CLI Integration

- FR-066: `acode routing heuristics` MUST show state
- FR-067: MUST show enabled heuristics
- FR-068: MUST show current weights
- FR-069: `acode routing override` MUST show overrides
- FR-070: MUST show precedence chain
- FR-071: `acode routing evaluate` MUST dry-run heuristics
- FR-072: Evaluate MUST show individual scores
- FR-073: Evaluate MUST show weighted calculation
- FR-074: Evaluate MUST show recommended model
- FR-075: All commands MUST support --json output

### Extensibility

- FR-076: IRoutingHeuristic MUST be public
- FR-077: Custom heuristics MUST be registrable via DI
- FR-078: Custom heuristics MUST integrate with engine
- FR-079: Priority MUST control execution order
- FR-080: Engine MUST support runtime heuristic registration

### Error Handling

- FR-081: Invalid model ID MUST fail fast
- FR-082: Mode constraint violation MUST fail fast
- FR-083: Failed heuristic MUST be skipped
- FR-084: All heuristics failed MUST use default score
- FR-085: Errors MUST include actionable messages

### Configuration Validation

- FR-086: Config schema MUST validate weights
- FR-087: Weight values MUST be positive numbers
- FR-088: Threshold values MUST be 0-100
- FR-089: Low threshold MUST be less than high threshold
- FR-090: Invalid config MUST prevent startup

### Integration with Task 009

- FR-091: HeuristicContext MUST extend RoutingContext
- FR-092: Adaptive strategy MUST consult HeuristicEngine
- FR-093: Score MUST map to model tiers
- FR-094: Tier mapping MUST use config from Task 009
- FR-095: RoutingDecision MUST include heuristic metadata

### Integration with Task 009.a

- FR-096: Role and complexity MUST both influence routing
- FR-097: (Role, Complexity) matrix MUST be configurable
- FR-098: Role precedence MUST be documented
- FR-099: Combined routing MUST log both factors
- FR-100: Role override MUST coexist with heuristics

---

## Non-Functional Requirements

### Performance

- NFR-001: Heuristic evaluation MUST complete < 50ms
- NFR-002: All heuristics combined MUST complete < 100ms
- NFR-003: Override lookup MUST complete < 1ms
- NFR-004: Results MUST be cached per request

### Reliability

- NFR-005: Heuristic failure MUST not block routing
- NFR-006: Failed heuristic MUST be skipped
- NFR-007: Invalid override MUST fail fast
- NFR-008: Partial heuristics MUST still produce score

### Security

- NFR-009: Overrides MUST respect mode constraints
- NFR-010: Model IDs MUST be validated
- NFR-011: Config parsing MUST be safe

### Observability

- NFR-012: All heuristics MUST log results
- NFR-013: Score calculation MUST be auditable
- NFR-014: Override chain MUST be visible
- NFR-015: Metrics SHOULD track score distribution

### Maintainability

- NFR-016: Heuristics MUST be pluggable
- NFR-017: New heuristics MUST be addable
- NFR-018: All public APIs MUST have XML docs
- NFR-019: Tests MUST cover all heuristics
- NFR-020: Code coverage MUST exceed 90%
- NFR-021: Configuration schema MUST be versioned

### Usability

- NFR-022: CLI output MUST be human-readable
- NFR-023: Error messages MUST be actionable
- NFR-024: Dry-run mode MUST not affect state
- NFR-025: Configuration MUST have inline examples
- NFR-026: Heuristic reasoning MUST be clear

### Compatibility

- NFR-027: MUST integrate with Task 009 routing
- NFR-028: MUST integrate with Task 009.a roles
- NFR-029: MUST respect Task 001 mode constraints
- NFR-030: MUST use Task 004 model catalog

---

## User Manual Documentation

### Overview

Routing heuristics analyze tasks to inform model selection. Overrides enable explicit control when needed. This guide covers both mechanisms.

### Quick Start

Heuristics are enabled by default:

```yaml
# .agent/config.yml
models:
  routing:
    strategy: adaptive  # Uses heuristics
```

### Heuristics

#### How Heuristics Work

1. Task arrives for routing
2. Heuristics analyze task characteristics
3. Each heuristic produces a complexity score (0-100)
4. Scores are weighted and combined
5. Final score determines model tier

#### Built-in Heuristics

**FileCountHeuristic:**
- Files < 3: Low complexity (+10)
- Files 3-10: Medium complexity (+30)
- Files > 10: High complexity (+50)

**TaskTypeHeuristic:**
- Bug fix: Lower baseline (+10)
- Enhancement: Medium baseline (+25)
- New feature: Higher baseline (+35)
- Refactoring: Highest baseline (+45)

**LanguageHeuristic:**
- Simple languages (Markdown, JSON): +5
- Standard languages (JS, Python): +20
- Complex languages (C++, Rust): +35

#### Score Interpretation

| Score | Complexity | Recommended Model |
|-------|------------|-------------------|
| 0-30 | Low | Small/fast model |
| 31-70 | Medium | Default model |
| 71-100 | High | Large/capable model |

#### Heuristic Configuration

```yaml
models:
  heuristics:
    enabled: true
    
    weights:
      file_count: 1.0      # Default weight
      task_type: 1.2       # Slightly more important
      language: 0.8        # Slightly less important
    
    thresholds:
      low: 30              # Below this = low
      high: 70             # Above this = high
```

### Overrides

#### Precedence Order

1. **Request override** (highest) - `--model` flag
2. **Session override** - Environment variable
3. **Config override** - Configuration file
4. **Heuristics** (lowest) - Automatic calculation

#### Request-Level Override

Use for single requests:

```bash
# Force specific model for this request
acode run --model llama3.2:70b

# Useful for complex one-off tasks
acode analyze --model llama3.2:70b "Review architecture"
```

#### Session-Level Override

Use for a series of related tasks:

```bash
# Set for entire session
export ACODE_MODEL=llama3.2:70b
acode run  # Uses llama3.2:70b

# Or via CLI
acode config set-session --model llama3.2:70b
acode run  # Uses llama3.2:70b

# Clear session override
acode config clear-session
```

#### Configuration Override

Use for persistent preferences:

```yaml
# .agent/config.yml
models:
  override:
    # Force specific model always
    model: llama3.2:70b
    
    # Or disable heuristics
    disable_heuristics: true
```

### Adaptive Routing

Adaptive routing uses heuristics to select models:

```yaml
models:
  routing:
    strategy: adaptive
    
    # Model tiers
    tiers:
      low: llama3.2:7b      # Fast, for simple tasks
      medium: llama3.2:7b   # Default
      high: llama3.2:70b    # Capable, for complex tasks
```

### CLI Commands

```bash
# Show heuristic state
$ acode routing heuristics
Heuristics: enabled

Registered Heuristics:
  FileCountHeuristic (priority: 1)
  TaskTypeHeuristic (priority: 2)
  LanguageHeuristic (priority: 3)

Current Weights:
  file_count: 1.0
  task_type: 1.2
  language: 0.8

# Show override state
$ acode routing override
Active Overrides:

  Request: (none)
  Session: llama3.2:70b (via ACODE_MODEL)
  Config: (none)

Effective Model: llama3.2:70b (from session)

# Test heuristics on a task
$ acode routing evaluate "Add input validation"
Evaluating task: "Add input validation"

Heuristic Results:
  FileCountHeuristic: 25 (3 files, confidence: 0.8)
  TaskTypeHeuristic: 35 (new feature, confidence: 0.9)
  LanguageHeuristic: 20 (TypeScript, confidence: 1.0)

Combined Score: 27 (Low complexity)
Recommended Tier: low
Recommended Model: llama3.2:7b
```

### Advanced Configuration

#### Custom Threshold Tuning

Adjust thresholds to match your model inventory:

```yaml
models:
  heuristics:
    thresholds:
      low: 25      # More aggressive routing to fast models
      high: 75     # Reserve capable model for very complex tasks

    # Per-heuristic configuration
    file_count:
      thresholds:
        low: 2     # < 2 files = low complexity
        high: 15   # > 15 files = high complexity

    task_type:
      scores:
        bug: 15           # Bug fixes are simple
        enhancement: 30   # Enhancements are moderate
        feature: 60       # Features are complex
        refactor: 80      # Refactors are very complex
```

#### Disable Specific Heuristics

```yaml
models:
  heuristics:
    enabled: true
    disabled_heuristics:
      - LanguageHeuristic  # Ignore language complexity
```

#### Custom Heuristic Registration

```csharp
// Startup.cs or Program.cs
services.AddSingleton<IRoutingHeuristic, CustomSecurityHeuristic>();
services.AddSingleton<IRoutingHeuristic, CustomPerformanceHeuristic>();
```

### Best Practices

#### General Principles

1. **Start with defaults** - Built-in heuristics work well for 80% of cases
2. **Monitor first, tune later** - Collect data on routing decisions before adjusting
3. **Use overrides sparingly** - Rely on heuristics for consistency
4. **Document custom heuristics** - Include reasoning in code comments
5. **Test heuristic changes** - Use `acode routing evaluate` before deploying

#### Heuristic Configuration Best Practices

6. **Tune weights incrementally** - Change one weight at a time by 0.1-0.3
7. **Higher weights for reliable signals** - File count is objective, task type is subjective
8. **Lower weights for noisy signals** - Language detection can be ambiguous
9. **Use confidence levels** - Let heuristics indicate their certainty
10. **Balance precision and recall** - Over-tuning can overfit to current workload

#### Override Usage Patterns

11. **Request overrides for experiments** - Test model behavior without config changes
12. **Session overrides for sprints** - Lock model for focused work periods
13. **Config overrides for policies** - Enforce team standards in shared configuration
14. **Clear overrides promptly** - Avoid confusion from stale session settings
15. **Log override reasons** - Document why overrides were needed for future tuning

#### Security and Critical Work

16. **Override for security code** - Use capable models for authentication, authorization, crypto
17. **Override for data migrations** - Complex schema changes need careful review
18. **Override for public APIs** - API design decisions have long-term consequences
19. **Override for performance-critical code** - Optimization requires deep reasoning
20. **Override for compliance code** - Regulatory requirements demand accuracy

### Troubleshooting

#### Issue 1: Heuristics Choosing Wrong Model

**Symptoms:**
```
Task seems complex but routed to small/fast model
Task seems simple but routed to large/slow model
Inconsistent routing for similar tasks
```

**Possible Causes:**
- Heuristics underestimate or overestimate complexity
- Task description lacks sufficient detail for heuristics
- Weights not tuned for your workload
- Thresholds don't match your model capabilities

**Solutions:**

1. **Immediate fix - Use override:**
   ```bash
   acode run --model llama3.2:70b "your task"
   ```

2. **Investigate - Check heuristic evaluation:**
   ```bash
   acode routing evaluate "your task description"
   ```
   Review individual heuristic scores and reasoning.

3. **Tune weights - Adjust influential heuristics:**
   ```yaml
   models:
     heuristics:
       weights:
         task_type: 1.5  # Increase if task type is better predictor
         file_count: 0.7 # Decrease if file count is misleading
   ```

4. **Adjust thresholds - Match model capabilities:**
   ```yaml
   models:
     heuristics:
       thresholds:
         low: 35   # Raise if fast model is very capable
         high: 80  # Raise if you want to reserve large model
   ```

#### Issue 2: Override Not Working

**Symptoms:**
```
Specified --model flag but different model was used
Set ACODE_MODEL but routing ignores it
Config override has no effect
```

**Possible Causes:**
- Model ID is invalid or not registered
- Operating mode constraint violation
- Higher-precedence override exists
- Typo in model ID or environment variable name

**Solutions:**

1. **Verify model exists:**
   ```bash
   acode models list
   ```
   Check if specified model ID appears in output.

2. **Check operating mode:**
   ```bash
   acode config show mode
   ```
   Ensure requested model is compatible with current mode (LocalOnly, Burst, etc.).

3. **Check override precedence:**
   ```bash
   acode routing override
   ```
   Shows all active overrides. Request override beats session, session beats config.

4. **Fix common typos:**
   - ✅ `ACODE_MODEL` (correct)
   - ❌ `ACODE_MODELS` (wrong)
   - ✅ `llama3.2:70b` (correct format)
   - ❌ `llama-3.2-70b` (wrong format)

#### Issue 3: Heuristics Disabled or Always Return Same Score

**Symptoms:**
```
Score always 50 regardless of task
Routing logs show "heuristics: disabled"
No individual heuristic scores in logs
```

**Possible Causes:**
- Heuristics explicitly disabled in configuration
- Config override with `disable_heuristics: true`
- All heuristics failed to evaluate (error condition)

**Solutions:**

1. **Check heuristic state:**
   ```bash
   acode routing heuristics
   ```
   Shows enabled/disabled status.

2. **Enable heuristics in config:**
   ```yaml
   models:
     heuristics:
       enabled: true  # Ensure this is true
     override:
       disable_heuristics: false  # Ensure this is false or absent
   ```

3. **Check for heuristic errors in logs:**
   ```bash
   acode logs --level error --filter heuristic
   ```
   Look for exceptions during heuristic evaluation.

#### Issue 4: Session Override Persists Unexpectedly

**Symptoms:**
```
Simple tasks using large model after complex work finished
Session override still active in new terminal
ACODE_MODEL set but don't remember setting it
```

**Possible Causes:**
- Environment variable set in shell profile (.bashrc, .zshrc)
- Session override set via CLI not cleared
- Parent process exported variable to child processes

**Solutions:**

1. **Check environment variable:**
   ```bash
   echo $ACODE_MODEL
   ```
   If set, unset it:
   ```bash
   unset ACODE_MODEL
   ```

2. **Clear session override:**
   ```bash
   acode config clear-session
   ```

3. **Check shell profile:**
   ```bash
   grep ACODE_MODEL ~/.bashrc ~/.zshrc
   ```
   Remove any `export ACODE_MODEL=...` lines.

#### Issue 5: Custom Heuristic Not Running

**Symptoms:**
```
Custom heuristic registered but not in `acode routing heuristics` output
Custom heuristic scores not appearing in logs
Routing decisions don't reflect custom logic
```

**Possible Causes:**
- Heuristic not registered in dependency injection
- IRoutingHeuristic interface not implemented correctly
- Exception thrown during heuristic evaluation
- Priority conflicts with other heuristics

**Solutions:**

1. **Verify DI registration:**
   ```csharp
   // In Startup.cs or Program.cs
   services.AddSingleton<IRoutingHeuristic, YourCustomHeuristic>();
   ```

2. **Check interface implementation:**
   ```csharp
   public class YourCustomHeuristic : IRoutingHeuristic
   {
       public string Name => "YourCustom";  // Must be unique
       public int Priority => 10;           // Must be set

       public HeuristicResult Evaluate(HeuristicContext context)
       {
           // Implementation must return valid result
       }
   }
   ```

3. **Check logs for exceptions:**
   ```bash
   acode logs --filter "YourCustomHeuristic" --level error
   ```

4. **Test heuristic in isolation:**
   ```bash
   acode routing evaluate --verbose "test task"
   ```
   Look for your heuristic name in output.

### FAQ

**Q1: Can I disable heuristics completely?**
A: Yes. Set `models.heuristics.enabled: false` in config. Routing will use the default model for adaptive strategy or the configured model for fixed strategy.

**Q2: How do I know which heuristics are running?**
A: Run `acode routing heuristics` to see all registered heuristics, their priorities, and enabled status.

**Q3: Can I override heuristics for specific file types?**
A: Yes, create a custom heuristic that checks file extensions and returns high/low scores accordingly. Register it with high priority to dominate other heuristics.

**Q4: Do overrides respect operating mode constraints?**
A: Yes. All overrides are validated against operating mode constraints from Task 001. Attempting to override to a cloud model in LocalOnly mode will fail with an error.

**Q5: How do I test heuristic changes before deploying?**
A: Use `acode routing evaluate "task description"` to dry-run heuristic evaluation. This shows exactly how heuristics would score the task without executing it.

**Q6: Can I use different heuristic weights for different projects?**
A: Yes. Heuristic configuration lives in `.agent/config.yml`, which is per-project. Each project can have custom weights and thresholds.

**Q7: What happens if a heuristic crashes?**
A: The engine catches exceptions, logs an error, and continues with remaining heuristics. If all heuristics fail, the system falls back to a default medium score of 50.

**Q8: How do I see why a specific model was selected?**
A: Enable debug logging and review the `heuristic_evaluation` events. They include individual heuristic scores, confidence levels, reasoning, and the final weighted score.

**Q9: Can I combine role-based routing (Task 009.a) with heuristics?**
A: Yes. The routing system considers both role and complexity score. A two-dimensional matrix maps (role, complexity) pairs to models. For example, (planner, high) routes to the most capable model.

**Q10: Are heuristic weights per-heuristic or global?**
A: Per-heuristic. You can set `file_count: 1.5` and `task_type: 0.8` to give different weights to different heuristics.

---

## Assumptions

### Technical Assumptions

1. **Task 009 routing foundation exists** - Assumes RoutingPolicy, IModelRouter, and RoutingDecision classes are implemented
2. **Task 009.a role routing exists** - Assumes role-based routing rules and AgentRole enumeration are available
3. **Task 004 model catalog available** - Assumes ModelCatalog with registered models and model ID validation
4. **Task 001 operating modes enforced** - Assumes OperatingMode constraints are validated in model selection
5. **Dependency injection configured** - Assumes DI container supports interface registration and resolution
6. **Configuration system available** - Assumes .agent/config.yml parsing and validation infrastructure exists
7. **Logging infrastructure available** - Assumes structured logging with fields, levels, and filtering capabilities

### Operational Assumptions

8. **Task descriptions are meaningful** - Assumes users provide task descriptions with sufficient detail for heuristic analysis
9. **File metadata is available** - Assumes RoutingContext includes file paths and extensions for file count heuristics
10. **Language detection is reliable** - Assumes file extensions accurately indicate programming language
11. **Heuristic evaluation is fast** - Assumes individual heuristics complete in single-digit milliseconds
12. **Model tier mapping is configured** - Assumes users configure light/default/capable model tiers in Task 009 routing config
13. **Models are running and available** - Assumes model endpoints from Task 004-006 are operational when routing occurs

### Integration Assumptions

14. **CLI framework from Task 010 available** - Assumes command parsing, flag handling, and output formatting capabilities
15. **RoutingContext extensibility** - Assumes RoutingContext from Task 009 can be extended with HeuristicContext property
16. **Error code system exists** - Assumes error code registry and formatting from Task 000/002 infrastructure
17. **Metrics collection available** - Assumes instrumentation hooks for recording heuristic evaluation timing
18. **Session state management exists** - Assumes CLI can persist and retrieve session-scoped configuration

### Deployment Assumptions

19. **Single-threaded heuristic evaluation** - Assumes heuristic evaluation happens sequentially per request (no parallel heuristic execution)
20. **Configuration changes require restart** - Assumes heuristic weights and thresholds are loaded at startup, not hot-reloaded
21. **Custom heuristics are compiled** - Assumes custom heuristics are part of the compiled application, not loaded dynamically at runtime
22. **No distributed routing** - Assumes routing decisions are made locally per process, no coordination across multiple processes

---

## Security Considerations

### Threat 1: Override Injection via Environment Variables

**Description:** An attacker with access to set environment variables on the system could inject malicious model overrides by setting `ACODE_MODEL` to compromise routing decisions. If the attacker can set the environment variable to a cloud model in a compromised environment, they could exfiltrate code or task details to an external endpoint.

**Impact:** Code and task descriptions could be sent to attacker-controlled endpoints, leaking intellectual property or sensitive data.

**Mitigation:**

```csharp
namespace AgenticCoder.Infrastructure.Heuristics;

public sealed class OverrideValidator
{
    private readonly IModelCatalog _catalog;
    private readonly OperatingMode _currentMode;
    private readonly ILogger<OverrideValidator> _logger;

    public ValidationResult ValidateOverride(string modelId, OverrideSource source)
    {
        // Step 1: Validate model ID exists in catalog
        var model = _catalog.GetModel(modelId);
        if (model is null)
        {
            _logger.LogWarning(
                "Override validation failed: model {ModelId} not found in catalog. Source: {Source}",
                modelId, source);

            return ValidationResult.Failure(
                ErrorCode.ACODE_HEU_001,
                $"Model '{modelId}' not found in catalog. " +
                $"Use 'acode models list' to see available models.");
        }

        // Step 2: Validate model compatible with operating mode
        var isCompatible = _currentMode switch
        {
            OperatingMode.LocalOnly => model.Deployment == ModelDeployment.Local,
            OperatingMode.Burst => model.Deployment != ModelDeployment.ExternalAPI,
            OperatingMode.AirGapped => model.Deployment == ModelDeployment.Local,
            _ => false
        };

        if (!isCompatible)
        {
            _logger.LogWarning(
                "Override validation failed: model {ModelId} incompatible with mode {Mode}. Source: {Source}",
                modelId, _currentMode, source);

            return ValidationResult.Failure(
                ErrorCode.ACODE_HEU_002,
                $"Model '{modelId}' requires {model.Deployment} deployment, " +
                $"but current mode is {_currentMode}. " +
                $"Available models: {string.Join(", ", _catalog.GetModelsForMode(_currentMode))}");
        }

        // Step 3: Audit log successful override validation
        _logger.LogInformation(
            "Override validated successfully: model {ModelId}, source {Source}, mode {Mode}",
            modelId, source, _currentMode);

        return ValidationResult.Success(model);
    }
}
```

**Additional Controls:**
- Restrict environment variable modification to trusted users only at OS level
- Audit log all environment variable-based overrides with source tracking
- Implement allowlist of permitted models per operating mode in configuration
- Consider requiring cryptographic signature on override values for high-security deployments

---

### Threat 2: Heuristic Manipulation to Bypass Routing

**Description:** An attacker who can influence task descriptions or file metadata could craft inputs that manipulate heuristics into routing to unintended models. For example, describing a complex malicious task as a "simple typo fix" to route to a fast, less-scrutinized model that might miss security issues.

**Impact:** Security-critical or high-risk tasks could be routed to inadequate models, leading to low-quality code generation with vulnerabilities or logic errors.

**Mitigation:**

```csharp
namespace AgenticCoder.Infrastructure.Heuristics;

public sealed class TaskTypeHeuristic : IRoutingHeuristic
{
    private readonly ILogger<TaskTypeHeuristic> _logger;
    private readonly ISanitizer _sanitizer;

    // Security-critical keywords that force conservative routing
    private static readonly HashSet<string> SecurityKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "authentication", "authorization", "security", "crypto", "encryption",
        "password", "token", "credentials", "permission", "access control",
        "sanitize", "validate", "xss", "sql injection", "csrf"
    };

    public HeuristicResult Evaluate(HeuristicContext context)
    {
        // Step 1: Sanitize input to prevent injection attacks
        var sanitizedDescription = _sanitizer.SanitizeTaskDescription(context.TaskDescription);

        // Step 2: Check for security-critical keywords
        var containsSecurityKeyword = SecurityKeywords.Any(keyword =>
            sanitizedDescription.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (containsSecurityKeyword)
        {
            // Force high complexity for security-critical tasks
            _logger.LogWarning(
                "Security-critical task detected in heuristic evaluation: {Task}. " +
                "Forcing high complexity score.",
                sanitizedDescription);

            return new HeuristicResult
            {
                Score = 85,  // High complexity
                Confidence = 1.0,  // Maximum confidence
                Reasoning = "Security-critical task detected. Conservative routing enforced."
            };
        }

        // Step 3: Normal task type detection
        var taskType = DetectTaskType(sanitizedDescription);
        var score = taskType switch
        {
            TaskType.Bug => 20,
            TaskType.Enhancement => 35,
            TaskType.Feature => 55,
            TaskType.Refactor => 75,
            _ => 50  // Conservative default
        };

        // Step 4: Audit log heuristic decision
        _logger.LogDebug(
            "TaskTypeHeuristic evaluation: type={Type}, score={Score}, task={Task}",
            taskType, score, sanitizedDescription);

        return new HeuristicResult
        {
            Score = score,
            Confidence = CalculateConfidence(sanitizedDescription, taskType),
            Reasoning = $"Detected task type: {taskType}"
        };
    }
}
```

**Additional Controls:**
- Implement minimum complexity floor—no task can score below 15 regardless of heuristics
- Require human approval for routing decisions that deviate significantly from recent patterns
- Log all heuristic evaluations with full task descriptions for audit review
- Implement anomaly detection on routing decisions to flag suspicious patterns

---

### Threat 3: Configuration File Tampering

**Description:** An attacker with write access to `.agent/config.yml` could disable heuristics, manipulate weights, or inject overrides to force routing to compromised models or to degrade system performance by forcing expensive models for all tasks.

**Impact:** System availability could be degraded through resource exhaustion, or routing could be manipulated to leak data to attacker-controlled models.

**Mitigation:**

```csharp
namespace AgenticCoder.Infrastructure.Configuration;

public sealed class HeuristicConfigurationValidator
{
    private readonly ILogger<HeuristicConfigurationValidator> _logger;

    public ValidationResult ValidateConfiguration(HeuristicConfiguration config)
    {
        var errors = new List<string>();

        // Step 1: Validate weights are positive
        foreach (var (heuristicName, weight) in config.Weights)
        {
            if (weight <= 0)
            {
                errors.Add($"Weight for '{heuristicName}' must be positive. Got: {weight}");
            }

            if (weight > 10.0)
            {
                errors.Add($"Weight for '{heuristicName}' exceeds maximum 10.0. Got: {weight}");
            }
        }

        // Step 2: Validate thresholds are in range and ordered correctly
        if (config.Thresholds.Low < 0 || config.Thresholds.Low > 100)
        {
            errors.Add($"Low threshold must be 0-100. Got: {config.Thresholds.Low}");
        }

        if (config.Thresholds.High < 0 || config.Thresholds.High > 100)
        {
            errors.Add($"High threshold must be 0-100. Got: {config.Thresholds.High}");
        }

        if (config.Thresholds.Low >= config.Thresholds.High)
        {
            errors.Add(
                $"Low threshold ({config.Thresholds.Low}) must be less than " +
                $"high threshold ({config.Thresholds.High})");
        }

        // Step 3: Validate disabled heuristics exist
        foreach (var disabledName in config.DisabledHeuristics ?? Array.Empty<string>())
        {
            // This will be checked against registered heuristics at runtime
            _logger.LogInformation("Heuristic '{Name}' disabled via configuration", disabledName);
        }

        // Step 4: Check for suspicious configurations
        if (config.Weights.Values.All(w => w < 0.1))
        {
            _logger.LogWarning(
                "Suspicious configuration: all heuristic weights < 0.1. " +
                "This effectively disables heuristic routing.");
        }

        // Step 5: Compute configuration hash for tamper detection
        var configHash = ComputeConfigurationHash(config);
        _logger.LogInformation(
            "Configuration validated. Hash: {Hash}, Enabled: {Enabled}, Weights: {WeightCount}",
            configHash, config.Enabled, config.Weights.Count);

        if (errors.Any())
        {
            return ValidationResult.Failure(
                ErrorCode.ACODE_HEU_004,
                "Configuration validation failed:\n" + string.Join("\n", errors));
        }

        return ValidationResult.Success();
    }

    private string ComputeConfigurationHash(HeuristicConfiguration config)
    {
        // Compute SHA-256 hash of configuration for audit trail
        var json = JsonSerializer.Serialize(config);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
```

**Additional Controls:**
- Use file system permissions to restrict `.agent/config.yml` to read-only for application user
- Implement configuration signing—require cryptographic signature on configuration changes
- Store previous configuration hashes and alert on unexpected changes
- Require configuration changes to go through version control with review

---

### Threat 4: Model ID Spoofing

**Description:** An attacker could register a malicious model in the model catalog with an ID that mimics a trusted model (e.g., `llama3.2:70b-malicious` vs `llama3.2:70b`). If validation is insufficient, users might accidentally override to the malicious model, exposing code to untrusted endpoints.

**Impact:** Code and task descriptions sent to attacker-controlled model endpoints, enabling data exfiltration.

**Mitigation:**

```csharp
namespace AgenticCoder.Infrastructure.Models;

public sealed class ModelCatalog
{
    private readonly Dictionary<string, ModelRegistration> _models = new();
    private readonly ILogger<ModelCatalog> _logger;

    public void RegisterModel(ModelRegistration registration)
    {
        // Step 1: Validate model ID format (strict alphanumeric + version)
        var idRegex = new Regex(@"^[a-z0-9]+(\.[a-z0-9]+)*:[0-9]+(\.[0-9]+)*[a-z]?$");
        if (!idRegex.IsMatch(registration.ModelId))
        {
            throw new ArgumentException(
                $"Model ID '{registration.ModelId}' does not match required format. " +
                "Expected: <name>:<version> (e.g., llama3.2:70b)");
        }

        // Step 2: Check for suspicious model IDs
        var suspiciousPatterns = new[]
        {
            "gpt", "claude", "openai", "anthropic"  // External API vendors
        };

        var containsSuspiciousPattern = suspiciousPatterns.Any(pattern =>
            registration.ModelId.Contains(pattern, StringComparison.OrdinalIgnoreCase));

        if (containsSuspiciousPattern && registration.Deployment == ModelDeployment.Local)
        {
            _logger.LogWarning(
                "Suspicious model registration: {ModelId} claims local deployment " +
                "but has external vendor name. Rejecting registration.",
                registration.ModelId);

            throw new SecurityException(
                $"Model ID '{registration.ModelId}' contains external vendor name " +
                "but claims local deployment. This is not allowed.");
        }

        // Step 3: Validate endpoint URL for local models
        if (registration.Deployment == ModelDeployment.Local)
        {
            var uri = new Uri(registration.Endpoint);
            var isLocalhost = uri.Host == "localhost" ||
                              uri.Host == "127.0.0.1" ||
                              uri.Host.EndsWith(".local");

            if (!isLocalhost)
            {
                _logger.LogWarning(
                    "Local model {ModelId} has non-localhost endpoint: {Endpoint}",
                    registration.ModelId, registration.Endpoint);

                throw new SecurityException(
                    $"Model '{registration.ModelId}' marked as local deployment " +
                    $"but endpoint '{registration.Endpoint}' is not localhost.");
            }
        }

        // Step 4: Audit log registration
        _logger.LogInformation(
            "Model registered: {ModelId}, deployment={Deployment}, endpoint={Endpoint}",
            registration.ModelId, registration.Deployment, registration.Endpoint);

        _models[registration.ModelId] = registration;
    }
}
```

**Additional Controls:**
- Maintain allowlist of trusted model IDs in configuration
- Require administrator approval for new model registrations
- Implement model endpoint certificate pinning for external models
- Periodically audit model catalog for unexpected entries

---

### Threat 5: Logging Sensitive Task Details

**Description:** Heuristic evaluation logs include task descriptions and file paths. If these logs are not properly protected, sensitive information like file names containing secrets (`api_key.txt`), task descriptions mentioning confidential projects, or proprietary algorithm details could be exposed.

**Impact:** Information disclosure through log files, especially if logs are sent to external monitoring systems or stored insecurely.

**Mitigation:**

```csharp
namespace AgenticCoder.Infrastructure.Logging;

public sealed class SensitiveDataRedactor
{
    private static readonly Regex SecretPatterns = new(
        @"(password|token|key|secret|credential|api[-_]?key)[:=\s]\S+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PathPatterns = new(
        @"(/|\\)(home|users|documents|desktop)(\\|/)[^\s]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string RedactTaskDescription(string taskDescription)
    {
        // Step 1: Redact secret-like patterns
        var redacted = SecretPatterns.Replace(
            taskDescription,
            match => match.Groups[1].Value + "=<REDACTED>");

        // Step 2: Redact absolute file paths
        redacted = PathPatterns.Replace(
            redacted,
            match => match.Groups[1].Value + "...<path redacted>");

        // Step 3: Redact any remaining sensitive keywords
        var sensitiveKeywords = new[]
        {
            "password", "secret", "token", "api_key", "private_key"
        };

        foreach (var keyword in sensitiveKeywords)
        {
            // Preserve keyword but redact surrounding context
            var pattern = new Regex($@"\b{keyword}\b\s*[:=]?\s*\S+", RegexOptions.IgnoreCase);
            redacted = pattern.Replace(redacted, $"{keyword}=<REDACTED>");
        }

        return redacted;
    }
}

public sealed class HeuristicLogger
{
    private readonly ILogger _logger;
    private readonly SensitiveDataRedactor _redactor;

    public void LogHeuristicEvaluation(HeuristicContext context, HeuristicResult result)
    {
        // Redact sensitive data before logging
        var redactedDescription = _redactor.RedactTaskDescription(context.TaskDescription);
        var redactedFiles = context.Files.Select(f => RedactFilePath(f)).ToArray();

        _logger.LogInformation(
            "Heuristic evaluation: score={Score}, confidence={Confidence}, " +
            "task={Task}, files={FileCount}",
            result.Score,
            result.Confidence,
            redactedDescription,
            redactedFiles.Length);  // Log count, not actual paths

        // Detailed file paths only in debug mode with explicit opt-in
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "File details: {Files}",
                string.Join(", ", redactedFiles));
        }
    }

    private string RedactFilePath(string path)
    {
        // Keep filename, redact directory structure
        var fileName = Path.GetFileName(path);
        return $".../{fileName}";
    }
}
```

**Additional Controls:**
- Implement log level controls—detailed task info only at Debug level, disabled in production
- Encrypt log files at rest using OS-level encryption
- Restrict log file access to administrator users only
- Implement log retention policies with automatic purging after 30-90 days
- Sanitize logs before export to external monitoring systems

---

## Acceptance Criteria

### Interface

- [ ] AC-001: IRoutingHeuristic in Application
- [ ] AC-002: Evaluate method exists
- [ ] AC-003: Returns HeuristicResult
- [ ] AC-004: Result has score
- [ ] AC-005: Result has confidence
- [ ] AC-006: Result has reasoning
- [ ] AC-007: Name property exists
- [ ] AC-008: Priority property exists

### Engine

- [ ] AC-009: HeuristicEngine in Infrastructure
- [ ] AC-010: Runs all heuristics
- [ ] AC-011: Aggregates results
- [ ] AC-012: Weights by confidence
- [ ] AC-013: Returns combined score
- [ ] AC-014: Logs evaluations

### Score

- [ ] AC-015: Range 0-100
- [ ] AC-016: 0-30 = low
- [ ] AC-017: 31-70 = medium
- [ ] AC-018: 71-100 = high
- [ ] AC-019: Maps to tiers
- [ ] AC-020: Tiers configurable

### Built-in Heuristics

- [ ] AC-021: FileCountHeuristic works
- [ ] AC-022: TaskTypeHeuristic works
- [ ] AC-023: LanguageHeuristic works
- [ ] AC-024: All return valid scores
- [ ] AC-025: All include reasoning

### Precedence

- [ ] AC-026: Request override highest
- [ ] AC-027: Session overrides config
- [ ] AC-028: Config overrides heuristics
- [ ] AC-029: Heuristics lowest
- [ ] AC-030: Documented

### Request Override

- [ ] AC-031: --model flag works
- [ ] AC-032: Validates model ID
- [ ] AC-033: Single request only
- [ ] AC-034: Logged
- [ ] AC-035: Respects constraints

### Session Override

- [ ] AC-036: ACODE_MODEL works
- [ ] AC-037: set-session works
- [ ] AC-038: Persists in session
- [ ] AC-039: Clearable
- [ ] AC-040: Logged

### Config Override

- [ ] AC-041: models.override section
- [ ] AC-042: override.model works
- [ ] AC-043: disable_heuristics works
- [ ] AC-044: Reloadable

### CLI

- [ ] AC-045: heuristics command works
- [ ] AC-046: override command works
- [ ] AC-047: evaluate command works
- [ ] AC-048: Shows all details
- [ ] AC-049: JSON output mode works
- [ ] AC-050: Verbose mode shows reasoning

### Extensibility

- [ ] AC-051: Custom heuristic registrable
- [ ] AC-052: Custom heuristic executes
- [ ] AC-053: Custom heuristic appears in introspection
- [ ] AC-054: Priority ordering works
- [ ] AC-055: DI integration works

### Error Handling

- [ ] AC-056: Invalid model fails fast
- [ ] AC-057: Mode violation fails fast
- [ ] AC-058: Failed heuristic skipped
- [ ] AC-059: All failures use default score
- [ ] AC-060: Error messages actionable

### Configuration

- [ ] AC-061: Config validates on load
- [ ] AC-062: Invalid weights rejected
- [ ] AC-063: Invalid thresholds rejected
- [ ] AC-064: Threshold ordering enforced
- [ ] AC-065: Config reload works

### Security

- [ ] AC-066: Override validation enforces mode constraints
- [ ] AC-067: Model ID format validated
- [ ] AC-068: Security keywords force high scores
- [ ] AC-069: Task descriptions sanitized in logs
- [ ] AC-070: File paths redacted in logs

### Integration

- [ ] AC-071: Works with Task 009 routing
- [ ] AC-072: Works with Task 009.a roles
- [ ] AC-073: Respects Task 001 modes
- [ ] AC-074: Uses Task 004 catalog
- [ ] AC-075: Logs to infrastructure logger

---

## Testing Requirements

### Unit Tests

#### FileCountHeuristicTests.cs

```csharp
namespace AgenticCoder.Application.Tests.Heuristics;

public sealed class FileCountHeuristicTests
{
    private readonly FileCountHeuristic _sut = new();

    [Fact]
    public void Should_Return_Low_Score_For_Single_File()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Fix typo in README",
            Files = new[] { "README.md" }
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeLessThan(30);
        result.Confidence.Should().BeGreaterThan(0.8);
        result.Reasoning.Should().Contain("1 file");
    }

    [Fact]
    public void Should_Return_Low_Score_For_Two_Files()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Update controller and tests",
            Files = new[] { "Controller.cs", "ControllerTests.cs" }
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeLessThan(30);
        result.Confidence.Should().BeGreaterThan(0.7);
        result.Reasoning.Should().Contain("2 files");
    }

    [Fact]
    public void Should_Return_Medium_Score_For_Five_Files()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Refactor authentication module",
            Files = new[]
            {
                "Auth.cs", "AuthService.cs", "AuthController.cs",
                "AuthTests.cs", "AuthIntegrationTests.cs"
            }
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeInRange(31, 70);
        result.Confidence.Should().BeGreaterThan(0.8);
        result.Reasoning.Should().Contain("5 files");
    }

    [Fact]
    public void Should_Return_High_Score_For_Fifteen_Files()
    {
        // Arrange
        var files = Enumerable.Range(1, 15)
            .Select(i => $"File{i}.cs")
            .ToArray();

        var context = new HeuristicContext
        {
            TaskDescription = "Large refactoring",
            Files = files
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeGreaterThan(70);
        result.Confidence.Should().BeGreaterThan(0.9);
        result.Reasoning.Should().Contain("15 files");
    }

    [Fact]
    public void Should_Return_Valid_Score_Range()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Any task",
            Files = new[] { "file.cs" }
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeInRange(0, 100);
        result.Confidence.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void Should_Have_Lower_Confidence_At_Threshold_Boundaries()
    {
        // Arrange - exactly 3 files (low-medium boundary)
        var context = new HeuristicContext
        {
            TaskDescription = "Boundary case",
            Files = new[] { "A.cs", "B.cs", "C.cs" }
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Confidence.Should().BeLessThan(0.9);
        result.Reasoning.Should().Contain("3 files");
    }

    [Fact]
    public void Should_Return_Name_And_Priority()
    {
        // Act & Assert
        _sut.Name.Should().Be("FileCount");
        _sut.Priority.Should().BeGreaterThan(0);
    }
}
```

#### TaskTypeHeuristicTests.cs

```csharp
namespace AgenticCoder.Application.Tests.Heuristics;

public sealed class TaskTypeHeuristicTests
{
    private readonly TaskTypeHeuristic _sut = new();

    [Fact]
    public void Should_Score_Bug_Fix_Lower_Than_Feature()
    {
        // Arrange
        var bugContext = new HeuristicContext
        {
            TaskDescription = "Fix null reference exception in login handler",
            Files = new[] { "LoginHandler.cs" }
        };

        var featureContext = new HeuristicContext
        {
            TaskDescription = "Implement new password reset feature",
            Files = new[] { "PasswordReset.cs" }
        };

        // Act
        var bugResult = _sut.Evaluate(bugContext);
        var featureResult = _sut.Evaluate(featureContext);

        // Assert
        bugResult.Score.Should().BeLessThan(featureResult.Score);
        bugResult.Reasoning.Should().Contain("bug", Exactly.Once());
        featureResult.Reasoning.Should().Contain("feature", Exactly.Once());
    }

    [Fact]
    public void Should_Score_Refactor_Higher_Than_Enhancement()
    {
        // Arrange
        var enhancementContext = new HeuristicContext
        {
            TaskDescription = "Add validation to existing form",
            Files = new[] { "Form.cs" }
        };

        var refactorContext = new HeuristicContext
        {
            TaskDescription = "Refactor authentication to use dependency injection",
            Files = new[] { "Auth.cs" }
        };

        // Act
        var enhancementResult = _sut.Evaluate(enhancementContext);
        var refactorResult = _sut.Evaluate(refactorContext);

        // Assert
        refactorResult.Score.Should().BeGreaterThan(enhancementResult.Score);
        refactorResult.Reasoning.Should().Contain("refactor");
    }

    [Theory]
    [InlineData("Fix typo in comment", 20)]
    [InlineData("Fix crash on startup", 25)]
    [InlineData("Add input validation", 40)]
    [InlineData("Implement OAuth integration", 60)]
    [InlineData("Refactor to clean architecture", 80)]
    public void Should_Assign_Expected_Score_Range_For_Task_Type(
        string taskDescription, int expectedScore)
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = taskDescription,
            Files = new[] { "file.cs" }
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeApproximately(expectedScore, delta: 15);
    }

    [Fact]
    public void Should_Detect_Security_Critical_Task_And_Force_High_Score()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Update authentication token validation logic",
            Files = new[] { "Auth.cs" }
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeGreaterThan(70);
        result.Confidence.Should().Be(1.0);
        result.Reasoning.Should().Contain("security");
    }

    [Theory]
    [InlineData("Implement encryption for user passwords")]
    [InlineData("Fix SQL injection vulnerability")]
    [InlineData("Add CSRF protection")]
    [InlineData("Update authorization rules")]
    public void Should_Force_High_Score_For_All_Security_Keywords(string taskDescription)
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = taskDescription,
            Files = new[] { "Security.cs" }
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeGreaterThan(70);
        result.Confidence.Should().Be(1.0);
    }

    [Fact]
    public void Should_Return_Medium_Score_For_Ambiguous_Task()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Update code",  // Vague description
            Files = new[] { "file.cs" }
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeInRange(40, 60);
        result.Confidence.Should().BeLessThan(0.7);
    }
}
```

#### HeuristicEngineTests.cs

```csharp
namespace AgenticCoder.Infrastructure.Tests.Heuristics;

public sealed class HeuristicEngineTests
{
    [Fact]
    public void Should_Run_All_Registered_Heuristics()
    {
        // Arrange
        var heuristic1 = Substitute.For<IRoutingHeuristic>();
        heuristic1.Name.Returns("Heuristic1");
        heuristic1.Priority.Returns(1);
        heuristic1.Evaluate(Arg.Any<HeuristicContext>())
            .Returns(new HeuristicResult
            {
                Score = 30,
                Confidence = 0.8,
                Reasoning = "Test reason 1"
            });

        var heuristic2 = Substitute.For<IRoutingHeuristic>();
        heuristic2.Name.Returns("Heuristic2");
        heuristic2.Priority.Returns(2);
        heuristic2.Evaluate(Arg.Any<HeuristicContext>())
            .Returns(new HeuristicResult
            {
                Score = 50,
                Confidence = 0.9,
                Reasoning = "Test reason 2"
            });

        var engine = new HeuristicEngine(new[] { heuristic1, heuristic2 });
        var context = new HeuristicContext
        {
            TaskDescription = "Test task",
            Files = new[] { "test.cs" }
        };

        // Act
        var result = engine.Evaluate(context);

        // Assert
        heuristic1.Received(1).Evaluate(Arg.Any<HeuristicContext>());
        heuristic2.Received(1).Evaluate(Arg.Any<HeuristicContext>());
        result.CombinedScore.Should().BeInRange(0, 100);
    }

    [Fact]
    public void Should_Weight_Scores_By_Confidence()
    {
        // Arrange
        var lowConfidenceHeuristic = Substitute.For<IRoutingHeuristic>();
        lowConfidenceHeuristic.Name.Returns("LowConfidence");
        lowConfidenceHeuristic.Priority.Returns(1);
        lowConfidenceHeuristic.Evaluate(Arg.Any<HeuristicContext>())
            .Returns(new HeuristicResult
            {
                Score = 100,
                Confidence = 0.1,  // Low confidence
                Reasoning = "Uncertain"
            });

        var highConfidenceHeuristic = Substitute.For<IRoutingHeuristic>();
        highConfidenceHeuristic.Name.Returns("HighConfidence");
        highConfidenceHeuristic.Priority.Returns(2);
        highConfidenceHeuristic.Evaluate(Arg.Any<HeuristicContext>())
            .Returns(new HeuristicResult
            {
                Score = 20,
                Confidence = 0.9,  // High confidence
                Reasoning = "Very certain"
            });

        var engine = new HeuristicEngine(new[] { lowConfidenceHeuristic, highConfidenceHeuristic });
        var context = new HeuristicContext { TaskDescription = "Test", Files = new[] { "test.cs" } };

        // Act
        var result = engine.Evaluate(context);

        // Assert - high confidence score should dominate
        result.CombinedScore.Should().BeCloseTo(20, precision: 15);
    }

    [Fact]
    public void Should_Handle_Failed_Heuristic_Gracefully()
    {
        // Arrange
        var failingHeuristic = Substitute.For<IRoutingHeuristic>();
        failingHeuristic.Name.Returns("Failing");
        failingHeuristic.Priority.Returns(1);
        failingHeuristic.Evaluate(Arg.Any<HeuristicContext>())
            .Throws(new Exception("Heuristic failed"));

        var workingHeuristic = Substitute.For<IRoutingHeuristic>();
        workingHeuristic.Name.Returns("Working");
        workingHeuristic.Priority.Returns(2);
        workingHeuristic.Evaluate(Arg.Any<HeuristicContext>())
            .Returns(new HeuristicResult
            {
                Score = 50,
                Confidence = 0.8,
                Reasoning = "Works fine"
            });

        var logger = Substitute.For<ILogger<HeuristicEngine>>();
        var engine = new HeuristicEngine(new[] { failingHeuristic, workingHeuristic }, logger);
        var context = new HeuristicContext { TaskDescription = "Test", Files = new[] { "test.cs" } };

        // Act
        var result = engine.Evaluate(context);

        // Assert - should still produce result using working heuristic
        result.CombinedScore.Should().BeInRange(0, 100);
        logger.Received().LogError(
            Arg.Any<Exception>(),
            Arg.Is<string>(s => s.Contains("failed")));
    }

    [Fact]
    public void Should_Return_Default_Score_When_All_Heuristics_Fail()
    {
        // Arrange
        var heuristic = Substitute.For<IRoutingHeuristic>();
        heuristic.Evaluate(Arg.Any<HeuristicContext>())
            .Throws(new Exception("Failed"));

        var logger = Substitute.For<ILogger<HeuristicEngine>>();
        var engine = new HeuristicEngine(new[] { heuristic }, logger);
        var context = new HeuristicContext { TaskDescription = "Test", Files = new[] { "test.cs" } };

        // Act
        var result = engine.Evaluate(context);

        // Assert
        result.CombinedScore.Should().Be(50);  // Default medium score
        logger.Received().LogError(
            Arg.Any<Exception>(),
            Arg.Is<string>(s => s.Contains("All heuristics failed")));
    }

    [Fact]
    public void Should_Execute_Heuristics_In_Priority_Order()
    {
        // Arrange
        var executionOrder = new List<string>();

        var heuristic1 = Substitute.For<IRoutingHeuristic>();
        heuristic1.Name.Returns("Priority3");
        heuristic1.Priority.Returns(3);
        heuristic1.Evaluate(Arg.Do<HeuristicContext>(_ => executionOrder.Add("Priority3")))
            .Returns(new HeuristicResult { Score = 50, Confidence = 0.8, Reasoning = "Test" });

        var heuristic2 = Substitute.For<IRoutingHeuristic>();
        heuristic2.Name.Returns("Priority1");
        heuristic2.Priority.Returns(1);
        heuristic2.Evaluate(Arg.Do<HeuristicContext>(_ => executionOrder.Add("Priority1")))
            .Returns(new HeuristicResult { Score = 50, Confidence = 0.8, Reasoning = "Test" });

        var heuristic3 = Substitute.For<IRoutingHeuristic>();
        heuristic3.Name.Returns("Priority2");
        heuristic3.Priority.Returns(2);
        heuristic3.Evaluate(Arg.Do<HeuristicContext>(_ => executionOrder.Add("Priority2")))
            .Returns(new HeuristicResult { Score = 50, Confidence = 0.8, Reasoning = "Test" });

        var engine = new HeuristicEngine(new[] { heuristic1, heuristic2, heuristic3 });
        var context = new HeuristicContext { TaskDescription = "Test", Files = new[] { "test.cs" } };

        // Act
        engine.Evaluate(context);

        // Assert
        executionOrder.Should().Equal("Priority1", "Priority2", "Priority3");
    }

    [Fact]
    public void Should_Cache_Results_Within_Request_Scope()
    {
        // Arrange
        var heuristic = Substitute.For<IRoutingHeuristic>();
        heuristic.Name.Returns("Cacheable");
        heuristic.Priority.Returns(1);
        heuristic.Evaluate(Arg.Any<HeuristicContext>())
            .Returns(new HeuristicResult { Score = 50, Confidence = 0.8, Reasoning = "Test" });

        var engine = new HeuristicEngine(new[] { heuristic });
        var context = new HeuristicContext { TaskDescription = "Test", Files = new[] { "test.cs" } };

        // Act - evaluate same context twice
        var result1 = engine.Evaluate(context);
        var result2 = engine.Evaluate(context);

        // Assert - heuristic should only be called once (cached)
        heuristic.Received(1).Evaluate(Arg.Any<HeuristicContext>());
        result1.CombinedScore.Should().Be(result2.CombinedScore);
    }
}
```

#### OverrideResolverTests.cs

```csharp
namespace AgenticCoder.Infrastructure.Tests.Heuristics;

public sealed class OverrideResolverTests
{
    private readonly IModelCatalog _catalog = Substitute.For<IModelCatalog>();
    private readonly ILogger<OverrideResolver> _logger = Substitute.For<ILogger<OverrideResolver>>();

    [Fact]
    public void Should_Apply_Request_Override_With_Highest_Precedence()
    {
        // Arrange
        var model = new ModelRegistration
        {
            ModelId = "llama3.2:70b",
            Deployment = ModelDeployment.Local
        };
        _catalog.GetModel("llama3.2:70b").Returns(model);

        var resolver = new OverrideResolver(_catalog, OperatingMode.LocalOnly, _logger);
        var context = new RoutingContext
        {
            RequestOverride = "llama3.2:70b",
            SessionOverride = "llama3.2:7b",
            ConfigOverride = "mistral:7b"
        };

        // Act
        var result = resolver.Resolve(context);

        // Assert
        result.ModelId.Should().Be("llama3.2:70b");
        result.Source.Should().Be(OverrideSource.Request);
    }

    [Fact]
    public void Should_Apply_Session_Override_When_No_Request_Override()
    {
        // Arrange
        var model = new ModelRegistration
        {
            ModelId = "llama3.2:7b",
            Deployment = ModelDeployment.Local
        };
        _catalog.GetModel("llama3.2:7b").Returns(model);

        var resolver = new OverrideResolver(_catalog, OperatingMode.LocalOnly, _logger);
        var context = new RoutingContext
        {
            RequestOverride = null,
            SessionOverride = "llama3.2:7b",
            ConfigOverride = "mistral:7b"
        };

        // Act
        var result = resolver.Resolve(context);

        // Assert
        result.ModelId.Should().Be("llama3.2:7b");
        result.Source.Should().Be(OverrideSource.Session);
    }

    [Fact]
    public void Should_Apply_Config_Override_When_No_Request_Or_Session()
    {
        // Arrange
        var model = new ModelRegistration
        {
            ModelId = "mistral:7b",
            Deployment = ModelDeployment.Local
        };
        _catalog.GetModel("mistral:7b").Returns(model);

        var resolver = new OverrideResolver(_catalog, OperatingMode.LocalOnly, _logger);
        var context = new RoutingContext
        {
            RequestOverride = null,
            SessionOverride = null,
            ConfigOverride = "mistral:7b"
        };

        // Act
        var result = resolver.Resolve(context);

        // Assert
        result.ModelId.Should().Be("mistral:7b");
        result.Source.Should().Be(OverrideSource.Config);
    }

    [Fact]
    public void Should_Return_Null_When_No_Overrides_Present()
    {
        // Arrange
        var resolver = new OverrideResolver(_catalog, OperatingMode.LocalOnly, _logger);
        var context = new RoutingContext
        {
            RequestOverride = null,
            SessionOverride = null,
            ConfigOverride = null
        };

        // Act
        var result = resolver.Resolve(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Should_Reject_Invalid_Model_ID()
    {
        // Arrange
        _catalog.GetModel("invalid-model").Returns((ModelRegistration)null);

        var resolver = new OverrideResolver(_catalog, OperatingMode.LocalOnly, _logger);
        var context = new RoutingContext
        {
            RequestOverride = "invalid-model"
        };

        // Act
        var act = () => resolver.Resolve(context);

        // Assert
        act.Should().Throw<RoutingException>()
            .WithMessage("*not found in catalog*")
            .Which.ErrorCode.Should().Be("ACODE-HEU-001");
    }

    [Fact]
    public void Should_Reject_Model_Incompatible_With_Operating_Mode()
    {
        // Arrange
        var cloudModel = new ModelRegistration
        {
            ModelId = "gpt-4",
            Deployment = ModelDeployment.ExternalAPI
        };
        _catalog.GetModel("gpt-4").Returns(cloudModel);

        var resolver = new OverrideResolver(_catalog, OperatingMode.LocalOnly, _logger);
        var context = new RoutingContext
        {
            RequestOverride = "gpt-4"
        };

        // Act
        var act = () => resolver.Resolve(context);

        // Assert
        act.Should().Throw<RoutingException>()
            .WithMessage("*incompatible*LocalOnly*")
            .Which.ErrorCode.Should().Be("ACODE-HEU-002");
    }

    [Fact]
    public void Should_Log_Override_Application()
    {
        // Arrange
        var model = new ModelRegistration
        {
            ModelId = "llama3.2:70b",
            Deployment = ModelDeployment.Local
        };
        _catalog.GetModel("llama3.2:70b").Returns(model);

        var resolver = new OverrideResolver(_catalog, OperatingMode.LocalOnly, _logger);
        var context = new RoutingContext { RequestOverride = "llama3.2:70b" };

        // Act
        resolver.Resolve(context);

        // Assert
        _logger.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("Override") && s.Contains("llama3.2:70b")));
    }
}
```

### Integration Tests

#### HeuristicRoutingIntegrationTests.cs

```csharp
namespace AgenticCoder.Tests.Integration.Heuristics;

public sealed class HeuristicRoutingIntegrationTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public HeuristicRoutingIntegrationTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Should_Route_Simple_Task_To_Fast_Model()
    {
        // Arrange
        var router = _factory.Services.GetRequiredService<IModelRouter>();
        var context = new RoutingContext
        {
            TaskDescription = "Fix typo in README.md",
            Files = new[] { "README.md" },
            Strategy = RoutingStrategy.Adaptive
        };

        // Act
        var decision = await router.Route(context);

        // Assert
        decision.SelectedModel.Should().Contain("7b");
        decision.Reason.Should().Contain("low complexity");
        decision.HeuristicScore.Should().BeLessThan(30);
    }

    [Fact]
    public async Task Should_Route_Complex_Task_To_Capable_Model()
    {
        // Arrange
        var router = _factory.Services.GetRequiredService<IModelRouter>();
        var context = new RoutingContext
        {
            TaskDescription = "Refactor authentication system to use OAuth2 with PKCE flow",
            Files = Enumerable.Range(1, 12).Select(i => $"Auth{i}.cs").ToArray(),
            Strategy = RoutingStrategy.Adaptive
        };

        // Act
        var decision = await router.Route(context);

        // Assert
        decision.SelectedModel.Should().Contain("70b");
        decision.Reason.Should().Contain("high complexity");
        decision.HeuristicScore.Should().BeGreaterThan(70);
    }

    [Fact]
    public async Task Should_Apply_Request_Override_Over_Heuristics()
    {
        // Arrange
        var router = _factory.Services.GetRequiredService<IModelRouter>();
        var context = new RoutingContext
        {
            TaskDescription = "Simple task",  // Would normally route to small model
            Files = new[] { "file.cs" },
            RequestOverride = "llama3.2:70b",  // Override to large model
            Strategy = RoutingStrategy.Adaptive
        };

        // Act
        var decision = await router.Route(context);

        // Assert
        decision.SelectedModel.Should().Be("llama3.2:70b");
        decision.Reason.Should().Contain("request override");
        decision.OverrideApplied.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Force_High_Complexity_For_Security_Task()
    {
        // Arrange
        var router = _factory.Services.GetRequiredService<IModelRouter>();
        var context = new RoutingContext
        {
            TaskDescription = "Update password encryption algorithm to bcrypt",
            Files = new[] { "Auth.cs" },  // Single file, but security-critical
            Strategy = RoutingStrategy.Adaptive
        };

        // Act
        var decision = await router.Route(context);

        // Assert
        decision.SelectedModel.Should().Contain("70b");
        decision.HeuristicScore.Should().BeGreaterThan(70);
        decision.Reason.Should().Contain("security");
    }

    [Fact]
    public async Task Should_Combine_Role_And_Complexity_Routing()
    {
        // Arrange
        var router = _factory.Services.GetRequiredService<IModelRouter>();
        var context = new RoutingContext
        {
            TaskDescription = "Plan architecture for new microservice",
            Files = Array.Empty<string>(),  // Planning phase, no files yet
            Role = AgentRole.Planner,  // From Task 009.a
            Strategy = RoutingStrategy.Adaptive
        };

        // Act
        var decision = await router.Route(context);

        // Assert
        decision.SelectedModel.Should().Contain("70b");  // Planner + high complexity
        decision.Reason.Should().Contain("planner");
        decision.Reason.Should().Contain("complexity");
    }
}
```

### End-to-End Tests

#### AdaptiveRoutingE2ETests.cs

```csharp
namespace AgenticCoder.Tests.E2E.Heuristics;

public sealed class AdaptiveRoutingE2ETests : IClassFixture<E2ETestFixture>
{
    private readonly E2ETestFixture _fixture;

    public AdaptiveRoutingE2ETests(E2ETestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_Use_Fast_Model_For_Simple_CLI_Request()
    {
        // Arrange
        var cli = _fixture.CreateCLI();

        // Act
        var result = await cli.RunAsync("acode run 'Fix typo in README'");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Using model: llama3.2:7b");
        result.Output.Should().Contain("Complexity: low");
        result.Logs.Should().Contain(log =>
            log.Contains("heuristic_evaluation") && log.Contains("score") && log.Contains("25"));
    }

    [Fact]
    public async Task Should_Use_Large_Model_For_Complex_CLI_Request()
    {
        // Arrange
        var cli = _fixture.CreateCLI();

        // Act
        var result = await cli.RunAsync(
            "acode run 'Refactor entire authentication module to microservices architecture'");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Using model: llama3.2:70b");
        result.Output.Should().Contain("Complexity: high");
        result.Logs.Should().Contain(log =>
            log.Contains("heuristic_evaluation") && log.Contains("score") && log.Contains("8"));
    }

    [Fact]
    public async Task Should_Respect_Model_Override_Flag()
    {
        // Arrange
        var cli = _fixture.CreateCLI();

        // Act
        var result = await cli.RunAsync("acode run --model llama3.2:70b 'Simple task'");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Using model: llama3.2:70b");
        result.Output.Should().Contain("Override: request");
        result.Logs.Should().Contain(log => log.Contains("override_applied"));
    }

    [Fact]
    public async Task Should_Show_Heuristic_Details_Via_CLI()
    {
        // Arrange
        var cli = _fixture.CreateCLI();

        // Act
        var result = await cli.RunAsync("acode routing heuristics");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Heuristics: enabled");
        result.Output.Should().Contain("FileCountHeuristic");
        result.Output.Should().Contain("TaskTypeHeuristic");
        result.Output.Should().Contain("LanguageHeuristic");
        result.Output.Should().Contain("file_count: 1.0");
    }

    [Fact]
    public async Task Should_Evaluate_Task_Without_Executing()
    {
        // Arrange
        var cli = _fixture.CreateCLI();

        // Act
        var result = await cli.RunAsync("acode routing evaluate 'Refactor auth system'");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("FileCountHeuristic:");
        result.Output.Should().Contain("TaskTypeHeuristic:");
        result.Output.Should().Contain("Combined Score:");
        result.Output.Should().Contain("Recommended Model:");
        result.Logs.Should().NotContain(log => log.Contains("inference_started"));
    }
}
```

### Performance Tests

#### HeuristicPerformanceBenchmarks.cs

```csharp
namespace AgenticCoder.Tests.Performance.Heuristics;

[MemoryDiagnoser]
public class HeuristicPerformanceBenchmarks
{
    private HeuristicEngine _engine = null!;
    private HeuristicContext _simpleContext = null!;
    private HeuristicContext _complexContext = null!;

    [GlobalSetup]
    public void Setup()
    {
        var heuristics = new IRoutingHeuristic[]
        {
            new FileCountHeuristic(),
            new TaskTypeHeuristic(),
            new LanguageHeuristic()
        };

        _engine = new HeuristicEngine(heuristics);

        _simpleContext = new HeuristicContext
        {
            TaskDescription = "Fix typo",
            Files = new[] { "README.md" }
        };

        _complexContext = new HeuristicContext
        {
            TaskDescription = "Refactor authentication system to microservices",
            Files = Enumerable.Range(1, 20).Select(i => $"File{i}.cs").ToArray()
        };
    }

    [Benchmark]
    public void FileCountHeuristic_Evaluation()
    {
        var heuristic = new FileCountHeuristic();
        heuristic.Evaluate(_simpleContext);
    }

    [Benchmark]
    public void TaskTypeHeuristic_Evaluation()
    {
        var heuristic = new TaskTypeHeuristic();
        heuristic.Evaluate(_simpleContext);
    }

    [Benchmark]
    public void All_Heuristics_Simple_Task()
    {
        _engine.Evaluate(_simpleContext);
    }

    [Benchmark]
    public void All_Heuristics_Complex_Task()
    {
        _engine.Evaluate(_complexContext);
    }

    [Benchmark]
    public void Override_Resolution()
    {
        var catalog = Substitute.For<IModelCatalog>();
        catalog.GetModel(Arg.Any<string>()).Returns(new ModelRegistration
        {
            ModelId = "test",
            Deployment = ModelDeployment.Local
        });

        var resolver = new OverrideResolver(catalog, OperatingMode.LocalOnly, Substitute.For<ILogger<OverrideResolver>>());
        var context = new RoutingContext { RequestOverride = "test" };

        resolver.Resolve(context);
    }
}

// Performance assertions
[Fact]
public void Heuristic_Evaluation_Should_Complete_Under_50ms()
{
    // Arrange
    var heuristic = new FileCountHeuristic();
    var context = new HeuristicContext
    {
        TaskDescription = "Test",
        Files = Enumerable.Range(1, 100).Select(i => $"File{i}.cs").ToArray()
    };
    var stopwatch = Stopwatch.StartNew();

    // Act
    for (int i = 0; i < 100; i++)
    {
        heuristic.Evaluate(context);
    }
    stopwatch.Stop();

    // Assert
    var avgTime = stopwatch.ElapsedMilliseconds / 100.0;
    avgTime.Should().BeLessThan(50);
}

[Fact]
public void All_Heuristics_Should_Complete_Under_100ms()
{
    // Arrange
    var engine = new HeuristicEngine(new IRoutingHeuristic[]
    {
        new FileCountHeuristic(),
        new TaskTypeHeuristic(),
        new LanguageHeuristic()
    });

    var context = new HeuristicContext
    {
        TaskDescription = "Complex refactoring with security implications",
        Files = Enumerable.Range(1, 50).Select(i => $"File{i}.cs").ToArray()
    };

    var stopwatch = Stopwatch.StartNew();

    // Act
    for (int i = 0; i < 100; i++)
    {
        engine.Evaluate(context);
    }
    stopwatch.Stop();

    // Assert
    var avgTime = stopwatch.ElapsedMilliseconds / 100.0;
    avgTime.Should().BeLessThan(100);
}

[Fact]
public void Override_Lookup_Should_Complete_Under_1ms()
{
    // Arrange
    var catalog = Substitute.For<IModelCatalog>();
    catalog.GetModel(Arg.Any<string>()).Returns(new ModelRegistration
    {
        ModelId = "test",
        Deployment = ModelDeployment.Local
    });

    var resolver = new OverrideResolver(catalog, OperatingMode.LocalOnly, Substitute.For<ILogger<OverrideResolver>>());
    var context = new RoutingContext { RequestOverride = "test" };
    var stopwatch = Stopwatch.StartNew();

    // Act
    for (int i = 0; i < 1000; i++)
    {
        resolver.Resolve(context);
    }
    stopwatch.Stop();

    // Assert
    var avgTime = stopwatch.ElapsedMilliseconds / 1000.0;
    avgTime.Should().BeLessThan(1);
}
```

### Regression Tests

#### HeuristicRegressionTests.cs

```csharp
namespace AgenticCoder.Tests.Regression.Heuristics;

public sealed class HeuristicRegressionTests
{
    [Fact]
    public void Should_Not_Change_Score_For_Known_Simple_Task()
    {
        // Regression test - score should remain stable across refactorings
        var engine = CreateStandardEngine();
        var context = new HeuristicContext
        {
            TaskDescription = "Fix typo in README.md",
            Files = new[] { "README.md" }
        };

        var result = engine.Evaluate(context);

        result.CombinedScore.Should().BeInRange(10, 25);
    }

    [Fact]
    public void Should_Not_Change_Score_For_Known_Complex_Task()
    {
        // Regression test - score should remain stable across refactorings
        var engine = CreateStandardEngine();
        var context = new HeuristicContext
        {
            TaskDescription = "Refactor authentication module to clean architecture with CQRS",
            Files = Enumerable.Range(1, 15).Select(i => $"Auth{i}.cs").ToArray()
        };

        var result = engine.Evaluate(context);

        result.CombinedScore.Should().BeInRange(75, 90);
    }

    [Fact]
    public void Should_Always_Force_High_Score_For_Security_Keywords()
    {
        // Regression test - security tasks must always route conservatively
        var engine = CreateStandardEngine();
        var securityTasks = new[]
        {
            "Update password hashing",
            "Fix SQL injection vulnerability",
            "Implement CSRF protection",
            "Add encryption for sensitive data"
        };

        foreach (var task in securityTasks)
        {
            var context = new HeuristicContext
            {
                TaskDescription = task,
                Files = new[] { "Security.cs" }
            };

            var result = engine.Evaluate(context);

            result.CombinedScore.Should().BeGreaterThan(70,
                because: $"security task '{task}' must route to capable model");
        }
    }

    private static HeuristicEngine CreateStandardEngine()
    {
        return new HeuristicEngine(new IRoutingHeuristic[]
        {
            new FileCountHeuristic(),
            new TaskTypeHeuristic(),
            new LanguageHeuristic()
        });
    }
}
```

---

## User Verification Steps

### Scenario 1: View Heuristic State

1. Run `acode routing heuristics`
2. **Verify:** Output shows "Heuristics: enabled"
3. **Verify:** Lists all registered heuristics (FileCount, TaskType, Language)
4. **Verify:** Shows current weights for each heuristic
5. **Verify:** Displays priority values

**Expected Output:**
```
Heuristics: enabled

Registered Heuristics:
  FileCountHeuristic (priority: 1)
  TaskTypeHeuristic (priority: 2)
  LanguageHeuristic (priority: 3)

Current Weights:
  file_count: 1.0
  task_type: 1.2
  language: 0.8
```

### Scenario 2: Evaluate Simple Task

1. Run `acode routing evaluate "Fix typo in README"`
2. **Verify:** FileCountHeuristic returns low score (< 30)
3. **Verify:** TaskTypeHeuristic detects "fix" as bug/low complexity
4. **Verify:** Combined score is low (< 30)
5. **Verify:** Recommended model is fast/small model (e.g., llama3.2:7b)

**Expected Output:**
```
Evaluating task: "Fix typo in README"

Heuristic Results:
  FileCountHeuristic: 15 (1 file, confidence: 0.9)
  TaskTypeHeuristic: 20 (bug fix, confidence: 0.8)
  LanguageHeuristic: 5 (Markdown, confidence: 1.0)

Combined Score: 16 (Low complexity)
Recommended Tier: low
Recommended Model: llama3.2:7b
```

### Scenario 3: Evaluate Complex Task

1. Run `acode routing evaluate "Refactor authentication to microservices with OAuth2"`
2. **Verify:** TaskTypeHeuristic detects "refactor" as high complexity
3. **Verify:** Keywords "authentication" and "OAuth2" boost score
4. **Verify:** Combined score is high (> 70)
5. **Verify:** Recommended model is capable/large model (e.g., llama3.2:70b)

**Expected Output:**
```
Evaluating task: "Refactor authentication to microservices with OAuth2"

Heuristic Results:
  FileCountHeuristic: 50 (estimated 10+ files, confidence: 0.6)
  TaskTypeHeuristic: 85 (refactor + security keywords, confidence: 1.0)
  LanguageHeuristic: 25 (C#, confidence: 0.9)

Combined Score: 78 (High complexity)
Recommended Tier: high
Recommended Model: llama3.2:70b
```

### Scenario 4: Request Override with CLI Flag

1. Run `acode run --model llama3.2:70b "Simple task"`
2. **Verify:** Output shows "Using model: llama3.2:70b"
3. **Verify:** Output indicates "Override: request"
4. **Verify:** Log shows "override_applied" event with source "request"
5. Run `acode routing override`
6. **Verify:** Shows active request override

### Scenario 5: Session Override via Environment Variable

1. Run `export ACODE_MODEL=llama3.2:70b`
2. Run `acode run "Task 1"`
3. **Verify:** Uses llama3.2:70b
4. Run `acode run "Task 2"`
5. **Verify:** Still uses llama3.2:70b (session persists)
6. Run `unset ACODE_MODEL`
7. Run `acode run "Task 3"`
8. **Verify:** Returns to heuristic-based routing

### Scenario 6: Session Override via CLI Command

1. Run `acode config set-session --model llama3.2:70b`
2. **Verify:** Output confirms "Session model set to llama3.2:70b"
3. Run `acode routing override`
4. **Verify:** Shows session override active
5. Run `acode config clear-session`
6. **Verify:** Output confirms "Session overrides cleared"
7. Run `acode routing override`
8. **Verify:** Shows no session override

### Scenario 7: Override Precedence Order

1. Edit `.agent/config.yml` to set `models.override.model: mistral:7b`
2. Run `export ACODE_MODEL=llama3.2:7b`
3. Run `acode routing override`
4. **Verify:** Shows config override (mistral:7b) and session override (llama3.2:7b)
5. **Verify:** "Effective Model" is llama3.2:7b (session wins over config)
6. Run `acode run --model llama3.2:70b "task"`
7. **Verify:** Uses llama3.2:70b (request wins over session and config)

**Expected Output from `acode routing override`:**
```
Active Overrides:

  Request: (none)
  Session: llama3.2:7b (via ACODE_MODEL)
  Config: mistral:7b

Effective Model: llama3.2:7b (from session)
```

### Scenario 8: Disable Heuristics Completely

1. Edit `.agent/config.yml` and set `models.heuristics.enabled: false`
2. Restart application
3. Run `acode routing heuristics`
4. **Verify:** Shows "Heuristics: disabled"
5. Run `acode run "Complex refactoring task"`
6. **Verify:** Uses default model (not adaptively routed)
7. Check logs for "heuristic_evaluation" events
8. **Verify:** No heuristic evaluation events logged

### Scenario 9: Adjust Heuristic Weights

1. Edit `.agent/config.yml`:
   ```yaml
   models:
     heuristics:
       weights:
         file_count: 1.5
         task_type: 0.5
   ```
2. Restart application
3. Run `acode routing heuristics`
4. **Verify:** Shows updated weights (file_count: 1.5, task_type: 0.5)
5. Run `acode routing evaluate "Refactor 20 files"`
6. **Verify:** Combined score heavily influenced by file count
7. **Verify:** Score is higher than with default weights

### Scenario 10: Security Keyword Detection

1. Run `acode routing evaluate "Update password encryption"`
2. **Verify:** TaskTypeHeuristic detects security keywords
3. **Verify:** Score is forced high (> 70) regardless of other factors
4. **Verify:** Reasoning includes "security-critical"
5. Run `acode run "Update password encryption in Auth.cs"`
6. **Verify:** Routes to large/capable model
7. Check logs
8. **Verify:** Log contains warning about security-critical task detected

---

## Implementation Prompt

You are implementing Task 009.b: Routing Heuristics + Overrides for the Acode project. This task builds upon Task 009 (Model Routing) and Task 009.a (Role-Based Routing) to add intelligent, heuristic-based model selection with user override capabilities.

### Architecture Overview

The heuristic system consists of:
1. **Application Layer** - Interfaces and domain models (`IRoutingHeuristic`, `HeuristicResult`, `HeuristicContext`)
2. **Infrastructure Layer** - Concrete implementations (`HeuristicEngine`, built-in heuristics, `OverrideResolver`)
3. **CLI Commands** - User-facing commands for introspection and debugging (`acode routing heuristics`, `acode routing evaluate`, `acode routing override`)

### Complete File Structure

```
src/AgenticCoder.Application/Heuristics/
├── IRoutingHeuristic.cs           # Core heuristic interface
├── HeuristicResult.cs             # Result with score, confidence, reasoning
├── HeuristicContext.cs            # Context passed to heuristics
└── ComplexityScore.cs             # Aggregated score from all heuristics

src/AgenticCoder.Infrastructure/Heuristics/
├── HeuristicEngine.cs             # Orchestrates heuristic execution
├── FileCountHeuristic.cs          # Built-in: scores by file count
├── TaskTypeHeuristic.cs           # Built-in: scores by task type + security keywords
├── LanguageHeuristic.cs           # Built-in: scores by programming language complexity
├── OverrideResolver.cs            # Resolves overrides in precedence order
├── HeuristicConfiguration.cs      # Configuration model for weights and thresholds
└── SensitiveDataRedactor.cs       # Redacts secrets from logs

src/AgenticCoder.CLI/Commands/Routing/
├── RoutingHeuristicsCommand.cs    # `acode routing heuristics`
├── RoutingEvaluateCommand.cs      # `acode routing evaluate`
└── RoutingOverrideCommand.cs      # `acode routing override`

tests/AgenticCoder.Application.Tests/Heuristics/
├── FileCountHeuristicTests.cs
├── TaskTypeHeuristicTests.cs
└── HeuristicEngineTests.cs

tests/AgenticCoder.Infrastructure.Tests/Heuristics/
├── OverrideResolverTests.cs
└── HeuristicConfigurationValidatorTests.cs
```

---

### Application Layer Implementation

#### IRoutingHeuristic.cs

```csharp
namespace AgenticCoder.Application.Heuristics;

/// <summary>
/// Interface for routing heuristics that estimate task complexity to inform model selection.
/// Heuristics analyze task metadata and return a score (0-100) with confidence level.
/// </summary>
public interface IRoutingHeuristic
{
    /// <summary>
    /// Unique name of the heuristic (e.g., "FileCount", "TaskType").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Execution priority. Lower numbers execute first.
    /// Allows ordering dependencies between heuristics.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Evaluates the heuristic against the provided context.
    /// </summary>
    /// <param name="context">Task metadata for evaluation.</param>
    /// <returns>Result containing score, confidence, and reasoning.</returns>
    HeuristicResult Evaluate(HeuristicContext context);
}
```

#### HeuristicResult.cs

```csharp
namespace AgenticCoder.Application.Heuristics;

/// <summary>
/// Result of a heuristic evaluation containing score, confidence, and human-readable reasoning.
/// </summary>
public sealed class HeuristicResult
{
    /// <summary>
    /// Complexity score from 0 (simple) to 100 (complex).
    /// </summary>
    public required int Score { get; init; }

    /// <summary>
    /// Confidence in this score from 0.0 (uncertain) to 1.0 (certain).
    /// Used for weighted aggregation in HeuristicEngine.
    /// </summary>
    public required double Confidence { get; init; }

    /// <summary>
    /// Human-readable explanation of why this score was assigned.
    /// Logged for debugging and displayed in CLI introspection commands.
    /// </summary>
    public required string Reasoning { get; init; }

    /// <summary>
    /// Validates that score and confidence are in valid ranges.
    /// </summary>
    public void Validate()
    {
        if (Score < 0 || Score > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Score),
                Score,
                "Score must be between 0 and 100");
        }

        if (Confidence < 0.0 || Confidence > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Confidence),
                Confidence,
                "Confidence must be between 0.0 and 1.0");
        }

        if (string.IsNullOrWhiteSpace(Reasoning))
        {
            throw new ArgumentException(
                "Reasoning must not be empty",
                nameof(Reasoning));
        }
    }
}
```

#### HeuristicContext.cs

```csharp
namespace AgenticCoder.Application.Heuristics;

/// <summary>
/// Context containing task metadata provided to heuristics for evaluation.
/// Extends RoutingContext from Task 009 with heuristic-specific data.
/// </summary>
public sealed class HeuristicContext
{
    /// <summary>
    /// User-provided task description.
    /// Used by TaskTypeHeuristic for keyword analysis.
    /// </summary>
    public required string TaskDescription { get; init; }

    /// <summary>
    /// List of file paths affected by this task.
    /// Used by FileCountHeuristic and LanguageHeuristic.
    /// </summary>
    public required string[] Files { get; init; }

    /// <summary>
    /// Optional agent role from Task 009.a (planner, executor, verifier, reviewer).
    /// Can influence combined routing decisions.
    /// </summary>
    public AgentRole? Role { get; init; }

    /// <summary>
    /// Optional metadata dictionary for custom heuristics.
    /// Enables extensibility without modifying core context.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
```

#### ComplexityScore.cs

```csharp
namespace AgenticCoder.Application.Heuristics;

/// <summary>
/// Aggregated complexity score from all heuristic evaluations.
/// Contains the weighted combined score and individual heuristic results.
/// </summary>
public sealed class ComplexityScore
{
    /// <summary>
    /// Combined weighted score (0-100) from all heuristics.
    /// </summary>
    public int CombinedScore { get; }

    /// <summary>
    /// Complexity tier based on configured thresholds.
    /// </summary>
    public ComplexityTier Tier { get; }

    /// <summary>
    /// Individual results from each heuristic for debugging.
    /// </summary>
    public IReadOnlyList<(string Name, HeuristicResult Result)> IndividualResults { get; }

    public ComplexityScore(
        int combinedScore,
        IReadOnlyList<(string Name, HeuristicResult Result)> individualResults,
        int lowThreshold = 30,
        int highThreshold = 70)
    {
        CombinedScore = Math.Clamp(combinedScore, 0, 100);
        IndividualResults = individualResults;

        Tier = CombinedScore switch
        {
            <= var low when low == lowThreshold => ComplexityTier.Low,
            >= var high when high == highThreshold => ComplexityTier.High,
            _ => ComplexityTier.Medium
        };
    }
}

/// <summary>
/// Complexity tier mapped to model selection tiers.
/// </summary>
public enum ComplexityTier
{
    Low,     // Route to fast/small models
    Medium,  // Route to default models
    High     // Route to capable/large models
}
```

---

### Infrastructure Layer Implementation

#### HeuristicEngine.cs

```csharp
namespace AgenticCoder.Infrastructure.Heuristics;

/// <summary>
/// Orchestrates execution of all registered heuristics and aggregates results.
/// Implements weighted averaging based on confidence levels.
/// </summary>
public sealed class HeuristicEngine
{
    private readonly IEnumerable<IRoutingHeuristic> _heuristics;
    private readonly HeuristicConfiguration _config;
    private readonly ILogger<HeuristicEngine> _logger;
    private readonly Dictionary<HeuristicContext, ComplexityScore> _cache = new();

    public HeuristicEngine(
        IEnumerable<IRoutingHeuristic> heuristics,
        HeuristicConfiguration config,
        ILogger<HeuristicEngine> logger)
    {
        _heuristics = heuristics;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates all heuristics against the context and returns aggregated score.
    /// Results are cached per request scope.
    /// </summary>
    public ComplexityScore Evaluate(HeuristicContext context)
    {
        // Check cache first
        if (_cache.TryGetValue(context, out var cached))
        {
            _logger.LogDebug("Returning cached heuristic result for task: {Task}",
                context.TaskDescription);
            return cached;
        }

        if (!_config.Enabled)
        {
            _logger.LogInformation("Heuristics disabled. Returning default medium score.");
            return CreateDefaultScore();
        }

        var stopwatch = Stopwatch.StartNew();
        var results = new List<(string Name, HeuristicResult Result)>();

        // Execute heuristics in priority order
        foreach (var heuristic in _heuristics.OrderBy(h => h.Priority))
        {
            // Skip disabled heuristics
            if (_config.DisabledHeuristics?.Contains(heuristic.Name) == true)
            {
                _logger.LogDebug("Skipping disabled heuristic: {Name}", heuristic.Name);
                continue;
            }

            try
            {
                var heuristicStopwatch = Stopwatch.StartNew();
                var result = heuristic.Evaluate(context);
                heuristicStopwatch.Stop();

                result.Validate();

                // Apply configured weight
                var weight = _config.Weights.GetValueOrDefault(
                    heuristic.Name.ToLowerInvariant(),
                    1.0);

                var weightedScore = (int)(result.Score * weight);
                var weightedResult = result with { Score = weightedScore };

                results.Add((heuristic.Name, weightedResult));

                _logger.LogDebug(
                    "Heuristic {Name} evaluated: score={Score} (weighted: {Weighted}), " +
                    "confidence={Confidence}, duration={Duration}ms",
                    heuristic.Name,
                    result.Score,
                    weightedScore,
                    result.Confidence,
                    heuristicStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Heuristic {Name} failed during evaluation. Skipping.",
                    heuristic.Name);
                // Continue with remaining heuristics
            }
        }

        stopwatch.Stop();

        // If all heuristics failed, return default score
        if (results.Count == 0)
        {
            _logger.LogError(
                "All heuristics failed for task: {Task}. Falling back to default score.",
                context.TaskDescription);
            return CreateDefaultScore();
        }

        // Weighted aggregation
        var weightedSum = results.Sum(r => r.Result.Score * r.Result.Confidence);
        var totalWeight = results.Sum(r => r.Result.Confidence);
        var combinedScore = (int)(weightedSum / totalWeight);

        var complexityScore = new ComplexityScore(
            combinedScore,
            results,
            _config.Thresholds.Low,
            _config.Thresholds.High);

        // Cache result
        _cache[context] = complexityScore;

        _logger.LogInformation(
            "Heuristic evaluation complete: combined_score={Score}, tier={Tier}, " +
            "heuristic_count={Count}, duration={Duration}ms",
            complexityScore.CombinedScore,
            complexityScore.Tier,
            results.Count,
            stopwatch.ElapsedMilliseconds);

        return complexityScore;
    }

    private ComplexityScore CreateDefaultScore()
    {
        return new ComplexityScore(
            50,  // Medium score
            Array.Empty<(string, HeuristicResult)>(),
            _config.Thresholds.Low,
            _config.Thresholds.High);
    }

    /// <summary>
    /// Clears the result cache. Called at request boundary.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }
}
```

#### FileCountHeuristic.cs

```csharp
namespace AgenticCoder.Infrastructure.Heuristics;

/// <summary>
/// Heuristic that scores complexity based on number of files affected.
/// Fewer files = simpler task, more files = more complex task.
/// </summary>
public sealed class FileCountHeuristic : IRoutingHeuristic
{
    public string Name => "FileCount";
    public int Priority => 1;  // Run first - objective metric

    public HeuristicResult Evaluate(HeuristicContext context)
    {
        var fileCount = context.Files.Length;

        // Score assignment
        var (score, confidence) = fileCount switch
        {
            0 => (0, 0.5),              // No files - uncertain
            1 => (10, 0.9),             // Single file - very simple
            2 => (20, 0.85),            // Two files - simple
            <= 5 => (35, 0.8),          // Few files - medium-low
            <= 10 => (55, 0.85),        // Several files - medium
            <= 20 => (75, 0.9),         // Many files - complex
            _ => (90, 0.95)             // Very many files - very complex
        };

        // Lower confidence at threshold boundaries
        if (fileCount == 3 || fileCount == 10)
        {
            confidence -= 0.2;
        }

        var reasoning = fileCount switch
        {
            0 => "No files specified",
            1 => $"Single file: {context.Files[0]}",
            <= 5 => $"{fileCount} files - limited scope",
            <= 10 => $"{fileCount} files - moderate scope",
            _ => $"{fileCount} files - large scope"
        };

        return new HeuristicResult
        {
            Score = score,
            Confidence = confidence,
            Reasoning = reasoning
        };
    }
}
```

#### TaskTypeHeuristic.cs

```csharp
namespace AgenticCoder.Infrastructure.Heuristics;

/// <summary>
/// Heuristic that scores complexity based on task type keywords.
/// Also detects security-critical keywords and forces conservative routing.
/// </summary>
public sealed class TaskTypeHeuristic : IRoutingHeuristic
{
    private readonly ISanitizer _sanitizer;
    private readonly ILogger<TaskTypeHeuristic> _logger;

    public string Name => "TaskType";
    public int Priority => 2;  // Run after FileCount

    // Security-critical keywords that force high complexity
    private static readonly HashSet<string> SecurityKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "authentication", "authorization", "security", "crypto", "encryption",
        "password", "token", "credentials", "permission", "access control",
        "sanitize", "validate", "xss", "sql injection", "csrf", "oauth",
        "saml", "jwt", "session", "cookie", "cors"
    };

    // Task type keywords
    private static readonly Dictionary<TaskType, string[]> TaskKeywords = new()
    {
        [TaskType.Bug] = new[] { "fix", "bug", "issue", "crash", "error", "typo" },
        [TaskType.Enhancement] = new[] { "add", "enhance", "improve", "update", "upgrade" },
        [TaskType.Feature] = new[] { "implement", "new feature", "create", "develop" },
        [TaskType.Refactor] = new[] { "refactor", "restructure", "redesign", "migrate" }
    };

    public TaskTypeHeuristic(ISanitizer sanitizer, ILogger<TaskTypeHeuristic> logger)
    {
        _sanitizer = sanitizer;
        _logger = logger;
    }

    public HeuristicResult Evaluate(HeuristicContext context)
    {
        var sanitized = _sanitizer.SanitizeTaskDescription(context.TaskDescription);

        // Check for security keywords first
        var containsSecurityKeyword = SecurityKeywords.Any(keyword =>
            sanitized.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (containsSecurityKeyword)
        {
            _logger.LogWarning(
                "Security-critical task detected: {Task}. Forcing high complexity.",
                sanitized);

            return new HeuristicResult
            {
                Score = 85,
                Confidence = 1.0,
                Reasoning = "Security-critical task detected. Conservative routing enforced."
            };
        }

        // Detect task type from keywords
        var taskType = DetectTaskType(sanitized);
        var (score, confidence) = GetScoreForTaskType(taskType, sanitized);

        var reasoning = $"Detected task type: {taskType}";

        return new HeuristicResult
        {
            Score = score,
            Confidence = confidence,
            Reasoning = reasoning
        };
    }

    private TaskType DetectTaskType(string description)
    {
        foreach (var (type, keywords) in TaskKeywords)
        {
            if (keywords.Any(keyword =>
                description.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return type;
            }
        }

        return TaskType.Unknown;
    }

    private (int Score, double Confidence) GetScoreForTaskType(TaskType type, string description)
    {
        return type switch
        {
            TaskType.Bug => (20, 0.8),
            TaskType.Enhancement => (40, 0.75),
            TaskType.Feature => (60, 0.8),
            TaskType.Refactor => (80, 0.85),
            TaskType.Unknown => (50, 0.5)  // Conservative default
        };
    }
}

public enum TaskType
{
    Unknown,
    Bug,
    Enhancement,
    Feature,
    Refactor
}
```

#### LanguageHeuristic.cs

```csharp
namespace AgenticCoder.Infrastructure.Heuristics;

/// <summary>
/// Heuristic that scores complexity based on programming language.
/// More complex languages (C++, Rust) score higher than simple ones (Markdown, JSON).
/// </summary>
public sealed class LanguageHeuristic : IRoutingHeuristic
{
    public string Name => "Language";
    public int Priority => 3;

    private static readonly Dictionary<string, int> LanguageComplexity = new(StringComparer.OrdinalIgnoreCase)
    {
        // Simple formats
        [".md"] = 5,
        [".txt"] = 5,
        [".json"] = 10,
        [".yml"] = 10,
        [".yaml"] = 10,
        [".xml"] = 10,

        // Scripting languages
        [".sh"] = 15,
        [".bash"] = 15,
        [".ps1"] = 20,

        // Standard languages
        [".js"] = 25,
        [".ts"] = 30,
        [".py"] = 25,
        [".rb"] = 25,
        [".go"] = 30,
        [".java"] = 35,
        [".cs"] = 35,

        // Complex languages
        [".cpp"] = 40,
        [".cc"] = 40,
        [".c"] = 35,
        [".rs"] = 45,
        [".hs"] = 45
    };

    public HeuristicResult Evaluate(HeuristicContext context)
    {
        if (context.Files.Length == 0)
        {
            return new HeuristicResult
            {
                Score = 0,
                Confidence = 0.0,
                Reasoning = "No files to analyze"
            };
        }

        var scores = context.Files
            .Select(Path.GetExtension)
            .Where(ext => !string.IsNullOrEmpty(ext))
            .Select(ext => LanguageComplexity.GetValueOrDefault(ext, 25))
            .ToList();

        if (scores.Count == 0)
        {
            return new HeuristicResult
            {
                Score = 25,  // Default score
                Confidence = 0.3,
                Reasoning = "Unable to detect file languages"
            };
        }

        var avgScore = (int)scores.Average();
        var languages = context.Files
            .Select(Path.GetExtension)
            .Where(ext => !string.IsNullOrEmpty(ext))
            .Distinct()
            .ToList();

        var reasoning = languages.Count == 1
            ? $"Single language: {languages[0]}"
            : $"Multiple languages: {string.Join(", ", languages)}";

        return new HeuristicResult
        {
            Score = avgScore,
            Confidence = 0.9,
            Reasoning = reasoning
        };
    }
}
```

#### OverrideResolver.cs

```csharp
namespace AgenticCoder.Infrastructure.Heuristics;

/// <summary>
/// Resolves model overrides in precedence order: request > session > config.
/// Validates overrides against operating mode constraints.
/// </summary>
public sealed class OverrideResolver
{
    private readonly IModelCatalog _catalog;
    private readonly OperatingMode _currentMode;
    private readonly ILogger<OverrideResolver> _logger;

    public OverrideResolver(
        IModelCatalog catalog,
        OperatingMode currentMode,
        ILogger<OverrideResolver> logger)
    {
        _catalog = catalog;
        _currentMode = currentMode;
        _logger = logger;
    }

    /// <summary>
    /// Resolves override from context, checking in precedence order.
    /// Returns null if no overrides are present.
    /// </summary>
    public OverrideResult? Resolve(RoutingContext context)
    {
        // Check request override (highest precedence)
        if (!string.IsNullOrEmpty(context.RequestOverride))
        {
            return ValidateAndReturn(
                context.RequestOverride,
                OverrideSource.Request);
        }

        // Check session override
        if (!string.IsNullOrEmpty(context.SessionOverride))
        {
            return ValidateAndReturn(
                context.SessionOverride,
                OverrideSource.Session);
        }

        // Check config override (lowest precedence)
        if (!string.IsNullOrEmpty(context.ConfigOverride))
        {
            return ValidateAndReturn(
                context.ConfigOverride,
                OverrideSource.Config);
        }

        // No overrides present
        return null;
    }

    private OverrideResult ValidateAndReturn(string modelId, OverrideSource source)
    {
        // Validate model exists
        var model = _catalog.GetModel(modelId);
        if (model is null)
        {
            _logger.LogWarning(
                "Override validation failed: model {ModelId} not found. Source: {Source}",
                modelId, source);

            throw new RoutingException(
                ErrorCode.ACODE_HEU_001,
                $"Model '{modelId}' not found in catalog. " +
                $"Available models: {string.Join(", ", _catalog.GetAllModelIds())}");
        }

        // Validate mode compatibility
        var isCompatible = _currentMode switch
        {
            OperatingMode.LocalOnly => model.Deployment == ModelDeployment.Local,
            OperatingMode.Burst => model.Deployment != ModelDeployment.ExternalAPI,
            OperatingMode.AirGapped => model.Deployment == ModelDeployment.Local,
            _ => false
        };

        if (!isCompatible)
        {
            _logger.LogWarning(
                "Override validation failed: model {ModelId} incompatible with mode {Mode}. Source: {Source}",
                modelId, _currentMode, source);

            throw new RoutingException(
                ErrorCode.ACODE_HEU_002,
                $"Model '{modelId}' requires {model.Deployment} deployment " +
                $"but current mode is {_currentMode}. " +
                $"Available models for {_currentMode}: " +
                $"{string.Join(", ", _catalog.GetModelsForMode(_currentMode))}");
        }

        _logger.LogInformation(
            "Override applied: model={ModelId}, source={Source}, mode={Mode}",
            modelId, source, _currentMode);

        return new OverrideResult
        {
            ModelId = modelId,
            Source = source,
            Model = model
        };
    }
}

public sealed class OverrideResult
{
    public required string ModelId { get; init; }
    public required OverrideSource Source { get; init; }
    public required ModelRegistration Model { get; init; }
}

public enum OverrideSource
{
    Request,
    Session,
    Config
}
```

#### HeuristicConfiguration.cs

```csharp
namespace AgenticCoder.Infrastructure.Heuristics;

/// <summary>
/// Configuration for heuristic system loaded from .agent/config.yml
/// </summary>
public sealed class HeuristicConfiguration
{
    public bool Enabled { get; set; } = true;

    public Dictionary<string, double> Weights { get; set; } = new()
    {
        ["file_count"] = 1.0,
        ["task_type"] = 1.0,
        ["language"] = 1.0
    };

    public ThresholdConfiguration Thresholds { get; set; } = new();

    public string[]? DisabledHeuristics { get; set; }
}

public sealed class ThresholdConfiguration
{
    public int Low { get; set; } = 30;   // Below this = low complexity
    public int High { get; set; } = 70;  // Above this = high complexity
}
```

### Error Codes

Define these in your error code registry (from Task 000/002):

| Code | Message | HTTP Status |
|------|---------|-------------|
| ACODE-HEU-001 | Invalid model ID in override | 400 Bad Request |
| ACODE-HEU-002 | Override violates operating mode constraint | 403 Forbidden |
| ACODE-HEU-003 | Heuristic evaluation failed | 500 Internal Server Error |
| ACODE-HEU-004 | Invalid heuristic configuration | 400 Bad Request |

### Logging Schema

All heuristic evaluations must log structured events:

```json
{
  "event": "heuristic_evaluation",
  "timestamp": "2026-01-04T12:34:56Z",
  "task_description": "Add validation to user input",
  "file_count": 3,
  "heuristics": [
    {
      "name": "FileCountHeuristic",
      "score": 35,
      "weighted_score": 35,
      "confidence": 0.8,
      "reasoning": "3 files - limited scope",
      "duration_ms": 2
    },
    {
      "name": "TaskTypeHeuristic",
      "score": 40,
      "weighted_score": 48,
      "confidence": 0.75,
      "reasoning": "Detected task type: Enhancement",
      "duration_ms": 5
    },
    {
      "name": "LanguageHeuristic",
      "score": 30,
      "weighted_score": 24,
      "confidence": 0.9,
      "reasoning": "Single language: .cs",
      "duration_ms": 1
    }
  ],
  "combined_score": 38,
  "complexity_tier": "medium",
  "total_duration_ms": 12,
  "override_active": false
}
```

### Implementation Checklist

Follow this sequence:

1. [ ] **Application Layer - Domain Models**
   - [ ] Create `IRoutingHeuristic.cs` interface
   - [ ] Create `HeuristicResult.cs` with validation
   - [ ] Create `HeuristicContext.cs`
   - [ ] Create `ComplexityScore.cs` with tier mapping
   - [ ] Write unit tests for domain models

2. [ ] **Infrastructure - Heuristic Engine**
   - [ ] Create `HeuristicEngine.cs` with weighted aggregation
   - [ ] Implement caching logic
   - [ ] Implement error handling (graceful degradation)
   - [ ] Write unit tests for engine

3. [ ] **Infrastructure - Built-in Heuristics**
   - [ ] Implement `FileCountHeuristic.cs`
   - [ ] Implement `TaskTypeHeuristic.cs` with security keyword detection
   - [ ] Implement `LanguageHeuristic.cs`
   - [ ] Write comprehensive unit tests for each heuristic

4. [ ] **Infrastructure - Override System**
   - [ ] Implement `OverrideResolver.cs` with precedence logic
   - [ ] Implement validation against operating mode constraints
   - [ ] Write unit tests for override resolution

5. [ ] **Configuration**
   - [ ] Create `HeuristicConfiguration.cs` model
   - [ ] Implement configuration validation
   - [ ] Add schema to `.agent/config.yml` template
   - [ ] Write configuration validation tests

6. [ ] **Integration with Task 009 Routing**
   - [ ] Extend `RoutingContext` with `HeuristicContext`
   - [ ] Integrate `HeuristicEngine` into `RoutingPolicy`
   - [ ] Integrate `OverrideResolver` into router
   - [ ] Write integration tests for end-to-end routing

7. [ ] **CLI Commands**
   - [ ] Implement `acode routing heuristics` command
   - [ ] Implement `acode routing evaluate` command
   - [ ] Implement `acode routing override` command
   - [ ] Write E2E tests for CLI commands

8. [ ] **Security**
   - [ ] Implement `SensitiveDataRedactor` for logs
   - [ ] Add security keyword detection to `TaskTypeHeuristic`
   - [ ] Write security tests (override injection, etc.)

9. [ ] **Documentation**
   - [ ] Add XML documentation to all public APIs
   - [ ] Update configuration reference docs
   - [ ] Add usage examples to user manual

10. [ ] **Testing**
    - [ ] Write unit tests (90%+ coverage)
    - [ ] Write integration tests
    - [ ] Write E2E tests
    - [ ] Write performance tests (< 50ms per heuristic)
    - [ ] Write regression tests

### Verification Commands

```bash
# Run all heuristic tests
dotnet test --filter "FullyQualifiedName~Heuristics"

# Run performance benchmarks
dotnet run --project tests/Performance.Benchmarks -c Release -- --filter *Heuristic*

# Verify configuration validation
dotnet test --filter "HeuristicConfigurationValidatorTests"

# E2E verification
acode routing heuristics
acode routing evaluate "Fix typo in README"
acode routing evaluate "Refactor authentication with OAuth2"
```

### Integration Points with Other Tasks

- **Task 009** - Extend `RoutingPolicy` to consult `HeuristicEngine` when strategy is `Adaptive`
- **Task 009.a** - Combine role-based and heuristic-based routing in two-dimensional matrix
- **Task 001** - Validate overrides against `OperatingMode` constraints
- **Task 004** - Use `IModelCatalog` for model ID validation in override resolution
- **Task 010** - Implement CLI commands using CLI framework
- **Task 002** - Use configuration system for `HeuristicConfiguration`

### Success Criteria

Task 009.b is complete when:

- [ ] All 100 Functional Requirements are implemented
- [ ] All 30 Non-Functional Requirements are met (especially < 100ms heuristic evaluation)
- [ ] All 75 Acceptance Criteria pass
- [ ] All tests pass (unit, integration, E2E, performance, regression)
- [ ] Code coverage > 90%
- [ ] All public APIs have XML documentation
- [ ] CLI commands work as specified in User Verification scenarios
- [ ] Security mitigations implemented for all 5 identified threats
- [ ] Integration with Task 009 and Task 009.a verified

---

**End of Task 009.b Specification**