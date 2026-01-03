# Task 008.c: Starter Packs (dotnet/react, strict minimal diff)

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 008, Task 008.a, Task 008.b  

---

## Description

Task 008.c creates the built-in starter prompt packs that ship with Acode. These packs provide ready-to-use prompts for common development scenarios—general coding, .NET/C# development, and React/TypeScript development. They also establish a "strict minimal diff" philosophy that emphasizes precise, focused code changes.

Starter packs are the out-of-box experience. When users install Acode and run it for the first time, they get the `acode-standard` pack by default. This pack works well for general coding tasks across languages. For specialized work, users can switch to `acode-dotnet` or `acode-react` packs that include language-specific guidance and framework patterns.

The "strict minimal diff" philosophy is a core principle encoded in all starter packs. It instructs the model to make the smallest possible changes that accomplish the task—no unnecessary refactoring, no style changes unrelated to the task, no "improvements" that weren't requested. This philosophy reduces review burden, minimizes risk, and keeps changes focused.

Each starter pack includes a system prompt that establishes the agent's identity, capabilities, and constraints. The system prompt is the foundation—it sets expectations about what the agent can and cannot do, how it approaches tasks, and what outputs it produces. All starter packs share core instructions with variations for specific domains.

Role prompts define behavior for different agent modes. The planner role focuses on task decomposition and planning. The coder role focuses on implementation with emphasis on correctness and minimal changes. The reviewer role focuses on code quality, correctness verification, and constructive feedback. Each pack includes prompts for all three roles.

Language prompts provide language-specific guidance. For `acode-dotnet`, this includes C# conventions, .NET idioms, async/await patterns, and common pitfalls. For `acode-react`, this includes TypeScript best practices, React patterns, hooks conventions, and state management guidance. These prompts are injected when the agent detects the relevant language.

Framework prompts provide framework-specific patterns. For .NET, this includes ASP.NET Core patterns, Entity Framework conventions, and dependency injection practices. For React, this includes Next.js patterns, component architecture, and testing approaches. Framework prompts layer on top of language prompts for deeper specialization.

The strict minimal diff instructions are repeated across prompts to reinforce the behavior. They specify: only modify code necessary for the task, preserve existing style, don't fix unrelated issues, don't add features not requested, explain any deviation from the request. This repetition ensures the model internalizes the constraint.

Built-in packs are embedded as resources in the application assembly. They are extracted to temporary locations when needed and are always available regardless of workspace state. Users can override built-in packs by creating user packs with the same ID in their workspace.

The packs use template variables for context awareness. `{{workspace_name}}` identifies the project, `{{language}}` identifies the primary language, `{{framework}}` identifies detected frameworks. These variables are populated during prompt composition, making prompts contextually relevant.

Quality of starter pack content directly impacts user experience. Poorly written prompts lead to poor model behavior. Each prompt is carefully crafted with clear, specific instructions. Prompts are tested with multiple models to ensure they work across the supported model range (Llama, Mistral, Qwen, DeepSeek).

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Starter Pack | Built-in prompt pack shipped with Acode |
| acode-standard | General purpose starter pack |
| acode-dotnet | .NET/C# specialized pack |
| acode-react | React/TypeScript specialized pack |
| Strict Minimal Diff | Philosophy of smallest possible changes |
| System Prompt | Base instructions for agent |
| Role Prompt | Instructions for specific mode |
| Language Prompt | Language-specific guidance |
| Framework Prompt | Framework-specific patterns |
| Embedded Resource | Pack bundled in assembly |
| Template Variable | Dynamic placeholder in prompt |
| Prompt Layering | Combining prompts hierarchically |
| Context Awareness | Adapting to project details |
| Model Agnostic | Works across supported models |
| Defensive Instruction | Repeated constraint reinforcement |

---

## Out of Scope

The following items are explicitly excluded from Task 008.c:

- **Custom user packs** - Covered in Task 008.a/008.b
- **Pack loading logic** - Covered in Task 008.b
- **Pack validation logic** - Covered in Task 008.b
- **Pack file format definition** - Covered in Task 008.a
- **Prompt composition engine** - Covered in Task 008
- **Model-specific prompt variations** - Not in MVP
- **Non-English prompts** - Not in MVP
- **Prompt optimization/testing** - Post-MVP
- **Community pack contributions** - Post-MVP
- **Pack marketplace** - Not planned

---

## Functional Requirements

### acode-standard Pack

- FR-001: Pack MUST have id "acode-standard"
- FR-002: Pack MUST have version 1.0.0
- FR-003: Pack MUST include system.md
- FR-004: Pack MUST include roles/planner.md
- FR-005: Pack MUST include roles/coder.md
- FR-006: Pack MUST include roles/reviewer.md
- FR-007: system.md MUST define agent identity
- FR-008: system.md MUST define capabilities
- FR-009: system.md MUST define limitations
- FR-010: system.md MUST include strict minimal diff
- FR-011: All roles MUST reference strict minimal diff

### acode-dotnet Pack

- FR-012: Pack MUST have id "acode-dotnet"
- FR-013: Pack MUST have version 1.0.0
- FR-014: Pack MUST include all acode-standard components
- FR-015: Pack MUST include languages/csharp.md
- FR-016: Pack MUST include frameworks/aspnetcore.md
- FR-017: csharp.md MUST cover naming conventions
- FR-018: csharp.md MUST cover async/await patterns
- FR-019: csharp.md MUST cover nullable reference types
- FR-020: csharp.md MUST cover common pitfalls
- FR-021: aspnetcore.md MUST cover DI patterns
- FR-022: aspnetcore.md MUST cover controller conventions
- FR-023: aspnetcore.md MUST cover EF Core patterns

### acode-react Pack

- FR-024: Pack MUST have id "acode-react"
- FR-025: Pack MUST have version 1.0.0
- FR-026: Pack MUST include all acode-standard components
- FR-027: Pack MUST include languages/typescript.md
- FR-028: Pack MUST include frameworks/react.md
- FR-029: typescript.md MUST cover type definitions
- FR-030: typescript.md MUST cover strict mode practices
- FR-031: typescript.md MUST cover import conventions
- FR-032: react.md MUST cover component patterns
- FR-033: react.md MUST cover hooks best practices
- FR-034: react.md MUST cover state management
- FR-035: react.md MUST cover testing patterns

### Strict Minimal Diff Instructions

- FR-036: MUST instruct: only modify necessary code
- FR-037: MUST instruct: preserve existing style
- FR-038: MUST instruct: don't fix unrelated issues
- FR-039: MUST instruct: don't add unrequested features
- FR-040: MUST instruct: explain deviations
- FR-041: MUST instruct: prefer small PRs
- FR-042: MUST instruct: minimize review burden
- FR-043: Instructions MUST appear in system.md
- FR-044: Instructions MUST be reinforced in coder.md
- FR-045: Instructions MUST be reinforced in reviewer.md

### System Prompt Structure

- FR-046: MUST start with identity statement
- FR-047: MUST list available tools
- FR-048: MUST describe output formats
- FR-049: MUST specify safety constraints
- FR-050: MUST include workspace context variable
- FR-051: MUST include date variable
- FR-052: Length MUST be under 4000 tokens
- FR-053: MUST be model-agnostic

### Role Prompt Structure

- FR-054: planner.md MUST focus on decomposition
- FR-055: planner.md MUST emphasize clear steps
- FR-056: planner.md MUST consider dependencies
- FR-057: coder.md MUST focus on implementation
- FR-058: coder.md MUST emphasize correctness
- FR-059: coder.md MUST enforce minimal diff
- FR-060: reviewer.md MUST focus on quality
- FR-061: reviewer.md MUST provide constructive feedback
- FR-062: reviewer.md MUST verify correctness

### Language Prompt Structure

- FR-063: MUST cover naming conventions
- FR-064: MUST cover common idioms
- FR-065: MUST cover error handling
- FR-066: MUST cover common pitfalls
- FR-067: MUST reference official style guides
- FR-068: Length MUST be under 2000 tokens

### Framework Prompt Structure

- FR-069: MUST cover architectural patterns
- FR-070: MUST cover common components
- FR-071: MUST cover testing approaches
- FR-072: MUST cover configuration
- FR-073: Length MUST be under 2000 tokens

### Embedded Resources

- FR-074: Packs MUST be in Resources/PromptPacks/
- FR-075: Resource path: Resources/PromptPacks/{id}/
- FR-076: All files MUST be embedded resources
- FR-077: manifest.yml MUST be included
- FR-078: Content hash MUST be pre-computed

### Template Variables

- FR-079: MUST use {{workspace_name}} in context
- FR-080: MUST use {{date}} for temporal context
- FR-081: MUST use {{language}} when applicable
- FR-082: MUST use {{framework}} when applicable
- FR-083: Variables MUST have fallback values

---

## Non-Functional Requirements

### Quality

- NFR-001: Prompts MUST be clear and unambiguous
- NFR-002: Prompts MUST avoid jargon
- NFR-003: Prompts MUST use consistent terminology
- NFR-004: Prompts MUST be tested with multiple models

### Size

- NFR-005: Total pack size MUST be under 500KB
- NFR-006: System prompt MUST be under 4000 tokens
- NFR-007: Role prompts MUST be under 2000 tokens
- NFR-008: Language prompts MUST be under 2000 tokens

### Compatibility

- NFR-009: Prompts MUST work with Llama models
- NFR-010: Prompts MUST work with Mistral models
- NFR-011: Prompts MUST work with Qwen models
- NFR-012: Prompts MUST work with DeepSeek models

### Maintainability

- NFR-013: Prompts MUST be well-commented
- NFR-014: Pack structure MUST be documented
- NFR-015: Changes MUST be versioned
- NFR-016: Prompts MUST be reviewable

### Security

- NFR-017: No secrets in prompts
- NFR-018: No personal information
- NFR-019: No external URLs
- NFR-020: No prompt injection vulnerabilities

---

## User Manual Documentation

### Overview

Acode ships with three built-in prompt packs that provide ready-to-use configurations for different development scenarios. This guide describes each pack and how to use them.

### Available Starter Packs

| Pack ID | Description | Best For |
|---------|-------------|----------|
| acode-standard | General purpose coding | Multi-language projects, exploration |
| acode-dotnet | .NET/C# development | ASP.NET Core, C# libraries, .NET services |
| acode-react | React/TypeScript | React apps, Next.js, TypeScript projects |

### Quick Start

```bash
# Using default pack (acode-standard)
$ acode run

# Switch to .NET pack
$ acode run --pack acode-dotnet

# Or configure in config file
# .agent/config.yml
prompts:
  pack_id: acode-dotnet
```

### acode-standard Pack

The default pack for general coding tasks.

**Contents:**
```
acode-standard/
├── manifest.yml
├── system.md           # Core agent instructions
└── roles/
    ├── planner.md      # Task planning
    ├── coder.md        # Implementation
    └── reviewer.md     # Code review
```

**Key behaviors:**
- Works with any programming language
- Emphasizes minimal, focused changes
- Balances quality with pragmatism
- Uses clear, step-by-step explanations

**Best for:**
- Multi-language projects
- General coding tasks
- When no specialized pack fits
- Learning and exploration

### acode-dotnet Pack

Specialized for .NET and C# development.

**Contents:**
```
acode-dotnet/
├── manifest.yml
├── system.md
├── roles/
│   ├── planner.md
│   ├── coder.md
│   └── reviewer.md
├── languages/
│   └── csharp.md       # C# conventions
└── frameworks/
    └── aspnetcore.md   # ASP.NET Core patterns
```

**Key behaviors:**
- Follows Microsoft coding conventions
- Uses modern C# features (records, pattern matching)
- Applies async/await correctly
- Understands nullable reference types
- Follows Clean Architecture patterns
- Uses dependency injection properly

**C# guidance includes:**
- Naming: PascalCase for public, _camelCase for private fields
- Async: ConfigureAwait, cancellation tokens
- Nullability: Nullable reference types, null checks
- Common patterns: IOptions, ILogger, Result types

**ASP.NET Core guidance includes:**
- Controllers: Thin controllers, service layer
- DI: Constructor injection, scoped lifetime
- EF Core: DbContext patterns, migrations
- Configuration: Options pattern, secrets

**Best for:**
- ASP.NET Core web APIs
- C# class libraries
- .NET microservices
- Entity Framework projects

### acode-react Pack

Specialized for React and TypeScript development.

**Contents:**
```
acode-react/
├── manifest.yml
├── system.md
├── roles/
│   ├── planner.md
│   ├── coder.md
│   └── reviewer.md
├── languages/
│   └── typescript.md   # TypeScript conventions
└── frameworks/
    └── react.md        # React patterns
```

**Key behaviors:**
- Follows TypeScript strict mode
- Uses functional components with hooks
- Applies React patterns correctly
- Understands component lifecycle
- Follows testing best practices

**TypeScript guidance includes:**
- Types: Interface vs type, generics
- Strict mode: noImplicitAny, strictNullChecks
- Imports: Named imports, path aliases
- Common patterns: Utility types, type guards

**React guidance includes:**
- Components: Functional with hooks
- State: useState, useReducer, context
- Effects: useEffect cleanup, dependencies
- Testing: React Testing Library, mocking

**Best for:**
- React applications
- Next.js projects
- TypeScript frontends
- React component libraries

### Strict Minimal Diff Philosophy

All starter packs enforce the "strict minimal diff" philosophy:

**Core principles:**
1. **Only modify what's necessary** - Don't touch unrelated code
2. **Preserve existing style** - Match the codebase
3. **Don't fix unrelated issues** - Focus on the task
4. **Don't add unrequested features** - Stay in scope
5. **Explain deviations** - Justify any unexpected changes

**Why this matters:**
- Smaller PRs are easier to review
- Focused changes are less risky
- Predictable behavior builds trust
- Easier to understand agent actions

**Example instruction from coder.md:**
```markdown
When implementing changes:
1. Identify the minimum code changes required
2. Do NOT refactor unrelated code
3. Do NOT change code style/formatting elsewhere
4. Do NOT add features not explicitly requested
5. If you must deviate, explain why in your response
```

### Switching Packs

**Via CLI:**
```bash
# One-time override
acode run --pack acode-dotnet

# Show current pack
acode prompts show
```

**Via config:**
```yaml
# .agent/config.yml
prompts:
  pack_id: acode-react
```

**Via environment:**
```bash
export ACODE_PROMPT_PACK=acode-dotnet
acode run
```

### Extending Starter Packs

Create a user pack that builds on a starter pack:

1. Create user pack directory:
   ```bash
   mkdir -p .acode/prompts/my-dotnet
   ```

2. Create manifest with same components:
   ```yaml
   # .acode/prompts/my-dotnet/manifest.yml
   format_version: "1.0"
   id: my-dotnet
   version: 1.0.0
   name: My .NET Pack
   description: Custom .NET pack for our team
   ```

3. Copy and modify components:
   ```bash
   # Start from acode-dotnet as base
   acode prompts export acode-dotnet .acode/prompts/my-dotnet
   ```

4. Edit prompts for your needs
5. Regenerate hash and validate

### Troubleshooting

**Pack not working well:**
- Check you selected the right pack for your project
- Verify language/framework detection is correct
- Try acode-standard for multi-language projects

**Model behavior differs:**
- Starter packs are tested with multiple models
- Some models follow instructions better than others
- Large models (70B+) typically follow better

**Missing language support:**
- Use acode-standard for unsupported languages
- Create a custom pack with language prompts
- Request language pack in issue tracker

---

## Acceptance Criteria

### acode-standard Pack

- [ ] AC-001: Pack id is acode-standard
- [ ] AC-002: Version is 1.0.0
- [ ] AC-003: system.md exists
- [ ] AC-004: roles/planner.md exists
- [ ] AC-005: roles/coder.md exists
- [ ] AC-006: roles/reviewer.md exists
- [ ] AC-007: system.md defines identity
- [ ] AC-008: system.md defines capabilities
- [ ] AC-009: system.md defines limitations
- [ ] AC-010: Strict minimal diff in system.md
- [ ] AC-011: Strict minimal diff in coder.md

### acode-dotnet Pack

- [ ] AC-012: Pack id is acode-dotnet
- [ ] AC-013: Version is 1.0.0
- [ ] AC-014: All standard components present
- [ ] AC-015: languages/csharp.md exists
- [ ] AC-016: frameworks/aspnetcore.md exists
- [ ] AC-017: C# naming conventions covered
- [ ] AC-018: Async/await patterns covered
- [ ] AC-019: Nullable types covered
- [ ] AC-020: DI patterns covered
- [ ] AC-021: Controller conventions covered

### acode-react Pack

- [ ] AC-022: Pack id is acode-react
- [ ] AC-023: Version is 1.0.0
- [ ] AC-024: All standard components present
- [ ] AC-025: languages/typescript.md exists
- [ ] AC-026: frameworks/react.md exists
- [ ] AC-027: Type definitions covered
- [ ] AC-028: Strict mode covered
- [ ] AC-029: Component patterns covered
- [ ] AC-030: Hooks covered
- [ ] AC-031: State management covered

### Strict Minimal Diff

- [ ] AC-032: Only modify necessary code
- [ ] AC-033: Preserve existing style
- [ ] AC-034: Don't fix unrelated issues
- [ ] AC-035: Don't add unrequested features
- [ ] AC-036: Explain deviations
- [ ] AC-037: Appears in system.md
- [ ] AC-038: Reinforced in coder.md
- [ ] AC-039: Reinforced in reviewer.md

### Prompt Quality

- [ ] AC-040: Clear and unambiguous
- [ ] AC-041: Consistent terminology
- [ ] AC-042: Model-agnostic language
- [ ] AC-043: Under token limits
- [ ] AC-044: Template variables work

### Embedded Resources

- [ ] AC-045: Packs in Resources/PromptPacks/
- [ ] AC-046: All files embedded
- [ ] AC-047: Manifest included
- [ ] AC-048: Hash pre-computed

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Resources/
├── StarterPackTests.cs
│   ├── Should_Have_Standard_Pack()
│   ├── Should_Have_DotNet_Pack()
│   ├── Should_Have_React_Pack()
│   ├── Should_Have_Valid_Manifests()
│   └── Should_Have_All_Required_Components()
│
├── PromptContentTests.cs
│   ├── Should_Include_Minimal_Diff_Instructions()
│   ├── Should_Have_Valid_Template_Variables()
│   └── Should_Be_Under_Token_Limits()
```

### Integration Tests

```
Tests/Integration/PromptPacks/
├── StarterPackLoadingTests.cs
│   ├── Should_Load_Standard_Pack()
│   ├── Should_Load_DotNet_Pack()
│   └── Should_Load_React_Pack()
```

### E2E Tests

```
Tests/E2E/PromptPacks/
├── StarterPackE2ETests.cs
│   ├── Should_Use_Standard_By_Default()
│   ├── Should_Switch_To_DotNet()
│   └── Should_Apply_Language_Prompts()
```

### Content Tests

- CONTENT-001: system.md under 4000 tokens
- CONTENT-002: Role prompts under 2000 tokens
- CONTENT-003: Language prompts under 2000 tokens
- CONTENT-004: Total pack under 500KB
- CONTENT-005: No broken template variables

---

## User Verification Steps

### Scenario 1: Default Pack

1. Fresh install of Acode
2. Run `acode prompts list`
3. Verify: acode-standard is active

### Scenario 2: List All Packs

1. Run `acode prompts list`
2. Verify: Three built-in packs shown
3. Verify: Standard, dotnet, react IDs

### Scenario 3: Switch to DotNet

1. Set pack_id to acode-dotnet
2. Run `acode prompts list`
3. Verify: acode-dotnet is active

### Scenario 4: View Pack Contents

1. Run `acode prompts show acode-dotnet`
2. Verify: Shows all components
3. Verify: Shows csharp.md and aspnetcore.md

### Scenario 5: Minimal Diff Behavior

1. Ask agent to make simple change
2. Review the diff
3. Verify: Only requested change made
4. Verify: No style changes elsewhere

### Scenario 6: Language Detection

1. Use acode-dotnet in C# project
2. Request code change
3. Verify: C# conventions applied
4. Verify: Async patterns correct

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/Resources/PromptPacks/
├── acode-standard/
│   ├── manifest.yml
│   ├── system.md
│   └── roles/
│       ├── planner.md
│       ├── coder.md
│       └── reviewer.md
│
├── acode-dotnet/
│   ├── manifest.yml
│   ├── system.md
│   ├── roles/
│   │   ├── planner.md
│   │   ├── coder.md
│   │   └── reviewer.md
│   ├── languages/
│   │   └── csharp.md
│   └── frameworks/
│       └── aspnetcore.md
│
└── acode-react/
    ├── manifest.yml
    ├── system.md
    ├── roles/
    │   ├── planner.md
    │   ├── coder.md
    │   └── reviewer.md
    ├── languages/
    │   └── typescript.md
    └── frameworks/
        └── react.md
```

### system.md Template

```markdown
# Acode Agent

You are Acode, an AI coding assistant running locally on the user's machine.

## Identity
- You are a helpful, precise coding assistant
- You run entirely locally with no external API calls
- You work on the {{workspace_name}} project

## Capabilities
- Read and modify files in the workspace
- Execute terminal commands
- Analyze code and suggest improvements
- Answer questions about the codebase

## Core Principle: Strict Minimal Diff
You MUST make the smallest possible changes:
1. Only modify code necessary for the task
2. Preserve existing code style and formatting
3. Do NOT fix unrelated issues
4. Do NOT add features not explicitly requested
5. Explain any deviation from the request

## Safety
- Never delete files without explicit confirmation
- Never execute destructive commands
- Always explain what you're about to do
```

### csharp.md Template

```markdown
# C# Coding Guidelines

When writing C# code, follow these conventions:

## Naming
- PascalCase for public members
- _camelCase for private fields
- Use meaningful, descriptive names

## Async/Await
- Always use async/await for I/O operations
- Pass CancellationToken through the chain
- Use ConfigureAwait(false) in libraries

## Nullable Reference Types
- Enable nullable reference types
- Use ? for nullable, not null!
- Add null checks for parameters

## Common Patterns
- Use IOptions<T> for configuration
- Use ILogger<T> for logging
- Prefer Result<T> over exceptions for expected failures
```

### Implementation Checklist

1. [ ] Create acode-standard pack structure
2. [ ] Write system.md with identity and constraints
3. [ ] Write planner.md with planning focus
4. [ ] Write coder.md with implementation focus
5. [ ] Write reviewer.md with review focus
6. [ ] Create acode-dotnet pack structure
7. [ ] Write csharp.md with C# conventions
8. [ ] Write aspnetcore.md with ASP.NET patterns
9. [ ] Create acode-react pack structure
10. [ ] Write typescript.md with TS conventions
11. [ ] Write react.md with React patterns
12. [ ] Add manifest.yml to each pack
13. [ ] Compute content hashes
14. [ ] Mark all as embedded resources
15. [ ] Write unit tests
16. [ ] Test with multiple models

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~StarterPack"
```

---

**End of Task 008.c Specification**