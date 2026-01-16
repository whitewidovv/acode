# Task-013a Completion Checklist: Gate Rules + Prompts

**Status:** Implementation Plan Ready
**Total Phases:** 4
**Estimated Total Effort:** 10-14 hours
**Last Updated:** 2026-01-16

---

## INSTRUCTIONS FOR IMPLEMENTING AGENT

This checklist is your implementation roadmap. You can pick it up fresh at any phase and understand exactly what to build. Do not skip phases - work through them sequentially. For each gap:

1. **Read the "Spec Reference"** line to locate the original requirement
2. **Read the "Implementation Details"** section with complete code from the spec
3. **Write failing tests FIRST** (RED) using the test code provided
4. **Implement production code** (GREEN) to make tests pass
5. **Mark complete** when tests pass and no NotImplementedException remains
6. **Move to next gap** and repeat

**Success Criteria:** When ALL gaps marked ✅, task is complete.

---

## WHAT EXISTS CURRENTLY

- ✅ Build infrastructure (dotnet 8.0, xUnit, FluentAssertions)
- ✅ Approval framework in NonInteractive/ (ApprovalManager, ApprovalPolicy) - DO NOT MODIFY
- ✅ PromptPacks system (different from this task) - DO NOT MODIFY
- ✅ YAML parsing capability (YamlDotNet in dependencies)
- ✅ Logging infrastructure (ILogger available)
- ❌ Approvals/Rules/ directory - MUST CREATE
- ❌ Cli/Prompts/ directory - MUST CREATE
- ❌ All gate rule engine files - MUST CREATE
- ❌ All approval prompt files - MUST CREATE

---

## PHASE 1: RULE ENGINE CORE (3-4 hours)

### Gap 1.1: IRule Interface

**Current State:** ❌ MISSING

**Spec Reference:** task-013a.md, Implementation Prompt section, lines 2524-2535

**What Exists:** Nothing - this is a new interface

**What's Missing:**
- IRule interface with Name, Matches, Policy properties
- Defines contract for all rule implementations

**Implementation Details (from spec):**

```csharp
namespace Acode.Application.Approvals.Rules;

public interface IRule
{
    string Name { get; }
    bool Matches(Operation operation);
    ApprovalPolicyType Policy { get; }
}
```

**Required Related Types:**
```csharp
// In same file or separate file - Operation and ApprovalPolicyType
public record Operation(
    OperationCategory Category,
    string Description,
    IReadOnlyDictionary<string, object> Details);

public enum OperationCategory
{
    FileRead,
    FileWrite,
    FileDelete,
    DirectoryCreate,
    TerminalCommand,
    ExternalRequest
}

public enum ApprovalPolicyType
{
    AutoApprove,
    Prompt,
    Deny,
    Skip
}
```

**Test Requirements:**
- Tests should verify IRule can be implemented
- Tests should check Name, Matches, Policy properties work
- See RuleParserTests and RuleEvaluatorTests below for usage examples

**Success Criteria:**
- [ ] File created at src/Acode.Application/Approvals/Rules/IRule.cs
- [ ] File also contains Operation record and OperationCategory/ApprovalPolicyType enums
- [ ] Interface compiles without errors
- [ ] No NotImplementedException in interface
- [ ] All properties and methods match spec

**Acceptance Criteria Covered:** AC-001 (config parsing foundation), AC-005-014 (evaluation foundation), AC-020-037 (all depend on this)

**Gap Checklist Item:** [ ] 🔄 IRule interface created with Operation and enum types, compiles successfully

---

### Gap 1.2: Rule Class Implementation

**Current State:** ❌ MISSING

**Spec Reference:** task-013a.md, Implementation Prompt section, lines 2537-2599

**What Exists:** Nothing

**What's Missing:**
- Rule class implementing IRule
- Pattern matching logic for paths and commands
- Support for category filtering

**Implementation Details (from spec):**

```csharp
namespace Acode.Application.Approvals.Rules;

public sealed class Rule : IRule
{
    private readonly IPatternMatcher _pathMatcher;
    private readonly IPatternMatcher _commandMatcher;

    public string Name { get; }
    public OperationCategory? Category { get; }
    public string PathPattern { get; }
    public string CommandPattern { get; }
    public ApprovalPolicyType Policy { get; }
    public string PromptMessage { get; }

    public Rule(
        string name,
        OperationCategory? category,
        string pathPattern,
        string commandPattern,
        ApprovalPolicyType policy,
        string promptMessage = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Category = category;
        PathPattern = pathPattern;
        CommandPattern = commandPattern;
        Policy = policy;
        PromptMessage = promptMessage;

        if (!string.IsNullOrWhiteSpace(pathPattern))
        {
            _pathMatcher = new GlobMatcher(pathPattern);
        }

        if (!string.IsNullOrWhiteSpace(commandPattern))
        {
            _commandMatcher = new RegexMatcher(commandPattern);
        }
    }

    public bool Matches(Operation operation)
    {
        // Check category if specified
        if (Category.HasValue && operation.Category != Category.Value)
        {
            return false;
        }

        // Check path pattern if specified
        if (_pathMatcher != null)
        {
            if (!operation.Details.TryGetValue("path", out var pathObj))
            {
                return false;
            }

            var path = pathObj?.ToString();
            if (string.IsNullOrEmpty(path) || !_pathMatcher.Matches(path))
            {
                return false;
            }
        }

        // Check command pattern if specified
        if (_commandMatcher != null)
        {
            if (!operation.Details.TryGetValue("command", out var commandObj))
            {
                return false;
            }

            var command = commandObj?.ToString();
            if (string.IsNullOrEmpty(command) || !_commandMatcher.Matches(command))
            {
                return false;
            }
        }

        return true;
    }
}
```

**Test Requirements:**

From RuleParserTests (spec lines 2022-2043):
```csharp
[Fact]
public void Should_Handle_All_Fields()
{
    // Arrange
    var rule = new Rule(
        name: "test-rule",
        category: OperationCategory.TerminalCommand,
        pathPattern: null,
        commandPattern: "^npm (install|ci)$",
        policy: ApprovalPolicyType.Prompt,
        promptMessage: "Install npm packages?");

    // Act & Assert
    Assert.Equal("test-rule", rule.Name);
    Assert.Equal(OperationCategory.TerminalCommand, rule.Category);
    Assert.Equal("^npm (install|ci)$", rule.CommandPattern);
    Assert.Equal(ApprovalPolicyType.Prompt, rule.Policy);
    Assert.Equal("Install npm packages?", rule.PromptMessage);
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Application/Approvals/Rules/Rule.cs
- [ ] Implements IRule interface
- [ ] Constructor accepts all parameters from spec
- [ ] Initializes GlobMatcher for path patterns
- [ ] Initializes RegexMatcher for command patterns
- [ ] Matches() evaluates category, path, command correctly
- [ ] Returns false on category mismatch
- [ ] Returns false on pattern mismatch
- [ ] Returns true when all criteria match
- [ ] No NotImplementedException

**Acceptance Criteria Covered:** AC-001-009 (pattern matching foundation), AC-010-014 (evaluation uses this)

**Gap Checklist Item:** [ ] 🔄 Rule class created, implements IRule, all properties and Matches() method work correctly

---

### Gap 1.3: RuleParser Service

**Current State:** ❌ MISSING

**Spec Reference:** task-013a.md, Implementation Prompt section, lines 2641-2706

**What Exists:** Nothing

**What's Missing:**
- RuleParser that converts YAML to Rule objects
- Configuration validation
- Policy and category enum parsing
- Integration with YamlDotNet

**Implementation Details (from spec - 65 lines provided, shown in 2 parts):**

```csharp
namespace Acode.Application.Approvals.Rules;

public interface IRuleParser
{
    IReadOnlyList<IRule> Parse(string yaml);
}

public sealed class RuleParser : IRuleParser
{
    private readonly ILogger<RuleParser> _logger;

    public RuleParser(ILogger<RuleParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<IRule> Parse(string yaml)
    {
        _logger.LogDebug("Parsing approval rules from YAML");

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize<ApprovalConfig>(yaml);

            if (config?.ApprovalRules == null)
            {
                return Array.Empty<IRule>();
            }

            var rules = new List<IRule>();

            foreach (var ruleDto in config.ApprovalRules)
            {
                ValidateRule(ruleDto);

                var policy = ParsePolicy(ruleDto.Policy);
                var category = ParseCategory(ruleDto.Category);

                var rule = new Rule(
                    Name: ruleDto.Name,
                    Category: category,
                    PathPattern: ruleDto.PathPattern,
                    CommandPattern: ruleDto.CommandPattern,
                    Policy: policy,
                    PromptMessage: ruleDto.PromptMessage);

                rules.Add(rule);
                _logger.LogDebug("Parsed rule: {RuleName} -> {Policy}", rule.Name, policy);
            }

            return rules.AsReadOnly();
        }
        catch (YamlException ex)
        {
            throw new InvalidRuleException("Failed to parse YAML", ex);
        }
    }

    private void ValidateRule(RuleDto rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Name))
        {
            throw new InvalidRuleException("Rule name is required");
        }

        if (string.IsNullOrWhiteSpace(rule.Policy))
        {
            throw new InvalidRuleException($"Policy is required for rule {rule.Name}");
        }

        if (rule.PathPattern == null && rule.CommandPattern == null)
        {
            throw new InvalidRuleException($"Rule {rule.Name} must have either path_pattern or command_pattern");
        }
    }

    private ApprovalPolicyType ParsePolicy(string policy)
    {
        return policy.ToLowerInvariant() switch
        {
            "auto" or "auto_approve" => ApprovalPolicyType.AutoApprove,
            "prompt" => ApprovalPolicyType.Prompt,
            "deny" => ApprovalPolicyType.Deny,
            _ => throw new InvalidRuleException($"Unknown policy: {policy}")
        };
    }

    private OperationCategory? ParseCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        return category.ToLowerInvariant() switch
        {
            "file_read" => OperationCategory.FileRead,
            "file_write" => OperationCategory.FileWrite,
            "file_delete" => OperationCategory.FileDelete,
            "directory_create" => OperationCategory.DirectoryCreate,
            "terminal_command" => OperationCategory.TerminalCommand,
            "external_request" => OperationCategory.ExternalRequest,
            _ => throw new InvalidRuleException($"Unknown category: {category}")
        };
    }
}

public sealed class ApprovalConfig
{
    public List<RuleDto> ApprovalRules { get; set; }
}

public sealed class RuleDto
{
    public string Name { get; set; }
    public string Category { get; set; }
    public string PathPattern { get; set; }
    public string CommandPattern { get; set; }
    public string Policy { get; set; }
    public string PromptMessage { get; set; }
}

public sealed class InvalidRuleException : Exception
{
    public InvalidRuleException(string message) : base(message) { }
    public InvalidRuleException(string message, Exception inner) : base(message, inner) { }
}
```

**Test Requirements:**

From RuleParserTests (spec lines 1982-2020):
```csharp
[Fact]
public void Should_Parse_Valid_Rules()
{
    // Arrange
    var yaml = @"
approval_rules:
  - name: auto-tests
    category: file_write
    path_pattern: '**/*.test.ts'
    policy: auto
  - name: prompt-src
    category: file_write
    path_pattern: 'src/**'
    policy: prompt
";

    // Act
    var rules = _parser.Parse(yaml);

    // Assert
    Assert.Equal(2, rules.Count);
    Assert.Equal("auto-tests", rules[0].Name);
    Assert.Equal(ApprovalPolicyType.AutoApprove, rules[0].Policy);
}

[Fact]
public void Should_Reject_Invalid_Rules()
{
    // Arrange
    var yaml = @"
approval_rules:
  - name: bad-rule
    policy: invalid_policy
";

    // Act & Assert
    Assert.Throws<InvalidRuleException>(() => _parser.Parse(yaml));
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Application/Approvals/Rules/RuleParser.cs
- [ ] File also contains IRuleParser interface, ApprovalConfig, RuleDto, InvalidRuleException
- [ ] Parse() deserializes YAML using YamlDotNet
- [ ] Parse() validates each rule with ValidateRule()
- [ ] Parse() returns empty list for null/empty rules
- [ ] ValidateRule() requires rule name
- [ ] ValidateRule() requires policy
- [ ] ValidateRule() requires path_pattern OR command_pattern
- [ ] ParsePolicy() converts "auto"/"auto_approve" to AutoApprove
- [ ] ParseCategory() converts category strings to OperationCategory enum
- [ ] InvalidRuleException thrown on parse errors
- [ ] Logging includes parsed rule names
- [ ] All test methods pass

**Acceptance Criteria Covered:** AC-001, AC-002, AC-003, AC-004

**Gap Checklist Item:** [ ] 🔄 RuleParser, ApprovalConfig, RuleDto, InvalidRuleException created and all unit tests passing

---

### Gap 1.4: RuleEvaluator Service

**Current State:** ❌ MISSING

**Spec Reference:** task-013a.md, Implementation Prompt section, lines 2741-2766

**What Exists:** Nothing

**What's Missing:**
- RuleEvaluator that applies first-match-wins logic
- Default policy fallback
- Logging of evaluation results

**Implementation Details (from spec):**

```csharp
namespace Acode.Application.Approvals.Rules;

public sealed class RuleEvaluator
{
    private readonly IReadOnlyList<IRule> _rules;
    private readonly ApprovalPolicyType _defaultPolicy;
    private readonly ILogger<RuleEvaluator> _logger;

    public RuleEvaluator(
        IReadOnlyList<IRule> rules,
        ApprovalPolicyType defaultPolicy,
        ILogger<RuleEvaluator> logger)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _defaultPolicy = defaultPolicy;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ApprovalPolicyType Evaluate(Operation operation)
    {
        _logger.LogDebug("Evaluating operation: {Category}", operation.Category);

        foreach (var rule in _rules)
        {
            if (rule.Matches(operation))
            {
                _logger.LogDebug("Rule {Name} matched, returning {Policy}", rule.Name, rule.Policy);
                return rule.Policy;
            }
        }

        _logger.LogDebug("No rule matched, using default policy: {Policy}", _defaultPolicy);
        return _defaultPolicy;
    }
}
```

**Test Requirements:**

From RuleEvaluatorTests (spec lines 2102-2168):
```csharp
[Fact]
public void Should_Evaluate_In_Order()
{
    // Arrange
    var rules = new List<IRule>
    {
        new Rule("first", OperationCategory.FileWrite, "**/*.ts", null, ApprovalPolicyType.AutoApprove),
        new Rule("second", OperationCategory.FileWrite, "src/**", null, ApprovalPolicyType.Prompt)
    };
    var evaluator = new RuleEvaluator(rules, ApprovalPolicyType.Prompt, NullLogger<RuleEvaluator>.Instance);

    var operation = new Operation(
        Category: OperationCategory.FileWrite,
        Description: "Write file",
        Details: new Dictionary<string, object> { { "path", "src/App.ts" } }.AsReadOnly());

    // Act
    var policy = evaluator.Evaluate(operation);

    // Assert
    Assert.Equal(ApprovalPolicyType.AutoApprove, policy); // First rule matches
}

[Fact]
public void Should_Apply_Default_When_No_Match()
{
    // Arrange
    var rules = new List<IRule>
    {
        new Rule("tests", OperationCategory.FileWrite, "**/*.test.ts", null, ApprovalPolicyType.AutoApprove)
    };
    var evaluator = new RuleEvaluator(rules, ApprovalPolicyType.Prompt, NullLogger<RuleEvaluator>.Instance);

    var operation = new Operation(
        Category: OperationCategory.FileWrite,
        Description: "Write file",
        Details: new Dictionary<string, object> { { "path", "src/App.ts" } }.AsReadOnly());

    // Act
    var policy = evaluator.Evaluate(operation);

    // Assert
    Assert.Equal(ApprovalPolicyType.Prompt, policy); // Default policy
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Application/Approvals/Rules/RuleEvaluator.cs
- [ ] Constructor accepts rules list, default policy, logger
- [ ] Evaluate() iterates rules in order
- [ ] Evaluate() returns first matching rule's policy
- [ ] Evaluate() returns default policy when no match
- [ ] Logging includes operation category, matched rule name, returned policy
- [ ] All test methods pass

**Acceptance Criteria Covered:** AC-010, AC-011, AC-012, AC-013, AC-014

**Gap Checklist Item:** [ ] 🔄 RuleEvaluator created, implements first-match-wins logic, all tests passing

---

## PHASE 2: PATTERN MATCHING (2-3 hours)

### Gap 2.1: IPatternMatcher Interface

**Current State:** ❌ MISSING

**Spec Reference:** task-013a.md, Implementation Prompt section, lines 2615-2617

**What Exists:** Nothing

**What's Missing:**
- Common interface for glob and regex matchers

**Implementation Details (from spec):**

```csharp
namespace Acode.Application.Approvals.Rules.Patterns;

public interface IPatternMatcher
{
    bool Matches(string input);
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Application/Approvals/Rules/Patterns/IPatternMatcher.cs
- [ ] Interface defines Matches(string input) method
- [ ] No NotImplementedException

**Acceptance Criteria Covered:** AC-006, AC-007, AC-008, AC-009

**Gap Checklist Item:** [ ] 🔄 IPatternMatcher interface created

---

### Gap 2.2: GlobMatcher Implementation

**Current State:** ❌ MISSING

**Spec Reference:** task-013a.md, Implementation Prompt section, lines 2619-2680

**What Exists:** Nothing

**What's Missing:**
- Glob pattern matching for file paths
- Support for `**`, `*`, `?`, `{}`, `[]` patterns
- Negation support

**Implementation Details (from spec):**

```csharp
namespace Acode.Application.Approvals.Rules.Patterns;

public sealed class GlobMatcher : IPatternMatcher
{
    private readonly string _pattern;
    private readonly bool _negate;
    private readonly Regex _regex;

    public GlobMatcher(string pattern, bool negate = false)
    {
        _pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        _negate = negate;
        _regex = CompileGlobToRegex(pattern);
    }

    public bool Matches(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        var matches = _regex.IsMatch(input);
        return _negate ? !matches : matches;
    }

    private static Regex CompileGlobToRegex(string pattern)
    {
        // Convert glob pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")     // ** matches any path
            .Replace("\\*", "[^/]*")     // * matches within segment
            .Replace("\\?", ".")          // ? matches single char
            + "$";

        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}
```

**Test Requirements:**

From PatternMatcherTests (spec lines 2048-2065):
```csharp
[Theory]
[InlineData("**/*.ts", "src/components/App.ts", true)]
[InlineData("**/*.ts", "src/components/App.tsx", false)]
[InlineData("src/**", "src/main.ts", true)]
[InlineData("src/**", "tests/test.ts", false)]
[InlineData("*.test.ts", "App.test.ts", true)]
[InlineData("*.test.ts", "src/App.test.ts", false)]
public void Should_Match_Glob_Patterns(string pattern, string path, bool expectedMatch)
{
    // Arrange
    var matcher = new GlobMatcher(pattern);

    // Act
    var matches = matcher.Matches(path);

    // Assert
    Assert.Equal(expectedMatch, matches);
}

[Fact]
public void Should_Handle_Negation()
{
    // Arrange
    var matcher = new GlobMatcher("**/*.ts", negate: true);

    // Act
    var matchTs = matcher.Matches("src/App.ts");
    var matchTsx = matcher.Matches("src/App.tsx");

    // Assert
    Assert.False(matchTs); // Negated - *.ts files don't match
    Assert.True(matchTsx);  // Non-*.ts files do match
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Application/Approvals/Rules/Patterns/GlobMatcher.cs
- [ ] Implements IPatternMatcher
- [ ] Constructor accepts pattern and optional negate flag
- [ ] CompileGlobToRegex() converts glob to regex correctly
- [ ] `**` becomes `.*` (matches any path)
- [ ] `*` becomes `[^/]*` (matches within segment only)
- [ ] `?` becomes `.` (matches single char)
- [ ] Matches() returns false for null/empty input
- [ ] Matches() applies negation correctly
- [ ] Regex is compiled with IgnoreCase flag
- [ ] All test cases pass

**Acceptance Criteria Covered:** AC-006, AC-009

**Gap Checklist Item:** [ ] 🔄 GlobMatcher created, all glob pattern tests passing

---

### Gap 2.3: RegexMatcher Implementation

**Current State:** ❌ MISSING

**Spec Reference:** task-013a.md, Implementation Prompt section, lines 2682-2708

**What Exists:** Nothing

**What's Missing:**
- Regex pattern matching for commands
- Validation of regex patterns
- Error handling for invalid patterns

**Implementation Details (from spec):**

```csharp
namespace Acode.Application.Approvals.Rules.Patterns;

public sealed class RegexMatcher : IPatternMatcher
{
    private readonly Regex _regex;

    public RegexMatcher(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));
        }

        try
        {
            _regex = new Regex(pattern, RegexOptions.Compiled);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidRuleException($"Invalid regex pattern: {pattern}", ex);
        }
    }

    public bool Matches(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        return _regex.IsMatch(input);
    }
}
```

**Test Requirements:**

From PatternMatcherTests (spec lines 2067-2082):
```csharp
[Theory]
[InlineData("^npm (install|ci)$", "npm install", true)]
[InlineData("^npm (install|ci)$", "npm run build", false)]
[InlineData("^git push", "git push origin main", true)]
[InlineData("^git push", "git pull", false)]
public void Should_Match_Regex_Patterns(string pattern, string command, bool expectedMatch)
{
    // Arrange
    var matcher = new RegexMatcher(pattern);

    // Act
    var matches = matcher.Matches(command);

    // Assert
    Assert.Equal(expectedMatch, matches);
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Application/Approvals/Rules/Patterns/RegexMatcher.cs
- [ ] Implements IPatternMatcher
- [ ] Constructor validates pattern is not null/empty
- [ ] Constructor creates compiled Regex
- [ ] InvalidRuleException thrown on invalid regex pattern
- [ ] Matches() returns false for null/empty input
- [ ] Matches() uses regex.IsMatch() correctly
- [ ] All test cases pass

**Acceptance Criteria Covered:** AC-007

**Gap Checklist Item:** [ ] 🔄 RegexMatcher created, all regex pattern tests passing

---

## PHASE 3: PROMPT SYSTEM (3-4 hours)

### Gap 3.1: SecretRedactor Service

**Current State:** ❌ MISSING

**Spec Reference:** task-013a.md, Implementation Prompt section, lines 2768-2799

**What Exists:** Nothing

**What's Missing:**
- Secret redaction for API keys, passwords, tokens
- Redaction counting
- Pattern-based detection

**Implementation Details (from spec):**

```csharp
namespace Acode.Cli.Prompts;

public sealed class SecretRedactor
{
    private static readonly Regex[] Patterns = new[]
    {
        new Regex(@"(?i)(api[_-]?key|apikey)\s*[:=]\s*['""]?[\w-]+", RegexOptions.Compiled),
        new Regex(@"(?i)(password|passwd|pwd)\s*[:=]\s*['""]?[^\s,;]+", RegexOptions.Compiled),
        new Regex(@"(?i)(token|secret|key)\s*[:=]\s*['""]?[\w-]+", RegexOptions.Compiled),
    };

    private readonly ILogger<SecretRedactor> _logger;

    public SecretRedactor(ILogger<SecretRedactor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public (string content, int count) Redact(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return (content, 0);
        }

        var count = 0;
        var result = content;

        foreach (var pattern in Patterns)
        {
            result = pattern.Replace(result, m =>
            {
                count++;
                _logger.LogDebug("Redacted secret of type: {PatternIndex}", Array.IndexOf(Patterns, pattern));
                return "[REDACTED]";
            });
        }

        return (result, count);
    }
}
```

**Test Requirements:**

From SecretRedactorTests (spec lines 2171-2227):
```csharp
[Fact]
public void Should_Redact_API_Keys()
{
    // Arrange
    var content = "apiKey = 'sk_test_abc123xyz'";

    // Act
    var (redacted, count) = _redactor.Redact(content);

    // Assert
    Assert.Contains("[REDACTED]", redacted);
    Assert.Equal(1, count);
}

[Fact]
public void Should_Redact_Passwords()
{
    // Arrange
    var content = "password: mySecretPass123";

    // Act
    var (redacted, count) = _redactor.Redact(content);

    // Assert
    Assert.Contains("[REDACTED]", redacted);
    Assert.DoesNotContain("mySecretPass123", redacted);
    Assert.Equal(1, count);
}

[Fact]
public void Should_Count_Redactions()
{
    // Arrange
    var content = @"
apiKey = 'abc123'
password = 'secret'
token = 'xyz789'
";

    // Act
    var (redacted, count) = _redactor.Redact(content);

    // Assert
    Assert.Equal(3, count);
    Assert.DoesNotContain("abc123", redacted);
    Assert.DoesNotContain("secret", redacted);
    Assert.DoesNotContain("xyz789", redacted);
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Cli/Prompts/SecretRedactor.cs
- [ ] Implements Redact() method returning (content, count) tuple
- [ ] Redact() returns (original, 0) for null/empty input
- [ ] Regex patterns detect API keys, passwords, tokens
- [ ] Matched secrets replaced with "[REDACTED]"
- [ ] Count incremented for each redaction
- [ ] Logging includes secret types
- [ ] All test methods pass

**Acceptance Criteria Covered:** AC-034, AC-035, AC-036, AC-037

**Gap Checklist Item:** [ ] 🔄 SecretRedactor created, all redaction tests passing

---

### Gap 3.2: ApprovalPromptRenderer Main Orchestrator

**Current State:** ❌ MISSING

**Spec Reference:** task-013a.md, Implementation Prompt section, lines 2800-2900 (spec doesn't provide complete code, use from description)

**What Exists:** Nothing

**What's Missing:**
- Main prompt renderer orchestrating all components
- Header, preview, options, timeout display
- Input handling and decision flow

**Implementation Details (inferred from spec description - ~100 lines):**

```csharp
namespace Acode.Cli.Prompts;

public sealed class ApprovalPromptRenderer
{
    private readonly IConsole _console; // Abstraction for console I/O
    private readonly SecretRedactor _redactor;
    private readonly ILogger<ApprovalPromptRenderer> _logger;

    private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(5);

    public ApprovalPromptRenderer(
        IConsole console,
        ILogger<ApprovalPromptRenderer> logger)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _redactor = new SecretRedactor(logger);
    }

    public ApprovalDecision Render(Operation operation, TimeSpan? timeout = null)
    {
        _logger.LogInformation("Rendering approval prompt for {Category}", operation.Category);

        var actualTimeout = timeout ?? _defaultTimeout;
        var startTime = DateTime.UtcNow;

        // Render header
        RenderHeader(operation);

        // Render operation details
        RenderOperationDetails(operation);

        // Render preview with redaction
        RenderPreview(operation);

        // Render options
        RenderOptions();

        // Render timeout and wait for input
        var decision = WaitForInput(actualTimeout);

        _logger.LogInformation("Approval decision: {Decision} after {ElapsedMs}ms",
            decision,
            (DateTime.UtcNow - startTime).TotalMilliseconds);

        return decision;
    }

    private void RenderHeader(Operation operation)
    {
        _console.WriteLine("⚠  Approval Required");
        _console.WriteLine("─────────────────────");
    }

    private void RenderOperationDetails(Operation operation)
    {
        _console.WriteLine($"Operation: {operation.Category}");
        _console.WriteLine($"Description: {operation.Description}");

        if (operation.Details.TryGetValue("path", out var path))
        {
            _console.WriteLine($"Path: {path}");
        }
    }

    private void RenderPreview(Operation operation)
    {
        _console.WriteLine("Preview:");

        if (operation.Details.TryGetValue("content", out var content))
        {
            var contentStr = content?.ToString() ?? "";
            var (redacted, count) = _redactor.Redact(contentStr);

            var lines = redacted.Split('\n').Take(10);
            foreach (var line in lines)
            {
                _console.WriteLine($"  {line}");
            }

            if (count > 0)
            {
                _console.WriteLine($"({count} secrets redacted)");
            }
        }
    }

    private void RenderOptions()
    {
        _console.WriteLine("[A]pprove  [D]eny  [S]kip  [V]iew all  [?]Help");
    }

    private ApprovalDecision WaitForInput(TimeSpan timeout)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            _console.Write("Choice: ");

            var key = _console.ReadKey(true);
            var choice = char.ToUpper(key.KeyChar);

            return choice switch
            {
                'A' => ApprovalDecision.Approved,
                'D' => ApprovalDecision.Denied,
                'S' => ApprovalDecision.Skipped,
                'V' => HandleViewAll(),
                '?' => HandleHelp(),
                _ => HandleInvalidInput()
            };
        }

        return ApprovalDecision.TimedOut;
    }

    private ApprovalDecision HandleViewAll() { /* impl */ return ApprovalDecision.Approved; }
    private ApprovalDecision HandleHelp() { /* impl */ return ApprovalDecision.Approved; }
    private ApprovalDecision HandleInvalidInput() { /* impl */ return ApprovalDecision.Approved; }
}

public enum ApprovalDecision
{
    Approved,
    Denied,
    Skipped,
    TimedOut
}

public interface IConsole
{
    void WriteLine(string text);
    void Write(string text);
    ConsoleKeyInfo ReadKey(bool intercept);
}
```

**Test Requirements:**

From PromptIntegrationTests (spec lines 2299-2340):
```csharp
[Fact]
public void Should_Render_Correctly()
{
    // Arrange
    var mockConsole = new Mock<IConsole>();
    var renderer = new ApprovalPromptRenderer(mockConsole.Object, NullLogger<ApprovalPromptRenderer>.Instance);

    var operation = new Operation(
        Category: OperationCategory.FileWrite,
        Description: "Write UserService.cs",
        Details: new Dictionary<string, object>
        {
            { "path", "src/UserService.cs" },
            { "size", 120 }
        }.AsReadOnly());

    // Act
    renderer.Render(operation);

    // Assert
    mockConsole.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("FILE_WRITE"))), Times.AtLeastOnce);
    mockConsole.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("UserService.cs"))), Times.AtLeastOnce);
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Cli/Prompts/ApprovalPromptRenderer.cs
- [ ] Implements Render() method accepting Operation and optional timeout
- [ ] Calls RenderHeader(), RenderOperationDetails(), RenderPreview(), RenderOptions()
- [ ] RenderPreview() integrates with SecretRedactor
- [ ] WaitForInput() handles timeout
- [ ] Input handler recognizes A, D, S, V, ? keys
- [ ] ApprovalDecision enum defined
- [ ] IConsole interface defined for testability
- [ ] All integration tests pass

**Acceptance Criteria Covered:** AC-020, AC-021, AC-022, AC-023, AC-024, AC-025, AC-026, AC-027, AC-028, AC-029, AC-030

**Gap Checklist Item:** [ ] 🔄 ApprovalPromptRenderer and ApprovalDecision created, core rendering working

---

### Gap 3.3: Prompt Components (Header, Preview, Options, Timeout)

**Current State:** ❌ MISSING

**Spec Reference:** task-013a.md, Implementation Prompt section, line descriptions

**What Exists:** Nothing

**What's Missing:**
- Individual component classes for modularity
- Header: warning display, status indicators
- Preview: file content preview with line numbers
- Options: action selection display
- Timeout: countdown timer display

**Implementation Details (provided for structure, can be simplified or detailed per needs):**

Create files:
- src/Acode.Cli/Prompts/PromptComponents/HeaderComponent.cs (~40 lines)
- src/Acode.Cli/Prompts/PromptComponents/PreviewComponent.cs (~50 lines)
- src/Acode.Cli/Prompts/PromptComponents/OptionsComponent.cs (~40 lines)
- src/Acode.Cli/Prompts/PromptComponents/TimeoutComponent.cs (~30 lines)

Each component should have:
- Render(IConsole console) method
- Input from Operation or parameters
- Clean separation of concerns

**Example HeaderComponent:**
```csharp
namespace Acode.Cli.Prompts.PromptComponents;

public sealed class HeaderComponent
{
    private readonly Operation _operation;

    public HeaderComponent(Operation operation)
    {
        _operation = operation ?? throw new ArgumentNullException(nameof(operation));
    }

    public void Render(IConsole console)
    {
        console.WriteLine("⚠  Approval Required");
        console.WriteLine("─────────────────────────────────────");
        console.WriteLine($"Category: {_operation.Category}");
        console.WriteLine($"Description: {_operation.Description}");
    }
}
```

**Test Requirements:**
- Components should be tested for correct output
- Mocked IConsole to verify Write/WriteLine calls
- Verify content includes expected strings

**Success Criteria:**
- [ ] All 4 component files created
- [ ] Each component has Render(IConsole console) method
- [ ] Components work together in ApprovalPromptRenderer
- [ ] Component tests pass

**Acceptance Criteria Covered:** AC-020-025, AC-031, AC-032, AC-033

**Gap Checklist Item:** [ ] 🔄 All prompt components created and rendering correctly

---

## PHASE 4: INTEGRATION & E2E (2-3 hours)

### Gap 4.1: Integration Tests

**Current State:** ❌ MISSING

**Spec Reference:** task-013a.md, Testing Requirements section, lines 2231-2297

**What Exists:** Nothing

**What's Missing:**
- RuleIntegrationTests testing real config loading
- PromptIntegrationTests testing end-to-end prompt flow

**Test Code (from spec):**

```csharp
namespace Acode.Application.Tests.Integration.Approvals.Rules;

public class RuleIntegrationTests
{
    [Fact]
    public void Should_Load_Rules_From_Config()
    {
        // Arrange
        var yaml = @"
approval_rules:
  - name: auto-tests
    category: file_write
    path_pattern: '**/*.test.ts'
    policy: auto
";

        var parser = new RuleParser(NullLogger<RuleParser>.Instance);

        // Act
        var rules = parser.Parse(yaml);

        // Assert
        Assert.Single(rules);
        Assert.Equal("auto-tests", rules[0].Name);
    }

    [Fact]
    public void Should_Merge_With_Defaults()
    {
        // Tests that default rules are provided even with empty config
    }

    [Fact]
    public void Should_Evaluate_Real_Operations()
    {
        // Tests full evaluation pipeline
    }
}
```

**Success Criteria:**
- [ ] tests/Acode.Application.Tests/Approvals/Rules/RuleIntegrationTests.cs created
- [ ] tests/Acode.Application.Tests/Approvals/Rules/PromptIntegrationTests.cs created
- [ ] All integration tests pass
- [ ] Tests verify real config parsing and evaluation

**Acceptance Criteria Covered:** AC-001-037 (all ACs tested in integration context)

**Gap Checklist Item:** [ ] 🔄 Integration tests created and passing

---

### Gap 4.2: E2E Tests

**Current State:** ❌ MISSING

**Spec Reference:** task-013a.md, Testing Requirements section, lines 2344-2417

**What Exists:** Nothing

**What's Missing:**
- E2E tests for complete rule application
- Tests for default rule overrides
- Tests for prompt behavior

**Test Code (from spec - simplified):**

```csharp
namespace Acode.Application.Tests.E2E.Approvals.Rules;

public class RuleE2ETests
{
    [Fact]
    public async Task Should_Apply_Custom_Rules_End_To_End()
    {
        // Arrange
        var yaml = @"
approval_rules:
  - name: auto-tests
    category: file_write
    path_pattern: '**/*.test.ts'
    policy: auto
";

        var parser = new RuleParser(NullLogger<RuleParser>.Instance);
        var rules = parser.Parse(yaml);
        var evaluator = new RuleEvaluator(rules, ApprovalPolicyType.Prompt, NullLogger<RuleEvaluator>.Instance);

        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write test",
            Details: new Dictionary<string, object> { { "path", "src/App.test.ts" } }.AsReadOnly());

        // Act
        var policy = evaluator.Evaluate(operation);

        // Assert - Should auto-approve without prompting
        Assert.Equal(ApprovalPolicyType.AutoApprove, policy);
    }

    [Fact]
    public async Task Should_Override_Defaults()
    {
        // Tests that custom rules override built-in defaults
    }

    [Fact]
    public async Task Should_Show_Correct_Prompts_For_Matched_Rules()
    {
        // Tests prompt matching
    }
}
```

**Success Criteria:**
- [ ] tests/Acode.Application.Tests/E2E/Approvals/Rules/RuleE2ETests.cs created
- [ ] All E2E tests pass
- [ ] Tests verify complete system behavior

**Acceptance Criteria Covered:** AC-001-037 (all ACs in end-to-end context)

**Gap Checklist Item:** [ ] 🔄 E2E tests created and passing

---

### Gap 4.3: Final Verification

**Current State:** ❌ INCOMPLETE

**Success Criteria for Task Completion:**

- [ ] All 13 production files created
- [ ] All 6 test files created with 23+ test methods
- [ ] No NotImplementedException in ANY file
- [ ] No TODO/FIXME indicating incomplete work
- [ ] Build: 0 errors, 0 warnings
- [ ] All tests passing: 23+ tests with 100% pass rate
- [ ] No Approvals/Rules files missing
- [ ] No CLI/Prompts files missing
- [ ] All 37 ACs verified implemented with evidence
- [ ] Gap analysis document updated to 100%
- [ ] All commits pushed to feature branch

**Verification Commands:**
```bash
# Scan for stubs
grep -r "NotImplementedException" src/Acode.Application/Approvals/
grep -r "NotImplementedException" src/Acode.Cli/Prompts/
grep -r "NotImplementedException" tests/Acode*/Approvals/

# Should return: NO MATCHES

# Build
dotnet build

# Should return: 0 errors, 0 warnings

# Test
dotnet test --filter "FullyQualifiedName~Approvals|Prompts"

# Should return: X/X passing (100%)
```

**Gap Checklist Item:** [ ] 🔄 Final verification checklist all items complete

---

## SUMMARY TABLE

| Phase | Description | Hours | ACs Covered | Status |
|-------|-------------|-------|------------|--------|
| 1.1 | IRule Interface | 0.5 | AC-001-037 | Pending |
| 1.2 | Rule Class | 0.5 | AC-001-009 | Pending |
| 1.3 | RuleParser | 1.5 | AC-001-004 | Pending |
| 1.4 | RuleEvaluator | 1 | AC-010-014 | Pending |
| **Phase 1 Total** | **Rule Engine Core** | **3-4 hrs** | **ACs 1-14** | **Pending** |
| 2.1 | IPatternMatcher | 0.5 | AC-006-009 | Pending |
| 2.2 | GlobMatcher | 1 | AC-006 | Pending |
| 2.3 | RegexMatcher | 1 | AC-007 | Pending |
| **Phase 2 Total** | **Pattern Matching** | **2-3 hrs** | **ACs 6-9** | **Pending** |
| 3.1 | SecretRedactor | 1 | AC-034-037 | Pending |
| 3.2 | ApprovalPromptRenderer | 1.5 | AC-020-030 | Pending |
| 3.3 | Prompt Components | 1.5 | AC-031-033 | Pending |
| **Phase 3 Total** | **Prompt System** | **3-4 hrs** | **ACs 20-37** | **Pending** |
| 4.1 | Integration Tests | 1 | AC-001-037 | Pending |
| 4.2 | E2E Tests | 1 | AC-001-037 | Pending |
| 4.3 | Final Verification | 0.5 | AC-001-037 | Pending |
| **Phase 4 Total** | **Integration & E2E** | **2-3 hrs** | **ACs 1-37** | **Pending** |
| | | | | |
| **GRAND TOTAL** | **All Phases** | **10-14 hrs** | **37/37 ACs** | **0% Complete** |

---

## KEY IMPLEMENTATION NOTES

**Dependency Order:**
1. Gap 1.1 (IRule) must come before 1.2 (Rule)
2. Gap 1.2 (Rule) must come before 1.3 (RuleParser)
3. Gaps 2.1-2.3 (Patterns) must come before 1.2 (Rule uses them)
4. Gap 1.4 (RuleEvaluator) can come after 1.1
5. Phase 3 (Prompts) independent from Phase 1-2 but uses Operation type
6. Phase 4 (Tests) depends on all production code

**Suggested Implementation Sequence:**
1. Gap 1.1 (IRule interface)
2. Gap 2.1 (IPatternMatcher interface)
3. Gap 2.2 (GlobMatcher)
4. Gap 2.3 (RegexMatcher)
5. Gap 1.2 (Rule)
6. Gap 1.3 (RuleParser + tests)
7. Gap 1.4 (RuleEvaluator + tests)
8. Gap 3.1 (SecretRedactor)
9. Gap 3.2 (ApprovalPromptRenderer)
10. Gap 3.3 (Prompt Components)
11. Gap 4.1 (Integration Tests)
12. Gap 4.2 (E2E Tests)
13. Gap 4.3 (Final Verification)

**Testing Strategy:**
- TDD: RED (write failing tests) → GREEN (implement) → REFACTOR (cleanup)
- Write tests BEFORE production code
- Use NullLogger<T>.Instance for unit tests
- Use Mock<IConsole> for prompt tests
- Create test fixtures for complex scenarios

---

**End of Implementation Checklist**

Use this document to implement task-013a systematically. Mark items ✅ when complete. Each gap has everything needed to implement it correctly.
