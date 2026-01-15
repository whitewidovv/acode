# Task-009b Completion Checklist: Routing Heuristics & Overrides

**Status:** 60% Complete (45/75 ACs verified)

**Objective:** Achieve 100% Acceptance Criteria compliance through systematic gap closure

**Methodology:** Test-Driven Development (RED → GREEN → REFACTOR)

---

## Instructions for Implementation

### Phase System

This checklist is organized into 5 sequential phases. **Each phase depends on previous phases being complete.**

1. **Phase 1: CLI Commands (CRITICAL)** - Implement user-facing interface
   - 3 CLI command classes
   - Modify main RoutingCommand to register subcommands
   - ~2.5 hours

2. **Phase 2: Security & Utilities (CRITICAL)** - Implement missing components
   - SensitiveDataRedactor service
   - DI registration
   - ~1 hour

3. **Phase 3: Integration Tests (CRITICAL)** - Verify component interactions
   - HeuristicRoutingIntegrationTests (~5 tests)
   - Test heuristics with IModelRouter
   - ~1 hour

4. **Phase 4: E2E & Performance Tests (HIGH)** - Comprehensive coverage
   - AdaptiveRoutingE2ETests (~5 tests)
   - HeuristicPerformanceBenchmarks
   - ~1.5 hours

5. **Phase 5: Regression & Verification (MEDIUM)** - Stability checks
   - HeuristicRegressionTests
   - Final audit
   - ~30 minutes

### How to Use This Checklist

- Mark items with **[🔄]** when starting
- Mark items with **[✅]** when complete with evidence
- Evidence = test output, code file path, or verification command
- **Do not proceed to next phase until current phase is 100% complete**
- Run `dotnet build` after each phase to catch compilation errors
- Run `dotnet test` after each phase to verify tests pass

---

## PHASE 1: CLI COMMANDS (CRITICAL)

**Duration:** ~2.5 hours
**Dependency:** None (can start immediately)
**Blocking:** Phases 3-5 cannot start until this phase complete

### 1.1 Create RoutingHeuristicsCommand.cs

**File:** `src/Acode.CLI/Commands/Routing/RoutingHeuristicsCommand.cs`

**Specification Reference:** Implementation Prompt lines 3040-3065

**What to implement:**
- [🔄] Create public class RoutingHeuristicsCommand : Command
- [🔄] Add constructor: `public RoutingHeuristicsCommand() : base("heuristics", "Display heuristic evaluation state and configuration")`
- [🔄] Add `--verbose` option with Option<bool>
- [🔄] Implement `override async Task<int> InvokeAsync(InvocationContext context)`
- [🔄] In InvokeAsync:
  - Get all registered IRoutingHeuristic from IServiceProvider (DI)
  - Output "Heuristics: enabled" or "Heuristics: disabled" based on config
  - List each heuristic with Name, Priority
  - Show current weights from HeuristicConfiguration
  - If verbose: show description of what each heuristic evaluates
  - Return 0 on success

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

**Code Template:**
```csharp
namespace AgenticCoder.CLI.Commands.Routing;

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using AgenticCoder.Application.Heuristics;
using AgenticCoder.Infrastructure.Configuration;

public sealed class RoutingHeuristicsCommand : Command
{
    private readonly IServiceProvider _serviceProvider;

    public RoutingHeuristicsCommand(IServiceProvider serviceProvider)
        : base("heuristics", "Display heuristic evaluation state and configuration")
    {
        _serviceProvider = serviceProvider;

        var verboseOption = new Option<bool>(
            new[] { "-v", "--verbose" },
            "Show reasoning for each heuristic");
        AddOption(verboseOption);
    }

    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        var verbose = context.ParseResult.GetValueForOption<bool>("--verbose");

        var heuristics = _serviceProvider.GetServices<IRoutingHeuristic>().ToList();
        var config = _serviceProvider.GetRequiredService<HeuristicConfiguration>();

        // Implement output logic here
        return 0;
    }
}
```

**Success Criteria:**
- [ ] File compiles without errors
- [ ] Command registered in RoutingCommand subcommands
- [ ] Manual test: `acode routing heuristics` runs without error
- [ ] Output shows "Heuristics: enabled" and lists all three heuristics

### 1.2 Create RoutingEvaluateCommand.cs

**File:** `src/Acode.CLI/Commands/Routing/RoutingEvaluateCommand.cs`

**Specification Reference:** Implementation Prompt lines 3070-3095

**What to implement:**
- [🔄] Create public class RoutingEvaluateCommand : Command
- [🔄] Add constructor with `<task>` argument
- [🔄] Add `--json` option for JSON output
- [🔄] Add `--verbose` option for reasoning
- [🔄] Implement `override async Task<int> InvokeAsync(InvocationContext context)`
- [🔄] In InvokeAsync:
  - Extract task description from argument
  - Build file list from working directory
  - Create HeuristicContext with task + files
  - Call IHeuristicEngine.Evaluate(context)
  - Display each heuristic's score, confidence, reasoning
  - Show combined score and recommended model tier
  - If --json: output as structured JSON
  - If --verbose: include full reasoning
  - Return 0 on success

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

**Code Template:**
```csharp
namespace AgenticCoder.CLI.Commands.Routing;

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using AgenticCoder.Application.Heuristics;
using AgenticCoder.Infrastructure.Heuristics;
using AgenticCoder.Domain.Routing;

public sealed class RoutingEvaluateCommand : Command
{
    private readonly IServiceProvider _serviceProvider;

    public RoutingEvaluateCommand(IServiceProvider serviceProvider)
        : base("evaluate", "Evaluate task complexity without executing")
    {
        _serviceProvider = serviceProvider;

        var taskArg = new Argument<string>("task", "Task description to evaluate");
        var jsonOption = new Option<bool>(new[] { "-j", "--json" }, "Output as JSON");
        var verboseOption = new Option<bool>(new[] { "-v", "--verbose" }, "Show reasoning");

        AddArgument(taskArg);
        AddOption(jsonOption);
        AddOption(verboseOption);
    }

    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        var taskDescription = context.ParseResult.GetValueForArgument<string>("task");
        var json = context.ParseResult.GetValueForOption<bool>("--json");
        var verbose = context.ParseResult.GetValueForOption<bool>("--verbose");

        // Implement evaluation logic here
        return 0;
    }
}
```

**Success Criteria:**
- [ ] File compiles without errors
- [ ] Command registered in RoutingCommand subcommands
- [ ] Manual test: `acode routing evaluate "Fix typo"` produces output with scores
- [ ] Manual test: `acode routing evaluate "Fix typo" --json` produces valid JSON

### 1.3 Create RoutingOverrideCommand.cs

**File:** `src/Acode.CLI/Commands/Routing/RoutingOverrideCommand.cs`

**Specification Reference:** Implementation Prompt lines 3100-3130

**What to implement:**
- [🔄] Create public class RoutingOverrideCommand : Command
- [🔄] Add constructor: `public RoutingOverrideCommand() : base("override", "Display active model overrides and precedence")`
- [🔄] Implement `override async Task<int> InvokeAsync(InvocationContext context)`
- [🔄] In InvokeAsync:
  - Get request override (may be null)
  - Get session override from SessionOverrideStore
  - Get config override from HeuristicConfiguration
  - Display all three in precedence order
  - Show "Effective Model" (which override wins)
  - Return 0 on success

**Expected Output:**
```
Active Overrides:

  Request: (none)
  Session: llama3.2:7b (via ACODE_MODEL)
  Config: mistral:7b

Effective Model: llama3.2:7b (from session)
```

**Code Template:**
```csharp
namespace AgenticCoder.CLI.Commands.Routing;

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using AgenticCoder.Infrastructure.Configuration;

public sealed class RoutingOverrideCommand : Command
{
    private readonly IServiceProvider _serviceProvider;

    public RoutingOverrideCommand(IServiceProvider serviceProvider)
        : base("override", "Display active model overrides and precedence")
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        // Implement override display logic here
        return 0;
    }
}
```

**Success Criteria:**
- [ ] File compiles without errors
- [ ] Command registered in RoutingCommand subcommands
- [ ] Manual test: `acode routing override` shows override status

### 1.4 Modify RoutingCommand.cs to Register Subcommands

**File:** `src/Acode.CLI/Commands/Routing/RoutingCommand.cs`

**What to modify:**
- [🔄] In constructor, add three subcommands:
  ```csharp
  AddCommand(new RoutingHeuristicsCommand(_serviceProvider));
  AddCommand(new RoutingEvaluateCommand(_serviceProvider));
  AddCommand(new RoutingOverrideCommand(_serviceProvider));
  ```

**Success Criteria:**
- [ ] `dotnet build` succeeds (no compilation errors)
- [ ] Manual test: `acode routing --help` shows 3 new subcommands (heuristics, evaluate, override)

### 1.5 Verify Phase 1 Complete

**Commands to run:**
```bash
# Build should succeed
dotnet build

# All three commands should be available
acode routing heuristics --help
acode routing evaluate --help
acode routing override --help

# Quick test
acode routing heuristics
acode routing evaluate "simple task"
acode routing override
```

**Evidence needed:**
- [ ] `dotnet build` output: "Build succeeded"
- [ ] All three commands produce non-error output
- [ ] No stack traces or exceptions

---

## PHASE 2: SECURITY & UTILITIES (CRITICAL)

**Duration:** ~1 hour
**Dependency:** Phase 1 must be complete
**Blocking:** Phase 3 cannot start until complete

### 2.1 Create ISensitiveDataRedactor Interface

**File:** `src/Acode.Domain/Security/ISensitiveDataRedactor.cs`

**What to implement:**
- [🔄] Create interface ISensitiveDataRedactor
- [🔄] Method: `string RedactTaskDescription(string? description)`
- [🔄] Method: `string RedactFilePath(string? filePath)`

**Code:**
```csharp
namespace AgenticCoder.Domain.Security;

public interface ISensitiveDataRedactor
{
    /// <summary>
    /// Redacts sensitive information from task descriptions (API keys, passwords, etc.)
    /// </summary>
    string RedactTaskDescription(string? description);

    /// <summary>
    /// Redacts sensitive information from file paths (credentials, private keys, etc.)
    /// </summary>
    string RedactFilePath(string? filePath);
}
```

**Success Criteria:**
- [ ] File compiles without errors

### 2.2 Create SensitiveDataRedactor Implementation

**File:** `src/Acode.Infrastructure/Security/SensitiveDataRedactor.cs`

**What to implement:**
- [🔄] Create class SensitiveDataRedactor : ISensitiveDataRedactor
- [🔄] Private static readonly string[] SensitivePatterns with regex patterns for:
  - API keys (api_key, apikey, API-KEY)
  - Passwords (password, passwd, pwd)
  - Tokens (token, auth_token, access_token, bearer)
  - Secrets (secret, SECRET)
  - SSH/Keys (ssh, private_key, private-key, BEGIN PRIVATE KEY)
  - Credentials (credentials, credential, auth, authorization)
  - Cloud providers (aws_, azure_, gcp_, AKIA)
- [🔄] Implement `public string RedactTaskDescription(string? description)`
  - Return empty string if null
  - Use Regex.Replace with case-insensitive flag
  - Replace matches with "[REDACTED]"
- [🔄] Implement `public string RedactFilePath(string? filePath)`
  - Return empty string if null
  - Same redaction logic as description

**Code Template:**
```csharp
namespace AgenticCoder.Infrastructure.Security;

using System.Text.RegularExpressions;
using AgenticCoder.Domain.Security;

public sealed class SensitiveDataRedactor : ISensitiveDataRedactor
{
    // Pattern matches common sensitive data formats
    private static readonly string SensitivePattern =
        @"(api[_-]?key|password|passwd|pwd|token|auth_token|access_token|bearer|secret|" +
        @"ssh|private[_-]?key|private[_-]?pem|begin.private.key|credentials?|" +
        @"aws_|azure_|gcp_|AKIA|[a-z0-9]{40}(?=[^a-z0-9]|$))";

    private static readonly Regex RedactionRegex = new(SensitivePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string RedactTaskDescription(string? description) =>
        description == null ? string.Empty
            : RedactionRegex.Replace(description, "[REDACTED]");

    public string RedactFilePath(string? filePath) =>
        filePath == null ? string.Empty
            : RedactionRegex.Replace(filePath, "[REDACTED]");
}
```

**Success Criteria:**
- [ ] File compiles without errors
- [ ] Redacts at least: "api_key=abc123", "password=secret", "token=xyz"

### 2.3 Create SensitiveDataRedactorTests.cs

**File:** `tests/Acode.Infrastructure.Tests/Security/SensitiveDataRedactorTests.cs`

**What to implement:**

Test methods (RED first, then implement 2.2):
- [🔄] `Should_Redact_API_Keys()`
  ```csharp
  // Input: "Configure API with api_key=sk-1234567890"
  // Expected: "Configure API with [REDACTED]"
  ```
- [🔄] `Should_Redact_Passwords()`
  ```csharp
  // Input: "Set password=MySecretPassword in config"
  // Expected: "Set [REDACTED] in config"
  ```
- [🔄] `Should_Redact_AWS_Credentials()`
  ```csharp
  // Input: "Use AWS access key AKIAIOSFODNN7EXAMPLE"
  // Expected: "Use AWS access key [REDACTED]"
  ```
- [🔄] `Should_Redact_SSH_Keys()`
  ```csharp
  // Input: "SSH private_key: -----BEGIN PRIVATE KEY-----"
  // Expected: "SSH [REDACTED]: [REDACTED]"
  ```
- [🔄] `Should_Preserve_Non_Sensitive_Text()`
  ```csharp
  // Input: "This is a normal task description with file.cs and README.md"
  // Expected: (unchanged - same as input)
  ```
- [🔄] `Should_Handle_Null_Input()`
  ```csharp
  // Null input should return empty string
  ```

**Code Template:**
```csharp
namespace AgenticCoder.Infrastructure.Tests.Security;

using FluentAssertions;
using AgenticCoder.Infrastructure.Security;
using Xunit;

public sealed class SensitiveDataRedactorTests
{
    private readonly SensitiveDataRedactor _sut = new();

    [Fact]
    public void Should_Redact_API_Keys()
    {
        // Arrange
        var input = "Configure API with api_key=sk-1234567890";

        // Act
        var result = _sut.RedactTaskDescription(input);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("sk-1234567890");
    }

    // Add other test methods...
}
```

**Success Criteria:**
- [ ] All 6 test methods written and failing (RED phase)
- [ ] Tests compile without errors

### 2.4 Implement SensitiveDataRedactor Tests

**What to verify:**
- [✅] All tests pass after implementation of 2.2
- [ ] `dotnet test --filter "SensitiveDataRedactor"` shows all 6 tests passing

**Success Criteria:**
- [ ] Green phase: 6/6 tests passing

### 2.5 Register SensitiveDataRedactor in DI

**File:** `src/Acode.Infrastructure/ServiceCollectionExtensions.cs` (or similar DI configuration file)

**What to modify:**
- [🔄] Add registration: `services.AddSingleton<ISensitiveDataRedactor, SensitiveDataRedactor>();`
- [🔄] This should be in the infrastructure service collection configuration

**Code:**
```csharp
// In infrastructure service registration
services.AddSingleton<ISensitiveDataRedactor, SensitiveDataRedactor>();
```

**Success Criteria:**
- [ ] File compiles without errors
- [ ] Can resolve ISensitiveDataRedactor from DI in tests

### 2.6 Update HeuristicEngine to Use Redactor

**File:** `src/Acode.Infrastructure/Heuristics/HeuristicEngine.cs`

**What to modify:**
- [🔄] Add ISensitiveDataRedactor parameter to constructor
- [🔄] Store as private readonly field
- [🔄] In Evaluate method, before logging, call:
  ```csharp
  var redactedDescription = _redactor.RedactTaskDescription(context.TaskDescription);
  // Use redactedDescription in log output
  ```
- [🔄] Verify existing HeuristicEngineTests still pass

**Success Criteria:**
- [ ] `dotnet test --filter "HeuristicEngine"` all tests still passing
- [ ] No compilation errors

### 2.7 Verify Phase 2 Complete

**Commands to run:**
```bash
# Build should succeed
dotnet build

# All SensitiveDataRedactor tests should pass
dotnet test --filter "SensitiveDataRedactor"

# HeuristicEngine tests should still pass
dotnet test --filter "HeuristicEngine"
```

**Evidence needed:**
- [ ] `dotnet build` output: "Build succeeded"
- [ ] SensitiveDataRedactor tests: "6 passed"
- [ ] HeuristicEngine tests: all passing

---

## PHASE 3: INTEGRATION TESTS (CRITICAL)

**Duration:** ~1 hour
**Dependency:** Phases 1-2 must be complete
**Blocking:** Phase 4 cannot start until complete

### 3.1 Create HeuristicRoutingIntegrationTests.cs

**File:** `tests/Acode.Tests.Integration/Heuristics/HeuristicRoutingIntegrationTests.cs`

**Specification Reference:** Testing Requirements lines 2278-2401

**What to implement:** Full integration test suite with 5 test methods:

#### 3.1.1 Test: Should_Route_Simple_Task_To_Fast_Model

**Specification (lines 2294-2313):**
```csharp
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
```

**Verification:** Verifies AC-071 (Works with Task 009 routing)

#### 3.1.2 Test: Should_Route_Complex_Task_To_Capable_Model

**Specification (lines 2315-2334):**
```csharp
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
```

**Verification:** Verifies AC-071 (Heuristics influence model routing)

#### 3.1.3 Test: Should_Apply_Request_Override_Over_Heuristics

**Specification (lines 2336-2356):**
```csharp
[Fact]
public async Task Should_Apply_Request_Override_Over_Heuristics()
{
    // Arrange
    var router = _factory.Services.GetRequiredService<IModelRouter>();
    var context = new RoutingContext
    {
        TaskDescription = "Simple task",
        Files = new[] { "file.cs" },
        RequestOverride = "llama3.2:70b",
        Strategy = RoutingStrategy.Adaptive
    };

    // Act
    var decision = await router.Route(context);

    // Assert
    decision.SelectedModel.Should().Be("llama3.2:70b");
    decision.Reason.Should().Contain("request override");
    decision.OverrideApplied.Should().BeTrue();
}
```

**Verification:** Verifies AC-071 (Overrides respected)

#### 3.1.4 Test: Should_Force_High_Complexity_For_Security_Task

**Specification (lines 2358-2377):**
```csharp
[Fact]
public async Task Should_Force_High_Complexity_For_Security_Task()
{
    // Arrange
    var router = _factory.Services.GetRequiredService<IModelRouter>();
    var context = new RoutingContext
    {
        TaskDescription = "Update password encryption algorithm to bcrypt",
        Files = new[] { "Auth.cs" },
        Strategy = RoutingStrategy.Adaptive
    };

    // Act
    var decision = await router.Route(context);

    // Assert
    decision.SelectedModel.Should().Contain("70b");
    decision.HeuristicScore.Should().BeGreaterThan(70);
    decision.Reason.Should().Contain("security");
}
```

**Verification:** Verifies AC-071, AC-068 (Security keywords boost scores)

#### 3.1.5 Test: Should_Combine_Role_And_Complexity_Routing

**Specification (lines 2379-2401):**
```csharp
[Fact]
public async Task Should_Combine_Role_And_Complexity_Routing()
{
    // Arrange
    var router = _factory.Services.GetRequiredService<IModelRouter>();
    var context = new RoutingContext
    {
        TaskDescription = "Plan architecture for new microservice",
        Files = Array.Empty<string>(),
        Role = AgentRole.Planner,
        Strategy = RoutingStrategy.Adaptive
    };

    // Act
    var decision = await router.Route(context);

    // Assert
    decision.SelectedModel.Should().Contain("70b");
    decision.Reason.Should().Contain("planner");
    decision.Reason.Should().Contain("complexity");
}
```

**Verification:** Verifies AC-072 (Works with Task 009.a roles)

### 3.2 Complete Test Implementation

**Commands to run:**
```bash
# Build should succeed
dotnet build

# Run integration tests
dotnet test --filter "HeuristicRoutingIntegrationTests"
```

**Success Criteria:**
- [ ] File compiles without errors
- [ ] All 5 tests pass: `5 passed`
- [ ] Output shows router is selecting correct models based on complexity

---

## PHASE 4: E2E & PERFORMANCE TESTS (HIGH)

**Duration:** ~1.5 hours
**Dependency:** Phases 1-3 must be complete
**Blocking:** None (Phase 5 is independent)

### 4.1 Create AdaptiveRoutingE2ETests.cs

**File:** `tests/Acode.Tests.E2E/Heuristics/AdaptiveRoutingE2ETests.cs`

**Specification Reference:** Testing Requirements lines 2403-2506

**What to implement:** Complete E2E test suite with 5 test methods:

#### 4.1.1 Test: Should_Use_Fast_Model_For_Simple_CLI_Request

**Specification (lines 2419-2434):**
```csharp
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
```

**Verification:** Verifies AC-045, AC-047 (CLI commands work end-to-end)

#### 4.1.2 Test: Should_Use_Large_Model_For_Complex_CLI_Request

**Specification (lines 2436-2452):**
```csharp
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
```

**Verification:** Verifies AC-047 (evaluate command works), AC-050 (verbose output)

#### 4.1.3 Test: Should_Respect_Model_Override_Flag

**Specification (lines 2454-2468):**
```csharp
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
```

**Verification:** Verifies AC-046 (override command), AC-048 (shows all details)

#### 4.1.4 Test: Should_Show_Heuristic_Details_Via_CLI

**Specification (lines 2470-2486):**
```csharp
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
```

**Verification:** Verifies AC-045 (heuristics command shows details)

#### 4.1.5 Test: Should_Evaluate_Task_Without_Executing

**Specification (lines 2488-2504):**
```csharp
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
```

**Verification:** Verifies AC-047 (evaluate command works without executing)

### 4.2 Create HeuristicPerformanceBenchmarks.cs

**File:** `tests/Acode.Tests.Performance/Heuristics/HeuristicPerformanceBenchmarks.cs`

**Specification Reference:** Testing Requirements lines 2508-2672

**What to implement:** BenchmarkDotNet performance tests:

**Key benchmarks:**
- [🔄] FileCountHeuristic_Evaluation (target: < 50ms average)
- [🔄] TaskTypeHeuristic_Evaluation (target: < 50ms average)
- [🔄] All_Heuristics_Simple_Task (target: < 100ms average)
- [🔄] All_Heuristics_Complex_Task (target: < 100ms average)
- [🔄] Override_Resolution (target: < 1ms average)

**Code template from spec:**
```csharp
namespace AgenticCoder.Tests.Performance.Heuristics;

using BenchmarkDotNet.Attributes;
using AgenticCoder.Application.Heuristics;
using AgenticCoder.Infrastructure.Heuristics;
using AgenticCoder.Domain.Routing;

[MemoryDiagnoser]
public class HeuristicPerformanceBenchmarks
{
    private HeuristicEngine _engine = null!;
    private HeuristicContext _simpleContext = null!;
    private HeuristicContext _complexContext = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup standard engine with all heuristics
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
    public void FileCountHeuristic_Evaluation() => new FileCountHeuristic().Evaluate(_simpleContext);

    [Benchmark]
    public void All_Heuristics_Simple_Task() => _engine.Evaluate(_simpleContext);

    [Benchmark]
    public void All_Heuristics_Complex_Task() => _engine.Evaluate(_complexContext);
}

// Performance assertions (fact methods)
[Fact]
public void Heuristic_Evaluation_Should_Complete_Under_50ms()
{
    // Implementation from spec lines 2592-2613
}

[Fact]
public void All_Heuristics_Should_Complete_Under_100ms()
{
    // Implementation from spec lines 2616-2644
}

[Fact]
public void Override_Lookup_Should_Complete_Under_1ms()
{
    // Implementation from spec lines 2647-2671
}
```

**Success Criteria:**
- [ ] File compiles without errors
- [ ] All benchmarks run without errors
- [ ] All performance assertions pass (< 100ms total)

### 4.3 Verify Phase 4 Complete

**Commands to run:**
```bash
# Build should succeed
dotnet build

# E2E tests should pass
dotnet test --filter "AdaptiveRoutingE2E"

# Performance tests should pass
dotnet test --filter "HeuristicPerformanceBenchmarks"
```

**Evidence needed:**
- [ ] `dotnet build` output: "Build succeeded"
- [ ] E2E tests: "5 passed"
- [ ] Performance assertions: all passed (< 100ms)

---

## PHASE 5: REGRESSION TESTS & FINAL VERIFICATION (MEDIUM)

**Duration:** ~30 minutes
**Dependency:** Phases 1-4 must be complete
**Blocking:** None

### 5.1 Create HeuristicRegressionTests.cs

**File:** `tests/Acode.Tests.Regression/Heuristics/HeuristicRegressionTests.cs`

**Specification Reference:** Testing Requirements lines 2674-2752

**What to implement:** Complete regression test suite with 3 test methods:

#### 5.1.1 Test: Should_Not_Change_Score_For_Known_Simple_Task

**Specification (lines 2683-2697):**
```csharp
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
```

#### 5.1.2 Test: Should_Not_Change_Score_For_Known_Complex_Task

**Specification (lines 2699-2713):**
```csharp
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
```

#### 5.1.3 Test: Should_Always_Force_High_Score_For_Security_Keywords

**Specification (lines 2715-2741):**
```csharp
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
```

### 5.2 Implement CreateStandardEngine Helper

**What to add:**
- [🔄] Private helper method: `private static HeuristicEngine CreateStandardEngine()`
  - Returns HeuristicEngine with all 3 heuristics
  - Used by all regression tests

**Code:**
```csharp
private static HeuristicEngine CreateStandardEngine()
{
    return new HeuristicEngine(new IRoutingHeuristic[]
    {
        new FileCountHeuristic(),
        new TaskTypeHeuristic(),
        new LanguageHeuristic()
    });
}
```

### 5.3 Verify Phase 5 Complete

**Commands to run:**
```bash
# Build should succeed
dotnet build

# Regression tests should pass
dotnet test --filter "HeuristicRegressionTests"
```

**Evidence needed:**
- [ ] `dotnet build` output: "Build succeeded"
- [ ] Regression tests: "3 passed"

---

## FINAL VERIFICATION: TASK-009B COMPLETE

### Step 1: Full Build

```bash
dotnet build
```

**Expected:** "Build succeeded"

### Step 2: Run All Tests

```bash
dotnet test
```

**Expected:** All tests pass, including:
- 113 existing unit tests (heuristics)
- 5 new integration tests
- 5 new E2E tests
- 3 performance benchmarks pass
- 3 regression tests pass

### Step 3: Verify CLI Commands

```bash
# Test all three new commands
acode routing heuristics
acode routing heuristics --verbose
acode routing evaluate "Fix typo in README"
acode routing evaluate "Complex refactoring" --json
acode routing override
```

**Expected:** All commands succeed without errors and produce expected output

### Step 4: Manual Acceptance Criteria Check

Create verification checklist: All 75 ACs should be verifiable:
- AC-001 through AC-008: Interface (✅ verify with reflection)
- AC-009 through AC-014: Engine (✅ unit tests)
- AC-015 through AC-020: Score (✅ unit tests)
- AC-021 through AC-025: Heuristics (✅ unit tests)
- AC-026 through AC-030: Precedence (✅ unit tests)
- AC-031 through AC-035: Request Override (✅ unit tests)
- AC-036 through AC-040: Session Override (✅ unit tests, CLI)
- AC-041 through AC-044: Config Override (✅ unit tests)
- **AC-045 through AC-050: CLI (✅ E2E tests)**
- AC-051 through AC-055: Extensibility (✅ unit tests)
- AC-056 through AC-060: Error Handling (✅ unit tests)
- AC-061 through AC-065: Configuration (✅ unit tests)
- **AC-066 through AC-070: Security (✅ unit tests + new SensitiveDataRedactor)**
- **AC-071 through AC-075: Integration (✅ new integration tests)**

### Step 5: Update Progress Notes

Document completion:
- [ ] All 75 ACs verified
- [ ] All test suites passing
- [ ] All CLI commands working
- [ ] No compilation errors
- [ ] Performance benchmarks < 100ms

### Step 6: Create Commit

```bash
git add -A
git commit -m "feat(task-009b): complete routing heuristics and overrides implementation

Implements all CLI commands, integration/E2E tests, security utilities.
Achieves 100% acceptance criteria compliance (75/75 ACs).

- Add RoutingHeuristicsCommand, RoutingEvaluateCommand, RoutingOverrideCommand
- Add SensitiveDataRedactor for logging security
- Add HeuristicRoutingIntegrationTests (5 tests)
- Add AdaptiveRoutingE2ETests (5 tests)
- Add HeuristicPerformanceBenchmarks
- Add HeuristicRegressionTests (3 tests)
- All 75 acceptance criteria verified and passing
- All tests passing (120+ total tests)
- Performance verified < 100ms

🤖 Generated with Claude Code
Co-Authored-By: Claude Haiku <noreply@anthropic.com>"
```

### Step 7: Create Pull Request

```bash
gh pr create --title "feat(task-009b): Complete routing heuristics and overrides" \
  --body "Complete implementation of task-009b with all CLI commands, integration tests, and security utilities. 75/75 acceptance criteria verified."
```

---

## Checklist Summary (Check Off as You Complete Each Phase)

- [ ] Phase 1: CLI Commands (RoutingHeuristicsCommand, RoutingEvaluateCommand, RoutingOverrideCommand)
- [ ] Phase 2: Security & Utilities (SensitiveDataRedactor)
- [ ] Phase 3: Integration Tests (HeuristicRoutingIntegrationTests - 5 tests)
- [ ] Phase 4: E2E & Performance Tests (AdaptiveRoutingE2ETests - 5 tests, HeuristicPerformanceBenchmarks)
- [ ] Phase 5: Regression Tests (HeuristicRegressionTests - 3 tests)
- [ ] Final Verification: All tests passing, all 75 ACs verified
- [ ] Git commit and PR created

---

## Reference Documents

- **Gap Analysis:** `docs/implementation-plans/task-009b-gap-analysis.md`
- **Spec File:** `docs/tasks/refined-tasks/Epic 01/task-009b-routing-heuristics-overrides.md`
- **Related Task:** Task-009a (Roles) - must work alongside heuristics
- **Related Task:** Task-009 (Routing) - heuristics feed into main routing decision

---

**Status Update:** This task is ~60% complete. Complete all 5 phases to reach 100%.
