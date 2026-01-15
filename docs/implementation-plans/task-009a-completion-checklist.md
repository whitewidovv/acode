# Task-009a Completion Checklist: Planner/Coder/Reviewer Roles

**Task**: task-009a-planner-coder-reviewer-roles.md (Epic 01)
**Current Status**: 88.9% Semantically Complete (40/45 ACs Met)
**Date**: 2026-01-14
**Instructions for Next Agent**: This checklist contains ONLY what's missing to reach 100% semantic completeness. The domain/application/infrastructure layers are production-ready. Follow items in order. Mark [🔄] when starting, [✅] when done.

---

## What Already Exists (DO NOT Recreate - Just Use)

**Fully Implemented and Tested**:
- ✅ Domain Layer: AgentRole enum, ContextStrategy enum, RoleDefinition sealed class (4 files, 100% complete)
- ✅ Application Layer: IRoleRegistry interface, RoleTransitionEntry record, InvalidRoleTransitionException (3 files, 100% complete)
- ✅ Infrastructure Layer: RoleRegistry implementation, RoleDefinitionProvider static class (2 files, 100% complete)
- ✅ Tests: 34 test methods across 3 test files (AgentRoleTests, RoleDefinitionTests, RoleRegistryTests - all passing)
- ✅ Prompts: planner.md, coder.md, reviewer.md embedded resources (3 files)
- ✅ DI Registration: Verify AddSingleton<IRoleRegistry, RoleRegistry>() in ServiceCollectionExtensions

**Critical Verification** (should already work):
- ✅ AgentRole enum values (Default=0, Planner=1, Coder=2, Reviewer=3)
- ✅ Role transition validation (Default→Planner→Coder, Reviewer→Coder, etc.)
- ✅ Thread-safe RoleRegistry with locking
- ✅ Role history tracking with timestamps
- ✅ Immutable RoleDefinition with validation

---

## What's Missing (Only 3 Gaps, All Fixable)

### GAP 1: Implement RoleCommand CLI (CRITICAL - BLOCKING)

**Priority**: 🔴 CRITICAL - Blocks 5 ACs (11.1% of task)

**Status**: ❌ FILES DO NOT EXIST

**Files to Create**: 7 files total
1. `src/Acode.Cli/Commands/RoleCommand.cs` - Main command class
2. `src/Acode.Cli/Commands/Roles/ListRolesCommand.cs` - `role list` subcommand
3. `src/Acode.Cli/Commands/Roles/ShowRoleCommand.cs` - `role show` subcommand
4. `src/Acode.Cli/Commands/Roles/CurrentRoleCommand.cs` - `role current` subcommand
5. `src/Acode.Cli/Commands/Roles/SetRoleCommand.cs` - `role set` subcommand
6. `src/Acode.Cli/Commands/Roles/RoleHistoryCommand.cs` - `role history` subcommand
7. `tests/Acode.Cli.Tests/Commands/RoleCommandTests.cs` - CLI integration tests

### GAP 2: Implement RoleTransitionTests (HIGH - STRENGTHENS COVERAGE)

**Priority**: 🟡 HIGH - Adds test coverage for transition validation

**Status**: ❌ FILE DOES NOT EXIST

**File to Create**: `tests/Acode.Application.Tests/Roles/RoleTransitionTests.cs`

**Test Methods to Add**: 7 test methods (from spec lines 1994-2093)

### GAP 3: Verify DI Registration (MEDIUM - RUNTIME DEPENDENCY)

**Priority**: 🟡 MEDIUM - Ensures runtime dependency injection works

**Status**: ⚠️ NEEDS VERIFICATION

**File to Check**: `src/Acode.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`

**Verification**: Confirm `services.AddSingleton<IRoleRegistry, RoleRegistry>();` is present

---

## PHASE 1: Implement RoleCommand CLI (CRITICAL)

### 1.1 - Create RoleCommand.cs Main Class [🔄 → ✅]

**File**: `src/Acode.Cli/Commands/RoleCommand.cs`

**Purpose**: Entry point for `acode role` command, delegates to subcommands

**Template Code** (from spec lines 3472-3530):
```csharp
using Spectre.Console;
using Acode.Application.Roles;
using Acode.Cli.Infrastructure;

namespace Acode.Cli.Commands;

/// <summary>
/// Manages agent role transitions and displays role information.
/// </summary>
public class RoleCommand : Command
{
    private readonly IRoleRegistry _roleRegistry;
    
    public RoleCommand(IRoleRegistry roleRegistry) : base("role", "Manage agent roles (planner, coder, reviewer)")
    {
        _roleRegistry = roleRegistry;
        
        AddCommand(new ListRolesCommand(_roleRegistry));
        AddCommand(new ShowRoleCommand(_roleRegistry));
        AddCommand(new CurrentRoleCommand(_roleRegistry));
        AddCommand(new SetRoleCommand(_roleRegistry));
        AddCommand(new RoleHistoryCommand(_roleRegistry));
    }
}
```

**Acceptance Criteria Met**: Part of AC-041

---

### 1.2 - Create ListRolesCommand.cs [🔄 → ✅]

**File**: `src/Acode.Cli/Commands/Roles/ListRolesCommand.cs`

**Purpose**: Implements `acode role list` - Lists all available roles

**Command**: `role list`

**Output Format**: Table with columns
- Role (enum name)
- Name (display name)
- Capabilities (count)
- Constraints (count)

**Template Code** (from spec lines 3531-3555):
```csharp
using Spectre.Console;
using Acode.Application.Roles;
using Acode.Cli.Infrastructure;

namespace Acode.Cli.Commands;

public class ListRolesCommand : Command
{
    private readonly IRoleRegistry _roleRegistry;
    
    public ListRolesCommand(IRoleRegistry roleRegistry) : base("list", "List all available roles")
    {
        _roleRegistry = roleRegistry;
    }
    
    public override int Execute()
    {
        try
        {
            var roles = _roleRegistry.ListRoles();
            
            var table = new Table();
            table.AddColumn("Role");
            table.AddColumn("Name");
            table.AddColumn("Capabilities");
            table.AddColumn("Constraints");
            
            foreach (var role in roles)
            {
                table.AddRow(
                    role.Role.ToString(),
                    role.Name,
                    role.Capabilities.Count.ToString(),
                    role.Constraints.Count.ToString()
                );
            }
            
            AnsiConsole.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}
```

**Acceptance Criteria Met**: AC-041, AC-042 (list all roles)

**Verification**:
```bash
dotnet run -- role list
# Expected output: Table with 4 rows (Default, Planner, Coder, Reviewer)
```

---

### 1.3 - Create ShowRoleCommand.cs [🔄 → ✅]

**File**: `src/Acode.Cli/Commands/Roles/ShowRoleCommand.cs`

**Purpose**: Implements `acode role show <role>` - Shows details for specific role

**Command**: `role show <role-name>`

**Arguments**: `<role>` - One of: default, planner, coder, reviewer

**Output Format**: 
- Role name and description
- Capabilities (bulleted list)
- Constraints (bulleted list)

**Template Code** (from spec lines 3556-3590):
```csharp
using System.CommandLine;
using Spectre.Console;
using Acode.Application.Roles;
using Acode.Cli.Infrastructure;
using Acode.Domain.Roles;

namespace Acode.Cli.Commands;

public class ShowRoleCommand : Command
{
    private readonly IRoleRegistry _roleRegistry;
    
    public ShowRoleCommand(IRoleRegistry roleRegistry) : base("show", "Show details for a role")
    {
        _roleRegistry = roleRegistry;
        
        var roleArgument = new Argument<string>("role", "Role name (default, planner, coder, reviewer)");
        AddArgument(roleArgument);
    }
    
    public override int Execute()
    {
        try
        {
            var roleName = Context?.ParseResult.GetValueForArgument(GetArguments().First() as Argument<string>) as string 
                ?? throw new InvalidOperationException("Role argument required");
            
            if (!AgentRoleExtensions.TryParse(roleName, out var role))
            {
                AnsiConsole.MarkupLine($"[red]Error: Invalid role '{roleName}'[/]");
                return 1;
            }
            
            var roleDefinition = _roleRegistry.GetRole(role);
            
            AnsiConsole.MarkupLine($"[bold cyan]{roleDefinition.Name}[/]");
            AnsiConsole.MarkupLine($"[grey]{roleDefinition.Description}[/]");
            
            AnsiConsole.MarkupLine("\n[bold]Capabilities:[/]");
            foreach (var cap in roleDefinition.Capabilities)
            {
                AnsiConsole.MarkupLine($"  • {cap}");
            }
            
            AnsiConsole.MarkupLine("\n[bold]Constraints:[/]");
            foreach (var constraint in roleDefinition.Constraints)
            {
                AnsiConsole.MarkupLine($"  • {constraint}");
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}
```

**Acceptance Criteria Met**: AC-043, AC-044 (show role details)

**Verification**:
```bash
dotnet run -- role show planner
# Expected output: Planner name, description, capabilities list, constraints list
```

---

### 1.4 - Create CurrentRoleCommand.cs [🔄 → ✅]

**File**: `src/Acode.Cli/Commands/Roles/CurrentRoleCommand.cs`

**Purpose**: Implements `acode role current` - Shows currently active role

**Command**: `role current`

**Output Format**: 
- Current role name
- Transition reason
- Timestamp

**Template Code**:
```csharp
using Spectre.Console;
using Acode.Application.Roles;
using Acode.Cli.Infrastructure;

namespace Acode.Cli.Commands;

public class CurrentRoleCommand : Command
{
    private readonly IRoleRegistry _roleRegistry;
    
    public CurrentRoleCommand(IRoleRegistry roleRegistry) : base("current", "Show currently active role")
    {
        _roleRegistry = roleRegistry;
    }
    
    public override int Execute()
    {
        try
        {
            var currentRole = _roleRegistry.GetCurrentRole();
            var roleDefinition = _roleRegistry.GetRole(currentRole);
            var history = _roleRegistry.GetRoleHistory();
            var lastTransition = history.LastOrDefault();
            
            AnsiConsole.MarkupLine($"[bold cyan]Current Role: {roleDefinition.Name}[/]");
            
            if (lastTransition != null)
            {
                AnsiConsole.MarkupLine($"[grey]Reason: {lastTransition.Reason}[/]");
                AnsiConsole.MarkupLine($"[grey]Since: {lastTransition.Timestamp:O}[/]");
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}
```

**Acceptance Criteria Met**: AC-045 (current role command)

**Verification**:
```bash
dotnet run -- role current
# Expected output: "Current Role: Default (or whatever active role is)"
```

---

### 1.5 - Create SetRoleCommand.cs [🔄 → ✅]

**File**: `src/Acode.Cli/Commands/Roles/SetRoleCommand.cs`

**Purpose**: Implements `acode role set <role> --reason <reason>` - Transitions to new role

**Command**: `role set <role> --reason <reason>`

**Arguments**: 
- `<role>` - Target role: default, planner, coder, reviewer
- `--reason <reason>` (required) - Reason for transition

**Error Handling**:
- Reject if invalid role
- Reject if transition not allowed
- Reject if reason not provided

**Template Code**:
```csharp
using System.CommandLine;
using Spectre.Console;
using Acode.Application.Roles;
using Acode.Cli.Infrastructure;
using Acode.Domain.Roles;

namespace Acode.Cli.Commands;

public class SetRoleCommand : Command
{
    private readonly IRoleRegistry _roleRegistry;
    
    public SetRoleCommand(IRoleRegistry roleRegistry) : base("set", "Transition to a new role")
    {
        _roleRegistry = roleRegistry;
        
        AddArgument(new Argument<string>("role", "Target role"));
        AddOption(new Option<string>(new[] { "--reason", "-r" }, "Reason for transition") { IsRequired = true });
    }
    
    public override int Execute()
    {
        try
        {
            // Parse arguments and options from System.CommandLine context
            // (Implementation depends on your Command base class structure)
            
            if (!AgentRoleExtensions.TryParse(roleName, out var targetRole))
            {
                AnsiConsole.MarkupLine($"[red]Error: Invalid role '{roleName}'[/]");
                return 1;
            }
            
            _roleRegistry.SetCurrentRole(targetRole, reason);
            
            AnsiConsole.MarkupLine($"[green]✓ Transitioned to {AgentRoleExtensions.ToDisplayString(targetRole)}[/]");
            AnsiConsole.MarkupLine($"[grey]Reason: {reason}[/]");
            
            return 0;
        }
        catch (InvalidRoleTransitionException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: Cannot transition from {ex.FromRole} to {ex.ToRole}[/]");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}
```

**Acceptance Criteria Met**: AC-041 (set role command)

**Verification**:
```bash
dotnet run -- role set planner --reason "Starting analysis phase"
# Expected: "✓ Transitioned to Planner"

dotnet run -- role set reviewer --reason "Invalid"
# Expected: Error "Cannot transition from ..."
```

---

### 1.6 - Create RoleHistoryCommand.cs [🔄 → ✅]

**File**: `src/Acode.Cli/Commands/Roles/RoleHistoryCommand.cs`

**Purpose**: Implements `acode role history` - Shows role transition history

**Command**: `role history`

**Output Format**: Table with columns
- Timestamp
- From Role
- To Role
- Reason

**Template Code**:
```csharp
using Spectre.Console;
using Acode.Application.Roles;
using Acode.Cli.Infrastructure;

namespace Acode.Cli.Commands;

public class RoleHistoryCommand : Command
{
    private readonly IRoleRegistry _roleRegistry;
    
    public RoleHistoryCommand(IRoleRegistry roleRegistry) : base("history", "Show role transition history")
    {
        _roleRegistry = roleRegistry;
    }
    
    public override int Execute()
    {
        try
        {
            var history = _roleRegistry.GetRoleHistory();
            
            if (!history.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No role transitions recorded[/]");
                return 0;
            }
            
            var table = new Table();
            table.AddColumn("Timestamp");
            table.AddColumn("Transition");
            table.AddColumn("Reason");
            
            foreach (var entry in history)
            {
                var fromRole = entry.FromRole?.ToString() ?? "None";
                var toRole = entry.ToRole.ToString();
                table.AddRow(
                    entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    $"{fromRole} → {toRole}",
                    entry.Reason
                );
            }
            
            AnsiConsole.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}
```

**Acceptance Criteria Met**: AC-041 (history command)

**Verification**:
```bash
dotnet run -- role history
# Expected: Table showing all role transitions with timestamps and reasons
```

---

### 1.7 - Create RoleCommandTests.cs [🔄 → ✅]

**File**: `tests/Acode.Cli.Tests/Commands/RoleCommandTests.cs`

**Purpose**: Test all RoleCommand subcommands and error handling

**Test Methods to Implement**:

1. **ListRolesCommand_Should_Display_All_Roles** [Fact]
   - Runs: `role list`
   - Verifies: Output contains all 4 roles
   - Asserts: Exit code 0, table with Default/Planner/Coder/Reviewer

2. **ShowRoleCommand_Should_Display_Role_Details** [Theory]
   - [InlineData("planner")] [InlineData("coder")] [InlineData("reviewer")]
   - Runs: `role show <role>`
   - Verifies: Output includes name, capabilities, constraints

3. **ShowRoleCommand_Should_Reject_Invalid_Role** [Fact]
   - Runs: `role show invalid-role`
   - Asserts: Exit code 1, error message displayed

4. **CurrentRoleCommand_Should_Show_Active_Role** [Fact]
   - Runs: `role current`
   - Verifies: Shows "Default" initially
   - Asserts: Output contains current role name

5. **SetRoleCommand_Should_Transition_Role** [Fact]
   - Runs: `role set planner --reason "Analysis phase"`
   - Verifies: Transition succeeds, registry updated
   - Asserts: Exit code 0, output confirms transition

6. **SetRoleCommand_Should_Reject_Invalid_Transition** [Fact]
   - Initial: role set to Coder
   - Runs: `role set planner --reason "Invalid"`
   - Asserts: Exit code 1, error message about invalid transition

7. **RoleHistoryCommand_Should_Show_Transitions** [Fact]
   - After multiple transitions
   - Runs: `role history`
   - Verifies: Table shows all transitions
   - Asserts: Exit code 0, output contains timestamps and reasons

**Verification**:
```bash
dotnet test tests/Acode.Cli.Tests/Commands/RoleCommandTests.cs --verbosity normal
# Expected: All 7 tests passing
```

**Acceptance Criteria Met**: AC-041, AC-042, AC-043, AC-044, AC-045 (all CLI ACs)

---

## PHASE 2: Implement RoleTransitionTests (HIGH)

### 2.1 - Create RoleTransitionTests.cs [🔄 → ✅]

**File**: `tests/Acode.Application.Tests/Roles/RoleTransitionTests.cs`

**Purpose**: Dedicated testing for role transition validation logic

**Test Methods** (from spec lines 1994-2093):

```csharp
using FluentAssertions;
using Xunit;
using Acode.Application.Roles;
using Acode.Infrastructure.Roles;
using Microsoft.Extensions.Logging;

namespace Acode.Application.Tests.Roles;

public class RoleTransitionTests
{
    private readonly IRoleRegistry _registry;
    private readonly ILogger<RoleRegistry> _logger;
    
    public RoleTransitionTests()
    {
        // Create mock logger or use test logger
        _logger = new TestLogger<RoleRegistry>();
        _registry = new RoleRegistry(_logger);
    }
    
    [Fact]
    public void Should_Allow_Default_To_Planner_Transition()
    {
        // Arrange
        _registry.GetCurrentRole().Should().Be(AgentRole.Default);
        
        // Act
        _registry.SetCurrentRole(AgentRole.Planner, "Initial decomposition phase");
        
        // Assert
        _registry.GetCurrentRole().Should().Be(AgentRole.Planner);
    }
    
    [Fact]
    public void Should_Allow_Planner_To_Coder_Transition()
    {
        // Arrange
        _registry.SetCurrentRole(AgentRole.Planner, "Decomposition complete");
        _registry.GetCurrentRole().Should().Be(AgentRole.Planner);
        
        // Act
        _registry.SetCurrentRole(AgentRole.Coder, "Implementation phase");
        
        // Assert
        _registry.GetCurrentRole().Should().Be(AgentRole.Coder);
    }
    
    [Fact]
    public void Should_Allow_Coder_To_Reviewer_Transition()
    {
        // Arrange
        _registry.SetCurrentRole(AgentRole.Planner, "Start");
        _registry.SetCurrentRole(AgentRole.Coder, "Implement");
        
        // Act
        _registry.SetCurrentRole(AgentRole.Reviewer, "Review code");
        
        // Assert
        _registry.GetCurrentRole().Should().Be(AgentRole.Reviewer);
    }
    
    [Fact]
    public void Should_Allow_Reviewer_To_Coder_Transition_For_Revisions()
    {
        // Arrange
        _registry.SetCurrentRole(AgentRole.Planner, "Start");
        _registry.SetCurrentRole(AgentRole.Coder, "Implement");
        _registry.SetCurrentRole(AgentRole.Reviewer, "Review");
        
        // Act - Back to Coder for revisions
        _registry.SetCurrentRole(AgentRole.Coder, "Revisions needed");
        
        // Assert
        _registry.GetCurrentRole().Should().Be(AgentRole.Coder);
    }
    
    [Theory]
    [InlineData(AgentRole.Planner)]
    [InlineData(AgentRole.Coder)]
    [InlineData(AgentRole.Reviewer)]
    public void Should_Allow_Any_Role_To_Default_Transition(AgentRole sourceRole)
    {
        // Arrange
        _registry.SetCurrentRole(sourceRole, "Setup");
        _registry.GetCurrentRole().Should().Be(sourceRole);
        
        // Act
        _registry.SetCurrentRole(AgentRole.Default, "Reset");
        
        // Assert
        _registry.GetCurrentRole().Should().Be(AgentRole.Default);
    }
    
    [Fact]
    public void Should_Log_Transition_Reason()
    {
        // Arrange
        var reason = "Specific reason for transition";
        
        // Act
        _registry.SetCurrentRole(AgentRole.Planner, reason);
        
        // Assert
        var history = _registry.GetRoleHistory();
        var lastTransition = history.Last();
        lastTransition.Reason.Should().Be(reason);
    }
    
    [Theory]
    [InlineData(AgentRole.Planner, AgentRole.Reviewer)]  // Invalid
    [InlineData(AgentRole.Coder, AgentRole.Planner)]     // Invalid
    [InlineData(AgentRole.Reviewer, AgentRole.Planner)]  // Invalid
    public void Should_Reject_Invalid_Transitions(AgentRole from, AgentRole to)
    {
        // Arrange
        _registry.SetCurrentRole(from, "Setup");
        
        // Act & Assert
        var act = () => _registry.SetCurrentRole(to, "Invalid transition");
        act.Should().Throw<InvalidRoleTransitionException>();
    }
}
```

**Verification**:
```bash
dotnet test tests/Acode.Application.Tests/Roles/RoleTransitionTests.cs --verbosity normal
# Expected: All 8 tests passing
```

---

## PHASE 3: Verify DI Registration (MEDIUM)

### 3.1 - Verify RoleRegistry DI Registration [🔄 → ✅]

**File to Check**: `src/Acode.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`

**Expected Code**: 
```csharp
services.AddSingleton<IRoleRegistry, RoleRegistry>();
```

**Verification Command**:
```bash
grep -A 5 "public static.*AddRoles\|AddSingleton<IRoleRegistry" src/Acode.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
```

**Expected Output**: Method `AddRoles()` or similar with registration of IRoleRegistry → RoleRegistry

**If Missing**: Add registration:
```csharp
/// <summary>
/// Registers role management services with dependency injection.
/// </summary>
public static IServiceCollection AddRoles(this IServiceCollection services)
{
    services.AddSingleton<IRoleRegistry, RoleRegistry>();
    return services;
}
```

Then ensure this is called in Program.cs or main DI setup:
```csharp
services.AddRoles();
```

---

## PHASE 4: Final Verification

### 4.1 - Build and Run All Tests [🔄 → ✅]

**Commands** (run in order):

```bash
# Step 1: Build
dotnet build --configuration Debug
# Expected: Build succeeded. - 0 Error(s), 0 Warning(s)

# Step 2: Run all role-related tests
dotnet test tests/Acode.Domain.Tests/Roles/ tests/Acode.Application.Tests/Roles/ tests/Acode.Infrastructure.Tests/Roles/ tests/Acode.Cli.Tests/Commands/RoleCommandTests.cs --verbosity normal --configuration Debug

# Expected output:
#   Domain.Tests:         12 tests ✅ PASSED
#   Application.Tests:    7 tests ✅ PASSED (from RoleTransitionTests)
#   Infrastructure.Tests: 15 tests ✅ PASSED
#   Cli.Tests:            7 tests ✅ PASSED (from RoleCommandTests)
#   Total: 41+ tests PASSED

# Step 3: Verify CLI commands work manually
dotnet run -- role list
dotnet run -- role show planner
dotnet run -- role current
dotnet run -- role set coder --reason "Implementation phase"
dotnet run -- role history
```

**Success Criteria**:
- [ ] Build succeeds: "Build succeeded. - 0 Error(s), 0 Warning(s)"
- [ ] All 41+ tests pass
- [ ] CLI commands execute without errors
- [ ] Output formatting is correct
- [ ] Error handling works (invalid roles, invalid transitions)

---

## Summary of Work

| Phase | Task | Priority | Est. Time | Files | Tests |
|-------|------|----------|-----------|-------|-------|
| 1 | RoleCommand CLI | CRITICAL | 3-4h | 7 | 7 |
| 2 | RoleTransitionTests | HIGH | 1h | 1 | 8 |
| 3 | DI Registration | MEDIUM | 15 min | 0-1 | 0 |
| 4 | Final Verification | - | 30 min | 0 | - |
| **TOTAL** | | | **4.5-5 hours** | **8-9** | **15+** |

---

## Success Definition

**Task-009a is complete when**:

1. ✅ Build succeeds: `dotnet build` → "Build succeeded"
2. ✅ All 45 Acceptance Criteria verified (40 existing + 5 new from CLI)
3. ✅ 41+ tests passing (26 existing + 15 new)
4. ✅ CLI commands work:
   - `role list` shows all roles ✅
   - `role show <role>` shows details ✅
   - `role current` shows active role ✅
   - `role set <role>` transitions with validation ✅
   - `role history` shows transitions ✅
5. ✅ Error handling works for invalid inputs
6. ✅ DI registration verified
7. ✅ All ACs documented as met

---

## References

- **Task Spec**: docs/tasks/refined-tasks/Epic 01/task-009a-planner-coder-reviewer-roles.md
- **Gap Analysis**: docs/implementation-plans/task-009a-gap-analysis.md
- **CLI Code**: Spec lines 3472-3597 (complete implementation)
- **Test Code**: Spec lines 1994-2093 (RoleTransitionTests)
- **Acceptance Criteria**: Lines 1562-1634 (45 total)

